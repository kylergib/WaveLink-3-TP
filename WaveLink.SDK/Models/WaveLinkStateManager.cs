using System.Text.Json;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;

namespace WaveLink.SDK.Models;

public class WaveLinkStateManager
{
    private readonly WaveLinkMessageRouter MessageRouter;
    private readonly ILogger _logger;
    public AppInfoResult AppInfo { get; private set; } = new();
    public App? FocusedApp { get; private set; }
    public List<InputDevice> InputDevices { get; private set; } = [];
    public List<Mix> Mixes { get; private set; } = [];
    public List<OutputDevice> OutputDevices { get; private set; } = [];
    public List<Channel> Channels { get; private set; } = [];
    public bool SubscribedToFocusApp { get; set; } = false;

    public event EventHandler<WaveLinkStateEvent<Channel>>? OnChannelUpdated;
    public event EventHandler<App>? OnFocusedAppUpdated;
    public event EventHandler<WaveLinkStateEvent<InputDevice>>? OnInputDeviceUpdated;
    public event EventHandler<WaveLinkStateEvent<Mix>>? OnMixUpdated;
    public event EventHandler<WaveLinkStateEvent<OutputDevice>>? OnOutputDeviceUpdated;


    public event EventHandler<WaveLinkStateEvent<Channel>>? OnChannelAdded;
    public event EventHandler<WaveLinkStateEvent<InputDevice>>? OnInputDeviceAdded;
    public event EventHandler<WaveLinkStateEvent<Mix>>? OnMixAdded;
    public event EventHandler<WaveLinkStateEvent<OutputDevice>>? OnOutputDeviceAdded;

    public event EventHandler<WaveLinkStateEvent<Channel>>? OnChannelRemoved;
    public event EventHandler<WaveLinkStateEvent<InputDevice>>? OnInputDeviceRemoved;
    public event EventHandler<WaveLinkStateEvent<Mix>>? OnMixRemoved;
    public event EventHandler<WaveLinkStateEvent<OutputDevice>>? OnOutputDeviceRemoved;

    public event EventHandler<bool>? OnSubscribedToFocusAppChanged;

    public Dictionary<int, object> SendState { get; private set; } = [];
    public int SendCount { get; set; } = 11;

    public WaveLinkStateManager(ILogger logger, WaveLinkMessageRouter messageRouter)
    {
        MessageRouter = messageRouter;
        _logger = logger;
        Initialize();
    }
    public void Initialize()
    {
        MessageRouter.OnReceivedAppInfo += (s, e) =>
        {
            AppInfo = e.Result ?? AppInfo;
            _logger.LogDebug("WaveLink App Info initialized: {AppInfo}", AppInfo);
        };
        MessageRouter.OnReceivedGetInputDevices += (s, e) =>
        {
            InputDevices = e.Result?.InputDevices ?? InputDevices;
            _logger.LogDebug("WaveLink Input Devices initialized: {InputDevicesCount} devices", InputDevices.Count);
        };
        MessageRouter.OnReceivedGetOutputDevices += (s, e) =>
        {
            OutputDevices = e.Result?.OutputDevices ?? OutputDevices;
            _logger.LogDebug("WaveLink Output Devices initialized: {OutputDevicesCount} devices", OutputDevices.Count);
        };
        MessageRouter.OnReceivedGetChannels += (s, e) =>
        {
            Channels = e.Result?.Channels ?? Channels;
            _logger.LogDebug("WaveLink Channels initialized: {ChannelsCount} channels", Channels.Count);
        };
        MessageRouter.OnReceivedGetMixes += (s, e) =>
        {
            Mixes = e.Result?.Mixes ?? Mixes;
            _logger.LogDebug("WaveLink Mixes initialized: {MixesCount} mixes", Mixes.Count);
        };

        MessageRouter.OnReceivedChannelChanged += (s, e) =>
        {
            var updatedChannel = e.Params;
            if (updatedChannel == null) return;
            UpdateChannel(updatedChannel, false);
        };

        MessageRouter.OnReceivedChannelsChanged += (s, e) =>
        {
            var updatedChannels = e.Params?.Channels;
            if (updatedChannels == null) return;
            UpdateChannels(updatedChannels, false);
        };
        MessageRouter.OnReceivedFocusedAppChanged += (s, e) =>
        {
            var updatedApp = e.Params;
            if (updatedApp == null) return;
            UpdateFocusedApp(updatedApp);
        };
        MessageRouter.OnReceivedInputDeviceChanged += (s, e) =>
        {
            var updatedDevice = e.Params;
            if (updatedDevice == null) return;
            UpdateInputDevice(updatedDevice, false);
        };
        MessageRouter.OnReceivedInputDevicesChanged += (s, e) =>
        {
            var updatedDevices = e.Params?.Inputs;
            if (updatedDevices == null) return;
            UpdateInputDevices(updatedDevices, false);
        };
        MessageRouter.OnReceivedMixChanged += (s, e) =>
        {
            var updatedMix = e.Params;
            if (updatedMix == null) return;
            UpdateMix(updatedMix, false);
        };
        MessageRouter.OnReceivedMixesChanged += (s, e) =>
        {
            var updatedMixes = e.Params?.Mixes;
            if (updatedMixes == null) return;
            UpdateMixes(updatedMixes, false);
        };
        MessageRouter.OnReceivedOutputDeviceChanged += (s, e) =>
        {
            var updatedDevice = e.Params;
            if (updatedDevice == null) return;
            UpdateOutputDevice(updatedDevice, false);
        };
        MessageRouter.OnReceivedOutputDevicesChanged += (s, e) =>
        {
            var updatedDevices = e.Params?.OutputDevices;
            if (updatedDevices == null) return;
            UpdateOutputDevices(updatedDevices, false);
        };

        MessageRouter.OnReceivedResult += (s, e) =>
        {

            JsonDocument doc = JsonDocument.Parse(e);
            var root = doc.RootElement;
            if (!root.TryGetProperty("id", out var resultElement) || !resultElement.TryGetInt32(out int thisId) || !SendState.TryGetValue(thisId, out var sendState))
            {
                _logger.LogError("Could not process this result: {e}", e);
                return;
            }
            SendState.Remove(thisId);
            if (!sendState.GetType().IsGenericType || sendState.GetType().GetGenericTypeDefinition() != typeof(WaveLinkSendMethod<>))
            {
                _logger.LogError("Send state is not of expected generic type: {SendStateType}", sendState.GetType().Name);
                return;
            }
            var paramType = sendState.GetType().GetProperty("Params")?.GetValue(sendState)?.GetType();
           
            switch (paramType?.Name ?? string.Empty)
            {
                case nameof(AddAppToChannelInfo):
                case nameof(MethodChannelInfo):
                    var channel = JsonSerializer.Deserialize<Channel>(root.GetProperty("result").GetRawText(), Statics.JsonSerializerOptionsDefault);
                    if (channel != null) UpdateChannel(channel, true);
                    break;
                case nameof(MethodMixInfo):
                    var mix = JsonSerializer.Deserialize<Mix>(root.GetProperty("result").GetRawText(), Statics.JsonSerializerOptionsDefault);
                    if (mix != null) UpdateMix(mix, true);
                    break;
                case nameof(MethodInputDeviceInfo):
                    var inputDevice = JsonSerializer.Deserialize<InputDevice>(root.GetProperty("result").GetRawText(), Statics.JsonSerializerOptionsDefault);
                    if (inputDevice != null) UpdateInputDevice(inputDevice, true);
                    break;
                 case nameof(MethodOutputDeviceInfo):
                    var outputDevice = JsonSerializer.Deserialize<OutputDevice>(root.GetProperty("result").GetRawText(), Statics.JsonSerializerOptionsDefault);
                    if (outputDevice != null) UpdateOutputDevice(outputDevice, true);
                    break;
                case nameof(MethodSubscriptionInfo):
                    var subscriptionInfo = JsonSerializer.Deserialize<MethodSubscriptionInfo>(root.GetProperty("result").GetRawText(), Statics.JsonSerializerOptionsDefault);
                    if (subscriptionInfo != null) {
                        SubscribedToFocusApp = subscriptionInfo.FocusedAppChanged.IsEnabled;
                        OnSubscribedToFocusAppChanged?.Invoke(this, SubscribedToFocusApp);
                    }
                    break;
                default:
                    _logger.LogWarning("Received result that is not recognized:");
                    _logger.LogWarning("{message}", e.Minify());
                    break;
            }
        };

    }
    public Channel? FindChannel(string id)
    {
        var index = Channels.FindIndex(c => c.Id == id);
        if (index < 0) return null;
        return Channels[index];
    }
    //public void UpdateChannel(MuteChannelInfo channelInfo)
    //{
    //    var existingDevice = FindChannel(channelInfo.Id);
    //    if (existingDevice == null) return;

    //    existingDevice.IsMuted = channelInfo.IsMuted;
    //    OnChannelUpdated?.Invoke(this, existingDevice);
    //    _logger.LogDebug("Channel updated: {ChannelId} Muted: {IsMuted}", existingDevice.Id, existingDevice.IsMuted);

    //}
    public void UpdateChannel(Channel updatedChannel, bool isResult)
    {
        //var index = Channels.FindIndex(c => c.Id == updatedChannel.Id);
        //if (index < 0) return;

        var existingDevice = FindChannel(updatedChannel.Id);
        if (existingDevice == null) return;

        bool channelChanged = !existingDevice.Equals(updatedChannel) ||
                              !existingDevice.CompareMixes(updatedChannel.Mixes) ||
                              !existingDevice.CompareApps(updatedChannel.Apps) ||
                              !existingDevice.CompareEffects(updatedChannel.Effects);

        if (!channelChanged) return;

        existingDevice.Name = updatedChannel.Name ?? existingDevice.Name;
        existingDevice.Type = updatedChannel.Type ?? existingDevice.Type;

        foreach (var updatedMix in updatedChannel.Mixes ?? [])
        {
            var mix = existingDevice.Mixes?.FirstOrDefault(m => m.Id == updatedMix.Id);
            if (mix ==null) continue;

            mix.Level = updatedMix.Level ?? mix.Level;
            mix.IsMuted = updatedMix.IsMuted ?? mix.IsMuted;
        }
        existingDevice.Level = updatedChannel.Level ?? existingDevice.Level;
        existingDevice.IsMuted = updatedChannel.IsMuted ?? existingDevice.IsMuted;
        existingDevice.Apps = updatedChannel.Apps ?? existingDevice.Apps;
        existingDevice.Effects = updatedChannel.Effects ?? existingDevice.Effects;
        existingDevice.Image = updatedChannel.Image ?? existingDevice.Image;

        OnChannelUpdated?.Invoke(this, new(existingDevice, isResult));
        _logger.LogDebug("Channel updated: {ChannelId}", existingDevice.Id);
    }

    public void UpdateChannels(List<Channel> updatedChannels, bool isResult)
    {
        var currentChannels = Channels.Where(d => updatedChannels.Any(ud => ud.Id == d.Id)).ToList();
        var missingChannels = updatedChannels.Where(ud => !Channels.Any(d => d.Id == ud.Id)).ToList();
        var removedChannels = Channels.Where(d => !updatedChannels.Any(ud => ud.Id == d.Id)).ToList();

        foreach (var updatedChannel in currentChannels)
        {
            UpdateChannel(updatedChannel, isResult);
        }
        foreach (var missingChannel in missingChannels)
        {
            Channels.Add(missingChannel);
            OnChannelAdded?.Invoke(this, new(missingChannel, isResult));
            _logger.LogDebug("Channel added: {ChannelId}", missingChannel.Id);
        }
        foreach (var removedChannel in removedChannels)
        {
            Channels.Remove(removedChannel);
            OnChannelRemoved?.Invoke(this, new(removedChannel, isResult));
            _logger.LogDebug("Channel removed: {ChannelId}", removedChannel.Id);
        }
    }
    public void UpdateFocusedApp(App updatedApp)
    {
        if (FocusedApp == null)
        {
            FocusedApp = updatedApp;
            return;
        }
        if (FocusedApp.Equals(updatedApp)) return;
        FocusedApp = updatedApp;
        OnFocusedAppUpdated?.Invoke(this, updatedApp);
        _logger.LogDebug("Focused App updated: {AppId}", updatedApp.Id);
    }
    public void UpdateInputDevice(InputDevice updatedDevice, bool isResult)
    {
        var index = InputDevices.FindIndex(d => d.Id == updatedDevice.Id);
        if (index < 0) return;

        var existingDevice = InputDevices[index];

        var deviceChanged = !existingDevice.Equals(updatedDevice);

        existingDevice.Name = updatedDevice.Name ?? existingDevice.Name;
        existingDevice.Type = updatedDevice.Type ?? existingDevice.Type;

        var inputsChanged = false;

        foreach (var input in existingDevice.Inputs ?? [])
        {
            var updatedInput = updatedDevice.Inputs?.FirstOrDefault(i => i.Id == input.Id);
            bool inputChanged = (updatedInput != null && !input.Equals(updatedInput)) ||
                                !input.CompareEffects(updatedInput?.Effects ?? new()) ||
                                !input.CompareDspEffects(updatedInput?.DspEffects ?? new());

            if (inputChanged)
            {
                inputsChanged = true;
                input.Name = updatedInput?.Name ?? input.Name;
                input.Gain = updatedInput?.Gain ?? input.Gain;
                input.IsMuted = updatedInput?.IsMuted ?? input.IsMuted;
                input.IsGainLockOn = updatedInput?.IsGainLockOn ?? input.IsGainLockOn;
                input.MicPcMixId = updatedInput?.MicPcMixId ?? input.MicPcMixId;
                input.Effects = updatedInput?.Effects ?? input.Effects;
                input.DspEffects = updatedInput?.DspEffects ?? input.DspEffects;
            }
        }

        if (!deviceChanged && !inputsChanged) return;

        //InputDevices[index] = updatedDevice;
        OnInputDeviceUpdated?.Invoke(this, new(existingDevice, isResult));
        _logger.LogDebug("Input Device updated: {DeviceId}", existingDevice.Id);
    }
    public void UpdateInputDevices(List<InputDevice> updatedDevices, bool isResult)
    {
        var currentDevices = InputDevices.Where(d => updatedDevices.Any(ud => ud.Id == d.Id)).ToList();
        var missingDevices = updatedDevices.Where(ud => !InputDevices.Any(d => d.Id == ud.Id)).ToList();
        var removedDevices = InputDevices.Where(d => !updatedDevices.Any(ud => ud.Id == d.Id)).ToList();

        foreach (var updatedDevice in currentDevices)
        {
            UpdateInputDevice(updatedDevice, isResult);
        }
        foreach (var missingDevice in missingDevices)
        {
            InputDevices.Add(missingDevice);
            OnInputDeviceAdded?.Invoke(this, new(missingDevice, isResult));
            _logger.LogDebug("Input Device added: {DeviceId}", missingDevice.Id);
        }
        foreach (var removedDevice in removedDevices)
        {
            InputDevices.Remove(removedDevice);
            OnInputDeviceRemoved?.Invoke(this, new(removedDevice, isResult));
            _logger.LogDebug("Input Device removed: {DeviceId}", removedDevice.Id);
        }
    }
    public void UpdateMix(Mix updatedMix, bool isResult)
    {
        var index = Mixes.FindIndex(m => m.Id == updatedMix.Id);
        if (index < 0) return;

        var existingMix = Mixes[index];

        bool mixChanged = !existingMix.Equals(updatedMix);
        if (!mixChanged) return;

        existingMix.Name = updatedMix.Name ?? existingMix.Name;
        existingMix.Level = updatedMix.Level ?? existingMix.Level;
        existingMix.IsMuted = updatedMix.IsMuted ?? existingMix.IsMuted;
        existingMix.Image = updatedMix.Image ?? existingMix.Image;

        //Mixes[index] = updatedMix;
        OnMixUpdated?.Invoke(this, new(existingMix, isResult));
        _logger.LogDebug("Mix updated: {MixId}", existingMix.Id);
    }
    public void UpdateMixes(List<Mix> updatedMixes, bool isResult)
    {
        var currentMixes = Mixes.Where(d => updatedMixes.Any(ud => ud.Id == d.Id)).ToList();
        var missingMixes = updatedMixes.Where(ud => !Mixes.Any(d => d.Id == ud.Id)).ToList();
        var removedMixes = Mixes.Where(d => !updatedMixes.Any(ud => ud.Id == d.Id)).ToList();

        foreach (var updatedMix in currentMixes)
        {
            UpdateMix(updatedMix, isResult);
        }
        foreach (var missingMix in missingMixes)
        {
            Mixes.Add(missingMix);
            OnMixAdded?.Invoke(this, new(missingMix, isResult));
            _logger.LogDebug("Mix added: {MixId}", missingMix.Id);
        }
        foreach (var removedMix in removedMixes)
        {
            Mixes.Remove(removedMix);
            OnMixRemoved?.Invoke(this, new(removedMix, isResult));
            _logger.LogDebug("Mix removed: {MixId}", removedMix.Id);
        }
    }
    public void UpdateOutputDevice(OutputDevice updated, bool isResult)
    {
        var index = OutputDevices.FindIndex(d => d.Id == updated.Id);
        if (index < 0) return;

        var existingDevice = OutputDevices[index];

        bool deviceChanged = !existingDevice.Equals(updated);
        bool outputsChanged = false;

        existingDevice.Name = updated.Name ?? existingDevice.Name;
        existingDevice.Type = updated.Type ?? existingDevice.Type;

        foreach (var oldOutput in existingDevice.Outputs ?? [])
        {
            var newOutput = updated.Outputs?.FirstOrDefault(o => o.Id == oldOutput.Id);
            if (newOutput != null && !oldOutput.Equals(newOutput))
            {
                outputsChanged = true;
                oldOutput.Name = newOutput.Name ?? oldOutput.Name;
                oldOutput.Level = newOutput.Level ?? oldOutput.Level;
                oldOutput.MixId = newOutput.MixId ?? oldOutput.MixId;
                oldOutput.IsMuted = newOutput.IsMuted ?? oldOutput.IsMuted;
            }
        }

        if (!deviceChanged && !outputsChanged) return;

        //OutputDevices[index] = updated;

        OnOutputDeviceUpdated?.Invoke(this, new(existingDevice, isResult));
        _logger.LogDebug("Output Device existingDevice: {Name} - {DeviceId}", existingDevice.Name, existingDevice.Id);
    }
    public void UpdateOutputDevices(List<OutputDevice> updatedDevices, bool isResult)
    {
        var currentDevices = OutputDevices.Where(d => updatedDevices.Any(ud => ud.Id == d.Id)).ToList();
        var missingDevices = updatedDevices.Where(ud => !OutputDevices.Any(d => d.Id == ud.Id)).ToList();
        var removedDevices = OutputDevices.Where(d => !updatedDevices.Any(ud => ud.Id == d.Id)).ToList();

        foreach (var updatedDevice in currentDevices)
        {
            UpdateOutputDevice(updatedDevice, isResult);
        }
        foreach (var missingDevice in missingDevices)
        {
            OutputDevices.Add(missingDevice);
            OnOutputDeviceAdded?.Invoke(this, new(missingDevice, isResult));
            _logger.LogDebug("Output Device added: {DeviceId}", missingDevice.Id);
        }
        foreach (var removedDevice in removedDevices)
        {
            OutputDevices.Remove(removedDevice);
            OnOutputDeviceRemoved?.Invoke(this, new(removedDevice, isResult));
            _logger.LogDebug("Output Device removed: {DeviceId}", removedDevice.Id);
        }
    }

    public void Reset()
    {
        AppInfo = new();
        InputDevices.Clear();
        OutputDevices.Clear();
        Channels.Clear();
        _logger.LogDebug("WaveLink State Manager has been reset.");
    }
    public void Dispose()
    {
        MessageRouter.OnReceivedAppInfo -= null;
        MessageRouter.OnReceivedGetInputDevices -= null;
        MessageRouter.OnReceivedGetOutputDevices -= null;
        MessageRouter.OnReceivedGetChannels -= null;
        _logger.LogDebug("WaveLink State Manager disposed.");
    }

}

public enum EventStatus
{
    Added,
    Removed,
    Updated
}

public record WaveLinkStateEvent<T>(T Item, bool IsResult);