using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using System.Linq;

namespace KeyVaultExplorer.ViewModels;

public class NotificationViewModel
{
    public WindowNotificationManager? NotificationManager { get; set; } = new();

    public void AddMessage(Notification notification)
    {
        NotificationManager?.Show(notification);
    }

    public async void ShowPopup(Notification notification)
    {
        var td = new TaskDialog
        {
            Title = notification.Title,
            Header = notification.Title,
            Content = notification.Message,
            FooterVisibility = TaskDialogFooterVisibility.Always,
            IsFooterExpanded = false,
            Buttons = { TaskDialogButton.CloseButton },
        };
        //Avalonia.Application.Current.GetTopLevel() as AppWindow;
        var lifetime = App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        td.XamlRoot = lifetime.Windows.Last() as AppWindow;
        await td.ShowAsync(true);
    }
}