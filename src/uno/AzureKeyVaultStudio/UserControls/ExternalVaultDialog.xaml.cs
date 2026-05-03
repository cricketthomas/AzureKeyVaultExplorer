using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Presentation.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class ExternalVaultDialog : ContentDialog
{
    private readonly IStringLocalizer _localizer;
    public ExternalVaultDialogViewModel ViewModel => DataContext as ExternalVaultDialogViewModel;

    public ExternalVaultDialog()
    {
        DataContext = new ExternalVaultDialogViewModel();
        this.InitializeComponent();
        _localizer = (Application.Current as App).Host?.Services?.GetRequiredService<IStringLocalizer>();
        WeakReferenceMessenger.Default.Register<ShowValidationErrorMessage, Guid>(this, ViewModel.MessengerToken, async (r, m) =>
        {
            ShowDialogMessage(_localizer["NewItemErrorTitle"], m.Data, _localizer["NewItemDismissButtonText"]);
        });
    }

    private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.ValidateCommand.Execute(null);
        if (ViewModel.HasErrors)
        {
            args.Cancel = true;
            return;
        }
        await ViewModel.SubmitCommand.ExecuteAsync(null);
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
                RequestedTheme = this.ActualTheme,
            };
            await contentDialog.ShowAsync();
        });
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }
}
