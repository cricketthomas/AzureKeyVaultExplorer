using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class NewVersionDialog : ContentDialog
{
    public NewVersionViewModel ViewModel => DataContext as NewVersionViewModel;
    private IStringLocalizer? _localizer { get; set; }

    public NewVersionDialog()
    {
        this.InitializeComponent();
        Loaded += NewVersionDialog_Loaded;
        Unloaded += NewVersionDialog_Unloaded;
        _localizer = (Application.Current as App)?.Host?.Services.GetRequiredService<IStringLocalizer>();

    }

    private void NewVersionDialog_Unloaded(object sender, RoutedEventArgs e)
    {
        // WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private void NewVersionDialog_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<ShowValidationErrorMessage, Guid>(this, ViewModel.MessengerToken, async (r, m) =>
        {
            ShowDialogMessage(_localizer["NewItemErrorTitle"], m.Data, _localizer["NewItemDismissButtonText"]);
        });
        WeakReferenceMessenger.Default.Register<ShowSuccessOperationMessage, Guid>(this, ViewModel.MessengerToken, async (r, m) =>
        {
            string message;
            if (m.IsNewVersion)
                message = _localizer["NewItemVersionSavedMessage"];
            else
                message = string.Format(_localizer["NewItemCreatedMessage"], m.SecretName);

            ShowDialogMessage(_localizer["NewItemSuccessTitle"], message, _localizer["NewItemDismissButtonText"]);
        });

    }

    private void ShowDialogMessage(string title, string message, string dismissButtonText)
    {
        _ = DispatcherQueue.TryEnqueue(async () =>
        {
            var contentDialog = new ContentDialog
            {
                Title = title,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = message,
                        IsTextSelectionEnabled = true,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 0)
                    },
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    MaxHeight = 300
                },
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot,
                RequestedTheme = this.ActualTheme
            };
            await contentDialog.ShowAsync();
        });
    }
    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (ViewModel is null)
            return;

        if (ViewModel.IsEdit)
            return;

        if (!ViewModel.ValidateForSubmit())
            args.Cancel = true;
    }
}
