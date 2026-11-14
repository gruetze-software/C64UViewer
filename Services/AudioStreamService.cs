using System;

namespace C64UViewer.Services;

public class AudioStreamService : IDisposable
{
    private const int HeaderLength = 2;
    private readonly UdpStreamListener _listener = new();
    private readonly CrossPlatformAudioPlayer _audioPlayer = new();

    public event Action<byte[]>? OnAudioPacketReceived;

    public void InitializeAndListen(int port)
    {
        _audioPlayer.Init();
        _listener.Start(port, packet =>
        {
            int payloadLength = packet.Length - HeaderLength;
            if (payloadLength <= 0 || payloadLength % 4 != 0)
                return;

            OnAudioPacketReceived?.Invoke(packet);
            _audioPlayer.EnqueueSamples(packet.AsSpan(HeaderLength));
        });
    }

    public void Stop()
    {
        _listener.Stop();
        _audioPlayer.ClearBuffer();
    }

    public void Dispose()
    {
        Stop();
        _audioPlayer.Dispose();
    }
}
