import logging
from celery import shared_task
from django.utils import timezone
from datetime import timedelta
from .models import Mailbox, ParsedEmail
from .parsers import EmailParser
from subscriptions.models import Subscription, Payment, Service

logger = logging.getLogger(__name__)

@shared_task
def check_all_mailboxes():
    """Периодическая проверка всех активных почтовых ящиков"""
    mailboxes = Mailbox.objects.filter(is_active=True)
    
    for mailbox in mailboxes:
        # Проверяем, не проверяли ли ящик слишком часто
        if mailbox.last_checked:
            next_check = mailbox.last_checked + timedelta(minutes=mailbox.check_frequency)
            if timezone.now() < next_check:
                continue
        
        check_mailbox.delay(mailbox.id)
    
    return f"Scheduled checks for {mailboxes.count()} mailboxes"

@shared_task
def check_mailbox(mailbox_id):
    """Проверка конкретного почтового ящика"""
    try:
        mailbox = Mailbox.objects.get(id=mailbox_id, is_active=True)
    except Mailbox.DoesNotExist:
        logger.error(f"Mailbox {mailbox_id} not found or inactive")
        return f"Mailbox {mailbox_id} not found"
    
    logger.info(f"Checking mailbox {mailbox.email}")
    
    from .parsers import EmailParser
    parser = EmailParser(mailbox)
    parsed_emails = parser.run_parse(limit=50)
    
    created_count = 0
    for email_data in parsed_emails:
        # Проверяем, не обрабатывали ли уже это письмо
        if ParsedEmail.objects.filter(message_id=email_data.get('uid')).exists():
            continue
        
        # Создаем запись о распарсенном письме
        parsed = ParsedEmail.objects.create(
            mailbox=mailbox,
            message_id=email_data.get('uid', ''),
            subject=email_data.get('subject', '')[:500],
            from_email=email_data.get('from', '')[:255],
            received_date=email_data.get('date') or timezone.now(),
            service_name=email_data.get('service', '')[:200],
            amount=email_data.get('amount'),
            payment_date=email_data.get('payment_date'),
            next_payment_date=email_data.get('next_payment_date'),
            raw_content=email_data.get('body', '')[:10000],
        )
        
        created_count += 1
        
        # Если есть сумма и название сервиса, пробуем создать подписку
        if parsed.amount and parsed.service_name:
            process_parsed_email.delay(parsed.id)
    
    mailbox.last_checked = timezone.now()
    mailbox.save()
    
    logger.info(f"Created {created_count} new parsed emails for {mailbox.email}")
    return f"Processed {created_count} emails for {mailbox.email}"

@shared_task
def process_parsed_email(parsed_email_id):
    """Обработка распарсенного письма - создание подписки или платежа"""
    try:
        parsed = ParsedEmail.objects.get(id=parsed_email_id, is_processed=False)
    except ParsedEmail.DoesNotExist:
        return f"Parsed email {parsed_email_id} not found"
    
    try:
        user = parsed.mailbox.user
        
        # Ищем или создаем сервис
        service, created = Service.objects.get_or_create(
            name__icontains=parsed.service_name,
            defaults={
                'name': parsed.service_name,
                'is_active': True,
            }
        )
        
        # Ищем существующую активную подписку
        subscription = Subscription.objects.filter(
            user=user,
            service=service,
            status='active'
        ).first()
        
        if not subscription and parsed.amount:
            # Создаем новую подписку
            from datetime import timedelta
            next_payment = parsed.next_payment_date or (parsed.payment_date + timedelta(days=30)) if parsed.payment_date else timezone.now().date() + timedelta(days=30)
            
            subscription = Subscription.objects.create(
                user=user,
                service=service,
                name=parsed.service_name,
                amount=parsed.amount or 0,
                currency=parsed.currency,
                start_date=parsed.payment_date or timezone.now().date(),
                next_payment_date=next_payment,
                status='active',
                billing_cycle='monthly',
            )
            logger.info(f"Created new subscription: {subscription}")
        
        # Создаем платеж, если есть подписка
        if subscription and parsed.amount:
            payment = Payment.objects.create(
                subscription=subscription,
                amount=parsed.amount or 0,
                currency=parsed.currency,
                payment_date=parsed.payment_date or timezone.now().date(),
                source='email',
                source_id=parsed.message_id,
                description=f"Автоматически из письма: {parsed.subject[:100]}",
            )
        
        parsed.is_processed = True
        parsed.processed_subscription = subscription
        parsed.save()
        
        logger.info(f"Successfully processed email {parsed.id} for user {user.username}")
        return f"Processed email {parsed.id}"
        
    except Exception as e:
        parsed.error_message = str(e)[:500]
        parsed.save()
        logger.error(f"Error processing email {parsed.id}: {e}")
        return f"Error: {str(e)}"