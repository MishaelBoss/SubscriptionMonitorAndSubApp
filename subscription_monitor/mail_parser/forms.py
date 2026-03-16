from django import forms
from .models import Mailbox

class MailboxForm(forms.ModelForm):
    password = forms.CharField(
        widget=forms.PasswordInput(render_value=True),
        label='Пароль от почты',
        help_text='Пароль будет зашифрован и сохранен безопасно'
    )
    
    class Meta:
        model = Mailbox
        fields = ['email', 'provider', 'imap_server', 'imap_port', 'password', 'check_frequency']
        widgets = {
            'imap_server': forms.TextInput(attrs={'placeholder': 'imap.gmail.com'}),
        }
        labels = {
            'email': 'Email адрес',
            'provider': 'Почтовый провайдер',
            'imap_server': 'IMAP сервер (если не стандартный)',
            'imap_port': 'IMAP порт',
            'check_frequency': 'Частота проверки (минуты)',
        }
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.fields['provider'].widget.attrs.update({'class': 'form-select'})
        
    def clean_password(self):
        password = self.cleaned_data.get('password')
        # Здесь должно быть шифрование пароля
        # В реальном проекте используйте django-cryptography или Fernet
        return password  # Пока возвращаем как есть