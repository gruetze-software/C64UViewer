using System;

namespace C64UViewer.Services;

public class VideoStreamService
{
    private readonly UdpStreamListener _listener = new();

    public event Action<byte[]>? OnRawFrameReceived;

    public void InitializeAndListen(int port) =>
        _listener.Start(port, packet => OnRawFrameReceived?.Invoke(packet), 2 * 1024 * 1024);

    public void Stop() => _listener.Stop();
}
