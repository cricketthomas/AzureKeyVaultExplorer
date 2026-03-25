using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.Presentation;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthenticationService _authentication;
    private readonly ILocalSettingsService _localSettings;
    private readonly INavigator _navigator;
    private readonly IDispatcher _dispatcher;
    private readonly VaultService _vaultService;
    private readonly AuthService _authService;
    private readonly IServiceProvider _serviceProvider;

    private readonly KeyVaultTreeViewModel _keyVaultTreeViewModel;
    public KeyVaultTreeViewModel KeyVaultTreeViewModel => _keyVaultTreeViewModel;


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

    public string? Title { get; }

    public ICommand GoToSecond { get; }

    public ICommand Logout { get; }

    //private async Task GoToSecondView()
    //{
    //    await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: new Entity(Name!));
    //}

    public async Task DoLogout(CancellationToken token)
    {
        await _authentication.LogoutAsync(token);
    }

    [ObservableProperty]
    public partial SplitViewDisplayMode SplitViewDisplay { get; set; } = SplitViewDisplayMode.Inline;

    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; } = true;

    [ObservableProperty]
    public partial double SplitViewWidth { get; set; } = 220;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial object? SelectedKeyVaultItem { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPaneLeft))]
    [NotifyPropertyChangedFor(nameof(FontIconType))]
    public partial SplitViewPanePlacement PanePlacement { get; set; } = SplitViewPanePlacement.Right;

    public bool IsPaneLeft => PanePlacement == SplitViewPanePlacement.Left;

    public string FontIconType => PanePlacement == SplitViewPanePlacement.Left ? "\uE8E4" : "\uE8E2";
    //public FontIcon FontIconType => PanePlacement == SplitViewPanePlacement.Left ? new FontIcon() { Glyph = "&#xE8E4;" } : new FontIcon() { Glyph = "&#xE8E2;" };

    private void LoadSplitViewSettings()
    {
        SplitViewDisplay = GetSettingsValue(nameof(SplitViewDisplay), SplitViewDisplayMode.Inline);
        SplitViewWidth = _localSettings.GetValue(nameof(SplitViewWidth), 300d);
        IsPaneOpen = _localSettings.GetValue(nameof(IsPaneOpen), true);
        PanePlacement = GetSettingsValue(nameof(PanePlacement), SplitViewPanePlacement.Right);
    }



    [RelayCommand]
    public void ToggleSplitViewDisplay()
    {
        SplitViewDisplay = SplitViewDisplay == SplitViewDisplayMode.Overlay ? SplitViewDisplayMode.Inline : SplitViewDisplayMode.Overlay;
    }

    [RelayCommand]
    public void TogglePaneLocation()
    {
        PanePlacement = PanePlacement == SplitViewPanePlacement.Right ? SplitViewPanePlacement.Left : SplitViewPanePlacement.Right;
    }



    private void Save<T>(string key, T value)
    {
        _localSettings.SetValue(key, value);
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
    public void ClosePane()
    {
        IsPaneOpen = false;
        WeakReferenceMessenger.Default.Send(new PaneStateChangedMessage(false));
    }

    private const double PaneMinWidth = 100;
    private const double PaneMaxWidth = 1000;

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
                OnPropertyChanged(nameof(SplitViewWidth));
            }
        }
    }
    private T GetSettingsValue<T>(string key, T defaultValue) where T : struct, Enum
    {
        var stringValue = _localSettings.GetValue(key, defaultValue.ToString());
        return Enum.TryParse<T>(stringValue, out var result) ? result : defaultValue;
    }
}
