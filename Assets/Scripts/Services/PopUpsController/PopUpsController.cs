using System;
using App.AppStates;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UIManager;
using UIManager.LowBatteryErrorPopUp;
using UIManager.VideoErrorPopUp;
using UIManager.WiFiErrorPopUp;
using UIManager.Windows;
using VIAVR.Scripts.Core;
using Zenject;
using Object = UnityEngine.Object;

namespace Services
{
    [UsedImplicitly]
    public class PopUpsController
    {
        [Inject] private AndroidDeviceHelper _androidDeviceHelper;
        [Inject] private PopUpService _popUpService;
        [Inject] private AppFSM _appFsm;
        [Inject] private VideoDownloader _videoDownloader;

        private VideoErrorPopUp _videoErrorPopUp;
        private ControllersHandler _controllersHandler;
        private bool _isPossibleShowWifiErrorPopup = true;
        private bool _isPossibleShowContentErrorPopup = true;
        private bool _isBatteryPopUpShowedAtMaxChargePercents;
        private bool _isBatteryPopUpShowedAtMinChargePercents;
        
        private const int ShowPopupCooldownTimeMilliseconds = 15000;
        private const int MinChargeValueToShowPopUpInPercents = 5;
        private const int MaxChargeValueToShowPopUpInPercents = 10;
        
        public bool IsContentShowingBlocked { get; private set; }

        public PopUpsController()
        {
            _controllersHandler = Object.FindObjectOfType<ControllersHandler>();

            if (_controllersHandler.NoControllerMode)
                InitializeNoControllerCenter();
            else
                InitializeControllerGuide();
        }

        public void TryShowWifiErrorPopUp(bool isPossibleCheckWifi)
        {
            if (_appFsm.GetCurrentState().GetType() != typeof(ConnectToWiFiState))
            {
                if (_isPossibleShowWifiErrorPopup && isPossibleCheckWifi)
                {
                    _popUpService.GetPopUp<WiFiErrorPopUp>().OnClosePerformed += (async () =>
                        await StartErrorPopupCooldownTimer(value => _isPossibleShowWifiErrorPopup = value));
                    _popUpService.OpenPopup<WiFiErrorPopUp>();
                    
                    _appFsm.SetState<StartedState>();
                }
            }
        }

        public void TryShowChargeErrorPopUp()
        {
            if (_androidDeviceHelper.GetBatteryLevel() > MaxChargeValueToShowPopUpInPercents
                || _androidDeviceHelper.IsDeviceCharging())
            {
                _isBatteryPopUpShowedAtMaxChargePercents = false;
                _isBatteryPopUpShowedAtMinChargePercents = false;
                return;
            }
            
            if (_androidDeviceHelper.GetBatteryLevel() <= MaxChargeValueToShowPopUpInPercents 
                && _androidDeviceHelper.GetBatteryLevel() > MinChargeValueToShowPopUpInPercents 
                && !_isBatteryPopUpShowedAtMaxChargePercents)
            {
                _isBatteryPopUpShowedAtMaxChargePercents = true;
                _popUpService.OpenPopup<LowBatteryErrorPopUp>();
            }
            
            if (_androidDeviceHelper.GetBatteryLevel() <= MinChargeValueToShowPopUpInPercents &&
                !_isBatteryPopUpShowedAtMinChargePercents)
            {
                _isBatteryPopUpShowedAtMinChargePercents = true;
                _popUpService.OpenPopup<LowBatteryErrorPopUp>();
            }
        }

        public void ShowContentErrorPopUp()
        {
            if (_isPossibleShowContentErrorPopup)
            {
                _videoErrorPopUp = _popUpService.GetPopUp<VideoErrorPopUp>();
                _videoErrorPopUp.OnClosePerformed += (async () =>
                    await StartErrorPopupCooldownTimer(value => _isPossibleShowContentErrorPopup = value));

                _popUpService.OpenPopup<VideoErrorPopUp>();
                _appFsm.SetState<StartedState>();

                IsContentShowingBlocked = true;
            }
        }

        public async void ShowVerificationPopup(string verificationCode)
        {
            var verificationPopup = _popUpService.GetPopUp<VerificationPopup>();
            
            if (verificationPopup.gameObject.activeSelf)
            {
                verificationPopup.Close();
                await UniTask.Delay(500);
            }
            verificationPopup.VerificationCode = verificationCode;
            _popUpService.OpenPopup<VerificationPopup>();
        }

        public void UnblockDevice()
        {
            _videoErrorPopUp?.Close();
            IsContentShowingBlocked = false;
        }

        public void ShowWebCenteringPopUp() => 
            _popUpService.OpenPopup<NoControllerWebCenterPopUp>();

        private async void InitializeControllerGuide()
        {
            await UniTask.Delay(2000);

            _popUpService.GetPopUp<ControllerGuidePopUp>().Close();

            _controllersHandler.OnControllerConnectChanged += state =>
            {
                if (state == ControllersHandler.ControllerConnectState.CHANGED_TO_DISCONNECTED)
                    _popUpService.OpenPopup<ControllerGuidePopUp>();
            };

            if (_controllersHandler.IsConnected == false)
                _popUpService.OpenPopup<ControllerGuidePopUp>();
        }

        private async UniTask StartErrorPopupCooldownTimer(Action<bool> setFlag)
        {
            setFlag(false);
            await UniTask.Delay(ShowPopupCooldownTimeMilliseconds);
            setFlag(true);
        }

        private async void InitializeNoControllerCenter()
        {
            await UniTask.Delay(1500);

            _popUpService.OpenPopup<NoControllerLauncherCenterPopUp>();
        }
    }
}