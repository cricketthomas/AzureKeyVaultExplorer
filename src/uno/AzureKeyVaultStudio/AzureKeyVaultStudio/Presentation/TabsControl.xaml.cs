using System.Diagnostics;
using AzureKeyVaultStudio.Presentation.TabsModels;
using AzureKeyVaultStudio.Presentation.ViewModels;
using AzureKeyVaultStudio.UserControls;

namespace AzureKeyVaultStudio.Presentation;

public sealed partial class TabsControl : UserControl
{
    private readonly Dictionary<TabContainerBase, Frame> _tabFrameCache = new();
    private readonly Dictionary<TabContainerBase, CancellationTokenSource> _tabCancellationTokens = new();
    private Frame? _currentFrame;

    public TabsControl()
    {
        ViewModel = new TabsControlViewModel();
        InitializeComponent();
        //Loaded += TabsControl_Loaded;
        //Unloaded += TabsControl_Unloaded;
    }
    public TabsControlViewModel ViewModel { get; }

    //public FrameworkElement DragRegion => CustomDragRegion;

    private void TabsControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTab is not null &&
            _tabFrameCache.TryGetValue(ViewModel.SelectedTab, out var frame))
        {
            frame.Visibility = Visibility.Visible;
            _currentFrame = frame;
        }
    }

    private void HomeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewModelAsync<LoginViewModel>(this);
        SetTabPageState();
    }

    private void MyTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabView tabView || tabView.SelectedItem is not TabContainerBase tab)
            return;

        if (_currentFrame is not null)
        {
            _currentFrame.Visibility = Visibility.Collapsed;
            if (_currentFrame?.Content is VaultTabContentPage { ViewModel: { } previousVm })
                previousVm.IsActive = false;
        }

        if (!_tabFrameCache.TryGetValue(tab, out var frame))
        {
            frame = new Frame();
            _tabFrameCache[tab] = frame;
            FrameContainer.Children.Add(frame);
            frame.HorizontalAlignment = HorizontalAlignment.Stretch;
            frame.VerticalAlignment = VerticalAlignment.Stretch;

            // defer content
            _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                _ = LoadTabContentAsync(tab, frame);
            });
        }
        else
            _ = LoadTabContentAsync(tab, frame);

        frame.Visibility = Visibility.Visible;
        if (frame.Content is VaultTabContentPage { ViewModel: { } newVm })
            newVm.IsActive = true;
        _currentFrame = frame;
    }

    private async Task LoadTabContentAsync(TabContainerBase tab, Frame frame)
    {
        if (_tabCancellationTokens.TryGetValue(tab, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _tabCancellationTokens[tab] = cts;

        try
        {
            var needsNavigation = frame.Content?.GetType() != GetPageTypeForTab(tab);

            if (!needsNavigation)
            {
                if (tab is VaultTabContainer vaultTab)
                {
                    await vaultTab.EnsureDataLoadedAsync();
                }
                return;
            }

            switch (tab)
            {
                case VaultTabContainer vaultTab:
                    NavigateIfNeeded(frame, typeof(VaultTabContentPage), vaultTab.ViewModel);
                    cts.Token.ThrowIfCancellationRequested();
                    await vaultTab.EnsureDataLoadedAsync();
                    break;

                case PasswordGeneratorTab passwordTab:
                    NavigateIfNeeded(frame, typeof(PasswordGeneratorPage), passwordTab);
                    break;
                    //case PasswordGeneratorTab passwordTab:
                    //    NavigateIfNeeded(frame, typeof(PasswordGeneratorPage), passwordTab);
                    //    break;
            }
            if (frame.Content is VaultTabContentPage { ViewModel: { } loadedVm })
                loadedVm.IsActive = true;
        }
        catch (OperationCanceledException)
        {
            // Expected when tab is closed while loading
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load tab content: {ex}");
        }
        finally
        {
            if (_tabCancellationTokens.TryGetValue(tab, out var currentCts) && currentCts == cts)
            {
                _tabCancellationTokens.Remove(tab);
                cts.Dispose();
            }
        }
    }

    private static Type? GetPageTypeForTab(TabContainerBase tab)
    {
        return tab switch
        {
            VaultTabContainer => typeof(VaultTabContentPage),
            PasswordGeneratorTab => typeof(PasswordGeneratorPage),
            _ => null
        };
    }

    private static void NavigateIfNeeded(Frame frame, Type targetPageType, object? parameter)
    {
        if (frame.Content?.GetType() == targetPageType)
        {
            return;
        }

        frame.Navigate(targetPageType, parameter);
    }

    private void SetTabPageState()
    {
        SplitButtonTeachingTip.IsOpen = false;
    }
    private void SettingsMenuItemButton_Click(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewModelAsync<SettingsViewModel>(this);
        SetTabPageState();
    
    }

    private void SubscriptionsButton_Click(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewModelAsync<SubscriptionViewModel>(this);
        SetTabPageState();
    }

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is TabContainerBase tab)
        {
            if (_tabCancellationTokens.TryGetValue(tab, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _tabCancellationTokens.Remove(tab);
            }

            if (_tabFrameCache.TryGetValue(tab, out var frame))
            {
                CleanupFrame(frame);
                _tabFrameCache.Remove(tab);

                if (_currentFrame == frame)
                {
                    _currentFrame = null;
                }
            }

            if (tab is IDisposable disposable)
                disposable.Dispose();

            ViewModel.CloseTabCommand.Execute(tab);
        }
    }

    private void TabsControl_Unloaded(object sender, RoutedEventArgs e)
    {
        CleanupResources();
    }

    private void CleanupResources()
    {
        Unloaded -= TabsControl_Unloaded;

        foreach (var kvp in _tabCancellationTokens.ToList())
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }
        _tabCancellationTokens.Clear();

        foreach (var kvp in _tabFrameCache.ToList())
        {
            CleanupFrame(kvp.Value);
        }
        _tabFrameCache.Clear();
        _currentFrame = null;
    }

    private void CleanupFrame(Frame frame)
    {
        try
        {
            if (FrameContainer?.Children?.Contains(frame) == true)
                FrameContainer.Children.Remove(frame);

            frame.BackStack?.Clear();
            frame.ForwardStack?.Clear();

            if (frame.Content is IDisposable disposableContent)
                disposableContent.Dispose();

            frame.Content = null;
            frame.DataContext = null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error cleaning up frame: {ex}");
        }
    }

    private void PaneToggleButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        ViewModel.IsPaneOpen = !ViewModel.IsPaneOpen;
    }

    private async void OpenExternalItemByURLMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var stringLocalizer = (Application.Current as App)?.Host?.Services?.GetRequiredService<IStringLocalizer>();
        var dialog = new ExternalVaultItemDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = this.ActualTheme,
            Title = stringLocalizer?["OpenExternalVaultItemDialogTitle"] ?? "Open External Vault Item",
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = stringLocalizer?["SubmitButtonText"] ?? "Submit",
            CloseButtonText = stringLocalizer?["CancelButtonText"] ?? "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };
        await dialog.ShowAsync();
    }

    private async void OpenExternalVaultButton_Click(object sender, RoutedEventArgs e)
    {
        var stringLocalizer = (Application.Current as App)?.Host?.Services?.GetRequiredService<IStringLocalizer>();
        var dialog = new ExternalVaultDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = this.ActualTheme,
            Title = stringLocalizer?["OpenExternalDialogTitle"] ?? "Open External",
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = stringLocalizer?["SubmitButtonText"] ?? "Submit",
            CloseButtonText = stringLocalizer?["CancelButtonText"] ?? "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };
        await dialog.ShowAsync();
    }
}
