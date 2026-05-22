using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;


public sealed class TogglePaneMessage : ValueChangedMessage<bool>
{
    public TogglePaneMessage(bool toggleState) : base(toggleState)
    {
        ToggleState = toggleState;
    }

    public bool ToggleState { get; }
}
