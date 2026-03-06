using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SubApp.Data;
using SubApp.Scripts;
using SubApp.ViewModels;
using SubApp.ViewModels.Components;
using SubApp.Views;
using System;
using System.IO;
using System.Linq;
using Xamarin.Essentials;

namespace SubApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>();

        services.AddTransient<LoginUserControlViewModel>();

        var logFile = Path.Combine(FileSystem.AppDataDirectory, "logs.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        Log.Information("=== ЗАПУСК ПРИЛОЖЕНИЯ. ЛОГ: {Path} ===", logFile);

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


        if (!await AuthService.TryAutoLoginAsync())
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseLoginMessage());
        }

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