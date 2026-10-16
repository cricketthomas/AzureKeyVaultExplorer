using AzureKeyVaultStudio.Services;
using Microsoft.UI.Windowing;
using Uno.Disposables;

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class SharedWindow : Window
{
    private FrameworkElement? _mainContent;
    private static bool _isDisposed = false;

    public SharedWindow()
    {
        this.InitializeComponent();
        this.Activated += OnWindowActivated;

        if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
        {
#if WINDOWS && !HAS_UNO
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
#endif
            this.SetWindowIcon();
        }
        TrackWindow(this);
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (this.Content is FrameworkElement rootElement)
        {
            if (Application.Current is App app && app.MainWindow?.Content is FrameworkElement mainContent)
            {
                _mainContent = mainContent;
                rootElement.RequestedTheme = mainContent.ActualTheme;
                _mainContent.ActualThemeChanged += OnMainWindowThemeChanged;
            }
            this.Activated -= OnWindowActivated;
        }
    }

    private void OnMainWindowThemeChanged(FrameworkElement sender, object args)
    {
        if (this.Content is FrameworkElement element && _mainContent != null && !_isDisposed)
        {
            element.RequestedTheme = _mainContent.ActualTheme;
        }
    }

    private static void TrackWindow(Window window)
    {
        window.Closed += (sender, args) =>
        {
            App.ActiveWindows.Remove(window.AppWindow.Id, out window);
            _isDisposed = true;
            window?.TryDispose();


        };
        App.ActiveWindows.Add(window.AppWindow.Id, window);
    }
}
