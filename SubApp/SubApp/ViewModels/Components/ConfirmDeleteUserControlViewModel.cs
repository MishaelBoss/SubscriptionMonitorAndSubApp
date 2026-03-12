using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class ConfirmDeleteUserControlViewModel(Func<Task>? deleteAction) : ViewModelBase
{
    [RelayCommand]
    public async void Delete()
    {
        if(deleteAction == null) return;
        try 
        {
            await deleteAction();
            Close();
        }
        catch (Exception ex) 
        {
            Debug.WriteLine($"Ошибка удаления: {ex.Message}");
        }
    }
    
    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmDelete());
    }
}