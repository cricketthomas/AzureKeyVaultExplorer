using Azure.ResourceManager.KeyVault;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;


public sealed class PinKeyVaultMessage : ValueChangedMessage<KeyVaultResource>
{
    public PinKeyVaultMessage(KeyVaultResource data) : base(data)
    {
        KeyVaultResource = data;
    }

    public KeyVaultResource KeyVaultResource { get; }
}
