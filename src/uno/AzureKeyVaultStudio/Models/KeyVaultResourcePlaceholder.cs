using Azure.Core;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;

namespace AzureKeyVaultStudio.Models;

public class KeyVaultResourcePlaceholder : KeyVaultResource
{
    private static readonly ResourceIdentifier PlaceholderId =
        new("/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/loading/providers/Microsoft.KeyVault/vaults/loading");

    private static KeyVaultData CreatePlaceholderData()
    {
        var properties = new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard));
        return new KeyVaultDataPlaceholder(AzureLocation.EastUS2, properties);
    }

    private readonly KeyVaultData _placeholderData = CreatePlaceholderData();

    public override ResourceIdentifier Id => PlaceholderId;

    public override KeyVaultData? Data => _placeholderData;
}

public class KeyVaultDataPlaceholder : KeyVaultData
{
    public KeyVaultDataPlaceholder(AzureLocation location, KeyVaultProperties properties) : base(location, properties)
    {
    }
}
