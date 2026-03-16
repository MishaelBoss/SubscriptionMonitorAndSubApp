from django.db import models
from django.contrib.auth.models import User
from django.utils import timezone
from datetime import timedelta
import uuid

class Category(models.Model):
    """Категория подписок"""
    name = models.CharField(max_length=100, unique=True)
    icon = models.CharField(max_length=50, default="📦")
    color = models.CharField(max_length=20, default="#6c757d")
    created_at = models.DateTimeField(auto_now_add=True)
    
    class Meta:
        verbose_name_plural = "Categories"
        ordering = ['name']
    
    def __str__(self):
        return self.name

class Service(models.Model):
    """Сервис/платформа подписки"""
    name = models.CharField(max_length=200)
    logo = models.ImageField(upload_to='service_logos/', null=True, blank=True)
    website = models.URLField(max_length=500, blank=True)
    category = models.ForeignKey(Category, on_delete=models.SET_NULL, null=True, related_name='services')
    is_active = models.BooleanField(default=True)
    created_at = models.DateTimeField(auto_now_add=True)
    
    class Meta:
        ordering = ['name']
    
    def __str__(self):
        return self.name

class Subscription(models.Model):
    """Подписка пользователя"""
    BILLING_CYCLE_CHOICES = [
        ('monthly', 'Ежемесячно'),
        ('quarterly', 'Ежеквартально'),
        ('yearly', 'Ежегодно'),
        ('weekly', 'Еженедельно'),
        ('custom', 'Свой период'),
    ]
    
    CURRENCY_CHOICES = [
        ('RUB', '₽'),
        ('USD', '$'),
        ('EUR', '€'),
    ]
    
    STATUS_CHOICES = [
        ('active', 'Активна'),
        ('paused', 'Приостановлена'),
        ('cancelled', 'Отменена'),
        ('expired', 'Истекла'),
        ('trial', 'Пробный период'),
    ]
    
    user = models.ForeignKey(User, on_delete=models.CASCADE, related_name='subscriptions')
    service = models.ForeignKey(Service, on_delete=models.CASCADE, related_name='subscriptions')
    name = models.CharField(max_length=200)
    amount = models.DecimalField(max_digits=10, decimal_places=2)
    currency = models.CharField(max_length=3, choices=CURRENCY_CHOICES, default='RUB')
    billing_cycle = models.CharField(max_length=20, choices=BILLING_CYCLE_CHOICES, default='monthly')
    billing_cycle_days = models.IntegerField(default=30)
    start_date = models.DateField()
    next_payment_date = models.DateField()
    end_date = models.DateField(null=True, blank=True)
    last_checked = models.DateTimeField(auto_now=True)
    status = models.CharField(max_length=20, choices=STATUS_CHOICES, default='active')
    auto_renew = models.BooleanField(default=True)
    is_active = models.BooleanField(default=True)
    notes = models.TextField(blank=True)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)
    uuid = models.UUIDField(default=uuid.uuid4, editable=False, unique=True)
    
    class Meta:
        ordering = ['next_payment_date']
        indexes = [
            models.Index(fields=['user', 'status']),
            models.Index(fields=['next_payment_date']),
        ]
    
    def __str__(self):
        overdue = " (ПРОСРОЧЕНА)" if self.is_overdue else ""
        return f"{self.user.username} - {self.service.name}{overdue}"
    
    def days_until_next_payment(self):
        """Дней до следующего платежа (отрицательное значение = просрочка)"""
        if self.next_payment_date:
            delta = self.next_payment_date - timezone.now().date()
            return delta.days
        return None
    
    @property
    def is_overdue(self):
        """Проверка, просрочена ли подписка"""
        if self.status != 'active':
            return False
        if self.next_payment_date:
            return self.next_payment_date < timezone.now().date()
        return False
    
    @property
    def overdue_days(self):
        """Количество дней просрочки"""
        if self.is_overdue:
            delta = timezone.now().date() - self.next_payment_date
            return delta.days
        return 0
    
    @property
    def status_display(self):
        """Отображение статуса с учетом просрочки"""
        if self.is_overdue:
            return "ПРОСРОЧЕНА"
        return self.get_status_display()
    
    @property
    def status_color(self):
        """Цвет статуса для UI"""
        if self.is_overdue:
            return "danger"
        status_colors = {
            'active': 'success',
            'paused': 'warning',
            'cancelled': 'danger',
            'expired': 'secondary',
            'trial': 'info',
        }
        return status_colors.get(self.status, 'secondary')
    
    def is_expiring_soon(self, days=7):
        """Проверка, истекает ли подписка скоро"""
        if self.next_payment_date and self.status == 'active' and not self.is_overdue:
            delta = self.next_payment_date - timezone.now().date()
            return 0 <= delta.days <= days
        return False
    
    def calculate_monthly_cost(self):
        """Расчет ежемесячной стоимости"""
        if self.billing_cycle == 'monthly':
            return float(self.amount)
        elif self.billing_cycle == 'yearly':
            return float(self.amount) / 12
        elif self.billing_cycle == 'quarterly':
            return float(self.amount) / 3
        elif self.billing_cycle == 'weekly':
            return float(self.amount) * 4.33
        elif self.billing_cycle == 'custom' and self.billing_cycle_days:
            return (float(self.amount) / self.billing_cycle_days) * 30
        return float(self.amount)
    
    def mark_as_paid(self, payment_date=None):
        """Отметить подписку как оплаченную"""
        if not payment_date:
            payment_date = timezone.now().date()
        
        if self.billing_cycle == 'monthly':
            self.next_payment_date = payment_date + timedelta(days=30)
        elif self.billing_cycle == 'yearly':
            self.next_payment_date = payment_date + timedelta(days=365)
        elif self.billing_cycle == 'quarterly':
            self.next_payment_date = payment_date + timedelta(days=90)
        elif self.billing_cycle == 'weekly':
            self.next_payment_date = payment_date + timedelta(days=7)
        elif self.billing_cycle == 'custom':
            self.next_payment_date = payment_date + timedelta(days=self.billing_cycle_days)
        
        self.save()
        return self.next_payment_date

class Payment(models.Model):
    """Платеж по подписке"""
    subscription = models.ForeignKey(Subscription, on_delete=models.CASCADE, related_name='payments')
    amount = models.DecimalField(max_digits=10, decimal_places=2)
    currency = models.CharField(max_length=3, default='RUB')
    payment_date = models.DateField()
    description = models.CharField(max_length=500, blank=True)
    
    SOURCE_CHOICES = [
        ('manual', 'Ручной ввод'),
        ('email', 'Парсинг почты'),
        ('bank', 'Интеграция с банком'),
    ]
    source = models.CharField(max_length=20, choices=SOURCE_CHOICES, default='manual')
    source_id = models.CharField(max_length=500, blank=True)
    created_at = models.DateTimeField(auto_now_add=True)
    
    class Meta:
        ordering = ['-payment_date']
    
    def __str__(self):
        return f"{self.subscription} - {self.amount} {self.currency} ({self.payment_date})"

class UsageData(models.Model):
    """Данные об использовании подписки"""
    subscription = models.ForeignKey(Subscription, on_delete=models.CASCADE, related_name='usage_data')
    date = models.DateField(auto_now_add=True)
    last_used = models.DateTimeField(null=True, blank=True)
    usage_count = models.IntegerField(default=0)
    notes = models.TextField(blank=True)
    
    class Meta:
        verbose_name_plural = "Usage data"
        ordering = ['-date']
    
    def __str__(self):
        return f"{self.subscription} - {self.date}"

class Notification(models.Model):
    """Уведомление для пользователя"""
    NOTIFICATION_TYPES = [
        ('upcoming', 'Предстоящий платеж'),
        ('overdue', 'Просрочка платежа'),
        ('price_increase', 'Повышение цены'),
        ('inactive', 'Неактивность'),
        ('expired', 'Истекла'),
        ('cancelled', 'Отменена'),
        ('recommendation', 'Рекомендация'),
    ]
    
    user = models.ForeignKey(User, on_delete=models.CASCADE, related_name='notifications')
    subscription = models.ForeignKey(Subscription, on_delete=models.CASCADE, null=True, blank=True, related_name='notifications')
    notification_type = models.CharField(max_length=20, choices=NOTIFICATION_TYPES)
    title = models.CharField(max_length=200)
    message = models.TextField()
    is_read = models.BooleanField(default=False)
    sent_at = models.DateTimeField(auto_now_add=True)
    
    class Meta:
        ordering = ['-sent_at']
    
    def __str__(self):
        return f"{self.user.username} - {self.title}"