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
    
    // Getrennte Zeitstempel für präzise Statusanzeige
    private DateTime _lastVideoReceived = DateTime.MinValue;
    private DateTime _lastAudioReceived = DateTime.MinValue;
    private bool _isDirty = false;
    private string _version;

    // Services
    private readonly VideoStreamService _streamVideoService = new();
    private readonly AudioStreamService _streamAudioService = new();

    [ObservableProperty] private WriteableBitmap _screenBitmap = new(new PixelSize(384, 272), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
    [ObservableProperty] private string _localIpAddress = "";
    [ObservableProperty] private string _statusMessage = "Waiting for data (Start Stream on C64U)...";
    
    // Status-Flags für die UI
    [ObservableProperty] private bool _isVideoStreaming = false;
    [ObservableProperty] private bool _isAudioStreaming = false;
    
    // Die Farbe richtet sich primär nach dem Video-Status
    public string StatusColor => IsVideoStreaming ? "SpringGreen" : (IsAudioStreaming ? "Yellow" : "Red");

    [ObservableProperty] private int _udpVideoPort = 11000;
    [ObservableProperty] private int _udpAudioPort = 11001;
    [ObservableProperty] private bool _isAudioEnabled = true;

    [ObservableProperty] private bool _isUpdateAvailable = false;
    [ObservableProperty] private string _latestVersionText = "";
    [ObservableProperty] private bool _isAudioAvailable = false; 

    public MainWindowViewModel()
    {
        // 1. Titelleiste generieren
        var assembly = Assembly.GetExecutingAssembly();
        var title = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "C64U Slim-Viewer";
        _version = assembly.GetName().Version?.ToString(3) ?? "1.1.0";
        var author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Grütze-Software";
        AppTitle = $"{title} v{_version} by {author}";

        // 2. Initialisierung
        ClearScreen();
        var settings = AppSettings.Load(); 
        UdpVideoPort = settings.UdpPort;
        UdpAudioPort = settings.UdpAudioPort;
        IsAudioEnabled = settings.IsAudioEnabled;
        LocalIpAddress = GetLocalIpAddress();
        
        // 3. UDP-Events verknüpfen
        _streamVideoService.OnRawFrameReceived += ProcessUdpPacket;
        _streamAudioService.OnAudioPacketReceived += ProcessAudioPacket;
        
        // 4. BILD-MOTOR (60 FPS Refresh)
        DispatcherTimer.Run(() =>
        {
            if (_isDirty)
            {
                OnPropertyChanged(nameof(ScreenBitmap));
                _isDirty = false;
            }
            return true; 
        }, TimeSpan.FromMilliseconds(16), DispatcherPriority.MaxValue);

        // 5. STATUS-WÄCHTER (Trennt Video- und Audio-Status)
        DispatcherTimer.Run(() => {
            var now = DateTime.UtcNow;
            bool videoActive = (now - _lastVideoReceived).TotalSeconds < 2;
            bool audioActive = (now - _lastAudioReceived).TotalSeconds < 2;

            IsVideoStreaming = videoActive;
            IsAudioStreaming = audioActive;

            if (videoActive) {
                StatusMessage = audioActive ? "STREAMING ACTIVE (VIDEO + AUDIO)" : "STREAMING ACTIVE (VIDEO ONLY)";
            } else if (audioActive) {
                StatusMessage = "AUDIO ONLY - Check Video Port!";
            } else {
                StatusMessage = "Waiting for data (Start Stream on C64U)...";
            }

            OnPropertyChanged(nameof(StatusColor));
            return true;
        }, TimeSpan.FromSeconds(1));

        _isAudioAvailable = CheckAudioAvailability();
        RestartUdpListener();
    }

    private void ProcessUdpPacket(byte[] data)
    {
        _lastVideoReceived = DateTime.UtcNow;

        // Zurück zur robusten v1.1 Logik
        if (data == null || data.Length < 12) return;

        using (var lockedBitmap = ScreenBitmap.Lock())
        {
            unsafe
            {
                uint* backBuffer = (uint*)lockedBitmap.Address;
                
                // Zeilennummer aus Byte 4 & 5 (Little Endian)
                int lineNumber = (data[5] << 8 | data[4]) & 0x7FFF;
                
                if (lineNumber >= 272 || lineNumber < 0) return;

                int startPixelIndex = lineNumber * 384;
                int headerOffset = 12; 
                int pixelDataLength = data.Length - headerOffset;

                for (int i = 0; i < pixelDataLength; i++)
                {
                    byte val = data[i + headerOffset];
                    int p1 = startPixelIndex + (i * 2);
                    
                    if (p1 < (384 * 272))
                    {
                        backBuffer[p1] = C64Colors.Palette[(byte)(val & 0x0F)];
                        
                        int p2 = p1 + 1;
                        if (p2 < (384 * 272))
                            backBuffer[p2] = C64Colors.Palette[(byte)(val >> 4)];
                    }
                }
            }
        }
        _isDirty = true;
    }

    private void ProcessAudioPacket(byte[] data)
    {
        if (!IsAudioEnabled || !IsAudioAvailable) return;
        _lastAudioReceived = DateTime.UtcNow;
        _streamAudioService.PushAudioData(data);
    }

    [RelayCommand]
    public void RestartUdpListener()
    {
        Trace.WriteLine("RestartUdpListener: Neustart des UDP-Listeners");
        _streamVideoService.InitializeAndListen(UdpVideoPort);
        if (IsAudioEnabled && IsAudioAvailable)
        {
            _streamAudioService.InitializeAndListen(UdpAudioPort);
        }
        
        // Settings speichern
        var settings = new AppSettings { 
            UdpPort = UdpVideoPort, 
            UdpAudioPort = UdpAudioPort, 
            IsAudioEnabled = IsAudioEnabled 
        };
        settings.Save();
    }

    private void ClearScreen()
    {
        using (var buf = ScreenBitmap.Lock())
        {
            unsafe
            {
                uint* ptr = (uint*)buf.Address;
                for (int i = 0; i < 384 * 272; i++) ptr[i] = C64Colors.Palette[0];
            }
        }
        _isDirty = true;
    }

    private string GetLocalIpAddress()
    {
        try {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        } catch { return "127.0.0.1"; }
    }

    [RelayCommand]
    public void OpenUpdateUrl()
    {
        // Öffnet die GitHub Release-Seite im Standard-Browser
        var url = "https://github.com/gruetze-software/C64UViewer/releases";
        try {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        } catch { /* Ignorieren falls Browser nicht startet */ }
    }

    [RelayCommand]
    public void ShowHelpCommand()
    {
        // Hier könntest du ein Hilfe-Fenster öffnen oder einfach einen Link
        var url = "https://github.com/dein-benutzername/C64UViewer#readme";
        try {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        } catch { }
    }

    private bool CheckAudioAvailability()
    {
        try {
            SDL2.SDL.SDL_GetVersion(out _);
            return true;
        } catch {
            return false;
        }
    }

    public void OnExit()
    {
        // Stoppt die UDP-Empfänger und gibt Ports frei
        _streamVideoService.Stop();
        _streamAudioService.Dispose(); // Audio-Service hat Dispose, was Stop inkludiert
        
        Trace.WriteLine("App-Exit: Ressourcen wurden freigegeben.");
    }
}