using System;
using System.Threading.Tasks;
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

            // Beim Beenden der App aufräumen
            desktop.Exit += (sender, args) =>
            {
                // Wir rufen eine Cleanup-Methode im ViewModel auf
                viewModel.OnExit();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void AboutMenuClick(object? sender, EventArgs e) =>
        await OpenMainWindowDialogAsync(window => window.ShowInfoAsync());

    private async void SetupMenuClick(object? sender, EventArgs e) =>
        await OpenMainWindowDialogAsync(window => window.ShowSetupAsync());

    private async void CrtMenuClick(object? sender, EventArgs e) =>
        await OpenMainWindowDialogAsync(window => window.ShowCrtSettingsAsync());

    private Task OpenMainWindowDialogAsync(Func<MainWindow, Task> showDialog) =>
        ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow { IsVisible: true, IsEnabled: true } window }
            ? showDialog(window)
            : Task.CompletedTask;
}
