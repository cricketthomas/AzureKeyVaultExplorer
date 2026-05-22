using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;

public sealed class PaneStateChangedMessage(bool value) : ValueChangedMessage<bool>(value);
