using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.WithCaller;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Extensions.Logging;
using Serilog.Settings.Configuration;
using Serilog.Sinks.SystemConsole.Themes;
using WaveLink.SDK;
using WaveLink.SDK.Models;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string localhost = config["WaveLink:Host"] ?? Statics.Localhost;
string macHost = "192.168.1.8";


int port = int.Parse(config["WaveLink:Port"] ?? Statics.DefaultPort.ToString());

// Clone the default
var baseTheme = AnsiConsoleTheme.Code;  // looks close to your screenshot

//var CustomTheme = new AnsiConsoleTheme(
//    new Dictionary<ConsoleThemeStyle, string>(baseTheme.)
//    {
//        [ConsoleThemeStyle.LevelDebug] = "\x1b[94m" // bright blue debug
//    });

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .Enrich.WithCaller(true, 1)
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
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
ILoggerFactory loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(dispose: true);
});

//var _logger = loggerFactory.CreateLogger("WaveLinkConsoleApp");
var _logger = loggerFactory.CreateLogger<Helpers>();
Helpers.WriteHeader();

Helpers helper = new();
_ = Task.Run(() => helper.Start(_logger, loggerFactory, localhost, port));
//Helpers helperMac = new();
//_ = Task.Run(() => helperMac.Start(_logger, loggerFactory, macHost, port));


while (true)
{
    var key = Console.ReadLine();
    if (key != null && key.ToLower() == "exit")
    {
        break;
    }
    switch (key)
    {
        case "stopSub":
            WaveLinkSendMethod<MethodSubscriptionInfo> subscribe = new(WaveLinkMethod.setSubscription, new() { FocusedAppChanged = new() { IsEnabled = false } });
            _ = helper.client?.SendRequestAsync<MethodSubscriptionInfo>(subscribe);
            break;
        case "addToChannel":
            var channel2 = helper.client?.StateManager.Channels.Find(c => c.Id == "PCM_OUT_00_V_02_SD2");
            var appToAdd = helper.client?.StateManager.FocusedApp;
            if (appToAdd != null && channel2 != null)
            {
                WaveLinkSendMethod<AddAppToChannelInfo> addToChannelRequest = new(WaveLinkMethod.addToChannel, new() { ChannelId = channel2.Id, AppId = appToAdd.Id });
                _ = helper.client?.SendRequestAsync<AddAppToChannelInfo>(addToChannelRequest);
            }
            break;
        case "setChannel":
            var channel = helper.client?.StateManager.Channels.Find(c => c.Id == "PCM_OUT_00_V_02_SD2");
            if (channel != null)
            {
                //channel.IsMuted = !channel.IsMuted;
                WaveLinkSendMethod<MethodChannelInfo> setRequest = new(WaveLinkMethod.setChannel, new() { Id = channel.Id, IsMuted = !channel.IsMuted ?? false, Level = 1m});
                _ = helper.client?.SendRequestAsync<MethodChannelInfo>(setRequest);
            }
            
            break;
         case "setInputDevice":
            var inputDevice = helper.client?.StateManager.InputDevices.Find(c => c.Id == "{0.0.1.00000000}.{ceb28ad9-8935-4632-8353-043f4f1bbe99}");
            if (inputDevice != null)
            {
                MethodInputInfo inputInfo = new()
                {
                    Id = inputDevice?.Inputs?[0].Id ?? string.Empty,
                    IsMuted = !inputDevice?.Inputs?[0].IsMuted ?? false
                };
                WaveLinkSendMethod<MethodInputDeviceInfo> setInputDeviceRequest = new(WaveLinkMethod.setInputDevice, new() { Id = inputDevice!.Id, Inputs = [inputInfo] });
                _ = helper.client?.SendRequestAsync<MethodInputDeviceInfo>(setInputDeviceRequest);
            }
            
            break;
         case "setOutputDevice":
            var outputDevice = helper.client?.StateManager.OutputDevices.Find(c => c.Id == "{0.0.0.00000000}.{9e81e019-4e19-419f-a9e8-395ca09c3ed3}");
            if (outputDevice != null)
            {
                MethodOutputInfo outputInfo = new()
                {
                    Id = outputDevice?.Outputs?[0].Id ?? string.Empty,
                    //IsMuted = !outputDevice?.Outputs?[0].IsMuted ?? false
                    //Level = 1.0m
                    MixId = "PCM_IN_01_V_00_SD1" // adds the output to the mix, does not change the default though
                };
                MethodOutputDeviceParamInfo deviceParam = new() { Id = outputDevice!.Id, Outputs = [outputInfo] };

                WaveLinkSendMethod<MethodOutputDeviceInfo> setOutputDeviceRequest = new(WaveLinkMethod.setOutputDevice, new() { OutputDevice = deviceParam });
                _ = helper.client?.SendRequestAsync<MethodOutputDeviceInfo>(setOutputDeviceRequest);
            }
            break;
         case "setMixLevel":
            var mix = helper.client?.StateManager.Mixes.Find(c => c.Id == "PCM_IN_01_V_00_SD1");
            if (mix != null)
            {
                //channel.IsMuted = !channel.IsMuted;
                WaveLinkSendMethod<MethodMixInfo> setRequest2 = new(WaveLinkMethod.setMix, new() { Id = mix.Id, Level = 0.3m});
                _ = helper.client?.SendRequestAsync<MethodMixInfo>(setRequest2);
            }
            
            break;
         case "setMixMute":
            var mix2 = helper.client?.StateManager.Mixes.Find(c => c.Id == "PCM_IN_01_V_00_SD1");
            if (mix2 != null)
            {
                //channel.IsMuted = !channel.IsMuted;
                WaveLinkSendMethod<MethodMixInfo> setRequest3 = new(WaveLinkMethod.setMix, new() { Id = mix2.Id, IsMuted = !mix2.IsMuted ?? false, Level = 0.3m});
                _ = helper.client?.SendRequestAsync<MethodMixInfo>(setRequest3);
            }
            
            break;
    }
}
