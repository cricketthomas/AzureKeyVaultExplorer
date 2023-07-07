﻿using avalonia.kvexplorer.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Windowing;
using kvexplorer.shared;
using Avalonia.Media;
using Avalonia.Controls.Notifications;

namespace avalonia.kvexplorer.Views;

public partial class MainWindow : AppWindow
{
    private MainViewModel _mainWindowViewModel;
    private AuthService _authService;
    private AppWindow _appWindowTitleBar;


    public MainWindow()
    {
        InitializeComponent();
        _mainWindowViewModel = new MainViewModel();
        //TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(35, 155, 155, 155);
    }

    public MainWindow(AuthService authService, MainViewModel mainWindowViewModel)
    {
        _authService = authService;
        _mainWindowViewModel = mainWindowViewModel;
    }

    private void OpenWindowButton_Click(object? sender, RoutedEventArgs e)
    {
        // Create the window object
        var sampleWindow =
            new Window
            {
                Title = "Sample Window",
                Width = 200,
                Height = 200
            };

        // open the window
        sampleWindow.Show();
    }

    public void button_Click(object sender, RoutedEventArgs e)
    {
        // Change button text when button is clicked.
        var not = new Notification("Test", "this is a test notification message", NotificationType.Warning);
        var nm = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 1
        };
        nm.TemplateApplied += (sender, args) =>
        {
            nm.Show(not);
        };
        var button = (Button)sender;

        button.Content = "Hello, Avalonia!";
    }
}