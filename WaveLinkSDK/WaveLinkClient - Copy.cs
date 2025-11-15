//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Diagnostics.Tracing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using WaveLinkSDK.Interfaces;
//using WebSocketSharp;

//namespace WaveLinkSDK;

//public class WaveLinkClient : IWaveLinkClient
//{
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly ILogger _logger;
//    private WebSocket _ws;
//    public string Url { get; set; }
//    public int Port { get; set; } // 28196
//    public string Uuid { get; set; } = Guid.NewGuid().ToString("N").ToLower();
//    public WaveLinkClient(ILoggerFactory? loggerFactory = null) : this(Statics.Localhost, 1884, loggerFactory) { }
//    public WaveLinkClient(string uri, int port, ILoggerFactory? loggerFactory = null)
//    {
//        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
//        _logger = _loggerFactory.CreateLogger<WaveLinkClient>();
//        Url = $"ws://{uri}:{port.ToString()}";
//        _ws = new(Url);

//        // Modify the Origin header to match the working app's Origin header
//        //_ws.Headers.Add("Origin", "streamdeck://");

//        //// Add the Sec-WebSocket-Extensions header to match the working app's request
//        //_ws.Headers.Add("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
//        // Origin header
//        //_ws.Origin = "streamdeck://";

//        // Subprotocols
//        //_ws.Protocol = "my-protocol";

//        // Cookies
//        _ws.SetCookie(new WebSocketSharp.Net.Cookie("Origin", "streamdeck://"));
//        _ws.SetCookie(new WebSocketSharp.Net.Cookie("Sec-WebSocket-Extensions", Uri.EscapeDataString("permessage-deflate; client_max_window_bits")));


//        // User-Agent (limited)
//        //_ws.CustomHeaders["User-Agent"] = "";

//        _ws.OnOpen += (sender, e) =>
//        {
           
//            _logger.LogInformation("WebSocket connected to {Url}", Url);
//            var payload = new
//            {
//                @event = "registerPlugin",
//                uuid = Uuid
//            };

//            string json = JsonConvert.SerializeObject(payload);
//            //Send(json);
//        };
//        _ws.OnMessage += (sender, e) => OnMessage(sender, e);
//        _ws.OnClose += (sender, e) => OnClose(sender, e);
//        _ws.OnError += (sender, e) => OnError(sender, e);
//    }
//    public void Connect() => _ws.Connect();
//    public void Close() => _ws.Close();

//    public void SetUpEvents()
//    {
//        throw new NotImplementedException();
//    }

//    public void Send(string message)
//    {
//        _logger.LogInformation("Sendng {message}", message);
//        _ws.Send(message);
//    }

//    public void OnMessage(object? sender, EventArgs e)
//    {
//        var eventArgs = e as MessageEventArgs;
//        _logger.LogInformation("OnMessage {data}", eventArgs?.Data);
//    }

//    public void OnError(object? sender, EventArgs e)
//    {
//        var eventArgs = e as MessageEventArgs;
//        _logger.LogInformation("OnError {data}", eventArgs?.Data);
//    }

//    public void OnClose(object? sender, EventArgs e)
//    {
//        var eventArgs = e as MessageEventArgs;
//        _logger.LogInformation("OnClose {data}", e);
//    }
//}
