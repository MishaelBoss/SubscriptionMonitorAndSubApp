using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SubApp.ViewModels.Components;

public partial class RegistrationUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private string _registrationError = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _username = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _email = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _password = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _confirmPassword = string.Empty;

    public bool IsActiveConfirmButton
        => !string.IsNullOrEmpty(Username)
        && !string.IsNullOrEmpty(Password)
        && Password == ConfirmPassword;

    [RelayCommand]
    public async Task RegisterAsync()
    {
        /*if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            RegistrationError = "Введите логин и пароль!";

            if (Password != ConfirmPassword)
            {
                RegistrationError = "Пароли не совпадают!";
                return;
            }

            return;
        }*/
        
        Console.WriteLine($"Начало регистрации пользователя: {Username}");

        try
        {   
            await using var db = new AppDbContext();

            var exists = await db.Users.AnyAsync(u => u.Username == Username);
            if (exists) {
                Console.WriteLine($"Пользователь {Username} уже существует");
                return;
            }

            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var newUser = new User
                {
                    Username = Username,
                    Email = Email,
                    Password = AuthService.HashPasswordDjango(Password),
                    DateJoined = DateTime.UtcNow,
                    IsActive = true,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    IsStaff = false,
                    IsSuperuser = false
                };

                db.Users.Add(newUser);
                await db.SaveChangesAsync();
                Console.WriteLine($"User создан, ID: {newUser.Id}");
            
                var newProfile = new Profile
                {
                    UserId = newUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.Profiles.Add(newProfile);
                await db.SaveChangesAsync();

                await transaction.CommitAsync();
                Console.WriteLine($"Регистрация успешна для {Username}");

                await AuthService.LoginAsync(Username, Password);

                WeakReferenceMessenger.Default.Send(new OpenOrCloseRegistrationMessage());
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                Console.WriteLine(ex);
                throw;
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            RegistrationError = "Username уже занят!";
        }

        ClearForm();
    }

    [RelayCommand]
    public void OpenLogin() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseLoginMessage());
    }

    private void ClearForm() 
    {
        RegistrationError = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }
}
