using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace C64UViewer.Services;

public class AudioStreamService
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private readonly CrossPlatformAudioPlayer _audioPlayer = new();
    private bool _isInitialized = false;

    public void InitializeAndListen(int port)
    {
        // Falls schon ein Listener läuft, stoppen wir ihn erst
        Stop();

        if (!_isInitialized)
        {
            _audioPlayer.Init();
            _isInitialized = true;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            try
            {
                _udpClient = new UdpClient();
                // Port-Sharing erlauben, falls andere Tools mitlauschen wollen
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                Trace.WriteLine($"Audio-Listener gestartet auf Port {port}");

                while (!token.IsCancellationRequested)
                {
                    var result = await _udpClient.ReceiveAsync(token);
                    
                    // Ultimate 64 Audio-Pakete validieren
                    // Header ist meist 12 Bytes, Daten folgen
                    if (result.Buffer.Length > 12)
                    {
                        // Wir überspringen den 12-Byte Header und senden nur die PCM-Daten
                        // Format: 48kHz, 16-Bit, Stereo
                        byte[] pcmData = new byte[result.Buffer.Length - 12];
                        Buffer.BlockCopy(result.Buffer, 12, pcmData, 0, pcmData.Length);
                        
                        _audioPlayer.EnqueueSamples(pcmData);
                    }
                }
            }
            catch (OperationCanceledException) { /* Normaler Stopp */ }
            catch (Exception ex)
            {
                Trace.WriteLine($"Audio UDP Fehler: {ex.Message}");
            }
            finally
            {
                _udpClient?.Close();
                _udpClient = null;
            }
        }, token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        
        // Den Hardware-Puffer leeren, damit beim nächsten Start kein "Rest-Sound" kommt
        _audioPlayer.ClearBuffer(); 
    }

    public void Dispose()
    {
        Stop();
        _audioPlayer.Dispose(); // Ruft SDL_Quit() auf
    }
}