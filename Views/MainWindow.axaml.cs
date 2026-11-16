using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using C64UViewer.ViewModels;

namespace C64UViewer.Views;

public partial class MainWindow : Window
{
    private async void InfoClick(object? sender, RoutedEventArgs e) => await ShowInfoAsync();

    internal async Task ShowInfoAsync()
    {
        if (DataContext is MainWindowViewModel viewModel)
            await new AboutWindow(viewModel.DisplayVersion).ShowDialog(this);
    }

    private async void CrtClick(object? sender, RoutedEventArgs e) => await ShowCrtSettingsAsync();

    private async void SetupClick(object? sender, RoutedEventArgs e) => await ShowSetupAsync();

    internal Task ShowSetupAsync() =>
        DataContext is MainWindowViewModel viewModel
            ? new HelpWindow(viewModel.LocalIpAddress, viewModel.UdpVideoPort, viewModel.UdpAudioPort).ShowDialog(this)
            : Task.CompletedTask;

    internal async Task ShowCrtSettingsAsync()
    {
        if (DataContext is MainWindowViewModel viewModel)
            await new CrtSettingsWindow(viewModel).ShowDialog(this);
    }
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };

    public MainWindow()
    {
        InitializeComponent();
        _renderTimer.Tick += (_, _) =>
        {
            if (DataContext is not MainWindowViewModel viewModel)
                return;

            int previousHeight = viewModel.VideoHeight;
            if (viewModel.PresentFrame())
            {
                C64Display.InvalidateVisual();
                if (viewModel.GlowOpacity > 0)
                    GlowDisplay.InvalidateVisual();

                if (viewModel.VideoHeight != previousHeight && WindowState == WindowState.Normal &&
                    VideoArea.Bounds.Width > 0 && VideoArea.Bounds.Height > 0)
                {
                    double surroundingHeight = ClientSize.Height - VideoArea.Bounds.Height;
                    Height = surroundingHeight +
                        VideoArea.Bounds.Width * viewModel.VideoHeight / MainWindowViewModel.Width;
                }
            }
        };
        Opened += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.Start();

            _renderTimer.Start();
        };
        Closed += (_, _) => _renderTimer.Stop();
    }
}
