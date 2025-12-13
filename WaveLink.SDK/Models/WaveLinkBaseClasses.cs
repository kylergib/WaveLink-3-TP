using System.Text.Json.Serialization;

namespace WaveLink.SDK.Models;
public class WaveLinkRequest
{
    public int Id { get; set; }
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = Statics.JsonRpcVersion;
    public override string ToString() { return System.Text.Json.JsonSerializer.Serialize(this);  }
}
public class WaveLinkRequestMethod : WaveLinkRequest
{
    public string Method { get; set; } = string.Empty;
    public WaveLinkRequestMethod(WaveRequestId waveRequestId)
    {
        Id = (int)waveRequestId;
        Method = waveRequestId.ToString();
    }
} //WaveLinkSendMethod<T>
public class WaveLinkSendMethod<T> : WaveLinkRequest
{
    public WaveLinkMethod Method { get; set; }
    public T Params { get; set; }
    public WaveLinkSendMethod(WaveLinkMethod waveMethod,T objectData)
    {
        Id = 11;
        Method = waveMethod;
        Params = objectData;
    }
}
public class WaveLinkResponse<T>
{
    public int Id { get; set; }
    public string JsonRpc { get; set; } = string.Empty;
    
    public T? Result { get; set; }
}
public class WaveLinkRecievedMethod<T>
{
    public int? Id { get; set; }
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = Statics.JsonRpcVersion;
    public string Method { get; set; } = string.Empty;
    public T? Params { get; set; }
}
public class ChannelsChangedInfo
{
    public List<Channel> Channels { get; set; } = new();
}
public class MixesChangedReceived
{
    public List<Mix> Mixes { get; set; } = new();
}
public enum WaveRequestId
{
    getApplicationInfo = 0,
    getInputDevices = 1,
    getOutputDevices = 2,
    getChannels = 3,
    getMixes = 4
}
