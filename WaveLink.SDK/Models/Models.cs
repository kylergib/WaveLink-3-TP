//using WaveLink.SDK.Enums;

//namespace WaveLink.SDK.Models
//{
//    public class WaveDevice
//    {
//        public WaveDeviceType Type { get; set; }
//        public string Identifier { get; set; }
//        public bool IsMicMuted { get; set; }
//        public decimal OutputVolume { get; set; }
//        public bool IsClipGuardOn { get; set; }
//        public int LowCutType { get; set; }
//        public decimal Gain { get; set; }
//        public decimal Balance { get; set; }
//        public bool IsLowCutOn { get; set; }
//        public string Name { get; set; }
//        public bool IsGainLocked { get; set; }
//    }
    
//    public class Output
//    {
//        private string Identifier;
//        private string Name;
//        private bool IsSelected;
//    }

//    public class Input
//    {
//        public string Identifier { get; set; }
//        public string Name { get; set; }
//        public bool IsAvailable { get; set; }
//        public bool StreamMixerMuted { get; set; }
//        public bool LocalMixerMuted { get; set; }
//        public int LocalMixerLevel { get; set; }
//        public int StreamMixerLevel { get; set; }
//        public List<InputPlugin> Plugins { get; set; }
//        public int InputType { get; set; }
//        // TODO rename if rename InputPlugin
//        public bool PluginBypassLocal { get; set; } // if false then it is not bypassing the filter/plugin
//        public bool PluginBypassStream { get; set; } // if false then it is not bypassing the filter/plugin
//        public decimal LevelLeft { get; set; }
//        public decimal LevelRight { get; set; }
//        public string LocalMuteStateId { get; set; }
//        public string StreamMuteStateId { get; set; }
//        public string LocalFilterBypassStateId { get; set; }
//        public string StreamFilterBypassStateId { get; set; }
//        public string LevelLeftStateId { get; set; }
//        public string LevelRightStateId { get; set; }
//        public string LocalVolumeStateId { get; set; }
//        public string StreamVolumeStateId { get; set; }
//        public bool StatesSentToTP { get; set; }

//    }

//    public class InputPlugin // TODO: should this be effect now?
//    {
//        public string FilterID;
//        public string PluginID;
//        public string Name;
//        public bool IsActive;
//    }

//    public class OutputConfig
//    {
//        public bool StreamMixerMuted { get; set; }
//        public int StreamMixerLevel { get; set; }
//        public bool LocalMixerMuted { get; set; }
//        public int LocalMixerLevel { get; set; }
//    }
    
//}
