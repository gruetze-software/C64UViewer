using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace C64UViewer.Services;

public class VideoStreamService
{
    private UdpClient? _udpClient;

    public event Action<byte[]>? OnRawFrameReceived;

    public void InitializeAndListen(int port)
    {
        if (_udpClient != null) return;

        _udpClient = new UdpClient();
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        // Puffer auf 2MB erhöhen, damit bei UI-Last keine Pakete verloren gehen
        _udpClient.Client.ReceiveBufferSize = 2 * 1024 * 1024;
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var result = await _udpClient.ReceiveAsync();
                    //Trace.WriteLine($"Paket erhalten! Größe: {result.Buffer.Length} Bytes");
                    OnRawFrameReceived?.Invoke(result.Buffer);
                }
            }
            catch { /* Port geschlossen oder Fehler */ }
        });
    }
   
}