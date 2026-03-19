using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultStudio.Exceptions;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using System.Linq;
using CommunityToolkit.WinUI.UI.Controls;

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class ItemDetails : UserControl
{
    public ItemPropertiesViewModel ViewModel => DataContext as ItemPropertiesViewModel;

    public ItemDetails()
    {
        InitializeComponent();
    }

    private void VersionsDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
    {
        var sortBy = e.Column.Tag?.ToString();

        if (string.IsNullOrEmpty(sortBy) || ViewModel?.ItemPropertiesList == null)
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

        var items = ViewModel.ItemPropertiesList.ToList();

        if (direction == DataGridSortDirection.Ascending)
            items = items.OrderBy(x => GetPropertyValue(x, sortBy)).ToList();
        else
            items = items.OrderByDescending(x => GetPropertyValue(x, sortBy)).ToList();

        ViewModel.ItemPropertiesList.Clear();
        ViewModel.ItemPropertiesList.AddRange(items);
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

    private async void OnNewVersionClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !ViewModel.IsSecret)
            return;

        try
        {
            var currentItem = ViewModel.ItemPropertiesList.OrderByDescending(x => x.CreatedOn).First();
            var newVersion = new SecretProperties(currentItem.Id)
            {
                Enabled = true,
                ContentType = currentItem.ContentType,
            };

            foreach (var tag in currentItem.Tags)
                newVersion.Tags.Add(tag.Key, tag.Value);

            var vm = new NewVersionViewModel
            {
                ItemPropertiesModel = KeyVaultItemProperties.FromSecretProperties(newVersion)
            };

            try
            {
                if (ViewModel.OpenedItem.Enabled)
                    vm.SecretValue = (await ViewModel.VaultService.GetSecret(keyVaultUri: ViewModel.OpenedItem.VaultUri, name: ViewModel.OpenedItem.Name)).Value;
            }
            catch (KeyVaultInsufficientPrivilegesException ex)
            {
                WeakReferenceMessenger.Default.Send(message: new ShowValidationErrorMessage(ex.Message), ViewModel.MessengerToken);
            }

            var dialog = new NewVersionDialog
            {
                Title = "New Version",
                XamlRoot = XamlRoot,
                MinHeight = 500,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = "Create",
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonCommand = vm.NewSecretVersionCommand,
                CloseButtonText = "Cancel",
                DataContext = vm,
                RequestedTheme = this.ActualTheme
            };

            await dialog.ShowAsync();
        }
        catch (KeyVaultItemNotFoundException)
        {
        }
    }

    private async void OnEditVersionClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;

        try
        {
            if (ViewModel.IsSecret)
            {
                var currentItem = ViewModel.ItemPropertiesList.OrderByDescending(x => x.CreatedOn).First();
                var vm = new NewVersionViewModel
                {
                    ItemPropertiesModel = currentItem,
                    IsEdit = true
                };

                var dialog = new NewVersionDialog
                {
                    Title = "Edit " + (ViewModel.IsKey ? "Key" : ViewModel.IsSecret ? "Secret" : "Certificate"),
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = "Apply Changes",
                    DefaultButton = ContentDialogButton.Primary,
                    PrimaryButtonCommand = vm.SaveSecretDetailsChangesCommand,
                    CloseButtonText = "Cancel",
                    XamlRoot = XamlRoot,
                    DataContext = vm,
                    RequestedTheme = this.ActualTheme
                };

                await dialog.ShowAsync();
            }
        }
        catch (KeyVaultItemNotFoundException)
        {
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            _ = await ViewModel.Navigator.ShowMessageDialogAsync<string>(this,
                title: "Insufficient Rights",
                content: ex.Message.ToString(),
                buttons: [new DialogAction("Dismiss")]
            );
        }
    }
}
