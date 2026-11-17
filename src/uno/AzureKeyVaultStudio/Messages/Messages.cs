using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AzureKeyVaultStudio.Messages;

public sealed record NavigationBackRequestedMessage;
//public sealed record BackButtonIsVisibleMessage;
//public sealed record BackButtonIsHiddenMessage;

public sealed class TitleBarChangedMessage(FrameworkElement passThrough, FrameworkElement dragRegion) 
    :ValueChangedMessage<(FrameworkElement, FrameworkElement)>((passThrough, dragRegion));
