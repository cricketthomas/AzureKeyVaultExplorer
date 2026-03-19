using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;


public sealed class ShowValidationErrorMessage : ValueChangedMessage<string>
{
    public ShowValidationErrorMessage(string value) : base(value)
    {
        Data = value;
    }

    public string Data { get; }
}
