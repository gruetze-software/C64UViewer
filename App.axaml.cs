using Avalonia;
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
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            // Beim Beenden der App aufräumen
            desktop.Exit += (sender, args) =>
            {
                // Wir rufen eine Cleanup-Methode im ViewModel auf
                viewModel.OnExit();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

}
