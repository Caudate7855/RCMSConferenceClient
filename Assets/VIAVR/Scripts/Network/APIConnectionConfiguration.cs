namespace VIAVR.Scripts.Network
{
    public static class APIConnectionConfiguration
    {
        /// <summary>
        /// Модель устройства при регистрации устройства в ресепшене отеля.
        /// </summary>
        public const string DeviceModel = "pico";
    
        public static string BaseUrl { get; } = "https://api.vrcinema.pro/api/v1";

        //public static string Logs { get; } = BaseUrl + "/logs";
    
        public static string LinkDeviceUrl { get; } = BaseUrl + "/link-device";
        public static string DeviceDetailsUrl { get; } = BaseUrl + "/device";
        public static string DeviceRuntimeUrl { get; } = BaseUrl + "/device/runtime";
        public static string DeviceFirmwareUrl { get; } = BaseUrl + "/device/firmware";
        public static string DeviceAppsUrl { get; } = BaseUrl + "/device/apps";
        public static string DeviceCmd { get; } = BaseUrl + "/device/cmd";
    }
}