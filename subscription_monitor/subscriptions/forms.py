from django import forms
from .models import Subscription, Payment

class SubscriptionForm(forms.ModelForm):
    class Meta:
        model = Subscription
        fields = [
            'service', 'name', 'amount', 'currency', 'billing_cycle',
            'billing_cycle_days', 'start_date', 'next_payment_date',
            'status', 'auto_renew', 'notes'
        ]
        widgets = {
            'start_date': forms.DateInput(attrs={'type': 'date'}),
            'next_payment_date': forms.DateInput(attrs={'type': 'date'}),
            'notes': forms.Textarea(attrs={'rows': 3}),
        }

class PaymentForm(forms.ModelForm):
    class Meta:
        model = Payment
        fields = ['amount', 'currency', 'payment_date', 'description', 'source']
        widgets = {
            'payment_date': forms.DateInput(attrs={'type': 'date'}),
            'description': forms.Textarea(attrs={'rows': 2}),
        }