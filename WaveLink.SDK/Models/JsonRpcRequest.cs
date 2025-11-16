using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace WaveLink.SDK.Models;
public class JsonRpcRequest
{
    public readonly string jsonrpc = Statics.JsonRpcVersion;
    public readonly JsonObject JsonObject = [];
    public string Method { get; set; }
    public WaveRequestId Id { get; set; }
    public JsonObject? ParamsObj;
    public JsonRpcRequest(WaveRequestId id, JsonObject? paramsObj)
    {
        Method = id.ToString();
        Id = id;
        ParamsObj = paramsObj;

        JsonObject = new JsonObject
        {
            ["id"] = (int)Id,
            ["jsonrpc"] = jsonrpc,
            ["method"] = Method
        };

        if (ParamsObj != null)
        {
            JsonObject["params"] = ParamsObj;
        }
    }
    public string ToJsonString()
    {
        return JsonObject.ToJsonString();
    }
}
public class ApplicationInfoResponse : IEquatable<ApplicationInfoResponse>
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("result")]
    public AppInfo? Result { get; set; }
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

public class AppInfo : IEquatable<AppInfo>
{
    [JsonPropertyName("appID")]
    public string AppID { get; set; } = string.Empty;

    [JsonPropertyName("operatingSystem")]
    public string OperatingSystem { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("build")]
    public int Build { get; set; }

    [JsonPropertyName("interfaceRevision")]
    public int InterfaceRevision { get; set; }
    public bool Equals(AppInfo? other)
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

    public override bool Equals(object? obj) => Equals(obj as AppInfo);

    public override int GetHashCode()
    {
        return HashCode.Combine(AppID, OperatingSystem, Name, Version, Build, InterfaceRevision);
    }
}

public enum WaveRequestId
{
    getApplicationInfo = 1
}