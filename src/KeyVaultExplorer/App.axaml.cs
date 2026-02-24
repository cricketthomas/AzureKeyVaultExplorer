using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using KeyVaultExplorer.Database;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.Services;
using KeyVaultExplorer.ViewModels;
using KeyVaultExplorer.Views;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace KeyVaultExplorer;

public partial class App : Application
{
    public App()
    {
        DataContext = new AppViewModel();
    }

    public static void CreateDesktopResources()
    {
        Directory.CreateDirectory(Constants.LocalAppDataFolder);
        var dbExists = File.Exists(Constants.DatabaseFilePath);

        var dbPassExists = File.Exists(Path.Combine(Constants.LocalAppDataFolder, Constants.EncryptedSecretFileName));
        if (!dbPassExists)
            DatabaseEncryptedPasswordManager.SetSecret($"keyvaultexplorer_{System.Guid.NewGuid().ToString()[..6]}");

        Dispatcher.UIThread.Post(async () =>
        {
            if (!dbExists)
                KvExplorerDb.InitializeDatabase();
        }, DispatcherPriority.Loaded);

        string settingsPath = Path.Combine(Constants.LocalAppDataFolder, "settings.json");
        if (!File.Exists(settingsPath))
        {
            File.WriteAllText(settingsPath, """
                {
                    "BackgroundTransparency": false,
                    "NavigationLayoutMode": "Left",
                    "AppTheme": "System",
                    "PaneDisplayMode": "inline"
                }
                """);
        }
    }


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        // BindingPlugins.DataValidators.RemoveAt(0);

        var collection = new ServiceCollection();
        collection.AddCommonServices();
        var services = collection.BuildServiceProvider();
        Defaults.Locator.ConfigureServices(services);
        var vm = services.GetRequiredService<MainViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = vm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

public static class ApplicationExtensions
{
    /// <summary>
    /// Returns the TopLevel from the main window or view.
    /// </summary>
    /// <param name="app">The application to get the TopLevel for.</param>
    /// <returns>A TopLevel object.</returns>
    public static TopLevel? GetTopLevel(this Application? app)
    {
        if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        if (app?.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
        {
            var visualRoot = viewApp.MainView?.GetVisualRoot();
            return visualRoot as TopLevel;
        }
        return null;
    }
}