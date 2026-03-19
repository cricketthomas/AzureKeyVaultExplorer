using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyVaultExplorer.Database;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KeyVaultExplorer.ViewModels;

public partial class SubscriptionsPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string continuationToken;

    [ObservableProperty]
    private bool isBusy = true;

    [ObservableProperty]
    private ObservableCollection<SubscriptionDataItem> subscriptions;

    private readonly KvExplorerDb _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly VaultService _vaultService;
    private readonly AuthService _authService;
    private NotificationViewModel _notificationViewModel;

    public SubscriptionsPageViewModel()
    {
        _vaultService = Defaults.Locator.GetRequiredService<VaultService>();
        _dbContext = Defaults.Locator.GetRequiredService<KvExplorerDb>();
        _memoryCache = Defaults.Locator.GetRequiredService<IMemoryCache>();
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        _notificationViewModel = Defaults.Locator.GetRequiredService<NotificationViewModel>();
        Subscriptions = [];
    }

    [RelayCommand]
    public async Task GetSubscriptions()
    {

        int count = 0;
        Subscriptions.Clear();

        var savedSubscriptions = (await _dbContext.GetStoredSubscriptions(_authService.TenantId ?? null)).ToDictionary(s => s.SubscriptionId);

        await foreach (var item in _vaultService.GetAllSubscriptions())
        {
            Subscriptions.Add(new SubscriptionDataItem
            {
                Data = item.SubscriptionResource.Data,
                IsPinned = savedSubscriptions.GetValueOrDefault(item.SubscriptionResource.Data.SubscriptionId)?.SubscriptionId is not null
            });
            count++;
            if (item.ContinuationToken != null && count > 150)
            {
                ContinuationToken = item.ContinuationToken;
                Debug.WriteLine(item.ContinuationToken);
                break;
            }
        }
        IsBusy = false;
    }

    [RelayCommand]
    public async Task LoadMore()
    {
        int count = 0;
        var savedSubscriptions = (await _dbContext.GetStoredSubscriptions(_authService.TenantId ?? null)).ToDictionary(s => s.SubscriptionId);

        await foreach (var item in _vaultService.GetAllSubscriptions(continuationToken: ContinuationToken))
        {
            Subscriptions.Add(new SubscriptionDataItem
            {
                Data = item.SubscriptionResource.Data,
                IsPinned = savedSubscriptions.GetValueOrDefault(item.SubscriptionResource.Data.SubscriptionId)?.SubscriptionId is not null
            });
            count++;
            if (item.ContinuationToken != null && count > 150)
            {
                ContinuationToken = item.ContinuationToken;
                Debug.WriteLine(item.ContinuationToken);
                break;
            }
        }
        IsBusy = false;
    }

    [RelayCommand]
    public void SelectAllSubscriptions()
    {
        foreach (var item in Subscriptions)
            item.IsPinned = true;
        Subscriptions = new ObservableCollection<SubscriptionDataItem>(Subscriptions);
    }

    [RelayCommand]
    public void ClearSelectedSubscriptions()
    {
        foreach (var item in Subscriptions)
            item.IsPinned = false;
        Subscriptions = new ObservableCollection<SubscriptionDataItem>(Subscriptions);
    }

    [RelayCommand]
    public async Task SaveSelectedSubscriptions()
    {
        var updatedItems = Subscriptions.Where(i => i.IsUpdated is not null || i.IsUpdated == true);

        var added = updatedItems.Where(i => i.IsPinned).Select(s => new Subscriptions
        {
            DisplayName = s.Data.DisplayName,
            SubscriptionId = s.Data.SubscriptionId,
            TenantId = s.Data.TenantId ?? Guid.Empty,
        });

        var removed = updatedItems.Where(i => !i.IsPinned).Select(s => s.Data.SubscriptionId);

        await _dbContext.InsertSubscriptions(added.ToList());
        await _dbContext.RemoveSubscriptionsBySubscriptionIDs(removed);
        _memoryCache.Remove($"subscriptions_{_authService.TenantId}");
        _notificationViewModel.AddMessage(new Avalonia.Controls.Notifications.Notification("Saved", "Your changes have been saved.", Avalonia.Controls.Notifications.NotificationType.Information));

        foreach (var item in Subscriptions)
            item.IsUpdated = false;
    
    }
}