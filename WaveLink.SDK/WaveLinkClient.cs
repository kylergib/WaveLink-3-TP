using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using WaveLink.SDK.Interfaces;
using WaveLink.SDK.Models;

namespace WaveLink.SDK;

public class WaveLinkClient : IWaveLinkClient
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    private readonly WaveSocket WaveSocket;

    public event EventHandler? OnConnection;
    public event EventHandler<string>? OnMessage;
    public event EventHandler? OnClose;

    public event EventHandler<ApplicationInfoResponse>? OnReceivedAppInfo;

    public string Url => WaveSocket.Url;
    public int Port => WaveSocket.Port;
    // leave i want to try to add multiple wave link connections
    public string Uuid { get; init; } = Guid.NewGuid().ToString("N").ToLower();

    public bool IsConnected => WaveSocket.IsConnected;

    public WaveLinkClient(ILoggerFactory? loggerFactory = null) : this(Statics.Localhost, Statics.DefaultPort, loggerFactory) { }
    public WaveLinkClient(string uri, int port, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<WaveLinkClient>();
        WaveSocket = new(uri, port, _loggerFactory.CreateLogger<WaveSocket>());
        WaveSocket.OnConnection += (s, e) => OnConnection?.Invoke(this, e);
        WaveSocket.OnMessage += (s, msg) => HandleReceivedMessage(msg);
        WaveSocket.OnClose += (s, e) => OnClose?.Invoke(this, e);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default) => await WaveSocket.ConnectAsync(cancellationToken);
    private void HandleReceivedMessage(string message)
    {
        JsonDocument doc = JsonDocument.Parse(message);
        var root = doc.RootElement;
        bool isResult = root.TryGetProperty("result", out var resultElement);
        bool isMethod = root.TryGetProperty("method", out var methodElement);
        _logger.LogDebug("isResult: {isResult}", isResult);
        _logger.LogDebug("isMethod: {isMethod}", isMethod);

        // is part of setup i think?
        if (isResult)
        {
            int id = root.GetProperty("id").GetInt32();

            switch ((WaveRequestId)id)
            {
                case WaveRequestId.getApplicationInfo:
                    var appInfo = JsonSerializer.Deserialize<ApplicationInfoResponse>(message);
                    _logger.LogDebug("Received Application Info: {AppInfo}", appInfo?.Result);
                    if (appInfo != null) OnReceivedAppInfo?.Invoke(this, appInfo);
                    break;

                default:
                    _logger.LogWarning("Received result that is not recognized:");
                    _logger.LogWarning("{message}", message.Minify());
                    break;
            }
        } else
        {
            _logger.LogWarning("Received message is neither a result nor a method call:");
            _logger.LogWarning("{message}", message.Minify());

        }
        OnMessage?.Invoke(this, message);
    }
    public async Task SendJsonRequestAsync(JsonRpcRequest request)
    {
        var message = request.ToJsonString();
        if (string.IsNullOrEmpty(message))
        {
            _logger.LogWarning("Attempted to send empty JSON request.");
            throw new ArgumentException("JSON request is empty.");
        }
        await WaveSocket.SendMessageAsync(message);
    }
    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "Closing", CancellationToken cancellationToken = default)
    {
        await WaveSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
    }

    /// <summary>
    /// No-op placeholder to preserve compatibility with older callers/tests.
    /// Use events directly.
    /// </summary>
    public void SetUpEvents() { /* intentionally left blank; wire to OnMessage/OnConnection/OnClose */ }
}
public static class JsonExtensions
{
    public static string Minify(this string json)
    {
        return JsonNode.Parse(json)?.ToJsonString() ?? json;
    }
}