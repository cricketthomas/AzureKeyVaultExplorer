// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

using System.Linq;
using CommunityToolkit.WinUI.UI.Controls;

namespace AzureKeyVaultStudio.Presentation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SubscriptionsPage : Page
{
    public SubscriptionViewModel ViewModel => DataContext as SubscriptionViewModel;

    public SubscriptionsPage()
    {
        this.InitializeComponent();
        DataContextChanged += SubscriptionsPage_DataContextChanged;
    }

    private void SubscriptionsPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (ViewModel is SubscriptionViewModel vm)
            vm.LoadSubscriptionCommand.ExecuteAsync(CancellationToken.None);
    }

    private void SubscriptionsDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
    {
        var sortBy = e.Column.Tag?.ToString();

        if (string.IsNullOrEmpty(sortBy) || ViewModel?.Subscriptions == null)
            return;

        var direction = e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending
            ? DataGridSortDirection.Ascending
            : DataGridSortDirection.Descending;

        if (sender is DataGrid dg)
        {
            foreach (var column in dg.Columns)
                if (column != e.Column)
                    column.SortDirection = null;
        }

        e.Column.SortDirection = direction;

        var items = ViewModel.Subscriptions.ToList();

        if (direction == DataGridSortDirection.Ascending)
            items = items.OrderBy(x => GetPropertyValue(x, sortBy)).ToList();
        else
            items = items.OrderByDescending(x => GetPropertyValue(x, sortBy)).ToList();

        ViewModel.Subscriptions.Clear();
        ViewModel.Subscriptions.AddRange(items);
    }

    private static object? GetPropertyValue(object obj, string propertyPath)
    {
        if (obj == null || string.IsNullOrEmpty(propertyPath))
            return null;

        object? current = obj;
        foreach (var part in propertyPath.Split('.'))
        {
            if (current == null)
                return null;
            var prop = current.GetType().GetProperty(part);
            if (prop == null)
                return null;
            current = prop.GetValue(current);
        }
        return current;
    }
}
