using Avalonia.Controls;
using KeyVaultExplorer.ViewModels;

namespace KeyVaultExplorer;

public partial class CreateNewSecretVersion : UserControl
{
    public CreateNewSecretVersion()
    {
        InitializeComponent();
        var vm = new CreateNewSecretVersionViewModel();
        DataContext = vm;
    }

    //private void Subscription_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    //{
    //    var item = (sender as AutoCompleteBox)!.SelectedItem as SubscriptionDataItem;
    //    (DataContext as CreateNewSecretVersionViewModel)!.SelectedSubscriptionChangedCommand.Execute(item);
    //}
}