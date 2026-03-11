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
using Avalonia.Controls.ApplicationLifetimes;

namespace C64UViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string AppTitle { get; }
    public string StatusColor => IsStreaming ? "SpringGreen" : "Red";
    private DateTime _lastDataReceived = DateTime.MinValue;
    private bool _isDirty = false;
    private readonly VideoStreamService _streamVideoService = new();
    private readonly AudioStreamService _streamAudioService = new();
    [ObservableProperty] private WriteableBitmap _screenBitmap = new(new PixelSize(384, 272), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
    [ObservableProperty] private string _localIpAddress = "";
    [ObservableProperty] private string _statusMessage = "Waiting for UDP data (Start VIC Stream on C64U)...";
    [ObservableProperty] private bool _isStreaming = false;
    [ObservableProperty] private int _udpVideoPort = 11000;
    [ObservableProperty] private int _udpAudioPort = 11001;
    [ObservableProperty] private bool _isAudioEnabled = false;
    
    [ObservableProperty]
    private bool _isUpdateAvailable;

    private string _version = "";

    [ObservableProperty]
    private string _latestVersionText = string.Empty;
    private string _downloadUrl = string.Empty;

    public MainWindowViewModel()
    {
        // 1. Titelleiste aus Assembly-Infos
        var assembly = Assembly.GetExecutingAssembly();
        var title = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "C64U Slim-Viewer";
        _version = assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        var author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Grütze-Software";
        AppTitle = $"{title} v{_version} by {author}";

        // 2. Initialisierung
        ClearScreen();
        var settings = AppSettings.Load(); 
        UdpVideoPort = settings.UdpPort;
        UdpAudioPort = settings.UdpAudioPort;
        IsAudioEnabled = settings.IsAudioEnabled;
        LocalIpAddress = GetLocalIpAddress();
        
        // 3. UDP-Event verknüpfen
        _streamVideoService.OnRawFrameReceived += ProcessUdpPacket;
        
        // 4. DATEN-MOTOR (60 FPS Refresh)
        Trace.WriteLine("Starte UI-Refresh Timer (60 FPS)..."); 
        DispatcherTimer.Run(() =>
        {
            if (_isDirty)
            {
                // Wir erzwingen die Aktualisierung der UI-Bindung
                OnPropertyChanged(nameof(ScreenBitmap));
                _isDirty = false;
            }
            return true; 
        }, TimeSpan.FromMilliseconds(16), DispatcherPriority.MaxValue);

        // 5. STATUS-WÄCHTER (Prüft ob Pakete eintreffen)
        Trace.WriteLine("Starte Streaming-Status Timer (1 Sekunde)...");
        DispatcherTimer.Run(() => {
            if (_lastDataReceived != DateTime.MinValue) 
            {
                var secondsSinceLastData = (DateTime.Now - _lastDataReceived).TotalSeconds;
                bool currentlyStreaming = secondsSinceLastData < 2;
                
                if (IsStreaming != currentlyStreaming) 
                {
                    IsStreaming = currentlyStreaming;
                    OnPropertyChanged(nameof(StatusColor)); // WICHTIG: Farbe aktualisieren
                }
                
                if (!IsStreaming) 
                {
                    StatusMessage = "Waiting for data (Start Stream on C64U)...";
                    //if (secondsSinceLastData > 5) 
                        //ClearScreen();
                } 
                else 
                {
                    StatusMessage = "STREAMING ACTIVE";
                }
            }
            return true;
        }, TimeSpan.FromSeconds(1));

        // 6. SOFORT LAUSCHEN 
        RestartUdpListener();

        // 7. UPDATE PRÜFEN (asynchron)
        _ = CheckVersionAsync();
    }

    // Diese Methode wird vom Toolkit AUTOMATISCH generiert und aufgerufen,
    // sobald UdpPort (über das UI oder Code) geändert wird.
    partial void OnUdpVideoPortChanged(int value)
    {
            // 1. Erstmal den Stream als inaktiv setzen
        IsStreaming = false;

        // 2. Validierung: Ports gehen nur bis 65535
        if (value < 1024 || value > 65535)
        {
            StatusMessage = "Invalid Port (1024-65535)";
            OnPropertyChanged(nameof(StatusColor)); // Erzwingt Rot
            return;
        }

        SaveCurrentSettings();
        Trace.WriteLine($"Wechsle UDP-Video-Lauscher auf neuen Port {value}...");
        
        // 3. Den neuen Port initialisieren
        RestartUdpListener();

    }

    private void RestartUdpListener()
    {
        if (UdpVideoPort < 1024 || UdpVideoPort > 65535)
        {
            StatusMessage = "Invalid UDP Video Port (1024-65535)";
            OnPropertyChanged(nameof(StatusColor)); // Erzwingt Rot
            return;
        }

        if (UdpAudioPort < 1024 || UdpAudioPort > 65535)
        {
            StatusMessage = "Invalid UDP Audio Port (1024-65535)";
            OnPropertyChanged(nameof(StatusColor)); // Erzwingt Rot
            return;
        }

        Trace.WriteLine($"Starte UDP-Video-Lauscher auf Port {UdpVideoPort}...");
        try
        {
            _streamVideoService.InitializeAndListen(UdpVideoPort);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Fehler beim Starten des UDP-Listeners: {ex.Message}");
            StatusMessage = "Port Error: " + ex.Message;
            IsStreaming = false;
            OnPropertyChanged(nameof(StatusColor)); // Sicherstellen, dass es Rot bleibt
        }

        if ( IsAudioEnabled )
        {
            Trace.WriteLine($"Starte UDP-Audio-Lauscher auf Port {UdpAudioPort}...");
            try
            {
                _streamAudioService.InitializeAndListen(UdpAudioPort);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Fehler beim Starten des UDP-Audio-Listeners: {ex.Message}");
                StatusMessage = "Port Error: " + ex.Message;
                IsStreaming = false;
                OnPropertyChanged(nameof(StatusColor)); // Sicherstellen, dass es Rot bleibt
            }
        }
    }

    public async Task CheckVersionAsync()
    {
        var service = new UpdateService(_version);
        var (available, url, version) = await service.CheckForUpdates();
        
        if (available)
        {
            _downloadUrl = url;
            LatestVersionText = $"Update to {version} available (click to download)";
            IsUpdateAvailable = true;
        }
    }

    public void OpenUpdateUrl()
    {
        if (!string.IsNullOrEmpty(_downloadUrl))
        {
            Process.Start(new ProcessStartInfo(_downloadUrl) { UseShellExecute = true });
        }
    }

    [RelayCommand]
    public void ShowHelp()
    {
        var helpWin = new Views.HelpWindow();
        helpWin.Show();
    }

    public void SaveCurrentSettings()
    {
        Trace.WriteLine($"Speichere Einstellungen...");
        var settings = new AppSettings();
        settings.UdpPort = UdpVideoPort;
        settings.UdpAudioPort = UdpAudioPort;
        settings.IsAudioEnabled = IsAudioEnabled;
        settings.Save();
    }

    private string GetLocalIpAddress()
    {
        try {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530); // Verbindet nicht echt, hilft nur bei IP-Wahl
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        } catch { return "IP not found"; }
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

    public void OnExit()
    {
        // Stoppt den UDP-Listener und den SDL2-Player
        _streamAudioService.Stop();
        
        // Player direkt disposen:
        _streamAudioService.Dispose(); 
    }
}
