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
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace C64UViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string AppTitle { get; }
    [ObservableProperty] private bool _scanlinesEnabled;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ScanlineOpacity))]
    private int _scanlineStrength;
    public double ScanlineOpacity => ScanlineStrength / 100.0;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(GlowOpacity))]
    private bool _glowEnabled;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(GlowOpacity))]
    private int _glowStrength;
    public double GlowOpacity => GlowEnabled ? GlowStrength / 100.0 : 0;
    public string DisplayVersion => _version.Build == 0 ? _version.ToString(2) : _version.ToString(3);

    internal const int Width = 384;
    private const int Height = 272;
    private const int HeaderLength = 12;
    [ObservableProperty] private int _videoHeight = Height;
    private int _receivedVideoHeight = Height;
    private const string ProjectUrl = "https://github.com/gruetze-software/C64UViewer";

    private readonly uint[] _pixels = new uint[Width * Height];
    private readonly object _frameLock = new();
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Version _version;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _isDirty;
    private bool _started;
    private bool _closed;
    private long _lastVideoReceived;
    private long _lastAudioReceived;
    private int? _activeVideoPort;
    private int? _activeAudioPort;
    private string? _listenerError;
    private string? _updateUrl;


    // Services
    private readonly VideoStreamService _streamVideoService = new();
    private readonly AudioStreamService _streamAudioService = new();

    public WriteableBitmap ScreenBitmap { get; } = new(new PixelSize(Width, Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
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
        _version = assembly.GetName().Version ?? new Version(1, 3, 0);
        var displayVersion = _version.Build == 0 ? _version.ToString(2) : _version.ToString(3);
        var author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Grütze-Software";
        AppTitle = $"{title} v{displayVersion} by {author}";

        var settings = AppSettings.Load();
        _scanlinesEnabled = settings.ScanlinesEnabled;
        _scanlineStrength = Math.Clamp(settings.ScanlineStrength, 0, 70);
        _glowEnabled = settings.GlowEnabled;
        _glowStrength = Math.Clamp(settings.GlowStrength, 0, 100);
        _udpVideoPort = IsValidPort(settings.UdpPort) ? settings.UdpPort : 11000;
        _udpAudioPort = IsValidPort(settings.UdpAudioPort) ? settings.UdpAudioPort : 11001;
        _isAudioEnabled = settings.IsAudioEnabled;

        Array.Fill(_pixels, C64Colors.Palette[0]);
        _isDirty = true;
        PresentFrame();
        _streamVideoService.OnRawFrameReceived += ProcessUdpPacket;
        _streamAudioService.OnAudioPacketReceived += _ =>
            Interlocked.Exchange(ref _lastAudioReceived, Environment.TickCount64);
        _statusTimer.Tick += (_, _) => UpdateStatus();
    }

    public void Start()
    {
        if (_started || _closed)
            return;

        _started = true;
        LocalIpAddress = GetLocalIpAddress();
        IsAudioAvailable = CheckAudioAvailability();
        ConfigureListeners();
        _statusTimer.Start();
#if !WINDOWS_STORE
        _ = CheckForUpdatesAsync();
#endif
    }

    private void ProcessUdpPacket(byte[] data)
    {
        if (data.Length <= HeaderLength)
            return;

        int lineField = data[4] | data[5] << 8;
        int line = lineField & 0x7FFF;
        if (line >= Height)
            return;

        int pixel = line * Width;
        int length = Math.Min(data.Length - HeaderLength, (_pixels.Length - pixel) / 2);

        lock (_frameLock)
        {
            // The high bit marks the last packet of a VIC frame.
            int endLine = line + length * 2 / Width;
            if ((lineField & 0x8000) != 0 && endLine is 240 or Height)
                _receivedVideoHeight = endLine;

            for (int i = 0; i < length; i++)
            {
                byte colors = data[HeaderLength + i];
                _pixels[pixel++] = C64Colors.Palette[colors & 0x0F];
                _pixels[pixel++] = C64Colors.Palette[colors >> 4];
            }

            _isDirty = true;
        }

        Interlocked.Exchange(ref _lastVideoReceived, Environment.TickCount64);
    }

    public unsafe bool PresentFrame()
    {
        int videoHeight;
        lock (_frameLock)
        {
            if (!_isDirty || _closed)
                return false;

            using var bitmap = ScreenBitmap.Lock();
            for (int row = 0; row < Height; row++)
            {
                var destination = new Span<uint>((byte*)bitmap.Address + row * bitmap.RowBytes, Width);
                _pixels.AsSpan(row * Width, Width).CopyTo(destination);
            }

            _isDirty = false;
            videoHeight = _receivedVideoHeight;
        }

        VideoHeight = videoHeight;
        return true;
    }

    partial void OnUdpVideoPortChanged(int value) => ApplySettings();
    partial void OnUdpAudioPortChanged(int value) => ApplySettings();
    partial void OnIsAudioEnabledChanged(bool value) => ApplySettings();

    private void ApplySettings()
    {
        if (!_started || _closed)
            return;

        ConfigureListeners();
        if (!IsValidPort(UdpVideoPort) || !IsValidPort(UdpAudioPort))
            return;

        SaveSettings();
    }

    public void SetCrtSettings(bool enabled, int strength, bool glowEnabled, int glowStrength)
    {
        ScanlinesEnabled = enabled;
        ScanlineStrength = Math.Clamp(strength, 0, 70);
        GlowEnabled = glowEnabled;
        GlowStrength = Math.Clamp(glowStrength, 0, 100);
        SaveSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var previous = AppSettings.Load();
            new AppSettings
            {
                UdpPort = IsValidPort(UdpVideoPort) ? UdpVideoPort : previous.UdpPort,
                UdpAudioPort = IsValidPort(UdpAudioPort) ? UdpAudioPort : previous.UdpAudioPort,
                IsAudioEnabled = IsAudioEnabled,
                ScanlinesEnabled = ScanlinesEnabled,
                ScanlineStrength = ScanlineStrength,
                GlowEnabled = GlowEnabled,
                GlowStrength = GlowStrength
            }.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.WriteLine(ex);
        }
    }

    private static bool IsValidPort(int port) => port is >= 1 and <= IPEndPoint.MaxPort;

    private void ConfigureListeners()
    {
        bool enableAudio = IsAudioEnabled && IsAudioAvailable;
        if (_activeAudioPort.HasValue &&
            (!enableAudio || (IsValidPort(UdpAudioPort) && _activeAudioPort != UdpAudioPort)))
        {
            _streamAudioService.Stop();
            _activeAudioPort = null;
            Interlocked.Exchange(ref _lastAudioReceived, 0);
        }

        _listenerError = null;
        if (!IsValidPort(UdpVideoPort))
        {
            _listenerError = "Invalid video port (1–65535).";
        }
        else if (_activeVideoPort != UdpVideoPort)
        {
            _streamVideoService.Stop();
            _activeVideoPort = null;
            Interlocked.Exchange(ref _lastVideoReceived, 0);

            try
            {
                _streamVideoService.InitializeAndListen(UdpVideoPort);
                _activeVideoPort = UdpVideoPort;
            }
            catch (SocketException ex)
            {
                _listenerError = $"Cannot open video port {UdpVideoPort}.";
                Trace.WriteLine(ex);
            }
        }

        if (enableAudio && !IsValidPort(UdpAudioPort))
        {
            _listenerError ??= "Invalid audio port (1–65535).";
        }
        else if (enableAudio && _activeAudioPort != UdpAudioPort)
        {
            try
            {
                _streamAudioService.InitializeAndListen(UdpAudioPort);
                _activeAudioPort = UdpAudioPort;
            }
            catch (Exception ex) when (ex is SocketException or InvalidOperationException)
            {
                _listenerError ??= ex is SocketException socketError
                    ? $"Cannot open UDP audio port {UdpAudioPort} ({socketError.SocketErrorCode})."
                    : ex.Message;
                Trace.WriteLine(ex);
            }
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        long now = Environment.TickCount64;
        long video = Interlocked.Read(ref _lastVideoReceived);
        long audio = Interlocked.Read(ref _lastAudioReceived);
        IsVideoStreaming = video != 0 && now - video < 2000;
        IsAudioStreaming = audio != 0 && now - audio < 2000;
        StatusMessage = _listenerError ?? ((IsVideoStreaming, IsAudioStreaming) switch
        {
            (true, true) => "STREAMING ACTIVE (VIDEO + AUDIO)",
            (true, false) => "STREAMING ACTIVE (VIDEO ONLY)",
            (false, true) => "AUDIO ONLY - Check Video Port!",
            _ => "Waiting for data (Start Stream on C64U)..."
        });

        OnPropertyChanged(nameof(StatusColor));
    }

    private async Task CheckForUpdatesAsync()
    {
        var (available, url, tag) = await UpdateService.CheckForUpdatesAsync(_version, _lifetime.Token);
        if (!available || _closed)
            return;

        _updateUrl = url;
        LatestVersionText = $"Update available: {tag}";
        IsUpdateAvailable = true;
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
        var url = _updateUrl ?? ProjectUrl + "/releases";
        try {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        } catch { /* Ignorieren falls Browser nicht startet */ }
    }

    [RelayCommand]
    public void ShowHelp()
    {
        // Hier könntest du ein Hilfe-Fenster öffnen oder einfach einen Link
        var url = ProjectUrl + "#readme";
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
        if (_closed)
            return;

        _closed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _statusTimer.Stop();
        _streamVideoService.Stop();
        _streamAudioService.Dispose();
        ScreenBitmap.Dispose();

        Trace.WriteLine("App-Exit: Ressourcen wurden freigegeben.");
    }
}
