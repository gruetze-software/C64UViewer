using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using C64UViewer.ViewModels;

namespace C64UViewer.Views;


public partial class MainWindow : Window
{
    public MainWindow()
    {
       InitializeComponent();

        // Wir starten einen Timer direkt in der View, 
        // der das Image-Control zum Neuzeichnen zwingt.
        DispatcherTimer.Run(() =>
        {
            var image = this.FindControl<Image>("C64Display");
            if (image != null)
            {
                // Dies erzwingt, dass die Grafikkarte den aktuellen 
                // Inhalt des WriteableBitmaps neu ausliest.
                image.InvalidateVisual();
            }
            return true;
        }, TimeSpan.FromMilliseconds(16)); // Synchron zu deinen 60 FPS
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        var vm = DataContext as MainWindowViewModel;
        if (vm != null && vm.IsStreaming)
        {
            Trace.WriteLine("Close running Stream...");
            await vm.ToggleStream();
        }
        base.OnClosing(e);
    }
}