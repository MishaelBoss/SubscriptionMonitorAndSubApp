using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;

namespace SubApp.ViewModels.Pages
{
    public partial class EmailsUserControlViewModel : ViewModelBase
    {
        [RelayCommand]
        public void OpenAddEmail()
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditEmailMessage());
        }
        
        [RelayCommand]
        public void Run()
        {
            WeakReferenceMessenger.Default.Send(this);
        }
        
        [RelayCommand]
        public void FustRun()
        {
            WeakReferenceMessenger.Default.Send(this);
        }
        
        [RelayCommand]
        public void OpenEditEmail()
        {
            WeakReferenceMessenger.Default.Send(this);
        }
        
        [RelayCommand]
        public void OpenDeleteEmail()
        {
            WeakReferenceMessenger.Default.Send(this);
        }
    }
}
