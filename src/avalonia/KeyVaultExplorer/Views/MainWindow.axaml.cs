using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.UI.Windowing;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.Services;
using KeyVaultExplorer.ViewModels;
using KeyVaultExplorer.Views.Pages;
using System;
using System.IO;

namespace KeyVaultExplorer.Views;

public partial class MainWindow : AppWindow
{
    public static readonly RoutedEvent<RoutedEventArgs> SetAppThemeEvent = RoutedEvent.Register<MainView, RoutedEventArgs>(nameof(SetAppThemeEvent), RoutingStrategies.Tunnel);
    public static readonly RoutedEvent<RoutedEventArgs> TransparencyChangedEvent = RoutedEvent.Register<MainView, RoutedEventArgs>(nameof(TransparencyChangedEvent), RoutingStrategies.Tunnel);
    private IBrush BackgroundBrush;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(TransparencyChangedEvent, OnTransparencyChangedEvent, RoutingStrategies.Tunnel, handledEventsToo: false);
        AddHandler(SetAppThemeEvent, OnSetAppThemeEvent, RoutingStrategies.Tunnel, handledEventsToo: false);
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        //TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(35, 155, 155, 155);
        App.Current.Resources.TryGetResource("DynamicActiveBackgroundFAColor", null, out var bg);
        BackgroundBrush = (IBrush)bg;
        var path = Path.Combine(Constants.LocalAppDataFolder, "settings.json");
        var settings = Defaults.Locator.GetRequiredService<AppSettingReader>();
        TransparencyEnabled = settings.AppSettings.BackgroundTransparency;
        Application.Current.RequestedThemeVariant = GetThemeVariant(settings.AppSettings.AppTheme);

        // TODO: get background transparency from db to know if to enable this on startup.
        if (TransparencyEnabled)
        {
            Background = null;
        }

        if (OperatingSystem.IsWindows())
        {
            Activated += (sender, e) =>
        {
            if (TransparencyEnabled)
                Background = null;
        };
            Deactivated += (sender, e) =>
            {
                Background = BackgroundBrush;
            };
        }
        else
        {
            Background = BackgroundBrush;
        }
    }

    private bool TransparencyEnabled { get; set; }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var _notificationViewModel = Defaults.Locator.GetRequiredService<NotificationViewModel>();

        var nm = new Avalonia.Controls.Notifications.WindowNotificationManager(TopLevel.GetTopLevel(this))
        {
            Position = Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
            MaxItems = 3,
        };
        _notificationViewModel.NotificationManager = nm;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (TitleBar is not null && OperatingSystem.IsWindows())

        {
            var parent = this.FindControl<Views.MainView>("MainView")!.FindControl<FluentAvalonia.UI.Controls.Frame>("FrameView");

            var tab = ((MainPage)parent.Content).Content as TabViewPage;
            var dragRegion = tab.FindControl<Panel>("CustomDragRegion");
            //var tab = grid.Children[0] as TabViewPage;
            dragRegion.Width = FlowDirection == Avalonia.Media.FlowDirection.LeftToRight ?
                TitleBar.RightInset * 1.25 : TitleBar.LeftInset * 1.25;
        }
    }

    private ThemeVariant GetThemeVariant(string value)
    {
        return value switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            "System" or null => null,
            _ => throw new InvalidOperationException("Unknown theme variant")
        };
    }

    private void OnSetAppThemeEvent(object sender, RoutedEventArgs e)
    {
        var theme = (e.Source as ComboBox).SelectedItem as string;
        Application.Current.RequestedThemeVariant = GetThemeVariant(theme!);
        e.Handled = true;
    }

    private void OnTransparencyChangedEvent(object sender, RoutedEventArgs e)
    {
        var isChecked = (e.Source as CheckBox)?.IsChecked ?? false;
        if (OperatingSystem.IsWindows() && isChecked)
        {
            Background = null;
            TransparencyEnabled = true;
        }
        else
        {
            Background = BackgroundBrush;
            TransparencyEnabled = false;
        }
        e.Handled = true;
    }
}