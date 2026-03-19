using System.Collections.Specialized;
using System.ComponentModel;
using Azure.ResourceManager.KeyVault;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class KeyVaultTree : UserControl
{
    private IStringLocalizer? _localizer { get; set; }
    public DispatcherQueueTimer _debounceTimerSearch = DispatcherQueue.GetForCurrentThread().CreateTimer();

    public KeyVaultTreeViewModel? ViewModel => DataContext as KeyVaultTreeViewModel;

    public KeyVaultTree()
    {
        InitializeComponent();
        //Loaded += KeyVaultTree_Loaded;
        DataContextChanged += OnDataContextChanged;

#if HAS_UNO
        var flyout = Resources["TreeViewContextFlyout"] as MenuFlyout;
        flyout.DataContext = null;
        flyout.Opening += (s, e) =>
        {
            if (DataContext is KeyVaultTreeViewModel viewModel)
            {
                flyout.DataContext = viewModel.SelectedItem;
            }
        };
#endif
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (ViewModel?.RefreshCommand is not null)
        {
            Bindings.Update();
            ViewModel.RefreshCommand.Execute(CancellationToken.None);
        }
    }

    //private async void KeyVaultTreeView_Loaded(object sender, RoutedEventArgs e)
    //{
    //    if (ViewModel?.RefreshCommand is not null)
    //    {
    //        ViewModel.RefreshCommand.ExecuteAsync(null);
    //    }
    //}

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is KeyVaultTreeViewModel viewModel && sender is TextBox model)
        {
            _debounceTimerSearch.Debounce(() =>
            {
                ViewModel.SearchQuery = model.Text;
                ViewModel.ExecuteSearchCommand.ExecuteAsync(null);
            },
            interval: TimeSpan.FromMilliseconds(120));
        }
    }

    private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext is KeyVaultResource model)
        {
            ViewModel?.OpenInNewTabCommand?.Execute(model);
        }
        e.Handled = true;
    }

    private void SearchTextBox_KeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SearchTextBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        args.Handled = true;
    }

    private void ViewModelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    { }

    private void SubscriptionModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    { }

    private void SubNodeCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    { }

    private void ResourceGroupNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    { }

}
