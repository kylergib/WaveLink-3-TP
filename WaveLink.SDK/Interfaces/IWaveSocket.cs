using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WaveLink.SDK.Interfaces;

public interface IWaveSocket : IDisposable
{
    event EventHandler? OnConnection;
    event EventHandler<string>? OnMessage;
    event EventHandler? OnClose;

    string Url { get; set; }
    int Port { get; set; }
    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
    Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "Closing", CancellationToken cancellationToken = default);
}