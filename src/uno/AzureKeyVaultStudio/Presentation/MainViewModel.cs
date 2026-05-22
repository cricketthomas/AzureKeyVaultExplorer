using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.Presentation;

public partial class MainViewModel : ObservableObject
{
    private const double PaneMaxWidth = 800;
    private const double PaneMinWidth = 120;
    private readonly IAuthenticationService _authentication;
    private readonly AuthService _authService;
    private readonly IDispatcher _dispatcher;
    private readonly KeyVaultTreeViewModel _keyVaultTreeViewModel;
    private readonly ILocalSettingsService _localSettings;
    private readonly INavigator _navigator;
    private readonly IServiceProvider _serviceProvider;
    private readonly VaultService _vaultService;
    public MainViewModel(
        IDispatcher dispatcher,
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        IAuthenticationService authentication,
        INavigator navigator,
        ILocalSettingsService localSettings,
        KeyVaultTreeViewModel keyVaultTreeViewModel,
        VaultService vaultService,
        IServiceProvider serviceProvider,
        AuthService authService)
    {
        _navigator = navigator;
        _authentication = authentication;
        _localSettings = localSettings;
        _dispatcher = dispatcher;
        _vaultService = vaultService;
        _serviceProvider = serviceProvider;
        keyVaultTreeViewModel.SetDispatcher(dispatcher); // HACK
        _keyVaultTreeViewModel = keyVaultTreeViewModel;
        _authService = authService;

        Title = "Main";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";

        //GoToSecond = new AsyncRelayCommand(GoToSecondView);
        Logout = new AsyncRelayCommand(DoLogout);

        LoadSplitViewSettings();

        WeakReferenceMessenger.Default.Register<MainViewModel, PaneStateChangedMessage>(this, (r, m) =>
        {
            r.IsPaneOpen = m.Value;
        });
    }

    public string FontIconType => PanePlacement == SplitViewPanePlacement.Left ? "\uE8E4" : "\uE8E2";
    public ICommand GoToSecond { get; }
    public double InvertedSplitViewWidth
    {
        get => PaneMaxWidth - (SplitViewWidth - PaneMinWidth);
        set
        {
            var clamped = Math.Clamp(value, PaneMinWidth, PaneMaxWidth);
            var newActual = PaneMaxWidth - (clamped - PaneMinWidth);

            if (SplitViewWidth != newActual)
            {
                SplitViewWidth = newActual;
            }
        }
    }

    public bool IsPaneLeft => PanePlacement == SplitViewPanePlacement.Left;
    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; } = true;

    public KeyVaultTreeViewModel KeyVaultTreeViewModel => _keyVaultTreeViewModel;
    public ICommand Logout { get; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPaneLeft))]
    [NotifyPropertyChangedFor(nameof(FontIconType))]
    public partial SplitViewPanePlacement PanePlacement { get; set; } = SplitViewPanePlacement.Right;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial object? SelectedKeyVaultItem { get; set; }

    [ObservableProperty]
    public partial SplitViewDisplayMode SplitViewDisplay { get; set; } = SplitViewDisplayMode.Inline;

    [ObservableProperty]
    public partial double SplitViewWidth { get; set; } = 220;

    public string? Title { get; }
    [RelayCommand]
    public void ClosePane()
    {
        IsPaneOpen = false;
        WeakReferenceMessenger.Default.Send(new PaneStateChangedMessage(false));
    }

    public async Task DoLogout(CancellationToken token)
    {
        await _authentication.LogoutAsync(token);
    }

    [RelayCommand]
    public void SaveAllSettings()
    {
        Save(nameof(SplitViewDisplay), SplitViewDisplay.ToString());
        Save(nameof(SplitViewWidth), SplitViewWidth);
        Save(nameof(PanePlacement), PanePlacement.ToString());
        Save(nameof(IsPaneOpen), IsPaneOpen);
    }

    [RelayCommand]
    public void TogglePaneLocation()
    {
        PanePlacement = PanePlacement == SplitViewPanePlacement.Right ? SplitViewPanePlacement.Left : SplitViewPanePlacement.Right;
    }

    [RelayCommand]
    public void ToggleSplitViewDisplay()
    {
        SplitViewDisplay = SplitViewDisplay == SplitViewDisplayMode.Overlay ? SplitViewDisplayMode.Inline : SplitViewDisplayMode.Overlay;
    }

    private T GetSettingsValue<T>(string key, T defaultValue) where T : struct, Enum
    {
        var stringValue = _localSettings.GetValue(key, defaultValue.ToString());
        return Enum.TryParse<T>(stringValue, out var result) ? result : defaultValue;
    }

    private void LoadSplitViewSettings()
    {
        SplitViewDisplay = GetSettingsValue(nameof(SplitViewDisplay), SplitViewDisplayMode.Inline);
        SplitViewWidth = _localSettings.GetValue(nameof(SplitViewWidth), 300d);
        IsPaneOpen = _localSettings.GetValue(nameof(IsPaneOpen), true);
        PanePlacement = GetSettingsValue(nameof(PanePlacement), SplitViewPanePlacement.Right);
    }
    private void Save<T>(string key, T value)
    {
        _localSettings.SetValue(key, value);
    }
}
