from django.contrib import admin
from .models import Mailbox, ParsedEmail

@admin.register(Mailbox)
class MailboxAdmin(admin.ModelAdmin):
    list_display = ['email', 'user', 'provider', 'is_active', 'last_checked']
    list_filter = ['provider', 'is_active']
    search_fields = ['email', 'user__username']

@admin.register(ParsedEmail)
class ParsedEmailAdmin(admin.ModelAdmin):
    list_display = ['subject', 'from_email', 'service_name', 'amount', 'is_processed', 'received_date']
    list_filter = ['is_processed', 'received_date']
    search_fields = ['subject', 'from_email', 'service_name']