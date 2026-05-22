using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;


public sealed class CloseWindowMessage : ValueChangedMessage<Window>
{
    public CloseWindowMessage(Window value) : base(value)
    {
        CurrentWindow = value;
    }
    public Window CurrentWindow { get; }

}
