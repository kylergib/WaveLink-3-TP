using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;
using WaveLink.SDK;
using WaveLink.SDK.Models;


var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string host = config["WaveLink:Host"] ?? Statics.Localhost;

int port = int.Parse(config["WaveLink:Port"] ?? Statics.DefaultPort.ToString());

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .WriteTo.Conditional(
        evt =>
            evt.Level == LogEventLevel.Debug &&
            evt.Properties.ContainsKey("SourceContext") &&
            evt.Properties["SourceContext"].ToString().Contains("WaveLinkClient.Response"),
        wt => wt.File(
            "logs/response-debug-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
        )
    )
    .CreateLogger();

// Create ILoggerFactory with ONLY Serilog
ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(dispose: true);
});

var _logger = loggerFactory.CreateLogger("WaveLinkConsoleApp");
Helpers helper = new();
Helpers.WriteHeader();
await helper.Start(_logger, loggerFactory, host, port);

//while (clien)
//client.OnClose += (e, s) =>
//{
//    _logger.LogInformation("Client closed");

//}

Console.ReadLine();