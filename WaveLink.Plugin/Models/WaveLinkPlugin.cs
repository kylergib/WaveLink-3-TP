using System;
using System.Diagnostics;
using System.Runtime;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using Serilog.Events;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;
using WaveLink.Plugin;
using WaveLink.SDK;
using WaveLink.SDK.Models;
using static System.Net.Mime.MediaTypeNames;
namespace WaveLink.Plugin.Models;

public class WaveLinkPlugin : ITouchPortalEventHandler
{
    public string PluginId => Statics.PluginId;
    public int PluginVersion { get; set; }
    public string UpdateUrl { get; set; } = string.Empty;

    public readonly ITouchPortalClient _client;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<WaveLinkPlugin> _logger;
    private IReadOnlyCollection<Setting> _settings;
    //private WaveLinkHandler? WaveLinkHandler = null;

    // settings values
    public string LogLevel { get; set; } = "none";
    public bool SaveToFile { get; set; } = false;
    public string IpAddress { get; set; } = WaveLink.SDK.Statics.Localhost;
    public bool SubscribeToFocusedApp { get; set; } = true;
    public List<string> IpList = new();

    public EventHandler<WaveLinkStateEvent<SDK.Models.Channel>>? OnChannelUpdated;
    public EventHandler<App>? OnFocusedAppUpdated;
    public EventHandler<WaveLinkStateEvent<InputDevice>>? OnInputDeviceUpdated;
    public EventHandler<WaveLinkStateEvent<Mix>>? OnMixUpdated;
    public EventHandler<WaveLinkStateEvent<OutputDevice>>? OnOutputDeviceUpdated;


    public EventHandler<WaveLinkStateEvent<SDK.Models.Channel>>? OnChannelAdded;
    public EventHandler<WaveLinkStateEvent<InputDevice>>? OnInputDeviceAdded;
    public EventHandler<WaveLinkStateEvent<Mix>>? OnMixAdded;
    public EventHandler<WaveLinkStateEvent<OutputDevice>>? OnOutputDeviceAdded;

    public EventHandler<WaveLinkStateEvent<SDK.Models.Channel>>? OnChannelRemoved;
    public EventHandler<WaveLinkStateEvent<InputDevice>>? OnInputDeviceRemoved;
    public EventHandler<WaveLinkStateEvent<Mix>>? OnMixRemoved;
    public EventHandler<WaveLinkStateEvent<OutputDevice>>? OnOutputDeviceRemoved;

    public EventHandler<bool>? OnSubscribedToFocusAppChanged;

    // router events
    public EventHandler<WaveLinkResponse<InputDeviceResult>>? OnReceivedGetInputDevices;
    public EventHandler<WaveLinkResponse<OutputDeviceResult>>? OnReceivedGetOutputDevices;
    public EventHandler<WaveLinkResponse<ChannelsResult>>? OnReceivedGetChannels;
    public EventHandler<WaveLinkResponse<MixesResult>>? OnReceivedGetMixes;

    public LoggingLevelSwitch? LogLevelSwitch { get; set; }
    public LoggingLevelSwitch? FileLevelSwitch { get; set; }
    public LoggingLevelSwitch? WaveSocketSwitch { get; set; }

    // name to short id list mapping
    public Dictionary<string, List<string>> InputShortConnectorIds { get; set; } = new();
    public Dictionary<string, List<string>> OutputShortConnectorIds { get; set; } = new();
    public Dictionary<string, List<string>> ChannelShortConnectorIds { get; set; } = new();
    public Dictionary<string, List<string>> MixShortConnectorIds { get; set; } = new();

    // dictionary for additional wave link instances
    public Dictionary<string, WaveLinkHandler> AdditionalWaveLinkInstances { get; set; } = new();
    // key to ip address mapping
    //public Dictionary<string, string> AdditionalWaveLinkInstancesIpAddress { get; set; } = new();

    //public Dictionary<int, string> WaveLinkNumToIpAddress { get; set; } = new();
    //public Dictionary<string, int> IpAddressToWaveLinkNum { get; set; } = new();

    public WaveLinkPlugin(ILoggerFactory logFactory)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(baseDir, "entry.tp");
        string json = File.ReadAllText(filePath);
        using JsonDocument doc = JsonDocument.Parse(json);
        PluginVersion = doc.RootElement.GetProperty("version").GetInt32();
        //var test = await UpdateAvailable();
        //var test = Task.Run(() => UpdateAvailable());
        _client = TouchPortalFactory.CreateClient(this);
        _loggerFactory = logFactory;
        _logger = logFactory?.CreateLogger<WaveLinkPlugin>() ?? throw new ArgumentNullException(nameof(logFactory));
    }

    public void Run()
    {
        _client.Connect();
    }

    // Event received when plugin connects to Touch Portal.
    public void OnInfoEvent(InfoEvent message)
    {
        _logger.LogInformation(
          "[InfoEvent] VersionCode: '{TpVersionCode}', VersionString: '{TpVersionString}', SDK: '{SdkVersion}', PluginVersion: '{PluginVersion}', Status: '{Status}'",
          message.TpVersionCode, message.TpVersionString, message.SdkVersion, message.PluginVersion, message.Status
        );

        _settings = message.Settings;

        var subscribeToFocusAppValue = _settings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.SubscribeToFocusedApp)?.Value;
        var saveToFileValue = _settings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.SaveToFile)?.Value;

        IpAddress = _settings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.IPAddress)?.Value ?? WaveLink.SDK.Statics.Localhost;
        LogLevel = _settings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.LogLevel)?.Value ?? "none";
        if (LogLevel.ToLower() == "debug")
        {
            LogLevelSwitch?.MinimumLevel = LogEventLevel.Debug;
            WaveSocketSwitch?.MinimumLevel = LogEventLevel.Debug;
        }
        else if (LogLevel.ToLower() == "information")
        {
            LogLevelSwitch?.MinimumLevel = LogEventLevel.Information;
            WaveSocketSwitch?.MinimumLevel = LogEventLevel.Information;
        }
        else if (LogLevel.ToLower() == "warning")
        {
            LogLevelSwitch?.MinimumLevel = LogEventLevel.Warning;
            WaveSocketSwitch?.MinimumLevel = LogEventLevel.Fatal + 1;
        }


        SaveToFile = saveToFileValue is "1" or "true" or "True" or "on" or "On";
        SubscribeToFocusedApp = subscribeToFocusAppValue is "1" or "true" or "True" or "on" or "On";

        if (SaveToFile)
        {
            FileLevelSwitch?.MinimumLevel = LogEventLevel.Verbose;
            LogLevelSwitch?.MinimumLevel = LogEventLevel.Verbose;
            WaveSocketSwitch?.MinimumLevel = LogEventLevel.Verbose;
        }

        var ipAddresses = _settings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.AdditionalAddresses)?.Value ?? string.Empty;

        var dict = string.IsNullOrEmpty(ipAddresses) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(ipAddresses)!;

        _logger.LogDebug("Found additional addresses: {0}", string.Join(", ", dict.Values));
        if (!AdditionalWaveLinkInstances.TryGetValue("default", out var waveHandler))
        {
            AdditionalWaveLinkInstances.Add("default", new(_loggerFactory, IpAddress, WaveLink.SDK.Statics.DefaultPort));
            IpList.Add(IpAddress);
        }
        foreach (var kvp in dict)
        {
            Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
            if (string.IsNullOrEmpty(kvp.Value.Trim()) || kvp.Value.Trim() == WaveLink.SDK.Statics.Localhost) continue;
            if (IpList.Contains(kvp.Value)) continue;

            IpList.Add(kvp.Key);
            if (!AdditionalWaveLinkInstances.TryGetValue(kvp.Key, out var handler))
            {
                AdditionalWaveLinkInstances.Add(kvp.Key, new WaveLinkHandler(_loggerFactory, kvp.Value.Trim(), WaveLink.SDK.Statics.DefaultPort, kvp.Key));
                IpList.Add(kvp.Value.Trim());
            }
        }

        var updateAvailable = Task.Run(() => UpdateAvailable());
        updateAvailable.Wait();
        if (!string.IsNullOrEmpty(updateAvailable.Result) && !string.IsNullOrEmpty(UpdateUrl))
        {
            _logger.LogWarning("Update is available: {0}", updateAvailable);
            _client.ShowNotification(
               TouchPortalIdHelper.UpdateNotificationId,
               $"Update available: {{updateAvailable}}",
               "A new version of the Wave Link Plugin is available.",
               [new() { Id = "learnMore", Title = "Learn More" }]
           );
        }

        ConnectToWaveLink();
    }
    public void ConnectToWaveLink()
    {

        //WaveLinkHandler = new(_loggerFactory, IpAddress, WaveLink.SDK.Statics.DefaultPort);
        //WaveLinkHandler.SubscribeToFocusApp = SubscribeToFocusedApp;
        //WaveLinkHandler?.Start();
        //WaveLinkHandler?.OnConnection += (sender, args) =>
        //{
        //    InitializeEventHandler("");
        //    SubscribeToEvents(WaveLinkHandler);
        //    _client.StateUpdate(TouchPortalIdHelper.IsConnectedToWaveLinkId(), "true");
        //};
        //WaveLinkHandler?.OnClose += (sender, args) =>
        //{
        //    _client.StateUpdate(TouchPortalIdHelper.IsConnectedToWaveLinkId(), "false");
        //};
        foreach (var kvp in AdditionalWaveLinkInstances)
        {
            _logger?.LogInformation("Trying to connect to Wave Link on: {IpAddress} for key: {key}", IpAddress, kvp.Key);
            var kvpHandler = kvp.Value;
            if (kvpHandler != null)
            {
                // TODO:
            }
            kvpHandler?.SubscribeToFocusApp = kvp.Key == "default" ? SubscribeToFocusedApp : false;
            kvpHandler?.Start();
            kvpHandler?.OnConnection += (sender, args) =>
            {
                InitializeEventHandler(kvp.Key);
                SubscribeToEvents(kvpHandler);
                if (kvp.Key != "default") _client.StateUpdate(TouchPortalIdHelper.IsConnectedToWaveLinkId(), "true");
            };
            kvpHandler?.OnClose += (sender, args) =>
            {
                if (kvp.Key != "default") _client.StateUpdate(TouchPortalIdHelper.IsConnectedToWaveLinkId(), "false");
            };
        }
    }
    public async Task DisconnectFromWaveLink()
    {
        foreach (var kvp in AdditionalWaveLinkInstances)
        {
            _logger?.LogInformation("Trying to connect to Wave Link on: {IpAddress} for key: {key}", IpAddress, kvp.Key);
            var kvpHandler = kvp.Value;
            if (kvpHandler is null || kvpHandler.Client is null) return;
            kvpHandler.Retry = false;
            await kvpHandler!.Client!.CloseAsync();

            kvpHandler = null;
        }
    }

    // Event triggered when one of this plugin's actions, defined in entry.tp, is triggered.
    public void OnActionEvent(ActionEvent message)
    {
        _logger.LogDebug("{@message}", message);
        TouchPortalIdHelper.ActionId(nameof(SetMuteInput));
        // action id == com.kylergib.wavelink_3.WaveLinkOutputs.action.SetOutputMute
        switch (message.ActionId)
        {
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetMuteInput)):
                SetMuteInput(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetLevelInput)):
                SetLevelInput(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetMuteOutput)):
                SetMuteOutput(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetLevelOutput)):
                SetLevelOutput(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetMuteChannel)):
                SetMuteChannel(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetOuputDevice)):
                SetOuputDevice(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetLevelChannel)):
                SetLevelChannel(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetMuteMix)):
                SetMuteMix(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetLevelMix)):
                SetLevelMix(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(SetFocusAppNotification)):
                SetFocusAppNotification(message);
                break;
            case var _ when message.ActionId == TouchPortalIdHelper.ActionId(nameof(AddToChannel)):
                SetMuteInput(message);
                break;
            default:
                _logger.LogWarning("Action not implemented");
                break;
        }

    }
    public void OnUnhandledEvent(string jsonMessage)
    {
        _logger?.LogWarning($"Unhandled message: {jsonMessage}");
    }
    public void OnListChangedEvent(ListChangeEvent message)
    {
        _logger?.LogDebug($"[OnListChanged] {message.ListId}/{message.ActionId}/{message.InstanceId} '{message.Value}'");

        //switch (message.ListId)
        //{
        //    //Dynamically updates the dropdown of data3 based on value chosen from data2 dropdown:
        //    case "category1.action1.data2" when message.InstanceId is not null:
        //        var prefix = message.Value;
        //        _client.ChoiceUpdate("category1.action1.data3", new[] { $"{prefix} second 1", $"{prefix} second 2", $"{prefix} second 3" }, message.InstanceId);
        //        break;
        //}
    }
    public void OnBroadcastEvent(BroadcastEvent message)
    {
        //Use this to reapply all state... Some times if you update the state, and the page is not visible, it will not be reflected in the app.
        _logger?.LogDebug($"[Broadcast] Event: '{message.Event}', PageName: '{message.PageName}'");
    }
    public async void OnSettingsEvent(SettingsEvent message)
    {
        var newSettings = message.Values;
        var subscribeToFocusAppValue = newSettings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.SubscribeToFocusedApp)?.Value;
        var saveToFileValue = newSettings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.SaveToFile)?.Value;

        var ipAddress = newSettings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.IPAddress)?.Value ?? WaveLink.SDK.Statics.Localhost;
        var logLevel = newSettings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.LogLevel)?.Value ?? "none";

        var saveToFile = saveToFileValue is "1" or "true" or "True" or "on" or "On";
        var subscribeToFocusedApp = subscribeToFocusAppValue is "1" or "true" or "True" or "on" or "On";

        _settings = newSettings;
        _logger?.LogDebug($"[OnSettings] Settings: {JsonSerializer.Serialize(_settings)}");

        if (LogLevel != logLevel)
        {
            LogLevel = logLevel;
            if (LogLevel.ToLower() == "debug")
            {
                LogLevelSwitch?.MinimumLevel = LogEventLevel.Debug;
                WaveSocketSwitch?.MinimumLevel = LogEventLevel.Debug;
            }
            else if (LogLevel.ToLower() == "information")
            {
                LogLevelSwitch?.MinimumLevel = LogEventLevel.Information;
                WaveSocketSwitch?.MinimumLevel = LogEventLevel.Information;
            }
            else if (LogLevel.ToLower() == "warning")
            {
                LogLevelSwitch?.MinimumLevel = LogEventLevel.Warning;
                WaveSocketSwitch?.MinimumLevel = LogEventLevel.Fatal + 1;
            }
        }

        if (SaveToFile != saveToFile)
        {
            SaveToFile = saveToFile;
            FileLevelSwitch?.MinimumLevel = SaveToFile ? LogEventLevel.Verbose : LogEventLevel.Fatal + 1;
            LogLevelSwitch?.MinimumLevel = SaveToFile ? LogEventLevel.Verbose : LogLevelSwitch?.MinimumLevel ?? LogEventLevel.Warning;
            WaveSocketSwitch?.MinimumLevel = SaveToFile ? LogEventLevel.Verbose : WaveSocketSwitch?.MinimumLevel ?? LogEventLevel.Warning;
        }

        if (IpAddress != ipAddress)
        {
            //await DisconnectFromWaveLink();
            IpAddress = ipAddress;
            //ConnectToWaveLink();
        }

        //if (SubscribeToFocusedApp != subscribeToFocusedApp)
        //{
        //    SubscribeToFocusedApp = subscribeToFocusedApp;
        //    WaveLinkHandler?.SubscribeToFocusApp = SubscribeToFocusedApp;
        //    WaveLinkSendMethod<MethodSubscriptionInfo> subscribe = new(WaveLinkMethod.setSubscription, new() { FocusedAppChanged = new() { IsEnabled = SubscribeToFocusedApp } });
        //    _ = WaveLinkHandler?.Client?.SendRequestAsync<MethodSubscriptionInfo>(subscribe);
        //}

        var ipAddresses = _settings.FirstOrDefault(s => s.Name == TouchPortalIdHelper.AdditionalAddresses)?.Value ?? string.Empty;
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(ipAddresses)!;
        //foreach (var kvp in dict)
        //{
        //    Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
        //    if (string.IsNullOrEmpty(kvp.Value.Trim()) || kvp.Value.Trim() == WaveLink.SDK.Statics.Localhost) continue;
        //    if (IpKeyList.Contains(kvp.Key)) continue;

        //    IpKeyList.Add(kvp.Key);
        //    if (!AdditionalWaveLinkInstances.TryGetValue(kvp.Key, out var handler))
        //    {
        //        AdditionalWaveLinkInstances.Add(kvp.Key, new WaveLinkHandler(_loggerFactory, kvp.Value, WaveLink.SDK.Statics.DefaultPort));
        //    }
        //}
        foreach (var kvp in dict)
        {
            Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
            if (string.IsNullOrEmpty(kvp.Value.Trim()) || kvp.Value.Trim() == WaveLink.SDK.Statics.Localhost) continue;
            if (IpList.Contains(kvp.Value.Trim())) continue;

            IpList.Add(kvp.Key);
            if (!AdditionalWaveLinkInstances.TryGetValue(kvp.Key, out var handler))
            {
                AdditionalWaveLinkInstances.Add(kvp.Key, new WaveLinkHandler(_loggerFactory, kvp.Value.Trim(), WaveLink.SDK.Statics.DefaultPort, kvp.Key));
                IpList.Add(kvp.Value.Trim());
                continue;
            }
            if (handler == null)
            {
                AdditionalWaveLinkInstances[kvp.Key] = new WaveLinkHandler(_loggerFactory, kvp.Value.Trim(), WaveLink.SDK.Statics.DefaultPort, kvp.Key);
            }
        }


        // remove unused 
        var needRemovedDict = AdditionalWaveLinkInstances
            .Where(kvp => !dict.ContainsKey(kvp.Key) && kvp.Key != "default")
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        foreach (var kvp in needRemovedDict)
        {
            IpList.Remove(kvp.Value.Host);
            if (AdditionalWaveLinkInstances.TryGetValue(kvp.Key, out var num))
            {
                AdditionalWaveLinkInstances.Remove(kvp.Key);
            }
        }
    }
    public void OnNotificationOptionClickedEvent(NotificationOptionClickedEvent message)
    {
        if (message.NotificationId == TouchPortalIdHelper.UpdateNotificationId && message.OptionId == "learnMore" && !string.IsNullOrEmpty(UpdateUrl))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = UpdateUrl.Trim(),
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open URL: {ex}");
            }
        }
        _logger?.LogDebug($"[OnNotificationOptionClickedEvent] NotificationId: '{message.NotificationId}', OptionId: '{message.OptionId}'");
        //if (message.NotificationId is "TouchPortal.SamplePlugin|update")
        //{
        //    switch (message.OptionId)
        //    {
        //        //Example for opening a web browser (windows):
        //        case "update":
        //            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        //            {
        //                UseShellExecute = true,
        //                FileName = "https://www.nuget.org/packages/TouchPortalSDK/"
        //            });
        //            break;
        //        case "readMore":
        //            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        //            {
        //                UseShellExecute = true,
        //                FileName = "https://github.com/oddbear/TouchPortalSDK/"
        //            });
        //            break;
        //    }
        //}
    }

    public void OnConnecterChangeEvent(ConnectorChangeEvent message)
    {
        _logger?.LogDebug($"[OnConnecterChangeEvent] ConnectorId: '{message.ConnectorId}', Value: '{message.Value}'");
        switch (message.ConnectorId)
        {
            case var _ when message.ConnectorId == TouchPortalIdHelper.InputVolumeConnector:
                SetLevelInput(message);
                break;
            case var _ when message.ConnectorId == TouchPortalIdHelper.OutputVolumeConnector:
                SetLevelOutput(message);
                break;
            case var _ when message.ConnectorId == TouchPortalIdHelper.ChannelVolumeConnector:
                SetLevelChannel(message);
                break;
            case var _ when message.ConnectorId == TouchPortalIdHelper.MixVolumeConnector:
                SetLevelMix(message);
                break;
        }
    }

    public void OnShortConnectorIdNotificationEvent(ShortConnectorIdNotificationEvent message)
    {
        _logger?.LogDebug($"[OnShortConnectorIdNotificationEvent] ConnectorId: '{message.ConnectorId}', ShortID: '{message.ShortId}'");
        if (message.ActualConnectorId == TouchPortalIdHelper.InputVolumeConnector)
        {
            var value = message.Data[TouchPortalIdHelper.InputListId()];
            if (string.IsNullOrEmpty(value)) return;
            if (!InputShortConnectorIds.TryGetValue(value, out var shortIdList))
            {
                shortIdList = new();
            }
            if (!shortIdList.Contains(message.ShortId))
            {
                shortIdList.Add(message.ShortId);
                InputShortConnectorIds.Add(value, shortIdList);
            }
        }
        else if (message.ActualConnectorId == TouchPortalIdHelper.OutputVolumeConnector)
        {
            var value = message.Data[TouchPortalIdHelper.OutputListId()];
            if (string.IsNullOrEmpty(value)) return;

            if (!OutputShortConnectorIds.TryGetValue(value, out var shortIdList))
            {
                shortIdList = new();
            }
            if (!shortIdList.Contains(message.ShortId))
            {
                shortIdList.Add(message.ShortId);
                OutputShortConnectorIds.Add(value, shortIdList);
            }
        }
        else if (message.ActualConnectorId == TouchPortalIdHelper.ChannelVolumeConnector)
        {
            var value = message.Data[TouchPortalIdHelper.ChannelListId()];
            if (string.IsNullOrEmpty(value)) return;

            if (!ChannelShortConnectorIds.TryGetValue(value, out var shortIdList))
            {
                shortIdList = new();
            }
            if (!shortIdList.Contains(message.ShortId))
            {
                shortIdList.Add(message.ShortId);
                ChannelShortConnectorIds.Add(value, shortIdList);
            }

        }
        else if (message.ActualConnectorId == TouchPortalIdHelper.MixVolumeConnector)
        {
            var value = message.Data[TouchPortalIdHelper.MixListId()];
            if (string.IsNullOrEmpty(value)) return;

            if (!MixShortConnectorIds.TryGetValue(value, out var shortIdList))
            {
                shortIdList = new();
            }
            if (!shortIdList.Contains(message.ShortId))
            {
                shortIdList.Add(message.ShortId);
                MixShortConnectorIds.Add(value, shortIdList);
            }
        }
    }

    public void InitializeEventHandler(string? waveKey = null)
    {
        OnReceivedGetInputDevices += async (s, response) =>
        {
            //var devices = response?.Result?.InputDevices ?? [];
            //List<string> deviceList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var channels = handler?.Client?.StateManager.InputDevices.Select(c => GetWavePrefix(kvp.Key) + c.Name).ToArray();

            //    deviceList.AddRange(channels ?? []);
            //}
            //_client.ChoiceUpdate(TouchPortalIdHelper.InputListId(), deviceList.ToArray());
            UpdateChoiceInputList();
            //_client.ChoiceUpdate(TouchPortalIdHelper.InputListId(), devices?.Select(d => GetWavePrefix(waveKey) + d.Name).ToArray());
            _logger.LogDebug("Received Input Devices Info:");
            foreach (var inputDevice in response?.Result?.InputDevices ?? new())
            {
                _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {GetWavePrefix(waveKey)}{inputDevice.Name}, Type: {inputDevice.Type}");
                foreach (var input in inputDevice?.Inputs ?? [])
                {
                    _client.CreateState(TouchPortalIdHelper.InputMute(GetWavePrefix(waveKey) + input.Name!), $"{GetWavePrefix(waveKey)}{input.Name!} muted", input.IsMuted.ToString(), TouchPortalIdHelper.InputMutedCategoryName);
                    _client.CreateState(TouchPortalIdHelper.InputLevel(GetWavePrefix(waveKey) + input.Name!), $"{GetWavePrefix(waveKey)}{input.Name!} level", ToInt(input.Gain?.Value ?? 0).ToString(), TouchPortalIdHelper.InputLevelCategoryName);
                    ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + input.Name, ToInt(input.Gain?.Value ?? 0), InputShortConnectorIds);

                    _logger.LogDebug($"Input ID: {input.Id}, Name: {GetWavePrefix(waveKey)}{input.Name}, Level: {input?.Gain?.Value}, Min: {input?.Gain?.Min}, Max: {input?.Gain?.Max}\n");
                }
            }
        };

        OnReceivedGetOutputDevices += async (s, response) =>
        {
            //var devices = response?.Result?.OutputDevices ?? [];
            //List<string> deviceList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var channels = handler?.Client?.StateManager.OutputDevices.Select(c =>GetWavePrefix(kvp.Key) + c.Name).ToArray();

            //    deviceList.AddRange(channels ?? []);
            //}
            //_client.ChoiceUpdate(TouchPortalIdHelper.OutputListId(), deviceList.ToArray());
            UpdateChoiceOutputList();
            //_client.ChoiceUpdate(TouchPortalIdHelper.OutputListId(), devices?.Select(d => GetWavePrefix(waveKey) + d.Name).ToArray());
            _logger.LogDebug("Received Output Devices Info:");
            foreach (var inputDevice in response?.Result?.OutputDevices ?? [])
            {
                _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {GetWavePrefix(waveKey)}{inputDevice.Name}, Type: {inputDevice.Type}");
                foreach (var output in inputDevice.Outputs ?? [])
                {
                    _client.CreateState(TouchPortalIdHelper.OutputMute(GetWavePrefix(waveKey) + output.Name!), $"{output.Name!} muted", output.IsMuted.ToString(), TouchPortalIdHelper.OutputMutedCategoryName);
                    _client.CreateState(TouchPortalIdHelper.OutputLevel(GetWavePrefix(waveKey) + output.Name!), $"{GetWavePrefix(waveKey)}{output.Name!} level", ToInt(output.Level ?? 0).ToString(), TouchPortalIdHelper.OutputLevelCategoryName);
                    ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + output.Name, ToInt(output.Level ?? 0), OutputShortConnectorIds);

                    _logger.LogDebug($"Input ID: {output.Id}, Name: {GetWavePrefix(waveKey)}{output.Name}, Level: {output.Level}, IsMuted: {output.IsMuted}");
                }
            }
            _logger.LogDebug("");
        };

        OnReceivedGetChannels += async (s, response) =>
        {
            var channels = response?.Result?.Channels ?? [];
            //List<string> channelList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var channelsList = handler?.Client?.StateManager.Channels.Select(c => GetWavePrefix(kvp.Key) + c.Name).ToArray();

            //    channelList.AddRange(channelsList ?? []);
            //}
            //_client.ChoiceUpdate(TouchPortalIdHelper.ChannelListId(), channelList.ToArray());
            UpdateChoiceChannelList();
            //_client.ChoiceUpdate(TouchPortalIdHelper.ChannelListId(), channels?.Select(c => GetWavePrefix(waveKey) + c.Name).ToArray());
            _logger.LogDebug("Received Channels Info:");
            foreach (var channel in channels!)
            {
                _client.CreateState(TouchPortalIdHelper.ChannelMute(GetWavePrefix(waveKey) + channel.Name!), $"{GetWavePrefix(waveKey)}{channel.Name!} muted", channel.IsMuted.ToString(), TouchPortalIdHelper.ChannelMutedCategoryName);
                _client.CreateState(TouchPortalIdHelper.ChannelLevel(GetWavePrefix(waveKey) + channel.Name!), $"{GetWavePrefix(waveKey)}{channel.Name!} level", ToInt(channel.Level ?? 0).ToString(), TouchPortalIdHelper.ChannelLevelCategoryName);
                ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + channel.Name, ToInt(channel.Level ?? 0), ChannelShortConnectorIds);

                _logger.LogDebug($"Channel ID: {channel.Id}, Name: {GetWavePrefix(waveKey)}{channel.Name}, Level: {channel.Level}, IsMuted: {channel.IsMuted}, Type: {channel.Type}");
                // can get image data from above
                foreach (var app in channel.Apps ?? [])
                {
                    _logger.LogDebug($"App ID: {app.Id}, Name: {app.Name}");
                }
                foreach (var mix in channel.Mixes ?? [])
                {
                    _logger.LogDebug($"Mix ID: {mix.Id}, Level: {mix.Level}, IsMuted: {mix.IsMuted}");
                }
                foreach (var effect in channel.Effects ?? [])
                {
                    //_logger.LogDebug($"Effect ID: {effect.Id}, Name: {effect.Name}, Type: {effect.Type}");
                }
            }
            _logger.LogDebug("");
        };

        OnReceivedGetMixes += async (s, response) =>
        {
            var mixes = response?.Result?.Mixes ?? [];
            //List<string> mixList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var tempMixes = handler?.Client?.StateManager.Channels.Select(c => GetWavePrefix(kvp.Key) + c.Name).ToArray();

            //    mixList.AddRange(tempMixes ?? []);
            //}
            //_client.ChoiceUpdate(TouchPortalIdHelper.ChannelListId(), mixList.ToArray());
            UpdateChoiceMixList();
            //_client.ChoiceUpdate(TouchPortalIdHelper.MixListId(), mixes?.Select(m => GetWavePrefix(waveKey) + m.Name).Concat(new[] { string.Empty }).ToArray());
            _logger.LogDebug("Received Mixes Info:");
            foreach (var mix in mixes!)
            {
                _client.CreateState(TouchPortalIdHelper.MixMute(GetWavePrefix(waveKey) + mix.Name!), $"{GetWavePrefix(waveKey)}{mix.Name!} muted", mix.IsMuted.ToString(), TouchPortalIdHelper.MixMutedCategoryName);
                _client.CreateState(TouchPortalIdHelper.MixLevel(GetWavePrefix(waveKey) + mix.Name!), $"{GetWavePrefix(waveKey)}{mix.Name!} level", ToInt(mix.Level ?? 0).ToString(), TouchPortalIdHelper.MixLevelCategoryName);
                ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + mix.Name, ToInt(mix.Level ?? 0), MixShortConnectorIds);
                _logger.LogDebug($"Mix ID: {mix.Id}, Name: {GetWavePrefix(waveKey)}{mix.Name}, Level: {mix.Level}, IsMuted: {mix.IsMuted}\n");
            }
        };


        OnChannelUpdated = (sender, result) =>
        {
            var channel = result.Item;
            _logger.LogDebug("Received Channels update:");
            _logger.LogDebug($"Channel ID: {channel.Id}, Name: {GetWavePrefix(waveKey)}{channel.Name}, Level: {channel.Level}, IsMuted: {channel.IsMuted}, Type: {channel.Type}");

            _client.StateUpdate(TouchPortalIdHelper.ChannelMute(GetWavePrefix(waveKey) + channel.Name!), channel.IsMuted.ToString());
            _client.StateUpdate(TouchPortalIdHelper.ChannelLevel(GetWavePrefix(waveKey) + channel.Name!), ToInt(channel.Level ?? 0).ToString());
            
            if (!result.IsResult)
            {
                ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + channel.Name, ToInt(channel.Level ?? 0), ChannelShortConnectorIds);
            }

            foreach (var app in channel.Apps ?? [])
            {
                _logger.LogDebug($"App ID: {app.Id}, Name: {app.Name}");
            }
            foreach (var mix in channel.Mixes ?? [])
            {
                _logger.LogDebug($"Mix ID: {mix.Id}, Level: {mix.Level}, IsMuted: {mix.IsMuted}");
            }
            foreach (var effect in channel.Effects ?? [])
            {
                //_logger.LogDebug($"Effect ID: {effect.Id}, Name: {effect.Name}, Type: {effect.Type}");
            }
        };

        OnFocusedAppUpdated += (sender, app) =>
        {
            _logger.LogDebug("Received Focused App update:");
            _logger.LogDebug("App ID: {app.Id}, Name: {app.Name}", app.Id, app.Name);
            _client.StateUpdate(TouchPortalIdHelper.FocusedAppId(), app.Name);

        };

        OnInputDeviceUpdated = (sender, result) =>
        {
            var inputDevice = result.Item;
            _logger.LogDebug("Received Input Devices update:");
            _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {GetWavePrefix(waveKey)}{inputDevice.Name}, Type: {inputDevice.Type}");
            foreach (var input in inputDevice.Inputs ?? [])
            {
                _logger.LogDebug($"Input ID: {input.Id}, Name: {input.Name}, Level: {input?.Gain?.Value}, Min: {input?.Gain?.Min}, Max: {input?.Gain?.Max}\n");
                _client.StateUpdate(TouchPortalIdHelper.InputMute(GetWavePrefix(waveKey) + input.Name!), input.IsMuted.ToString());
                _client.StateUpdate(TouchPortalIdHelper.InputLevel(GetWavePrefix(waveKey) + input.Name!), ToInt(input.Gain?.Value ?? 0).ToString());

                if (!result.IsResult)
                {
                    ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + input.Name, ToInt(input.Gain?.Value ?? 0), InputShortConnectorIds);
                }
            }
        };

        OnMixUpdated = (sender, result) =>
        {
            var mix = result.Item;
            _logger.LogDebug("Received Mixes update:");
            _logger.LogDebug($"Mix ID: {mix.Id}, Name: {GetWavePrefix(waveKey)}{mix.Name}, Level: {mix.Level}, IsMuted: {mix.IsMuted}, ImageName: {mix.Image?.Name}\n");
            _client.StateUpdate(TouchPortalIdHelper.MixMute(GetWavePrefix(waveKey) + mix.Name!), mix.IsMuted.ToString());
            _client.StateUpdate(TouchPortalIdHelper.MixLevel(GetWavePrefix(waveKey) + mix.Name!), ToInt(mix.Level ?? 0).ToString());
            
            if (!result.IsResult)
            {
                ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + mix.Name, ToInt(mix.Level ?? 0), MixShortConnectorIds);
            }
        };

        OnOutputDeviceUpdated = (sender, result) =>
        {
            var outputDevice = result.Item;
            _logger.LogDebug("Received Output Devices update:");
            _logger.LogDebug($"Device ID: {outputDevice.Id}, Name: {GetWavePrefix(waveKey)}{outputDevice.Name}, Type: {outputDevice.Type}");
            foreach (var output in outputDevice.Outputs ?? [])
            {
                _logger.LogDebug($"Input ID: {output.Id}, Name: {GetWavePrefix(waveKey)}{output.Name}, Level: {output.Level}, IsMuted: {output.IsMuted}");
                _client.StateUpdate(TouchPortalIdHelper.OutputMute(GetWavePrefix(waveKey) + output.Name!), output.IsMuted.ToString());
                _client.StateUpdate(TouchPortalIdHelper.OutputLevel(GetWavePrefix(waveKey) + output.Name!), ToInt(output.Level ?? 0).ToString());
                
                if (!result.IsResult)
                {
                    ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + output.Name, ToInt(output.Level ?? 0), OutputShortConnectorIds);
                }
            }
        };

        OnChannelAdded = (sender, result) =>
        {
            var channel = result.Item;
            _logger.LogDebug("Received Channels added:");
            _logger.LogDebug($"Channel ID: {channel.Id}, Name: {channel.Name}, Level: {channel.Level}, IsMuted: {channel.IsMuted}, Type: {channel.Type}");

            //List<string> channelList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var channels = handler?.Client?.StateManager.Channels.Select(c => kvp.Key + c.Name).ToArray();

            //    channelList.AddRange(channels ?? []);

            //    foreach (var app in channel.Apps ?? [])
            //    {
            //        _logger.LogDebug($"App ID: {app.Id}, Name: {app.Name}");
            //    }
            //    foreach (var mix in channel.Mixes ?? [])
            //    {
            //        _logger.LogDebug($"Mix ID: {mix.Id}, Level: {mix.Level}, IsMuted: {mix.IsMuted}");
            //    }
            //    foreach (var effect in channel.Effects ?? [])
            //    {
            //        //_logger.LogDebug($"Effect ID: {effect.Id}, Name: {effect.Name}, Type: {effect.Type}");
            //    }
            //}
            //_client.ChoiceUpdate(TouchPortalIdHelper.ChannelListId(), channelList.ToArray());
            UpdateChoiceChannelList();
            _client.CreateState(TouchPortalIdHelper.ChannelMute(GetWavePrefix(waveKey) + channel.Name!), $"{GetWavePrefix(waveKey)}{channel.Name!} muted", channel.IsMuted.ToString(), TouchPortalIdHelper.ChannelMutedCategoryName);
            _client.CreateState(TouchPortalIdHelper.ChannelLevel(GetWavePrefix(waveKey) + channel.Name!), $"{GetWavePrefix(waveKey)}{channel.Name!} level", ToInt(channel.Level ?? 0).ToString(), TouchPortalIdHelper.ChannelLevelCategoryName);
            
            if (!result.IsResult)
            {
                ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + channel.Name, ToInt(channel.Level ?? 0), ChannelShortConnectorIds);
            }
        };

        OnInputDeviceAdded = (sender, result) =>
        {
            var inputDevice = result.Item;
            _logger.LogDebug("Received Input Devices added:");
            _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}");

            //List<string> deviceList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var devices = handler?.Client?.StateManager.InputDevices.Select(d => kvp.Key + d.Name).ToArray();
            //    deviceList.AddRange(devices ?? []);
            //}

            //_client.ChoiceUpdate(TouchPortalIdHelper.InputListId(), deviceList.ToArray());
            UpdateChoiceInputList();

            foreach (var input in inputDevice.Inputs ?? [])
            {
                //Adds a state we can work with:
                _client.CreateState(TouchPortalIdHelper.InputMute(GetWavePrefix(waveKey) + input.Name!), $"{GetWavePrefix(waveKey)}{input.Name!} muted", input.IsMuted.ToString(), TouchPortalIdHelper.InputMutedCategoryName);
                _client.CreateState(TouchPortalIdHelper.InputLevel(GetWavePrefix(waveKey) + input.Name!), $"{GetWavePrefix(waveKey)}{input.Name!} level", ToInt(input.Gain?.Value ?? 0).ToString(), TouchPortalIdHelper.InputLevelCategoryName);
                
                if (!result.IsResult)
                {
                    ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + input.Name, ToInt(input.Gain?.Value ?? 0), InputShortConnectorIds);
                }
                _logger.LogDebug($"Input ID: {input.Id}, Name: {GetWavePrefix(waveKey)}{input.Name}, Level: {input?.Gain?.Value}, Min: {input?.Gain?.Min}, Max: {input?.Gain?.Max}\n");
            }
        };

        OnMixAdded = (sender, result) =>
        {
            var mix = result.Item;
            _logger.LogDebug("Received Mixes added:");
            _logger.LogDebug($"Mix ID: {mix.Id}, Name: {mix.Name}, Level: {mix.Level}, IsMuted: {mix.IsMuted}, ImageName: {mix.Image?.Name}\n");

            //List<string> mixList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var mixes = handler?.Client?.StateManager.Mixes.Select(m => kvp.Key + m.Name).ToArray();
            //    mixList.AddRange(mixes ?? []);
            //}

            ////var mixes = WaveLinkHandler?.Client?.StateManager.Mixes;
            //_client.ChoiceUpdate(TouchPortalIdHelper.MixListId(), mixList.Concat(new[] { string.Empty }).ToArray());
            UpdateChoiceMixList();

            _client.CreateState(TouchPortalIdHelper.MixMute(GetWavePrefix(waveKey) + mix.Name!), $"{GetWavePrefix(waveKey)}{mix.Name!} muted", mix.IsMuted.ToString(), TouchPortalIdHelper.MixMutedCategoryName);
            _client.CreateState(TouchPortalIdHelper.MixLevel(GetWavePrefix(waveKey) + mix.Name!), $"{GetWavePrefix(waveKey)}{mix.Name!} level", ToInt(mix.Level ?? 0).ToString(), TouchPortalIdHelper.MixLevelCategoryName);
            if (!result.IsResult)
            {
                ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + mix.Name, ToInt(mix.Level ?? 0), MixShortConnectorIds);
            }
        };

        OnOutputDeviceAdded = (sender, result) =>
        {
            var outputDevice = result.Item;
            _logger.LogDebug("Received Output Devices added:");
            _logger.LogDebug($"Device ID: {outputDevice.Id}, Name: {outputDevice.Name}, Type: {outputDevice.Type}");

            //var devices = WaveLinkHandler?.Client?.StateManager.OutputDevices;
            //List<string> deviceList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var devices = handler?.Client?.StateManager.OutputDevices.Select(d => kvp.Key + d.Name).ToArray();
            //    deviceList.AddRange(devices ?? []);
            //}

            //_client.ChoiceUpdate(TouchPortalIdHelper.OutputListId(), deviceList.ToArray());
            UpdateChoiceOutputList();

            foreach (var output in outputDevice.Outputs ?? [])
            {
                _logger.LogDebug($"Input ID: {output.Id}, Name: {output.Name}, Level: {output.Level}, IsMuted: {output.IsMuted}");
                _client.CreateState(TouchPortalIdHelper.OutputMute(GetWavePrefix(waveKey) + output.Name!), $"{GetWavePrefix(waveKey)}{output.Name!} muted", output.IsMuted.ToString(), TouchPortalIdHelper.OutputMutedCategoryName);
                _client.CreateState(TouchPortalIdHelper.OutputLevel(GetWavePrefix(waveKey) + output.Name!), $"{GetWavePrefix(waveKey)}{output.Name!} level", ToInt(output.Level ?? 0).ToString(), TouchPortalIdHelper.OutputLevelCategoryName);
                if (!result.IsResult)
                {
                    ShortConnectorUpdateHelper(GetWavePrefix(waveKey) + output.Name, ToInt(output.Level ?? 0), OutputShortConnectorIds);
                }
            }
        };

        OnChannelRemoved = (sender, result) =>
        {
            var channel = result.Item;
            _logger.LogDebug("Received Channels removed:");
            _logger.LogDebug($"Channel ID: {channel.Id}, Name: {channel.Name}, Level: {channel.Level}, IsMuted: {channel.IsMuted}, Type: {channel.Type}, ImageNull: {string.IsNullOrEmpty(channel.Image?.ImgData)}");

            //var channels = WaveLinkHandler?.Client?.StateManager.Channels;
            //List<string> channelList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var devices = handler?.Client?.StateManager.Channels.Select(c => kvp.Key + c.Name).ToArray();
            //    channelList.AddRange(devices ?? []);
            //}
            //_client.ChoiceUpdate(TouchPortalIdHelper.ChannelListId(), channelList.ToArray());
            UpdateChoiceChannelList();

            _client.RemoveState(TouchPortalIdHelper.ChannelMute(GetWavePrefix(waveKey) + channel.Name!));
            _client.RemoveState(TouchPortalIdHelper.ChannelLevel(GetWavePrefix(waveKey) + channel.Name!));
        };

        OnInputDeviceRemoved = (sender, result) =>
        {
            var inputDevice = result.Item;
            _logger.LogDebug("Received Input Devices removed:");
            _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}");

            //var devices = WaveLinkHandler?.Client?.StateManager.InputDevices;
            //List<string> deviceList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var devices = handler?.Client?.StateManager.InputDevices.Select(d => kvp.Key + d.Name).ToArray();
            //    deviceList.AddRange(devices ?? []);
            //}

            //_client.ChoiceUpdate(TouchPortalIdHelper.InputListId(), deviceList.ToArray());
            UpdateChoiceInputList();
            _client.RemoveState(TouchPortalIdHelper.InputMute(GetWavePrefix(waveKey) + inputDevice.Name!));
            _client.RemoveState(TouchPortalIdHelper.InputLevel(GetWavePrefix(waveKey) + inputDevice.Name!));
        };

        OnMixRemoved = (sender, result) =>
        {
            var mix = result.Item;
            _logger.LogDebug("Received Mixes removed:");
            _logger.LogDebug($"Mix ID: {mix.Id}, Name: {mix.Name}, Level: {mix.Level}, IsMuted: {mix.IsMuted}, ImageName: {mix.Image?.Name}\n");

            //var mixes = WaveLinkHandler?.Client?.StateManager.Mixes;
            //List<string> mixList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var mixes = handler?.Client?.StateManager.Mixes.Select(m => kvp.Key + m.Name).ToArray();
            //    mixList.AddRange(mixes ?? []);
            //}
            //_client.ChoiceUpdate(TouchPortalIdHelper.MixListId(), mixList.Concat(new[] { string.Empty }).ToArray());
            UpdateChoiceMixList();
            _client.RemoveState(TouchPortalIdHelper.MixMute(GetWavePrefix(waveKey) + mix.Name!));
            _client.RemoveState(TouchPortalIdHelper.MixLevel(GetWavePrefix(waveKey) + mix.Name!));
        };

        OnOutputDeviceRemoved = (sender, result) =>
        {
            var outputDevice = result.Item;
            _logger.LogDebug("Received Output Devices removed:");
            _logger.LogDebug($"Device ID: {outputDevice.Id}, Name: {outputDevice.Name}, Type: {outputDevice.Type}");

            //var outputs = WaveLinkHandler?.Client?.StateManager.OutputDevices;
            //List<string> deviceList = new();

            //foreach (var kvp in AdditionalWaveLinkInstances)
            //{
            //    var handler = kvp.Value;
            //    var devices = handler?.Client?.StateManager.OutputDevices.Select(d => kvp.Key + d.Name).ToArray();
            //    deviceList.AddRange(devices ?? []);
            //}
            //_client.ChoiceUpdate(TouchPortalIdHelper.OutputListId(), deviceList.ToArray());
            UpdateChoiceOutputList();
            _client.RemoveState(TouchPortalIdHelper.OutputMute(GetWavePrefix(waveKey) + outputDevice.Name!));
            _client.RemoveState(TouchPortalIdHelper.OutputLevel(GetWavePrefix(waveKey) + outputDevice.Name!));
        };

        OnSubscribedToFocusAppChanged += (sender, subscribed) =>
        {
            _logger.LogDebug("Subscribed to Focused App: {Subscribed}", subscribed);
        };
    }
    public void SubscribeToEvents(WaveLinkHandler handler)
    {
        handler?.Client?.StateManager.OnChannelUpdated += OnChannelUpdated;
        handler?.Client?.StateManager.OnFocusedAppUpdated += OnFocusedAppUpdated;
        handler?.Client?.StateManager.OnInputDeviceUpdated += OnInputDeviceUpdated;
        handler?.Client?.StateManager.OnMixUpdated += OnMixUpdated;
        handler?.Client?.StateManager.OnOutputDeviceUpdated += OnOutputDeviceUpdated;
        handler?.Client?.StateManager.OnChannelAdded += OnChannelAdded;
        handler?.Client?.StateManager.OnInputDeviceAdded += OnInputDeviceAdded;
        handler?.Client?.StateManager.OnMixAdded += OnMixAdded;
        handler?.Client?.StateManager.OnOutputDeviceAdded += OnOutputDeviceAdded;
        handler?.Client?.StateManager.OnChannelRemoved += OnChannelRemoved;
        handler?.Client?.StateManager.OnInputDeviceRemoved += OnInputDeviceRemoved;
        handler?.Client?.StateManager.OnSubscribedToFocusAppChanged += OnSubscribedToFocusAppChanged;

        handler?.Client?.MessageRouter.OnReceivedGetInputDevices += OnReceivedGetInputDevices;
        handler?.Client?.MessageRouter.OnReceivedGetOutputDevices += OnReceivedGetOutputDevices;
        handler?.Client?.MessageRouter.OnReceivedGetChannels += OnReceivedGetChannels;
        handler?.Client?.MessageRouter.OnReceivedGetMixes += OnReceivedGetMixes;
    }

    public void ClearEvents(WaveLinkHandler handler)
    {
        handler?.Client?.StateManager.OnChannelUpdated -= OnChannelUpdated;
        handler?.Client?.StateManager.OnFocusedAppUpdated -= OnFocusedAppUpdated;
        handler?.Client?.StateManager.OnInputDeviceUpdated -= OnInputDeviceUpdated;
        handler?.Client?.StateManager.OnMixUpdated -= OnMixUpdated;
        handler?.Client?.StateManager.OnOutputDeviceUpdated -= OnOutputDeviceUpdated;
        handler?.Client?.StateManager.OnChannelAdded -= OnChannelAdded;
        handler?.Client?.StateManager.OnInputDeviceAdded -= OnInputDeviceAdded;
        handler?.Client?.StateManager.OnMixAdded -= OnMixAdded;
        handler?.Client?.StateManager.OnOutputDeviceAdded -= OnOutputDeviceAdded;
        handler?.Client?.StateManager.OnChannelRemoved -= OnChannelRemoved;
        handler?.Client?.StateManager.OnInputDeviceRemoved -= OnInputDeviceRemoved;
        handler?.Client?.StateManager.OnSubscribedToFocusAppChanged -= OnSubscribedToFocusAppChanged;

        handler?.Client?.MessageRouter.OnReceivedGetInputDevices -= OnReceivedGetInputDevices;
        handler?.Client?.MessageRouter.OnReceivedGetOutputDevices -= OnReceivedGetOutputDevices;
        handler?.Client?.MessageRouter.OnReceivedGetChannels -= OnReceivedGetChannels;
        handler?.Client?.MessageRouter.OnReceivedGetMixes -= OnReceivedGetMixes;
    }

    // input actions
    public void SetMuteInput(ActionEvent message)
    {
        var inputName = message[TouchPortalIdHelper.InputListId()] ?? "<null>";
        var muteValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetMuteInput))] ?? "<null>";
        var handler = GetWaveLinkHandler(ref inputName);

        if (!string.IsNullOrEmpty(inputName) && !string.IsNullOrEmpty(muteValue) && handler != null)
        {
            handler?.SetInput(inputName, muteValue);
        }
    }
    public void SetLevelInput(ActionEvent message)
    {
        var inputName = message[TouchPortalIdHelper.InputListId()] ?? "<null>";
        var levelValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetLevelInput))] ?? "<null>";
        var handler = GetWaveLinkHandler(ref inputName);
        if (!string.IsNullOrEmpty(inputName) &&
            !string.IsNullOrEmpty(levelValue) &&
            decimal.TryParse(levelValue, out decimal newLevel))
        {
            handler?.SetInput(inputName, null, newLevel);
        }
    }

    public void SetLevelInput(ConnectorChangeEvent message)
    {
        var inputName = message[TouchPortalIdHelper.InputListId()] ?? "<null>";
        var handler = GetWaveLinkHandler(ref inputName);

        if (!string.IsNullOrEmpty(inputName))
        {
            handler?.SetInput(inputName, null, message.Value);
        }
    }

    // output actions
    public void SetMuteOutput(ActionEvent message)
    {
        var outputName = message[TouchPortalIdHelper.OutputListId()] ?? "<null>";
        var muteValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetMuteOutput))] ?? "<null>";
        var handler = GetWaveLinkHandler(ref outputName);

        if (!string.IsNullOrEmpty(outputName) && !string.IsNullOrEmpty(muteValue))
        {
            handler?.SetOutput(outputName, muteValue);
        }
    }
    public void SetLevelOutput(ActionEvent message)
    {
        var outputName = message[TouchPortalIdHelper.OutputListId()] ?? "<null>";
        var levelValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetLevelOutput))] ?? "<null>";
        var handler = GetWaveLinkHandler(ref outputName);

        if (!string.IsNullOrEmpty(outputName) &&
            !string.IsNullOrEmpty(levelValue) &&
            decimal.TryParse(levelValue, out decimal newLevel))
        {
            handler?.SetOutput(outputName, null, newLevel);
        }
    }

    public void SetLevelOutput(ConnectorChangeEvent message)
    {
        var outputName = message[TouchPortalIdHelper.OutputListId()] ?? "<null>";
        var handler = GetWaveLinkHandler(ref outputName);

        if (!string.IsNullOrEmpty(outputName))
        {
            handler?.SetOutput(outputName, null, message.Value);
        }
    }

    public void SetOuputDevice(ActionEvent message)
    {
        var outputName = message[TouchPortalIdHelper.OutputListId()] ?? "<null>";
        var mixName = message[TouchPortalIdHelper.MixListId()] ?? "<null>";
        var handler = GetWaveLinkHandler(ref outputName);

        if (!string.IsNullOrEmpty(outputName) &&
            mixName != null)
        {
            handler?.SetOutput(outputName, null, null, mixName);
        }
    }

    // channel actions
    public void SetLevelChannel(ActionEvent message)
    {
        var channelName = message[TouchPortalIdHelper.ChannelListId()] ?? "<null>";
        var levelValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetLevelChannel))] ?? "<null>";
        var handler = GetWaveLinkHandler(ref channelName);

        if (!string.IsNullOrEmpty(channelName) &&
            !string.IsNullOrEmpty(levelValue) &&
            decimal.TryParse(levelValue, out decimal newLevel))
        {
            handler?.SetChannel(channelName, null, newLevel);
        }
    }
    public void SetLevelChannel(ConnectorChangeEvent message)
    {
        var channelName = message[TouchPortalIdHelper.ChannelListId()] ?? "<null>";
        var handler = GetWaveLinkHandler(ref channelName);

        if (!string.IsNullOrEmpty(channelName))
        {
            handler?.SetChannel(channelName, null, message.Value);
        }
    }

    public void SetMuteChannel(ActionEvent message)
    {
        var channelName = message[TouchPortalIdHelper.ChannelListId()] ?? "<null>";
        var muteValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetMuteChannel))] ?? "<null>";
        var handler = GetWaveLinkHandler(ref channelName);

        if (!string.IsNullOrEmpty(channelName) && !string.IsNullOrEmpty(muteValue))
        {
            handler?.SetChannel(channelName, muteValue);
        }
    }

    // mix actions
    public void SetLevelMix(ActionEvent message)
    {
        var mixName = message[TouchPortalIdHelper.MixListId()] ?? "<null>";
        var levelValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetLevelMix))] ?? "<null>";
        var handler = GetWaveLinkHandler(ref mixName);

        if (!string.IsNullOrEmpty(mixName) &&
            !string.IsNullOrEmpty(levelValue) &&
            decimal.TryParse(levelValue, out decimal newLevel))
        {
            handler?.SetMix(mixName, null, newLevel);
        }
    }
    public void SetLevelMix(ConnectorChangeEvent message)
    {
        var mixName = message[TouchPortalIdHelper.MixListId()] ?? "<null>";
        var handler = GetWaveLinkHandler(ref mixName);

        if (!string.IsNullOrEmpty(mixName))
        {
            handler?.SetMix(mixName, null, message.Value);
        }
    }

    public void SetMuteMix(ActionEvent message)
    {
        var mixName = message[TouchPortalIdHelper.MixListId()] ?? "<null>";
        var muteValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetMuteMix))] ?? "<null>";
        var handler = GetWaveLinkHandler(ref mixName);

        if (!string.IsNullOrEmpty(mixName) && !string.IsNullOrEmpty(muteValue))
        {
            handler?.SetMix(mixName, muteValue);
        }
    }

    // subscribe to focus app changes
    public void SetFocusAppNotification(ActionEvent message)
    {
        var subValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetFocusAppNotification))] ?? "<null>";
        var temp = string.Empty;
        var handler = GetWaveLinkHandler(ref temp);

        if (!string.IsNullOrEmpty(subValue))
        {
            handler?.SetFocusAppSubscription(subValue);
        }
    }

    // add focused app to channel
    public void AddToChannel(ActionEvent message)
    {
        var channelName = message[TouchPortalIdHelper.ChannelListId()] ?? "<null>";
        var temp = string.Empty;

        var handler = GetWaveLinkHandler(ref temp);

        if (!string.IsNullOrEmpty(channelName))
        {
            handler?.AddToChannel(channelName);
        }
    }
    public int ToInt(decimal value)
    {
        return (int)(value * 100);
    }

    // helper function to update short connector ids
    public void ShortConnectorUpdateHelper(string? name, int value, Dictionary<string, List<string>> shortConnectorIds)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (shortConnectorIds.TryGetValue(name, out var list))
        {
            foreach (var shortId in list)
            {
                _client.ConnectorUpdateShort(shortId, value);
            }
        }
    }

    public async Task<string?> UpdateAvailable()
    {
        string repositoryOwner = "kylergib";
        string repositoryName = "WaveLinkPluginTouchPortal";
        HttpClient client = new HttpClient();
        try
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("C# App"); // GitHub requires UA
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

            string url = $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/releases";
            using HttpResponseMessage response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed : HTTP Error code : {(int)response.StatusCode}");
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);

            var release = doc.RootElement
               .EnumerateArray()
               .Where(r => r.GetProperty("prerelease").GetBoolean() && r.GetProperty("tag_name").GetString().StartsWith("3_"))
               .OrderByDescending(r => DateTime.Parse(r.GetProperty("published_at").GetString()!))
               .FirstOrDefault();
            UpdateUrl = release.GetProperty("html_url").GetString() ?? string.Empty;
            var tag = release.GetProperty("tag_name").GetString();


            List<string> versionParts = new List<string>(tag.Replace("3_", "").Split('.'));
            int newestVersion = 0;

            if (versionParts.Count == 3
                && int.TryParse(versionParts[0], out int part1)
                && int.TryParse(versionParts[1], out int part2)
                && int.TryParse(versionParts[2], out int part3))
            {
                newestVersion = (part1 * 100) + (part2 * 10) + part3;
            }

            _logger.LogInformation($"Newest version available is: {newestVersion}");
            _logger.LogInformation($"Current version is: {PluginVersion}");

            if (PluginVersion < newestVersion) return tag;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        return null;
    }
    public string GetWavePrefix(string? waveKey)
    {
        return string.IsNullOrEmpty(waveKey) || waveKey.ToLower() == "default" ? string.Empty : waveKey + "_";
    }
    public WaveLinkHandler? GetWaveLinkHandler(ref string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var tempName = name;
        var waveKey = AdditionalWaveLinkInstances.Keys.FirstOrDefault(k => tempName.StartsWith(k + "_"));
        if (waveKey == null)
        {
            if (AdditionalWaveLinkInstances.TryGetValue("default", out var handler))
            {
                return handler;
            }
        }
        else if (AdditionalWaveLinkInstances.TryGetValue(waveKey, out var handler))
        {
            name = name.ReplaceFirst(waveKey + "_", string.Empty);
            return handler;
        }
        return null;
    }
    public void UpdateChoiceInputList()
    {
        List<string> inputList = new();
        foreach (var kvp in AdditionalWaveLinkInstances)
        {
            var handler = kvp.Value;
            var inputs = handler?.Client?.StateManager.InputDevices.Select(d => GetWavePrefix(kvp.Key) + d.Name).ToArray();
            inputList.AddRange(inputs ?? []);
        }
        _client.ChoiceUpdate(TouchPortalIdHelper.InputListId(), inputList.ToArray());
    }
    public void UpdateChoiceOutputList()
    {
        List<string> outputList = new();
        foreach (var kvp in AdditionalWaveLinkInstances)
        {
            var handler = kvp.Value;
            var outputs = handler?.Client?.StateManager.OutputDevices.Select(d => GetWavePrefix(kvp.Key) + d.Name).ToArray();
            outputList.AddRange(outputs ?? []);
        }
        _client.ChoiceUpdate(TouchPortalIdHelper.OutputListId(), outputList.ToArray());
    }

    public void UpdateChoiceChannelList()
    {
        List<string> channelList = new();
        foreach (var kvp in AdditionalWaveLinkInstances)
        {
            var handler = kvp.Value;
            var channels = handler?.Client?.StateManager.Channels.Select(c => GetWavePrefix(kvp.Key) + c.Name).ToArray();
            channelList.AddRange(channels ?? []);
        }
        _client.ChoiceUpdate(TouchPortalIdHelper.ChannelListId(), channelList.ToArray());
    }

    public void UpdateChoiceMixList()
    {
        List<string> mixList = new() { string.Empty };
        foreach (var kvp in AdditionalWaveLinkInstances)
        {
            var handler = kvp.Value;
            var mixes = handler?.Client?.StateManager.Mixes.Select(m => GetWavePrefix(kvp.Key) + m.Name).ToArray();
            mixList.AddRange(mixes ?? []);
        }
        _client.ChoiceUpdate(TouchPortalIdHelper.MixListId(), mixList.Concat(new[] { string.Empty }).ToArray());
    }
}
public static class StringExtensions
{
    public static string ReplaceFirst(
        this string text,
        string search,
        string replacement,
        StringComparison comparison = StringComparison.Ordinal)
    {
        int pos = text.IndexOf(search, comparison);
        return pos < 0
            ? text
            : text[..pos] + replacement + text[(pos + search.Length)..];
    }
}
