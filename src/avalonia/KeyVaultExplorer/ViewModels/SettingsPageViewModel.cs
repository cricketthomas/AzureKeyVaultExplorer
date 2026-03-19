using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using KeyVaultExplorer.Database;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KeyVaultExplorer.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string version;

    private const string BackgroundTranparency = "BackgroundTransparency";
    private readonly AuthService _authService;
    private readonly KvExplorerDb _dbContext;
    private FluentAvaloniaTheme _faTheme;

    //private static Configuration ConfigFile => ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

    [ObservableProperty]
    private string[] appThemes = ["System", "Light", "Dark"];

    [ObservableProperty]
    private AuthenticatedUserClaims? authenticatedUserClaims;

    [ObservableProperty]
    private int clearClipboardTimeout;

    [ObservableProperty]
    private string currentAppTheme;

    [ObservableProperty]
    private bool isBackgroundTransparencyEnabled;

    [ObservableProperty]
    private ObservableCollection<Settings> settings;

    [ObservableProperty]
    private bool settingsPageClientIdCheckbox;

    [ObservableProperty]
    private string customClientId;

    public SettingsPageViewModel()
    {
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        _dbContext = Defaults.Locator.GetRequiredService<KvExplorerDb>();
        _faTheme = App.Current.Styles[0] as FluentAvaloniaTheme;
        Dispatcher.UIThread.Invoke(async () =>
        {
            Version = GetAppVersion();
            var jsonSettings = await GetAppSettings();
            //var s = await _dbContext.GetToggleSettings();
            ClearClipboardTimeout = jsonSettings.ClipboardTimeout;
            IsBackgroundTransparencyEnabled = jsonSettings.BackgroundTransparency;
            CurrentAppTheme = jsonSettings.AppTheme ?? "System";
            CustomClientId = jsonSettings.CustomClientId;
            SettingsPageClientIdCheckbox = jsonSettings.SettingsPageClientIdCheckbox;
            //NavigationLayoutMode = s.NavigationLayoutMode;
        }, DispatcherPriority.MaxValue);
    }

    public static string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version == null ? "(Unknown)" : $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    public async Task AddOrUpdateAppSettings<T>(string key, T value)
    {
        var path = Path.Combine(Constants.LocalAppDataFolder, "settings.json");
        var records = await GetAppSettings();
        // Assuming records is a class with a property that matches the key
        var property = records.GetType().GetProperty(key);
        if (property != null && property.PropertyType == typeof(T))
        {
            property.SetValue(records, value);
            var newJson = JsonSerializer.Serialize(records);

            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(fs);
            writer.WriteLine(newJson);

            //File.WriteAllText(path, newJson);
        }
    }

    public async Task<AppSettings> GetAppSettings()
    {
        var path = Path.Combine(Constants.LocalAppDataFolder, "settings.json");
        using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream);
    }

    [RelayCommand]
    private async Task SetBackgroundColorSetting()
    {
        await AddOrUpdateAppSettings(BackgroundTranparency, IsBackgroundTransparencyEnabled);
    }

    partial void OnCurrentAppThemeChanging(string? oldValue, string newValue)
    {
        if (oldValue is not null && oldValue != newValue)
            Dispatcher.UIThread.InvokeAsync(async () => await AddOrUpdateAppSettings(nameof(AppSettings.AppTheme), CurrentAppTheme), DispatcherPriority.Background);
    }



    partial void OnSettingsPageClientIdCheckboxChanged(bool value)
    {

        Dispatcher.UIThread.InvokeAsync(async () => 
            await AddOrUpdateAppSettings(nameof(AppSettings.SettingsPageClientIdCheckbox), SettingsPageClientIdCheckbox),
        DispatcherPriority.Background);
    }

    partial void OnCustomClientIdChanged(string value)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(50);
            await AddOrUpdateAppSettings(nameof(AppSettings.CustomClientId), CustomClientId);
        },
         DispatcherPriority.Background);
    }

    partial void OnClearClipboardTimeoutChanging(int oldValue, int newValue)
    {
        if (oldValue != 0 && oldValue != newValue)
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(50); // TOOD: figure out a way to get the value without having to wait for it to propagate.
                await AddOrUpdateAppSettings(nameof(AppSettings.ClipboardTimeout), ClearClipboardTimeout);
            }, DispatcherPriority.Background);
    }

    [RelayCommand]
    private async Task SetSplitViewDisplayMode(string splitViewDisplayMode)
    {
        await AddOrUpdateAppSettings(nameof(AppSettings.SplitViewDisplayMode), splitViewDisplayMode);
    }

    [RelayCommand]
    private async Task SignInOrRefreshTokenAsync()
    {
        var cancellation = new CancellationToken();
        var account = await _authService.RefreshTokenAsync(cancellation);

        if (account is null)
            account = await _authService.LoginAsync(cancellation);
        AuthenticatedUserClaims = _authService.AuthenticatedUserClaims;
    }

    [RelayCommand]
    private async Task SignOut()
    {
        await _authService.RemoveAccount();
        AuthenticatedUserClaims = _authService.AuthenticatedUserClaims;
    }

    [RelayCommand]
    private void OpenIssueGithub()
    {
        Process.Start(new ProcessStartInfo("https://github.com/cricketthomas/KeyVaultExplorer/issues/new") { UseShellExecute = true, Verb = "open" });
    }


    [RelayCommand]
    private Task DeleteDatabase()
    {
        _ = _dbContext.DropTablesAndRecreate();
        return Task.CompletedTask;
    }
 

}