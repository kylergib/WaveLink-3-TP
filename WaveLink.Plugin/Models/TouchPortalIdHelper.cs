namespace WaveLink.Plugin.Models;

public static class TouchPortalIdHelper
{
    public static readonly string BaseCategory = Statics.PluginId + ".WaveLink";
    public static readonly string OutputCategory = BaseCategory + ".Outputs";
    public static readonly string InputCategory = BaseCategory + ".Inputs";
    public static readonly string ChannelCategory = BaseCategory + ".Channels";
    public static readonly string MixCategory = BaseCategory + ".Mixes";

    // category names
    public static string OutputCategoryName => "Outputs";
    public static string OutputLevelCategoryName = "Outputs Level";
    public static string OutputMutedCategoryName = "Outputs Muted";

    public static string InputCategoryName => "Inputs";
    public static string InputLevelCategoryName = "Inputs Level";
    public static string InputMutedCategoryName = "Inputs Muted";

    public static string ChannelCategoryName => "Channels";
    public static string ChannelLevelCategoryName = "Channels Level";
    public static string ChannelMutedCategoryName = "Channels Muted";

    public static string MixCategoryName => "Mixes";
    public static string MixLevelCategoryName = "Mixes Level";
    public static string MixMutedCategoryName = "Mixes Muted";

    // states - choice lists
    public static string OutputListId(string waveLinkNum = "") => BaseCategory + $"{WaveLinkNumberPrefix(waveLinkNum)}.state.outputDeviceList";
    public static string InputListId(string waveLinkNum = "") => BaseCategory + $"{WaveLinkNumberPrefix(waveLinkNum)}.state.inputDeviceList";
    public static string ChannelListId(string waveLinkNum = "") => BaseCategory + $"{WaveLinkNumberPrefix(waveLinkNum)}.state.channelsList";
    public static string MixListId(string waveLinkNum = "") => BaseCategory + $"{WaveLinkNumberPrefix(waveLinkNum)}.state.mixesList";

    // states
    public static string FocusedAppId(string waveLinkNum = "") => BaseCategory + $"{WaveLinkNumberPrefix(waveLinkNum)}.state.focusedApp";
    public static string IsConnectedToWaveLinkId(string waveLinkNum = "") => BaseCategory + $"{WaveLinkNumberPrefix(waveLinkNum)}.state.isConnectedToWaveLink";


    // dynamic states
    public static string OutputMute(string outputName, string waveLinkNum = "") => $"{Statics.PluginId}{WaveLinkNumberPrefix(waveLinkNum)}.state.{outputName}.mute";
    public static string OutputLevel(string outputName, string waveLinkNum = "") => $"{Statics.PluginId}{WaveLinkNumberPrefix(waveLinkNum)}.state.{outputName}.level";
    public static string InputMute(string inputName, string waveLinkNum = "") => $"{Statics.PluginId}{WaveLinkNumberPrefix(waveLinkNum)}.state.{inputName}.mute";
    public static string InputLevel(string inputName, string waveLinkNum = "") => $"{Statics.PluginId}{WaveLinkNumberPrefix(waveLinkNum)}.state.{inputName}.level";

    public static string ChannelLevel(string channelName, string waveLinkNum = "") => $"{Statics.PluginId}{WaveLinkNumberPrefix(waveLinkNum)}.state.{channelName}.level";
    public static string ChannelMute(string channelName, string waveLinkNum = "") => $"{Statics.PluginId}{WaveLinkNumberPrefix(waveLinkNum)}.state.{channelName}.mute";

    public static string MixLevel(string mixName, string waveLinkNum = "") => $"{Statics.PluginId}{WaveLinkNumberPrefix(waveLinkNum)}.state.{mixName}.level";
    public static string MixMute(string mixName, string waveLinkNum = "") => $"{Statics.PluginId}{WaveLinkNumberPrefix(waveLinkNum)}.state.{mixName}.mute";

    // action data
    public static string ActionId(string actionName) => $"{BaseCategory}.action.{actionName}";
    public static string ActionDataValue(string actionName, int? valueNum = null) =>
        $"{ActionId(actionName)}.data{valueNum?.ToString() ?? string.Empty}.value";
     public static string ActionData(string actionName, string dataName) =>
        $"{ActionId(actionName)}.data.{dataName}";

    // setting names
    public static string IPAddress => "IP Address";
    public static string SubscribeToFocusedApp => "Subscribe To Focused App";
    public static string LogLevel => "Log Level";
    public static string SaveToFile => "Save Logs To File";
    public static string AdditionalAddresses => "Additional IP Addresses";

    // connectors
    public static string ConnectorCategory => BaseCategory + ".connector";
    public static string InputVolumeConnector => ConnectorCategory + ".inputVolume";
    public static string OutputVolumeConnector => ConnectorCategory + ".outputVolume";
    public static string ChannelVolumeConnector => ConnectorCategory + ".channelVolume";
    public static string MixVolumeConnector => ConnectorCategory + ".mixVolume";

    // notification id
    public static string UpdateNotificationId => $"{BaseCategory}.notification.updateNotification";

    private static string WaveLinkNumberPrefix(string waveLinkNum) => string.IsNullOrWhiteSpace(waveLinkNum) ? string.Empty : $".{waveLinkNum}";

}
