using System.Collections.ObjectModel;
using System.Diagnostics;
using AzureKeyVaultStudio.Database;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.Extensions.Caching.Memory;

namespace AzureKeyVaultStudio.Presentation;

public partial class SubscriptionViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? ContinuationToken { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; } = false;

    [ObservableProperty]
    public partial ObservableCollection<SubscriptionDataItemModel> Subscriptions { get; set; } = [];

    private readonly IMemoryCache _memoryCache;
    private readonly VaultService _vaultService;
    private readonly AuthService _authService;
    private readonly IDispatcher _dispatcher;
    private readonly IStringLocalizer _localizer;
    private readonly SemaphoreSlim _loadingLock = new SemaphoreSlim(1, 1);

    public SubscriptionViewModel(VaultService vaultService, AuthService authService,
        IMemoryCache memoryCache, IDispatcher dispatcher, IStringLocalizer localizer)
    {
        _vaultService = vaultService;
        _authService = authService;
        _memoryCache = memoryCache;
        _dispatcher = dispatcher;
        _localizer = localizer;
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private async Task LoadSubscriptionAsync(CancellationToken cancellationToken)
    {
        try
        {
            _dispatcher.TryEnqueue(() => IsBusy = true);

            var savedSubscriptions = await Task.Run(async () =>
                (await DbContext.GetStoredSubscriptions(_authService.TenantId ?? null)).ToDictionary(s => s.SubscriptionId),  cancellationToken);

            int count = 0;
            var loadedItems = new List<SubscriptionDataItemModel>();

            await foreach (var item in _vaultService.GetAllSubscriptions(cancellationToken).WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                loadedItems.Add(new SubscriptionDataItemModel
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

            _dispatcher.TryEnqueue(() =>
            {
                Subscriptions.Clear();
                Subscriptions.AddRange(loadedItems);
                IsBusy = false;
            });
        }
        catch (OperationCanceledException)
        {
            _dispatcher.TryEnqueue(() => IsBusy = false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading subscriptions: {ex.Message}");
            _dispatcher.TryEnqueue(() => IsBusy = false);
        }
        finally
        {
            _loadingLock.Release();
        }
    }

    [RelayCommand]
    public async Task LoadMoreSubscriptionsAsync()
    {
        if (string.IsNullOrEmpty(ContinuationToken))
            return;

        int count = 0;
        var loadedItems = new List<SubscriptionDataItemModel>();

        try
        {
            _dispatcher.TryEnqueue(() => IsBusy = true);

            var savedSubscriptions = await Task.Run(async () =>
                (await DbContext.GetStoredSubscriptions(_authService.TenantId ?? null)).ToDictionary(s => s.SubscriptionId));

            await foreach (var item in _vaultService.GetAllSubscriptions(continuationToken: ContinuationToken))
            {
                loadedItems.Add(new SubscriptionDataItemModel
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

            _dispatcher.TryEnqueue(() =>
            {
                foreach (var item in loadedItems)
                {
                    Subscriptions.Add(item);
                }
                IsBusy = false;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading more subscriptions: {ex.Message}");
            _dispatcher.TryEnqueue(() => IsBusy = false);
        }
    }

    [RelayCommand]
    public void SelectAllSubscriptions()
    {
        _dispatcher.ExecuteAsync(() =>
        {
            Subscriptions.ForEach(item => item.IsPinned = true);
        });
    }

    [RelayCommand]
    public void ClearSelectedSubscriptions()
    {
        _dispatcher.ExecuteAsync(() =>
        {
            Subscriptions.ForEach(item => item.IsPinned = false);
        });
    }

    [RelayCommand]
    public async Task SaveSelectedSubscriptions()
    {
        await _loadingLock.WaitAsync();
        try
        {
            var updatedItems = Subscriptions.Where(i => i.IsUpdated == true);

            var added = updatedItems.Where(i => i.IsPinned).Select(s => new Subscriptions
            {
                DisplayName = s.Data.DisplayName,
                SubscriptionId = s.Data.SubscriptionId,
                TenantId = s.Data.TenantId ?? Guid.Empty,
            });

            var removed = updatedItems.Where(i => !i.IsPinned).Select(s => s.Data.SubscriptionId);

            await DbContext.InsertSubscriptions(added);
            await DbContext.RemoveSubscriptionsBySubscriptionIDs(removed);

            _memoryCache.Remove($"subscriptions_{_authService.TenantId}");

            _dispatcher.TryEnqueue(() =>
            {
                WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
                {
                    Severity = InfoBarSeverity.Informational,
                    Message = _localizer["SavedChangesMessage"] ?? "Your changes have been saved.",
                    Title = _localizer["SavedTitle"] ?? "Saved.",
                    Duration = TimeSpan.FromSeconds(5)
                }));
                Subscriptions.ForEach(item => item.IsUpdated = false);
            });
        }
        finally
        {
            _loadingLock.Release();
        }
    }
}
