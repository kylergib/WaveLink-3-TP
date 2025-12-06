using System.Text.Json.Nodes;

namespace WaveLink.SDK.Models;
public static class JsonExtensions
{
    public static string Minify(this string json)
    {
        return JsonNode.Parse(json)?.ToJsonString() ?? json;
    }
}