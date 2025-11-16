using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WaveLink.SDK;
using WaveLink.SDK.Models;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string host = config["WaveLink:Host"] ?? Statics.Localhost;
int port = int.Parse(config["WaveLink:Port"] ?? Statics.DefaultPort.ToString());

Console.WriteLine("Starting wave link plugin");

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConfiguration(config.GetSection("Logging"));
    builder.AddSimpleConsole(options =>
    {
        options.TimestampFormat = "HH:mm:ss ";
        options.SingleLine = true;
        options.ColorBehavior = LoggerColorBehavior.Enabled;
    });
});
var _logger = loggerFactory.CreateLogger("WaveLinkConsoleApp");
WaveLinkClient client;
while (true)
{
    try
    {
        client = new(host, port, loggerFactory);
        // on connection, request application info
        client.OnConnection += async (s, e) =>
        {
            Console.WriteLine("Connected to Wave Link on port " + port);
            JsonRpcRequest request = new(WaveRequestId.getApplicationInfo, null);
            await client.SendJsonRequestAsync(request);
        };
        // print received application info
        client.OnReceivedAppInfo += (s, response) =>
        {
            _logger.LogDebug("Received Application Info:");
            _logger.LogDebug($"AppID: {response.Result.AppID}");
            _logger.LogDebug($"OperatingSystem: {response.Result.OperatingSystem}");
            _logger.LogDebug($"Name: {response.Result.Name}");
            _logger.LogDebug($"Version: {response.Result.Version}");
            _logger.LogDebug($"Build: {response.Result.Build}");
            _logger.LogDebug($"InterfaceRevision: {response.Result.InterfaceRevision}");
        };

        _logger.LogInformation($"Trying to connect: {host}:{port}");
        await client.ConnectAsync();

        break; // Exit the loop if connection is successful
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Connection failed: {ex.Message}");
        Console.WriteLine("Retrying in 5 seconds...");
        await Task.Delay(1000); // Wait for 5 seconds before retrying
    }
    port++;
}


Console.ReadLine();

await client.CloseAsync();