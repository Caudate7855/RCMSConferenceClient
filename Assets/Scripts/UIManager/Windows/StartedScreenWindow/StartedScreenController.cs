using System;
using System.Collections.Generic;
using System.Threading;
using App.AppStates;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using Services;
using TMPro;
using UIManager.UISystem.Abstracts;
using UIManager.UISystem.Attributes;
using UIManager.UISystem.UIManager;
using UnityEngine;
using UnityEngine.UI;
using VIAVR.Scripts.Core;
using Zenject;
using Object = UnityEngine.Object;

namespace UIManager.Windows
{
    [AssetAddress("StartedScreenWindow"), UsedImplicitly]
    public class StartedScreenController : UIControllerBase<StartedScreenView>
    {
        private const string NoSessionKey = "No session";
        private const float DarkenWifiButtonAmount = 80f;
        private const float MaxColorValue = 255f;
        private const int UpdateDeviceInfoDelayInMilliseconds = 2000;

        [Inject] private AppFSM _appFsm;
        [Inject] private AndroidDeviceHelper _androidDeviceHelper;
        [Inject] private VideoDownloader _videoDownloader;
        
        private readonly Dictionary<LocalizationLanguages, GameObject> _languageOutlines =
            new Dictionary<LocalizationLanguages, GameObject>();
        private LocalizationLanguages _currentLanguage;

        private DownloadInfoWindow _downloadContentWindow;
        private GameObject _waitingForContentWindow;
        private CanvasGroup _techPanel;
        private ButtonBase _techPanelButton;
        private TMP_Text _controllerChargeValue;
        private TMP_Text _deviceChargeValue;
        private TMP_Text _deviceWiFiConnectionValue;
        private TMP_Text _deviceToken;
        private TMP_Text _sessionTitle;
        private Slider _headsetChargeSlider;
        private Slider _controllerChargeSlider;
        
        private float _ssidStableTime = 0f;
        private bool _isPanelOpenedByUser = false;

        private List<Image> _buttonImages = new List<Image>();
        private List<Color> _originalColors = new List<Color>();

        private CancellationTokenSource _cts;

        protected async override void Initialize()
        {
            _deviceToken = View.TokenValue;
            _deviceChargeValue = View.DeviceChargeValue;
            _controllerChargeValue = View.ControllerChargeValue;
            _deviceWiFiConnectionValue = View.DeviceWiFiConnectionValue;
            _downloadContentWindow = View.DownloadContentWindow;
            _sessionTitle = View.SessionTitle;

            _headsetChargeSlider = View.HeadsetChargeSlider;
            _controllerChargeSlider = View.ControllerChargeSlider;
            _techPanel = View.TechPanel;
            _techPanelButton = View.TechPanelButton;
            
            _waitingForContentWindow = View.WaitingForContentWindow;
            _waitingForContentWindow.SetActive(false);

            View.WiFiButton.OnClick += ShowWifiNetworksListWindow;
            View.RussianLanguageButton.OnClick += () => ChangeLanguage(LocalizationLanguages.Russian);
            View.EnglishLanguageButton.OnClick += () => ChangeLanguage(LocalizationLanguages.English);

            _languageOutlines.Add(LocalizationLanguages.Russian, View.RussianFlagOutline);
            _languageOutlines.Add(LocalizationLanguages.English, View.EnglishFlagOutline);

            _currentLanguage = LocalizationLanguages.Russian;

            //если режим без контроллера - отключаем строку "Заряд контроллера"
            if (Object.FindObjectOfType<ControllersHandler>().NoControllerMode) 
                View.ChargeControllerGameObject.gameObject.SetActive(false);
            
            //при инициализации отключаем окно загрузки чтобы не висело пустое окно со спиннером
            CloseDownloadContentWindow();

            await UniTask.Delay(150);
            
            _videoDownloader.OnDownloadInfoChanged += UpdateDownloadInfo;
            _videoDownloader.OnDownloadingStarted += _downloadContentWindow.SetDownloadingState;
            
            _buttonImages.AddRange(View.WiFiButton.GetComponentsInChildren<Image>());
            
            foreach (var img in _buttonImages)
            {
                _originalColors.Add(img.color);
            }

            _techPanelButton.OnClick += OnTechButtonClick;
        }
        
        public void UpdateDeviceToken(string newDeviceToken) => _deviceToken.text = newDeviceToken;

        public void ShowSessionName(string sessionName)
        {
            _sessionTitle.text = sessionName;

            _sessionTitle.GetComponent<LocalizableText>().IsLocalizable = false;
            
            Debug.Log($"TestBuild Session {sessionName}");
        }
        
        public async void ShowNoSession()
        {
            _sessionTitle.GetComponent<LocalizableText>().IsLocalizable = true;

            _sessionTitle.text = await LocalizationManager.GetLocalizedTextAsync(NoSessionKey);
        }
        
        public void ShowTechnologyPanel()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            
            _techPanel.gameObject.SetActive(true);
            _techPanel.DOFade(1f, 0.5f);
        }

        public void CloseTechnologyPanel()
        {
            if (!_isPanelOpenedByUser) 
                _techPanel.DOFade(0f, 0.5f).OnComplete(() => _techPanel.gameObject.SetActive(false));
        }

        public void ShowDownloadContentWindow()
        {
            _downloadContentWindow.gameObject.SetActive(true);
            _techPanelButton.gameObject.SetActive(true);
        }

        public void CloseDownloadContentWindow()
        {
            _downloadContentWindow.gameObject.SetActive(false);
            _techPanelButton.gameObject.SetActive(false);
        }

        public void ShowWaitingForContentWindow()
        {
            _waitingForContentWindow.SetActive(true);
            _techPanelButton.gameObject.SetActive(true);
        }

        public void CloseWaitingForContentWindow()
        {
            _waitingForContentWindow.SetActive(false);
            _techPanelButton.gameObject.SetActive(false);
        }


        protected override async void OnOpen()
        {
            await StartUpdateInfoCycle();
        }

        private void ShowWifiNetworksListWindow() => _appFsm.SetState<ConnectToWiFiState>();

        private async void OnTechButtonClick()
        {
            _isPanelOpenedByUser = !_isPanelOpenedByUser;
            
            _techPanel.DOKill();

            if (_isPanelOpenedByUser)
            {
                ShowTechnologyPanel();

                _cts = new CancellationTokenSource();
                var isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(20), cancellationToken: _cts.Token).SuppressCancellationThrow();
                if (!isCancelled)
                {
                    _isPanelOpenedByUser = false;
                    CloseTechnologyPanel();
                }
            }
            else
            {
                CloseTechnologyPanel();
            }
        }
        private async UniTask StartUpdateInfoCycle()
        {
            while (IsOpened)
            {
                UpdateDeviceInfoValues();
                await UniTask.Delay(UpdateDeviceInfoDelayInMilliseconds);
            }
        }

        private void UpdateDownloadInfo(DownloadInfoData data)
        {
            _downloadContentWindow.UpdateSliderValue(data.DownloadedBytes, data.TotalBytes);
            _downloadContentWindow.UpdateDownloadableContentInfo(data.CurrentContentName, data.CurrentContentValue, data.MaxContentValue );
            _downloadContentWindow.UpdateDownloadedSize(data.DownloadedBytes, data.TotalBytes);
        }

        private void UpdateDeviceInfoValues()
        {
            UpdateDeviceConnectionSsid();
            UpdateChargeValues();
            UpdateWifiSignalImage();
            UpdateDeviceToken();
        }

        private void UpdateDeviceConnectionSsid()
        {
            string newSsidText = _androidDeviceHelper.GetConnectedWifiSSID();
            
            if (!_androidDeviceHelper.IsWifiConnected() ||
                string.IsNullOrEmpty(newSsidText) 
                || newSsidText == "<unknown ssid>")
            {
                _deviceWiFiConnectionValue.text = "-";
                _appFsm.SetState<StartedState>();
                return;
            }
            
            _deviceWiFiConnectionValue.text = newSsidText.Trim(' ', '"');
        }

        private void UpdateChargeValues()
        {
            _deviceChargeValue.text = $"{_androidDeviceHelper.GetBatteryLevel()} %";
            _controllerChargeValue.text = $"{_androidDeviceHelper.GetControllerBatteryLevel(0)} %";

            _headsetChargeSlider.value = _androidDeviceHelper.GetBatteryLevel();
            _controllerChargeSlider.value = _androidDeviceHelper.GetControllerBatteryLevel(0);
        }

        private void UpdateWifiSignalImage()
        {
            if (!_androidDeviceHelper.IsWifiConnected())
            {
                View.WifiSignalImage.sprite = View.WifiSignalStrengthSprites[0];
                return;
            }

            float wifiSignalStep = 100f / (View.WifiSignalStrengthSprites.Count - 1);
            int signalStrength = _androidDeviceHelper.GetSignalStrength();;

            int signalImageNumber = (int) (signalStrength / wifiSignalStep);

            if (signalImageNumber + 1 >= View.WifiSignalStrengthSprites.Count)
            {
                View.WifiSignalImage.sprite = View.WifiSignalStrengthSprites[View.WifiSignalStrengthSprites.Count - 1];
                return;
            }

            View.WifiSignalImage.sprite = View.WifiSignalStrengthSprites[signalImageNumber + 1];
        }

        private void UpdateDeviceToken()
        {
            if (PlayerPrefs.HasKey(GameGlobalConsts.DeviceTokenSaveKay))
                _deviceToken.text = PlayerPrefs.GetString(GameGlobalConsts.DeviceTokenSaveKay);
        }

        private void ChangeLanguage(LocalizationLanguages languageType)
        {
            if (_currentLanguage == languageType)
                return;

            LocalizationManager.ChangeLanguage(languageType);
            
            SwitchFlagsOutline(languageType);

            _currentLanguage = languageType;

            _downloadContentWindow.UpdateDownloadedSize();
        }

        private void SwitchFlagsOutline(LocalizationLanguages languageType)
        {
            foreach (var outline in _languageOutlines.Values)
            {
                outline.SetActive(false);
            }

            if (_languageOutlines.TryGetValue(languageType, out var outlineToActivate))
            {
                outlineToActivate.SetActive(true);
            }
        }
    }
}