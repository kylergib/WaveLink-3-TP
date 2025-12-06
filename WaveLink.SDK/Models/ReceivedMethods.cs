namespace WaveLink.SDK.Models;

public enum ReceivedMethods
{
    channelChanged,
    channelsChanged,
    focusedAppChanged,
    mixChanged,
    mixesChanged,
    outputDeviceChanged,
    outputDevicesChanged
}
public class OutputDevicesChangedInfo
{
    public string MainOutput { get; set; } = string.Empty;
    public List<OutputDevice> OutputDevices { get; set; } = new();
}
