using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using WaveLink.SDK;
using WaveLink.SDK.Models;

namespace WaveLink.Plugin.Models;

public class WaveLinkHandler
{
    private readonly ILogger<WaveLinkHandler> _logger;
    public WaveLinkClient? Client = null;
    public bool Retry = true;
    public event EventHandler? OnConnection;
    public event EventHandler? OnClose;
    public bool SubscribeToFocusApp = true;
    public string _appId = "EWL";
    public string Host { get; set; }
    public int Port { get; set; }
    public ILoggerFactory _loggerFactory { get; set; }
    public List<int> WindowsPorts = new();
    public WaveLinkPort? WindowsWaveLinkPort { get; set; }
    public WaveLinkHandler(ILoggerFactory loggerFactory, string host, int port)
    {
        _logger = loggerFactory?.CreateLogger<WaveLinkHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        _loggerFactory = loggerFactory;
        Host = host;
        Port = port;
        if (OperatingSystem.IsWindows())
        {
            WindowsPorts = FindWaveLinkWebSocketWindows() ?? WindowsPorts;
            WindowsWaveLinkPort = GetNetShInfo();
        }
    }
    public async Task Start()
    {
        _ = Task.Run(() => Start(_loggerFactory, Host, Port));
    }
    public async Task Start(ILoggerFactory loggerFactory, string host, int port)
    {
        while (Retry)
        {
            if (OperatingSystem.IsWindows())
            {
                if (WindowsWaveLinkPort != null && WindowsWaveLinkPort.Port != null &&
                    WindowsPorts.Contains((int)WindowsWaveLinkPort.Port))
                {
                    port = (int)WindowsWaveLinkPort.Port;
                    try
                    {
                        _ = await TryConnect(loggerFactory, host, port);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"Connection failed on port: {port}");
                    }
                }
                Thread.Sleep(1000);

                // get updated ports just in case
                WindowsPorts = FindWaveLinkWebSocketWindows() ?? WindowsPorts;
                WindowsWaveLinkPort = GetNetShInfo();
                continue;
            }

            // will be mac here
            if (await IsPortOpenAsync(host, port, 100) || true)
            {
                try
                {
                    if (!await TryConnect(loggerFactory, host, port))
                    {
                        port++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Connection failed on port: {port}");
                    port++;
                }
            }
            else
            {
                port++;
            }

            // reset port if we get here
            if (port == 1895)
            {
                Thread.Sleep(1000);
                _logger.LogDebug("Retrying...");
                port = 1884;
            }
        }
    }

    public async Task<bool> TryConnect(ILoggerFactory loggerFactory, string host, int port)
    {
        bool portWorked = false;
        try
        {
            Client = new(host, port, loggerFactory);
            // on connection, request application info
            Client.OnConnection += async (s, e) =>
            {
                _logger.LogInformation("Connected to Wave Link on port " + port);
                OnConnection?.Invoke(this, EventArgs.Empty);
                Client.OnClose += OnClose;
                WaveLinkRequestMethod request = new(WaveRequestId.getApplicationInfo);
                await Client.SendRequestAsync(request);
            };
            // print received application info
            Client.MessageRouter.OnReceivedAppInfo += async (s, response) =>
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
                    portWorked = true;
                    _logger.LogInformation($"Connected to wave link");
                    WaveLinkRequestMethod mixRequest = new(WaveRequestId.getMixes);
                    _ = Client.SendRequestAsync(mixRequest);
                    WaveLinkRequestMethod request = new(WaveRequestId.getInputDevices);
                    _ = Client.SendRequestAsync(request);
                    WaveLinkRequestMethod outputRequest = new(WaveRequestId.getOutputDevices);
                    _ = Client.SendRequestAsync(outputRequest);
                    WaveLinkRequestMethod channelRequest = new(WaveRequestId.getChannels);
                    _ = Client.SendRequestAsync(channelRequest);


                    if (SubscribeToFocusApp)
                    {
                        WaveLinkSendMethod<MethodSubscriptionInfo> subscribe = new(WaveLinkMethod.setSubscription, new() { FocusedAppChanged = new() { IsEnabled = SubscribeToFocusApp } });
                        _ = Client?.SendRequestAsync<MethodSubscriptionInfo>(subscribe);
                    }
                }
                else
                {
                    _logger.LogWarning("Connected to an app that was not Wave Link. Closing and retrying...");
                    await Client.CloseAsync();
                }
            };

            _logger.LogDebug($"Trying to connect: {host}:{port}");
            await Client.ConnectAsync();
            
            await Client.WaitForCloseAsync();
            _logger.LogWarning("Wave Link closed. Reconnecting...");
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Connection failed on port: {port}");
            throw;
        }
        return portWorked;
    }
    public void SetInput(string inputName, string? shouldMute = null, decimal? newLevel = null, AdjustmentType? adjustmentType = AdjustmentType.Fixed)
    {
        var inputDevice = Client?.StateManager.InputDevices.Find(c => c.Name == inputName);
        var input = inputDevice?.Inputs?[0] ?? null;
        if (input != null)
        {

            MethodInputInfo inputInfo = new()
            {
                Id = input.Id ?? string.Empty
            };

            inputInfo.IsMuted = ConvertIsMuted(shouldMute, inputDevice?.Inputs?[0].IsMuted);

            decimal? level = newLevel != null ? ToDecimal((int)newLevel) : null;
            if (level != null && adjustmentType == AdjustmentType.Percent)
            {
                var tempGainLevel = input.Gain?.Value == null ? null : input.Gain.Value <= 0m ? 0.010m : input.Gain.Value;
                var newValue = tempGainLevel != null ? tempGainLevel * (1 + level ?? 0) : level;
                inputInfo.Gain = new() { Value = newValue };
            }
            else if (newLevel != null)
            {
                inputInfo.Gain = new() { Value = level };
            }

            if (inputInfo.IsMuted == null && inputInfo.Gain == null) return;

            WaveLinkSendMethod<MethodInputDeviceInfo> setInputDeviceRequest = new(WaveLinkMethod.setInputDevice, new() { Id = inputDevice!.Id, Inputs = [inputInfo] });
            _ = Client?.SendRequestAsync<MethodInputDeviceInfo>(setInputDeviceRequest);
        }
    }

    // mix id will add the output to the mix, if it is empty it will remove, if null it will not change
    public void SetOutput(string outputName, string? shouldMute = null, decimal? newLevel = null, string? mixName = null, AdjustmentType? adjustmentType = AdjustmentType.Fixed)
    {
        var outputDevice = Client?.StateManager.OutputDevices.Find(c => c.Name == outputName);
        var mixId = mixName != null ? Client?.StateManager.Mixes.Find(mix => mix.Name == mixName)?.Id ?? string.Empty : null;
        decimal? level = newLevel == null ? null : ToDecimal((int)newLevel);

        var output = outputDevice?.Outputs?[0] ?? null;

        if (output != null)
        {
            MethodOutputInfo outputInfo = new()
            {
                Id = output.Id ?? string.Empty,
                MixId = mixId // adds the output to the mix, does not change the default though
            };

            outputInfo.IsMuted = ConvertIsMuted(shouldMute, outputDevice?.Outputs?[0].IsMuted);

            if (adjustmentType == AdjustmentType.Percent)
            {
                var tempOutputLevel = output.Level == null ? null : output.Level <= 0m ? 0.010m : output.Level;
                outputInfo.Level = tempOutputLevel != null ? tempOutputLevel * (1 + level ?? 0) : level;
            }
            else
            {
                outputInfo.Level = level;
            }

            MethodOutputDeviceParamInfo deviceParam = new() { Id = outputDevice!.Id, Outputs = [outputInfo] };

            WaveLinkSendMethod<MethodOutputDeviceInfo> setOutputDeviceRequest = new(WaveLinkMethod.setOutputDevice, new() { OutputDevice = deviceParam });
            _ = Client?.SendRequestAsync<MethodOutputDeviceInfo>(setOutputDeviceRequest);
        }
    }

    public void SetChannel(string channelName, string? shouldMute = null, decimal? newLevel = null, AdjustmentType? adjustmentType = AdjustmentType.Fixed, string? mixName = null)
    {
        var channel = Client?.StateManager.Channels.Find(c => c.Name == channelName);
        var fullMix = mixName != null ? Client?.StateManager.Mixes.Find(mix => mix.Name == mixName) : null;
        var mix = fullMix != null ? channel?.Mixes?.Find(mix => mix.Id == fullMix.Id) : null;

        if (channel == null) return;

        MethodChannelInfo channelInfo = new() { Id = channel.Id };
        decimal? level = newLevel == null ? null : ToDecimal((int)newLevel);

        if (mix != null)
        {
            MethodMixInfo mixInfo = new() { Id = mix.Id };
            mixInfo.IsMuted = ConvertIsMuted(shouldMute, mix.IsMuted);
            // adjust level based on mix level if needed
            if (adjustmentType == AdjustmentType.Percent)
            {
                var tempMixLevel = mix.Level == null ? null : mix.Level <= 0m ? 0.010m : mix.Level;
                mixInfo.Level = tempMixLevel != null ? tempMixLevel * (1 + level ?? 0) : level;
            }
            else
            {
                mixInfo.Level = level;
            }
            channelInfo.Mixes = new List<MethodMixInfo>() { mixInfo };
        }
        else
        {
            channelInfo.IsMuted = ConvertIsMuted(shouldMute, channel.IsMuted);
            if (adjustmentType == AdjustmentType.Percent)
            {

                var tempChannelLevel = channel.Level == null ? null : channel.Level <= 0m ? 0.010m : channel.Level;
                channelInfo.Level = tempChannelLevel != null ? tempChannelLevel * (1 + level ?? 0) : level;
            }
            else
            {
                channelInfo.Level = level == null ? null : level <= 0 ? 0 : level;
            }
        }

        WaveLinkSendMethod<MethodChannelInfo> setRequest = new(WaveLinkMethod.setChannel, channelInfo);
        _ = Client?.SendRequestAsync<MethodChannelInfo>(setRequest);
    }

    public void SetMix(string mixName, string? shouldMute = null, decimal? newLevel = null, AdjustmentType? adjustmentType = AdjustmentType.Fixed)
    {
        var mix = Client?.StateManager.Mixes.Find(mix => mix.Name == mixName);
        if (mix != null)
        {
            decimal? level = newLevel == null ? null : ToDecimal((int)newLevel);

            MethodMixInfo mixInfo = new() { Id = mix.Id };

            mixInfo.IsMuted = ConvertIsMuted(shouldMute, mix.IsMuted);
            
            if (adjustmentType == AdjustmentType.Percent)
            {
                var tempMixLevel = mix.Level == null ? null : mix.Level <= 0m ? 0.010m : mix.Level;
                mixInfo.Level = tempMixLevel != null ? tempMixLevel * (1 + level ?? 0) : level;
            }
            else
            {
                mixInfo.Level = newLevel == null ? null : ToDecimal((int)newLevel);
            }

            WaveLinkSendMethod<MethodMixInfo> request = new(WaveLinkMethod.setMix, mixInfo);
            _ = Client?.SendRequestAsync<MethodMixInfo>(request);
        }
    }

    public void SetFocusAppSubscription(string subscribe)
    {
        WaveLinkSendMethod<MethodSubscriptionInfo> request = new(WaveLinkMethod.setSubscription, new() { FocusedAppChanged = new() { IsEnabled = ConvertIsMuted(subscribe, true) ?? true } });
        _ = Client?.SendRequestAsync<MethodSubscriptionInfo>(request);
    }

    public void AddToChannel(string channelName)
    {
        var channel = Client?.StateManager.Channels.Find(c => c.Name == channelName);
        var appToAdd = Client?.StateManager.FocusedApp;
        if (appToAdd != null && channel != null)
        {
            WaveLinkSendMethod<AddAppToChannelInfo> addToChannelRequest = new(WaveLinkMethod.addToChannel, new() { ChannelId = channel.Id, AppId = appToAdd.Id });
            _ = Client?.SendRequestAsync<AddAppToChannelInfo>(addToChannelRequest);
        }
    }

    public decimal ToDecimal(int number)
    {
        if (number <= -100) return -100m;
        if (number >= 100) return 1m;
        return number / 100m;
    }

    public bool? ConvertIsMuted(string? value, bool? currentValue)
    {
        if (value == "toggle") return !currentValue ?? null;
        else if (value != null) return value.ToLower() == "true";
        return null;
    }

    public async Task<bool> IsPortOpenAsync(string host, int port, int timeoutMs)
    {
        using var client = new TcpClient();
        try
        {
            var connectTask = client.ConnectAsync(host, port);
            var delayTask = Task.Delay(timeoutMs);

            var completed = await Task.WhenAny(connectTask, delayTask);

            if (completed == delayTask || !client.Connected)
                return false; // timed out or not connected

            return true; // connected
        }
        catch
        {
            return false;
        }
    }
    public List<int>? FindWaveLinkWebSocketWindows()
    {
        return IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Where(ep => IPAddress.IsLoopback(ep.Address))
            .Select(ep => ep.Port)
            .Where(p => p >= 1024)
            .Distinct()
            .OrderByDescending(p => p)
            .ToList();
    }
    public WaveLinkPort? GetNetShInfo()
    {
        var netShInfo = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = "http show servicestate view=requestq",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(netShInfo)!;
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        var lines = stdout.Split('\n');
        WaveLinkPort? current = null;

        foreach (var line in lines)
        {
            var l = line.Trim();

            if (l.ToLower().Contains("wave") && l.ToLower().Contains("link"))
            {
                current = new WaveLinkPort
                {
                    IsWaveLink = true
                };
            }

            if (l.StartsWith("HTTP://", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(l, @"HTTP://[^:]+:(\d+)/");
                if (match.Success && current != null && current.IsWaveLink)
                {
                    current.Port = int.Parse(match.Groups[1].Value);
                    return current;
                }
            }
        }
        return null;
    }
}

public class WaveLinkPort
{
    public int? Port { get; set; }
    public bool IsWaveLink { get; set; } = new();
}

public enum AdjustmentType
{
    Fixed,
    Percent
}