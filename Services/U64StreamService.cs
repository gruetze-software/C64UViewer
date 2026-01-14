using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace C64UViewer.Services;

public class U64StreamService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private UdpClient? _udpClient;

    public event Action<byte[]>? OnRawFrameReceived;

    // 1. Diese Funktion wird EINMAL beim Start der App aufgerufen
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

    // 2. Diese Funktion wird aufgerufen, wenn du den Button drückst
    public async Task SendStartSignal(string c64Ip, string myIp, int videoPort)
    {
        // Wir nutzen NUR noch die REST-API (HTTP PUT)
        var url = $"http://{c64Ip}/v1/streams/video:start?ip={myIp}:{videoPort}";
        Trace.WriteLine(url);
        var response = await _httpClient.PutAsync(url, null);
        
        if (!response.IsSuccessStatusCode)
            throw new Exception($"C64U reports Error: {response.StatusCode}");
    }

    public async Task SendStopSignal(string c64Ip)
{
    try
    {
        // Der offizielle REST-Befehl zum Stoppen des Video-Streams
        var url = $"http://{c64Ip}/v1/streams/video:stop";
        Trace.WriteLine(url);
        var response = await _httpClient.PutAsync(url, null);
        
        // Und für Audio sicherheitshalber auch
        var audioUrl = $"http://{c64Ip}/v1/streams/audio:stop";
        await _httpClient.PutAsync(audioUrl, null);
    }
    catch (Exception ex)
    {
        // Falls der C64U schon aus ist, ignorieren wir den Fehler beim Stoppen
        Console.WriteLine($"Stop-Signal failed: {ex.Message}");
    }
}
}