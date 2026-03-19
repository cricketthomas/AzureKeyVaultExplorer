using System.Diagnostics;
using AzureKeyVaultStudio.UserControls.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;

namespace AzureKeyVaultStudio.Presentation;

public sealed partial class MainPage : Page
{
    private AppWindow? _appWindow;
    public MainViewModel? ViewModel => DataContext as MainViewModel;
    public KeyVaultTreeViewModel? KeyVaultTreeViewModel => ViewModel?.KeyVaultTreeViewModel;

    public MainPage()
    {
        this.InitializeComponent();
        Loaded += MainPage_Loaded;
        _appWindow = (Application.Current as App)?.MainWindow?.AppWindow;
        if (_appWindow is not null)
            _appWindow.Closing += _appWindow_Closing;
    }

    private void _appWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (ViewModel is MainViewModel mv)
        {
            mv.SaveAllSettings();
        }
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
#if !MACCATALYST && !HAS_UNO
        var currentWindow = (Application.Current as App)?.MainWindow;
        if (currentWindow is null)
            return;

        try
        {
            currentWindow.SetTitleBar(DefaultOverrideTitleBar);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set title bar: {ex.Message}");
        }

#endif
    }

   

}
