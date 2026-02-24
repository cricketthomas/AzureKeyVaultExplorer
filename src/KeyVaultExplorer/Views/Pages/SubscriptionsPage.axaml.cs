using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using KeyVaultExplorer.ViewModels;

namespace KeyVaultExplorer.Views.Pages;

public partial class SubscriptionsPage : UserControl
{
    private bool IsInitialLoad = true;

    public SubscriptionsPage()
    {
        InitializeComponent();
        DataContext = Defaults.Locator.GetRequiredService<SubscriptionsPageViewModel>();
        AddHandler(Frame.NavigatedToEvent, OnNavigatedTo, RoutingStrategies.Direct);
    }

    private void OnNavigatedTo(object sender, NavigationEventArgs e)
    {
        if (e.NavigationMode != NavigationMode.Back && IsInitialLoad)
        {
            IsInitialLoad = false;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await (DataContext as SubscriptionsPageViewModel)!.GetSubscriptions();
            }, DispatcherPriority.Background);
        }
    }
}