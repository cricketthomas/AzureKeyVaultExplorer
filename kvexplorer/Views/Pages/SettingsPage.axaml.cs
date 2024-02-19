﻿using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using kvexplorer.ViewModels;

namespace kvexplorer.Views.Pages;

public partial class SettingsPage : UserControl
{
    private bool IsInitialLoad = true;

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = Defaults.Locator.GetRequiredService<SettingsPageViewModel>();
        var bgCheckbox = this.FindControl<CheckBox>("BackgroundTransparencyCheckbox")!;
        bgCheckbox.IsCheckedChanged += BackgroundTransparency_ChangedEvent;
        AddHandler(Frame.NavigatedToEvent, OnNavigatedTo, RoutingStrategies.Direct);
    }

    private void OnNavigatedTo(object sender, NavigationEventArgs e)
    {
        if (e.NavigationMode != NavigationMode.Back && IsInitialLoad)
        {
            IsInitialLoad = false;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                (DataContext as SettingsPageViewModel).SignInOrRefreshTokenCommand.Execute(null);
            }, DispatcherPriority.Background);
        }
    }

    private void BackgroundTransparency_ChangedEvent(object? sender, RoutedEventArgs e)
    {
        Control control = (Control)sender!;
        control.RaiseEvent(new RoutedEventArgs(MainWindow.TransparencyChangedEvent));
    }

    private void FetchUserInfoSettingsExpanderItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as SettingsPageViewModel).SignInOrRefreshTokenCommand.Execute(null);
    }

    private void NumericUpDown_Spinned(object? sender, Avalonia.Controls.SpinEventArgs e)
    {
        (DataContext as SettingsPageViewModel).SetClearClipboardTimeoutCommand.Execute(null);
    }

    private void FAComboBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        Console.WriteLine((sender as FAComboBoxItem).Content);

    }

    
}