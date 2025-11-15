using Microsoft.Extensions.Logging;
using WaveLinkSDK;

Console.WriteLine("Starting wave link plugin");

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
WaveLinkClient client;
int port = 1884;
while (true)
{
    try
    {
        client =  new(Statics.Localhost, port, loggerFactory);
        Console.WriteLine("Trying to connect..." + port);
        await client.ConnectAsync();

        // Send a message after the connection
        await Task.Delay(1000); // Add some delay to make sure the connection is established
        Console.WriteLine("after delay");
        await client.SendMessageAsync("""
            {"id":0,"jsonrpc":"2.0","method":"getApplicationInfo"}
            """);

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

client.Close();