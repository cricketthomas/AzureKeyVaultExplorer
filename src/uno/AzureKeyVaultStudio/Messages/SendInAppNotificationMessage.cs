using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI.Behaviors;

namespace AzureKeyVaultStudio.Messages;


public sealed class SendInAppNotificationMessage : ValueChangedMessage<Notification>
{
    public SendInAppNotificationMessage(Notification value) : base(value)
    {
        //var notification = new Notification
        //{
        //    Title = $"Notification {DateTimeOffset.Now}",
        //    Message = GetRandomText(),
        //    Severity = MUXC.InfoBarSeverity.Informational,
        //};

        Notification = value;
    }

    public Notification Notification { get; }
}
