using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using WaveLink.SDK;
using WaveLink.SDK.Models;

namespace WaveLink.Tests;

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
                Version = "3.0.0.1755", // typically should not be necessary to match exact version
                Build = 1755, // typically should not be necessary to match exact build
                InterfaceRevision = 1
            }
        };

        client.MessageRouter.OnReceivedAppInfo += (s, response) =>
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

        WaveLinkRequestMethod request = new(WaveRequestId.getApplicationInfo);
        await client.SendRequestAsync(request);

        // Wait for assert to run inside event
        await tcs.Task;
    }
    [Fact]
    public async Task Should_SendGetInput_WithResponse()
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
                Version = "3.0.0.1755", // typically should not be necessary to match exact version
                Build = 1755, // typically should not be necessary to match exact build
                InterfaceRevision = 1
            }
        };

        client.MessageRouter.OnReceivedGetInputDevices += async (s, response) =>
        {
            //_logger.LogDebug("Received Input Devices Info:");
            //foreach (var inputDevice in response.Result.InputDevices)
            //{
            //    _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}, IsMuted: {inputDevice.IsMuted}");
            //    foreach (var input in inputDevice.Inputs)
            //    {
            //        _logger.LogDebug($"Input ID: {input.Id}, Name: {input.Name}, Level: {input.Gain.Value}, Min: {input.Gain.Min}, Max: {input.Gain.Max}\n");
            //    }
            //}
        };

        WaveLinkRequestMethod request = new(WaveRequestId.getApplicationInfo);
        await client.SendRequestAsync(request);

        // Wait for assert to run inside event
        await tcs.Task;
    }
}

