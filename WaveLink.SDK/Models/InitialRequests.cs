using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WaveLink.SDK.Enums;
namespace WaveLink.SDK.Models;
//public class JsonRpcRequest
//{
//    public readonly string jsonrpc = Statics.JsonRpcVersion;
//    public readonly JsonObject JsonObject = [];
//    public string Method { get; set; }
//    public WaveRequestId Id { get; set; }
//    public JsonObject? ParamsObj;
//    public JsonRpcRequest(WaveRequestId id, JsonObject? paramsObj)
//    {
//        Method = id.ToString();
//        Id = id;
//        ParamsObj = paramsObj;

//        JsonObject = new JsonObject
//        {
//            ["id"] = (int)Id,
//            ["jsonrpc"] = jsonrpc,
//            ["method"] = Method
//        };

//        if (ParamsObj != null)
//        {
//            JsonObject["params"] = ParamsObj;
//        }
//    }
//    public string ToJsonString()
//    {
//        return JsonObject.ToJsonString();
//    }
//}
public class ApplicationInfoResponse : WaveLinkResponse<AppInfoResult>, IEquatable<ApplicationInfoResponse>
{
    public bool Equals(ApplicationInfoResponse? other)
    {
        if (other is null) return false;

        return
            JsonRpc == other.JsonRpc &&
            Id == other.Id &&
            ((Result == null && other.Result == null) || (Result != null && Result.Equals(other.Result)));
    }

    public override bool Equals(object? obj) => Equals(obj as ApplicationInfoResponse);

    public override int GetHashCode()
    {
        return HashCode.Combine(JsonRpc, Id, Result);
    }
}

public class AppInfoResult : IEquatable<AppInfoResult>
{
    public string AppID { get; set; } = string.Empty;

    public string OperatingSystem { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public int Build { get; set; }

    public int InterfaceRevision { get; set; }
    public bool Equals(AppInfoResult? other)
    {
        if (other is null) return false;

        return
            AppID == other.AppID &&
            OperatingSystem == other.OperatingSystem &&
            Name == other.Name &&
            Version == other.Version &&
            Build == other.Build &&
            InterfaceRevision == other.InterfaceRevision;
    }

    public override bool Equals(object? obj) => Equals(obj as AppInfoResult);

    public override int GetHashCode()
    {
        return HashCode.Combine(AppID, OperatingSystem, Name, Version, Build, InterfaceRevision);
    }
}

//public class InputDevicesResponse : WaveLinkResponse<InputDeviceResult>, IEquatable<ApplicationInfoResponse>
//{
//    public bool Equals(ApplicationInfoResponse? other)
//    {
//        if (other is null) return false;

//        return
//            JsonRpc == other.JsonRpc &&
//            Id == other.Id &&
//            ((Result == null && other.Result == null) || (Result != null && Result.Equals(other.Result)));
//    }

//    public override bool Equals(object? obj) => Equals(obj as ApplicationInfoResponse);

//    public override int GetHashCode()
//    {
//        return HashCode.Combine(JsonRpc, Id, Result);
//    }
//}

public class InputDeviceResult
{
    public List<InputDevice> InputDevices { get; set; } = new();
}
public class InputDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public WaveDeviceType Type { get; set; } = WaveDeviceType.Unknown;
    public List<Input> Inputs { get; set; } = new();
    public bool IsMuted { get; set; } = false;
}
public class Input
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public InputGain? Gain { get; set; }
}
public class InputGain
{
    public decimal Value { get; set; } = -1;
    public decimal Min { get; set; } = -1;
    public decimal Max { get; set; } = -1;
    //public object LookupTable { get; set; }
}

//public class OutputDevicesResponse : WaveLinkResponse<OutputDeviceResult> { }

public class OutputDeviceResult
{
    public List<OutputDevice> OutputDevices { get; set; } = new();
}
public class OutputDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public WaveDeviceType Type { get; set; } = WaveDeviceType.Unknown;
    public List<Output> Outputs { get; set; } = new();
}
public class Output
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Level { get; set; }
    public string MixId { get; set; } = string.Empty;
    public bool IsMuted { get; set; }
}
public class ChannelsResult
{
    public List<Channel> Channels { get; set; } = new();
}
public class Channel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // unsure what this is exaxtly, i only have seen "software"
    public List<ChannelMix> Mixes { get; set; } = new();
    public decimal Level { get; set; } = -1;
    public bool IsMuted { get; set; } = false;
    public List<App> Apps { get; set; } = new();
    public List<ChannelEffect> Effects { get; set; } = new();
    public ChannelImage Image { get; set; } = new ChannelImage();

}
public class ChannelMix
{
    public string Id { get; set; } = string.Empty;
    public decimal Level { get; set; }
    public bool IsMuted { get; set; }
}
public class App
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
public class ChannelEffect
{
    
}
public class ChannelImage
{
    public string ImgData { get; set; } = string.Empty;
}

public class MixesResult
{
    public List<Mix> Mixes { get; set; } = new();
}
public class Mix
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Level { get; set; }
    public bool IsMuted { get; set; }
    public MixImage Image { get; set; } = new MixImage();
}
public class MixImage
{ 
    public string Name { get; set; } = string.Empty;
}
