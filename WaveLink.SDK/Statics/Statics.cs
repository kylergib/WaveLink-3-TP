using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WaveLink.SDK;
public static class Statics
{
    public static readonly string Localhost = "127.0.0.1";
    public static readonly int DefaultPort = 1884;

    public static readonly string JsonRpcVersion = "2.0";

    // default options to convert json
    public static JsonSerializerOptions JsonSerializerOptionsDefault = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

}