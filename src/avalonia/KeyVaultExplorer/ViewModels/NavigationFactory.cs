using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using KeyVaultExplorer.Views.Pages;
using System;
using System.Collections.Generic;

namespace KeyVaultExplorer.ViewModels;

public class NavigationFactory : INavigationPageFactory
{
    // Do this to avoid needing Activator.CreateInstance to create from type info
    // and to avoid a ridiculous amount of 'ifs'
    private readonly Control[] _pages =
    {
        new MainPage(),
        new SubscriptionsPage(),
        new SettingsPage(),
    };

    private readonly Dictionary<string, Func<Control>> CorePages = new Dictionary<string, Func<Control>>
    {
        { "MainPage", () => new MainPage() },
        { "SubscriptionsPage", () => new SubscriptionsPage() },
        { "SettingsPage", () => new SettingsPage() },
    };

    public NavigationFactory()
    {
        Instance = this;
    }

    private static NavigationFactory? Instance { get; set; }

    public static Control[] GetPages()
    {
        return Instance!._pages;
    }

    // Create a page based on a Type, but you can create it however you want
    public Control? GetPage(Type srcType)
    {
        // Return null here because we won't use this method at all
        CorePages.TryGetValue(srcType.FullName, out var func);
        Control page = null;
        page = func();
        return page;
    }

    // Create a page based on an object, such as a view model
    public Control? GetPageFromObject(object target)
    {
        return target switch
        {
            MainPage => _pages[0],
            SubscriptionsPage => _pages[1],
            SettingsPage => _pages[2],

            _ => throw new Exception()
        };
    }
}