using Azure.ResourceManager.KeyVault;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;


public sealed class AddDocumentMessage : ValueChangedMessage<KeyVaultData>
{
    public AddDocumentMessage(KeyVaultData keyVaultData) : base(keyVaultData)
    {
        KeyVaultData = keyVaultData;
    }

    public KeyVaultData KeyVaultData { get; }
}
