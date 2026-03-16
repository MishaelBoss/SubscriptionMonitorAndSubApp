from django.urls import path, include
from rest_framework.routers import DefaultRouter
from . import views

router = DefaultRouter()
router.register(r'mailboxes', views.MailboxViewSet, basename='mailbox-api')
router.register(r'emails', views.ParsedEmailViewSet, basename='emails-api')

app_name = 'mail_parser'

urlpatterns = [
    path('', views.mailbox_list, name='list'),
    path('add/', views.mailbox_add, name='add'),
    path('<int:pk>/edit/', views.mailbox_edit, name='edit'),
    path('<int:pk>/delete/', views.mailbox_delete, name='delete'),
    path('<int:pk>/check/', views.mailbox_check_recent, name='check'),
    path('<int:pk>/parse/', views.mailbox_parse, name='parse'),
    path('<int:pk>/progress/', views.get_progress, name='progress'),
    path('emails/', views.parsed_emails, name='emails'),
    path('clear-session/', views.clear_session_data, name='clear_session'),

    path('api/', include(router.urls)),
    path('add/', views.mailbox_add, name='add'),
    path('<int:pk>/edit/', views.mailbox_edit, name='edit'),
    path('<int:pk>/delete/', views.mailbox_delete, name='delete'),
]