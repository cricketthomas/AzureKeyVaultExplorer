using System.Collections.ObjectModel;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;

namespace AzureKeyVaultStudio.Services;

public class AzureSearchService
{
    private readonly VaultService _vaultService;

    public AzureSearchService(VaultService vaultService)
    {
        _vaultService = vaultService;
    }

    public async Task<IList<KvSubscriptionModel>> SearchAsync(string query, KvSubscriptionModel? quickAccessNode = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken };
        var subscriptions = await _vaultService.GetSubscriptionsAsync();

        static bool Contains(string? value, string q)
            => !string.IsNullOrEmpty(value) && value.Contains(q, StringComparison.OrdinalIgnoreCase);

        var subMatches = subscriptions
            .Where(s => Contains(s.Data?.DisplayName, query))
            .ToList();

        if (subMatches.Count > 0)
        {
            var subResults = subMatches.Select(s => new KvSubscriptionModel
            {
                DisplayName = s.Data.DisplayName,
                SubscriptionId = s.Data.Id,
                Subscription = s,
                IsExpanded = true,
                ResourceGroups = [new KvResourceGroupModel
                {
                    DisplayName = string.Empty,
                    KeyVaultResources = [new KeyVaultResourcePlaceholder()]
                }]
            }).ToList<KvSubscriptionModel>();

            return PrependQuickAccess(subResults, quickAccessNode, Contains, query);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var rgResults = await SearchByResourceGroupAsync(subscriptions, parallelOptions, Contains, query);
        if (rgResults.Count > 0)
            return PrependQuickAccess(rgResults, quickAccessNode, Contains, query);

        cancellationToken.ThrowIfCancellationRequested();

        var kvResults = await SearchByKeyVaultAsync(subscriptions, parallelOptions, Contains, query);
        return PrependQuickAccess(kvResults, quickAccessNode, Contains, query);
    }

    private static List<KvSubscriptionModel> PrependQuickAccess(
        List<KvSubscriptionModel> results,
        KvSubscriptionModel? quickAccessNode,
        Func<string?, string, bool> contains,
        string query)
    {
        if (quickAccessNode is null)
            return results;

        var matchingPins = quickAccessNode.PinnedItems
            .Where(kv => kv.HasData && contains(kv.Data.Name, query))
            .ToList();

        var filteredNode = new KvSubscriptionModel
        {
            DisplayName = quickAccessNode.DisplayName,
            Type = KvSubscriptionModel.ExplorerItemType.QuickAccess,
            IsExpanded = true,
            PinnedItems = new ObservableCollection<KeyVaultResource>(matchingPins)
        };

        return [filteredNode, .. results];
    }

    private static async Task<List<KvSubscriptionModel>> SearchByResourceGroupAsync(IEnumerable<SubscriptionResource> subscriptions, ParallelOptions parallelOptions, Func<string?, string, bool> contains, string query)
    {
        var results = new List<KvSubscriptionModel>();

        await Parallel.ForEachAsync(subscriptions, parallelOptions, async (sub, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            var matchingRgs = new List<KvResourceGroupModel>();

            await foreach (var rg in sub.GetResourceGroups())
            {
                ct.ThrowIfCancellationRequested();

                if (!contains(rg.Data?.Name, query))
                    continue;

                var rgModel = new KvResourceGroupModel
                {
                    DisplayName = rg.Data.Name,
                    ResourceGroupResource = rg,
                    IsExpanded = true,
                    KeyVaultResources = []
                };

                await foreach (var kv in rg.GetKeyVaults())
                {
                    ct.ThrowIfCancellationRequested();
                    rgModel.KeyVaultResources.Add(kv);
                }

                matchingRgs.Add(rgModel);
            }

            if (matchingRgs.Count > 0)
                results.Add(ToSubscriptionModel(sub, matchingRgs));
        });

        return results;
    }

    private static async Task<List<KvSubscriptionModel>> SearchByKeyVaultAsync(IEnumerable<SubscriptionResource> subscriptions, ParallelOptions parallelOptions, Func<string?, string, bool> contains, string query)
    {
        var results = new List<KvSubscriptionModel>();

        await Parallel.ForEachAsync(subscriptions, parallelOptions, async (sub, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            var matchingRgs = new List<KvResourceGroupModel>();

            await foreach (var rg in sub.GetResourceGroups())
            {
                ct.ThrowIfCancellationRequested();
                var matchingKvs = new List<KeyVaultResource>();

                await foreach (var kv in rg.GetKeyVaults())
                {
                    ct.ThrowIfCancellationRequested();
                    if (kv.HasData && contains(kv.Data.Name, query))
                        matchingKvs.Add(kv);
                }

                if (matchingKvs.Count > 0)
                {
                    matchingRgs.Add(new KvResourceGroupModel
                    {
                        DisplayName = rg.Data.Name,
                        ResourceGroupResource = rg,
                        IsExpanded = true,
                        KeyVaultResources = new ObservableCollection<KeyVaultResource>(matchingKvs)
                    });
                }
            }

            if (matchingRgs.Count > 0)
                results.Add(ToSubscriptionModel(sub, matchingRgs));
        });

        return results;
    }

    private static KvSubscriptionModel ToSubscriptionModel(SubscriptionResource sub, List<KvResourceGroupModel> rgs) =>
        new()
        {
            DisplayName = sub.Data.DisplayName,
            SubscriptionId = sub.Data.Id,
            Subscription = sub,
            IsExpanded = true,
            ResourceGroups = new ObservableCollection<KvResourceGroupModel>(rgs)
        };
}
