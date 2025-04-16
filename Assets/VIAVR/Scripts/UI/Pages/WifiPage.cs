using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.UI.Activators;
using VIAVR.Scripts.UI.Buttons;
using Debug = CustomDebug.Debug;

namespace VIAVR.Scripts.UI.Pages
{
    public class WifiPage : PageBase
    {
        public enum WifiPageState
        {
            CLOSED, INITIALIZATION, PINCODE, MAIN_SCREEN
        }

        private const string PagePincode = "314141"; // ПАРОЛЬ
    
        [Header("Wifi Page")]
        [SerializeField] private int _settingsWhitelistTimeMs = 60000;
        [SerializeField] private ActivatorGameObject _panelPincode;
        [SerializeField] private ActivatorGameObject _panelConnected;

        [SerializeField] private ActivatorsGroup _activatorsGroup;

        [HorizontalLine]
        [SerializeField] private TextMeshProUGUI _textInfoConnected;
        [SerializeField] private TextMeshProUGUI _macAddress;

        [SerializeField] private PincodeControl _pincodeControl;

        [SerializeField] private ButtonBase _closeButton;
        [SerializeField] private ButtonBase _buttonSettings;
        [SerializeField] private GameObject _panelMacGameObject;

        private AndroidDeviceHelper _androidDeviceHelper;

        string _currentSsid;
        private string CurrentSSID
        {
            get => _currentSsid;
            set
            {
                if (_currentSsid == value)
                    return;

                _currentSsid = value;
                UpdateInfoTexts();
            }
        }

        private bool IsConnected => !string.IsNullOrEmpty(CurrentSSID);

        private WifiPageState _currentPageState = WifiPageState.CLOSED;

        private bool _access;

        private bool _isCheckingConnection;

        public override bool InitializePage()
        {
            if(base.InitializePage()) return PAGE_INITIALIZED;

            _androidDeviceHelper = Singleton<AndroidDeviceHelper>.Instance;
        
            _pincodeControl.Initialize();

            _pincodeControl.OnPincodeFilled += pincode =>
            {
                if(TryGetAccess(pincode)) ShowPage(WifiPageState.INITIALIZATION);
            };
        
            _pincodeControl.OnPincodeSend += pincode =>
            {
                if(TryGetAccess(pincode)) ShowPage(WifiPageState.INITIALIZATION);
            };

            _buttonSettings.OnClick += () =>
            {
                GoToPicoSettings();
            };

            _closeButton.OnClick += () =>
            {
                _currentPageState = WifiPageState.CLOSED;
            
                _appCore.UIManager.ClosePage(this);
            };

            CurrentSSID = _androidDeviceHelper.GetConnectedWifiSSID();

            return PAGE_CONTINUE_INITIALIZATION;
        }

        public override void OnPageOpen()
        {
            _access = false;
        
            _pincodeControl.Clear();

            CurrentSSID = _androidDeviceHelper.GetConnectedWifiSSID();

            ShowPage(WifiPageState.INITIALIZATION); // PINCODE
        }

        public override void OnPageClose()
        {
            _currentPageState = WifiPageState.CLOSED;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if(!gameObject.activeSelf) return;


            if (Input.GetKeyDown(KeyCode.I))
                CurrentSSID = "LOLKEK";
        }
#endif

        void OnApplicationPause(bool pause)
        {
            if (!_initialized)
                return;

            if(!pause)
                CurrentSSID = _androidDeviceHelper.GetConnectedWifiSSID();
        }

        private bool TryGetAccess(string pincode)
        {
            _access = pincode == PagePincode;
        
            _pincodeControl.DisplayPincodeValidation(_access);
        
            return _access;
        }

        private void ShowPage(WifiPageState state)
        {
            if(_currentPageState == state) return;

            _currentPageState = state;

            switch (state)
            {
                case WifiPageState.CLOSED:
                    _currentPageState = WifiPageState.CLOSED;
                    break;
            
                case WifiPageState.PINCODE:
                    _activatorsGroup.ActivateElement(_panelPincode, true, false);

                    _panelMacGameObject.SetActive(false);
                    _macAddress.text = "";
                    break;
            
                case WifiPageState.INITIALIZATION:
                    _activatorsGroup.ActivateElement(null, true, false);
                
                    _panelMacGameObject.SetActive(true);
                    _macAddress.text = _androidDeviceHelper.GetWifiMac();
                
                    CurrentSSID = _androidDeviceHelper.GetConnectedWifiSSID();

                    ShowPage(WifiPageState.MAIN_SCREEN);
                    break;
            
                case WifiPageState.MAIN_SCREEN:
                    _activatorsGroup.ActivateElement(_panelConnected, true, false);

                    CurrentSSID = _androidDeviceHelper.GetConnectedWifiSSID();

                    CheckLostConnection();
                    break;
            
                default:
                    Debug.Log($"Unhandled {nameof(WifiPageState)} {state}");
                    break;
            }
        }

        private void GoToPicoSettings()
        {
            _appCore.WatchdogWhitelistAddApp("com.picovr.settings", _settingsWhitelistTimeMs);
            _androidDeviceHelper.ExecuteShellSync("am start -n com.picovr.settings/com.picovr.vrsettingslib.UnityActivity");
        }

        async void CheckLostConnection()
        {
            if(_isCheckingConnection) return;

            _isCheckingConnection = true;
        
            while (_isCheckingConnection)
            {
                if(Visible && _currentPageState == WifiPageState.MAIN_SCREEN)
                    UpdateInfoTexts();
            
                await UniTask.Delay(1000);
            }

            _isCheckingConnection = false;
        }

        void UpdateInfoTexts()
        {
            _textInfoConnected.text = IsConnected ?
                $"Шлем подключен к точке доступа {CurrentSSID}" :
                "Шлем не подключен к WiFi";
        }
    }
}