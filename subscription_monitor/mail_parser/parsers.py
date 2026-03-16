import imaplib
import email
import re
from email.utils import parsedate_to_datetime
from datetime import datetime, date, timedelta
import logging
from decimal import Decimal, InvalidOperation
import time
from django.utils import timezone
import base64
import quopri

logger = logging.getLogger(__name__)

def decode_mime_string(encoded_str):
    """Декодирование MIME строк (например, =?utf-8?b?...?=)"""
    if not encoded_str or '=?' not in encoded_str:
        return encoded_str
    
    try:
        # Пробуем декодировать через email.header
        from email.header import decode_header
        decoded_parts = []
        for part, encoding in decode_header(encoded_str):
            if isinstance(part, bytes):
                if encoding:
                    decoded_parts.append(part.decode(encoding, errors='ignore'))
                else:
                    decoded_parts.append(part.decode('utf-8', errors='ignore'))
            else:
                decoded_parts.append(part)
        return ' '.join(decoded_parts)
    except:
        return encoded_str

class EmailParser:
    """Парсер email сообщений для поиска подписок"""
    
    def __init__(self, mailbox):
        self.mailbox = mailbox
        self.connection = None
        self.total_emails = 0
        self.processed = 0
        self.found_subscriptions = 0
        self.subscriptions_found = []
        
        # Парсим письма за последние 6 месяцев
        self.months_to_parse = 6
        self.max_emails_to_parse = 500
        
        # ID для отслеживания прогресса
        self.progress_id = None
        self.progress_callback = None
        
        # Ключевые слова для поиска подписок
        self.subscription_keywords = [
            'subscription', 'подписк', 'ежемесячная', 'ежегодная',
            'payment', 'платеж', 'списание', 'автоплатеж',
            'receipt', 'чек', 'invoice', 'счет', 'billing',
            'monthly', 'ежемесяч', 'yearly', 'ежегодн',
            'renew', 'продлен', 'автоматическое продление',
            'ваш счет', 'оплата подписки', 'списание средств',
            'prime', 'premium', 'plus', 'pro', 'fizovod', 'fizo vod',
            # Слова для определения просрочки
            'остановлен', 'приостановлен', 'заблокирован', 'деактивирован',
            'просрочен', 'истек', 'закончился', 'требует оплаты',
            'восстановить', 'разблокировать', 'активировать', 'zero+'
        ]
        
        # Слова для ИСКЛЮЧЕНИЯ (НЕ подписки)
        self.exclude_keywords = [
            'uptimerobot', 'monitor', 'мониторинг', 'alert', 'оповещение',
            'down', 'авария', 'сбой', 'ошибка', 'error', 'fail',
            'yaklass', 'якласс', 'leadteh', 'uchi.ru',
            'promo', 'промо', 'промокод', 'discount', 'скидка', 'sale', 'распродажа',
            'акция', 'акции', 'спецпредложение', 'выгода',
            'newsletter', 'рассылка', 'новости', 'news', 'update', 'обновление',
            'welcome', 'добро пожаловать', 'приветствие', 'invitation', 'приглашение',
            'security', 'безопасность', 'password', 'пароль', 'login', 'вход',
            'verification', 'верификация', 'confirm', 'подтверждение',
            'facebook', 'instagram', 'twitter', 'tiktok',
            'delivery', 'доставка', 'order', 'заказ', 'tracking', 'отслеживание',
            'statement', 'выписка', 'balance', 'баланс', 'transaction', 'транзакция'
        ]
        
        # Известные сервисы подписок
        self.known_services = {
            'netflix': 'Netflix',
            'yandex plus': 'Яндекс.Плюс',
            'kinopoisk': 'Кинопоиск',
            'spotify': 'Spotify',
            'apple music': 'Apple Music',
            'apple tv': 'Apple TV+',
            'youtube premium': 'YouTube Premium',
            'google one': 'Google One',
            'telegram premium': 'Telegram Premium',
            'vk combo': 'VK Combo',
            'ivi': 'IVI',
            'okko': 'Okko',
            'wink': 'Wink',
            'prime': 'Prime',
            'skillbox': 'Skillbox',
            'geekbrains': 'GeekBrains',
            'stepik': 'Stepik',
            'coursera': 'Coursera',
            'udemy': 'Udemy',
            'dropbox': 'Dropbox',
            'google drive': 'Google Drive',
            'yandex disk': 'Яндекс.Диск',
            'icloud': 'iCloud',
            'microsoft 365': 'Microsoft 365',
            'adobe': 'Adobe Creative Cloud',
            'canva': 'Canva',
            'figma': 'Figma',
            'fizovod': 'FizoVod',
            'fizo vod': 'FizoVod',
            'zero+': 'Zero+',
            'zero plus': 'Zero+',
        }
    
    def set_progress_callback(self, progress_id, callback):
        """Установка callback для обновления прогресса"""
        self.progress_id = progress_id
        self.progress_callback = callback
    
    def update_progress(self):
        """Обновление прогресса через callback"""
        if self.progress_callback and self.progress_id:
            percentage = (self.processed / self.total_emails) * 100 if self.total_emails > 0 else 0
            self.progress_callback(
                self.progress_id,
                self.processed,
                self.total_emails,
                self.found_subscriptions,
                f'Обработка писем... Найдено подписок: {self.found_subscriptions}'
            )
    
    def connect(self):
        """Подключение к почтовому серверу"""
        try:
            server = self.mailbox.imap_server or self._get_default_server()
            self.connection = imaplib.IMAP4_SSL(server, self.mailbox.imap_port)
            self.connection.login(self.mailbox.email, self.mailbox.password_encrypted)
            logger.info(f"✅ Connected to {self.mailbox.email}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to connect: {e}")
            return False
    
    def _get_default_server(self):
        providers = {
            'gmail': 'imap.gmail.com',
            'yandex': 'imap.yandex.ru',
            'mailru': 'imap.mail.ru',
            'outlook': 'imap-mail.outlook.com',
        }
        return providers.get(self.mailbox.provider, 'imap.gmail.com')
    
    def disconnect(self):
        if self.connection:
            try:
                self.connection.close()
                self.connection.logout()
            except:
                pass
    
    def get_emails_since_date(self, since_date):
        """Получить письма с определенной даты"""
        if not self.connect():
            return []
        
        try:
            self.connection.select('INBOX')
            
            since_str = since_date.strftime("%d-%b-%Y")
            logger.info(f"🔍 Поиск писем с {since_str}")
            
            result, data = self.connection.uid('search', None, f'SINCE {since_str}')
            if result != 'OK':
                return []
            
            uids = data[0].split()
            if not uids:
                logger.info("📭 Писем не найдено")
                return []
            
            self.total_emails = len(uids)
            logger.info(f"📧 Найдено писем: {self.total_emails}")
            
            if self.total_emails > self.max_emails_to_parse:
                uids = uids[-self.max_emails_to_parse:]
                logger.info(f"📊 Парсим последние {self.max_emails_to_parse} писем")
                self.total_emails = len(uids)
            
            self.update_progress()
            
            results = []
            self.processed = 0
            self.found_subscriptions = 0
            self.subscriptions_found = []
            
            for i, uid in enumerate(uids):
                try:
                    msg = self.fetch_email(uid)
                    if msg:
                        parsed = self.process_email(msg)
                        if parsed:
                            parsed['uid'] = uid.decode() if isinstance(uid, bytes) else uid
                            results.append(parsed)
                            
                            if parsed.get('is_subscription'):
                                self.found_subscriptions += 1
                                self.subscriptions_found.append({
                                    'service': parsed.get('service'),
                                    'amount': parsed.get('amount'),
                                    'date': parsed.get('payment_date'),
                                    'subject': parsed.get('subject'),
                                    'from': parsed.get('from'),
                                    'is_overdue': parsed.get('is_overdue', False),
                                    'requires_restore': parsed.get('requires_restore', False)
                                })
                    
                    self.processed += 1
                    
                    if self.processed % 5 == 0 or self.processed == self.total_emails:
                        self.update_progress()
                        if self.processed % 50 == 0:
                            percentage = (self.processed / self.total_emails) * 100
                            logger.info(f"📊 Прогресс: {self.processed}/{self.total_emails} ({percentage:.1f}%) - Найдено подписок: {self.found_subscriptions}")
                    
                except Exception as e:
                    logger.error(f"❌ Error processing UID {uid}: {e}")
                    self.processed += 1
            
            self.update_progress()
            logger.info(f"✅ Завершено! Обработано {self.processed} писем, найдено {self.found_subscriptions} подписок")
            return results
            
        except Exception as e:
            logger.error(f"❌ Fatal error: {e}")
            return []
        finally:
            self.disconnect()
    
    def fetch_email(self, uid):
        """Получить письмо с повторными попытками"""
        max_retries = 3
        for attempt in range(max_retries):
            try:
                result, data = self.connection.uid('fetch', uid, '(BODY.PEEK[])')
                if result == 'OK':
                    return email.message_from_bytes(data[0][1])
                else:
                    time.sleep(1)
            except Exception as e:
                if attempt < max_retries - 1:
                    time.sleep(2)
                    self.reconnect()
                else:
                    logger.error(f"❌ Failed to fetch UID {uid}: {e}")
        return None
    
    def reconnect(self):
        """Переподключение"""
        try:
            self.disconnect()
            time.sleep(1)
            self.connect()
        except:
            pass
    
    def parse_all_emails(self, months=6):
        """Парсинг писем за последние N месяцев"""
        since_date = datetime.now().date() - timedelta(days=30 * months)
        logger.info(f"📅 Парсим письма за последние {months} месяцев (с {since_date})")
        self.subscriptions_found = []
        results = self.get_emails_since_date(since_date)
        return results, self.subscriptions_found
    
    def is_subscription_email(self, subject, from_email, body):
        """Проверка на подписку"""
        text = f"{subject} {from_email} {body}".lower()
        
        for exclude in self.exclude_keywords:
            if exclude.lower() in text:
                return False
        
        amount = self.parse_amount(text)
        if not amount:
            return False
        
        has_subscription_word = 'подписк' in text
        has_known_service = any(s.lower() in text for s in self.known_services.keys())
        has_payment_word = any(word in text for word in ['оплат', 'списан', 'стоим', 'руб', '₽'])
        has_overdue_word = any(word in text for word in ['остановлен', 'просрочен', 'истек', 'закончился'])
        has_date = self.parse_date(text) is not None
        
        if has_subscription_word and amount:
            return True
        
        if has_known_service and amount:
            return True
        
        if amount and (has_payment_word or has_overdue_word):
            return True
        
        return False
    
    def parse_amount(self, text):
        """Извлечение суммы"""
        patterns = [
            r'(\d+[.,]\d{2})\s*[₽$€]',
            r'(\d+)\s*[₽$€]',
            r'(\d+[.,]\d{2})\s*(руб|rub|₽)',
            r'(\d+)\s*(руб|rub|₽)',
            r'(\d+)\s*₽',
            r'(\d+)\s*руб(?:лей)?',
            r'сумму\s*(\d+)',
        ]
        
        for pattern in patterns:
            match = re.search(pattern, text, re.IGNORECASE)
            if match:
                try:
                    for group in match.groups():
                        if group and re.search(r'\d', group):
                            amount_str = group.replace(',', '.').strip()
                            amount_str = re.sub(r'[^\d.]', '', amount_str)
                            if amount_str:
                                amount = Decimal(amount_str)
                                if 10 <= amount <= 100000:
                                    return amount
                except:
                    continue
        return None
    
    def parse_service_name(self, subject, from_email, body):
        """Определение сервиса с декодированием"""
        # Декодируем subject и from_email
        decoded_subject = decode_mime_string(subject)
        decoded_from = decode_mime_string(from_email)
        
        text = f"{decoded_subject} {decoded_from} {body}".lower()
        
        for key, name in self.known_services.items():
            if key.lower() in text:
                return name
        
        if 'zero+' in text or 'zero plus' in text:
            return 'Zero+'
        
        if 'prime' in text:
            return 'Prime'
        
        match = re.search(r'@([^.]+)\.', decoded_from)
        if match:
            domain = match.group(1).capitalize()
            if domain.lower() not in ['gmail', 'yandex', 'mail', 'outlook']:
                return domain
        
        return None
    
    def parse_date(self, text):
        """Извлечение даты"""
        patterns = [
            r'(\d{2})[./](\d{2})[./](\d{4})',
            r'до (\d{2})[./](\d{2})[./](\d{4})',
            r'(\d{4})[-](\d{2})[-](\d{2})',
        ]
        
        for pattern in patterns:
            match = re.search(pattern, text)
            if match:
                try:
                    groups = match.groups()
                    if len(groups) > 3:
                        groups = groups[-3:]
                    
                    if len(groups) == 3:
                        if '-' in str(pattern):
                            return date(int(groups[0]), int(groups[1]), int(groups[2]))
                        elif len(groups[2]) == 4:
                            return date(int(groups[2]), int(groups[1]), int(groups[0]))
                except:
                    continue
        return None
    
    def extract_text_from_email(self, msg):
        """Извлечение текста"""
        body_text = ""
        
        if msg.is_multipart():
            for part in msg.walk():
                if part.get_content_type() == "text/plain":
                    payload = part.get_payload(decode=True)
                    if payload:
                        try:
                            body_text += payload.decode('utf-8', errors='ignore')
                        except:
                            pass
                elif part.get_content_type() == "text/html":
                    payload = part.get_payload(decode=True)
                    if payload:
                        try:
                            html = payload.decode('utf-8', errors='ignore')
                            text = re.sub(r'<[^>]+>', ' ', html)
                            text = re.sub(r'\s+', ' ', text)
                            body_text += text
                        except:
                            pass
        else:
            payload = msg.get_payload(decode=True)
            if payload:
                try:
                    body_text += payload.decode('utf-8', errors='ignore')
                except:
                    pass
        
        return body_text
    
    def process_email(self, msg):
        """Обработка письма"""
        try:
            result = {
                'subject': '',
                'from': '',
                'date': None,
                'body': '',
                'is_subscription': False,
                'amount': None,
                'service': None,
                'payment_date': None,
                'is_overdue': False,
                'requires_restore': False,
            }
            
            # Декодируем заголовки
            raw_subject = msg.get('Subject', '')
            raw_from = msg.get('From', '')
            
            result['subject'] = decode_mime_string(raw_subject)
            result['from'] = decode_mime_string(raw_from)
            
            date_str = msg.get('Date')
            if date_str:
                try:
                    naive_date = parsedate_to_datetime(date_str)
                    if naive_date:
                        result['date'] = timezone.make_aware(naive_date) if timezone.is_naive(naive_date) else naive_date
                except:
                    pass
            
            result['body'] = self.extract_text_from_email(msg)
            
            result['is_subscription'] = self.is_subscription_email(
                result['subject'], result['from'], result['body']
            )
            
            if result['is_subscription']:
                result['amount'] = self.parse_amount(result['body'])
                result['service'] = self.parse_service_name(
                    result['subject'], result['from'], result['body']
                )
                result['payment_date'] = self.parse_date(result['body'])
                
                if not result['payment_date'] and result['date']:
                    result['payment_date'] = result['date'].date()
                
                text = f"{result['subject']} {result['body']}".lower()
                
                overdue_indicators = ['остановлен', 'приостановлен', 'заблокирован', 'просрочен', 'истек']
                restore_indicators = ['восстановить', 'разблокировать', 'активировать']
                
                for indicator in overdue_indicators:
                    if indicator in text:
                        result['is_overdue'] = True
                        logger.info(f"⚠️ Обнаружена просроченная подписка: {result.get('service')}")
                        break
                
                for indicator in restore_indicators:
                    if indicator in text:
                        result['requires_restore'] = True
                        logger.info(f"🔄 Требуется восстановление подписки: {result.get('service')}")
                        break
                
                if result['service']:
                    status_text = "⚠️ ПРОСРОЧЕНА" if result['is_overdue'] else "✅ АКТИВНА"
                    restore_text = " 🔄 ТРЕБУЕТ ВОССТАНОВЛЕНИЯ" if result['requires_restore'] else ""
                    logger.info(f"{status_text}{restore_text}: {result['service']}, {result['amount']}₽, {result['payment_date']}")
                    return result
                else:
                    logger.debug(f"❌ Сервис не определен: {result['subject'][:50]}")
                    return None
            
            return result
            
        except Exception as e:
            logger.error(f"Error processing email: {e}")
            return None
    
    def run_parse(self, limit=2):
        """Быстрая проверка последних писем"""
        if not self.connect():
            return []
        
        try:
            self.connection.select('INBOX')
            result, data = self.connection.uid('search', None, 'ALL')
            if result != 'OK':
                return []
            
            uids = data[0].split()
            if not uids:
                return []
            
            recent_uids = uids[-limit:]
            results = []
            
            for uid in recent_uids:
                msg = self.fetch_email(uid)
                if msg:
                    parsed = self.process_email(msg)
                    if parsed:
                        parsed['uid'] = uid.decode() if isinstance(uid, bytes) else uid
                        results.append(parsed)
            
            return results
            
        finally:
            self.disconnect()