using AzureKeyVaultStudio.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.Presentation;

public partial class StatusBarViewModel : ObservableRecipient
{

    public StatusBarViewModel()
    {
        IsActive = true;
    }


    [ObservableProperty]
    public partial AuthenticatedUserClaims Claims { get; set; }


    [ObservableProperty]
    public partial string ItemCount { get; set; } = "0 items";

    protected override void OnActivated()
    {
        base.OnActivated();

        Messenger.Register<StatusBarViewModel, AuthenticationStateChangedMessage>(this, (r, m) =>
        {
            r.Claims = m.Value;
        });

        Messenger.Register<StatusBarViewModel, VaultItemCountChangedMessage>(this, (r, m) =>
        {
            r.ItemCount = m.Value;
        });
    }
}
