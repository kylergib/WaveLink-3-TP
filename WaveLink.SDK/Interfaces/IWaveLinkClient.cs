using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WaveLink.SDK.Models;

namespace WaveLink.SDK.Interfaces;

public interface IWaveLinkClient
{
    event EventHandler? OnConnection;
    event EventHandler<string>? OnMessage;
    event EventHandler? OnClose;

    event EventHandler<ApplicationInfoResponse>? OnReceivedAppInfo;
    string Url { get; }
    int Port { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task SendJsonRequestAsync(JsonRpcRequest request);
    Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "Closing", CancellationToken cancellationToken = default);
    bool IsConnected { get; }
}