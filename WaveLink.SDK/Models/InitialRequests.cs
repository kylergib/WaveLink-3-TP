using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WaveLink.SDK.Enums;
namespace WaveLink.SDK.Models;
public class ApplicationInfoResponse : WaveLinkResponse<AppInfoResult>, IEquatable<ApplicationInfoResponse>
{
    public bool Equals(ApplicationInfoResponse? other)
    {
        if (other is null) return false;

        return
            JsonRpc == other.JsonRpc &&
            Id == other.Id &&
            ((Result == null && other.Result == null) || (Result != null && Result.Equals(other.Result)));
    }

    public override bool Equals(object? obj) => Equals(obj as ApplicationInfoResponse);

    public override int GetHashCode()
    {
        return HashCode.Combine(JsonRpc, Id, Result);
    }
}

public class AppInfoResult : IEquatable<AppInfoResult>
{
    public string AppID { get; set; } = string.Empty;

    public string OperatingSystem { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public int Build { get; set; }

    public int InterfaceRevision { get; set; }
    public bool Equals(AppInfoResult? other)
    {
        if (other is null) return false;

        return
            AppID == other.AppID &&
            OperatingSystem == other.OperatingSystem &&
            Name == other.Name &&
            Version == other.Version &&
            Build == other.Build &&
            InterfaceRevision == other.InterfaceRevision;
    }

    public override bool Equals(object? obj) => Equals(obj as AppInfoResult);

    public override int GetHashCode()
    {
        return HashCode.Combine(AppID, OperatingSystem, Name, Version, Build, InterfaceRevision);
    }
}

public class InputDeviceResult
{
    public List<InputDevice>? InputDevices { get; set; }
}
public class InputDevice : IEquatable<InputDevice>
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public WaveDeviceType? Type { get; set; }
    public List<Input>? Inputs { get; set; }
    public bool CompareInputs(List<Input>? otherInputs)
    {
        if (Inputs == null && otherInputs == null) return true;
        if (Inputs == null || otherInputs == null) return false;

        if (Inputs.Count != otherInputs.Count) return false;
        for (int i = 0; i < Inputs.Count; i++)
        {
            if (!Inputs[i].Equals(otherInputs[i])) return false;
        }
        return true;
    }
    public bool Equals(InputDevice? other)
    {
        if (other is null) return false;
        return
            Id == other.Id &&
            Name == other.Name &&
            Type == other.Type;
    }
    public override bool Equals(object? obj) => Equals(obj as InputDevice);
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Type);
    }
}
public class Input : IEquatable<Input>
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public InputGain? Gain { get; set; }
    public bool? IsMuted { get; set; }
    public bool? IsGainLockOn { get; set; }
    public int? MicPcMixId { get; set; }
    public List<InputEffect>? Effects { get; set; }
    public List<InputEffect>? DspEffects { get; set; }
    public bool CompareEffects(List<InputEffect>? otherEffects)
    {
        if (Effects == null && otherEffects == null) return true;
        if (Effects == null || otherEffects == null) return false;

        if (Effects.Count != otherEffects.Count) return false;
        for (int i = 0; i < Effects.Count; i++)
        {
            if (!Effects[i].Equals(otherEffects[i])) return false;
        }
        return true;
    }
    public bool CompareDspEffects(List<InputEffect>? otherDspEffects)
    {
        if (DspEffects == null && otherDspEffects == null) return true;
        if (DspEffects == null || otherDspEffects == null) return false;

        if (DspEffects.Count != otherDspEffects.Count) return false;
        for (int i = 0; i < DspEffects.Count; i++)
        {
            if (!DspEffects[i].Equals(otherDspEffects[i])) return false;
        }
        return true;
    }
    public bool Equals(Input? other)
    {
        if (other is null) return false;
        return
            Id == other.Id &&
            Name == other.Name &&
            IsMuted == other.IsMuted &&
            IsGainLockOn == other.IsGainLockOn &&
            MicPcMixId == other.MicPcMixId &&
            Gain == other.Gain;
    }
    public override bool Equals(object? obj) => Equals(obj as Input);
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, IsMuted, IsGainLockOn, MicPcMixId);
    }
}
public class InputGain : IEquatable<InputGain>
{
    public decimal? Value { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    //public object LookupTable { get; set; } 
    public bool Equals(InputGain? other)
    {
        if (other is null) return false;
        return
            Value == other.Value &&
            Min == other.Min &&
            Max == other.Max;
    }
    public override bool Equals(object? obj) => Equals(obj as InputGain);
    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Min, Max);
    }
}
public class InputEffect : IEquatable<InputEffect>
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool? IsEnabled { get; set; }
    public bool Equals(InputEffect? other)
    {
        if (other is null) return false;
        return
            Id == other.Id &&
            Name == other.Name &&
            IsEnabled == other.IsEnabled; 
    }
    public override bool Equals(object? obj) => Equals(obj as InputEffect);
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, IsEnabled);
    }
}

public class OutputDeviceResult
{
    public List<OutputDevice>? OutputDevices { get; set; }
}
public class OutputDevice : IEquatable<OutputDevice>
{
    public string Id { get; set; }
    public string? Name { get; set; }
    public WaveDeviceType? Type { get; set; }
    public List<Output>? Outputs { get; set; }
    public bool CompareOutputs(List<Output> otherOutputs)
    {
        if (Outputs == null && otherOutputs == null) return true;
        if (Outputs == null || otherOutputs == null) return false;

        if (Outputs.Count != otherOutputs.Count) return false;
        for (int i = 0; i < Outputs.Count; i++)
        {
            if (!Outputs[i].Equals(otherOutputs[i])) return false;
        }
        return true;
    }
    public bool Equals(OutputDevice? other)
    {
        if (other is null) return false;
        return
            Id == other.Id &&
            Name == other.Name &&
            Type == other.Type;
    }
    public override bool Equals(object? obj) => Equals(obj as OutputDevice);
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Type);
    }
}
public class Output : IEquatable<Output>
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public decimal? Level { get; set; }
    public string? MixId { get; set; }
    public bool? IsMuted { get; set; }
    public bool Equals(Output? other)
    {
        if (other is null) return false;
        return
            Id == other.Id &&
            Name == other.Name &&
            Level == other.Level &&
            MixId == other.Id &&
            IsMuted == other.IsMuted;
    }
    public override bool Equals(object? obj) => Equals(obj as Output);
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Level, MixId, IsMuted);
    }
}
public class ChannelsResult
{
    public List<Channel>? Channels { get; set; }
}
public class Channel : IEquatable<Channel>
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Type { get; set; }
    public List<ChannelMix>? Mixes { get; set; }
    public decimal? Level { get; set; }
    public bool? IsMuted { get; set; }
    public List<App>? Apps { get; set; }
    public List<ChannelEffect>? Effects { get; set; }
    public ChannelImage? Image { get; set; }
    public bool CompareMixes(List<ChannelMix>? otherMixes)
    {
        if (Mixes == null && otherMixes == null) return true;
        if (Mixes == null || otherMixes == null) return false;

        if (Mixes.Count != otherMixes.Count) return false;

        for (int i = 0; i < Mixes.Count; i++)
        {
            if (!Mixes[i].Equals(otherMixes[i])) return false;
        }
        return true;
    }
    public bool CompareApps(List<App>? otherApps)
    {
        if (Apps == null && otherApps == null) return true;
        if (Apps == null || otherApps == null) return false;

        if (Apps.Count != otherApps.Count) return false;
        for (int i = 0; i < Apps.Count; i++)
        {
            if (!Apps[i].Equals(otherApps[i])) return false;
        }
        return true;
    }
    public bool CompareEffects(List<ChannelEffect>? otherEffects)
    {
        if (Effects == null && otherEffects == null) return true;
        if (Effects == null || otherEffects == null) return false;

        if (Effects.Count != otherEffects.Count) return false;
        return true;
    }
    public bool Equals(Channel? other)
    {
        if (other is null) return false;
        return
            Id == other.Id &&
            Name == other.Name &&
            Type == other.Type &&
            Level == other.Level &&
            IsMuted == other.IsMuted &&
            Image == other.Image;
    }
    public override bool Equals(object? obj) => Equals(obj as Channel);
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Type, Level, IsMuted);
    }

}
public class ChannelMix : IEquatable<ChannelMix>
{
    public string Id { get; set; } = string.Empty;
    public decimal? Level { get; set; }
    public bool? IsMuted { get; set; }
    public bool Equals(ChannelMix? other)
    {
        if (other is null) return false;
        return
            Id == other.Id &&
            Level == other.Level &&
            IsMuted == other.IsMuted;
    }
    public override bool Equals(object? obj) => Equals(obj as ChannelMix);
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Level, IsMuted);
    }
}
public class App : IEquatable<App>
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; } = string.Empty;
    public bool Equals(App? other)
    {
        if (other is null) return false;
        return
            Id == other.Id &&
            Name == other.Name;
    }
    public override bool Equals(object? obj) => Equals(obj as App);
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }
}
public class ChannelEffect
{
    
}
public class ChannelImage : IEquatable<ChannelImage>
{
    public string? ImgData { get; set; } = string.Empty;
    public bool Equals(ChannelImage? other)
    {
        if (other is null) return false;
        return
            ImgData == other.ImgData;
    }
    public override bool Equals(object? obj) => Equals(obj as ChannelImage);
    public override int GetHashCode()
    {
        return HashCode.Combine(ImgData);
    }
}

public class MixesResult
{
    public List<Mix>? Mixes { get; set; }
}
public class Mix : IEquatable<Mix>
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public decimal? Level { get; set; }
    public bool? IsMuted { get; set; }
    public MixImage? Image { get; set; }
    public bool Equals(Mix? other)
    {
        if (other is null) return false;
        return
            Id == other.Id &&
            Name == other.Name &&
            Level == other.Level &&
            IsMuted == other.IsMuted &&
            Image == other.Image;
    }
    public override bool Equals(object? obj) => Equals(obj as Mix);
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Level, IsMuted);
    }
}
public class MixImage : IEquatable<MixImage>
{ 
    public string? Name { get; set; } = string.Empty;
    public bool Equals(MixImage? other)
    {
        if (other is null) return false;
        return
            Name == other.Name;
    }
    public override bool Equals(object? obj) => Equals(obj as MixImage);
    public override int GetHashCode()
    {
        return HashCode.Combine(Name);
    }
}