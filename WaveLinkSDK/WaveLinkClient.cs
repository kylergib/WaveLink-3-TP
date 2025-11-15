using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using WaveLinkSDK.Interfaces;

namespace WaveLinkSDK;

public class WaveLinkClient //: IWaveLinkClient
{
    
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    public event EventHandler OnConnection;
    public event EventHandler<string> OnMessage;
    public event EventHandler OnClose;

    private ClientWebSocket _ws;
    public string Url { get; set; }
    public int Port { get; set; } // 28196
    public string Uuid { get; set; } = Guid.NewGuid().ToString("N").ToLower();
    public WaveLinkClient(ILoggerFactory? loggerFactory = null) : this(Statics.Localhost, 1884, loggerFactory) { }
    public WaveLinkClient(string uri, int port, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<WaveLinkClient>();
        Url = $"ws://{uri}:{port.ToString()}";
    }
    public async Task ConnectAsync()
    {
        var uri = new Uri("ws://127.0.0.1:1884");
        _ws = new ClientWebSocket();

        // Set the Origin header
        _ws.Options.SetRequestHeader("Origin", "streamdeck://");

        // Optional: set Host manually (usually automatic)
        _ws.Options.SetRequestHeader("Host", "127.0.0.1:1884");

        _logger.LogInformation("Trying to connect to: {Url}", Url);
        await _ws.ConnectAsync(uri, CancellationToken.None);
        if (_ws.State != WebSocketState.Open)
        {
            _logger.LogError("Failed to connect to WebSocket at {Url}", Url);
            return;
        }
        OnConnection?.Invoke(this, EventArgs.Empty);
        _logger.LogInformation("Connected successfully");

        var receiveTask = Task.Run(() => ReceiveMessages());
        //await receiveTask;
    }
    // This method will run in the background and receive messages continuously
    private async Task ReceiveMessages()
    {
        var buffer = new byte[1024];

        while (_ws.State == WebSocketState.Open)
        {
            try
            {
                var result = await _ws.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Server closed the connection.");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received: {message}");
                OnMessage?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                break;
            }
        }
        OnClose?.Invoke(this, EventArgs.Empty);

        Console.WriteLine("Receive loop has stopped.");
    }

    // Method to send messages externally
    public async Task SendMessageAsync(string message)
    {
        if (_ws != null && _ws.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            _logger.LogInformation($"Sent from external call: {message}");
        }
        else
        {
            _logger.LogWarning("WebSocket is not connected.");
        }
    }

    public void SetUpEvents()
    {
        throw new NotImplementedException();
    }
    public bool IsConnected()
    {
        return _ws != null && _ws.State == WebSocketState.Open;
    }
    public void Close()
    {
        _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
    }

}
