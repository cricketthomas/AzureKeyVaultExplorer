using System.Reflection;
using AzureKeyVaultStudio.Messages;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;

namespace AzureKeyVaultStudio.Presentation;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    public ContentControl ContentControl => Splash;
    public const int NotifcationTimeoutSeconds = 2;

    public Shell()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<SendInAppNotificationMessage>(this, (r, m) => ShowInAppNotification(m.Notification));
        PointerPressed += Shell_PointerPressed;
    }

    private async void Shell_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as UIElement).Properties.IsXButton1Pressed)
        {
            WeakReferenceMessenger.Default.Send(new NavigationBackRequestedMessage());
        }
    }

    private void ShowInAppNotification(Notification notification)
    {
        notification.Message = notification?.Message?.Length > 200 ? notification.Message.Substring(0, 200) + "..." : notification?.Message;
        //        if (Constants.IsAppPackaged)
        //        {
        //#if WINDOWS
        //            var toastNotifier = Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier();
        //            var toastXml = Windows.UI.Notifications.ToastNotificationManager.GetTemplateContent(
        //                Windows.UI.Notifications.ToastTemplateType.ToastText02);

        //            var stringElements = toastXml.GetElementsByTagName("text");
        //            stringElements[0].AppendChild(toastXml.CreateTextNode(notification.Title));

        //            stringElements[1].AppendChild(toastXml.CreateTextNode(notification.Message));

        //            var toast = new Windows.UI.Notifications.ToastNotification(toastXml)
        //            {
        //                ExpirationTime = DateTimeOffset.Now.AddSeconds(NotifcationTimeoutSeconds),
        //                Tag = "AzureKeyVaultExplorer",
        //            };

        //            toastNotifier.Show(toast);
        //            //#else
        //#endif
        //        }
        notification?.Duration = notification.Duration ?? TimeSpan.FromSeconds(NotifcationTimeoutSeconds);
        NotificationQueue.Show(notification);
    }
}
