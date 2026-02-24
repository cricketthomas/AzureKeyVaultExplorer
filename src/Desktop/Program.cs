using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

namespace Desktop;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // prepare and run your App here
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (HttpRequestException e) when (e.InnerException.Message.Contains("No such host is known", StringComparison.InvariantCultureIgnoreCase))
        {
            // here we can work with the exception, for example add it to our log file
            bool IsConnected = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (!IsConnected)
            {
                Debug.Write(e.ToString());
            }
            //Log.Fatal(e, "Something very bad happened");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<KeyVaultExplorer.App>()
                 .AfterSetup(_ =>
                 {
                     KeyVaultExplorer.App.CreateDesktopResources();
                     // HACK. https://github.com/AvaloniaUI/Avalonia/issues/14059
                     var x = Comparer<int>.Default;
                     var y = Comparer<decimal>.Default;
                     var d = Comparer<DateTime>.Default;
                     var dto = Comparer<DateTimeOffset?>.Default;
                     var dton = Comparer<DateTimeOffset?>.Default;
                     var g = Comparer<Guid>.Default;
                     var gn = Comparer<Guid?>.Default;
                     var dtn = Comparer<DateTime?>.Default;
                     var z = Comparer<PlatformID>.Default;
                 })
                .UsePlatformDetect()
                .LogToTrace();
}