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
    public MainOutput? MainOutput { get; set; } = new();
    public List<OutputDevice> OutputDevices { get; set; } = new();
}
public class MainOutputChangedInfo
{
    public string OutputDeviceId { get; set; } = string.Empty;
    public string OutputId { get; set; } = string.Empty;
}

public class InputDevicesChangedInfo
{
    public List<InputDevice> Inputs { get; set; } = new();
}