using Avalonia.Controls;
using Avalonia.Interactivity;
using KeyVaultExplorer.ViewModels;
using KeyVaultExplorer.Views.Pages;

namespace KeyVaultExplorer.Views.CustomControls;

public partial class ToolBar : UserControl
{
    public ToolBar()
    {
        InitializeComponent();
        //DataContext = Defaults.Locator.GetRequiredService<ToolBarViewModel>();
    }

    private void SettingsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Control control = (Control)sender!;
        control.RaiseEvent(new RoutedEventArgs(MainView.NavigateSettingsEvent));
    }

    private void SubscriptionsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Control control = (Control)sender!;
        control.RaiseEvent(new RoutedEventArgs(MainView.NavigateSubscriptionsEvent));
    }

    private void IsPaneToggledButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Control control = (Control)sender!;
        control.RaiseEvent(new RoutedEventArgs(TabViewPage.PaneToggledRoutedEvent));
    }
}