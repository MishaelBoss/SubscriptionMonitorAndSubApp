from rest_framework import serializers
from .models import ParsedEmail 

class ParsedEmailSerializer(serializers.ModelSerializer):
    user_id = serializers.ReadOnlyField(source='mailbox.user.id')
    mailbox_email = serializers.ReadOnlyField(source='mailbox.email')

    class Meta:
        model = ParsedEmail
        fields = '__all__'