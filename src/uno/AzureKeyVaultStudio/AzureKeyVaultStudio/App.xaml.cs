using System.Diagnostics;
using AzureKeyVaultStudio.Database;
using AzureKeyVaultStudio.Services;
using AzureKeyVaultStudio.UserControls;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.UI.WindowManagement;

namespace AzureKeyVaultStudio;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();

        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            Debug.WriteLine($"[FirstChance] {e.Exception.GetType().FullName}: {e.Exception.Message}");
            Debug.WriteLine(e.Exception.StackTrace);
        };

        UnhandledException += (sender, args) =>
        {
            Debug.WriteLine($"[Unhandled] {args.Exception}");
#if DEBUG
            args.Handled = true;
#else
            // Do not swallow in packaged Release while diagnosing.
            args.Handled = false;
#endif
        };
    }

    public Window? MainWindow { get; private set; }
    public IHost? Host { get; private set; }
    public string AppTitle { get; init; } = "Key Vault Explorer";

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = DbContext.InitializeDatabase();

        var builder = this.CreateBuilder(args)
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    var logLevel = LogLevel.Debug;
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Error)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

#if DEBUG
                    //Uno Platform namespace filter groups
                    //Uncomment individual methods to see more detailed logging
                    // Generic Xaml events
                    logBuilder.XamlLogLevel(logLevel);
                    // Layout specific messages
                    logBuilder.XamlLayoutLogLevel(logLevel);
                    // Storage messages
                    logBuilder.StorageLogLevel(logLevel);
                    // Binding related messages
                    logBuilder.XamlBindingLogLevel(logLevel);
                    // Binder memory references tracking
                    logBuilder.BinderMemoryReferenceLogLevel(logLevel);
                    // DevServer and HotReload related
                    logBuilder.HotReloadCoreLogLevel(logLevel);
#endif
                }, enableUnoLogging: true)

                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                        .Section<ProjectUrls>()
                )

                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                .UseAuthentication(auth =>
            auth.AddCustom(custom =>
                custom.Login(async (sp, dispatcher, credentials, cancellationToken) =>
                    {
                        try
                        {
                            if (credentials?.TryGetValue(nameof(AuthenticatedUserClaims.Username), out var username) ?? false && !username.IsNullOrEmpty())
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var authService = sp.GetRequiredService<AuthService>();
                                if (!await authService.LoginOrRefreshAsync(cancellationToken))
                                    return null;
                                credentials ??= new Dictionary<string, string>();
                                credentials[nameof(AuthenticatedUserClaims.Username)] = authService.AuthenticatedUserClaims.Username;
                                credentials["Expiry"] = authService.Expiry.AddMinutes(-10).ToString("g");
                                return await ValueTask.FromResult<IDictionary<string, string>?>(credentials);
                            }

                            return await ValueTask.FromResult<IDictionary<string, string>?>(default);
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine("Loading canceled.");
                            return await ValueTask.FromResult<IDictionary<string, string>?>(default);
                        }
                    })
                .Refresh(async (sp, tokenDictionary, cancellationToken) =>
                {
                    try
                    {
                        var authService = sp.GetRequiredService<AuthService>();
                        var forcedRefresh = !await authService.LoginOrRefreshAsync(cancellationToken);
                        if (!forcedRefresh)
                        {
                            tokenDictionary ??= new Dictionary<string, string>();
                            tokenDictionary[nameof(AuthenticatedUserClaims.Username)] = authService.AuthenticatedUserClaims.Username;
                            tokenDictionary["Expiry"] = authService.Expiry.AddMinutes(-10).ToString("g");
                            return await ValueTask.FromResult<IDictionary<string, string>?>(tokenDictionary);
                        }

                        return await ValueTask.FromResult<IDictionary<string, string>?>(default);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("refresh canceled.");
                        return default;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Auth refresh failed: {ex.Message}");
                        return default;
                    }
                }), name: "CustomAuth")
                )
             .ConfigureServices((context, services) =>
             {
                 services.AddMemoryCache();
                 services.AddSingleton<ILocalSettingsService>(_ => LocalSettingsServiceFactory.Create());
                 services.AddSingleton<AuthService>();
                 services.AddSingleton<VaultService>();
                 services.AddSingleton<KeyVaultTreeViewModel>();
                 services.AddTransient<ItemPropertiesViewModel>();
             }).UseNavigation(RegisterRoutes));

        MainWindow = builder.Window;
        MainWindow.Title = AppTitle;
        EnsureEarlyWindow(MainWindow);
        //builder.Build();
#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>
            (initialNavigate: async (services, navigator) =>
            {
                var auth = services.GetRequiredService<IAuthenticationService>();
                var authenticated = await auth.RefreshAsync(CancellationToken.None);
                if (authenticated)
                {
                    await navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.Nested);
                }
                else
                {
                    await navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.Nested);
                }
            });
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<LoginPage, LoginViewModel>(),
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<SettingsPage, SettingsViewModel>(),
            new ViewMap<SubscriptionsPage, SubscriptionViewModel>(),
            new DataViewMap<ItemDetails, ItemPropertiesViewModel, KeyVaultItemProperties>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("Login", View: views.FindByViewModel<LoginViewModel>()),
                    new ("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault:true),
                    new ("SettingsPage", View: views.FindByViewModel<SettingsViewModel>()),
                    new ("SubscriptionsPage", View: views.FindByViewModel<SubscriptionViewModel>()),
                ]
            )
        );
    }

    private static void EnsureEarlyWindow(Window window)
    {
#if WINDOWS && !HAS_UNO
        window.AppWindow.Resize(new SizeInt32 { Width = 1100, Height = 640 });
        window.AppWindow.Move(new PointInt32 { X = 250, Y = 250 });
        window.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        //if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
        //{
        //}
        //window.ExtendsContentIntoTitleBar = true;
        //var theme = window.GetThemeService();
        //window.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        //window.AppWindow.TitleBar.PreferredTheme = theme.Theme switch
        //{
        //    AppTheme.Light => TitleBarTheme.Light,
        //    AppTheme.Dark => TitleBarTheme.Dark,
        //    _ => TitleBarTheme.UseDefaultAppMode,
        //};


        //window.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;

        window.SystemBackdrop = new MicaBackdrop()
        {
            Kind = MicaKind.Base,
        };
#else
        _ = window;
#endif
    }


}
