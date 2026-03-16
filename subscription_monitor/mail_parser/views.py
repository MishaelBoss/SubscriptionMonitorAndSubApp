from django.shortcuts import render, redirect, get_object_or_404
from django.contrib.auth.decorators import login_required
from django.contrib import messages
from django.utils import timezone
from django.utils.safestring import mark_safe
from django.http import JsonResponse
from .models import Mailbox, ParsedEmail
from .forms import MailboxForm
from .parsers import EmailParser
from subscriptions.models import Subscription, Payment, Service
from datetime import timedelta
import logging
import threading
import uuid
from rest_framework import viewsets
from rest_framework.permissions import IsAuthenticated
from .models import Mailbox
from rest_framework import serializers
from .serializers import ParsedEmailSerializer

logger = logging.getLogger(__name__)

# Глобальный словарь для хранения прогресса парсинга
progress_data = {}
notification_queues = {}

class MailboxSerializer(serializers.ModelSerializer):
    class Meta:
        model = Mailbox
        extra_kwargs = {'user': {'required': False}}
        fields = '__all__'
        read_only_fields = ['user'] 

class MailboxViewSet(viewsets.ModelViewSet):
    permission_classes = [IsAuthenticated]
    serializer_class = MailboxSerializer

    def get_queryset(self):
        return Mailbox.objects.filter(user=self.request.user)

    def perform_create(self, serializer):
        serializer.save(user=self.request.user)

class ParsedEmailViewSet(viewsets.ModelViewSet):
    permission_classes = [IsAuthenticated]
    serializer_class = ParsedEmailSerializer

    def get_queryset(self):
        queryset = ParsedEmail.objects.filter(mailbox__user=self.request.user)
        
        sub_id = self.request.query_params.get('subscription_id')
        if sub_id:
            from subscriptions.models import Subscription
            try:
                sub = Subscription.objects.get(id=sub_id)
                from django.db.models import Q
                queryset = queryset.filter(
                    Q(processed_subscription_id=sub_id) | 
                    Q(service_name__icontains=sub.name.strip())
                )
            except Subscription.DoesNotExist:
                pass
        
        return queryset.distinct().order_by('-received_date')
    
    def create(self, request, *args, **kwargs):
        serializer = self.get_serializer(data=request.data)
        serializer.is_valid(raise_exception=True)
        
        message_id = serializer.validated_data.get('message_id')
        
        obj, created = ParsedEmail.objects.update_or_create(
            message_id=message_id,
            defaults=serializer.validated_data
        )
        
        status_code = status.HTTP_201_CREATED if created else status.HTTP_200_OK
        return Response(ParsedEmailSerializer(obj).data, status=status_code)

def update_progress(progress_id, processed, total, found, status):
    """Функция обратного вызова для обновления прогресса"""
    if progress_id in progress_data:
        progress_data[progress_id]['processed'] = processed
        progress_data[progress_id]['total'] = total
        progress_data[progress_id]['found'] = found
        progress_data[progress_id]['status'] = status

@login_required
def mailbox_list(request):
    """Список подключенных почтовых ящиков"""
    mailboxes = Mailbox.objects.filter(user=request.user)
    
    user_id = request.user.id
    if user_id in notification_queues and notification_queues[user_id]:
        for notification in notification_queues[user_id]:
            send_email_notification(request, notification)
        notification_queues[user_id] = []
    
    return render(request, 'mail_parser/list.html', {'mailboxes': mailboxes})

@login_required
def mailbox_add(request):
    """Подключение нового почтового ящика"""
    if request.method == 'POST':
        form = MailboxForm(request.POST)
        if form.is_valid():
            mailbox = form.save(commit=False)
            mailbox.user = request.user
            mailbox.password_encrypted = form.cleaned_data['password']
            mailbox.save()
            
            messages.success(request, f'Почтовый ящик {mailbox.email} успешно подключен')
            
            progress_data[mailbox.id] = {
                'processed': 0,
                'total': 0,
                'status': 'Инициализация...',
                'found': 0,
                'start_time': timezone.now().isoformat(),
                'user_id': request.user.id
            }
            
            thread = threading.Thread(target=run_parsing, args=(request.user.id, mailbox.id))
            thread.daemon = True
            thread.start()
            
            return redirect('mail_parser:list')
    else:
        form = MailboxForm()
    
    return render(request, 'mail_parser/form.html', {'form': form, 'title': 'Подключить почтовый ящик'})

@login_required
def mailbox_parse(request, pk):
    """Запуск парсинга по кнопке Старт"""
    mailbox = get_object_or_404(Mailbox, pk=pk, user=request.user)
    
    progress_data[pk] = {
        'processed': 0,
        'total': 0,
        'status': 'Инициализация...',
        'found': 0,
        'start_time': timezone.now().isoformat(),
        'user_id': request.user.id
    }
    
    thread = threading.Thread(target=run_parsing, args=(request.user.id, pk))
    thread.daemon = True
    thread.start()
    
    return JsonResponse({'success': True})

@login_required
def get_progress(request, pk):
    """Получение реального прогресса парсинга"""
    mailbox = get_object_or_404(Mailbox, pk=pk, user=request.user)
    
    progress = progress_data.get(pk, {
        'processed': 0,
        'total': 0,
        'status': 'Нет активного парсинга',
        'found': 0
    })
    
    percentage = 0
    if progress['total'] > 0:
        percentage = round((progress['processed'] / progress['total']) * 100, 1)
    
    return JsonResponse({
        'processed': progress['processed'],
        'total': progress['total'],
        'status': progress['status'],
        'found': progress['found'],
        'percentage': percentage,
        'start_time': progress.get('start_time', '')
    })

@login_required
def clear_progress(request, pk):
    """Очистка прогресса после завершения"""
    if pk in progress_data:
        del progress_data[pk]
    return JsonResponse({'success': True})

def run_parsing(user_id, mailbox_id):
    """Функция парсинга, выполняемая в фоновом потоке"""
    from django.contrib.auth.models import User
    
    try:
        user = User.objects.get(id=user_id)
        mailbox = Mailbox.objects.get(id=mailbox_id, user=user)
        
        progress_data[mailbox_id]['status'] = 'Подключение к почтовому серверу...'
        
        parser = EmailParser(mailbox)
        parser.set_progress_callback(mailbox_id, update_progress)
        
        results, found_subscriptions = parser.parse_all_emails(months=6)
        
        progress_data[mailbox_id]['status'] = 'Сохранение результатов...'
        
        subscription_count = 0
        new_notifications = []
        
        # Создаем подписки из найденных с уникальными ID
        for sub_info in found_subscriptions:
            try:
                unique_id = f"sub_{sub_info['service']}_{sub_info['date']}_{uuid.uuid4().hex[:8]}"
                
                if ParsedEmail.objects.filter(message_id=unique_id).exists():
                    continue
                
                parsed = ParsedEmail.objects.create(
                    mailbox=mailbox,
                    message_id=unique_id,
                    subject=sub_info.get('subject', 'Подписка найдена')[:500],
                    from_email=sub_info.get('from', 'email@service.com')[:255],
                    received_date=timezone.now(),
                    service_name=sub_info['service'][:200],
                    amount=sub_info['amount'],
                    payment_date=sub_info['date'],
                    raw_content='',
                )
                
                sub = create_subscription_from_email(parsed, sub_info)
                if sub:
                    subscription_count += 1
                    progress_data[mailbox_id]['found'] = subscription_count
                    
                    status_icon = "⚠️ ПРОСРОЧЕНА" if sub_info.get('is_overdue') else "✅"
                    notification = {
                        'subject': sub_info.get('subject', 'Подписка найдена'),
                        'from': sub_info.get('from', 'Email сервиса'),
                        'body_preview': f"{status_icon} {sub_info['service']} на сумму {sub_info['amount']}₽",
                        'is_subscription': True,
                        'service': sub_info['service'],
                        'amount': str(sub_info['amount']),
                        'is_overdue': sub_info.get('is_overdue', False),
                        'requires_restore': sub_info.get('requires_restore', False)
                    }
                    new_notifications.append(notification)
                    logger.info(f"✅ Создана подписка из найденных: {sub_info['service']} (просрочена: {sub_info.get('is_overdue', False)})")
                    
            except Exception as e:
                logger.error(f"Error creating subscription from found: {e}")
                continue
        
        # Обрабатываем все результаты
        for i, email_data in enumerate(results):
            try:
                if not email_data:
                    continue
                    
                if ParsedEmail.objects.filter(message_id=email_data.get('uid')).exists():
                    continue
                
                received_date = email_data.get('date')
                if received_date and timezone.is_naive(received_date):
                    received_date = timezone.make_aware(received_date)
                
                parsed = ParsedEmail.objects.create(
                    mailbox=mailbox,
                    message_id=email_data.get('uid', ''),
                    subject=email_data.get('subject', '')[:500],
                    from_email=email_data.get('from', '')[:255],
                    received_date=received_date or timezone.now(),
                    service_name=email_data.get('service', '')[:200] if email_data.get('service') else '',
                    amount=email_data.get('amount'),
                    payment_date=email_data.get('payment_date'),
                    raw_content=email_data.get('body', '')[:10000] if email_data.get('body') else '',
                )
                
                if email_data.get('is_subscription') and email_data.get('service'):
                    existing = Subscription.objects.filter(
                        user=user,
                        service__name__icontains=email_data.get('service'),
                    ).first()
                    
                    if not existing:
                        sub = create_subscription_from_email(parsed, email_data)
                        if sub:
                            subscription_count += 1
                            progress_data[mailbox_id]['found'] = subscription_count
                            logger.info(f"✅ Создана подписка из результата: {email_data.get('service')} (просрочена: {email_data.get('is_overdue', False)})")
                
            except Exception as e:
                logger.error(f"Error processing email in thread: {e}")
                continue
        
        mailbox.last_checked = timezone.now()
        mailbox.save()
        
        if new_notifications:
            if user_id not in notification_queues:
                notification_queues[user_id] = []
            notification_queues[user_id].extend(new_notifications[-2:])
        
        final_count = subscription_count
        progress_data[mailbox_id]['found'] = final_count
        progress_data[mailbox_id]['status'] = f'✅ Завершено! Найдено подписок: {final_count}'
        progress_data[mailbox_id]['processed'] = progress_data[mailbox_id]['total']
        
        logger.info(f"✅ Парсинг завершен для {mailbox.email}. Найдено подписок: {final_count}")
        
    except Exception as e:
        logger.error(f"❌ Error in parsing thread: {e}")
        if mailbox_id in progress_data:
            progress_data[mailbox_id]['status'] = f'❌ Ошибка: {str(e)}'

def create_subscription_from_email(parsed_email, email_data=None):
    """Создание подписки из распарсенного письма с учетом просрочки"""
    try:
        user = parsed_email.mailbox.user
        
        if not parsed_email.service_name:
            logger.warning("No service name in parsed email")
            return None
        
        service_name = parsed_email.service_name.strip()
        if not service_name:
            return None
            
        service, created = Service.objects.get_or_create(
            name__icontains=service_name,
            defaults={
                'name': service_name,
                'is_active': True,
            }
        )
        
        # Определяем статус подписки
        is_overdue = False
        requires_restore = False
        payment_date = parsed_email.payment_date
        
        if email_data:
            is_overdue = email_data.get('is_overdue', False)
            requires_restore = email_data.get('requires_restore', False)
        
        # Проверяем существующую подписку
        existing_subscription = Subscription.objects.filter(
            user=user,
            service=service,
        ).first()
        
        # Рассчитываем следующую дату платежа
        today = timezone.now().date()
        
        # Для просроченных подписок устанавливаем дату на сегодня
        if is_overdue:
            next_payment = today
            logger.info(f"⚠️ Просроченная подписка {service_name}: устанавливаем дату {next_payment}")
        elif payment_date:
            next_payment = payment_date
        else:
            next_payment = today
        
        if existing_subscription:
            # Обновляем существующую подписку
            existing_subscription.amount = parsed_email.amount
            existing_subscription.next_payment_date = next_payment
            existing_subscription.status = 'active'
            existing_subscription.save()
            
            # Создаем платеж
            Payment.objects.create(
                subscription=existing_subscription,
                amount=parsed_email.amount,
                currency='RUB',
                payment_date=payment_date or today,
                source='email',
                source_id=parsed_email.message_id,
                description=f"Из письма: {parsed_email.subject[:100]}" + 
                           (" (ПРОСРОЧЕНА)" if is_overdue else ""),
            )
            
            parsed_email.is_processed = True
            parsed_email.processed_subscription = existing_subscription
            parsed_email.save()
            
            status_text = "⚠️ ПРОСРОЧЕНА" if is_overdue else "✅"
            logger.info(f"{status_text} Обновлена подписка: {service_name} (дата платежа: {next_payment})")
            return existing_subscription
            
        else:
            # Создаем новую подписку
            subscription = Subscription.objects.create(
                user=user,
                service=service,
                name=service_name,
                amount=parsed_email.amount,
                currency='RUB',
                start_date=payment_date or today,
                next_payment_date=next_payment,
                status='active',
                billing_cycle='monthly',
            )
            
            # Создаем платеж
            Payment.objects.create(
                subscription=subscription,
                amount=parsed_email.amount,
                currency='RUB',
                payment_date=payment_date or today,
                source='email',
                source_id=parsed_email.message_id,
                description=f"Автоматически из письма: {parsed_email.subject[:100]}" + 
                           (" (ПРОСРОЧЕНА)" if is_overdue else ""),
            )
            
            parsed_email.is_processed = True
            parsed_email.processed_subscription = subscription
            parsed_email.save()
            
            status_text = "⚠️ ПРОСРОЧЕНА" if is_overdue else "✅"
            logger.info(f"{status_text} Создана подписка: {service_name} (дата платежа: {next_payment})")
            return subscription
        
    except Exception as e:
        logger.error(f"❌ Ошибка создания подписки: {e}")
        if parsed_email:
            parsed_email.error_message = str(e)[:500]
            parsed_email.save()
    
    return None

@login_required
def mailbox_check_recent(request, pk):
    """Быстрая проверка последних 2 писем"""
    mailbox = get_object_or_404(Mailbox, pk=pk, user=request.user)
    
    try:
        parser = EmailParser(mailbox)
        results = parser.run_parse(limit=2)
        
        count = 0
        subscription_count = 0
        recent_notifications = []
        
        for email_data in results:
            if not email_data:
                continue
                
            if ParsedEmail.objects.filter(message_id=email_data.get('uid')).exists():
                continue
            
            received_date = email_data.get('date')
            if received_date and timezone.is_naive(received_date):
                received_date = timezone.make_aware(received_date)
            
            parsed = ParsedEmail.objects.create(
                mailbox=mailbox,
                message_id=email_data.get('uid', ''),
                subject=email_data.get('subject', '')[:500],
                from_email=email_data.get('from', '')[:255],
                received_date=received_date or timezone.now(),
                service_name=email_data.get('service', '')[:200] if email_data.get('service') else '',
                amount=email_data.get('amount'),
                payment_date=email_data.get('payment_date'),
                raw_content=email_data.get('body', '')[:10000] if email_data.get('body') else '',
            )
            count += 1
            
            status_icon = "⚠️ ПРОСРОЧЕНА" if email_data.get('is_overdue') else "✅"
            email_info = {
                'subject': email_data.get('subject', ''),
                'from': email_data.get('from', ''),
                'body_preview': (email_data.get('body', '')[:200] + '...') if email_data.get('body') and len(email_data.get('body', '')) > 200 else email_data.get('body', ''),
                'is_subscription': email_data.get('is_subscription', False),
                'service': email_data.get('service', ''),
                'amount': str(email_data.get('amount')) if email_data.get('amount') else None,
                'is_overdue': email_data.get('is_overdue', False),
                'requires_restore': email_data.get('requires_restore', False)
            }
            
            recent_notifications.append(email_info)
            
            if email_data.get('is_subscription') and email_data.get('service'):
                sub = create_subscription_from_email(parsed, email_data)
                if sub:
                    subscription_count += 1
        
        mailbox.last_checked = timezone.now()
        mailbox.save()
        
        for email_info in recent_notifications[-2:]:
            send_email_notification(request, email_info)
        
        messages.success(
            request, 
            f'✅ Быстрая проверка завершена. Найдено {count} новых писем, из них {subscription_count} подписок.'
        )
        
    except Exception as e:
        logger.error(f"Error in quick check: {e}")
        messages.error(request, f'Ошибка при проверке: {str(e)}')
    
    return redirect('mail_parser:list')

def send_email_notification(request, email_info):
    """Отправка уведомления о письме"""
    if not email_info:
        return
    
    if email_info.get('is_overdue'):
        color = '#dc3545'  # Красный для просрочки
        title = "⚠️ ПРОСРОЧЕННАЯ ПОДПИСКА!"
    elif email_info.get('is_subscription'):
        color = '#28a745'  # Зеленый для обычной подписки
        title = "📦 НАЙДЕНА ПОДПИСКА!"
    else:
        color = '#6c757d'  # Серый для обычных писем
        title = "📧 Новое письмо"
    
    restore_text = " 🔄 ТРЕБУЕТ ВОССТАНОВЛЕНИЯ" if email_info.get('requires_restore') else ""
    
    notification_html = f'''
    <div class="email-notification" style="border-left: 4px solid {color}; padding: 10px;">
        <strong>{title}{restore_text}</strong><br>
        <small>От: {email_info.get('from', 'Неизвестно')}</small><br>
        <small>Тема: {email_info.get('subject', 'Без темы')}</small><br>
        <div style="margin-top: 8px; padding: 8px; background: #f8f9fa; border-radius: 4px; max-height: 100px; overflow-y: auto; font-size: 12px;">
            {email_info.get('body_preview', 'Нет текста')}
        </div>
        {f'<span class="badge bg-success mt-2">💰 Сумма: {email_info["amount"]} ₽</span>' if email_info.get('amount') else ''}
        {f'<span class="badge bg-info mt-2">📦 Сервис: {email_info["service"]}</span>' if email_info.get('service') else ''}
        {f'<span class="badge bg-danger mt-2">⚠️ Просрочена</span>' if email_info.get('is_overdue') else ''}
    </div>
    '''
    
    messages.info(request, mark_safe(notification_html))

@login_required
def mailbox_edit(request, pk):
    """Редактирование почтового ящика"""
    mailbox = get_object_or_404(Mailbox, pk=pk, user=request.user)
    
    if request.method == 'POST':
        form = MailboxForm(request.POST, instance=mailbox)
        if form.is_valid():
            mailbox = form.save(commit=False)
            if form.cleaned_data['password']:
                mailbox.password_encrypted = form.cleaned_data['password']
            mailbox.save()
            messages.success(request, f'Настройки ящика {mailbox.email} обновлены')
            return redirect('mail_parser:list')
    else:
        initial = {'password': mailbox.password_encrypted}
        form = MailboxForm(instance=mailbox, initial=initial)
    
    return render(request, 'mail_parser/form.html', {'form': form, 'title': 'Редактировать почтовый ящик'})

@login_required
def mailbox_delete(request, pk):
    """Удаление почтового ящика"""
    mailbox = get_object_or_404(Mailbox, pk=pk, user=request.user)
    
    if request.method == 'POST':
        email = mailbox.email
        mailbox.delete()
        if pk in progress_data:
            del progress_data[pk]
        messages.success(request, f'Почтовый ящик {email} удален')
        return redirect('mail_parser:list')
    
    return render(request, 'mail_parser/confirm_delete.html', {'mailbox': mailbox})

@login_required
def parsed_emails(request):
    """Список распарсенных писем"""
    emails = ParsedEmail.objects.filter(mailbox__user=request.user).order_by('-received_date')
    context = {
        'emails': emails,
    }
    return render(request, 'mail_parser/parsed_emails.html', context)

@login_required
def clear_session_data(request):
    """Очистка данных сессии"""
    messages.success(request, 'История проверки очищена')
    return redirect('mail_parser:emails')