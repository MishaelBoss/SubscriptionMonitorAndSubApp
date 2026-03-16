from rest_framework import serializers
from .models import Subscription, Service

class ServiceSerializer(serializers.ModelSerializer):
    class Meta:
        model = Service
        fields = '__all__'

class SubscriptionSerializer(serializers.ModelSerializer):
    service_name = serializers.ReadOnlyField(source='service.name')

    class Meta:
        model = Subscription
        fields = '__all__'
        read_only_fields = ['user']