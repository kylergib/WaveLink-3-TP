namespace WaveLink.SDK.Models;

public enum ReceivedMethods
{
    channelChanged,
    channelsChanged,
    focusedAppChanged,
    inputDeviceChanged,
    inputDevicesChanged,
    mixChanged,
    mixesChanged,
    outputDeviceChanged,
    outputDevicesChanged
}
public class OutputDevicesChangedInfo
{
    public List<OutputDevice> OutputDevices { get; set; } = new();
}

public class InputDevicesChangedInfo
{
    public List<InputDevice> Inputs { get; set; } = new();
}