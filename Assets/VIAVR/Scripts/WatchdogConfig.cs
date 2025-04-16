using System;
using Newtonsoft.Json;
using UnityEngine;
using VIAVR.Scripts.Network;

namespace VIAVR.Scripts
{
    [CreateAssetMenu(fileName = "WatchdogConfigScriptable", menuName = "VIAVR/Application Configurations/Watchdog Config")]
    public class WatchdogConfig : ScriptableObject
    {
        [Serializable]
        public class JsonConfig
        {
            // package который надо чекать
            [SerializeField] [JsonProperty("target_package")] private string _targetPackage;
            // параметр для запуска _targetPackage
            [SerializeField] [JsonProperty("target_package_activity")] private string _targetPackageActivity;
        
            // чекать _targetPackage каждые _checkProcessEveryMs миллисекунд
            [SerializeField] [JsonProperty("check_every_milliseconds")] private int _checkProcessEveryMs;
            // можно ли запускать _targetPackage
            [SerializeField] [JsonProperty("allow_start_vrlauncher")] private bool _allowStartLauncher;
        
            // разрешить ребут через вачдог
            [SerializeField] [JsonProperty("allow_reboot")] private bool _allowReboot;
            // каждый день выполнять перезагрузку шлема в _everydayRebootAtHour часов (24-часовой формат) 00 минут
            [SerializeField] [JsonProperty("reboot_at_hour")] private int _everydayRebootAtHour;
        
            // разрешить вейкап через вачдог
            [SerializeField] [JsonProperty("allow_wakeup")] private bool _allowWakeup; // в данном проекте если _allowWakeup == false то через ватчдог пересылается аналитика в DeviceRuntimeUrl
            // просыпаться каждые _wakeupEverySeconds (+ рандом)
            [SerializeField] [JsonProperty("wakeup_every_seconds")] private int _wakeupEverySeconds;
            // добавлять рандомное количество секунд в промежутке 0 - _wakeupRandomSeconds к времени вейкапа
            [SerializeField] [JsonProperty("wakeup_random_seconds")] private int _wakeupRandomSeconds;
        
            // разрешить слать данные шлема через вачдог
            [SerializeField] [JsonProperty("allow_runtime")] private bool _allowSendRuntime;
            // слать данные шлема каждые _runtimeEverySeconds (+ рандом)
            [SerializeField] [JsonProperty("runtime_every_seconds")] private int _runtimeEverySeconds;
            // добавлять рандомное количество секунд в промежутке 0 - _runtimeRandomSeconds к времени вейкапа
            [SerializeField] [JsonProperty("runtime_random_seconds")] private int _runtimeRandomSeconds;
        
            // можно ли убивать процессы свёрнутых приложений (лаунчер тоже)
            [SerializeField] [JsonProperty("allow_kill_apps")] private bool _allowKillApps;
            // не убивать процесс лаунчера при запуске пакета приложения из списка
            [SerializeField] [JsonProperty("launcher_kill_whitelist")] private string[] _launcherKillWhitelist; //TODO автоматом подхватывать игры из GameCenter
            [JsonProperty("watchdog_version")] public string WatchdogTargetVersion { get; set; }
            [JsonProperty("launcher_version")] public string LauncherVersion { get; set; }
            [JsonProperty("runtime_api_url")] public string DeviceRuntimeUrl { get; set; } // используется если _allowWakeup == false
        }
    
        [SerializeField] private string _version;
        [SerializeField] private string _packageName;
        [SerializeField] private string _serviceClassName;
        [SerializeField] private string _reloadIntentAction;
        [SerializeField] private string _commandIntentAction;
        [SerializeField] private string _streamingAssetsApkRelativePath;

        [SerializeField] private JsonConfig _jsonConfig;
    
        public string Version => _version;
        public string PackageName => _packageName;
        public string ServiceClassName => _serviceClassName;
        public string ReloadIntentAction => _reloadIntentAction;
        public string CommandIntentAction => _commandIntentAction;
        public string StreamingAssetsApkRelativePath => _streamingAssetsApkRelativePath;
    
        public string SerializedJsonConfig
        {
            get
            {
                //_jsonConfig.SentryDSN = APIConnectionConfiguration.SENTRY_WATCHDOG_DSN;
                _jsonConfig.WatchdogTargetVersion = Version;
                _jsonConfig.LauncherVersion = Application.version;
                _jsonConfig.DeviceRuntimeUrl = APIConnectionConfiguration.DeviceRuntimeUrl;
            
                return JsonConvert.SerializeObject(_jsonConfig, Formatting.Indented);
            }
        }
    }
}