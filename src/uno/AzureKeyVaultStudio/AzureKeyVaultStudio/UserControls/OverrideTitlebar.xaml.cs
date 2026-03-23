using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class OverrideTitlebar : UserControl
{
    public static readonly DependencyProperty IsBackButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsBackButtonVisible),
            typeof(bool),
            typeof(OverrideTitlebar),
            new PropertyMetadata(default(OverrideTitlebar)));

    public bool IsBackButtonVisible
    {
        get => (bool)GetValue(IsBackButtonVisibleProperty);
        set => SetValue(IsBackButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty SecondaryTitleProperty =
        DependencyProperty.Register(
            nameof(SecondaryTitle),
            typeof(string),
            typeof(OverrideTitlebar),
            new PropertyMetadata(default(OverrideTitlebar)));

    public string SecondaryTitle
    {
        get => (string)GetValue(SecondaryTitleProperty);
        set => SetValue(SecondaryTitleProperty, value);
    }

    private AppWindow? _appWindow;


    //#if WINDOWS && !MACCATALYST && !HAS_UNO
    //    public UIElement? TitleBarElement =>  DefaultTitleBar;
    //#endif

    public OverrideTitlebar()
    {
        this.InitializeComponent();
        _appWindow = (Application.Current as App)?.MainWindow?.AppWindow;

        if (_appWindow is not null)
        {
            //_appWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            if (!string.IsNullOrWhiteSpace(SecondaryTitle))
                _appWindow.Title = SecondaryTitle ;

        }
    }

    private async void BackButton_Click(object sender, RoutedEventArgs e)
    {
        await GoBack();
    }

    private async void DefaultTitleBar_BackRequested(TitleBar sender, object args)
    {
        await GoBack();
    }

    private async void Page_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as UIElement).Properties.IsXButton1Pressed)
        {
            await GoBack();
        }
    }

    private async Task GoBack()
    {
        var navigator = this.Navigator();
        if (navigator == null)
            return;

        try
        {
            if (await navigator.CanGoBack())
                await navigator.NavigateBackAsync(this);
        }
        catch (Exception ex)
        {
            Debug.Write($"Failed to navigate back {ex.Message}", ex.StackTrace);
        }
    }
}
