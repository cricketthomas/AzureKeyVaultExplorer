using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;

public sealed class CreateSecretMessage : ValueChangedMessage<string>
{
    public CreateSecretMessage(string value) : base(value)
    {
        Title = value;
    }

    public string Title { get; }
}
