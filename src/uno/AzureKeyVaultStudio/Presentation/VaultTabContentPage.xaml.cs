using System.Collections.ObjectModel;
using AzureKeyVaultStudio.Presentation.ViewModels;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace AzureKeyVaultStudio.Presentation;

public sealed partial class VaultTabContentPage : Page
{
    public VaultViewModel? ViewModel { get; set; }

    public VaultTabContentPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = NavigationCacheMode.Required;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is VaultViewModel vm)
        {
            if (ViewModel is not null)
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

            ViewModel = vm;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            Bindings.Update();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VaultViewModel.VaultContents)
            && ViewModel?.FilterAndLoadVaultValueTypeCommand.IsRunning == false)
        {
            UpdateDataGridItemsSource();
        }
    }

    private void UpdateDataGridItemsSource()
    {
        if (ViewModel?.VaultContents == null)
            return;

        if (ViewModel.SelectedTab == KeyVaultItemType.All && ViewModel.VaultContents.Count > 0)
        {
            var data = GroupData();
            VaultContentDataGrid.RowGroupHeaderPropertyNameAlternative = "Type";
            VaultContentDataGrid.ItemsSource = data.View;
        }
        else
        {
            VaultContentDataGrid.ItemsSource = ViewModel.VaultContents;
        }
    }

    private void VaultContentDataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.Handled)
            return;
        var selectedItem = VaultContentDataGrid.SelectedItem ?? ViewModel?.SelectedRow;
        if (selectedItem != null && ViewModel?.ShowPropertiesCommand.CanExecute(selectedItem) == true)
        {
            // hack, bug in uno WCT version, not highlighing the row on double click, so we get the item from the event args.
#if HAS_UNO_SKIA
           if(e.OriginalSource is UIElement {} elem)
                selectedItem = elem.DataContext as KeyVaultItemProperties;
#endif
            e.Handled = true;
            var item = selectedItem;
            // strange behaviro, could be a bug in my code. On Skia, we are opening dialogs twice, but on windows, they are handled correctly..
#if HAS_UNO_SKIA
            ViewModel.ShowPropertiesCommand.Execute(item);
#else
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
            {
                ViewModel.ShowPropertiesCommand.Execute(item);
            });
#endif
            e.Handled = true;
        }
    }

    private void LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row != null)
        {
            e.Row.RightTapped += Row_RightTapped;
            // e.Row.DoubleTapped += VaultContentDataGrid_DoubleTapped;
        }
    }

    private void Row_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is DataGridRow row && row.DataContext != null)
        {
            VaultContentDataGrid.SelectedItem = row.DataContext;
            e.Handled = true;
        }
    }

    private void SearchTextBox_KeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SearchTextBox.Focus(FocusState.Programmatic);
        args.Handled = true;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox model)
        {
            ViewModel.SearchQuery = model.Text;
        }
    }

    // only on skia.. hacky thing with kb accelerators
    private void VaultContentDataGrid_PreviewKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.C && Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            var selectedItem = VaultContentDataGrid.SelectedItem ?? ViewModel?.SelectedRow;
            if (selectedItem is not null && ViewModel?.CopyCommand.CanExecute(selectedItem) == true)
            {
                ViewModel.CopyCommand.Execute(selectedItem);
                args.Handled = true;
            }
        }
    }

    private void VaultContentDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
    {
        var sortBy = e.Column.Tag?.ToString();

        if (string.IsNullOrEmpty(sortBy) || ViewModel?.VaultContents == null)
            return;

        var direction = e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending
            ? DataGridSortDirection.Ascending
            : DataGridSortDirection.Descending;

        foreach (var column in VaultContentDataGrid.Columns)
            if (column != e.Column)
                column.SortDirection = null;

        e.Column.SortDirection = direction;

        var items = ViewModel.VaultContents.ToList();

        if (direction == DataGridSortDirection.Ascending)
            items = items.OrderBy(x => GetPropertyValue(x, sortBy)).ToList();
        else
            items = items.OrderByDescending(x => GetPropertyValue(x, sortBy)).ToList();

        if (ViewModel.SelectedTab == KeyVaultItemType.All)
        {
            var data = GroupData(items);
            VaultContentDataGrid.ItemsSource = data.View;
            ViewModel.SelectedRow = null; // HACK: weird behavior where it selects a row when sorting.
        }
        else
        {
            ViewModel.VaultContents.Clear();
            ViewModel.VaultContents.AddRange(items);
        }
    }

    private void VaultContentDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel?.SelectedTab != KeyVaultItemType.All)
            return;

        if (VaultContentDataGrid.SelectedItem is KeyVaultItemProperties item)
            ViewModel.SelectedRow = item;
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj);
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;

            current = VisualTreeHelper.GetParent(current);
        }

        return default;
    }

    public CollectionViewSource GroupData(List<KeyVaultItemProperties>? data = null)
    {
        ObservableCollection<GroupInfoCollection<KeyVaultItemProperties>> groups = new();

        var dataToGroup = data ?? ViewModel?.VaultContents.ToList();

        var query = dataToGroup?
            .GroupBy(x => x.Type)
            .Select(g => new { GroupName = g.Key, Items = g });

        foreach (var g in query)
        {
            var info = new GroupInfoCollection<KeyVaultItemProperties> { Key = g.GroupName };
            info.Key = g.GroupName;
            foreach (var item in g.Items)
                info.Add(item);
            groups.Add(info);
        }
        var groupedItems = new CollectionViewSource
        {
            IsSourceGrouped = true,
            Source = groups
        };
        return groupedItems;
    }

    public class GroupInfoCollection<T> : ObservableCollection<T>
    {
        public object Key { get; set; }
    }

    private void VaultContentDataGrid_LoadingRowGroup(object sender, DataGridRowGroupHeaderEventArgs e)
    {
        ICollectionViewGroup group = e.RowGroupHeader.CollectionViewGroup;
        var item = group.GroupItems[0] as KeyVaultItemProperties;
        e.RowGroupHeader.PropertyValue = item.Type.ToString();
    }

    private void Segmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateDataGridItemsSource();
    }

 
}
