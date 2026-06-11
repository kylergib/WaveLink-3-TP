namespace WaveLink.SDK.Models;

public enum WaveLinkMethod
{
   addToChannel,
   setChannel,
   setMix,
   setInputDevice,
   setOutputDevice,
   setSubscription
}

// channel dto
public class MethodChannelInfo
{
    public string Id { get; set; } = string.Empty;
    public bool? IsMuted { get; set; }
    public decimal? Level { get; set; } // max is 1, min is 0
    public List<MethodMixInfo> Mixes { get; set; } = new();
}

// mix dtos
public class MethodMixInfo
{
    public string Id { get; set; } = string.Empty;
    public bool? IsMuted { get; set; }
    public decimal? Level { get; set; }
    public string? MixId { get; set; }
}

// input device dto
public class MethodInputDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public List<MethodInputInfo> Inputs { get; set; } = new();
}
public class MethodInputInfo
{
    public string Id { get; set; } = string.Empty;
    public bool? IsMuted { get; set; }
    public MethodInputGain? Gain { get; set; }
}

public class MethodInputGain
{
    public decimal? Value { get; set; }
    public decimal? MicPcMix { get; set; } // only works for wave devices i assume
}
public class MethodOutputDeviceInfo
{
    public MethodOutputDeviceParamInfo OutputDevice { get; set; }
}
public class MethodOutputDeviceParamInfo
{
    public string Id { get; set; } = string.Empty;
    public List<MethodOutputInfo> Outputs { get; set; } = new();
}
public class MethodOutputInfo
{
    public string Id { get; set; } = string.Empty;
    public bool? IsMuted { get; set; }
    public decimal? Level { get; set; }
    public string? MixId { get; set; }
}
public class MethodMainOutputInfo
{
    public MethodMainDeviceParamInfo MainOutput { get; set; } = new();
}
public class MethodMainDeviceParamInfo
{
    public string OutputDeviceId { get; set; } = string.Empty;
    public string OutputId { get; set; } = string.Empty;
}
public class AddAppToChannelInfo
{
    public string ChannelId { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;

}
public class MethodSubscriptionInfo
{
    public SubscribeMethodParams FocusedAppChanged { get; set; }
}
public class SubscribeMethodParams
{
    public bool IsEnabled { get; set; }
}