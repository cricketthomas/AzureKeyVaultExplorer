using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;

public sealed class AuthenticationStateChangedMessage(AuthenticatedUserClaims value) : ValueChangedMessage<AuthenticatedUserClaims>(value);
public sealed record AuthenticationRemovedStateChangedMessage;
