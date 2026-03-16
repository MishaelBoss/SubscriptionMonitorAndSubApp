from django.urls import path, include
from rest_framework.routers import DefaultRouter
from . import views

router = DefaultRouter()
router.register(r'subscriptions', views.SubscriptionViewSet, basename='subscription-api')
router.register(r'services', views.ServiceViewSet, basename='service-api')

app_name = 'subscriptions'

urlpatterns = [
    path('', views.dashboard, name='dashboard'),
    path('list/', views.subscription_list, name='list'),
    path('create/', views.subscription_create, name='create'),
    path('<int:pk>/', views.subscription_detail, name='detail'),
    path('<int:pk>/edit/', views.subscription_edit, name='edit'),
    path('<int:pk>/delete/', views.subscription_delete, name='delete'),
    path('<int:pk>/cancel/', views.cancel_subscription, name='cancel'),
    path('<int:pk>/mark-paid/', views.mark_subscription_paid, name='mark_paid'),
    path('analytics/', views.analytics, name='analytics'),
    path('notifications/', views.notifications, name='notifications'),

    path('api/', include(router.urls)),
]