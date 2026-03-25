using System.Collections.Specialized;
using System.ComponentModel;
using Azure.ResourceManager.KeyVault;
using AzureKeyVaultStudio.Models;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using static AzureKeyVaultStudio.Models.KvTreeNodeModel;

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
                var node = (flyout.Target as FrameworkElement)?.DataContext as KvTreeNodeModel
                    ?? viewModel.SelectedItem as KvTreeNodeModel;
                flyout.DataContext = node;
            }
        };
#endif
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (ViewModel?.RefreshCommand is not null)
        {
            Bindings.Update();
            ViewModel.RefreshCommand.Execute(null);
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
        if (sender is TreeViewItem treeViewItem
            && treeViewItem.DataContext is KvTreeNodeModel node
            && node.VaultResource is KeyVaultResource model)
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

    private void KeyVaultTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (args.Item is KvSubscriptionModel sub)
        {
            sub.IsExpanded = true;
            //sub.IsSelected = true;
        }
        else if (args.Item is KvResourceGroupModel rg)
        {
            rg.IsExpanded = true;
            //rg.IsSelected = true;
        }
    }

    private void KeyVaultTreeView_Collapsed(TreeView sender, TreeViewCollapsedEventArgs args)
    {
        if (args.Item is KvSubscriptionModel sub)
            sub.IsExpanded = false;
        else if (args.Item is KvResourceGroupModel rg)
            rg.IsExpanded = false;
    }

    private void KeyVaultTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        args.Cancel = true;
        return;
    }
}
