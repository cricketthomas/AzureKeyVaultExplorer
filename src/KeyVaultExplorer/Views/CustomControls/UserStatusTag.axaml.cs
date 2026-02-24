using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using KeyVaultExplorer.ViewModels;
using KeyVaultExplorer.Services;
using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

namespace KeyVaultExplorer.Views.CustomControls;

public partial class UserStatusTag : UserControl
{
    public UserStatusTag()
    {
        InitializeComponent();
        DataContext = Defaults.Locator.GetRequiredService<MainViewModel>();
    }
}