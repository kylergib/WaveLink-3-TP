using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using WaveLink.SDK;
using WaveLink.SDK.Models;

namespace WaveLink.Test;

public class WaveLinkClientTests : IAsyncLifetime
{
    public WaveLinkClient client;
    public WaveLinkClientTests()
    {
        client = new WaveLinkClient(null);
    }

    public Task DisposeAsync()
    {
        // cleanup after each test (optional)
        return client.CloseAsync();
    }

    public async Task InitializeAsync()
    {
        
        await client.ConnectAsync();
    }

    [Fact]
    public async Task Should_SendGetAppInfo_WithResponse()
    {
        var tcs = new TaskCompletionSource();

        var expectedInfoResponse = new ApplicationInfoResponse()
        {
            JsonRpc = Statics.JsonRpcVersion,
            Id = (int)WaveRequestId.getApplicationInfo,
            Result = new()
            {
                AppID = "EWL",
                OperatingSystem = "windows", // TODO: make this dynamic based off current machine
                Name = "Elgato Wave Link",
                Version = "3.0.0.1635", // typically should not be necessary to match exact version
                Build = 1635, // typically should not be necessary to match exact build
                InterfaceRevision = 1
            }
        };

        client.OnReceivedAppInfo += (s, response) =>
        {
            try
            {
                Assert.Equal(expectedInfoResponse, response);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        };

        JsonRpcRequest request = new(WaveRequestId.getApplicationInfo, null);
        await client.SendJsonRequestAsync(request);

        // Wait for assert to run inside event
        await tcs.Task;
    }
}

