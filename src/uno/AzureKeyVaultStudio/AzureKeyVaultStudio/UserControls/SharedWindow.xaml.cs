using Microsoft.UI.Windowing;

namespace AzureKeyVaultStudio.UserControls;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SharedWindow : Window
{
    private FrameworkElement? _mainContent;
    private Window? _ownerWindow;
    private bool _themeInitialized = false;
    private bool _isDisposed = false;

    public SharedWindow()
    {
        this.InitializeComponent();
#if WINDOWS && !HAS_UNO
        this.Activated += OnWindowActivated;

        if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
        {
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        }
#endif


        AppWindow.SetIcon("Assets/AppIcon.png");
        if (Application.Current is App app && app.MainWindow is Window mainWindow && !ReferenceEquals(mainWindow, this))
        {
            _ownerWindow = mainWindow;
            _ownerWindow.Closed += OnOwnerWindowClosed;
        }

        this.Closed += OnWindowClosed;
    }

#if WINDOWS && !HAS_UNO
    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_isDisposed || _themeInitialized)
            return;
        try
        {
            if (this.Content is FrameworkElement rootElement)
            {
                _themeInitialized = true;
                if (Application.Current is App app && app.MainWindow?.Content is FrameworkElement mainContent)
                {
                    _mainContent = mainContent;
                    rootElement.RequestedTheme = mainContent.ActualTheme;
                    _mainContent.ActualThemeChanged += OnMainWindowThemeChanged;
                }
                this.Activated -= OnWindowActivated;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to sync theme: {ex.Message}");
        }
    }

    private void OnMainWindowThemeChanged(FrameworkElement sender, object args)
    {
        if (_isDisposed)
            return;

        try
        {
            if (this.Content is FrameworkElement element && _mainContent != null)
            {
                element.RequestedTheme = _mainContent.ActualTheme;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update theme: {ex.Message}");
        }
    }
#endif

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

#if WINDOWS && !HAS_UNO
        try
        {
            if (_mainContent != null)
            {
                _mainContent.ActualThemeChanged -= OnMainWindowThemeChanged;
                _mainContent = null;
            }
            this.Activated -= OnWindowActivated;
        }
        catch (Exception ex)
        {
            // Suppress exceptions during cleanup
            System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }
#endif

        this.Closed -= OnWindowClosed;

        if (_ownerWindow != null)
        {
            _ownerWindow.Closed -= OnOwnerWindowClosed;
            _ownerWindow = null;
        }
    }

    private void OnOwnerWindowClosed(object sender, WindowEventArgs args)
    {
        if (_isDisposed)
            return;

        this.Close();
    }
}
