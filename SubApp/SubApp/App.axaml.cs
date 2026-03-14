using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SubApp.Data;
using SubApp.Scripts;
using SubApp.ViewModels;
using SubApp.ViewModels.Components;
using SubApp.Views;
using System.Linq;
using System.Threading.Tasks;

namespace SubApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>();

        services.AddTransient<LoginUserControlViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }
        
        Task.Run(async () => 
        {
            if (!await AuthService.TryAutoLoginAsync())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => 
                    WeakReferenceMessenger.Default.Send(new OpenOrCloseLoginMessage()));
            }
            
            var mailWorker = new Services.MailBackgroundWorker();
            mailWorker.Start();
        });
        
        var culture = new System.Globalization.CultureInfo("ru-RU")
        {
            DateTimeFormat =
            {
                ShortDatePattern = "dd.MM.yyyy"
            },
            NumberFormat =
            {
                NumberDecimalSeparator = "."
            }
        };

        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
        
        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}