from django.shortcuts import render, get_object_or_404, redirect
from django.contrib.auth.decorators import login_required
from rest_framework.permissions import IsAuthenticated
from .serializers import SubscriptionSerializer
from rest_framework import viewsets
from django.contrib import messages
from django.utils import timezone
from datetime import timedelta
from .models import Subscription, Category, Payment, Notification, Service
from .forms import SubscriptionForm, PaymentForm
from .serializers import SubscriptionSerializer, ServiceSerializer


class SubscriptionViewSet(viewsets.ModelViewSet):
    permission_classes = [IsAuthenticated]
    serializer_class = SubscriptionSerializer

    def get_queryset(self):
        return Subscription.objects.filter(user=self.request.user)

    def perform_create(self, serializer):
        serializer.save(user=self.request.user)

class ServiceViewSet(viewsets.ReadOnlyModelViewSet):
    queryset = Service.objects.filter(is_active=True)
    serializer_class = ServiceSerializer
    permission_classes = [IsAuthenticated]

@login_required
def dashboard(request):
    """Главная страница с дашбордом"""
    subscriptions = Subscription.objects.filter(user=request.user, is_active=True)
    
    total_monthly = 0
    active_count = 0
    expiring_soon = 0
    overdue_count = 0
    
    for sub in subscriptions:
        if sub.status == 'active':
            total_monthly += sub.calculate_monthly_cost()
            active_count += 1
            if sub.is_expiring_soon(7):
                expiring_soon += 1
            if sub.is_overdue:
                overdue_count += 1
    
    recent_payments = Payment.objects.filter(
        subscription__user=request.user
    ).select_related('subscription__service').order_by('-payment_date')[:10]
    
    unread_notifications = Notification.objects.filter(
        user=request.user, 
        is_read=False
    ).count()
    
    context = {
        'subscriptions': subscriptions,
        'total_monthly': total_monthly,
        'total_yearly': total_monthly * 12,
        'active_count': active_count,
        'expiring_soon': expiring_soon,
        'overdue_count': overdue_count,
        'recent_payments': recent_payments,
        'unread_notifications': unread_notifications,
    }
    return render(request, 'subscriptions/dashboard.html', context)

@login_required
def subscription_list(request):
    """Список всех подписок"""
    subscriptions = Subscription.objects.filter(
        user=request.user
    ).select_related('service', 'service__category').order_by('next_payment_date')
    
    # Фильтр по статусу
    status = request.GET.get('status')
    if status == 'overdue':
        subscriptions = [s for s in subscriptions if s.is_overdue]
    elif status:
        subscriptions = subscriptions.filter(status=status)
    
    context = {
        'subscriptions': subscriptions,
    }
    return render(request, 'subscriptions/list.html', context)

@login_required
def subscription_detail(request, pk):
    """Детали подписки"""
    subscription = get_object_or_404(Subscription, pk=pk, user=request.user)
    payments = subscription.payments.all().order_by('-payment_date')[:12]
    days_until = subscription.days_until_next_payment()
    
    context = {
        'subscription': subscription,
        'payments': payments,
        'days_until': days_until,
        'is_overdue': subscription.is_overdue,
        'overdue_days': subscription.overdue_days,
    }
    return render(request, 'subscriptions/detail.html', context)

@login_required
def mark_subscription_paid(request, pk):
    """Отметить подписку как оплаченную"""
    subscription = get_object_or_404(Subscription, pk=pk, user=request.user)
    
    if request.method == 'POST':
        # Обновляем дату следующего платежа
        new_date = subscription.mark_as_paid()
        
        # Создаем уведомление
        Notification.objects.create(
            user=request.user,
            subscription=subscription,
            notification_type='upcoming',
            title='Подписка оплачена',
            message=f'Подписка {subscription.service.name} отмечена как оплаченная. Следующий платеж: {new_date}'
        )
        
        messages.success(request, f'Подписка {subscription.service.name} отмечена как оплаченная')
        return redirect('subscriptions:detail', pk=subscription.pk)
    
    return render(request, 'subscriptions/mark_paid_confirm.html', {'subscription': subscription})

@login_required
def subscription_create(request):
    """Создание новой подписки"""
    if request.method == 'POST':
        form = SubscriptionForm(request.POST)
        if form.is_valid():
            subscription = form.save(commit=False)
            subscription.user = request.user
            subscription.save()
            messages.success(request, 'Подписка успешно добавлена')
            return redirect('subscriptions:detail', pk=subscription.pk)
    else:
        form = SubscriptionForm()
    
    services = Service.objects.filter(is_active=True)
    
    context = {
        'form': form,
        'title': 'Новая подписка',
        'services': services,
    }
    return render(request, 'subscriptions/form.html', context)

@login_required
def subscription_edit(request, pk):
    """Редактирование подписки"""
    subscription = get_object_or_404(Subscription, pk=pk, user=request.user)
    
    if request.method == 'POST':
        form = SubscriptionForm(request.POST, instance=subscription)
        if form.is_valid():
            form.save()
            messages.success(request, 'Подписка обновлена')
            return redirect('subscriptions:detail', pk=subscription.pk)
    else:
        form = SubscriptionForm(instance=subscription)
    
    services = Service.objects.filter(is_active=True)
    
    context = {
        'form': form,
        'title': 'Редактировать подписку',
        'services': services,
        'subscription': subscription,
    }
    return render(request, 'subscriptions/form.html', context)

@login_required
def subscription_delete(request, pk):
    """Удаление подписки"""
    subscription = get_object_or_404(Subscription, pk=pk, user=request.user)
    
    if request.method == 'POST':
        subscription.delete()
        messages.success(request, 'Подписка удалена')
        return redirect('subscriptions:list')
    
    context = {
        'subscription': subscription,
    }
    return render(request, 'subscriptions/confirm_delete.html', context)

@login_required
def analytics(request):
    """Аналитика по подпискам"""
    subscriptions = Subscription.objects.filter(user=request.user, is_active=True)
    
    category_data = []
    total_monthly = 0
    active_count = 0
    overdue_count = 0
    
    categories = Category.objects.all()
    
    for category in categories:
        category_total = 0
        category_count = 0
        
        for sub in subscriptions:
            if sub.status == 'active' and sub.service and sub.service.category == category:
                monthly = sub.calculate_monthly_cost()
                category_total += monthly
                category_count += 1
                if sub.is_overdue:
                    overdue_count += 1
        
        if category_count > 0:
            category_data.append({
                'name': category.name,
                'total': category_total,
                'count': category_count,
                'icon': category.icon,
                'color': category.color,
            })
            total_monthly += category_total
    
    for sub in subscriptions:
        if sub.status == 'active':
            active_count += 1
    
    context = {
        'subscriptions': subscriptions,
        'category_data': category_data,
        'total_monthly': total_monthly,
        'total_yearly': total_monthly * 12,
        'active_count': active_count,
        'overdue_count': overdue_count,
    }
    return render(request, 'subscriptions/analytics.html', context)

@login_required
def notifications(request):
    """Список уведомлений"""
    notifications = Notification.objects.filter(user=request.user).order_by('-sent_at')
    unread_count = notifications.filter(is_read=False).count()
    
    if request.method == 'POST':
        if 'mark_all_read' in request.POST:
            notifications.filter(is_read=False).update(is_read=True)
            messages.success(request, 'Все уведомления отмечены как прочитанные')
            return redirect('subscriptions:notifications')
    
    context = {
        'notifications': notifications,
        'unread_count': unread_count,
    }
    return render(request, 'subscriptions/notifications.html', context)

@login_required
def cancel_subscription(request, pk):
    """Отмена подписки"""
    subscription = get_object_or_404(Subscription, pk=pk, user=request.user)
    
    if request.method == 'POST':
        subscription.status = 'cancelled'
        subscription.is_active = False
        subscription.save()
        
        Notification.objects.create(
            user=request.user,
            subscription=subscription,
            notification_type='cancelled',
            title='Подписка отменена',
            message=f'Подписка {subscription.service.name} успешно отменена'
        )
        
        messages.success(request, 'Подписка отменена')
        
        if subscription.service and subscription.service.website:
            return redirect(subscription.service.website)
        
        return redirect('subscriptions:detail', pk=subscription.pk)
    
    context = {
        'subscription': subscription,
    }
    return render(request, 'subscriptions/cancel_confirm.html', context)