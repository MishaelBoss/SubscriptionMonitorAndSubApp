using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;
using System.Threading.Tasks;

namespace SubApp.ViewModels.Components;

public partial class LoginUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _username = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _password = string.Empty;

    public bool IsActiveConfirmButton
        => !string.IsNullOrEmpty(Username)
        && !string.IsNullOrEmpty(Password);

    [RelayCommand]
    public async Task LoginAsync()
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Введите логин и пароль!";
            return;
        }

        bool success = await AuthService.LoginAsync(Username, Password);
        if (success)
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseLoginMessage());
            ClearForm();
        }
        else
        {
            ErrorMessage = "Неверное имя пользователя или пароль";
        }
    }

    [RelayCommand]
    public void OpenRegistration() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseRegistrationMessage());
    }

    private void ClearForm() 
    {
        ErrorMessage = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
    }
}
