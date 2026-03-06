using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace SubApp.ViewModels.Components
{
    public partial class SegmentedButtonViewModel : ViewModelBase
    {
        [ObservableProperty] private string _content;
        [ObservableProperty] private bool _isSelected;

        public ICommand Command { get; }
        public Bitmap IconPath { get; }

        public SegmentedButtonViewModel(string content, ICommand command, Bitmap iconPath, bool isSelected = false)
        {
            Content = content;
            Command = command;
            IconPath = iconPath;
            IsSelected = isSelected;
        }
    }
}
