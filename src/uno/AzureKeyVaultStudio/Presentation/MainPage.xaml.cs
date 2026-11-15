using System.Diagnostics;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Uno.Extensions.Navigation.Regions;
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
        _appWindow = (Application.Current as App)?.MainWindow?.AppWindow;
        if (_appWindow is not null)
        {
            _nonClientPointerSource = InputNonClientPointerSource.GetForWindowId(_appWindow.Id);
            _appWindow.Closing += _appWindow_Closing;
        }

        //AboveTreeViewArea.Loaded += (s, e) => UpdateDraggableRegion();
        SizeChanged += (_, _) => UpdateDraggableRegion();
        AboveTreeViewArea.SizeChanged += (s, e) => UpdateDraggableRegion();
        VaultTabsControl.CustomDragRegionTabViewFooter.Loaded += (_, _) => UpdateDraggableRegion();
        //AboveTreeViewArea.SizeChanged += (_, _) => UpdateDraggableRegion();
        MainSplitView.SizeChanged += (_, _) => UpdateDraggableRegion();
        MainSplitView.PaneClosed += (_, _) => UpdateDraggableRegion();
        MainSplitView.PaneOpened += (_, _) => UpdateDraggableRegion();
        VaultTabsControl.CustomDragRegionTabViewFooter.SizeChanged += (_, _) => UpdateDraggableRegion();
        MainSplitView.RegisterPropertyChangedCallback(SplitView.PanePlacementProperty,
        (sender, property) =>
        {
            UpdateDraggableRegion();
        });

        WeakReferenceMessenger.Default.Register<TitleBarChangedMessage>(this, (r, m) => UpdateTitleBar(m.Value));
    }

    private void UpdateTitleBar(FrameworkElement backButtonPassthrough)
    {
#if WINDOWS
        if (_nonClientPointerSource is null)
            return;
        var backButton = GetWindowRect(backButtonPassthrough);
        _nonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, [backButton]);
        Debug.WriteLine("title bar updated");
#endif
    }

    private RectInt32 GetWindowRect(FrameworkElement element)
    {
        var transform = element.TransformToVisual(null);
        var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

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
        try
        {
            var regions = new[]
            {
                GetWindowRect(VaultTabsControl.CustomDragRegionTabViewFooter)
             };

            if (MainSplitView.IsPaneOpen)
            {
                regions = new[]
                {
                    GetWindowRect(AboveTreeViewArea),
                    GetWindowRect(VaultTabsControl.CustomDragRegionTabViewFooter)
                };
            }

            _nonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, regions);
        }
        catch
        {
            Debug.Write("unsupported config");
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
