using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace C64UViewer.Services;

public class AudioStreamService : IDisposable
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private readonly CrossPlatformAudioPlayer _audioPlayer = new();
    private bool _isInitialized = false;

    // EVENT: Wird gefeuert, sobald IRGENDEIN Audio-Paket ankommt (für die Status-LED)
    public event Action<byte[]>? OnAudioPacketReceived;

    public void InitializeAndListen(int port)
    {
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
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                Trace.WriteLine($"Audio-Listener gestartet auf Port {port}");

                while (!token.IsCancellationRequested)
                {
                    // Wir warten auf Pakete
                    var result = await _udpClient.ReceiveAsync(token);
                    
                    // 1. Event feuern: Das ViewModel registriert das und setzt '_lastAudioReceived'
                    OnAudioPacketReceived?.Invoke(result.Buffer);
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

    /// <summary>
    /// Verarbeitet die Rohdaten der Ultimate 64 und schiebt sie in den SDL-Puffer.
    /// Wird vom ViewModel aufgerufen, wenn Audio aktiviert ist.
    /// </summary>
    public void PushAudioData(byte[] rawData)
    {
        try
        {
            // Ultimate 64 Audio-Pakete validieren
            // Der Header ist 12 Bytes lang, dahinter liegen die PCM-Daten
            if (rawData != null && rawData.Length > 12)
            {
                // Header überspringen (Format der U64: 48kHz, 16-Bit, Stereo)
                int payloadLength = rawData.Length - 12;
                byte[] pcmData = new byte[payloadLength];
                Buffer.BlockCopy(rawData, 12, pcmData, 0, payloadLength);
                
                // Ab in den CrossPlatformAudioPlayer (SDL2 Queue)
                _audioPlayer.EnqueueSamples(pcmData);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Fehler beim Verarbeiten der Audiodaten: {ex.Message}");
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        
        _audioPlayer.ClearBuffer(); 
    }

    public void Dispose()
    {
        Stop();
        _audioPlayer.Dispose();
    }
}