using App.AppStates;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Services;
using TMPro;
using UIManager.Keyboard;
using UIManager.UISystem.Abstracts;
using UIManager.UISystem.Attributes;
using UnityEngine;
using Zenject;

namespace UIManager.Windows
{
    [AssetAddress("WiFiPasswordWindow"), UsedImplicitly]
    public class WiFiPasswordController : UIControllerBase<WiFiPasswordWindow>
    {
        [Inject] private AppFSM _appFsm;
        [Inject] private RemoteRequestController _remoteRequestController;
        [Inject] private AndroidDeviceHelper _androidDeviceHelper;
        [Inject] private VideoDownloader _videoDownloader;

        private ButtonBase _backButton;
        private KeyboardControl _keyboardControl;
        private TMP_Text _password;
        private ButtonBase _enterPasswordButton;
        
        public string CurrentSSID { get; set; }
        
        protected override void Initialize()
        {
            _backButton = View.BackButton;
            _keyboardControl = View.KeyboardControl;
            _enterPasswordButton = View.EnterPasswordButton;

            _backButton.OnClick += () => _appFsm.SetState<StartedState>();
            _enterPasswordButton.OnClick += OnEnterButtonPressed;
            
            _password = View.Password;
            _keyboardControl.OnStringUpdated += newString => _password.text = newString;;

            _keyboardControl.Initialize();
        }

        protected override void OnClose()
        {
            View.ErrorText.gameObject.SetActive(false);
            View.KeyboardControl.Clear();
        }

        private async void OnEnterButtonPressed()
        {
            _remoteRequestController.DelayWiFiCheck();
            
            _videoDownloader.StopDownloading();

            View.StartLoading();
            View.ErrorText.gameObject.SetActive(false);
            
            bool isWifiConnectedSuccessful = await _androidDeviceHelper.ConnectWifi(CurrentSSID, _password.text);

            View.StopLoading();
            
            Debug.Log($"ssidToConnect {CurrentSSID}, connectedSSID {_androidDeviceHelper.GetConnectedWifiSSID()?.Trim(' ', '"')}");

            if (_androidDeviceHelper.IsWifiConnected() && isWifiConnectedSuccessful)
            {
                _androidDeviceHelper.WifiDataSaver.AddWifiSpot(CurrentSSID, _password.text);
                
                View.ErrorText.gameObject.SetActive(false);
                View.KeyboardControl.Clear();
                
                if(_appFsm.GetCurrentState().GetType() == typeof(ConnectToWiFiState))
                    _appFsm.SetState<StartedState>();
            }
            else
            {
                View.ErrorText.gameObject.SetActive(true);
            }
        }
    }
}