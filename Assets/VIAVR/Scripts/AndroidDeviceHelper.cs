using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Bsi;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Newtonsoft.Json;
using Pvr_UnitySDKAPI;
using Services;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.Networking;
using VIAVR.Scripts;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.Data;
using Debug = CustomDebug.Debug;

public enum PerfomanceMode
{
    BATTERY_SAVER = 0, STANDARD = 1, HIGH_PERFOMANCE = 2, UNKNOWN = 3
}

public class WifiNetworkData
{
    public string SSID { get; set; }
    public bool HasPassword { get; set; }
    public int SignalLevel { get; set; }
}

#pragma warning disable 162

public class AndroidDeviceHelper : MonoBehaviour
{
    public const string WifiSettingsFile = "w.dat";

    private const string CurrentPackage = "com.picovrtob.rcms";

    private const string EditorCheckMessage = "<color=#505050>AndroidDeviceHelper works only on Android device</color>";

    private const string StandardPerformanceScreenBuffer = "2048";

    private AndroidJavaObject _context;

    private bool _wifiReconnectionInProcess;

    public sealed class SystemAttribute
    {

        private readonly string attribute;

        #region attributes
        // https://sdk.picovr.com/docs/ADBCommand/chapter_two.html#modify-system-attribute

        /// <summary>
        /// In seconds. To keep the system never sleep, parameter should set to -1
        /// <para><c>Parameter:</c> positive integer</para>
        /// </summary>
        public static readonly SystemAttribute SYSTEM_SLEEP_TIMEOUT = new SystemAttribute("persist.psensor.sleep.delay");
        /// <summary>
        /// In seconds. To keep the screen never shutdown, parameter should set to 65535, and make sure to set the system never sleep first
        /// <para><c>Parameter:</c> positive integer</para>
        /// </summary>
        public static readonly SystemAttribute SCREEN_SHUTDOWN_TIMEOUT = new SystemAttribute("persist.psensor.screenoff.delay");
        /// <summary>
        /// Long press home button to reset orientation
        /// <para><c>Parameter:</c> 0: to reset orientation only horizontally; 1: to reset orientation horizontally and vertically</para>
        /// </summary>
        public static readonly SystemAttribute LONG_PRESS_HOME_BUTTON_TO_RESET_ORIENTATION = new SystemAttribute("persist.pvr.reset_mode");
        /// <summary>
        /// [Need reboot] 2D window default scaling
        /// <para><c>Parameter:</c> 1: one times scaling; 2: two times scaling; 3: three times scaling</para>
        /// </summary>
        public static readonly SystemAttribute WINDOW_2D_DEFAULT_SCALING = new SystemAttribute("persist.pvr.2dtovr.screen_scale");
        /// <summary>
        /// Set controller calibration on device boot-up
        /// <para><c>Parameter:</c> 0: to turn on power on calibration; 1: to turn off power on calibration</para>
        /// </summary>
        public static readonly SystemAttribute CONTROLLER_CALIBRATION_ON_DEVICE_BOOT_UP = new SystemAttribute("persist.pvr.openrecenter");
        /// <summary>
        /// Single-eyed mode screen cast
        /// <para><c>Parameter:</c> 0: to turn off single-eyed mode; 1: to turn on single-eyed mode</para>
        /// </summary>
        public static readonly SystemAttribute SINGLE_EYED_MODE_SCREEN_CAST = new SystemAttribute("persist.pvr.wfd.enable");
        /// <summary>
        /// [Need reboot] Turn on system logcat
        /// <para><c>Parameter:</c> 0: to turn off system logcat; 1: to turn on system logcat</para>
        /// </summary>
        public static readonly SystemAttribute TURN_ON_SYSTEM_LOGCAT = new SystemAttribute("persist.pvr.logcatch");
        /// <summary>
        /// [Need reboot] Storage logcatch into internal storage
        /// <para><c>Parameter:</c> 0: to turn off; 1: to turn on</para>
        /// </summary>
        public static readonly SystemAttribute STORAGE_LOGCATCH_INTO_INTERNAL_STORAGE = new SystemAttribute("persist.pvr.isLog2SDCard");
        /// <summary>
        /// Modify display mode
        /// <para><c>Parameter:</c> G2 3K: jdi3k75fps jdi3k90fps ；G2 4K: jdi2kt4k75 jdi4k75</para>
        /// </summary>
        public static readonly SystemAttribute MODIFY_DISPLAY_MODE = new SystemAttribute("pvr.display.type");
        /// <summary>
        /// To release limit, parameter should set to -1
        /// <para><c>Parameter:</c> positive integer</para>
        /// </summary>
        public static readonly SystemAttribute FPS_LIMIT = new SystemAttribute("persist.pvr.config.target_fps");
        /// <summary>
        /// Width and height recommend to be 1:1
        /// <para><c>Parameter:</c> 1600; 2048; 2496</para>
        /// </summary>
        public static readonly SystemAttribute EYEBUFFER_WIDTH = new SystemAttribute("persist.pvr.config.eyebuffer_width");
        /// <summary>
        /// Width and height recommend to be 1:1
        /// <para><c>Parameter:</c> 1600; 2048; 2496</para>
        /// <para><c>Parameter:</c> 1664: Battary Saver, 2048: Standard, 2496: High Perfomance</para>
        /// </summary>
        public static readonly SystemAttribute EYEBUFFER_HEIGTH = new SystemAttribute("persist.pvr.config.eyebuffer_height");
        /// <summary>
        /// Whether to reset the pose data when the HMD wakes up
        /// <para><c>Parameter:</c> 0: to turn off; 1: to turn on</para>
        /// </summary>
        public static readonly SystemAttribute RESET_POSE_WHEN_HMD_WAKES_UP = new SystemAttribute("persist.pvr.psensor.reset_pose");
        /// <summary>
        /// Whether to show the confirm panel when the device is connected
        /// <para><c>Parameter:</c> 0: to hide; 1: to show</para>
        /// </summary>
        public static readonly SystemAttribute SHOW_CONFIRM_WHEN_DEVICE_CONNECTED = new SystemAttribute("persist.pvr.show.adb_confirm");
        /// <summary>
        /// OTG Charging mode
        /// <para><c>Parameter:</c> 0: to turn off; 1: to turn on</para>
        /// </summary>
        public static readonly SystemAttribute OTG_CHARGING_MODE = new SystemAttribute("persist.pvr.otgmode");
        /// <summary>
        /// Settings of The Return Button of 2d Mode
        /// <para><c>Parameter:</c> 0: to hide; 1: to show</para>
        /// </summary>
        public static readonly SystemAttribute SETTINGS_RETURN_BUTTON_2D_MODE = new SystemAttribute("persist.pvr.2dtovr.button_back");
        /// <summary>
        /// Settings of The Return Button of 2d Mode
        /// <para><c>Parameter:</c> 0: to charge; 1: to boot</para>
        /// </summary>
        public static readonly SystemAttribute USB_PLUG_IN_BOOT_MODE = new SystemAttribute("persist.pvr.prebootmode");
        /// <summary>
        /// Combination keys
        /// <para><c>Parameter:</c> 0: to turn off; 1: to turn on</para>
        /// </summary>
        public static readonly SystemAttribute COMBINATION_KEYS = new SystemAttribute("persist.pvr.mulkey.enable");
        /// <summary>
        /// Standardizing Application (G2 4K)
        /// <para><c>Parameter:</c> 0: to show; 1: to hide</para>
        /// </summary>
        public static readonly SystemAttribute STANDARDIZING_APPLICATION_G2_4K = new SystemAttribute("persist.pvr.openrecenter");
        /// <summary>
        /// Accepting Upgrade from System
        /// <para><c>Parameter:</c> 0: not to accept; 1: to accept</para>
        /// </summary>
        public static readonly SystemAttribute ACCEPTING_UPGRADE_FROM_SYSTEM = new SystemAttribute("persist.accept.systemupdates");
        /// <summary>
        /// Charging while using
        /// <para><c>Parameter:</c> 3: on, 1: off</para>
        /// </summary>
        public static readonly SystemAttribute CHARGING_WHILE_USING = new SystemAttribute("persist.pvr.charge.policy");
        /// <summary>
        /// Do you want to display the app permission prompt window
        /// <para><c>Parameter:</c> 0: on, 1: off</para>
        /// </summary>
        public static readonly SystemAttribute DISPLAY_APP_PERMISSION_WINDOW = new SystemAttribute("persist.pvrpermission.autogrant");
        /// <summary>
        /// Auto sleep mode
        /// <para><c>Parameter:</c> 1: on, 0: off</para>
        /// </summary>
        public static readonly SystemAttribute AUTO_SLEEP_MODE = new SystemAttribute("persist.pvr.sleep_by_static");
        /// <summary>
        /// Текущая настройка производительности, по дефолту этого поля может не быть! Прописывается когда режим производительности переключается
        /// <para><c>Parameter:</c> 0: Battery Saver, 1: Standard, 2: High Perfomance</para>
        /// </summary>
        public static readonly SystemAttribute PERFOMANCE_MODE = new SystemAttribute("persist.pvr.performance_mode");
        #endregion

        private SystemAttribute(string attribute)
        {
            this.attribute = attribute;
        }

        public override string ToString()
        {
            return attribute;
        }
    }

    private string _cachedSerial = "";

    public string GetDeviceId()
    {
        if (Application.isEditor)
            return GlobalPersonalConsts.UNITY_DEVICE_ID;

#if PICO_G3
        return _cachedSerial;
#endif

        if (string.IsNullOrEmpty(_cachedSerial))
            _cachedSerial = Pvr_UnitySDKAPI.System.UPvr_GetDeviceSN();

        return _cachedSerial ?? string.Empty;
    }

    private readonly Dictionary<string, PerfomanceMode> _perfomanceModeByPerformanceProp = new Dictionary<string, PerfomanceMode>()
    {
        { "0", PerfomanceMode.BATTERY_SAVER },
        { "1", PerfomanceMode.STANDARD },
        { "2", PerfomanceMode.HIGH_PERFOMANCE }
    };

    private readonly Dictionary<string, PerfomanceMode> _perfomanceModeByBufferProp = new Dictionary<string, PerfomanceMode>()
    {
        { "1600", PerfomanceMode.BATTERY_SAVER },
        { "1664", PerfomanceMode.BATTERY_SAVER },
        { "2048", PerfomanceMode.STANDARD },
        { "2496", PerfomanceMode.HIGH_PERFOMANCE }
    };

    [SerializeField] private TextAsset _userKeyConfig;
    [SerializeField] private TextAsset _defaultKeyConfig;

    [SerializeField] [Range(0,99)] private int _debugWifiSignalLevel = 99;

    private const string SavedTimeZonePlayerPrefs = "saved_timezone";

    private Action<string> _shellCallback;

    private AndroidJavaObject _deviceHelper;
    private AndroidJavaObject _storageHelper;
    private AndroidJavaObject _bluetoothHelper;
    private AndroidJavaObject _wifiHelper;
    private AndroidJavaObject _powermanagerHelper;
    private AndroidJavaObject _wifiManagerHelper;
    private AndroidJavaObject _alarmManagerHelper;

    private bool _initToBServiceInProcess = false; // G2
    private bool _canUseToBService = false; // G2
    private bool _canUseEnterpriseService = false; // G3

    private WifiDataSaver _wifiDataSaver;

    public WifiDataSaver WifiDataSaver => _wifiDataSaver;

    void Awake()
    {
        // это имя определено в AndroidHelper_v1.1.4Custom.aar, при смене имени колбэки работать не будут!
        gameObject.name = "AndroidDeviceHelper";

        if (Application.isEditor)
            return;

#if PICO_G2
        InitializeToBService();
#endif
        InitializeAndroidHelperLib();

        _wifiDataSaver = new WifiDataSaver();

        SetDefaultSettings();
        
        Debug.Log("AndroidDeviceHelper initialized!");
    }

    private void OnDestroy()
    {
#if PICO_G2
        ToBService.UPvr_UnBindToBService();

        _initToBServiceInProcess = false;
        _canUseToBService = false;

#elif PICO_G3
        if(_canUseEnterpriseService)
            PXR_Enterprise.UnBindEnterpriseService();
#endif
    }

    /// <summary>
    /// Установка дефолтных настроек
    /// </summary>
    void SetDefaultSettings()
    {
        ReadWifiAutoSettings();

#if PICO_G2
        // Perfomance Mode "нормально" меняется только через настройки PICO, но влияющий на производительность EYEBUFFER_WIDTH/HEIGTH можно менять на "стандартный".
        SetScreenBuffer(StandardPerformanceScreenBuffer);

        // Если шлем был выключен, то при подключении зарядного устройства (или при подключении по USB к ПК) шлем включится и будет загружено приложение
        // иначе шлем будет работать в режиме зарядки (будет показывать "батарейку" на черном фоне)
        SetSystemSettingsProperty(SystemAttribute.USB_PLUG_IN_BOOT_MODE, "1");

        // Копирует содержимое конфига _userKeyConfig в /data/local/tmp/SystemKeyConfig.prop на устройстве, так можно изменить стандартное поведение при нажатии кнопок контроллера
        // файл /data/local/tmp/SystemKeyConfig.prop должен существовать, так как в /data/local/tmp/ можно создавать файлы в рантайме только на устройствах с root
        // в данном случае отключается функция по умолчанию: выходить в оболочку шлема при двойном наждатии кнопки "Домой" на контроллере
        SetKeyConfig(_userKeyConfig);

        // Альтернатива SetKeyConfig'у работающая через ToBService, лучше использовать этот метод т.к. не требует заранее созданного /data/local/tmp/SystemKeyConfig.prop
        SetKeyConfigNative();

        // Позволить заряжаться устройству во время работы. По умолчанию зарядка замедляется или отключается когда устройство используется чтоб снизить температуру батареи
        SetSystemSettingsProperty(SystemAttribute.CHARGING_WHILE_USING, "3");

        // Отключить автоматический отход устройства в сон
        SetSystemSettingsProperty(SystemAttribute.AUTO_SLEEP_MODE, "0");
        SetNoSleepMode();

        // svc power stayon [true|false|usb|ac|wireless]
        // Отключает отход девайса в сон когда он заряжается определенным (usb|ac|wireless) или любым (true) способом (экран при этом может отключаться, настройка это не регулирует)
        // В меню настроек Android обычно обозначается как настройка 'Keep awake while plugged in'
        // подробнее про svc power https://android.googlesource.com/platform/frameworks/base/+/master/cmds/svc/src/com/android/commands/svc/PowerCommand.java#43
        // подробнее про svc http://scofieldorz.blogspot.com/2019/09/android-svc-command.html
        ExecuteShellSync("svc power stayon true");

        //SetDefaultSleepMode("40","30", false);

        // Прописываем в автозагрузку, лишний раз прописать не помешает
        SetAutostartApp("com.picovrtob.rcms", "com.unity3d.player.UnityPlayerNativeActivityPico");

#elif PICO_G3
        PXR_Enterprise.InitEnterpriseService();

        PXR_Enterprise.BindEnterpriseService(bindResult =>
        {
            _canUseEnterpriseService = true;

            Debug.Log("PXR_Enterprise.BindEnterpriseService " + bindResult);

            _cachedSerial = PXR_Enterprise.StateGetDeviceInfo(SystemInfoEnum.EQUIPMENT_SN);

            Debug.Log("Serial number cashed - " + _cachedSerial);
            
            PXR_Enterprise.PropertySetHomeKey(HomeEventEnum.DOUBLE_CLICK_RIGHT_CTL, HomeFunctionEnum.VALUE_HOME_RECENTER,
                result => { Debug.Log("PXR_Enterprise.PropertySetHomeKey DOUBLE_CLICK_RIGHT_CTL " + result); });
            
            PXR_Enterprise.PropertySetHomeKey(HomeEventEnum.DOUBLE_CLICK, HomeFunctionEnum.VALUE_HOME_RECENTER,
                result => { Debug.Log("PXR_Enterprise.PropertySetHomeKey VALUE_HOME_RECENTER " + result); });

            PXR_Enterprise.PropertySetHomeKey(HomeEventEnum.DOUBLE_CLICK_HMD, HomeFunctionEnum.VALUE_HOME_RECENTER,
                result => { Debug.Log("PXR_Enterprise.PropertySetHomeKey DOUBLE_CLICK_HMD " + result); });
            
            PXR_Enterprise.PropertySetHomeKey(HomeEventEnum.SINGLE_CLICK, HomeFunctionEnum.VALUE_HOME_BACK,
                result => { Debug.Log("PXR_Enterprise.PropertySetHomeKey DOUBLE_CLICK_HMD " + result); });
            
            PXR_Enterprise.PropertySetHomeKey(HomeEventEnum.SINGLE_CLICK_RIGHT_CTL, HomeFunctionEnum.VALUE_HOME_BACK,
                result => { Debug.Log("PXR_Enterprise.PropertySetHomeKey DOUBLE_CLICK_HMD " + result); });
            
            PXR_Enterprise.PropertySetHomeKey(HomeEventEnum.SINGLE_CLICK_LEFT_CTL, HomeFunctionEnum.VALUE_HOME_BACK,
                result => { Debug.Log("PXR_Enterprise.PropertySetHomeKey DOUBLE_CLICK_HMD " + result); });

            PXR_Enterprise.SetAPPAsHome(SwitchEnum.S_ON, CurrentPackage);

            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_USB, SwitchEnum.S_ON);
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_USB_BOOT, SwitchEnum.S_ON);

            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_NAVGATION_SWITCH, SwitchEnum.S_OFF);

            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_CONTROLLER_UI, SwitchEnum.S_OFF);
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_SHORTCUT_SHOW_POWER_UI, SwitchEnum.S_OFF);
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_BASIC_SETTING_SHORTCUT_UI, SwitchEnum.S_OFF);

            //PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_BASIC_SETTING_KILL_BACKGROUND_VR_APP, SwitchEnum.S_ON); // непонятная функция
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_BASIC_SETTING_SHOW_APP_QUIT_CONFIRM_DIALOG, SwitchEnum.S_OFF);

            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_SYSTEM_UPDATE, SwitchEnum.S_OFF);
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_SYSTEM_UPDATE_OTA, SwitchEnum.S_OFF);
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_SYSTEM_UPDATE_APP, SwitchEnum.S_OFF);

            PXR_Enterprise.PropertySetSleepDelay(SleepDelayTimeEnum.THREE_HUNDRED);
            //PXR_Enterprise.PropertySetSleepDelay(SleepDelayTimeEnum.NEVER); // тест вебсокетов
            PXR_Enterprise.PropertySetScreenOffDelay(ScreenOffDelayTimeEnum.SIXTY, result => { Debug.Log("PXR_Enterprise.PropertySetScreenOffDelay " + result); });

            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_POWER_CTRL_WIFI_ENABLE, SwitchEnum.S_OFF); // S_ON может быть причиной отвала точки у Илоны

            string savedWifiSpots = PlayerPrefs.GetString(WifiDataSaver.PlayerPrefsSaveKey);
            if (!string.IsNullOrEmpty(savedWifiSpots))
            {
                List<WifiData> wifiDataList = JsonConvert.DeserializeObject<List<WifiData>>(savedWifiSpots);

                if (wifiDataList != null)
                {
                    WifiData wifiData = wifiDataList.Last();
                    PXR_Enterprise.ControlSetAutoConnectWIFI(wifiData.Ssid, wifiData.Password, result => { Debug.Log("ControlSetAutoConnectWIFI: " + result); });
                }
            }

            if (PXR_Enterprise.GetSystemLanguage() != "ru")
            {
                int result = PXR_Enterprise.SetSystemLanguage("ru");

                Debug.Log($"PXR_Enterprise.SetSystemLanguage(ru) result == '{result}'", Debug.LoggerBehaviour.ADD);
            }
        });
#endif

        // на случай отвала вачдога бэкап просыпы через будильник. При просыпе в ApplicationCoreManager проверяется работает ли сервис вачдога и перезапускается если надо
        WakeUpsBackup(6 * 60 * 60 * 1000, 0, false); // каждые 6 часов
    }

    // инициализация ToBService (либа прошивки с полезным функционалом похожая на AndroidDeviceHelper)
    void InitializeToBService()
    {
#if PICO_G2
        _initToBServiceInProcess = true;

        ToBService.UPvr_InitToBService();
        ToBService.UPvr_SetUnityObjectName(gameObject.name);
        ToBService.UPvr_BindToBService();
#endif
    }

    // G2/G3 инициализация java либы Assets/Plugins/Android/AndroidHelper_v1.1.4Custom.aar
    void InitializeAndroidHelperLib()
    {
        if (Application.isEditor) return;

        using (var activityContext = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
        {
            _deviceHelper = new AndroidJavaObject("com.picovr.androidhelper.DeviceHelper");
            _storageHelper = new AndroidJavaObject("com.picovr.androidhelper.StorageHelper");
            _bluetoothHelper = new AndroidJavaObject("com.picovr.androidhelper.BlueToothHelper");
            _wifiHelper = new AndroidJavaObject("com.picovr.androidhelper.WifiHelper");
            _powermanagerHelper = new AndroidJavaObject("com.picovr.androidhelper.PowerManagerHelper");
            _wifiManagerHelper = activityContext.Call<AndroidJavaObject>("getSystemService", "wifi");
            _alarmManagerHelper = activityContext.Call<AndroidJavaObject>("getSystemService", "alarm");

            _deviceHelper.Call("init", activityContext);
            _storageHelper.Call("init", activityContext);
            _bluetoothHelper.Call("init", activityContext);
            _wifiHelper.Call("init", activityContext);
            _powermanagerHelper.Call("init", activityContext);
        }

        Debug.Log("AndroidDeviceHelper: InitializeAndroidHelperLib initialization complete");
    }

    /// <summary>
    /// Чтение настроек для автоконнекта к вайфаю (файл w.dat который создается утилитой batTools)
    /// </summary>
    async void ReadWifiAutoSettings()
    {
        var wifiAutoSettings = ContentPath.GetFullPath(WifiSettingsFile, ContentPath.Storage.INTERNAL);
        string ssid = string.Empty;
        string pass = string.Empty;

        if (!File.Exists(wifiAutoSettings))
            return;

        var wifiAutoSettingsText = File.ReadAllText(wifiAutoSettings);

        if (string.IsNullOrEmpty(wifiAutoSettings))
            return;

        var wifiAutoSettingsLines = wifiAutoSettingsText.Split('\n');

        foreach (var line in wifiAutoSettingsLines)
        {
            var lineTrimmed = line.Trim(' ', '\t', '\r');

            if (lineTrimmed.StartsWith("SSID:"))
                ssid = lineTrimmed.Replace("SSID:", string.Empty);
            else if (lineTrimmed.StartsWith("PASS:"))
                pass = lineTrimmed.Replace("PASS:", string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(ssid))
        {
            await UniTask.WaitUntil(() => _wifiDataSaver != null);

            _wifiDataSaver.AddWifiSpot(ssid, pass);

            Debug.Log($"ReadWifiAutoSettings ssid: '{ssid}' pass: '{pass}'", Debug.LoggerBehaviour.ADD);
        }
        else
        {
            Debug.Log($"ReadWifiAutoSettings ssid is empty", Debug.LoggerBehaviour.ADD);
        }
    }

    // https://gist.github.com/Pulimet/5013acf2cd5b28e55036c82c91bd56d8 (не нужно писать 'adb shell', все команды которые начинаются с 'adb shell' - поддерживаются)
    public void ExecuteShellDefault(string commandLine)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _deviceHelper?.Call("executeShellDefault", commandLine);
    }

    public void ExecuteShellAsync(string commandLine, Action<string> callback = null)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _shellCallback = callback;

        _deviceHelper?.Call("executeShell", commandLine);
    }

    public string ExecuteShellSync(string commandLine)
    {
        if (Application.isEditor) { Debug.Log(EditorCheckMessage); return string.Empty; }

        return _deviceHelper?.Call<string>("executeShellString", commandLine);
    }

    public void ExecuteShellCallback(string message)
    {
        if (_shellCallback == null) Debug.Log("AndroidDeviceHelper: ExecuteShellCallback: no callback for result " + message);

        _shellCallback?.Invoke(message);
        _shellCallback = null;
    }

    public void InstallCallback(string message)
    {
        Debug.Log("AndroidDeviceHelper: InstallCallback message: " + message);
    }

    public async UniTask<bool> IsApkCompatibleWithDevice(string apkFullPath)
    {
        // чек файла
        if (string.IsNullOrEmpty(apkFullPath) || !File.Exists(apkFullPath))
        {
            Debug.LogError($"'File '{apkFullPath}' not exists");
            return false;
        }

        // чек расширеня файла, должен быть апк
        if (Path.GetExtension(apkFullPath) != ".apk")
        {
            Debug.LogError($"'File '{apkFullPath}' not '.apk'");
            return false;
        }

        string manifestName = "AndroidManifest.xml";

        ZipArchiveEntry entry = null;
        ZipArchive archive = null;

        try
        {
            archive = ZipFile.OpenRead(apkFullPath);
            entry = archive.GetEntry(manifestName);
        }
        catch (Exception e)
        {
            Debug.LogError($"Can't read '{apkFullPath}'");
            return false;
        }

        if (entry == null)
        {
            Debug.LogError($"{manifestName} not found in '{apkFullPath}'");
            return false;
        }

        using StreamReader reader = new StreamReader(entry.Open(), Encoding.Unicode);

        string manifestContents = await reader.ReadToEndAsync();

        archive.Dispose();

        // в манифесте должно быть упоминание package текущей приложухи
        return !string.IsNullOrEmpty(manifestContents) && manifestContents.Contains(Application.identifier);
    }

    // PICO INTERFACES

#region DeviceHelper

    /// <summary>
    /// Выставить приложение в автозагрузку при запуске шлема
    /// </summary>
    /// <param name="appPackage">пакет (напр. com.picovrtob.vrlauncher2)</param>
    /// <param name="appActivity">активити (напр. com.unity3d.player.UnityPlayerNativeActivityPico)</param>
    public void SetAutostartApp(string appPackage, string appActivity)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

#if PICO_G2
        // для "новой" прошивки шлема (4.0.0 и новее)
        ExecuteShellSync($"setprop persist.pxr.force.home {appPackage},{appActivity}");

        // для "старой" прошивки шлема
        ExecuteShellSync($"setprop persist.pvr.default.home {appPackage}");
#elif PICO_G3
        if(_canUseEnterpriseService)
            PXR_Enterprise.SetAPPAsHome(SwitchEnum.S_ON, appPackage);
#endif
    }

    /// <summary>
    /// (G2) Выдать разрешение приложению (приложение уже должно быть инсталлировано)
    /// </summary>
    /// <param name="appPackage">пакет (напр. com.picovrtob.vrlauncher2)</param>
    /// <param name="permission">разрешение (напр. android.permission.READ_EXTERNAL_STORAGE)</param>
    public void GrantPermissionToApp(string appPackage, string permission)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        ExecuteShellSync($"pm grant {appPackage} {permission}");
    }

    /// <summary>
    /// Возвращает версию установленного приложения
    /// </summary>
    /// <param name="appPackage">пакет (напр. com.picovrtob.vrlauncher2)</param>
    /// <returns>версия (напр. 1.0.0)</returns>
    public string GetPackageVersion(string appPackage)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return string.Empty; }

        if (!IsAppInstalled(appPackage))
            return "";

        string dump = ExecuteShellSync($"dumpsys package {appPackage}");

        if (string.IsNullOrEmpty(dump) || dump.Contains("Unable to find package"))
            return "";

        string[] lines = dump.Split('\n');

        return lines.First(line => line.Contains("versionName")).Replace("versionName=", "").Trim('\t', '\r', '\n', ' ');
    }

    /// <summary>
    /// Послать Intent приложению (adb shell am broadcast -a {intentAction})
    /// </summary>
    /// <param name="intentAction">экшон (напр. android.intent.action.MAIN)</param>
    public void BroadcastIntentAction(string intentAction)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _deviceHelper?.Call("broadcastIntent", intentAction);
    }

    /// <summary>
    /// Послать Intent приложению с данными (adb shell am broadcast -a {intentAction} --es {key} "{value}")
    /// </summary>
    /// <param name="intentAction">экшон (напр. android.intent.action.MAIN)</param>
    /// <param name="key">переменная (напр. extra_key)</param>
    /// <param name="value">екстовое значение переменной (напр. лол кек чебурек)</param>
    public void BroadcastIntentAction(string intentAction, string key, string value)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value))
        {
            BroadcastIntentAction(intentAction);
            return;
        }

        _deviceHelper?.Call("broadcastIntentWithExtra", intentAction, key, value);
    }

    public bool IsServiceRunning(string serviceClass)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return true; }

        bool? result = _deviceHelper?.Call<bool>("isServiceRunning", serviceClass);

        return result ?? false;
    }

    /// <summary>
    /// Запустить Android сервис
    /// </summary>
    /// <param name="appPackage">пакет (напр. com.picovrtob.service)</param>
    /// <param name="serviceClass">класс сервиса (напр. SomeService)</param>
    public void StartService(string appPackage, string serviceClass)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        //ExecuteShellSync($"am startservice {appPackage}/.{serviceClass}"); // API < 26
        ExecuteShellSync($"am start-foreground-service {appPackage}/.{serviceClass}"); // API >= 26
    }

    /// <summary>
    /// Остановить Android приложение
    /// </summary>
    /// <param name="appPackage">пакет (напр. com.picovrtob.service)</param>
    public void StopProcess(string appPackage)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        ExecuteShellSync($"am force-stop {appPackage}");
    }

    // если список нужно проверять много раз (например в цикле) то лучше юзать кэшированный список приложений для лучшей производительности
    private HashSet<string> _packagesCache = null;

    /// <summary>
    /// Возвращает список установленных приложений в виде пакетов
    /// </summary>
    /// <param name="force">true - обновить список приложений, false - чекать в кэше</param>
    /// <returns>список установленных приложений в виде пакетов, список НЕ отсортирован по алфавиту</returns>
    public HashSet<string> GetInstalledApps(bool force = true)
    {
        if (Application.isEditor)
        {
            return new HashSet<string>();
            //return new HashSet<string>(Directory.GetFiles(ContentPath.GetFullPath(GameCenter.EDITOR_EMULATED_INSTALL_FOLDER, ContentPath.Storage.INTERNAL)).Select(Path.GetFileName));
        }

        if (!force && _packagesCache != null)
            return _packagesCache;

        string packagesListRaw = ExecuteShellSync("pm list packages");

        if (string.IsNullOrEmpty(packagesListRaw))
            return new HashSet<string>();

        string[] packages = packagesListRaw.Split('\n');

        for (int i = 0; i < packages.Length; i++)
        {
            if(!packages[i].Contains("package:")) continue;

            packages[i] = packages[i].Replace("package:", "").Trim();
        }

        // в список закидывает "пакет" android (т.е. операционку), фильтруем
        _packagesCache = new HashSet<string>(packages.Where(package => !string.IsNullOrEmpty(package) && !package.Equals("android")));

        return _packagesCache;
    }

    /// <summary>
    /// Проверка установлено ли приложение
    /// </summary>
    /// <param name="appPackage">пакет (напр. com.picovrtob.vrlauncher)</param>
    /// <param name="force">true - обновить список приложений, false - чекать в кэше</param>
    /// <returns>true если приложение установлено, иначе false</returns>
    public bool IsAppInstalled(string appPackage, bool force = true)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return true; }

        if (string.IsNullOrWhiteSpace(appPackage))
            return false;

        return GetInstalledApps(force).Contains(appPackage);
    }

    private readonly Regex _lastUpdateTimeRegex = new Regex("lastUpdateTime=.*?(\n|\r)");
    /// <summary>
    /// Дата и время установки (или переустановки) приложения
    /// </summary>
    /// <param name="appPackage">пакет (напр. com.picovrtob.vrlauncher2)</param>
    /// <returns>Строка в формате yyyy-MM-dd HH:mm:ss, пример: 2021-09-03 19:10:41</returns>
    public string GetPackageInstallDate(string appPackage = CurrentPackage)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return DateTime.Now.ToString("u"); }

        if (!IsAppInstalled(appPackage))
        {
            Debug.Log($"[{appPackage}] app is not installed!");
            return string.Empty;
        }

        // чтоб юзать dumpsys package нужно разрешение android.permission.PACKAGE_USAGE_STATS
        string package = ExecuteShellSync($"dumpsys package {appPackage}");

        var lastUpdateTime = _lastUpdateTimeRegex.Match(package);

        if (!lastUpdateTime.Success)
        {
            Debug.LogError("Can't get package install date");
            return string.Empty;
        }

        return lastUpdateTime.Value.Replace("lastUpdateTime=","").Trim(' ', '\r', '\n');
    }

    /// <summary>
    /// Get PUI version of the device
    /// </summary>
    /// <returns>PUI version, e.g. 3.4.0</returns>
    public string GetPUIVersion()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return "2.2.8"; }

        return _deviceHelper?.Call<string>("getPUIVersion");
    }

    /// <summary>
    /// Get type of the device
    /// </summary>
    /// <returns>the device type, e.g. Pico G2</returns>
    public string GetDeviceType()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return "UNITY PC"; }

        return _deviceHelper?.Call<string>("getDeviceType");
    }

    public void AppKeepAlive(string package)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

#if PICO_G3
        if(string.IsNullOrEmpty(package)) return;

        if(_canUseEnterpriseService)
            PXR_Enterprise.AppKeepAlive(package, true, 0);
#else
        Debug.Log("AppKeepAlive works only on PICO G3");
#endif
    }

    /// <summary>
    /// Install the application without user interaction
    /// </summary>
    /// <param name="apkPath">the path of the APK you want to install</param>
    public void SilentInstall(string apkPath)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

#if PICO_G2
        _deviceHelper?.Call("silentInstall", apkPath, Application.identifier);
#elif PICO_G3
        if(_canUseEnterpriseService)
            PXR_Enterprise.ControlAPPManager(PackageControlEnum.PACKAGE_SILENCE_INSTALL, apkPath, result =>
            {
                Debug.Log($"SilentInstall('{apkPath}') result == " + result);
            });
#endif
    }

    /// <summary>
    /// Uninstall the application without user interaction
    /// </summary>
    /// <param name="packageName">package name of the application you want to uninstall</param>
    public void SilentUninstall(string packageName)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

#if PICO_G2
        _deviceHelper?.Call("silentUninstall", packageName);
#elif PICO_G3
        if(_canUseEnterpriseService)
            PXR_Enterprise.ControlAPPManager(PackageControlEnum.PACKAGE_SILENCE_UNINSTALL, packageName, result => Debug.Log("SilentUninstall: " + result));
#endif
    }

    /// <summary>
    /// Kill the application
    /// </summary>
    /// <param name="packageName">package name of the application to kill</param>
    public void KillApp(String packageName)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _deviceHelper?.Call("killApp", packageName);
    }

    /// <summary>
    /// Call specified browser to open the link
    /// </summary>
    /// <param name="browser">0: PUI built-in browser; 1: WebVR browser; 2: Firefox VR browser</param>
    /// <param name="link">the link to open</param>
    public void LaunchBrowser(int browser, String link)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _deviceHelper?.Call("launchBrowser", browser, link);
    }

    /// <summary>
    /// Call specified browser to open the link in the file
    /// </summary>
    /// <param name="browser">0: PUI built-in browser; 1: WebVR browser; 2: Firefox VR browser</param>
    /// <param name="filePath">the config file path</param>
    public void LaunchBrowserWithLinkInFile(int browser, String filePath)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _deviceHelper?.Call("launchBrowserWithLinkInFile", browser, filePath);
    }

    public static readonly string[] BrowserPackages =
    {
        "org.mozilla.vrbrowser",
        "org.chromium.chrome",
        "org.chromium.webview_shell"
    };

    /// <summary>
    /// Clear all installed browsers data
    /// </summary>
    public void ClearBrowsersHistory()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        foreach (var browserPackage in BrowserPackages)
        {
            if (!IsAppInstalled(browserPackage, false)) continue;

            ExecuteShellSync($"pm clear {browserPackage}");
        }
    }

    /// <summary>
    /// Start an application
    /// </summary>
    /// <param name="packageName">package name of the application you want to open</param>
    public void GoToApp(String packageName)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _deviceHelper?.Call("goToApp", packageName);
    }

    /// <summary>
    /// Launch Android 2D application
    /// </summary>
    /// <para><c>ways:</c></para>
    /// <para>0, launch with ComponentName, args is an array of length 2 consisting of PackageName and ClassName</para>
    /// <para>1, launch with PackageName, args is an array of length 1 consisting of PackageName</para>
    /// <para>2, launch with Action, args is an array of length 1 consisting of Action</para>
    /// <para>Example: StartVRShell(2, new string[] { "pui.settings.action.CONTROLLER_SETTINGS"})</para>
    public void StartVRShell(int way, String[] args)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _deviceHelper?.Call("startVRShell", way, args);
    }

    /// <summary>
    /// Get a list of installed applications in JSON format
    /// </summary>
    /// <returns>list of installed applications</returns>
    public string GetAppList()
    {
        if (Application.isEditor) { Debug.Log(EditorCheckMessage); return string.Empty; }

        return _deviceHelper?.Call<string>("getAppList");
    }

    /// <summary>
    /// Adjust startup calibration application
    /// </summary>
    public void OpenRecenterApp()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _deviceHelper?.Call("openRecenterApp");
    }

    /// <summary>
    /// Install the application. Unlike silentInstall interface, installApp will call up Android installation UI, Click "Install" button to complete the installation
    /// </summary>
    /// <param name="apkPath">the path of the APK you want to install</param>
    public void InstallApp(string apkPath)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _deviceHelper?.Call("installApp", apkPath);
    }

    /// <summary>
    /// Get specific system property
    /// </summary>
    /// <param name="key">attribute</param>
    /// <param name="defaultValue">The value to return if get the prop failed</param>
    /// <returns></returns>
    public string GetSystemSettingsProperty(SystemAttribute key, string defaultValue = "")
    {
        return GetSystemSettingsProperty(key.ToString(), defaultValue);
    }

    /// <summary>
    /// Set specified system property
    /// </summary>
    /// <param name="key">attribute</param>
    /// <param name="value">value</param>
    /// <returns>return true if set it successfully, return false otherwise</returns>
    public bool SetSystemSettingsProperty(SystemAttribute key, string value)
    {
        return SetSystemSettingsProperty(key.ToString(), value);
    }

    private string GetSystemSettingsProperty(string key, string defaultValue = "")
    {
        if (Application.isEditor) { Debug.Log(EditorCheckMessage); return string.Empty; }

        string propertyValue = _deviceHelper.Call<string>("getSystemProp", key, defaultValue);

        return propertyValue;
    }

    private bool SetSystemSettingsProperty(string key, string value)
    {
        if (Application.isEditor) { Debug.Log(EditorCheckMessage); return false; }

        bool result = _deviceHelper.Call<bool>("setSystemProp", key, value);

        if (!result)
            Debug.LogError($"SetSystemSettingsPropery: cannot set property '{key}' to value '{value}'");

        return result;
    }

    public void SetNoSleepMode()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

#if PICO_G2
        if (SetSystemSettingsProperty(SystemAttribute.SYSTEM_SLEEP_TIMEOUT, "-1")
            &&
            SetSystemSettingsProperty(SystemAttribute.SCREEN_SHUTDOWN_TIMEOUT, "65535"))
        {
            Debug.Log("AndroidDeviceHelper.SetNoSleepMode() success.");
        }
        else
            Debug.Log("AndroidDeviceHelper.SetNoSleepMode() not success.");
#elif PICO_G3
        if (_canUseEnterpriseService)
        {
            PXR_Enterprise.PropertySetSleepDelay(SleepDelayTimeEnum.NEVER);
            PXR_Enterprise.PropertySetScreenOffDelay(ScreenOffDelayTimeEnum.NEVER, result => { Debug.Log("PXR_Enterprise.PropertySetScreenOffDelay " + result); });
        }
#endif
    }

    public void SetDefaultSleepMode()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

#if PICO_G2
        SetDefaultSleepMode("40", "30", false);
#elif PICO_G3
        SetDefaultSleepMode(string.Empty, string.Empty, false);
#endif
    }

    public void SetDefaultSleepMode(string systemSleepTimeout, string screenSleepTimeout, bool forceDisableScreen)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

#if PICO_G2
        if (SetSystemSettingsProperty(SystemAttribute.SYSTEM_SLEEP_TIMEOUT, systemSleepTimeout)
            &&
            SetSystemSettingsProperty(SystemAttribute.SCREEN_SHUTDOWN_TIMEOUT, screenSleepTimeout))
        {
            if (forceDisableScreen)
                ExecuteShellSync("input keyevent 26");

            Debug.Log("AndroidDeviceHelper.SetDefaultSleepMode() success.");
        }
        else
            Debug.Log("AndroidDeviceHelper.SetDefaultSleepMode() not success.");
#elif PICO_G3
        if (_canUseEnterpriseService)
        {
            PXR_Enterprise.PropertySetSleepDelay(SleepDelayTimeEnum.THREE_HUNDRED);
            PXR_Enterprise.PropertySetScreenOffDelay(ScreenOffDelayTimeEnum.SIXTY, result => { Debug.Log("PXR_Enterprise.PropertySetScreenOffDelay " + result); });
        }
#endif
    }

#endregion

#region StorageHelper

    private string _cachedStoragesInfo = "";

    private readonly Regex _storageRegex = new Regex(@"(^(/[\w-]+)*|\s+)", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string GetStoragesInfo(bool asIs)
    {
        if (Application.isEditor)
            return string.Empty;

        if (string.IsNullOrEmpty(_cachedStoragesInfo))
        {
            string[] lines = ExecuteShellSync("df -h")?.Split('\n');

            if(lines == null)
                return string.Empty;

            string info = "";

            foreach (var line in lines)
            {
                if (line.Contains("/storage/"))
                    info += asIs ? $"{line}\n" : $"[{_storageRegex.Replace(line, " ").TrimStart()}] ";
            }

            _cachedStoragesInfo = info.TrimEnd(' ', '\r', '\n');
        }

        return _cachedStoragesInfo;
    }

    /// <summary>
    /// The remaining storage space inside the device
    /// </summary>
    /// <returns>Size of remaining space, e.g. 16384 or -1 on fail</returns>
    public float GetStorageFreeSize()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return -1; }

        return _storageHelper?.Call<float>("getStorageFreeSize") ?? -1;
    }

    /// <summary>
    /// Total storage space inside the device
    /// </summary>
    /// <returns>Total space size, e.g. 6114 or -1 on fail</returns>
    public float GetStorageTotalSize()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return -1; }

        return _storageHelper?.Call<float>("getStorageTotalSize") ?? -1;
    }

    /// <summary>
    /// Update storaged file
    /// </summary>
    /// <param name="filePath">filePath: the path of file you need to update</param>
    public void UpdateFile(string filePath)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _storageHelper?.Call("updateFile", filePath);
    }

    private string _cachedSDCardPath = "";
    /// <summary>
    /// Get external SD card path. e.g., "storage/3263-3533"
    /// </summary>
    /// <returns>The path of the SD card</returns>
    public string GetSDCardPath()
    {
        if (Application.isEditor)
            return GlobalPersonalConsts.CONTENT_FOLDER_EDITOR;

        if(string.IsNullOrEmpty(_cachedSDCardPath))
            _cachedSDCardPath = _storageHelper?.Call<string>("getSDCardPath");

        return _cachedSDCardPath;
    }

#endregion

#region BlueToothHelper

    /// <summary>
    /// Register the receiver for bluetooth status broadcast
    /// </summary>
    public void RegisterBlueToothReceiver()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _bluetoothHelper?.Call("registerBlueToothReceiver");
    }

    /// <summary>
    /// Unregister the receiver for bluetooth status broadcast
    /// </summary>
    public void UnregisterBlueToothReceiver()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _bluetoothHelper?.Call("unregisterBlueToothReceiver");
    }

    /// <summary>
    /// Get the name of connected bluetooth device
    /// </summary>
    /// <returns>name of connected bluetooth</returns>
    public string GetContentDevice()
    {
        if (Application.isEditor) { Debug.Log(EditorCheckMessage); return string.Empty; }

        return _bluetoothHelper?.Call<string>("getContentDevice");
    }

    /// <summary>
    /// Get MAC address of connected bluetooth
    /// </summary>
    /// <returns>bluetooth MAC address, e.g. 22:22:86:2A:22:E7</returns>
    public string GetBlueToothMac()
    {
        if (Application.isEditor) { Debug.Log(EditorCheckMessage); return "12:34:56:78:90"; }

        return _bluetoothHelper?.Call<string>("getBlueToothMac");
    }

    /// <summary>
    /// Check if bluetoorh is enabled or not
    /// </summary>
    /// <returns>true if bluetooth is enabled</returns>
    public bool IsBluetoothEnabled()
    {
        if (Application.isEditor) { return true; }

        bool result;

        if (_bluetoothHelper != null)
            result = _bluetoothHelper.Call<bool>("getBluetoothState");
        else
            result = ExecuteShellSync("settings get global bluetooth_on") == "1";

        return result;
    }

    /// <summary>
    /// Enable/disable bluetooth
    /// </summary>
    /// <param name="state">true to enable, false to disable</param>
    public void SetBluetoothState(bool state)
    {
        Debug.Log($"Set bluetooth state to: {(state ? "ON" : "OFF")}...", Debug.LoggerBehaviour.ADD);

        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _bluetoothHelper?.Call("setBluetoothState", state);
    }

    /// <summary>
    /// Reset bluetooth task
    /// </summary>
    public async UniTask ResetBluetooth()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        Debug.Log("Reset Bluetooth...");

        SetBluetoothState(false);

        do
        {
            // ждём пока блютус не выключится
            await UniTask.Delay(200);
        } while (IsBluetoothEnabled());

        SetBluetoothState(true);

        do
        {
            // ждём пока блютус не включится
            await UniTask.Delay(200);
        } while (!IsBluetoothEnabled());
    }

#endregion

#region WifiHelper

    /// <summary>
    /// Подключен ли девайс к какой-либо точке WIFI
    /// </summary>
    /// <returns>true если подключен, false если нет</returns>
    public bool IsWifiConnected()
    {
        if (Application.isEditor)
            return true;

        var result = _wifiHelper != null && _wifiHelper.Call<bool>("isWifiConnected");

        return result;
    }

    /// <summary>
    /// Включен ли на девайсе WIFI (сама функция)
    /// </summary>
    /// <returns>true если включен, false если нет</returns>
    public bool IsWifiEnabled()
    {
        if (Application.isEditor) {return true;}

        var result = _wifiHelper != null && _wifiHelper.Call<bool>("isWifiEnabled");

        return result;
    }

    /// <summary>
    /// Вкл/вкл WIFI
    /// </summary>
    /// <param name="enabled">true включить, false выключить</param>
    public void SetWifiState(bool enabled)
    {
        if (Application.isEditor) {return;}

        _wifiHelper?.Call("setWifiState", enabled);
        Debug.Log($"SetWifiState {enabled}");

        Debug.Log($"AndroidDeviceHelper.SetWifiState(): {enabled}", Debug.LoggerBehaviour.ADD);
    }

    /// <summary>
    /// Начать поиск доступных точек WIFI
    /// </summary>
    /// <returns>true если поиск успешно начался</returns>
    public bool ScanWifi()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return true; }

        var result = _wifiHelper != null && _wifiHelper.Call<bool>("scanWifi");

        return result;
    }

    /// <summary>
    /// Получить список доступных точек WIFI
    /// </summary>
    /// <returns>массив строк SSID-имён точек</returns>
    public string[] GetWifiSSIDs()
    {
        if (Application.isEditor)
            return new string[] { "WIFI_1", "WIFI_2", "WIFI_3", "WIFI_4", "WIFI_5" };

        var ssids = _wifiHelper?.Call<string[]>("getWifiSSIDs").Where(ssid => !string.IsNullOrWhiteSpace(ssid)).ToArray();

        if (ssids == null)
            return Array.Empty<string>();

        Array.Sort(ssids, StringComparer.OrdinalIgnoreCase);

        return ssids;
    }

    /// <summary>
    /// "Забыть" сохраненные настройки точек к которым ранее подключались
    /// </summary>
    public void ClearAllSavedWifiSpots()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _wifiHelper?.Call("clearAllSavedWifiSpots");
        PXR_Enterprise.ControlClearAutoConnectWIFI(removed =>
        {
            Debug.Log($"Removed - {removed}");
        });
    }

    /// <summary>
    /// Register the receiver of Wi-Fi status broadcast
    /// </summary>
    public void RegisterWifiReceiver()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _wifiHelper?.Call("registerWifiReceiver");
    }

    /// <summary>
    /// Unregister the receiver of Wi-Fi status broadcast
    /// </summary>
    public void UnregisterWifiReceiver()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _wifiHelper?.Call("unregisterWifiReceiver");
    }

    private readonly Regex _ssidRegex = new Regex("networkId=\".*?\"",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private string _lastSSID = "";

    public string GetConnectedWifiSSID()
    {
        if (Application.isEditor)
            return "Unity_PC";

        Debug.Log($"!!! can use enterprise? {_canUseEnterpriseService}");

        if (_canUseEnterpriseService)
            Debug.Log($"!!! wifiNameConnected {PXR_Enterprise.StateGetDeviceInfo(SystemInfoEnum.WIFI_NAME_CONNECTED)}");

#if PICO_G3
        return _canUseEnterpriseService && IsWifiEnabled() ? PXR_Enterprise.StateGetDeviceInfo(SystemInfoEnum.WIFI_NAME_CONNECTED) : string.Empty;
#endif

        string dump = ExecuteShellSync("dumpsys netstats");

        if (string.IsNullOrEmpty(dump))
            return string.Empty;

        string[] dumpLines = dump.Split('\n');

        for (var i = 0; i < dumpLines.Length; i++)
        {
            var line = dumpLines[i];

            if (line.Contains("Active interfaces:"))
            {
                if(i + 1 >= dumpLines.Length)
                    return string.Empty;

                string nextLine = dumpLines[i + 1];

                if(!_ssidRegex.IsMatch(nextLine))
                    return string.Empty;

                string ssid = _ssidRegex.Match(nextLine).Value.Replace("networkId=", "").Trim(' ', '"');

                if (ssid != _lastSSID)
                {
                    _lastSSID = ssid;
                    Debug.Log($"AndroidDeviceHelper.GetConnectedWifiSSID(): ssid changed to '{ssid}'", Debug.LoggerBehaviour.ADD);
                }

                return ssid;
            }
        }

        return string.Empty;
    }

    public void ClearWifiSavedPassword()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        var wifiAutoSettings = ContentPath.GetFullPath(WifiSettingsFile, ContentPath.Storage.INTERNAL);

        if(File.Exists(wifiAutoSettings))
            File.Delete(wifiAutoSettings);
    }

    private readonly Regex _macRegex = new Regex("([0-9a-f]{2}[:-]){5}([0-9a-f]{2})",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    /// <summary>
    /// Get the MAC address of connected Wi-Fi
    /// </summary>
    /// <returns>Wi-Fi MAC address, e.g. 0a:22:86:2A:22:E7</returns>
    public string GetWifiMac()
    {
        var result = Application.isEditor ?
            "00:72:64:32:a0:69" :
#if PICO_G2
        ExecuteShellSync("cat /sys/class/net/wlan0/address");

        if (!string.IsNullOrEmpty(result) && _macRegex.IsMatch(result))
            result = _macRegex.Match(result).Value;

#elif PICO_G3
        (_canUseEnterpriseService ? PXR_Enterprise.StateGetDeviceInfo(SystemInfoEnum.WLAN_MAC_ADDRESS) : "---");
#endif

        return result;
    }

    /// <summary>
    /// Get Wi-Fi IP address
    /// </summary>
    /// <returns>Wi-Fi IP address, e.g. 192.168.0.100</returns>
    public string GetWifiIpAddress()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return "127.0.0.1"; }

        return _wifiHelper?.Call<string>("getWifiIpAddress");
    }

    /// <summary>
    /// Connect to Wi-Fi
    /// </summary>
    /// <param name="ssid">SSID of Wi-Fi to be connected</param>
    /// <param name="password">password of Wi-Fi to be connected</param>
    /// <param name="clearSpots">Очистить настройки точки перед подключением</param>
    public async UniTask<bool> ConnectWifi(string ssid, string password, bool clearSpots = true)
    {
        if (string.IsNullOrEmpty(ssid))
        {
            Debug.LogError("ConnectWifi: ssid is empty!");
            return false;
        }

        if (password == null)
        {
            Debug.LogError("ConnectWifi: pass is null!");
            return false;
        }

        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return true; }

        if (!IsWifiEnabled())
        {
            Debug.Log("ConnectWifi: WiFi is disabled, enabling WiFi...");
            SetWifiState(true);
        }

        Debug.Log($"ConnectWifi: Connecting to ssid: '{ssid}', pass: '{password}'");

#if PICO_G2

        ForgetAllWifiNetworks();
        await UniTask.Delay(2000);
        
        ConnectToWifi(ssid, password);

        await UniTask.Delay(5000);
        
        if (GetConnectedWifiSSID() == ssid)
        {
            return true;
        }

#elif PICO_G3

        ClearAllSavedWifiSpots();

        bool isNetworkAutoConnectionSuccessful = false;
        
        if (_canUseEnterpriseService)
        {
            PXR_Enterprise.ControlSetAutoConnectWIFI(ssid, password, result =>
            {
                Debug.Log("ConnectWifi: ControlSetAutoConnectWIFI: " + result);

                isNetworkAutoConnectionSuccessful = result;
            });
        }

        if (!isNetworkAutoConnectionSuccessful)
        {
            // Даем время подключиться к сети. В среднем успешное подключение занимает 1-3 сек, если занимает больше времени - значит подключение безуспешное.
            await UniTask.Delay(5000);
        }

        return isNetworkAutoConnectionSuccessful;
#endif

        return false;
    }
    
    public async void TryReconnectWifi()
    {
        if (IsWifiConnected())
            return;

        if (_wifiReconnectionInProcess)
            return;

        _wifiReconnectionInProcess = true;

        await ReconnectWifiAsync();

        _wifiReconnectionInProcess = false;
    }
    
    
    private async UniTask ReconnectWifiAsync()
    {
        if (Application.isEditor) {{ Debug.Log(EditorCheckMessage); } return; }

        Debug.Log("Переподключение к WiFi", Debug.LoggerBehaviour.ADD);

        bool scan = ScanWifi();

        Debug.Log("WIFI Scanning... " + scan);

        await UniTask.Delay(5000);

        string[] ssids = GetWifiSSIDs();

        List<string> foundSavedSsids = new List<string>();

        string savedWifiSpots = PlayerPrefs.GetString(WifiDataSaver.PlayerPrefsSaveKey);

        if (string.IsNullOrEmpty(savedWifiSpots))
        {
            Debug.Log($"Нету сохраненных точек и нет вай фая", Debug.LoggerBehaviour.ADD);
            return;
        }

        List<WifiData> wifiDataList = JsonConvert.DeserializeObject<List<WifiData>>(savedWifiSpots);

        if (wifiDataList == null)
            return;

        List<string> savedSsids = wifiDataList.Select(t => t.Ssid).ToList();

        foundSavedSsids = ssids.Intersect(savedSsids).ToList();

        if (foundSavedSsids.Count <= 0)
        {
            string savedSsidsString = string.Join(", ", savedSsids);
            string foundSsidsString = string.Join(", ", ssids);
            Debug.Log($"Сохраненные точки {savedSsidsString} не совпадают с найденными {foundSsidsString}, нет вай фая", Debug.LoggerBehaviour.ADD);
            return;
        }

        while (foundSavedSsids.Count > 0 && !IsWifiConnected())
        {
            string wifiSsidToConnect = foundSavedSsids[foundSavedSsids.Count - 1];
            string password = wifiDataList.FirstOrDefault(t => t.Ssid == wifiSsidToConnect)?.Password;

            Debug.Log($"Пытаемся подключиться к сохраненному вай фаю {wifiSsidToConnect} - {password}", Debug.LoggerBehaviour.ADD);

            await ConnectWifi(wifiSsidToConnect, password, false);
            foundSavedSsids.RemoveAt(foundSavedSsids.Count - 1);

            await UniTask.Delay(10 * 1000);
        }

        Debug.Log(
            IsWifiConnected()
                ? $"Успешное переподключение к сохраненному вай фаю {GetConnectedWifiSSID()}"
                : $"Не удалось подключиться ни к одной точке wifi", Debug.LoggerBehaviour.ADD);
    }

    string HidePass(string password)
    {
        if (password == null) return "null";

        string hiddenPass = "";

        for (int i = 0; i < password.Length; i++)
            hiddenPass += "*";

        return hiddenPass;
    }

    public List<WifiNetworkData> GetAvailableNetworks()
    {
        if (Application.isEditor)
        {
            return new List<WifiNetworkData>()
            {
                new WifiNetworkData()
                {
                    SSID = "WIFI_1",
                    HasPassword = true,
                    SignalLevel = 99,
                },
                new WifiNetworkData()
                {
                    SSID = "WIFI_2",
                    HasPassword = true,
                    SignalLevel = 99,
                },
                new WifiNetworkData()
                {
                    SSID = "WIFI_3",
                    HasPassword = true,
                    SignalLevel = 99,
                }
            };
        }

        List<WifiNetworkData> wifiNetworks = new List<WifiNetworkData>();

        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                using (AndroidJavaObject wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
                {
                    wifiManager.Call<bool>("startScan");

                    AndroidJavaObject scanResults = wifiManager.Call<AndroidJavaObject>("getScanResults");
                    int size = scanResults.Call<int>("size");

                    List<WifiNetworkData> networks = new List<WifiNetworkData>();

                    for (int i = 0; i < size; i++)
                    {
                        AndroidJavaObject scanResult = scanResults.Call<AndroidJavaObject>("get", i);
                        string ssid = scanResult.Get<string>("SSID");
                        string capabilities = scanResult.Get<string>("capabilities");
                        int signalLevel = scanResult.Get<int>("level");

                        bool hasPassword = capabilities.Contains("WEP") || capabilities.Contains("WPA") ||
                                           capabilities.Contains("WPA2") || capabilities.Contains("WPA3");

                        if (networks.All(n => n.SSID != ssid))
                        {
                            networks.Add(new WifiNetworkData()
                            {
                                SSID = ssid,
                                HasPassword = hasPassword,
                                SignalLevel = signalLevel
                            });
                        }
                    }

                    return networks;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to get available wifi networks: " + ex.Message);
        }

        return wifiNetworks;
    }

# if PICO_G2
   private void ConnectToWifi(string ssid, string password = null)
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getApplicationContext");
            var wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi");

            if (wifiManager == null)
            {
                Debug.LogError("ConnectToWifi: wifiManager is null!");
                return;
            }

            var configuredNetworks = wifiManager.Call<AndroidJavaObject>("getConfiguredNetworks");
            int size = configuredNetworks.Call<int>("size");

            for (int i = 0; i < size; i++)
            {
                var network = configuredNetworks.Call<AndroidJavaObject>("get", i);
                string configuredSsid = network.Get<string>("SSID").Trim('"');

                if (configuredSsid == ssid)
                {
                    int networkId = network.Get<int>("networkId");

                    bool isEnabled = wifiManager.Call<bool>("enableNetwork", networkId, true);
                    if (!isEnabled)
                    {
                        Debug.LogError("ConnectToWifi: Failed to enable known network!");
                        return;
                    }

                    bool isConnected = wifiManager.Call<bool>("reconnect");
                    if (!isConnected)
                    {
                        Debug.LogError("ConnectToWifi: Failed to reconnect to known network!");
                        return;
                    }

                    Debug.Log("Connected to known Wi-Fi network: " + ssid);
                    return;
                }
            }

            var wifiConfig = new AndroidJavaObject("android.net.wifi.WifiConfiguration");
            wifiConfig.Set("SSID", "\"" + ssid + "\"");

            if (string.IsNullOrEmpty(password))
            {
                wifiConfig.Set("allowedKeyManagement", new AndroidJavaObject("java.util.BitSet"));
            }
            else
            {
                wifiConfig.Set("preSharedKey", "\"" + password + "\"");
            }

            int newNetworkId = wifiManager.Call<int>("addNetwork", wifiConfig);

            if (newNetworkId == -1)
            {
                Debug.LogError("ConnectToWifi: Failed to add network!");
                return;
            }

            bool newIsEnabled = wifiManager.Call<bool>("enableNetwork", newNetworkId, true);
            if (!newIsEnabled)
            {
                Debug.LogError("ConnectToWifi: Failed to enable network!");
                return;
            }

            bool newIsSaved = wifiManager.Call<bool>("saveConfiguration");
            if (!newIsSaved)
            {
                Debug.LogError("ConnectToWifi: Failed to save configuration!");
                return;
            }

            bool newIsConnected = wifiManager.Call<bool>("reconnect");
            if (!newIsConnected)
            {
                Debug.LogError("ConnectToWifi: Failed to reconnect to network!");
                return;
            }

            Debug.Log("Connected to Wi-Fi network: " + ssid);
        }
    }

    private void ForgetAllWifiNetworks()
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getApplicationContext");
            var wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi");

            if (wifiManager == null)
            {
                Debug.LogError("ForgetAllWifiNetworks: wifiManager is null!");
                return;
            }

            var configuredNetworks = wifiManager.Call<AndroidJavaObject>("getConfiguredNetworks");
            if (configuredNetworks == null)
            {
                Debug.LogError("ForgetAllWifiNetworks: configuredNetworks is null!");
                return;
            }

            int size = configuredNetworks.Call<int>("size");
            for (int i = 0; i < size; i++)
            {
                var network = configuredNetworks.Call<AndroidJavaObject>("get", i);
                int networkId = network.Get<int>("networkId");
                wifiManager.Call<bool>("removeNetwork", networkId);
            }

            wifiManager.Call<bool>("saveConfiguration");
        }
    }
#endif

#endregion

#region PowerManagerHelper

    public PerfomanceMode GetCurrentPerfomanceMode()
    {
        if (Application.isEditor)
            return PerfomanceMode.STANDARD;

#if PICO_G3
        return PerfomanceMode.STANDARD;
#endif
        // "стандартный" способ, но проперти может быть не прописана пока не сменили режим производительности.
        // После обновы PUI проперти стирается, но режим производительности после обновы остается прежним
        // ! SetSystemSettingsProperty(SystemAttribute.PERFOMANCE_MODE) не сменит режим производительности !
        string mode = GetSystemSettingsProperty(SystemAttribute.PERFOMANCE_MODE);

        if (_perfomanceModeByPerformanceProp.ContainsKey(mode))
            return _perfomanceModeByPerformanceProp[mode];

        Debug.Log($"Unexpected [{SystemAttribute.PERFOMANCE_MODE}] == '{mode}'");

        // "нестандартный" способ узнать производительность, у каждого режима свой размер буфера.
        string buffer = GetSystemSettingsProperty(SystemAttribute.EYEBUFFER_HEIGTH);

        // для энергосберегающего режима в доках стоит один размер буфера, по факту другой, в словаре прописаны оба
        if (_perfomanceModeByBufferProp.ContainsKey(buffer))
            return _perfomanceModeByBufferProp[buffer];

        Debug.Log($"Unexpected [{SystemAttribute.EYEBUFFER_HEIGTH}] == '{mode}'");

        return PerfomanceMode.UNKNOWN;
    }

    /// <summary>
    /// Смена буфера, влияет на производительность. Чем выше тем четче картинка, стандартный размер 2048
    /// Настройка вступает в силу после перезагрузки шлема. По размеру буфера проверяется Perfomance Mode в меню PICO, нестандартные размеры ставят галочку в High Perfomance
    /// </summary>
    /// <param name="buffer">размер буфера в пикселях, напр. "2048"</param>
    void SetScreenBuffer(string buffer)
    {
#if PICO_G2
        SetSystemSettingsProperty(SystemAttribute.EYEBUFFER_HEIGTH, buffer);
        SetSystemSettingsProperty(SystemAttribute.EYEBUFFER_WIDTH, buffer);
#endif
    }

    /// <summary>
    /// true if device is charging
    /// <para><c>Needs android.permission.DUMP</c></para>
    /// </summary>
    /// <returns></returns>
    public bool IsDeviceCharging()
    {
        if (Application.isEditor) return false;

        return SystemInfo.batteryStatus == BatteryStatus.Charging;
    }

    readonly Regex _batteryLevelRegex = new Regex("level: \\d+", RegexOptions.Compiled);
    /// <summary>
    /// Get device battery level
    /// </summary>
    /// <returns>battery level in range 0-100, -1 if command failed</returns>
    public int GetBatteryLevel()
    {
        if (Application.isEditor) return 100;

        return Mathf.RoundToInt(SystemInfo.batteryLevel * 100);
    }
    
    /// <summary>
    /// Get controller battery level
    /// </summary>
    /// <returns>battery level in range 0-100, with step 25 (0,25,50,75,100)</returns>
    /// <param name="hand">Hand index (0,1)</param>
    public float GetControllerBatteryLevel(int hand)
    {
        if (Application.isEditor)
        {
            return 100;
        }
        
        var value = Controller.UPvr_GetControllerPower(hand) * 25;
        var clampedValue = Mathf.Clamp(value,0, 100);
            
        return clampedValue;
    }

    /// <summary>
    /// Вывести девайс из режима сна в указанное время. Если указанное время меньше текущего (на девайсе) то сработает в указанное время следующего дня
    /// </summary>
    /// <param name="hours">0-24 часа</param>
    /// <param name="minutes">0-59 минут</param>
    /// <param name="seconds">0-59 секунд</param>
    [Obsolete("Начиная с версии 2.1.21 шлем просыпается через Watchdog", false)]
    public void WakeUpAtTime(int hours, int minutes, int seconds)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _powermanagerHelper?.Call("wakeupAtTime", hours, minutes, seconds);
    }

    [Obsolete("Начиная с версии 2.1.21 шлем просыпается через Watchdog", false)]
    public void CreateWakeUpAtDateTime(DateTime dateTime)
    {
        WakeUpAtTimeMilliseconds(DateTimeToMilliseconds(dateTime));

        Debug.Log("BACKUP wakeup set at: " + dateTime.ToString("dd.MM.yyyy HH:mm:ss UTCz"));
    }

    /// <summary>
    /// Вывести девайс из режима сна в указанное время, время задать в миллисекундах
    /// </summary>
    /// <param name="milliseconds">время в миллисекундах, прошедшее с 00:00 01.01.1970 (UTC)</param>
    [Obsolete("Начиная с версии 2.1.21 шлем просыпается через Watchdog", true)]
    public void WakeUpAtTimeMilliseconds(long milliseconds)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _powermanagerHelper?.Call("wakeupAtTimeMilliseconds", milliseconds);
    }

    public void WakeUpsBackup(long everyMs, int id, bool log)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

        _powermanagerHelper?.Call("wakeupRepeat", everyMs, id, log);
    }

    public long? GetCurrentTimeInMilliseconds()
    {
        if (Application.isEditor)
            return DateTimeToMilliseconds(DateTime.Now);

        return _powermanagerHelper?.Call<long>("getCurrentTimeMilliseconds");
    }

    /// <summary>
    /// Request a WakeLock. This function will prevent the device to sleep until releaseWakeLock() Called
    /// </summary>
    public void AcquireWakeLock()
    {
#if PICO_G2
        //_powermanagerHelper?.Call("acquireWakeLock");
#elif PICO_G3
        if(_canUseEnterpriseService)
            PXR_Enterprise.AcquireWakeLock();
#endif
    }

    /// <summary>
    /// Deactivate WakeLock
    /// </summary>
    public void ReleaseWakeLock()
    {
#if PICO_G2
        //_powermanagerHelper?.Call("releaseWakeLock");
#elif PICO_G3
        if(_canUseEnterpriseService)
            PXR_Enterprise.ReleaseWakeLock();
#endif
    }

    /// <summary>
    /// Set system sleep timeout. The sleep timeout must longer than screen off timeout
    /// </summary>
    /// <param name="time">timeout duration in seconds."-1" for never sleep mode</param>
    public void SetPropSleep(string time)
    {
#if PICO_G2
        _powermanagerHelper?.Call("setPropSleep", time);
#endif
    }

    /// <summary>
    /// Sets Screen Off Timeout
    /// </summary>
    /// <param name="time">timeout duration in seconds. "65535" for screen never off</param>
    public void SetPropScreenOff(string time)
    {
#if PICO_G2
        _powermanagerHelper?.Call("setPropScreenOff", time);
#endif
    }

    /// <summary>
    /// Shutdown the device
    /// </summary>
    public void AndroidShutDown()
    {
#if PICO_G2
        _powermanagerHelper?.Call("androidShutDown");
#elif PICO_G3
        if(_canUseEnterpriseService)
            PXR_Enterprise.ControlSetDeviceAction(DeviceControlEnum.DEVICE_CONTROL_SHUTDOWN, i => { });
#endif
    }

    /// <summary>
    /// Reboot the device
    /// </summary>
    public void AndroidReBoot()
    {
        if (Application.isEditor)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return;
        }

#if PICO_G2
        _powermanagerHelper?.Call("androidReBoot");
#elif PICO_G3
        if(_canUseEnterpriseService)
            PXR_Enterprise.ControlSetDeviceAction(DeviceControlEnum.DEVICE_CONTROL_REBOOT, i => { });
#endif
    }

    #endregion

    #region DeviceInfoSend

public DeviceParameters GetDeviceParameters()
   {
       var deviceParameters = new DeviceParameters
       {
           received_signal_strength_indicator = GetSignalStrength(),
           wifi_ssid = GetConnectedWifiSSID(),
           volume = 0,
       };

       return deviceParameters;
   }

public int GetSignalStrength()
   {
       if (Application.isEditor)
           return _debugWifiSignalLevel;

       var wifiConnectionInfo = _wifiManagerHelper.Call<AndroidJavaObject>("getConnectionInfo"); // не кешировать!
       int rssi = wifiConnectionInfo.Call<int>("getRssi");

       return _wifiManagerHelper.CallStatic<int>("calculateSignalLevel", rssi, 100); // https://developer.android.com/reference/android/net/wifi/WifiManager#calculateSignalLevel(int,%20int)
   }

   // большинство полей больше не используется, но вырезать их не надо т.к. на сервере запрос без полей не принимается

   public struct DeviceParameters
   {
       public float temperature;
       public int received_signal_strength_indicator;
       public int connection_speed;
       public int wifi_frequency;
       public int battery_charge_level;
       public int frame_per_second;
       public int[] cpu_frequency;
       public int free_ram;
       public int free_cache_memory;
       public string wifi_ssid;
       public float volume;
       public string storages_info;

       public string ToWifiInfoString()
       {
           if (string.IsNullOrEmpty(wifi_ssid))
               return "no connection";

           return $"ssid: '{wifi_ssid}', signal: {received_signal_strength_indicator}";
       }
   }

   #endregion

   #region KeyConfig

public async void SetHomeButtonFunction(PBS_HomeFunctionEnum homeFunction)
    {
        if (Application.isEditor) {{ Debug.Log(EditorCheckMessage); } return; }

#if PICO_G2
        if (!_canUseToBService)
            await UniTask.WaitWhile(() => _initToBServiceInProcess);

        ToBService.UPvr_PropertySetHomeKey(PBS_HomeEventEnum.SINGLE_CLICK, homeFunction,
            b => { Debug.Log($"SetHomeButtonFunction changed to => {homeFunction}"); });
#endif
    }

public void SetKeyConfig(TextAsset config)
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

#if PICO_G2
        if (config != null)
        {
            _storageHelper?.Call("setKeyConfig", config.text);
        }
        else
        {
            Debug.LogError("AndroidDeviceHelper: SetKeyConfig: config is null");
        }
#endif
    }

    async public void SetKeyConfigNative()
    {
        if (Application.isEditor) { { Debug.Log(EditorCheckMessage); } return; }

#if PICO_G2
        await UniTask.WaitWhile(() => _initToBServiceInProcess);

        if (!_canUseToBService)
        {
            Debug.LogError("AndroidDeviceHelper.SetKeyConfigNative(): call AndroidDeviceHelper.cs -> InitializeToBService() before using this method!");
            return;
        }

        // сообщения не выведутся в лог если эти же функции кнопок уже были присвоены ранее

        ToBService.UPvr_PropertySetHomeKey(PBS_HomeEventEnum.DOUBLE_CLICK, PBS_HomeFunctionEnum.VALUE_HOME_DISABLE,
            b => { Debug.Log("AndroidDeviceHelper.SetKeyConfigNative() =>: DISABLE [Home double click]"); });

        ToBService.UPvr_PropertySetHomeKey(PBS_HomeEventEnum.SINGLE_CLICK, PBS_HomeFunctionEnum.VALUE_HOME_BACK,
            b => { Debug.Log("AndroidDeviceHelper.SetKeyConfigNative() =>: ENABLE [Home double click]"); });
#endif
    }

    #endregion

    #region Other

/// <summary>
    /// Download file from Internet and save
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="saveTo">folder, e.g. "/storage/emulated/0/Download/"</param>
    /// <param name="saveFilename">name of downloaded file, e.g "VRlauncher.apk"</param>
    public IEnumerator DownloadFile(string url, string saveTo, string saveFilename, bool overwrite, Action<string> callback = null)
    {
        string savedPath = Path.Combine(saveTo, saveFilename);

        if (!Directory.Exists(saveTo))
        {
            Debug.LogError("AndroidDeviceHelper.DownloadFile(): saveTo path [" + saveTo + "] not exists! Abort DownloadFile");
            callback?.Invoke(null);
            yield break;
        }

        if (File.Exists(savedPath))
        {
            if (overwrite)
            {
                Debug.LogError("AndroidDeviceHelper.DownloadFile(): file [" + savedPath + "] exists, deleting...");
                File.Delete(savedPath);
            }
            else
            {
                Debug.LogError("AndroidDeviceHelper.DownloadFile(): file [" + savedPath + "] exists!  Abort DownloadFile");
                callback?.Invoke(null);
                yield break;
            }
        }

        using (var unityWebRequest = new UnityWebRequest(url))
        {
            unityWebRequest.method = UnityWebRequest.kHttpVerbGET;

            unityWebRequest.downloadHandler = new DownloadHandlerFile(savedPath) { removeFileOnAbort = true };

            yield return unityWebRequest.SendWebRequest();

            if (unityWebRequest.isHttpError || unityWebRequest.isNetworkError)
            {
                Debug.Log("AndroidDeviceHelper.DownloadFile(): " + unityWebRequest.error);
                callback?.Invoke(null);
            }
            else
            {
                Debug.Log("AndroidDeviceHelper.DownloadFile(): Download saved to: [" + savedPath + "]");
                callback?.Invoke(savedPath);
            }
        }
    }

#endregion

    #region ToBService Callbacks

// эти определения обязательны, иначи колбэки методов Pvr_UnitySDKAPI.ToBService не будут работать.

private void BoolCallback(string value)
    {
        if (ToBService.BoolCallback != null)
            ToBService.BoolCallback(bool.Parse(value));

        ToBService.BoolCallback = null;
    }

    private void IntCallback(string value)
    {
        if (ToBService.IntCallback != null)
            ToBService.IntCallback(int.Parse(value));

        ToBService.IntCallback = null;
    }

    private void LongCallback(string value)
    {
        if (ToBService.LongCallback != null)
            ToBService.LongCallback(long.Parse(value));

        ToBService.LongCallback = null;
    }

    public void toBServiceBind(string s)
    {
#if PICO_G2
        Debug.Log("AndroidDeviceHelper: ToBService binding success!");

        _initToBServiceInProcess = false;
        _canUseToBService = true;
#endif
    }

    #endregion

    /// <summary>
    /// Конвертация DateTime в миллисекунды для Android
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns>время в миллисекундах, прошедшее с 00:00 01.01.1970 (UTC)</returns>
    public static long DateTimeToMilliseconds(DateTime dateTime)
    {
        return (long)dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

public static long DateTimeToSeconds(DateTime dateTime)
    {
        return (long)dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    private bool _timeZoneSet;
    private string _timeZone;

#if PICO_G2
    public void SetTimeZone(string timeZone)
    {
        if (Application.isEditor || _timeZoneSet)
            return;

        if (_timeZone == null)
            _timeZone = PlayerPrefs.GetString(SavedTimeZonePlayerPrefs);

        if(string.IsNullOrEmpty(timeZone) || _timeZone == timeZone)
            return;

        _timeZone = timeZone;

        _alarmManagerHelper.Call("setTimeZone", timeZone);

        PlayerPrefs.SetString(SavedTimeZonePlayerPrefs, timeZone);
        PlayerPrefs.Save();

        _timeZoneSet = true;
    }

#elif PICO_G3
    public void SetTimeZone(WatchdogConfig watchdogConfig, string timeZone)
    {
        if (_timeZoneSet || watchdogConfig == null || string.IsNullOrEmpty(watchdogConfig.CommandIntentAction))
            return;

        if (string.IsNullOrEmpty(_timeZone))
            _timeZone = PlayerPrefs.GetString(SavedTimeZonePlayerPrefs);

        if (!string.IsNullOrEmpty(timeZone) && _timeZone != timeZone)
        {
            PlayerPrefs.SetString(SavedTimeZonePlayerPrefs, timeZone);
            PlayerPrefs.Save();
        }

        _timeZone = timeZone;

        //BroadcastIntentAction(watchdogConfig.CommandIntentAction, "set_utc_offset", "" + _timeZoneHelper.GetUtcHoursOffsetByTimeZone(timeZone));

        _timeZoneSet = true;
    }

    public DateTime GetLocalTime(DateTime dateTime)
    {
        if (!_timeZoneSet || string.IsNullOrEmpty(_timeZone))
            return dateTime;

        return new DateTime();
        //return dateTime.AddHours(_timeZoneHelper.GetUtcHoursOffsetByTimeZone(_timeZone));
    }
#endif

    public DateTime GetLocalTimeNow()
    {
        if(!_timeZoneSet || string.IsNullOrEmpty(_timeZone)) return DateTime.Now;

        return new DateTime();
        //return _timeZoneHelper.GetLocalTimeByTimeZone(_timeZone);
    }

    public string GetLogHeader()
    {
        return "";
        //if (HttpClient.Instance == null) return "HttpClient not initialized";
        //
        //bool hasPasswordAndSSID = PlayerPrefs.HasKey(WifiDataSaver.PlayerPrefsSaveKey);
        //
        //return $"WIFI INFO: {GetDeviceParameters().ToWifiInfoString()}, " +
        //       $"REQUESTS OK: {HttpClient.Instance.SuccessRequestsPercentage}% ({HttpClient.Instance.TotalRequests} total), " +
        //       $"API REQUESTS OK: {HttpClient.Instance.SuccessApiRequestsPercentage}% ({HttpClient.Instance.TotalApiRequests} total) ~{(HttpClient.Instance.TotalApiRequests > 0 ? Mathf.RoundToInt(Time.time / HttpClient.Instance.TotalApiRequests) : 0)} s. per req, " +
        //       $"saved ssid&pass: {hasPasswordAndSSID}";
    }
}

#pragma warning restore 162