using System.Collections.Specialized;
using System.ComponentModel;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using static AzureKeyVaultStudio.Models.KvTreeNodeModel;

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class KeyVaultTree : UserControl
{
    public DispatcherQueueTimer _debounceTimerSearch = DispatcherQueue.GetForCurrentThread().CreateTimer();
    private readonly MenuFlyout keyVaultMenuFlyout;

    public KeyVaultTreeViewModel? ViewModel => DataContext as KeyVaultTreeViewModel;

    public KeyVaultTree()
    {
        InitializeComponent();
        //Loaded += KeyVaultTree_Loaded;
        DataContextChanged += OnDataContextChanged;
        keyVaultMenuFlyout = Resources["TreeViewContextFlyout"] as MenuFlyout;
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
#if HAS_UNO_SKIA
    Bindings.Update();
#endif

        if (ViewModel?.RefreshCommand is null || ViewModel.HasFetchedData)
        {
            return;
        }
        Bindings.Update();

        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (ViewModel is { HasFetchedData: false } vm && !vm.RefreshCommand.IsRunning)
            {
                vm.RefreshCommand.Execute(null);
            }
        });
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is KeyVaultTreeViewModel viewModel && sender is TextBox model)
        {
            _debounceTimerSearch.Debounce(() =>
            {
                ViewModel.SearchQuery = model.Text;
                ViewModel.ExecuteSearchCommand.ExecuteAsync(null);
            }, interval: TimeSpan.FromMilliseconds(200));
        }
    }

    private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext is KvTreeNodeModel node)
        {
            ViewModel?.OpenInNewTabCommand?.Execute(node);
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

    // since we are not using a data template, we need to hide the menu for specfic items.
    // this is due to a DataTemplate bug which would crash the app in v2.0.0
    private void TreeViewItem_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        var point = new Point(0, 0);
        if (sender as TreeViewItem != null && (sender as TreeViewItem)!.DataContext is KvKeyVaultResourceModel)
        {
            if (args.TryGetPosition(sender, out point))
            {
                keyVaultMenuFlyout.ShowAt(sender, point);
            }
            else
            {
                keyVaultMenuFlyout.ShowAt((FrameworkElement)sender);
            }
        }
    }
}
