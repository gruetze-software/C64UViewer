using System.Net;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace C64UViewer.Views;

public partial class HelpWindow : Window
{
    public HelpWindow() : this(string.Empty, 11000, 11001)
    {
    }

    public HelpWindow(string localIpAddress, int videoPort, int audioPort)
    {
        InitializeComponent();

        string host = IPAddress.TryParse(localIpAddress, out var address) && !IPAddress.IsLoopback(address)
            ? localIpAddress
            : "<viewer computer LAN IP>";

        VideoDestination.Text = FormatDestination(host, videoPort);
        AudioDestination.Text = FormatDestination(host, audioPort);
    }

    private static string FormatDestination(string host, int port) =>
        port is >= 1 and <= IPEndPoint.MaxPort ? $"{host}:{port}" : $"{host}:<valid UDP port>";

    private void CloseClick(object? sender, RoutedEventArgs e) => Close();
}
