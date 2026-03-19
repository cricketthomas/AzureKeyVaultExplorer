using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Azure.Security.KeyVault.Secrets;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using KeyVaultExplorer.Exceptions;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#nullable disable

namespace KeyVaultExplorer.Views.Pages;

public partial class VaultPage : UserControl
{
    private const string DatGridElementName = "VaultContentDataGrid";
    private readonly NotificationViewModel _notificationViewModel;

    public VaultPage()
    {
        InitializeComponent();
        var model = new VaultPageViewModel();
        DataContext = model;
        ValuesDataGrid = this.FindControl<DataGrid>(DatGridElementName);
        ValuesDataGrid.ContextRequested += OnDataGridRowContextRequested;
        KeyUp += Control_KeyUp;
        TabHost.SelectionChanged += TabHostSelectionChanged;
        TabHostSelectionChanged(KeyVaultItemType.Secret, null);
        _notificationViewModel = Defaults.Locator.GetRequiredService<NotificationViewModel>();
    }

    public VaultPage(Uri kvUri)
    {
        InitializeComponent();
        var model = new VaultPageViewModel();
        DataContext = model;
        model.VaultUri = kvUri;
        ValuesDataGrid = this.FindControl<DataGrid>(DatGridElementName);
        ValuesDataGrid.ContextRequested += OnDataGridRowContextRequested;
        KeyUp += Control_KeyUp;
        TabHost.SelectionChanged += TabHostSelectionChanged;
        TabHostSelectionChanged(KeyVaultItemType.Secret, null);
        _notificationViewModel = Defaults.Locator.GetRequiredService<NotificationViewModel>();
    }

    private DataGrid ValuesDataGrid { get; set; }

    protected void DataGrid_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await (DataContext as VaultPageViewModel).CopyCommand.ExecuteAsync((KeyVaultContentsAmalgamation)e.Item);
        });
    }

    private void Control_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.F && (e.KeyModifiers == KeyModifiers.Control || e.Key == Avalonia.Input.Key.LWin || e.Key == Avalonia.Input.Key.RWin))
        {
            SearchTextBox.Focus();
            e.Handled = true;
        }
        if (e.Key == Avalonia.Input.Key.F5)
        {
            RefreshButton_OnClick(sender, e);
        }
    }

    private void OnDataGridRowContextRequested(object sender, ContextRequestedEventArgs e)
    {
        var dg = sender as DataGrid;
        var hideCopyCmd = false;
        //if (dg.SelectedItem is not null && (dg.SelectedItem as KeyVaultContentsAmalgamation).Type == KeyVaultItemType.Certificate)
        if (dg.SelectedItem is KeyVaultContentsAmalgamation { Type: KeyVaultItemType.Certificate })
        {
            hideCopyCmd = true;
        }
        ShowMenu(isTransient: true, hideCopyCommand: hideCopyCmd);
        e.Handled = true;
    }

    private void OnDoubleTapped(object sender, TappedEventArgs e)
    {
        // hack to prevent double click on column header from opening the properties flyout
        var control = (e.Source as Control);
        if (control.Name is not null && control.Name.EndsWith("PART_ColumnHeaderRoot"))
        {
            e.Handled = true;
            return;
        }
        var dg = (DataGrid)sender;
        var model = dg.SelectedItem as KeyVaultContentsAmalgamation;
        (DataContext as VaultPageViewModel).ShowPropertiesCommand.Execute(model);
    }

    public void RefreshButton_OnClick(object sender, RoutedEventArgs args)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            await (DataContext as VaultPageViewModel).RefreshCommand.ExecuteAsync(null);
            if (TabHost.SelectedIndex > 2)
            {
                ValuesDataGrid.ItemsSource = new DataGridCollectionView(ValuesDataGrid.ItemsSource)
                {
                    GroupDescriptions = { new DataGridPathGroupDescription("Type") }
                };
            }
        }, DispatcherPriority.Send);
    }

    private void SearchBoxChanges(object sender, TextChangedEventArgs e)
    {
        if (TabHost.SelectedIndex > 2)
        {
            ValuesDataGrid.ItemsSource = new DataGridCollectionView(ValuesDataGrid.ItemsSource)
            {
                GroupDescriptions = { new DataGridPathGroupDescription("Type") }
            };
        }
    }

    // We rely on code behind to show the flyout
    // Listen for the ContextRequested event so we can change the launch behavior based on whether it was a
    // left or right click.
    private void ShowMenu(bool isTransient, bool hideCopyCommand)
    {
        var flyout = Resources["FAMenuFlyout"] as FAMenuFlyout;

        // hide the copy value command for the certificate option.
        MenuFlyoutItem copyMenuItemOption = (MenuFlyoutItem)flyout.Items.ElementAt(0);
        copyMenuItemOption.IsVisible = !hideCopyCommand;

        flyout.ShowMode = isTransient ? FlyoutShowMode.Transient : FlyoutShowMode.Standard;
        flyout.ShowAt(this.FindControl<DataGrid>(DatGridElementName));
    }

    private async void TabHostSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = (DataContext as VaultPageViewModel);
        if (vm.VaultUri is null) return;
        var item = TabHost.SelectedIndex switch
        {
            0 => KeyVaultItemType.Secret,
            1 => KeyVaultItemType.Certificate,
            2 => KeyVaultItemType.Key,
            _ => KeyVaultItemType.All
        };
        await vm.FilterAndLoadVaultValueType(item);

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (item == KeyVaultItemType.All)
            {
                ValuesDataGrid.ItemsSource = new DataGridCollectionView(ValuesDataGrid.ItemsSource)
                {
                    GroupDescriptions = { new DataGridPathGroupDescription("Type") }
                };
            }
        }, DispatcherPriority.Input);
    }

    private void CreateSecret_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(async () => await CreateNewSecret());
    }

    private async Task CreateNewSecret()
    {
        var lifetime = App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        var vm = new CreateNewSecretVersionViewModel()
        {
            KeyVaultSecretModel = new SecretProperties("new_secret") { Enabled = true },
            VaultUri = (DataContext as VaultPageViewModel).VaultUri,
            IsEdit = false,
            IsNew = true,
        };

        var createSecretBtn = new TaskDialogButton("Create Secret", "CreateSecretButtonResult")
        {
            IsDefault = true,
        };

        createSecretBtn.Bind(TaskDialogButton.IsEnabledProperty, new Binding
        {
            Path = "!HasErrors",
            Mode = BindingMode.OneWay,
            FallbackValue = false,
            Source = vm,
        });

        var dialog = new TaskDialog()
        {
            Title = "Create New Secret",
            XamlRoot = lifetime?.Windows.Last() as AppWindow,
            Buttons = { createSecretBtn, TaskDialogButton.CancelButton, },
            MinWidth = 600,
            MinHeight = 650,
            Content = new CreateNewSecretVersion() { DataContext = vm, },
        };

        createSecretBtn.Click += async (sender, args) =>
        {
            try
            {
                await vm.NewVersionCommand.ExecuteAsync(null);
                _notificationViewModel.AddMessage(new Avalonia.Controls.Notifications.Notification("Created", "Your secret has been created.", Avalonia.Controls.Notifications.NotificationType.Success));
            }
            catch (KeyVaultInsufficientPrivilegesException ex)
            {
                _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Insufficient Privileges" });
            }
            catch (Exception ex)
            {
                _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Error" });
            }
        };

        ;
        var result = await dialog.ShowAsync();
    }
}