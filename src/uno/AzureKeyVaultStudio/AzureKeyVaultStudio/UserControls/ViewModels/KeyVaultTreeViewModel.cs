using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using AzureKeyVaultStudio.Database;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using CommunityToolkit.Mvvm.Messaging;
using Windows.System;

namespace AzureKeyVaultStudio.UserControls.ViewModels;

public partial class KeyVaultTreeViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private IDispatcher _dispatcher;
    private readonly VaultService _vaultService;
    private readonly IStringLocalizer _localizer;
    private readonly string[] WatchedNameOfProps = [nameof(KvSubscriptionModel.IsExpanded), nameof(KvSubscriptionModel.IsSelected)];
    private readonly ConcurrentDictionary<string, byte> _subscriptionsLoadingSubNodes = new();

    public KeyVaultTreeViewModel(AuthService authService, VaultService vaultService, IDispatcher dispatcher, IStringLocalizer localizer)
    {
        _authService = authService;
        _vaultService = vaultService;
        _dispatcher = dispatcher;
        _localizer = localizer;

        TreeDataSource.CollectionChanged += TreeViewList_CollectionChanged;

        var properties = new KeyVaultProperties(new Guid(), new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard));

        AddDocumentCommand = new RelayCommand(() =>
               WeakReferenceMessenger.Default.Send(new AddDocumentMessage(new KeyVaultDataPlaceholder(location: AzureLocation.EastUS2, properties: properties) { })));

        WeakReferenceMessenger.Default.Register<PinKeyVaultMessage>(this, async (r, m) =>
        {
            await PinVaultToQuickAccess(m.KeyVaultResource);
        });
    }

    public RelayCommand AddDocumentCommand { get; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; } = false;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial object SelectedItem { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<KvSubscriptionModel> TreeDataSource { get; set; } = [];

    private ImmutableList<KvSubscriptionModel> TreeDataSourceReadOnly { get; set; } = [];

    [RelayCommand]
    public void CollapseAll() => _ = Task.Run(() => _dispatcher.TryEnqueue(() => TreeDataSource.ForEach(item => item.IsExpanded = false)));

    [RelayCommand]
    public void ExpandAll() => _ = Task.Run(() => _dispatcher.TryEnqueue(() => TreeDataSource.ForEach(item => item.IsExpanded = true)));

    [RelayCommand(FlowExceptionsToTaskScheduler = true)]
    public async Task PinVaultToQuickAccess(KeyVaultResource model)
    {
        if (model is null)
            return;
        var exists = await DbContext.QuickAccessItemByKeyVaultIdExists(model.Id);
        if (exists) return;
        var qa = new QuickAccess
        {
            KeyVaultId = model.Id,
            Name = model.Data.Name,
            VaultUri = model.Data.Properties.VaultUri.ToString(),
            TenantId = model.Data.Properties.TenantId.ToString(),
            Location = model.Data.Location.Name,
        };

        await DbContext.InsertQuickAccessItemAsync(qa);
        _dispatcher.TryEnqueue(() =>
        {
            var items = new ObservableCollection<KvKeyVaultResourceModel>(TreeDataSource[0].Children.OfType<KvKeyVaultResourceModel>())
            {
                new KvKeyVaultResourceModel
                {
                    DisplayName = model.Data.Name,
                    Resource = model,
                }
            };
            TreeDataSource[0].Children.Clear();
            TreeDataSource[0].Children.AddRange(items);
        });
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private async Task Refresh(CancellationToken token)
    {
#if DEBUG
        //await Task.Delay(4000, token);
#endif
        SearchQuery = string.Empty;
        await ClearAndResetTree();
        await InitializeTreeDataSource(token);
    }

    [RelayCommand]
    private async Task RemovePinVaultToQuickAccess(KeyVaultResource model)
    {
        if (model is null)
            return;

        var exists = await DbContext.QuickAccessItemByKeyVaultIdExists(model.Id);
        if (!exists) return;

        await DbContext.DeleteQuickAccessItemByKeyVaultId(model.Id);

        _dispatcher.TryEnqueue(() =>
        {
            var items = new ObservableCollection<KvKeyVaultResourceModel>(
                TreeDataSource[0].Children
                    .OfType<KvKeyVaultResourceModel>()
                    .Where(s => s.VaultResource?.Data.Id != model.Id));
            TreeDataSource[0].Children.Clear();
            TreeDataSource[0].Children.AddRange(items);
        });
    }

    [RelayCommand]
    private void TriggerAddDocument() => AddDocumentCommand.Execute(null);

    private async Task InitializeTreeDataSource(CancellationToken token)
    {
        IsBusy = true;
        try
        {
            var subscriptionModel = new ObservableCollection<KvSubscriptionModel>();

            var resource = _vaultService.GetKeyVaultResourceBySubscription();
            try
            {
                await foreach (var item in resource)
                {
                    if (item != null)
                    {
                        item.PropertyChanged += KvSubscriptionModel_PropertyChanged;
                        //item.PropertyChanging += Item_PropertyChanging; ;
                        item.HasSubNodeDataBeenFetched = false;
                        subscriptionModel.Add(item);
                    }
                }

                //pinned items, insert the item so it appears instantly, then replace it once it finishes process items from KV
                var quickAccess = new KvSubscriptionModel
                {
                    IsExpanded = true,
                    DisplayName = _localizer["QuickAccessText"] ?? "Quick Access",
                    Type = KvSubscriptionModel.ExplorerItemType.QuickAccess
                };

                var savedItems = DbContext.GetQuickAccessItemsAsyncEnumerable(_authService.TenantId ?? null);
                var tokenString = await _authService.GetAzureArmTokenSilent();
                var tokenCredential = new CustomTokenCredential(tokenString);
                var armClient = new ArmClient(tokenCredential);

                await foreach (var item in savedItems)
                {
                    if (item?.KeyVaultId != null)
                    {
                        try
                        {
                            var kvr = armClient.GetKeyVaultResource(new ResourceIdentifier(item.KeyVaultId));
                            var kvrResponse = await kvr.GetAsync();
                            quickAccess.Children.Add(new KvKeyVaultResourceModel
                            {
                                DisplayName = kvrResponse.Value.Data.Name,
                                Resource = kvrResponse.Value,
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error loading quick access item {item.KeyVaultId}: {ex.Message}");
                        }
                    }
                }

                subscriptionModel.Insert(0, quickAccess);

                foreach (var sub in subscriptionModel)
                {
                    if (sub is not null)
                    {
                        sub.Children.CollectionChanged += TreeViewSubNode_CollectionChanged;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeTreeDataSource: {ex}");
            }

            if (_dispatcher != null)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    SelectedItem = null;
                    TreeDataSource = [];
                    TreeDataSource = new ObservableCollection<KvSubscriptionModel>(subscriptionModel);
                });
            }

            TreeDataSourceReadOnly = [.. subscriptionModel];
        }
        finally
        {
            IsBusy = false;
        }
    }

    //private void Item_PropertyChanging(object? sender, PropertyChangingEventArgs e)
    //{
    //    throw new NotImplementedException();
    //}
    private async void KvResourceGroupNode_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (WatchedNameOfProps.Contains(e.PropertyName) && sender is not null)
        {
            var kvResourceModel = (KvResourceGroupModel)sender;
            // if they are selecting the list item, expand it as a courtesy
            if (e.PropertyName == nameof(KvResourceGroupModel.IsSelected))
                kvResourceModel.IsExpanded = true;

            await LoadResourceGroupVaults(kvResourceModel);
        }
    }

    private async void KvSubscriptionModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (WatchedNameOfProps.Contains(e.PropertyName) && sender is not null)
        {
            var kvSubModel = (KvSubscriptionModel)sender;
            if (string.IsNullOrWhiteSpace(kvSubModel.SubscriptionId))
                return;

            var subscriptionId = kvSubModel.SubscriptionId;

            // if they are selecting the list item, expand it as a courtesy
            if (e.PropertyName == nameof(KvSubscriptionModel.IsSelected))
                kvSubModel.IsExpanded = true;

            bool isExpanded = kvSubModel.IsExpanded;
            if (isExpanded
                && !kvSubModel.HasSubNodeDataBeenFetched
                && _subscriptionsLoadingSubNodes.TryAdd(subscriptionId, 0))
            {
                try
                {
                    // Remove the first placeholder item that is used to keep the chevron, and also make sure not to raise the event. Not pretty.
                    // Remove the first item if it doesn't have a name
                    var hasPlaceholder = kvSubModel.Children.Any()
                        && kvSubModel.Children[0] is KvResourceGroupModel firstResourceGroup
                        && firstResourceGroup.Children.OfType<KvKeyVaultResourceModel>().Any(k => k.IsPlaceholder);

                    if (hasPlaceholder)
                    {
                        kvSubModel.Children.CollectionChanged -= TreeViewSubNode_CollectionChanged;
                        kvSubModel.Children.RemoveAt(0);
                        kvSubModel.Children.CollectionChanged += TreeViewSubNode_CollectionChanged;
                    }

                    await Task.Run(async () =>
                    {
                        var resourceGroups = _vaultService.GetResourceGroupBySubscription(kvSubModel);
                        var rgList = new List<KvResourceGroupModel>();

                        await foreach (var rg in resourceGroups)
                        {
                            rgList.Add(new KvResourceGroupModel
                            {
                                DisplayName = rg.Data.Name,
                                ResourceGroupResource = rg,
                                Children = { KvKeyVaultResourceModel.CreatePlaceholder() },
                            });
                        }

                        _dispatcher.TryEnqueue(() =>
                        {
                            foreach (var rgModel in rgList.OrderBy(x => x.DisplayName))
                            {
                                kvSubModel.Children.Add(rgModel);
                            }

                            kvSubModel.HasSubNodeDataBeenFetched = true;
                            kvSubModel.Children.CollectionChanged += TreeViewSubNode_CollectionChanged;
                            kvSubModel.IsExpanded = true;
                            _subscriptionsLoadingSubNodes.TryRemove(subscriptionId, out _);
                        });
                    });
                }
                catch
                {
                    _subscriptionsLoadingSubNodes.TryRemove(subscriptionId, out _);
                    throw;
                }
            }
        }
    }

    [RelayCommand(IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private Task ExecuteSearch(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            _dispatcher.TryEnqueue(() =>
            {
                SelectedItem = null;
                TreeDataSource = [];
                TreeDataSource = new ObservableCollection<KvSubscriptionModel>(TreeDataSourceReadOnly);
            });
            return Task.CompletedTask;
        }

        var source = TreeDataSourceReadOnly.Count > 0
            ? TreeDataSourceReadOnly
            : [.. TreeDataSource];

        var results = FilterService.Filter(source, SearchQuery);
        _dispatcher.TryEnqueue(() =>
        {
            SelectedItem = null;
            TreeDataSource = new ObservableCollection<KvSubscriptionModel>(results);
        });

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenInAzure(KeyVaultResource model)
    {
        if (model is null) return;
        var uri = new Uri($"https://portal.azure.com/#@{_authService.TenantName}/resource{model.Id}");
        await Launcher.LaunchUriAsync(uri);
    }

    [RelayCommand]
    private void OpenInNewTab(KeyVaultResource model)
    {
        if (model is KeyVaultResource)
        {
            WeakReferenceMessenger.Default.Send(new AddDocumentMessage(model.Data));
        }
    }

    private void TreeViewList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (KvSubscriptionModel newItem in e.NewItems)
            {
                newItem.PropertyChanged += KvSubscriptionModel_PropertyChanged;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (KvSubscriptionModel oldItem in e.OldItems)
            {
                oldItem.PropertyChanged -= KvSubscriptionModel_PropertyChanged;
            }
        }
    }

    private void TreeViewSubNode_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && sender is not null)
        {
            foreach (var newItem in e.NewItems.OfType<KvResourceGroupModel>())
            {
                newItem.PropertyChanged += KvResourceGroupNode_PropertyChanged;

                if (newItem.IsExpanded)
                    _ = LoadResourceGroupVaults(newItem);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (var oldItem in e.OldItems.OfType<KvResourceGroupModel>())
            {
                oldItem.PropertyChanged -= KvResourceGroupNode_PropertyChanged;
            }
        }
    }

    internal void SetDispatcher(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    private Task ClearAndResetTree()
    {
        if (_dispatcher is null)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _dispatcher.TryEnqueue(() =>
        {
            SearchQuery = "";
            SelectedItem = null;
            TreeDataSource = [];
            TreeDataSourceReadOnly = [];
            tcs.TrySetResult();
        });

        return tcs.Task;
    }

    partial void OnTreeDataSourceChanging(ObservableCollection<KvSubscriptionModel>? oldValue, ObservableCollection<KvSubscriptionModel>? newValue)
    {
        if (oldValue is null)
            return;

        oldValue.CollectionChanged -= TreeViewList_CollectionChanged;

        foreach (var sub in oldValue)
        {
            sub.PropertyChanged -= KvSubscriptionModel_PropertyChanged;
            sub.Children.CollectionChanged -= TreeViewSubNode_CollectionChanged;

            foreach (var rg in sub.Children.OfType<KvResourceGroupModel>())
            {
                rg.PropertyChanged -= KvResourceGroupNode_PropertyChanged;
            }
        }
    }

    partial void OnTreeDataSourceChanged(ObservableCollection<KvSubscriptionModel>? value)
    {
        if (value is null)
            return;

        value.CollectionChanged -= TreeViewList_CollectionChanged;
        value.CollectionChanged += TreeViewList_CollectionChanged;

        foreach (var sub in value)
        {
            sub.PropertyChanged -= KvSubscriptionModel_PropertyChanged;
            sub.PropertyChanged += KvSubscriptionModel_PropertyChanged;

            sub.Children.CollectionChanged -= TreeViewSubNode_CollectionChanged;
            sub.Children.CollectionChanged += TreeViewSubNode_CollectionChanged;

            foreach (var rg in sub.Children.OfType<KvResourceGroupModel>())
            {
                rg.PropertyChanged -= KvResourceGroupNode_PropertyChanged;
                rg.PropertyChanged += KvResourceGroupNode_PropertyChanged;

                if (rg.IsExpanded)
                    _ = LoadResourceGroupVaults(rg);
            }
        }
    }

    private async Task LoadResourceGroupVaults(KvResourceGroupModel kvResourceModel)
    {
        var hasPlaceholder = kvResourceModel.Children.OfType<KvKeyVaultResourceModel>().Any(k => k.IsPlaceholder);
        if (!kvResourceModel.IsExpanded || !hasPlaceholder)
            return;

        kvResourceModel.Children.Clear();

        try
        {
            await Task.Run(async () =>
            {
                var vaults = _vaultService.GetKeyVaultsByResourceGroup(kvResourceModel.ResourceGroupResource);
                var vaultsList = new List<KeyVaultResource>();

                await foreach (var vault in vaults)
                {
                    vaultsList.Add(vault);
                }

                _dispatcher.TryEnqueue(() =>
                {
                    foreach (var vault in vaultsList)
                    {
                        kvResourceModel.Children.Add(new KvKeyVaultResourceModel
                        {
                            DisplayName = vault.Data.Name,
                            Resource = vault,
                        });
                    }
                });
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading vaults for resource group {kvResourceModel.DisplayName}: {ex.Message}");
        }
    }
}
