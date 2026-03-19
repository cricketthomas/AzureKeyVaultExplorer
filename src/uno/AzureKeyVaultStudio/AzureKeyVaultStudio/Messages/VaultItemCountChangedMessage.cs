using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;

public sealed class VaultItemCountChangedMessage(string value) : ValueChangedMessage<string>(value);
