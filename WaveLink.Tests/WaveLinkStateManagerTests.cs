using Microsoft.Extensions.Logging.Abstractions;
using WaveLink.SDK.Models;


namespace WaveLink.Tests;


public class WaveLinkStateManagerTests
{
    [Fact]
    public void OutputDeviceChanged_PreservesConfiguredDeviceName()
    {
        var router = new WaveLinkMessageRouter(NullLogger.Instance);
        var stateManager = new WaveLinkStateManager(NullLogger.Instance, router);


        router.Route(WaveRequestId.getOutputDevices, """
        {
          "jsonrpc": "2.0",
          "id": 2,
          "result": {
            "outputDevices": [{
              "id": "device-id",
              "name": "Mic - XLR",
              "deviceType": "commonWave",
              "outputs": [{ "id": "endpoint-id", "name": "Headphones", "isMuted": false, "level": 0.75, "mixId": "" }]
            }]
          }
        }
        """);


        router.Route(ReceivedMethods.outputDeviceChanged, """
        {
          "jsonrpc": "2.0",
          "method": "outputDeviceChanged",
          "params": {
            "id": "device-id",
            "name": "Headphones (2- Elgato Wave:XLR)",
            "deviceType": "commonWave",
            "outputs": [{ "id": "endpoint-id", "name": "Headphones (2- Elgato Wave:XLR)", "isMuted": false, "level": 0.75, "mixId": "PCM_IN_01_V_04_SD3" }]
          }
        }
        """);


        var outputDevice = stateManager.OutputDevices.SingleOrDefault(device => device.Name == "Mic - XLR");


        Assert.NotNull(outputDevice);
        Assert.Equal("endpoint-id", outputDevice.Outputs?.Single().Id);
        Assert.Equal("PCM_IN_01_V_04_SD3", outputDevice.Outputs?.Single().MixId);
    }
}