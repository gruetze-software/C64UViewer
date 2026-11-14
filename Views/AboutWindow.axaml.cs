using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace C64UViewer.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    public AboutWindow(string version) : this()
    {
        VersionLabel.Text = $"Version {version}";
    }

    private void CloseDialog(object? sender, RoutedEventArgs e) => Close();

    private void OpenLink(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string url })
            return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            LinkStatus.IsVisible = false;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Trace.WriteLine(ex);
            LinkStatus.Text = "Could not open the link in your browser.";
            LinkStatus.IsVisible = true;
        }
    }
}
