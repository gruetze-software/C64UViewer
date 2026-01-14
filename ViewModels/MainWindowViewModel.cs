using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using C64UViewer.Models;
using C64UViewer.Services;
using System.Linq;
using System.Diagnostics;
using System.Reflection;

namespace C64UViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string AppTitle { get; }

    private DateTime _lastDataReceived = DateTime.MinValue;
    private bool _isDirty = false;

    private readonly U64StreamService _streamService = new();
    // Die Bitmap für die Anzeige (384x272 Pixel inkl. Border)
    [ObservableProperty]
    private WriteableBitmap _screenBitmap = new(
        new PixelSize(384, 272), 
        new Vector(96, 96), 
        PixelFormat.Bgra8888, 
        AlphaFormat.Opaque);
    
    [ObservableProperty] private string _u64IpAddress = "192.168.178.164";
    [ObservableProperty] private string _localIpAddress = "";
    [ObservableProperty] private string _statusMessage = "Ready for Start";
    [ObservableProperty] private bool _isStreaming = false;
    [ObservableProperty]
    private bool _isBusy = false;

   public MainWindowViewModel()
    {
        var assembly = Assembly.GetExecutingAssembly();
        // Produktname aus <Product> holen
        var title = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "C64U Slim-Viewer";
            // Version aus <Version> holen (gekürzt auf 3 Stellen, z.B. 1.0.0)
        var version = assembly.GetName().Version?.ToString(3) ?? "1.0.0";
            // Autor/Firma aus <Authors> (wird in AssemblyCompany gemappt)
        var author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Grütze-Software";
        AppTitle = $"{title} v{version} by {author}";

        // 1. Initialisierung
        ClearScreen();
        var settings = AppSettings.Load();
        U64IpAddress = settings.LastIpAddress; // Geladene IP setzen
        LocalIpAddress = GetLocalIpAddress();
        _streamService.OnRawFrameReceived += ProcessUdpPacket;
        
        // 2. DATEN-MOTOR (60 FPS)
        // Dieser Timer sorgt dafür, dass die UI-Bindung die neuen Pixeldaten "sieht"
        DispatcherTimer.Run(() =>
        {
            if (_isDirty)
            {
                OnPropertyChanged(nameof(ScreenBitmap));
                _isDirty = false;
            }
            return true; 
        }, TimeSpan.FromMilliseconds(16));

        // 3. WÄCHTER (Watchdog)
        // WICHTIG: Erhöhe die Zeit auf 7 oder 10 Sekunden. 
        // 3 Sekunden sind bei hoher CPU-Last (60 FPS Rendering) oft zu knapp,
        // was zu den ungewollten "Fail: break datastream" Meldungen führt.
        DispatcherTimer.Run(() =>
        {
            if (IsStreaming && _lastDataReceived != DateTime.MinValue)
            {
                var secondsSinceLastData = (DateTime.Now - _lastDataReceived).TotalSeconds;
                if (secondsSinceLastData > 7) 
                {
                    Dispatcher.UIThread.Post(() => {
                        StatusMessage = "Fail: break datastream (Check Network/C64U)";
                        IsStreaming = false; 
                    });
                }
            }
            return true;
        }, TimeSpan.FromSeconds(2));

        // 4. NETZWERK START
        // Den Port sofort zu öffnen ist unter Linux korrekt, damit der Socket bereit ist.
        _streamService.InitializeAndListen(11000); 
        StatusMessage = "Ready for Start";
    }

    [RelayCommand]
    private void ShowHelp()
    {
        var helpWin = new Views.HelpWindow();
        // Wenn du eine Referenz zum Hauptfenster hast, kannst du .ShowDialog(owner) nutzen
        helpWin.Show(); 
    }


    [RelayCommand]
    private async Task StartStream()
    {
       try
        {
            IsBusy = true;
            // Speichern, wenn der User auf Start drückt
            new AppSettings { LastIpAddress = U64IpAddress }.Save();
            if (!IsStreaming)
            {
                StatusMessage = ".";
                await _streamService.SendStartSignal(U64IpAddress, LocalIpAddress, 11000);
                
                // Der UDP-Listener wurde bereits im Konstruktor gestartet
                IsStreaming = true;
                StatusMessage = "Stream active...";
            }
            else
            {
                StatusMessage = "Stopping stream...";
                await _streamService.SendStopSignal(U64IpAddress);
                IsStreaming = false;
                ClearScreen();
                StatusMessage = "Ready for Start";
            }
        }
        catch (Exception ex)
        {
            IsStreaming = false;
            ClearScreen();
            StatusMessage = $"Fail: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ToggleStream()
    {
        if (IsStreaming)
        {
            await _streamService.SendStopSignal(U64IpAddress);
            IsStreaming = false;
            StatusMessage = "Stream stopped";
            ClearScreen();
        }
        else
        {
            await StartStream(); // Ruft die obige Logik auf
        }
    }

    private string GetLocalIpAddress()
    {
        try
        {
            // Wir holen uns alle Netzwerk-Schnittstellen (LAN, WLAN, etc.)
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            
            foreach (var ni in interfaces)
            {
                // Wir ignorieren Loopback (127.0.0.1) und inaktive Schnittstellen
                if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                    ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                {
                    var props = ni.GetIPProperties();
                    // Wir suchen die erste IPv4 Adresse, die mit 192. anfängt
                    var ip = props.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork 
                                        && a.Address.ToString().StartsWith("192."));

                    if (ip != null)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
        }
        catch
        {
            return "IP not ascertainable";
        }

        return "No 192.x Adress found";
    }

private void ClearScreen()
{
    using (var lockedBitmap = ScreenBitmap.Lock())
    {
        unsafe
        {
            uint* ptr = (uint*)lockedBitmap.Address;
            // Alles auf Schwarz setzen (Alpha: 255, R: 0, G: 0, B: 0)
            for (int i = 0; i < 384 * 272; i++) ptr[i] = 0xFF000000;
        }
    }
    // Explizit triggern, damit das Schwarz angezeigt wird
    OnPropertyChanged(nameof(ScreenBitmap));
}

private void ProcessUdpPacket(byte[] data)
{
    _lastDataReceived = DateTime.Now;
    
    // Ein U64 Video-Paket hat 12 Bytes Header + 768 Bytes Daten = 780 Bytes
    if (data == null || data.Length < 12) return;

    using (var lockedBitmap = ScreenBitmap.Lock())
    {
        unsafe
        {
            uint* backBuffer = (uint*)lockedBitmap.Address;
            
            // 1. Zeilennummer aus Byte 4 & 5 lesen (Little Endian)
            // Bit 15 der Zeilennummer ist das "Last Packet" Flag, das maskieren wir weg
            int lineNumber = (data[5] << 8 | data[4]) & 0x7FFF;
            
            // 2. Start-Pixel im Bild berechnen (Zeile * Breite)
            int startPixelIndex = lineNumber * 384;
            
            int headerOffset = 12;
            int pixelDataLength = data.Length - headerOffset;

            for (int i = 0; i < pixelDataLength; i++)
            {
                byte val = data[i + headerOffset];

                // Ein Byte enthält zwei 4-Bit Pixel (Nibbles)
                // Erstes Pixel (Untere 4 Bits)
                int p1 = startPixelIndex + (i * 2);
                if (p1 < (384 * 272))
                    backBuffer[p1] = C64Colors.Palette[(byte)(val & 0x0F)];

                // Zweites Pixel (Obere 4 Bits)
                int p2 = p1 + 1;
                if (p2 < (384 * 272))
                    backBuffer[p2] = C64Colors.Palette[(byte)(val >> 4)];
            }
        }
    }
    _isDirty = true;
}
}
