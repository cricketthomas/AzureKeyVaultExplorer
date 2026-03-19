using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class NewItem : UserControl
{
    public NewVersionViewModel ViewModel => DataContext as NewVersionViewModel;
    public Window? ParentWindow { get; set; }
    private IStringLocalizer? _localizer { get; set; }

    public NewItem()
    {
        this.InitializeComponent();
        Loaded += NewItem_Loaded;
        Unloaded += NewItem_Unloaded;
        _localizer = (Application.Current as App)?.Host?.Services.GetRequiredService<IStringLocalizer>();
    }


    private void NewItem_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }


    private void NewItem_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<ShowValidationErrorMessage, Guid>(this, ViewModel.MessengerToken, async (r, m) =>
        {
            ShowDialogMessage(_localizer["NewItemErrorTitle"], m.Data, _localizer["NewItemDismissButtonText"]);
        });
        WeakReferenceMessenger.Default.Register<ShowSuccessOperationMessage, Guid>(this, ViewModel.MessengerToken, async (r, m) =>
        {
            var message = string.Format(_localizer["NewItemCreatedMessage"], m.SecretName);
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
                RequestedTheme = this.ActualTheme,
            };
            contentDialog.CloseButtonClick += ContentDialog_CloseButtonClick;
            await contentDialog.ShowAsync();
        });
    }

    private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        DispatcherQueue.TryEnqueue(() =>
        {
            ParentWindow?.Close();
        });
    }

    private async void CreateNewItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is NewVersionViewModel vm)
        {
            await vm.NewSecretVersionCommand.ExecuteAsync(null);
        }
    }
}
