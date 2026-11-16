using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;

public sealed record NavigationBackRequestedMessage;
public sealed record BackButtonIsVisibleMessage;
public sealed record BackButtonIsHiddenMessage;

public enum TitleBarMode
{
    Default,
    MainPage
}

public sealed class TitleBarModeChangedMessage(TitleBarMode mode)
    : ValueChangedMessage<TitleBarMode>(mode);

public sealed class TitleBarChangedMessage(FrameworkElement passThrough, FrameworkElement dragRegion) 
    :ValueChangedMessage<(FrameworkElement, FrameworkElement)>((passThrough, dragRegion));
