using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace WaveLink.SDK.Models;

public class WaveLinkMessageRouter
{
    private readonly ILogger _logger;
    // set up info
    public event EventHandler<ApplicationInfoResponse>? OnReceivedAppInfo;
    public event EventHandler<WaveLinkResponse<InputDeviceResult>>? OnReceivedGetInputDevices;
    public event EventHandler<WaveLinkResponse<OutputDeviceResult>>? OnReceivedGetOutputDevices;
    public event EventHandler<WaveLinkResponse<ChannelsResult>>? OnReceivedGetChannels;
    public event EventHandler<WaveLinkResponse<MixesResult>>? OnReceivedGetMixes;


    // received methods
    public event EventHandler<WaveLinkRecievedMethod<Channel>>? OnReceivedChannelChanged;
    public event EventHandler<WaveLinkRecievedMethod<ChannelsChangedInfo>>? OnReceivedChannelsChanged;
    public event EventHandler<WaveLinkRecievedMethod<App>>? OnReceivedFocusedAppChanged;
    public event EventHandler<WaveLinkRecievedMethod<InputDevice>>? OnReceivedInputDeviceChanged;
    public event EventHandler<WaveLinkRecievedMethod<InputDevicesChangedInfo>>? OnReceivedInputDevicesChanged;
    public event EventHandler<WaveLinkRecievedMethod<Mix>>? OnReceivedMixChanged;
    public event EventHandler<WaveLinkRecievedMethod<MixesChangedReceived>>? OnReceivedMixesChanged;
    public event EventHandler<WaveLinkRecievedMethod<OutputDevice>>? OnReceivedOutputDeviceChanged;
    public event EventHandler<WaveLinkRecievedMethod<OutputDevicesChangedInfo>>? OnReceivedOutputDevicesChanged;

    // default result
    public event EventHandler<string>? OnReceivedResult;

    public WaveLinkMessageRouter(ILogger logger)
    {
        _logger = logger;
    }

    public void Route(WaveRequestId id, string message)
    {
        switch (id)
        {
            case WaveRequestId.getApplicationInfo:
                var appInfo = JsonSerializer.Deserialize<ApplicationInfoResponse>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received Application Info: {AppInfo}", appInfo?.Result);
                if (appInfo != null) OnReceivedAppInfo?.Invoke(this, appInfo);
                break;
            case WaveRequestId.getInputDevices:
                var inputDevices = JsonSerializer.Deserialize<WaveLinkResponse<InputDeviceResult>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received Input Devices: {InputDevices}", inputDevices?.Result);
                if (inputDevices != null) OnReceivedGetInputDevices?.Invoke(this, inputDevices);
                break;
            case WaveRequestId.getOutputDevices:
                var outputDevices = JsonSerializer.Deserialize<WaveLinkResponse<OutputDeviceResult>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received Output Devices: {OutputDevices}", outputDevices?.Result);
                if (outputDevices != null) OnReceivedGetOutputDevices?.Invoke(this, outputDevices);
                break;
            case WaveRequestId.getChannels:
                var channels = JsonSerializer.Deserialize<WaveLinkResponse<ChannelsResult>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received channels: {Channels}", channels?.Result);
                if (channels != null) OnReceivedGetChannels?.Invoke(this, channels);
                break;
            case WaveRequestId.getMixes:
                var mixes = JsonSerializer.Deserialize<WaveLinkResponse<MixesResult>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received mixes: {Mixes}", mixes?.Result);
                if (mixes != null) OnReceivedGetMixes?.Invoke(this, mixes);
                break;
            default:
                OnReceivedResult?.Invoke(this, message);
                break;
        }
    }

    public void Route(ReceivedMethods method, string message)
    {
        switch (method)
        {
            case ReceivedMethods.channelChanged:
                var channelData = JsonSerializer.Deserialize<WaveLinkRecievedMethod<Channel>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received channelData: {channelData}", channelData?.Params);
                if (channelData != null) OnReceivedChannelChanged?.Invoke(this, channelData);
                break;
            case ReceivedMethods.channelsChanged:
                var channelsData = JsonSerializer.Deserialize<WaveLinkRecievedMethod<ChannelsChangedInfo>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received channelsData: {channelsData}", channelsData?.Params);
                if (channelsData != null) OnReceivedChannelsChanged?.Invoke(this, channelsData ?? new());
                break;
            case ReceivedMethods.focusedAppChanged:
                var app = JsonSerializer.Deserialize<WaveLinkRecievedMethod<App>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received app: {app}", app?.Params);
                if (app != null) OnReceivedFocusedAppChanged?.Invoke(this, app);
                break;
             case ReceivedMethods.inputDeviceChanged:
                var inputDevice = JsonSerializer.Deserialize<WaveLinkRecievedMethod<InputDevice>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received inputDevice: {inputDevice}", inputDevice?.Params);
                if (inputDevice != null) OnReceivedInputDeviceChanged?.Invoke(this, inputDevice);
                break;
            case ReceivedMethods.inputDevicesChanged:
                var inputDevices = JsonSerializer.Deserialize<WaveLinkRecievedMethod<InputDevicesChangedInfo>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received inputDevices: {inputDevices}", inputDevices?.Params);
                if (inputDevices != null) OnReceivedInputDevicesChanged?.Invoke(this, inputDevices);
                break;
            case ReceivedMethods.mixChanged:
                var mix = JsonSerializer.Deserialize<WaveLinkRecievedMethod<Mix>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received mix: {mix}", mix?.Params);
                if (mix != null) OnReceivedMixChanged?.Invoke(this, mix);
                break;
            case ReceivedMethods.mixesChanged:
                var mixes = JsonSerializer.Deserialize<WaveLinkRecievedMethod<MixesChangedReceived>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received mixes: {mixes}", mixes?.Params);
                if (mixes != null) OnReceivedMixesChanged?.Invoke(this, mixes);
                break;
            case ReceivedMethods.outputDeviceChanged:
                var outputDevice = JsonSerializer.Deserialize<WaveLinkRecievedMethod<OutputDevice>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received outputDevice: {outputDevice}", outputDevice?.Params);
                if (outputDevice != null) OnReceivedOutputDeviceChanged?.Invoke(this, outputDevice);
                break;
            case ReceivedMethods.outputDevicesChanged:
                var outputDevices = JsonSerializer.Deserialize<WaveLinkRecievedMethod<OutputDevicesChangedInfo>>(message, Statics.JsonSerializerOptionsDefault);
                _logger.LogDebug("Received outputDevices: {outputDevices}", outputDevices?.Params);
                if (outputDevices != null) OnReceivedOutputDevicesChanged?.Invoke(this, outputDevices);
                break;
            default:
                _logger.LogWarning("Received a method that is not implemented.");
                _logger.LogWarning("{message}", message.Minify());
                break;
        }
    }
}
