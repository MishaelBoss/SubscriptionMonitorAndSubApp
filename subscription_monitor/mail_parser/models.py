from django.db import models
from django.contrib.auth.models import User

class Mailbox(models.Model):
    MAIL_PROVIDERS = [
        ('gmail', 'Gmail'),
        ('yandex', 'Yandex'),
        ('mailru', 'Mail.ru'),
        ('outlook', 'Outlook'),
        ('other', 'Другой'),
    ]
    
    user = models.ForeignKey(User, on_delete=models.CASCADE, related_name='mailboxes')
    email = models.EmailField()
    provider = models.CharField(max_length=20, choices=MAIL_PROVIDERS)
    imap_server = models.CharField(max_length=255, blank=True)
    imap_port = models.IntegerField(default=993)
    password_encrypted = models.TextField()
    is_active = models.BooleanField(default=True)
    last_checked = models.DateTimeField(null=True, blank=True)
    check_frequency = models.IntegerField(default=60)
    search_folder = models.CharField(max_length=100, default='INBOX')
    search_criteria = models.TextField(default='FROM "noreply" OR FROM "billing" OR SUBJECT "subscription" OR SUBJECT "payment"')
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)
    
    class Meta:
        unique_together = ['user', 'email']
    
    def __str__(self):
        return f"{self.user.username} - {self.email}"

class ParsedEmail(models.Model):
    mailbox = models.ForeignKey(Mailbox, on_delete=models.CASCADE, related_name='parsed_emails')
    message_id = models.CharField(max_length=500, unique=True)
    subject = models.CharField(max_length=500)
    from_email = models.EmailField()
    received_date = models.DateTimeField()
    service_name = models.CharField(max_length=200, blank=True)
    amount = models.DecimalField(max_digits=10, decimal_places=2, null=True, blank=True)
    currency = models.CharField(max_length=3, default='RUB')
    payment_date = models.DateField(null=True, blank=True)
    next_payment_date = models.DateField(null=True, blank=True)
    is_processed = models.BooleanField(default=False)
    processed_subscription = models.ForeignKey('subscriptions.Subscription', on_delete=models.SET_NULL, null=True, blank=True)
    error_message = models.TextField(blank=True)
    raw_content = models.TextField(blank=True)
    created_at = models.DateTimeField(auto_now_add=True)
    
    class Meta:
        ordering = ['-received_date']
        indexes = [
            models.Index(fields=['mailbox', 'is_processed']),
        ]
    
    def __str__(self):
        return f"{self.subject} - {self.received_date}"