
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.WithCaller;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Extensions.Logging;
using Serilog.Settings.Configuration;
using Serilog.Sinks.SystemConsole.Themes;
using WaveLink.Plugin.Models;
using WaveLink.SDK;
using WaveLink.SDK.Models;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

var baseTheme = AnsiConsoleTheme.Code;

var levelString =
        config["Serilog:MinimumLevel:Default"] ??
        config["Serilog:MinimumLevel"];
var levelSwitch = new LoggingLevelSwitch(Enum.TryParse<LogEventLevel>(levelString, true, out var level)
        ? level
        : LogEventLevel.Warning);
var waveSocketSwitch = new LoggingLevelSwitch(LogEventLevel.Fatal + 1);
var fileLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Fatal + 1);

LoggerConfiguration loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .MinimumLevel.ControlledBy(levelSwitch)
    .MinimumLevel.Override("WaveLink.SDK.WaveSocket", waveSocketSwitch)
    .WriteTo.File(
        path: "logs/response-.txt",
        rollingInterval: RollingInterval.Day,
        levelSwitch: fileLevelSwitch,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}:{LineNumber} -- {Message:lj}{NewLine}{Exception}"
    )
    .Enrich.WithCaller(true, 1);

Log.Logger = loggerConfig.CreateLogger();

ILoggerFactory loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(dispose: true);
});

WaveLink.Plugin.Statics.PluginId = config["PluginId"]?.Trim() ?? WaveLink.Plugin.Statics.PluginId;

WaveLinkPlugin plugin = new(loggerFactory)
{
    LogLevelSwitch = levelSwitch,
    FileLevelSwitch = fileLevelSwitch,
    WaveSocketSwitch = waveSocketSwitch

};
plugin.Run();