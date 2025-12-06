using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WaveLink.SDK;
using WaveLink.SDK.Models;

public class Helpers
{
    public WaveLinkClient? client = null;
    public bool retry = true;
    public event EventHandler? OnClose;
    public string _appId = "EWL";
    public static void WriteHeader()
    {
        Console.WriteLine("=====================================");
        Console.WriteLine("      WaveLink SDK Console App      ");
        Console.WriteLine("=====================================");
        Console.WriteLine();
    }
    public async Task Start(ILogger _logger, ILoggerFactory loggerFactory, string host, int port)
    {
        while (retry) {
            _logger.LogInformation($"{client?.IsConnected}");
            try
            {
                client = new(host, port, loggerFactory);
                // on connection, request application info
                client.OnConnection += async (s, e) =>
                {
                    _logger.LogInformation("Connected to Wave Link on port " + port);
                    client.OnClose += OnClose;
                    WaveLinkRequestMethod request = new(WaveRequestId.getApplicationInfo);
                    await client.SendRequestAsync(request);
                };
                // print received application info
                client.MessageRouter.OnReceivedAppInfo += async (s, response) =>
                {
                    _logger.LogDebug("Received Application Info:");
                    _logger.LogDebug($"AppID: {response.Result.AppID}");
                    _logger.LogDebug($"OperatingSystem: {response.Result.OperatingSystem}");
                    _logger.LogDebug($"Name: {response.Result.Name}");
                    _logger.LogDebug($"Version: {response.Result.Version}");
                    _logger.LogDebug($"Build: {response.Result.Build}");
                    _logger.LogDebug($"InterfaceRevision: {response.Result.InterfaceRevision}");
                    
                    if (response.Result.AppID == _appId)
                    {
                        _logger.LogInformation($"Connected to wave link");
                        WaveLinkRequestMethod request = new(WaveRequestId.getInputDevices);
                        _ = client.SendRequestAsync(request);
                        WaveLinkRequestMethod outputRequest = new(WaveRequestId.getOutputDevices);
                        _ = client.SendRequestAsync(outputRequest);
                        WaveLinkRequestMethod channelRequest = new(WaveRequestId.getChannels);
                        _ = client.SendRequestAsync(channelRequest);
                        WaveLinkRequestMethod mixRequest = new(WaveRequestId.getMixes);
                        _ = client.SendRequestAsync(mixRequest);
                    } else
                    {
                        _logger.LogWarning("Connected to an app that was not Wave Link. Closing and retrying...");
                        port++;
                        await client.CloseAsync();
                    }
                };
                client.MessageRouter.OnReceivedGetInputDevices += async (s, response) =>
                {
                    _logger.LogDebug("Received Input Devices Info:");
                    foreach (var inputDevice in response.Result.InputDevices)
                    {
                        _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}, IsMuted: {inputDevice.IsMuted}");
                        foreach (var input in inputDevice.Inputs)
                        {
                            _logger.LogDebug($"Input ID: {input.Id}, Name: {input.Name}, Level: {input.Gain.Value}, Min: {input.Gain.Min}, Max: {input.Gain.Max}\n");
                        }
                    }
                };
                client.MessageRouter.OnReceivedGetOutputDevices += async (s, response) =>
               {
                   _logger.LogDebug("Received Output Devices Info:");
                   foreach (var inputDevice in response.Result.OutputDevices)
                   {
                       _logger.LogDebug($"Device ID: {inputDevice.Id}, Name: {inputDevice.Name}, Type: {inputDevice.Type}");
                       foreach (var output in inputDevice.Outputs)
                       {
                           _logger.LogDebug($"Input ID: {output.Id}, Name: {output.Name}, Level: {output.Level}, IsMuted: {output.IsMuted}");
                       }
                   }
                   _logger.LogDebug("");
               };
                client.MessageRouter.OnReceivedGetChannels += async (s, response) =>
              {
                  _logger.LogDebug("Received Channels Info:");
                  foreach (var channel in response.Result.Channels)
                  {
                      _logger.LogDebug($"Channel ID: {channel.Id}, Name: {channel.Name}, Level: {channel.Level}, IsMuted: {channel.IsMuted}, Type: {channel.Type}, ImageNull: {string.IsNullOrEmpty(channel.Image.ImgData)}");
                      foreach (var app in channel.Apps)
                      {
                          _logger.LogDebug($"App ID: {app.Id}, Name: {app.Name}");
                      }
                      foreach (var mix in channel.Mixes)
                      {
                          _logger.LogDebug($"Mix ID: {mix.Id}, Level: {mix.Level}, IsMuted: {mix.IsMuted}");
                      }
                      foreach (var effect in channel.Effects)
                      {
                          //_logger.LogDebug($"Effect ID: {effect.Id}, Name: {effect.Name}, Type: {effect.Type}");
                      }
                  }
                  _logger.LogDebug("");
              };
                client.MessageRouter.OnReceivedGetMixes += async (s, response) =>
             {
                 _logger.LogDebug("Received Mixes Info:");
                 foreach (var mix in response.Result.Mixes)
                 {
                     _logger.LogDebug($"Mix ID: {mix.Id}, Name: {mix.Name}, Level: {mix.Level}, IsMuted: {mix.IsMuted}, ImageName: {mix.Image.Name}\n");
                 }
             };

                _logger.LogInformation($"Trying to connect: {host}:{port}");
                await client.ConnectAsync();
                
                await client.WaitForCloseAsync();
                _logger.LogWarning("Wave Link closed. Reconnecting...");
                await Task.Delay(10000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed on port: {port}");
                Console.WriteLine("Retrying...");
                await Task.Delay(500);
                port++;
            }
            if (port == 1895)
            {
                port = 1884;
            }
        }
    }
}
