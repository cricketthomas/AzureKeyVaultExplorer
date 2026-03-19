using Azure.Core;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;

namespace AzureKeyVaultStudio.Models;

public class KeyVaultResourcePlaceholder : KeyVaultResource
{
    public override ResourceIdentifier Id => base.Id;
    public override KeyVaultData? Data
    {
        get
        {
            KeyVaultData? keyVaultData = base.Data ?? null;
            return keyVaultData;
        }
    }
}

public class KeyVaultDataPlaceholder : KeyVaultData
{
    public KeyVaultDataPlaceholder(AzureLocation location, KeyVaultProperties properties) : base(location, properties)
    {
    }
}
