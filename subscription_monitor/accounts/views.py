from django.shortcuts import render, redirect
from django.contrib.auth import login
from django.contrib.auth.decorators import login_required
from django.contrib import messages
from .forms import UserRegistrationForm, UserProfileForm
from rest_framework.decorators import api_view, permission_classes
from rest_framework.permissions import AllowAny
from rest_framework.response import Response
from rest_framework.authtoken.models import Token
from rest_framework.permissions import IsAuthenticated
from django.contrib.auth.models import User
from rest_framework import status
from .models import Profile

@api_view(['POST'])
@permission_classes([AllowAny])
def api_register(request):
    username = request.data.get('username')
    password = request.data.get('password')
    email = request.data.get('email', '')

    if not username or not password:
        return Response({'error': 'Логин и пароль обязательны'}, status=status.HTTP_400_BAD_REQUEST)

    if User.objects.filter(username=username).exists():
        return Response({'error': 'Пользователь уже существует'}, status=status.HTTP_400_BAD_REQUEST)

    user = User.objects.create_user(username=username, password=password, email=email)
    
    token, created = Token.objects.get_or_create(user=user)
    
    return Response({
        'token': token.key,
        'username': user.username,
        'user_id': user.id
    }, status=status.HTTP_201_CREATED)


@api_view(['GET', 'PUT'])
@permission_classes([IsAuthenticated])
def api_user_profile(request):
    user = request.user
    user_profile, _ = Profile.objects.get_or_create(user=user)

    if request.method == 'GET':
        return Response({
            'username': user.username,
            'first_name': user.first_name,
            'last_name': user.last_name,
            'email': user.email,
            'phone': user_profile.phone,
            'email_notifications': user_profile.email_notifications,
            'push_notifications': user_profile.push_notifications,
            'date_joined': user.date_joined,
        })

    if request.method == 'PUT':
        user.username = request.data.get('username', user.username)
        user.first_name = request.data.get('first_name', user.first_name)
        user.last_name = request.data.get('last_name', user.last_name)
        user.email = request.data.get('email', user.email)
        
        user.save(update_fields=['username', 'first_name', 'last_name', 'email'])

        user_profile.phone = request.data.get('phone', user_profile.phone)
        user_profile.email_notifications = request.data.get('email_notifications', user_profile.email_notifications)
        user_profile.push_notifications = request.data.get('push_notifications', user_profile.push_notifications)
        user_profile.save()

        return Response({'status': 'success'})

def register(request):
    if request.method == 'POST':
        form = UserRegistrationForm(request.POST)
        if form.is_valid():
            user = form.save()
            login(request, user)
            messages.success(request, 'Регистрация успешна!')
            return redirect('subscriptions:dashboard')
    else:
        form = UserRegistrationForm()
    return render(request, 'accounts/register.html', {'form': form})

@login_required
def profile(request):
    return render(request, 'accounts/profile.html', {'user': request.user})

@login_required
def edit_profile(request):
    if request.method == 'POST':
        form = UserProfileForm(request.POST, request.FILES, instance=request.user)
        if form.is_valid():
            form.save()
            messages.success(request, 'Профиль обновлен')
            return redirect('accounts:profile')
    else:
        form = UserProfileForm(instance=request.user)
    return render(request, 'accounts/edit_profile.html', {'form': form})