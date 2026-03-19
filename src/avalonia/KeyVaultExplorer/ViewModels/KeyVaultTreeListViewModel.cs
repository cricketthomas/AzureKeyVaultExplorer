using Avalonia.Threading;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyVaultExplorer.Database;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static KeyVaultExplorer.Services.VaultService;

namespace KeyVaultExplorer.ViewModels;

public partial class KeyVaultTreeListViewModel : ViewModelBase
{
    public  ObservableCollection<KvSubscriptionModel> _treeViewList = [];

    [ObservableProperty]
    private bool isBusy = false;

    [ObservableProperty]
    private string searchQuery;

    [ObservableProperty]
    private KeyVaultResource selectedTreeItem;

    [ObservableProperty]
    private ObservableCollection<KvSubscriptionModel> treeViewList = [];

    private readonly AuthService _authService;
    private readonly KvExplorerDb _dbContext;
    private readonly VaultService _vaultService;
    private readonly NotificationViewModel _notificationViewModel;
    private readonly ClipboardService _clipboardService;
    private readonly string[] WatchedNameOfProps = [nameof(KvSubscriptionModel.IsExpanded), nameof(KvSubscriptionModel.IsSelected)];

    public KeyVaultTreeListViewModel()
    {
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        _vaultService = Defaults.Locator.GetRequiredService<VaultService>();
        _dbContext = Defaults.Locator.GetRequiredService<KvExplorerDb>();
        _notificationViewModel = Defaults.Locator.GetRequiredService<NotificationViewModel>();
        _clipboardService = Defaults.Locator.GetRequiredService<ClipboardService>();
        // PropertyChanged += OnMyViewModelPropertyChanged;

        //foreach (var item in TreeViewList)
        //{
        //    item.PropertyChanged += KvSubscriptionModel_PropertyChanged;
        //}
        // Handle CollectionChanged to attach/detach event handlers for new items
        TreeViewList.CollectionChanged += TreeViewList_CollectionChanged;
        //Dispatcher.UIThread.Post(() => GetAvailableKeyVaults(), DispatcherPriority.Default);
    }

    [RelayCommand]
    public void CollapseAll()
    {
        foreach (KvSubscriptionModel item in TreeViewList)
        {
            item.IsExpanded = false;
        }
    }

    [RelayCommand]
    public async Task GetAvailableKeyVaults(bool isRefresh = false)
    {
        if (isRefresh)
        {
            _treeViewList.Clear();
        }

        await Task.Run(async () =>
        {
            await DelaySetIsBusy(async () =>
            {
                //TODO: get all saved items, otherwise get the first item only.
                var resource = _vaultService.GetKeyVaultResourceBySubscription();
                try
                {
                    await foreach (var item in resource)
                    {
                        item.PropertyChanged += KvSubscriptionModel_PropertyChanged;
                        item.HasSubNodeDataBeenFetched = false;
                        _treeViewList.Add(item);
                    }

                    //pinned items, insert the item so it appears instantly, then replace it once it finishes process items from KV
                    var quickAccess = new KvSubscriptionModel
                    {
                        SubscriptionDisplayName = "Quick Access",
                        SubscriptionId = "",
                        IsExpanded = true,
                        ResourceGroups = [new KvResourceGroupModel { ResourceGroupDisplayName = string.Empty }],
                    };

                    _treeViewList.Insert(0, quickAccess);

                    var savedItems = _dbContext.GetQuickAccessItemsAsyncEnumerable(_authService.TenantId ?? null);
                    var token = new CustomTokenCredential(await _authService.GetAzureArmTokenSilent());
                    var armClient = new ArmClient(token);
                    await foreach (var item in savedItems)
                    {
                        var kvr = armClient.GetKeyVaultResource(new ResourceIdentifier(item.KeyVaultId));
                        var kvrResponse = await kvr.GetAsync();
                        //TODO: figure out why i can only have one or the other
                        quickAccess.ResourceGroups[0].KeyVaultResources.Add(kvrResponse);
                        quickAccess.PropertyChanged += KvSubscriptionModel_PropertyChanged;
                    }
                    quickAccess.ResourceGroups[0].KeyVaultResources = SortService.SortKeyVaults(quickAccess.ResourceGroups[0].KeyVaultResources);
                    
                    quickAccess.ResourceGroups[0].ResourceGroupDisplayName = "Pinned";
                    quickAccess.ResourceGroups[0].IsExpanded = true;

                    _treeViewList[0] = quickAccess;

                    foreach (var sub in _treeViewList)
                    {
                        sub.ResourceGroups.CollectionChanged += TreeViewSubNode_CollectionChanged;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                    Dispatcher.UIThread.Post(() => _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Error" }), DispatcherPriority.Background);
                }
            });
        });

        var searched = await Task.Run(() =>
        {
            return new ObservableCollection<KvSubscriptionModel>(_treeViewList);
            return SortService.SortSubscriptions(_treeViewList);
        });
        Dispatcher.UIThread.Post(() => {
            TreeViewList = searched;
            SearchQuery = string.Empty;
        }, DispatcherPriority.Background);
    }

    // this will set isBusy to true if the fetching takes longer than 1500 ms.
    private async Task DelaySetIsBusy(Func<Task> longRunningTaskFactory)
    {
        IsBusy = false;
        using var cts = new CancellationTokenSource();

        var delayTask = Task.Delay(1500, cts.Token);
        var longRunningTask = longRunningTaskFactory();

        var completedTask = await Task.WhenAny(longRunningTask, delayTask);

        if (completedTask == delayTask)
        {
            // The long-running task took longer than 1500 milliseconds
            IsBusy = true;
            await longRunningTask;
        }
        else
        {
            // The long-running task completed before the delay
            await completedTask;
        }

        IsBusy = false;
    }

    [RelayCommand]
    public async Task PinVaultToQuickAccess(KeyVaultResource model)
    {
        var exists = await _dbContext.QuickAccessItemByKeyVaultIdExists(model.Id);
        if (exists) return;
        var qa = new QuickAccess
        {
            KeyVaultId = model.Id,
            Name = model.Data.Name,
            VaultUri = model.Data.Properties.VaultUri.ToString(),
            TenantId = model.Data.Properties.TenantId.ToString(),
            Location = model.Data.Location.Name,
            //SubscriptionDisplayName = model.Data.s
        };

        await _dbContext.InsertQuickAccessItemAsync(qa);

        TreeViewList[0].ResourceGroups[0].KeyVaultResources.Add(model);
        
        // Sort the quick access key vaults alphabetically
        TreeViewList[0].ResourceGroups[0].KeyVaultResources = SortService.SortKeyVaults(TreeViewList[0].ResourceGroups[0].KeyVaultResources);
        
        var quickAccess = new KvSubscriptionModel
        {
            SubscriptionDisplayName = "Quick Access",
            IsExpanded = true,
            SubscriptionId = "",
            ResourceGroups = TreeViewList[0].ResourceGroups,
            Subscription = null,
        };
        quickAccess.ResourceGroups[0].ResourceGroupDisplayName = "Pinned";
        quickAccess.ResourceGroups[0].IsExpanded = true;
        TreeViewList[0] = quickAccess;
    }

    [RelayCommand]
    public async Task RemovePinVaultToQuickAccess(KeyVaultResource model)
    {
        var exists = await _dbContext.QuickAccessItemByKeyVaultIdExists(model.Id);
        if (!exists) return;

        await _dbContext.DeleteQuickAccessItemByKeyVaultId(model.Id);

        var rg = TreeViewList[0].ResourceGroups;
        var items = new ObservableCollection<KeyVaultResource>(TreeViewList[0].ResourceGroups[0].KeyVaultResources.Where(s => s.Data.Id != model.Id));
        rg[0].KeyVaultResources = items;
        rg[0].ResourceGroupDisplayName = "Pinned";

        var quickAccess = new KvSubscriptionModel
        {
            SubscriptionDisplayName = "Quick Access",
            IsExpanded = true,
            SubscriptionId = "",
            ResourceGroups = rg,
            Subscription = null,
        };
        TreeViewList[0] = quickAccess;
    }

    private void KvResourceGroupNode_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (WatchedNameOfProps.Contains(e.PropertyName))
        {
            var kvResourceModel = (KvResourceGroupModel)sender;
            // if they are selecting the list item, expand it as a courtesy
            if (e.PropertyName == nameof(KvResourceGroupModel.IsSelected))
                kvResourceModel.IsExpanded = true;
            var hasPlaceholder = kvResourceModel.KeyVaultResources.Any(k => k.GetType().Name == nameof(KeyVaultResourcePlaceholder));
            // if its being expanded and there are no items in the array reach out to azure
            if (kvResourceModel.IsExpanded && hasPlaceholder)
            {
                kvResourceModel.KeyVaultResources.Clear();

                Dispatcher.UIThread.Invoke(async () =>
                {
                    await DelaySetIsBusy(async () =>
                    {
                        var vaults = _vaultService.GetKeyVaultsByResourceGroup(kvResourceModel.ResourceGroupResource);

                        await foreach (var vault in vaults)
                        {
                            kvResourceModel.KeyVaultResources.Add(vault);
                        }
                        
                        // Sort the key vaults alphabetically
                        kvResourceModel.KeyVaultResources = SortService.SortKeyVaults(kvResourceModel.KeyVaultResources);
                    });
                }, DispatcherPriority.ContextIdle);
            }
        }
    }

    private void KvSubscriptionModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (WatchedNameOfProps.Contains(e.PropertyName))
        {
            var kvSubModel = (KvSubscriptionModel)sender;
            if (string.IsNullOrWhiteSpace(kvSubModel.SubscriptionId))
                return;

            // if they are selecting the list item, expand it as a courtesy
            if (e.PropertyName == nameof(KvSubscriptionModel.IsSelected))
                kvSubModel.IsExpanded = true;

            bool isExpanded = kvSubModel.IsExpanded;
            var placeholder = new KeyVaultResourcePlaceholder();

            if (isExpanded && !kvSubModel.HasSubNodeDataBeenFetched)
            {
                Dispatcher.UIThread.Invoke(async () =>
                {
                    await DelaySetIsBusy(async () =>
                    {
                        kvSubModel.ResourceGroups.Clear();
                        var resourceGroups = _vaultService.GetResourceGroupBySubscription(kvSubModel);

                        await foreach (var rg in resourceGroups)
                        {
                            kvSubModel.ResourceGroups.Add(
                                new KvResourceGroupModel
                                {
                                    ResourceGroupDisplayName = rg.Data.Name,
                                    ResourceGroupResource = rg,
                                    KeyVaultResources = [placeholder]
                                });
                        }
                        
                        // Sort the resource groups alphabetically
                        kvSubModel.ResourceGroups = SortService.SortResourceGroups(kvSubModel.ResourceGroups);
                        
                        kvSubModel.HasSubNodeDataBeenFetched = true;
                        kvSubModel.ResourceGroups.CollectionChanged += TreeViewSubNode_CollectionChanged;
                    });
                }, DispatcherPriority.ContextIdle);
            }
        }
    }


    partial void OnSearchQueryChanged(string value)
    {
        string query = value.Trim();
        var searched = Task.Run(() =>
         {
             return new ObservableCollection<KvSubscriptionModel>(FilterService.Filter(_treeViewList, query)); ;
         });

        var res = searched.GetAwaiter().GetResult();
        Dispatcher.UIThread.InvokeAsync(() => TreeViewList = res, DispatcherPriority.Background);
    }
    [RelayCommand]
    private void OpenInAzure(KeyVaultResource model)
    {
        if (model is null) return;
        var uri = $"https://portal.azure.com/#@{_authService.TenantName}/resource{model.Id}";
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true, Verb = "open" });
    }

    [RelayCommand]
    private async Task CopyPortalURL(KeyVaultResource model)
    {
        if (model is null) return;
        var uri = $"https://portal.azure.com/#@{_authService.TenantName}/resource{model.Id}";
        await _clipboardService.SetTextAsync(uri);
    }

    private void TreeViewList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

    private void TreeViewSubNode_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (KvResourceGroupModel newItem in e.NewItems)
            {
                newItem.PropertyChanged += KvResourceGroupNode_PropertyChanged;
            }
        }
        //else if (e.Action == NotifyCollectionChangedAction.Remove)
        //{
        //    foreach (KvResourceGroupModel oldItem in e.OldItems)
        //    {
        //        oldItem.PropertyChanged -= KvResourceGroupNode_PropertyChanged;
        //    }
        //}
    }
   
}