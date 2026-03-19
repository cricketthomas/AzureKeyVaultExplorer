using System.Collections.ObjectModel;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;

namespace AzureKeyVaultStudio.Models;

public partial class PinnedItemModel : ObservableObject
{
    [ObservableProperty]
    public partial bool HasSubNodeDataBeenFetched { get; set; } = false;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public string DisplayName { get; set; } = null!;

    public virtual ObservableCollection<KeyVaultResource> KeyVaultResources { get; set; } = [];
}

public partial class KvSubscriptionModel : ObservableObject
{
    [ObservableProperty]
    public partial bool HasSubNodeDataBeenFetched { get; set; } = false;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public enum ExplorerItemType
    { QuickAccess, ResourceGroup };

    public ExplorerItemType Type { get; set; } = ExplorerItemType.ResourceGroup;

    public ObservableCollection<KvResourceGroupModel> ResourceGroups { get; set; } = [];
    public virtual ObservableCollection<KeyVaultResource> PinnedItems { get; set; } = [];

    public SubscriptionResource Subscription { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? SubscriptionId { get; set; }
}

public partial class KvResourceGroupModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public ObservableCollection<KeyVaultResource> KeyVaultResources { get; set; } = [];
    public string DisplayName { get; set; } = null!;

    public ResourceGroupResource ResourceGroupResource { get; set; } = null!;
}

internal partial class ExplorerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate SubscriptionTemplate { get; set; }
    public DataTemplate ResourceGroupTemplate { get; set; }
    public DataTemplate KeyVaultResourceTemplate { get; set; }
    public DataTemplate PinnedItemTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is KvSubscriptionModel model)
        {
            if (model.Type == KvSubscriptionModel.ExplorerItemType.QuickAccess)
                return PinnedItemTemplate;
            else return SubscriptionTemplate;
        }

        if (item is KvResourceGroupModel)
            return ResourceGroupTemplate;

        if (item is KeyVaultResource)
            return KeyVaultResourceTemplate;

        return base.SelectTemplateCore(item);
    }
}
