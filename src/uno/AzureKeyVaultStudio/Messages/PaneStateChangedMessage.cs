using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;

public record PaneState(bool IsOpen, SplitViewPanePlacement PanePlacement);
public sealed class PaneStateChangedMessage(PaneState value) : ValueChangedMessage<PaneState>(value);
