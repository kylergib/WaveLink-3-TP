using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;
using WaveLink.Plugin;
using WaveLink.SDK;
using WaveLink.SDK.Models;
namespace WaveLink.Plugin.Models;

public class WaveLinkPlugin : ITouchPortalEventHandler
{
    public string PluginId => Statics.PluginId;

    public readonly ITouchPortalClient _client;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<WaveLinkPlugin> _logger;
    private IReadOnlyCollection<Setting> _settings;

    private WaveLinkHandler? WaveLinkHandler = null;
    public string LogLevel { get; set; } = "none";
    public bool SaveToFile { get; set; } = false;
    public string IpAddress { get; set; } = WaveLink.SDK.Statics.Localhost;
    public bool SubscribeToFocusedApp { get; set; } = true;

    public EventHandler<SDK.Models.Channel>? OnChannelUpdated;
    public EventHandler<App>? OnFocusedAppUpdated;
    public EventHandler<InputDevice>? OnInputDeviceUpdated;
    public EventHandler<Mix>? OnMixUpdated;
    public EventHandler<OutputDevice>? OnOutputDeviceUpdated;


    public EventHandler<SDK.Models.Channel>? OnChannelAdded;
    public EventHandler<InputDevice>? OnInputDeviceAdded;
    public EventHandler<Mix>? OnMixAdded;
    public EventHandler<OutputDevice>? OnOutputDeviceAdded;

    public EventHandler<SDK.Models.Channel>? OnChannelRemoved;
    public EventHandler<InputDevice>? OnInputDeviceRemoved;
    public EventHandler<Mix>? OnMixRemoved;
    public EventHandler<OutputDevice>? OnOutputDeviceRemoved;

    public EventHandler<bool>? OnSubscribedToFocusAppChanged;

    // router events
    public EventHandler<WaveLinkResponse<InputDeviceResult>>? OnReceivedGetInputDevices;
    public EventHandler<WaveLinkResponse<OutputDeviceResult>>? OnReceivedGetOutputDevices;
    public EventHandler<WaveLinkResponse<ChannelsResult>>? OnReceivedGetChannels;
    public EventHandler<WaveLinkResponse<MixesResult>>? OnReceivedGetMixes;

    public LoggingLevelSwitch? LogLevelSwitch { get; set; }
    public LoggingLevelSwitch? FileLevelSwitch { get; set; }
    public LoggingLevelSwitch? WaveSocketSwitch { get; set; }



    public WaveLinkPlugin(ILoggerFactory logFactory)
    {
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
        } else if (LogLevel.ToLower() == "information")
        {
            LogLevelSwitch?.MinimumLevel = LogEventLevel.Information;
            WaveSocketSwitch?.MinimumLevel = LogEventLevel.Information;
        } else if (LogLevel.ToLower() == "warning")
        {
            LogLevelSwitch?.MinimumLevel = LogEventLevel.Warning;
            WaveSocketSwitch?.MinimumLevel = LogEventLevel.Fatal + 1;
        }


        SaveToFile = saveToFileValue is "1" or "true" or "True" or "on" or "On";
        SubscribeToFocusedApp = subscribeToFocusAppValue is "1" or "true" or "True" or "on" or "On";

        if (SaveToFile)
        {
            FileLevelSwitch?.MinimumLevel = LogEventLevel.Verbose;
        }

        ConnectToWaveLink();
    }
    public void ConnectToWaveLink()
    {
        _logger?.LogInformation("Trying to connect to Wave Link on: {IpAddress}", IpAddress);
        WaveLinkHandler = new(_loggerFactory, IpAddress, WaveLink.SDK.Statics.DefaultPort);
        WaveLinkHandler.SubscribeToFocusApp = SubscribeToFocusedApp;
        WaveLinkHandler?.Start();
        WaveLinkHandler?.OnConnection += (sender, args) =>
        {
            InitializeEventHandler();
            SubscribeToEvents();
        };
    }
    public async Task DisconnectFromWaveLink()
    {
        if (WaveLinkHandler is null || WaveLinkHandler.Client is null) return;
        WaveLinkHandler.Retry = false;
        await WaveLinkHandler!.Client!.CloseAsync();

        WaveLinkHandler = null;
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
        _logger?.LogInformation($"[OnListChanged] {message.ListId}/{message.ActionId}/{message.InstanceId} '{message.Value}'");

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
            } else if (LogLevel.ToLower() == "information")
            {
                LogLevelSwitch?.MinimumLevel = LogEventLevel.Information;
                WaveSocketSwitch?.MinimumLevel = LogEventLevel.Information;
            } else if (LogLevel.ToLower() == "warning")
            {
                LogLevelSwitch?.MinimumLevel = LogEventLevel.Warning;
                WaveSocketSwitch?.MinimumLevel = LogEventLevel.Fatal + 1;
            }
        }

        if (SaveToFile != saveToFile)
        {
            SaveToFile = saveToFile;
            FileLevelSwitch?.MinimumLevel = SaveToFile ? LogEventLevel.Verbose : LogEventLevel.Fatal + 1;
        }

        if (IpAddress != ipAddress)
        {
            await DisconnectFromWaveLink();
            IpAddress = ipAddress;
            ConnectToWaveLink();
        }

        if (SubscribeToFocusedApp != subscribeToFocusedApp)
        {
            SubscribeToFocusedApp = subscribeToFocusedApp;
            WaveLinkHandler?.SubscribeToFocusApp = SubscribeToFocusedApp;
            WaveLinkSendMethod<MethodSubscriptionInfo> subscribe = new(WaveLinkMethod.setSubscription, new() { FocusedAppChanged = new() { IsEnabled = SubscribeToFocusedApp } });
            _ = WaveLinkHandler?.Client?.SendRequestAsync<MethodSubscriptionInfo>(subscribe);
        }
    }
    public void OnNotificationOptionClickedEvent(NotificationOptionClickedEvent message)
    {
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
        var dataArray = message.Data
            .Select(dataItem => $"\"{dataItem.Key}\":\"{dataItem.Value}\"")
            .ToArray();

        var dataString = string.Join(", ", dataArray);
        _logger?.LogDebug($"[OnConnecterChangeEvent] ConnectorId: '{message.ConnectorId}', Value: '{message.Value}', Data: '{dataString}'");
    }

    public void OnShortConnectorIdNotificationEvent(ShortConnectorIdNotificationEvent message)
    {
        _logger?.LogDebug($"[OnShortConnectorIdNotificationEvent] ConnectorId: '{message.ConnectorId}', ShortID: '{message.ShortId}'");
    }

    public void InitializeEventHandler()
    {
        OnReceivedGetInputDevices = async (s, response) =>
        {
            var devices = response?.Result?.InputDevices ?? [];
            _client.ChoiceUpdate(TouchPortalIdHelper.InputListId, devices?.Select(d => d.Name).ToArray());
            _logger.LogDebug("Received Input Devices Info:");
            foreach (var inputDevice in response?.Result?.InputDevices ?? new())
            {
                _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}");
                foreach (var input in inputDevice?.Inputs ?? [])
                {
                    _client.CreateState(TouchPortalIdHelper.InputMute(input.Name!), $"{input.Name!} muted", input.IsMuted.ToString(), TouchPortalIdHelper.InputMutedCategoryName);
                    _client.CreateState(TouchPortalIdHelper.InputLevel(input.Name!), $"{input.Name!} level", ToInt(input.Gain?.Value ?? 0).ToString(), TouchPortalIdHelper.InputLevelCategoryName);
                    _logger.LogDebug($"Input ID: {input.Id}, Name: {input.Name}, Level: {input?.Gain?.Value}, Min: {input?.Gain?.Min}, Max: {input?.Gain?.Max}\n");
                }
            }
        };

        OnReceivedGetOutputDevices = async (s, response) =>
        {
            var devices = response?.Result?.OutputDevices ?? [];
            _client.ChoiceUpdate(TouchPortalIdHelper.OutputListId, devices?.Select(d => d.Name).ToArray());
            _logger.LogDebug("Received Output Devices Info:");
            foreach (var inputDevice in response?.Result?.OutputDevices ?? [])
            {
                _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}");
                foreach (var output in inputDevice.Outputs ?? [])
                {
                    _client.CreateState(TouchPortalIdHelper.OutputMute(output.Name!), $"{output.Name!} muted", output.IsMuted.ToString(), TouchPortalIdHelper.OutputMutedCategoryName);
                    _client.CreateState(TouchPortalIdHelper.OutputLevel(output.Name!), $"{output.Name!} level", ToInt(output.Level ?? 0).ToString(), TouchPortalIdHelper.OutputLevelCategoryName);
                    _logger.LogDebug($"Input ID: {output.Id}, Name: {output.Name}, Level: {output.Level}, IsMuted: {output.IsMuted}");
                }
            }
            _logger.LogDebug("");
        };

        OnReceivedGetChannels = async (s, response) =>
        {
            var channels = response?.Result?.Channels ?? [];
            _client.ChoiceUpdate(TouchPortalIdHelper.ChannelListId, channels?.Select(c => c.Name).ToArray());
            _logger.LogDebug("Received Channels Info:");
            foreach (var channel in channels!)
            {
                _client.CreateState(TouchPortalIdHelper.ChannelMute(channel.Name!), $"{channel.Name!} muted", channel.IsMuted.ToString(), TouchPortalIdHelper.ChannelMutedCategoryName);
                _client.CreateState(TouchPortalIdHelper.ChannelLevel(channel.Name!), $"{channel.Name!} level", ToInt(channel.Level ?? 0).ToString(), TouchPortalIdHelper.ChannelLevelCategoryName);

                _logger.LogDebug($"Channel ID: {channel.Id}, Name: {channel.Name}, Level: {channel.Level}, IsMuted: {channel.IsMuted}, Type: {channel.Type}, ImageNull: {string.IsNullOrEmpty(channel.Image?.ImgData)}");
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

        OnReceivedGetMixes = async (s, response) =>
        {
            var mixes = response?.Result?.Mixes ?? [];
            _client.ChoiceUpdate(TouchPortalIdHelper.MixListId, mixes?.Select(m => m.Name).ToArray());
            _logger.LogDebug("Received Mixes Info:");
            foreach (var mix in mixes!)
            {
                _client.CreateState(TouchPortalIdHelper.MixMute(mix.Name!), $"{mix.Name!} muted", mix.IsMuted.ToString(), TouchPortalIdHelper.MixMutedCategoryName);
                _client.CreateState(TouchPortalIdHelper.MixLevel(mix.Name!), $"{mix.Name!} level", ToInt(mix.Level ?? 0).ToString(), TouchPortalIdHelper.MixLevelCategoryName);
                _logger.LogDebug($"Mix ID: {mix.Id}, Name: {mix.Name}, Level: {mix.Level}, IsMuted: {mix.IsMuted}, ImageName: {mix.Image?.Name}\n");
            }
        };


        OnChannelUpdated = (sender, channel) =>
        {
            _logger.LogDebug("Received Channels update:");
            _logger.LogDebug($"Channel ID: {channel.Id}, Name: {channel.Name}, Level: {channel.Level}, IsMuted: {channel.IsMuted}, Type: {channel.Type}, ImageNull: {string.IsNullOrEmpty(channel.Image?.ImgData)}");

            _client.StateUpdate(TouchPortalIdHelper.ChannelMute(channel.Name!), channel.IsMuted.ToString());
            _client.StateUpdate(TouchPortalIdHelper.ChannelLevel(channel.Name!), ToInt(channel.Level ?? 0).ToString());


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

        OnFocusedAppUpdated = (sender, app) =>
        {
            _logger.LogDebug("Received Focused App update:");
            _logger.LogDebug("App ID: {app.Id}, Name: {app.Name}", app.Id, app.Name);
            _client.StateUpdate(TouchPortalIdHelper.FocusedAppId, app.Name);

        };

        OnInputDeviceUpdated = (sender, inputDevice) =>
        {
            _logger.LogDebug("Received Input Devices update:");
            _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}");
            foreach (var input in inputDevice.Inputs ?? [])
            {
                _logger.LogDebug($"Input ID: {input.Id}, Name: {input.Name}, Level: {input?.Gain?.Value}, Min: {input?.Gain?.Min}, Max: {input?.Gain?.Max}\n");
                _client.StateUpdate(TouchPortalIdHelper.InputMute(input.Name!), input.IsMuted.ToString());
                _client.StateUpdate(TouchPortalIdHelper.InputLevel(input.Name!), ToInt(input.Gain?.Value ?? 0).ToString());
            }
        };

        OnMixUpdated = (sender, mix) =>
        {
            _logger.LogDebug("Received Mixes update:");
            _logger.LogDebug($"Mix ID: {mix.Id}, Name: {mix.Name}, Level: {mix.Level}, IsMuted: {mix.IsMuted}, ImageName: {mix.Image?.Name}\n");
            _client.StateUpdate(TouchPortalIdHelper.MixMute(mix.Name!), mix.IsMuted.ToString());
            _client.StateUpdate(TouchPortalIdHelper.MixLevel(mix.Name!), ToInt(mix.Level ?? 0).ToString());
        };

        OnOutputDeviceUpdated = (sender, outputDevice) =>
        {
            _logger.LogDebug("Received Output Devices update:");
            _logger.LogDebug($"Device ID: {outputDevice.Id}, Name: {outputDevice.Name}, Type: {outputDevice.Type}");
            foreach (var output in outputDevice.Outputs ?? [])
            {
                _logger.LogDebug($"Input ID: {output.Id}, Name: {output.Name}, Level: {output.Level}, IsMuted: {output.IsMuted}");
                _client.StateUpdate(TouchPortalIdHelper.OutputMute(output.Name!), output.IsMuted.ToString());
                _client.StateUpdate(TouchPortalIdHelper.OutputLevel(output.Name!), ToInt(output.Level ?? 0).ToString());
            }
        };

        OnChannelAdded = (sender, channel) =>
        {
            _logger.LogDebug("Received Channels added:");
            _logger.LogDebug($"Channel ID: {channel.Id}, Name: {channel.Name}, Level: {channel.Level}, IsMuted: {channel.IsMuted}, Type: {channel.Type}, ImageNull: {string.IsNullOrEmpty(channel.Image?.ImgData)}");

            var channels = WaveLinkHandler?.Client?.StateManager.Channels;
            _client.ChoiceUpdate(TouchPortalIdHelper.ChannelListId, channels?.Select(c => c.Name).ToArray());
            _client.CreateState(TouchPortalIdHelper.ChannelMute(channel.Name!), $"{channel.Name!} muted", channel.IsMuted.ToString(), TouchPortalIdHelper.ChannelMutedCategoryName);
            _client.CreateState(TouchPortalIdHelper.ChannelLevel(channel.Name!), $"{channel.Name!} level", ToInt(channel.Level ?? 0).ToString(), TouchPortalIdHelper.ChannelLevelCategoryName);

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

        OnInputDeviceAdded = (sender, inputDevice) =>
        {
            _logger.LogDebug("Received Input Devices added:");
            _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}");

            var devices = WaveLinkHandler?.Client?.StateManager.InputDevices;
            _client.ChoiceUpdate(TouchPortalIdHelper.InputListId, devices?.Select(d => d.Name).ToArray());

            foreach (var input in inputDevice.Inputs ?? [])
            {
                //Adds a state we can work with:
                _client.CreateState(TouchPortalIdHelper.InputMute(input.Name!), $"{input.Name!} muted", input.IsMuted.ToString(), TouchPortalIdHelper.InputMutedCategoryName);
                _client.CreateState(TouchPortalIdHelper.InputLevel(input.Name!), $"{input.Name!} level", ToInt(input.Gain?.Value ?? 0).ToString(), TouchPortalIdHelper.InputLevelCategoryName);
                _logger.LogDebug($"Input ID: {input.Id}, Name: {input.Name}, Level: {input?.Gain?.Value}, Min: {input?.Gain?.Min}, Max: {input?.Gain?.Max}\n");
            }
        };

        OnMixAdded = (sender, mix) =>
        {
            _logger.LogDebug("Received Mixes added:");
            _logger.LogDebug($"Mix ID: {mix.Id}, Name: {mix.Name}, Level: {mix.Level}, IsMuted: {mix.IsMuted}, ImageName: {mix.Image?.Name}\n");

            var mixes = WaveLinkHandler?.Client?.StateManager.Mixes;
            _client.ChoiceUpdate(TouchPortalIdHelper.MixListId, mixes?.Select(m => m.Name).ToArray());

            _client.CreateState(TouchPortalIdHelper.MixMute(mix.Name!), $"{mix.Name!} muted", mix.IsMuted.ToString(), TouchPortalIdHelper.MixMutedCategoryName);
            _client.CreateState(TouchPortalIdHelper.MixLevel(mix.Name!), $"{mix.Name!} level", ToInt(mix.Level ?? 0).ToString(), TouchPortalIdHelper.MixLevelCategoryName);


        };

        OnOutputDeviceAdded = (sender, outputDevice) =>
        {
            _logger.LogDebug("Received Output Devices added:");
            _logger.LogDebug($"Device ID: {outputDevice.Id}, Name: {outputDevice.Name}, Type: {outputDevice.Type}");

            var devices = WaveLinkHandler?.Client?.StateManager.OutputDevices;
            _client.ChoiceUpdate(TouchPortalIdHelper.OutputListId, devices?.Select(d => d.Name).ToArray());

            foreach (var output in outputDevice.Outputs ?? [])
            {
                _logger.LogDebug($"Input ID: {output.Id}, Name: {output.Name}, Level: {output.Level}, IsMuted: {output.IsMuted}");
                _client.CreateState(TouchPortalIdHelper.OutputMute(output.Name!), $"{output.Name!} muted", output.IsMuted.ToString(), TouchPortalIdHelper.OutputMutedCategoryName);
                _client.CreateState(TouchPortalIdHelper.OutputLevel(output.Name!), $"{output.Name!} level", ToInt(output.Level ?? 0).ToString(), TouchPortalIdHelper.OutputLevelCategoryName);
            }
        };

        OnChannelRemoved = (sender, channel) =>
        {
            _logger.LogDebug("Received Channels removed:");
            _logger.LogDebug($"Channel ID: {channel.Id}, Name: {channel.Name}, Level: {channel.Level}, IsMuted: {channel.IsMuted}, Type: {channel.Type}, ImageNull: {string.IsNullOrEmpty(channel.Image?.ImgData)}");

            var channels = WaveLinkHandler?.Client?.StateManager.Channels;
            _client.ChoiceUpdate(TouchPortalIdHelper.ChannelListId, channels?.Select(c => c.Name).ToArray());

            _client.RemoveState(TouchPortalIdHelper.ChannelMute(channel.Name!));
            _client.RemoveState(TouchPortalIdHelper.ChannelLevel(channel.Name!));
        };

        OnInputDeviceRemoved = (sender, inputDevice) =>
        {
            _logger.LogDebug("Received Input Devices removed:");
            _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}");

            var devices = WaveLinkHandler?.Client?.StateManager.InputDevices;
            _client.ChoiceUpdate(TouchPortalIdHelper.InputListId, devices?.Select(d => d.Name).ToArray());

            _client.RemoveState(TouchPortalIdHelper.InputMute(inputDevice.Name!));
            _client.RemoveState(TouchPortalIdHelper.InputLevel(inputDevice.Name!));
        };

        OnMixRemoved = (sender, mix) =>
        {
            _logger.LogDebug("Received Mixes removed:");
            _logger.LogDebug($"Mix ID: {mix.Id}, Name: {mix.Name}, Level: {mix.Level}, IsMuted: {mix.IsMuted}, ImageName: {mix.Image?.Name}\n");

            var mixes = WaveLinkHandler?.Client?.StateManager.Mixes;
            _client.ChoiceUpdate(TouchPortalIdHelper.MixListId, mixes?.Select(m => m.Name).ToArray());

            _client.RemoveState(TouchPortalIdHelper.MixMute(mix.Name!));
            _client.RemoveState(TouchPortalIdHelper.MixLevel(mix.Name!));
        };

        OnOutputDeviceRemoved = (sender, outputDevice) =>
        {
            _logger.LogDebug("Received Output Devices removed:");
            _logger.LogDebug($"Device ID: {outputDevice.Id}, Name: {outputDevice.Name}, Type: {outputDevice.Type}");

            var outputs = WaveLinkHandler?.Client?.StateManager.OutputDevices;
            _client.ChoiceUpdate(TouchPortalIdHelper.OutputListId, outputs?.Select(o => o.Name).ToArray());

            _client.RemoveState(TouchPortalIdHelper.OutputMute(outputDevice.Name!));
            _client.RemoveState(TouchPortalIdHelper.OutputLevel(outputDevice.Name!));
        };

        OnSubscribedToFocusAppChanged = (sender, subscribed) =>
        {
            _logger.LogDebug("Subscribed to Focused App: {Subscribed}", subscribed);
        };




    }
    public void SubscribeToEvents()
    {
        WaveLinkHandler?.Client?.StateManager.OnChannelUpdated += OnChannelUpdated;
        WaveLinkHandler?.Client?.StateManager.OnFocusedAppUpdated += OnFocusedAppUpdated;
        WaveLinkHandler?.Client?.StateManager.OnInputDeviceUpdated += OnInputDeviceUpdated;
        WaveLinkHandler?.Client?.StateManager.OnMixUpdated += OnMixUpdated;
        WaveLinkHandler?.Client?.StateManager.OnOutputDeviceUpdated += OnOutputDeviceUpdated;
        WaveLinkHandler?.Client?.StateManager.OnChannelAdded += OnChannelAdded;
        WaveLinkHandler?.Client?.StateManager.OnInputDeviceAdded += OnInputDeviceAdded;
        WaveLinkHandler?.Client?.StateManager.OnMixAdded += OnMixAdded;
        WaveLinkHandler?.Client?.StateManager.OnOutputDeviceAdded += OnOutputDeviceAdded;
        WaveLinkHandler?.Client?.StateManager.OnChannelRemoved += OnChannelRemoved;
        WaveLinkHandler?.Client?.StateManager.OnInputDeviceRemoved += OnInputDeviceRemoved;
        WaveLinkHandler?.Client?.StateManager.OnSubscribedToFocusAppChanged += OnSubscribedToFocusAppChanged;

        WaveLinkHandler?.Client?.MessageRouter.OnReceivedGetInputDevices += OnReceivedGetInputDevices;
        WaveLinkHandler?.Client?.MessageRouter.OnReceivedGetOutputDevices += OnReceivedGetOutputDevices;
        WaveLinkHandler?.Client?.MessageRouter.OnReceivedGetChannels += OnReceivedGetChannels;
        WaveLinkHandler?.Client?.MessageRouter.OnReceivedGetMixes += OnReceivedGetMixes;
    }

    public void ClearEvents()
    {
        WaveLinkHandler?.Client?.StateManager.OnChannelUpdated -= OnChannelUpdated;
        WaveLinkHandler?.Client?.StateManager.OnFocusedAppUpdated -= OnFocusedAppUpdated;
        WaveLinkHandler?.Client?.StateManager.OnInputDeviceUpdated -= OnInputDeviceUpdated;
        WaveLinkHandler?.Client?.StateManager.OnMixUpdated -= OnMixUpdated;
        WaveLinkHandler?.Client?.StateManager.OnOutputDeviceUpdated -= OnOutputDeviceUpdated;
        WaveLinkHandler?.Client?.StateManager.OnChannelAdded -= OnChannelAdded;
        WaveLinkHandler?.Client?.StateManager.OnInputDeviceAdded -= OnInputDeviceAdded;
        WaveLinkHandler?.Client?.StateManager.OnMixAdded -= OnMixAdded;
        WaveLinkHandler?.Client?.StateManager.OnOutputDeviceAdded -= OnOutputDeviceAdded;
        WaveLinkHandler?.Client?.StateManager.OnChannelRemoved -= OnChannelRemoved;
        WaveLinkHandler?.Client?.StateManager.OnInputDeviceRemoved -= OnInputDeviceRemoved;
        WaveLinkHandler?.Client?.StateManager.OnSubscribedToFocusAppChanged -= OnSubscribedToFocusAppChanged;

        WaveLinkHandler?.Client?.MessageRouter.OnReceivedGetInputDevices -= OnReceivedGetInputDevices;
        WaveLinkHandler?.Client?.MessageRouter.OnReceivedGetOutputDevices -= OnReceivedGetOutputDevices;
        WaveLinkHandler?.Client?.MessageRouter.OnReceivedGetChannels -= OnReceivedGetChannels;
        WaveLinkHandler?.Client?.MessageRouter.OnReceivedGetMixes -= OnReceivedGetMixes;
    }

    // input actions
    public void SetMuteInput(ActionEvent message)
    {
        var inputName = message[TouchPortalIdHelper.InputListId] ?? "<null>";
        var muteValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetMuteInput))] ?? "<null>";

        if (!string.IsNullOrEmpty(inputName) && !string.IsNullOrEmpty(muteValue))
        {
            WaveLinkHandler?.SetInput(inputName, muteValue);
        }
    }
    public void SetLevelInput(ActionEvent message)
    {
        var inputName = message[TouchPortalIdHelper.InputListId] ?? "<null>";
        var levelValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetLevelInput))] ?? "<null>";

        if (!string.IsNullOrEmpty(inputName) &&
            !string.IsNullOrEmpty(levelValue) &&
            decimal.TryParse(levelValue, out decimal newLevel))
        {
            WaveLinkHandler?.SetInput(inputName, null, newLevel);
        }
    }

    // output actions
    public void SetMuteOutput(ActionEvent message)
    {
        var outputName = message[TouchPortalIdHelper.OutputListId] ?? "<null>";
        var muteValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetMuteOutput))] ?? "<null>";

        if (!string.IsNullOrEmpty(outputName) && !string.IsNullOrEmpty(muteValue))
        {
            WaveLinkHandler?.SetOutput(outputName, muteValue);
        }
    }
    public void SetLevelOutput(ActionEvent message)
    {
        var outputName = message[TouchPortalIdHelper.OutputListId] ?? "<null>";
        var levelValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetLevelOutput))] ?? "<null>";

        if (!string.IsNullOrEmpty(outputName) &&
            !string.IsNullOrEmpty(levelValue) &&
            decimal.TryParse(levelValue, out decimal newLevel))
        {
            WaveLinkHandler?.SetOutput(outputName, null, newLevel);
        }
    }

    // channel actions
    public void SetLevelChannel(ActionEvent message)
    {
        var channelName = message[TouchPortalIdHelper.ChannelListId] ?? "<null>";
        var levelValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetLevelChannel))] ?? "<null>";

        if (!string.IsNullOrEmpty(channelName) &&
            !string.IsNullOrEmpty(levelValue) &&
            decimal.TryParse(levelValue, out decimal newLevel))
        {
            WaveLinkHandler?.SetChannel(channelName, null, newLevel);
        }
    }

    public void SetMuteChannel(ActionEvent message)
    {
        var channelName = message[TouchPortalIdHelper.ChannelListId] ?? "<null>";
        var muteValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetMuteChannel))] ?? "<null>";

        if (!string.IsNullOrEmpty(channelName) && !string.IsNullOrEmpty(muteValue))
        {
            WaveLinkHandler?.SetChannel(channelName, muteValue);
        }
    }

    // mix actions
    public void SetLevelMix(ActionEvent message)
    {
        var mixName = message[TouchPortalIdHelper.MixListId] ?? "<null>";
        var levelValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetLevelMix))] ?? "<null>";

        if (!string.IsNullOrEmpty(mixName) &&
            !string.IsNullOrEmpty(levelValue) &&
            decimal.TryParse(levelValue, out decimal newLevel))
        {
            WaveLinkHandler?.SetMix(mixName, null, newLevel);
        }
    }

    public void SetMuteMix(ActionEvent message)
    {
        var mixName = message[TouchPortalIdHelper.MixListId] ?? "<null>";
        var muteValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetMuteMix))] ?? "<null>";

        if (!string.IsNullOrEmpty(mixName) && !string.IsNullOrEmpty(muteValue))
        {
            WaveLinkHandler?.SetMix(mixName, muteValue);
        }
    }

    // subscribe to focus app changes
    public void SetFocusAppNotification(ActionEvent message)
    {
        var subValue = message[TouchPortalIdHelper.ActionDataValue(nameof(SetFocusAppNotification))] ?? "<null>";

        if (!string.IsNullOrEmpty(subValue))
        {
            WaveLinkHandler?.SetFocusAppSubscription(subValue);
        }
    }

    // add focused app to channel
    public void AddToChannel(ActionEvent message)
    {
        var channelName = message[TouchPortalIdHelper.ChannelListId] ?? "<null>";
        if (!string.IsNullOrEmpty(channelName))
        {
            WaveLinkHandler?.AddToChannel(channelName);
        }
    }
    public int ToInt(decimal value)
    {
        return (int)(value * 100);
    }
}

