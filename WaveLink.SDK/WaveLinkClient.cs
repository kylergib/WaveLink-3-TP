using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WaveLink.SDK.Interfaces;
using WaveLink.SDK.Models;

namespace WaveLink.SDK;

public class WaveLinkClient : IWaveLinkClient
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    private readonly WaveSocket WaveSocket;

    // handle connection events
    public event EventHandler? OnConnection;
    public event EventHandler<string>? OnMessage;
    public event EventHandler? OnClose;

    public WaveLinkMessageRouter MessageRouter { get; set; }

    public WaveLinkStateManager StateManager { get; set; }

    public string Url => WaveSocket.Url;
    public int Port => WaveSocket.Port;
    

    public bool IsConnected => WaveSocket.IsConnected;

    public WaveLinkClient(ILoggerFactory? loggerFactory = null) : this(Statics.Localhost, Statics.DefaultPort, loggerFactory) { }
    public WaveLinkClient(string uri, int port, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<WaveLinkClient>();

        WaveSocket = new(uri, port, _loggerFactory.CreateLogger<WaveSocket>());
        WaveSocket.OnConnection += (s, e) => OnConnection?.Invoke(this, e);
        WaveSocket.OnMessage += (s, msg) => HandleReceivedMessage(msg);
        //WaveSocket.OnClose += (s, e) => OnClose?.Invoke(this, e);
        MessageRouter = new WaveLinkMessageRouter(_loggerFactory.CreateLogger<WaveLinkMessageRouter>());
        WaveSocket.OnClose += (s, e) => HandleClose(e);
        StateManager = new WaveLinkStateManager(_loggerFactory.CreateLogger<WaveLinkStateManager>(), MessageRouter);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default) => await WaveSocket.ConnectAsync(cancellationToken);
    private void HandleReceivedMessage(string message)
    {
        try
        {
            JsonDocument doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            bool isResult = root.TryGetProperty("result", out var resultElement);
            bool isMethod = root.TryGetProperty("method", out var methodElement);

            if (isResult)
            {
                int id = root.GetProperty("id").GetInt32();
                WaveRequestId idEnum = (WaveRequestId)id;
                 _logger.LogDebug("Received Result: {result}", idEnum.ToString());
                MessageRouter.Route(idEnum, message);
            }
            else if (isMethod)
            {
                string method = root.GetProperty("method").GetString() ?? string.Empty;
                ReceivedMethods methodEnum = Enum.Parse<ReceivedMethods>(method);
                 _logger.LogDebug("Received Method: {method}", method);
                MessageRouter.Route(methodEnum, message);
            }
            else
            {
                _logger.LogError("Received message is neither a result nor a method call:");
                _logger.LogDebug("{message}", message.Minify());
            }
            OnMessage?.Invoke(this, message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error converting message");
            _logger.LogDebug("{message}", message);
            _logger.LogDebug(ex.StackTrace);
        }
    }
    public async Task SendRequestAsync(string request)
    {
        if (string.IsNullOrEmpty(request))
        {
            _logger.LogWarning("Attempted to send empty JSON request.");
            throw new ArgumentException("JSON request is empty.");
        }
        await WaveSocket.SendMessageAsync(request);
    }
    public async Task SendRequestAsync(WaveLinkRequest request)
    {
        var message = JsonSerializer.Serialize(request, Statics.JsonSerializerOptionsDefault);
        if (string.IsNullOrEmpty(message))
        {
            _logger.LogWarning("Attempted to send empty JSON request.");
            throw new ArgumentException("JSON request is empty.");
        }
       
        await SendRequestAsync(message);
    }
    public async Task SendRequestAsync(WaveLinkRequestMethod request)
    {
        var message = JsonSerializer.Serialize(request, Statics.JsonSerializerOptionsDefault);
        if (string.IsNullOrEmpty(message))
        {
            _logger.LogWarning("Attempted to send empty JSON request.");
            throw new ArgumentException("JSON request is empty.");
        }
        await SendRequestAsync(message);
    }
    public async Task SendRequestAsync<T>(WaveLinkSendMethod<T> request)
    {
        // set send count as id and add to dictionary
        request.Id = StateManager.SendCount;
        StateManager.SendCount++;
        StateManager.SendState.Add(request.Id, request);

        var message = JsonSerializer.Serialize(request, Statics.JsonSerializerOptionsDefault);
        if (string.IsNullOrEmpty(message))
        {
            _logger.LogWarning("Attempted to send empty JSON request.");
            throw new ArgumentException("JSON request is empty.");
        }
        await SendRequestAsync(message);
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "Closing", CancellationToken cancellationToken = default)
    {
       try
        {
             await WaveSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        } catch (WebSocketException ex)
        {
            _logger.LogWarning("WebSocket closed abruptly");
        }
        catch (IOException ex)
        {
            _logger.LogWarning("Transport closed abruptly");
        }
    }
    private TaskCompletionSource _closeTcs = new();

    public Task WaitForCloseAsync() => _closeTcs.Task;

    private void HandleClose(EventArgs e)
    {
        OnClose?.Invoke(this, e);
        _closeTcs.TrySetResult();
        // now the reconnect loop can continue
    }
}