using Azure.ResourceManager.KeyVault;

namespace KeyVaultExplorer.Services;

public partial class VaultService
{
    // needed to make the tree
    public class KeyVaultResourcePlaceholder : KeyVaultResource
    { }
}