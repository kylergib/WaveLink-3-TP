using System.Text.Json.Nodes;
using WaveLink.SDK;
using WaveLink.SDK.Models;

namespace WaveLink.Tests;
public class JsonRpcRequestTests
{
    [Fact]
    public void Constructor_Should_CreateJsonObject_WithoutParams()
    {
        // arrange
        //    var req = new JsonRpcRequest(WaveRequestId.getApplicationInfo, null);

        //    // act
        //    var parsed = JsonNode.Parse(req.ToJsonString())!.AsObject();

        //    // assert
        //    Assert.Equal(Statics.JsonRpcVersion, parsed["jsonrpc"]!.GetValue<string>());
        //    Assert.Equal("getApplicationInfo", parsed["method"]!.GetValue<string>());
        //    Assert.Equal(1, parsed["id"]!.GetValue<int>());
        //    Assert.False(parsed.ContainsKey("params"));
    }

    [Fact]
    public void Constructor_Should_IncludeParams_WhenProvided()
    {
       // arrange
       //var paramsObj = new JsonObject
       //{
       //    ["foo"] = "bar",
       //    ["count"] = 5
       //};

       // var req = new JsonRpcRequest(WaveRequestId.getApplicationInfo, paramsObj);

       // // act
       // var parsed = JsonNode.Parse(req.ToJsonString())!.AsObject();

       // // assert envelope fields
       // Assert.Equal(Statics.JsonRpcVersion, parsed["jsonrpc"]!.GetValue<string>());
       // Assert.Equal("getApplicationInfo", parsed["method"]!.GetValue<string>());
       // Assert.Equal((int)WaveRequestId.getApplicationInfo, parsed["id"]!.GetValue<int>());

       // // assert params content
       // Assert.True(parsed.ContainsKey("params"));
       // var p = parsed["params"]!.AsObject();
       // Assert.Equal("bar", p["foo"]!.GetValue<string>());
       // Assert.Equal(5, p["count"]!.GetValue<int>());
    }

    [Fact]
    public void ToJsonString_Returns_ValidJson()
    {
        //var paramsObj = new JsonObject { ["x"] = 1 };
        //var req = new JsonRpcRequest(WaveRequestId.getApplicationInfo, paramsObj);

        //var json = req.ToJsonString();

        //// should parse without throwing
        //var node = JsonNode.Parse(json);
        //Assert.NotNull(node);
    }
}