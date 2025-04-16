using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UIManager.UISystem.Abstracts;
using UIManager.UISystem.Attributes;
using UIManager.UISystem.UIManager;
using UnityEngine;
using Zenject;
using Debug = CustomDebug.Debug;

namespace UIManager.Windows
{
    [AssetAddress("WiFiNetworksListWindow"), UsedImplicitly]
    public class WiFiNetworksListController : UIControllerBase<WiFiNetworksListWindow>
    {
        [Inject] private AppFSM _appFsm;
        [Inject] private IUIManager _uiManager;
        [Inject] private AndroidDeviceHelper _androidDeviceHelper;

        private WiFiPasswordController _wiFiPasswordController;
        private NetworkButton _networkButton;
        private ButtonBase _backButton;
        private Transform _networksScroll;
        private List<NetworkButton> _networkWindows = new List<NetworkButton>();
        
        protected override void Initialize()
        {
            _backButton = View.BackButton;
            _networksScroll = View.NetworkScroll;
            _networkButton = View.NetworkButton;
            
            _backButton.OnClick += OnBackButtonPressed;
            View.OnUpdateWifiListCommand += CreateNetworkList;
        }

        private void CreateNetworkList()
        {
            foreach (Transform child in _networksScroll)
                Object.Destroy(child.gameObject);
            
            _networkWindows.Clear();

            var networks = _androidDeviceHelper.GetAvailableNetworks();

            if (networks == null || networks.Count == 0)
                return;
            
            string currentWifiName = "";
            bool connected = _androidDeviceHelper.IsWifiConnected();

            if (connected)
                currentWifiName = _androidDeviceHelper.GetConnectedWifiSSID()?.Trim(' ', '"');

            foreach (var network in networks)
            {
                if (network.SSID == currentWifiName || string.IsNullOrEmpty(network.SSID))
                    continue;
                
                var networkInstance = GameObject.Instantiate(_networkButton, _networksScroll);

                networkInstance.SetNetworkName(network.SSID);

                _networkWindows.Add(networkInstance);

                if (network.HasPassword)
                    networkInstance.OnNetworkButtonPressed += SelectNetworkAndOpenPasswordPage;
                else
                    networkInstance.OnNetworkButtonPressed += ConnectToWifiWithNoPassword;
            }
        }

        private void SelectNetworkAndOpenPasswordPage(string networkName)
        {
            Debug.Log($"Open Password Page {networkName}");
            
            _wiFiPasswordController = _uiManager.Load<WiFiPasswordController>();
            _wiFiPasswordController.CurrentSSID = networkName;
            Close();
            _wiFiPasswordController.Open();
        }

        private async void ConnectToWifiWithNoPassword(string ssid)
        {
            Debug.Log($"Connect to WiFi with no Password. SSID: {ssid}");

            _androidDeviceHelper.ConnectWifi(ssid, "");

            foreach (NetworkButton networkWindow in _networkWindows) 
                networkWindow.Button.Interactable = false;
            
            await UniTask.Delay(3000);
            
            foreach (NetworkButton networkWindow in _networkWindows) 
                networkWindow.Button.Interactable = true;

            Debug.Log($"ssidToConnect {ssid}, connectedSSID {_androidDeviceHelper.GetConnectedWifiSSID()?.Trim(' ', '"')}");
            
            if (_androidDeviceHelper.IsWifiConnected() && ssid == _androidDeviceHelper.GetConnectedWifiSSID()?.Trim(' ', '"'))
                _appFsm.SetState<StartedState>();
        }

        private void OnBackButtonPressed() => _appFsm.SetState<StartedState>();
    }
}