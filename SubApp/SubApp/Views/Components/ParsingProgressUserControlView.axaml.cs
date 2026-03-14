using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;
using SubApp.ViewModels.Components;

namespace SubApp.Views.Components;

public partial class ParsingProgressUserControlView : UserControl
{
    private bool _isDragging;
    private Point _startPointerPosition;

    private TranslateTransform? GetTransform() => Sheet.RenderTransform as TranslateTransform;
    
    public ParsingProgressUserControlView()
    {
        InitializeComponent();
        
        DataContextChanged += (s, e) =>
        {
            if (DataContext is ParsingProgressUserControlViewModel vm)
            {
                vm.Logs.CollectionChanged += Logs_CollectionChanged;
            }
        };
    }
    
    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var scroll = this.FindControl<ScrollViewer>("LogScroll");
            scroll?.ScrollToEnd();
        });
    }
    
    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        _isDragging = true;
        _startPointerPosition = e.GetPosition(this);
        e.Pointer.Capture(Sheet);
    }
    
    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        var currentPosition = e.GetPosition(this);
        var deltaY = currentPosition.Y - _startPointerPosition.Y;

        if (!(deltaY > 0)) return;
        var transform = GetTransform();
        if (transform == null) return;
        transform.Y = deltaY;
        BackgroundDim.Opacity = Math.Max(0, 0.5 - (deltaY / 600));
    }
    
    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        e.Pointer.Capture(null);
        
        var transform = GetTransform();
        if (transform == null) return;

        if (transform.Y > 100)
        {
            Close();
        }

        transform.Y = 0;
        BackgroundDim.Opacity = 0.5;
    }

    private static void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseProgressModalMessage());
    }

    private void OnCloseClicked(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is ParsingProgressUserControlViewModel vm && vm.IsFinished)
        {
            vm.CloseCommand.Execute(null);
        }
        
        Close();
    }
}