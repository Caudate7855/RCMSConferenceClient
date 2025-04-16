using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;
using VIAVR.Scripts.Core;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.Data;
using VIAVR.Scripts.Network;
using VIAVR.Scripts.UI.Pages;
using VIAVR.Scripts.UI.Pages.Popups;
using Debug = CustomDebug.Debug;

namespace VIAVR.Scripts
{
    public class AppCore : MonoBehaviour
    {
        public enum DeviceType
        {
            NOT_SPECIFIED, G2, G3
        }
    
        private const string TOKEN_PLAYER_PREFS_KEY = "token";    
    
        public event Action<bool> OnApplicationPaused;
        public event Action<string> OnUpdateFound;
        public event Action<string> OnAppLaunchError;

        [SerializeField] private DeviceType _deviceType = DeviceType.NOT_SPECIFIED;
        [SerializeField] private TextAsset _hotelsJson;
    
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private AndroidDeviceHelper _androidDeviceHelper;
        [SerializeField] private Logger.Logger _logger;
        [SerializeField] private RepeatingTasksManager _repeatingTasksManager;
        [SerializeField] private IdleScreenChecker _idleScreenChecker;
        [SerializeField] private ControllersHandler _controllersHandler;
        [SerializeField] private BatteryHandler _batteryHandler;
        [SerializeField] private VrTourController _vrTourController;
        [SerializeField] private SoundsManager _soundsManager;
    
        [SerializeField] [Expandable] private WatchdogConfig _watchdogConfig;

        private HttpClient _httpClient;

        public DeviceType Device => _deviceType;

        public UIManager UIManager => _uiManager;
        public ControllersHandler ControllersHandler => _controllersHandler;
        public BatteryHandler BatteryHandler => _batteryHandler;
        public SoundsManager SoundsManager => _soundsManager;

        private VrTourRegion _vrTourRegion;

        public VrTourRegion VrTourRegion => _vrTourRegion == null && _hotelsJson != null && !string.IsNullOrEmpty(_hotelsJson.text)
            ? JsonConvert.DeserializeObject<VrTourRegion>(_hotelsJson.text)
            : _vrTourRegion;
    
        public bool UsingNoControllerMode => _controllersHandler.NoControllerMode && _controllersHandler.CurrentControllerState != ControllersHandler.ControllerConnectState.CONNECTED;
        public bool WebServiceInitialized { get; private set; }
    
        [ReadOnly] [SerializeField]
        private string _token;
        public string TOKEN
        {
            get => _token;
            set {
                _token = value;
                PlayerPrefs.SetString(TOKEN_PLAYER_PREFS_KEY, _token);
                PlayerPrefs.Save();
            }
        }

        public string SERIAL => _androidDeviceHelper.GetDeviceId();

#if UNITY_EDITOR
        // эмуляция OnApplicationPause в эдиторе (Ctrl + Shift + P)
        private void Awake()
        {
            UnityEditor.EditorApplication.pauseStateChanged += pauseState =>
            {
                OnApplicationPause(pauseState == UnityEditor.PauseState.Paused);
            };
        }
#endif
    
        private void OnApplicationPause(bool pauseStatus)
        {
            OnApplicationPaused?.Invoke(pauseStatus);
        }

        public void Start()
        {
            //_logger.Initialize();
        
            BetterStreamingAssets.Initialize();
        
            InitializeHttpClient();
        
            InitializeControllerStateChecker();

            //InitializeBatteryHandler();
        
            _soundsManager.Initialize();
        
            _uiManager.Initialize();
        
            //OpenPage<InitializationPage>();
        
            _vrTourController.Initialize(_uiManager.GetPage<VrTourUIPage>());
        
            InitializeRepeatingTasks();
        
            _idleScreenChecker.Initialize(_vrTourController);
        
            //_uiManager.ClosePage<InitializationPage>();
            _uiManager.OpenPage<DefaultPage>();

            if (UsingNoControllerMode)
            {
                UIManager.OpenPage<NoControllerUIPage>();
            
                UniTask.Delay(2000).ContinueWith(RecenterViewRequest);
            }
        }
    
#if UNITY_EDITOR
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
                HttpClient.Instance.BlockRequests = !HttpClient.Instance.BlockRequests;
        }
#endif

        void InitializeHttpClient()
        {
            if (WebServiceInitialized) return;
        
            _httpClient = new HttpClient();
            _httpClient.Initialize(this, _androidDeviceHelper);
        
            _httpClient.OnNoWifiConnection += () =>
            {
                // TODO ReconnectWifi();
            };

            _httpClient.On401 += () =>
            {
                // await WebApiHasNoToken("HttpClient: Ответ от сервера 401. Шлем не привязан к админке?");
            };
        
            WebServiceInitialized = true;
        }

        void InitializeControllerStateChecker()
        {
            ControllersHandler.OnControllerConnectChanged += controllerState =>
            {
                switch (controllerState)
                {
                    case ControllersHandler.ControllerConnectState.DISCONNECTED:
                    case ControllersHandler.ControllerConnectState.CHANGED_TO_DISCONNECTED:
                        if (!ControllersHandler.NoControllerMode)
                        {
                            _uiManager.OpenPage<ControllerConnectPopup>();
                        }
                        else
                        {
                            UIManager.OpenPage<NoControllerUIPage>();
                        }
                        break;
                
                    case ControllersHandler.ControllerConnectState.CONNECTED:
                        _uiManager.ClosePage<ControllerConnectPopup>();
                        break;
                
                    case ControllersHandler.ControllerConnectState.CHANGED_TO_CONNECTED:
                        _uiManager.ClosePage<ControllerConnectPopup>();
                    
                        if (ControllersHandler.NoControllerMode)
                            UIManager.ClosePage<NoControllerUIPage>();
                    
                        // автоматическое выравнивание шлема
                        UniTask.Delay(1000).ContinueWith(RecenterViewRequest);
                        break;
                
                    case ControllersHandler.ControllerConnectState.ERROR:
                        break;
                
                    default:
                        Debug.LogError($"Unhandled {controllerState.GetType()} : {controllerState}.");
                        break;
                }
            };
        
            Singleton<ControllersHandler>.Instance.StartControllerHandling();
        }
    
        void InitializeBatteryHandler()
        {
            const int HELMET_LOW_POWER = 10;

            _batteryHandler = Singleton<BatteryHandler>.Instance;
            _batteryHandler.Initialize();

            /*_batteryHandler.OnControllerPowerChanged += (controllerPower) =>
        {
            _uiManager.ControllerPower = controllerPower;
        };*/

            _batteryHandler.OnHelmetPowerChanged += helmetPower =>
            {
                _uiManager.HelmetPower = helmetPower;

                if (helmetPower <= HELMET_LOW_POWER && !_androidDeviceHelper.IsDeviceCharging())
                {
                    _uiManager.OpenPage<BatteryLowPopup>();
                }
                else
                {
                    _uiManager.ClosePage<BatteryLowPopup>();
                }
            };
        
            _uiManager.HelmetPower = _batteryHandler.HelmetPower;
        }
    
        private const bool TASK_SUCCESS = true;
        private const bool TASK_FAILED = false;

        void InitializeRepeatingTasks()
        {
            // Иконка уровня сигнала вайфая
            _repeatingTasksManager.CreateSimpleTask("UpdateWiFiSignalIcon", async () =>
            {
                _uiManager.ShowWifiSignal(HttpClient.Instance.IsConnectionStable ? _androidDeviceHelper.GetSignalStrength() : 0);

                if (!HttpClient.Instance.IsConnectionStable && !_uiManager.IsPageActive<WifiPage>() && !_uiManager.IsPageActive<SettingsPage>())
                {
                    _uiManager.OpenPage<NoInternetPopup>();
                }
                else
                {
                    _uiManager.ClosePage<NoInternetPopup>();
                }
            
                return TASK_SUCCESS;
           
            }, intervalSeconds: 5);
        }
    
        public void WatchdogWhitelistAddApp(string package, long timeMilliseconds)
        {
            if (string.IsNullOrEmpty(package))
            {
                Debug.LogError($"Can't whitelist package '{package}'");
                return;
            }

            if (_watchdogConfig == null)
            {
                Debug.LogError($"Can't whitelist package '{package}': _watchdogConfig == null'");
                return;
            }
        
            if (timeMilliseconds > 0)
            {
                _androidDeviceHelper.BroadcastIntentAction(
                    _watchdogConfig.CommandIntentAction, "add_whitelist", $"{package}:{timeMilliseconds}"
                );
            }
            else
            {
                Debug.LogError($"Can't whitelist package '{package}': time must be > 0 (now {timeMilliseconds})");
            }
        }

        #region watchdog

        // формат: com.package.oneseconds:1000&com.package.twoseconds:2000&com.package.threeseconds:3000
        public void WatchdogWhitelistAddAppsList(string list)
        {
            if (string.IsNullOrEmpty(list))
            {
                Debug.LogError($"Can't whitelist packages list '{list}'");
                return;
            }

            if (_watchdogConfig == null)
            {
                Debug.LogError($"Can't whitelist packages list '{list}': _watchdogConfig == null'");
                return;
            }
        
            _androidDeviceHelper.BroadcastIntentAction(
                _watchdogConfig.CommandIntentAction, "add_whitelist", list
            );
        }

        public void WatchdogWhitelistRemoveApp(string package)
        {
            if (string.IsNullOrEmpty(package))
            {
                Debug.LogError($"Can't remove package '{package}' from whitelist");
                return;
            }
        
            if (_watchdogConfig == null)
            {
                Debug.LogError($"Can't remove package '{package}' from whitelist: _watchdogConfig == null'");
                return;
            }
        
            _androidDeviceHelper.BroadcastIntentAction(
                _watchdogConfig.CommandIntentAction, "remove_whitelist", package);
        }
    
        public void WatchdogWhitelistRemoveAllApps()
        {
            if (_watchdogConfig == null)
            {
                Debug.LogError($"Can't remove all packages from whitelist: _watchdogConfig == null'");
                return;
            }
        
            _androidDeviceHelper.BroadcastIntentAction(
                _watchdogConfig.CommandIntentAction, "clear_whitelist", "");
        }

        #endregion

        public void OpenWifiPage()
        {
            _uiManager.CloseAllPages(PageLayer.STATIC_POPUP);
        
            _uiManager.OpenPage<WifiPage>();
        }

        public void OpenSettings()
        {
            _uiManager.CloseAllPages(PageLayer.STATIC_POPUP);
        
            _uiManager.OpenPage<SettingsPage>();
        }

        public void StartVrTour(VrTourApartment roomData)
        {
            _uiManager.CloseAllPages(PageLayer.DEFAULT);
        
            _uiManager.OpenPage<VrTourUIPage>();
        
            _vrTourController.StartVrTour(roomData);
        }

        public void EndVrTour()
        {
            _uiManager.ClosePage<VrTourUIPage>();
        
            _uiManager.OpenPage<DefaultPage>();
        
            _vrTourController.EndVrTour();
        
            _uiManager.OpenPage<RecenterPopup>();
        }

        public void RecenterViewRequest()
        {
            _uiManager.OpenPage<RecenterPopup>();
        }
    }
}