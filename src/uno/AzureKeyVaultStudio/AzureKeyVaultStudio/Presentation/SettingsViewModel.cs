using System.Globalization;
using System.Reflection;
using AzureKeyVaultStudio.Database;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.Identity.Client;

namespace AzureKeyVaultStudio.Presentation;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IAuthenticationService _authentication;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IStringLocalizer _localizer;
    private readonly ILocalSettingsService _localSettings;
    private readonly IDispatcher _dispatcher;
    private readonly INavigator _navigator;
    private readonly AuthService _authService;
    private readonly IOptions<ProjectUrls> _appConfig;
    public string AppVersion { get; }

    [ObservableProperty]
    public partial string CustomClientId { get; set; }

    [ObservableProperty]
    public partial string CustomTenantId { get; set; }

    [ObservableProperty]
    public partial CultureInfo Language { get; set; } = CultureInfo.CurrentCulture;

    [ObservableProperty]
    public partial IReadOnlyList<CultureInfo>? AvailableLanguages { get; set; } = [];

    [ObservableProperty]
    public partial AppTheme SelectedTheme { get; set; }

    [ObservableProperty]
    public partial int SelectedCloudEnvironmentIndex { get; set; }

    [ObservableProperty]
    public partial bool SettingsPageClientIdCheckbox { get; set; }

    [ObservableProperty]
    public partial bool SettingsPageTenantIdCheckbox { get; set; }

    [ObservableProperty]
    public partial int ClearClipboardTimeout { get; set; }

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; } = false;

    [ObservableProperty]
    public partial AuthenticatedUserClaims? Claims { get; set; }
    public string? GitHubRepoistoryBaseUrl => _appConfig.Value.GitHubRepoistoryBaseUrl;
    public string? LicenseUrl => _appConfig.Value.LicenseUrl;
    public string? NewIssueUrl => _appConfig.Value.NewIssueUrl;
    public string? ReleasesPageUrl => _appConfig.Value.ReleasesPageUrl;

    public AppTheme DarkTheme => AppTheme.Dark;
    public AppTheme LightTheme => AppTheme.Light;
    public AppTheme SystemTheme => AppTheme.System;

    public AzureCloudInstance[] AzureCloudInstances { get; } = Enum.GetValues<AzureCloudInstance>();

    public SettingsViewModel(IDispatcher dispatcher, IAuthenticationService authentication,
        ILocalSettingsService localSettings, AuthService authService, IThemeService themeService, ILocalizationService localizationService, INavigator navigator, IOptions<ProjectUrls> appConfig, IStringLocalizer localizer)
    {
        _dispatcher = dispatcher;
        _authentication = authentication;
        _localSettings = localSettings;
        _authService = authService;
        _themeService = themeService;
        _localizationService = localizationService;
        _navigator = navigator;
        _appConfig = appConfig;
        _localizer = localizer;
        LoadSettings();
        SelectedTheme = _themeService.Theme;
        AppVersion = GetAppVersion();

        AvailableLanguages = _localizationService.SupportedCultures;

        var current = _localizationService.CurrentCulture ?? CultureInfo.CurrentCulture;
        Language =
            AvailableLanguages.FirstOrDefault(c => c.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase))
            ?? AvailableLanguages.FirstOrDefault(c => c.TwoLetterISOLanguageName.Equals(current.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
            ?? AvailableLanguages.FirstOrDefault()
            ?? current;

        WeakReferenceMessenger.Default.Register<SettingsViewModel, AuthenticationStateChangedMessage>(this, (r, m) =>
        {
            r.Claims = m.Value;
        });
        WeakReferenceMessenger.Default.Register<SettingsViewModel, AuthenticationRemovedStateChangedMessage>(this, (r, m) =>
        {
            Claims = new();
            IsAuthenticated = false;
        });
    }

    protected override void OnDeactivated()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnDeactivated();
    }

    private void Save<T>(string key, T value)
    {
        _localSettings.SetValue(key, value);
    }

    private T GetSettingsValue<T>(string key, T defaultValue) where T : struct, Enum
    {
        var stringValue = _localSettings.GetValue(key, defaultValue.ToString());
        return Enum.TryParse<T>(stringValue, out var result) ? result : defaultValue;
    }

    [RelayCommand]
    public async Task ToggleThemeChangeAsync(AppTheme theme)
    {
        await _themeService.SetThemeAsync(theme);
        //SelectedTheme = theme;
    }

    private void LoadSettings()
    {
        //_dispatcher.TryEnqueue(() =>
        Task.Run(() =>
        {
            CustomClientId = _localSettings.GetValue<string>(nameof(CustomClientId), string.Empty);
            CustomTenantId = _localSettings.GetValue<string>(nameof(CustomTenantId), string.Empty);
            SettingsPageClientIdCheckbox = _localSettings.GetValue(nameof(SettingsPageClientIdCheckbox), false);
            SettingsPageTenantIdCheckbox = _localSettings.GetValue(nameof(SettingsPageTenantIdCheckbox), false);
            ClearClipboardTimeout = _localSettings.GetValue(nameof(ClearClipboardTimeout), 10);
            var savedCloud = (AzureCloudInstance)_localSettings.GetValue<int>(Constants.SelectedCloudEnvironmentName, (int)AzureCloudInstance.None);
            SelectedCloudEnvironmentIndex = Array.IndexOf(AzureCloudInstances, savedCloud) is var idx and >= 0 ? idx : 0;
            Claims = _authService?.AuthenticatedUserClaims;
            //if (_themeService is not null) SelectedTheme = _themeService.Theme;
        });
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        var selectedCloud = SelectedCloudEnvironmentIndex >= 0 && SelectedCloudEnvironmentIndex < AzureCloudInstances.Length
            ? AzureCloudInstances[SelectedCloudEnvironmentIndex]
            : AzureCloudInstance.None;
        Save(Constants.SelectedCloudEnvironmentName, (int)selectedCloud);
        Save(nameof(SettingsPageClientIdCheckbox), SettingsPageClientIdCheckbox);
        Save(nameof(SettingsPageTenantIdCheckbox), SettingsPageTenantIdCheckbox);
        Save(nameof(ClearClipboardTimeout), ClearClipboardTimeout < 0 ? 10 : ClearClipboardTimeout);
        Save(nameof(CustomClientId), CustomClientId?.Trim() ?? string.Empty);
        Save(nameof(CustomTenantId), CustomTenantId?.Trim() ?? string.Empty);
        await _localizationService.SetCurrentCultureAsync(Language);

        WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
        {
            Severity = InfoBarSeverity.Informational,
            Title = _localizer["SavedTitle"] ?? "Saved.",
            Message = _localizer["SavedChangesMessage"] ?? "Your changes have been saved.",
            Duration = TimeSpan.FromSeconds(5)
        }));

    }


    [RelayCommand]
    private async Task SignInOrRefreshTokenAsync()
    {
        var cancellation = new CancellationToken();
        var account = await _authService.RefreshTokenAsync(cancellation);

        if (account is null)
            account = await _authService.LoginAsync(cancellation);
        Claims = _authService.AuthenticatedUserClaims;
    }

    [RelayCommand]
    private async Task SignOut()
    {
        await _authentication.LogoutAsync();
        await _authService.SignOutAsync();
        WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
        {
            Severity = InfoBarSeverity.Informational,
            Message = _localizer?["SignOutMessage"] ?? "You have been signed out.",
            Title = "Info",
            Duration = TimeSpan.FromSeconds(10),
        }));
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
    }

    [RelayCommand]
    private async Task DeleteDatabase()
    {
        await DbContext.DropTablesAndRecreate();
        WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
        {
            Severity = InfoBarSeverity.Warning,
            Message = _localizer["DatabseDeletedMessage"] ?? "Database deleted. Restart the app to recreate the database.",
            Title = "Info"
        }));
    }

    [RelayCommand]
    private async Task ResetApplicationState()
    {
        await DbContext.DeleteDatabaseFile();
        await _authentication.LogoutAsync();
        await _authService.SignOutAsync();

#if WINDOWS
        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
#endif

        WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
        {
            Severity = InfoBarSeverity.Warning,
            Message = _localizer["ApplicationResetMessage"] ?? "Application has been reset. Please exit the app.",
            Title = "Danger"
        }));
    }

    public async Task ToggleLocalization()
    {
        var currentCulture = _localizationService.CurrentCulture;
        var culture = _localizationService.SupportedCultures.First(culture => culture.Name != currentCulture.Name);
        await _localizationService.SetCurrentCultureAsync(culture);
    }

    public static string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version == null ? "(Unknown)" : $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    //NotificationQueue.Show(notification);
}
