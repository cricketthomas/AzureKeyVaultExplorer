using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;

public sealed class OpenItemDetailsWindowMessage : ValueChangedMessage<KeyVaultItemProperties>
{
    public OpenItemDetailsWindowMessage(KeyVaultItemProperties value) : base(value)
    {
        Data = value;
    }

    public KeyVaultItemProperties Data { get; }
}
