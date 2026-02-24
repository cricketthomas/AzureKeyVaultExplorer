using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace KeyVaultExplorer.Models;

public partial class KvSubscriptionModel : ObservableObject
{
    [ObservableProperty]
    private bool hasSubNodeDataBeenFetched = false;

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool isSelected;

    public ObservableCollection<KvResourceGroupModel> ResourceGroups { get; set; } = [];
    public SubscriptionResource Subscription { get; set; } = null!;
    public string SubscriptionDisplayName { get; set; } = null!;
    public string? SubscriptionId { get; set; }
}

public partial class KvResourceGroupModel : ObservableObject
{
    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool isSelected;

    public ObservableCollection<KeyVaultResource> KeyVaultResources { get; set; } = [];
    public string ResourceGroupDisplayName { get; set; } = null!;

    public ResourceGroupResource ResourceGroupResource { get; set; } = null!;
}