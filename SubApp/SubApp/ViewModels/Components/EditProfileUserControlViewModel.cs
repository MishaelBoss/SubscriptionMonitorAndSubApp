using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace SubApp.ViewModels.Components;

public partial class EditProfileUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private string _userName;
    [ObservableProperty] private string _lastName;
    [ObservableProperty] private string _fitstName;
    [ObservableProperty] private string _phone;
    [ObservableProperty] private string _email;
    [ObservableProperty] private string _password;
    [ObservableProperty] private string _confirmPassword;

    public bool IsActiveConfirmButton
        => !string.IsNullOrEmpty(UserName)
           && !string.IsNullOrEmpty(Password)
           && Password == ConfirmPassword;

    [RelayCommand]
    public async Task SaveAsync() { }
}
