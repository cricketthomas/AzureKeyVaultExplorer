using System.Diagnostics;
using AzureKeyVaultStudio.UserControls.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace AzureKeyVaultStudio.Presentation;

public sealed partial class MainPage : Page
{
    private AppWindow? _appWindow;
    public MainViewModel? ViewModel => DataContext as MainViewModel;
    public KeyVaultTreeViewModel? KeyVaultTreeViewModel => ViewModel?.KeyVaultTreeViewModel;
    private InputNonClientPointerSource? _nonClientPointerSource;

    public MainPage()
    {
        this.InitializeComponent();
        //Loaded += MainPage_Loaded;
        _appWindow = (Application.Current as App)?.MainWindow?.AppWindow;
        if (_appWindow is not null)
        {
            _nonClientPointerSource = InputNonClientPointerSource.GetForWindowId(_appWindow.Id);
            _appWindow.Closing += _appWindow_Closing;
        }

        //AboveTreeViewArea.Loaded += (s, e) => UpdateDraggableRegion();
        //AboveTreeViewArea.SizeChanged += (s, e) => UpdateDraggableRegion();
        AboveTreeViewArea.SizeChanged += (_, _) => UpdateDraggableRegion();
        MainSplitView.PaneClosed += (_, _) => UpdateDraggableRegion();
        MainSplitView.PaneOpened += (_, _) => UpdateDraggableRegion();
        VaultTabsControl.CustomDragRegionTabViewFooter.SizeChanged +=
            (_, _) => UpdateDraggableRegion();

        SizeChanged += (_, _) => UpdateDraggableRegion();
    }
    private RectInt32 GetWindowRect(FrameworkElement element)
    {
        var transform = element.TransformToVisual(null);
        var position = transform.TransformPoint(
            new Windows.Foundation.Point(0, 0));

        var scale = element.XamlRoot.RasterizationScale;

        return new RectInt32
        {
            X = (int)(position.X * scale),
            Y = (int)(position.Y * scale),
            Width = (int)(element.ActualWidth * scale),
            Height = (int)(element.ActualHeight * scale)
        };
    }

    private void UpdateDraggableRegion()
    {
        if (_nonClientPointerSource is null ||
            AboveTreeViewArea is null ||
            VaultTabsControl?.CustomDragRegionTabViewFooter is null ||
            _appWindow is null)
            return;
        AboveTreeViewArea.Background = new SolidColorBrush(Colors.AliceBlue);
        //VaultTabsControl.CustomDragRegionTabViewFooter.Background = new SolidColorBrush(Colors.AliceBlue);
        try
        {
            var regions = new[]
            {
          
            GetWindowRect(VaultTabsControl.CustomDragRegionTabViewFooter)
        };

            if (MainSplitView.IsPaneOpen)
                regions.Prepend(GetWindowRect(AboveTreeViewArea));

 _nonClientPointerSource.SetRegionRects( NonClientRegionKind.Caption, regions);
        }
        catch
        {
            // Region registration may fail on unsupported configurations.
        }
    }
    private void _appWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (ViewModel is MainViewModel mv)
        {
            mv.SaveAllSettings();
        }
    }



//    private void MainPage_Loaded(object sender, RoutedEventArgs e)
//    {
//#if !MACCATALYST && !HAS_UNO
//        var currentWindow = (Application.Current as App)?.MainWindow;
//        if (currentWindow is null)
//            return;

//        try
//        {
//            //currentWindow.SetTitleBar(DefaultOverrideTitleBar);
//            currentWindow.SetTitleBar(DefaultOverrideTitleBar.TitleBarElement);

//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"Failed to set title bar: {ex.Message}");
//        }

//#endif
//    }

   

}
