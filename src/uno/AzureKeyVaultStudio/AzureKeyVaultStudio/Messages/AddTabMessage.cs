using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;

public sealed class AddTabMessage : ValueChangedMessage<TabContainerBase>
{
    public AddTabMessage(TabContainerBase tab) : base(tab)
    {
    }

    public TabContainerBase Tab => Value;
}
