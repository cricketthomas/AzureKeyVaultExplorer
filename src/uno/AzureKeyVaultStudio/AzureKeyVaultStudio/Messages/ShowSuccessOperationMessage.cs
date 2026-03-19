using CommunityToolkit.Mvvm.Messaging.Messages;

public sealed class ShowSuccessOperationMessage : ValueChangedMessage<string>
{
    public ShowSuccessOperationMessage(string secretName, bool isNewVersion) : base(secretName)
    {
        SecretName = secretName;
        IsNewVersion = isNewVersion;
    }

    public string? SecretName { get; }
    public bool IsNewVersion { get; } = false;
}
