﻿using Avalonia.Animation.Easings;
using Avalonia.Threading;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kvexplorer.shared;
using kvexplorer.shared.Database;
using kvexplorer.shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static kvexplorer.shared.VaultService;

namespace avalonia.kvexplorer.ViewModels;

public partial class KeyVaultTreeListViewModel : ViewModelBase
{
    public IEnumerable<KeyVaultModel> _treeViewList;

    [ObservableProperty]
    public string searchQuery;

    [ObservableProperty]
    public KeyVaultResource selectedTreeItem;

    [ObservableProperty]
    public ObservableCollection<KeyVaultModel> treeViewList;


    [ObservableProperty]
    public bool isBusy;

    private readonly AuthService _authService;
    private readonly VaultService _vaultService;
    private readonly KvExplorerDbContext _kvDbContext;

    private readonly string[] WatchedNameOfProps = { nameof(KeyVaultModel.IsExpanded), nameof(KeyVaultModel.IsSelected) };
    private bool AttemptedLogin = false;

    

    public KeyVaultTreeListViewModel()
    {
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        _vaultService = Defaults.Locator.GetRequiredService<VaultService>();
        _kvDbContext = Defaults.Locator.GetRequiredService<KvExplorerDbContext>();

        // PropertyChanged += OnMyViewModelPropertyChanged;

        TreeViewList = new ObservableCollection<KeyVaultModel>
        {
            // new KeyVaultModel
            //{
            //    SubscriptionDisplayName = "Quick Access",
            //    SubscriptionId = "123",
            //    KeyVaultResources = new List<KeyVaultResource>{ },
            //    Subscription = null,
            //    GlyphIcon = "Pin"
            //}, new KeyVaultModel
            //{
            //    SubscriptionDisplayName = "2 Subscription",
            //    SubscriptionId = "123",
            //    KeyVaultResources = new List<KeyVaultResource>{ },
            //    Subscription = null
            //}, new KeyVaultModel
            //{
            //    SubscriptionDisplayName = "3 Subscription",
            //    SubscriptionId = "123",
            //    KeyVaultResources = new List<KeyVaultResource>{ },
            //    Subscription = null
            //},
        };

        //foreach (var item in TreeViewList)
        //{
        //    item.PropertyChanged += KeyVaultModel_PropertyChanged;
        //}
        // Handle CollectionChanged to attach/detach event handlers for new items
        TreeViewList.CollectionChanged += TreeViewList_CollectionChanged;
        //Dispatcher.UIThread.Post(() => GetAvailableKeyVaults(), DispatcherPriority.Default);
    }
    //var kvp = new Azure.ResourceManager.KeyVault.Models.KeyVaultProperties(Guid.Parse(item.TenantId), new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard));
    //kvp.VaultUri = new Uri(item.VaultUri);
    //var kvd = new KeyVaultData(Azure.Core.AzureLocation.EastUS2, kvp)
    //{
    //    Name = item.Name,
    //};
    //var kvr = new KeyVaultResource(armClient, kvd);
    [RelayCommand]
    public async Task GetAvailableKeyVaults(bool isRefresh = false)
    {
        if (isRefresh)
        {
            TreeViewList.Clear();
        }

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // all items 
            var resource = _vaultService.GetKeyVaultResourceBySubscriptionAndResourceGroup();
            await foreach (var item in resource)
            {
                item.PropertyChanged += KeyVaultModel_PropertyChanged;
                TreeViewList.Add(item);
            }

            //pinned items, insert the item so it appears instantly, then replace it once it finishes process items from KV
            var quickAccess = new KeyVaultModel { SubscriptionDisplayName = "Quick Access", SubscriptionId = "", KeyVaultResources = new List<KeyVaultResource> { }, Subscription = null, GlyphIcon = "Pin" };
            TreeViewList.Insert(0, quickAccess);

            var savedToQuickAccess = _kvDbContext.QuickAccessItems.AsAsyncEnumerable();
            var token = new CustomTokenCredential(await _authService.GetAzureArmTokenSilent());
            var armClient = new ArmClient(token);
            await foreach (var item in savedToQuickAccess)
            {
                var kvr = armClient.GetKeyVaultResource(new ResourceIdentifier(item.KeyVaultId));
                var kvrResponse = await kvr.GetAsync();
                quickAccess.KeyVaultResources.Add(kvrResponse);
                quickAccess.PropertyChanged += KeyVaultModel_PropertyChanged;
            }
            TreeViewList[0] = quickAccess;

        });

       _treeViewList = TreeViewList;
    }

    [RelayCommand]
    public async Task PinVaultToQuickAccess(KeyVaultResource model)
    {
        var exists = await _kvDbContext.QuickAccessItems.AnyAsync(qa => qa.KeyVaultId == model.Id);
        if (exists) return;
        var qa = new QuickAccess
        {
            KeyVaultId = model.Id,
            Name = model.Data.Name,
            VaultUri = model.Data.Properties.VaultUri.ToString(),
            TenantId = model.Data.Properties.TenantId.ToString(),
            Location = model.Data.Location.Name
            //SubscriptionDisplayName = model.Data.s
        };
        _kvDbContext.QuickAccessItems.Add(qa);
        await _kvDbContext.SaveChangesAsync();

        //await Dispatcher.UIThread.InvokeAsync(async () =>
        //{
        //    var resource = _vaultService.GetKeyVaultResourceBySubscriptionAndResourceGroup();
        //    await foreach (var item in resource)
        //    {
        //        item.PropertyChanged += KeyVaultModel_PropertyChanged;
        //        TreeViewList.Add(item);
        //    }
        //    _treeViewList = TreeViewList;
        //}, DispatcherPriority.Default);
    }

    void KeyVaultModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (WatchedNameOfProps.Contains(e.PropertyName))
        {
            var keyVaultModel = (KeyVaultModel)sender;
            // if they are selecting the list item, expand it as a courtesy
            if (e.PropertyName == nameof(KeyVaultModel.IsSelected))
                keyVaultModel.IsExpanded = true;

            bool isExpanded = keyVaultModel.IsExpanded;
            if (isExpanded && keyVaultModel.KeyVaultResources.Any(k => k.GetType().Name == nameof(KeyVaultResourcePlaceholder)))
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    //_vaultService.UpdateSubscriptionWithKeyVaults(ref keyVaultModel); /* This does not work with AOT */
                    keyVaultModel.KeyVaultResources.Clear();
                    var vaults = _vaultService.GetKeyVaultsBySubscription(keyVaultModel);
                    foreach (var vault in vaults)
                    {
                        keyVaultModel.KeyVaultResources.Add(vault);
                    }
                }, DispatcherPriority.ContextIdle);
            }
        }
    }

    void KeyVaultModel_PropertyRemoved(object sender, PropertyChangedEventArgs e)
    { }

    private async Task Login()
    {
        var cancellation = new CancellationToken();
        var account = await _authService.RefreshTokenAsync(cancellation);
        if (account == null)
            await _authService.LoginAsync(cancellation);
    }

    //private void OnMyViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName == nameof(SelectedTreeItem))
    //    {
    //        // Handle changes to the SelectedTreeItem property here
    //        //OnSelectedTreeItemChanged("test");
    //    }
    //}

    partial void OnSearchQueryChanged(string value)
    {
        string query = value.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(query))
        {
            TreeViewList = new ObservableCollection<KeyVaultModel>(_treeViewList);
        }
        var list = _treeViewList.Where(v => v.SubscriptionDisplayName.ToLowerInvariant().Contains(query));
        TreeViewList = new ObservableCollection<KeyVaultModel>(list);
    }

    private void TreeViewList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (KeyVaultModel newItem in e.NewItems)
            {
                newItem.PropertyChanged += KeyVaultModel_PropertyChanged;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (KeyVaultModel oldItem in e.OldItems)
            {
                oldItem.PropertyChanged -= KeyVaultModel_PropertyRemoved;
            }
        }
    }
}