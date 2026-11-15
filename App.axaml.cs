using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using C64UViewer.ViewModels;
using C64UViewer.Views;

namespace C64UViewer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
            var window = new MainWindow
            {
                DataContext = viewModel,
            };
            desktop.MainWindow = window;

            if (OperatingSystem.IsMacOS())
                ConfigureMacMenu(window);

            // Beim Beenden der App aufräumen
            desktop.Exit += (sender, args) =>
            {
                // Wir rufen eine Cleanup-Methode im ViewModel auf
                viewModel.OnExit();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureMacMenu(MainWindow window)
    {
        var info = new NativeMenuItem("About C64U Slim-Viewer");
        info.Click += async (_, _) => await window.ShowInfoAsync();
        var crt = new NativeMenuItem("CRT Effects…");
        crt.Click += async (_, _) => await window.ShowCrtSettingsAsync();
        var menu = new NativeMenu();
        menu.Items.Add(info);
        menu.Items.Add(crt);
        NativeMenu.SetMenu(this, menu);
    }
}
