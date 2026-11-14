using Avalonia;
using System;
using System.Runtime.InteropServices;

namespace C64UViewer;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (OperatingSystem.IsLinux())
        {
            NativeLibrary.SetDllImportResolver(typeof(SDL2.SDL).Assembly, (name, assembly, searchPath) =>
                name == "SDL2" && NativeLibrary.TryLoad("libSDL2-2.0.so.0", assembly, searchPath, out var handle)
                    ? handle : IntPtr.Zero);
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        const string appId = "de.gruetze_software.C64UViewer";
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        if (OperatingSystem.IsLinux())
        {
            builder.With(new X11PlatformOptions { WmClass = appId })
                .With(new WaylandPlatformOptions { AppId = appId });
            switch (Environment.GetEnvironmentVariable("C64UVIEWER_LINUX_BACKEND")?.Trim().ToLowerInvariant())
            {
                case "x11":
                    break;
                case "wayland":
                    builder.UseWayland();
                    break;
                default:
                    builder.UseWaylandWithFallback();
                    break;
            }
        }

        return builder;
    }
}
