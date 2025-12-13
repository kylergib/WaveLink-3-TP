using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WaveLink.SDK.Interfaces;
using WaveLink.SDK.Models;

namespace WaveLink.SDK;

public class WaveSocket : IWaveSocket
{
    private readonly ILogger _logger;

    private ClientWebSocket? _ws;
    private Task? _receiveTask;
    private CancellationTokenSource? _receiveCts;

    public event EventHandler? OnConnection;
    public event EventHandler<string>? OnMessage;
    public event EventHandler? OnClose;

    public string Url { get; set; }
    public int Port { get; set; }

    public bool IsConnected => _ws is not null && _ws.State == WebSocketState.Open;
    public WaveSocket(ILogger? logger = null) : this(Statics.Localhost, Statics.DefaultPort, logger) { }
    public WaveSocket(string uri, int port, ILogger? logger = null)
    {
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<WaveLinkClient>();
        Port = port;
        Url = $"ws://{uri}:{port}";
    }

    /// <summary>
    /// Creates the concrete ClientWebSocket. Override in tests to provide a test double.devig
    /// </summary>
    protected virtual ClientWebSocket CreateClientWebSocket() => new ClientWebSocket();

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.LogDebug("Already connected to {Url}", Url);
            return;
        }

        var uri = new Uri(Url);
        _ws = CreateClientWebSocket();

        // set headers commonly expected by the target server
        try
        {
            _ws.Options.SetRequestHeader("Origin", "streamdeck://");
            _ws.Options.SetRequestHeader("Host", uri.Authority);
        }
        catch (Exception ex)
        {
            // Some platforms may not support SetRequestHeader; log and continue
            _logger.LogDebug(ex, "Failed setting request headers on ClientWebSocket (non-fatal).");
        }

        _logger.LogInformation("Trying to connect to: {Url}", Url);
        try
        {
            await _ws.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Connect cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to connect to WebSocket at {Url}", Url);
            _logger.LogError(ex.Message);
            throw;
        }

        if (_ws.State != WebSocketState.Open)
        {
            _logger.LogError("WebSocket state is not Open after ConnectAsync to {Url}: {State}", Url, _ws.State);
            return;
        }

        _logger.LogInformation("Connected successfully to {Url}", Url);
        OnConnection?.Invoke(this, EventArgs.Empty);

        // start receive loop
        _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveTask = Task.Run(() => ReceiveMessages(_receiveCts.Token), CancellationToken.None);
    }

    private async Task ReceiveMessages(CancellationToken cancellationToken)
    {
        const int bufferSize = 4096;
        var buffer = new byte[bufferSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _ws != null)
            {
                var segment = new ArraySegment<byte>(buffer);
                WebSocketReceiveResult result;

                using var ms = new MemoryStream();

                do
                {
                    try
                    {
                        result = await _ws.ReceiveAsync(segment, cancellationToken);
                    }
                    catch (WebSocketException)
                    {
                        break; // abrupt close
                    }
                    catch (IOException)
                    {
                        break; // TCP closed
                    }

                    if (result.MessageType == WebSocketMessageType.Close ||
                        result.CloseStatus.HasValue ||
                        result.Count == 0)
                    {
                        break;
                    }

                    ms.Write(buffer, 0, result.Count);

                } while (!result.EndOfMessage);


                if (_ws.State != WebSocketState.Open)
                    break;

                string fullMessage = Encoding.UTF8.GetString(ms.ToArray());

                foreach (var json in SplitJsonMessages(fullMessage))
                {
                    _logger.LogDebug("Received JSON: {Json}", json);
                    OnMessage?.Invoke(this, json);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Receive loop stopped.");
            OnClose?.Invoke(this, EventArgs.Empty);
        }
    }


    private static IEnumerable<string> SplitJsonMessages(string input)
    {
        int depth = 0;
        int start = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '{') depth++;
            if (input[i] == '}') depth--;

            if (depth == 0 && i >= start)
            {
                var json = input.Substring(start, i - start + 1).Trim();
                if (json.Length > 0)
                    yield return json;

                start = i + 1;
            }
        }
    }
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_ws is null || _ws.State != WebSocketState.Open)
        {
            _logger.LogWarning("WebSocket is not connected. Send skipped.");
            throw new InvalidOperationException("WebSocket is not connected.");
        }

        var payload = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(payload);

        try
        {
            await _ws.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Sent message: {Message}", message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SendMessageAsync cancelled.");
            throw;
        }
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "Closing", CancellationToken cancellationToken = default)
    {
        if (_ws is null)
            return;

        try
        {
            if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived)
            {
                _logger.LogDebug("Closing WebSocket: {Status} {Desc}", closeStatus, statusDescription);
                await _ws.CloseAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (WebSocketException)
        {
            // Expected: server closed without handshake
            _logger.LogWarning("WebSocketException while closing WebSocket.");

        }
        catch (IOException)
        {
            // Expected: server dropped TCP
            _logger.LogWarning("IOException while closing WebSocket.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception while closing WebSocket.");
            //_logger.LogDebug(ex, "Exception while closing WebSocket.");
        }
        finally
        {
            try
            {
                _receiveCts?.Cancel();
                if (_receiveTask is not null)
                    await _receiveTask.ConfigureAwait(false);
            }
            catch { /* swallow */ }

            try
            {
                _ws.Dispose();
            }
            catch { /* swallow */ }

            _ws = null;
            _receiveTask = null;
            _receiveCts?.Dispose();
            _receiveCts = null;

            OnClose?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        try
        {
            _receiveCts?.Cancel();
        }
        catch { OnClose?.Invoke(this, EventArgs.Empty); }

        try
        {
            _ws?.Dispose();
        }
        catch { OnClose?.Invoke(this, EventArgs.Empty); }

        _receiveCts?.Dispose();
    }
}
