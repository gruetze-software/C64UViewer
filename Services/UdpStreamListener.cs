using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace C64UViewer.Services;

internal sealed class UdpStreamListener
{
    private CancellationTokenSource? _cancellation;
    private Task? _receiveTask;

    public void Start(int port, Action<byte[]> onPacket, int? receiveBufferSize = null)
    {
        Stop();

        var client = new UdpClient();
        try
        {
            client.ExclusiveAddressUse = true;
            if (receiveBufferSize is int size)
                client.Client.ReceiveBufferSize = size;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        }
        catch
        {
            client.Dispose();
            throw;
        }

        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        _receiveTask = Task.Run(() => ReceiveAsync(client, onPacket, token));
    }

    private static async Task ReceiveAsync(UdpClient client, Action<byte[]> onPacket, CancellationToken token)
    {
        using (client)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var packet = await client.ReceiveAsync(token).ConfigureAwait(false);
                    if (!token.IsCancellationRequested)
                        onPacket(packet.Buffer);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"UDP listener stopped: {ex}");
            }
        }
    }

    public void Stop()
    {
        _cancellation?.Cancel();
        _receiveTask?.GetAwaiter().GetResult();
        _cancellation?.Dispose();
        _cancellation = null;
        _receiveTask = null;
    }
}
