using System.Diagnostics;
using AzureKeyVaultStudio.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class OverrideTitlebar : UserControl
{
    public bool IsBackButtonVisible
    {
        get => (bool)GetValue(IsBackButtonVisibleProperty);
        set => SetValue(IsBackButtonVisibleProperty, value);
    }

    public string SecondaryTitle
    {
        get => (string)GetValue(SecondaryTitleProperty);
        set => SetValue(SecondaryTitleProperty, value);
    }

    public static readonly DependencyProperty IsBackButtonVisibleProperty =
    DependencyProperty.Register(nameof(IsBackButtonVisible), typeof(bool),
        typeof(OverrideTitlebar), new PropertyMetadata(false));

    public static readonly DependencyProperty SecondaryTitleProperty =
        DependencyProperty.Register(nameof(SecondaryTitle), typeof(string),
            typeof(OverrideTitlebar), new PropertyMetadata(string.Empty));

    public OverrideTitlebar()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {

        WeakReferenceMessenger.Default.Register<BackButtonIsVisibleMessage>(this, async (r, m) => IsBackButtonVisible = true);
        WeakReferenceMessenger.Default.Register<BackButtonIsHiddenMessage>(this, async (r, m) => IsBackButtonVisible = false);
        var window = (Application.Current as App)?.MainWindow;
        if (window is null) return;

        if (!string.IsNullOrWhiteSpace(SecondaryTitle))
            window.Title = SecondaryTitle;

#if WINDOWS
        if (!IsLoaded) return;
        window.SetTitleBar(DefaultTitleBar);
#endif
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<NavigationBackRequestedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<BackButtonIsVisibleMessage>(this);
        WeakReferenceMessenger.Default.Unregister<BackButtonIsHiddenMessage>(this);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(message: new NavigationBackRequestedMessage());
    }

    private void DefaultTitleBar_BackRequested(TitleBar sender, object args)
    {
        WeakReferenceMessenger.Default.Send(new NavigationBackRequestedMessage());
    }
  
}
