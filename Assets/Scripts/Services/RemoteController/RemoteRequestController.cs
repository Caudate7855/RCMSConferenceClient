using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using App.AppStates;
using BestHTTP.Extensions;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Services.GetExchange;
using Services.PostExchange;
using UIManager.UISystem.UIManager;
using UIManager.Windows;
using UIManager.Windows.VideoWindow;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;
using Player = Services.GetExchange.Player;

namespace Services
{
    public class RemoteRequestController : MonoBehaviour
    {
        [SerializeField] private bool _isProductionBuild;

        [Inject] private RemoteContentContainer _remoteContentContainer;
        [Inject] private IUIManager _uiManager;
        [Inject] private AppFSM _appFsm;
        [Inject] private VideoDownloader _videoDownloader;
        [Inject] private DeviceStorageManager _deviceStorageManager;
        [Inject] private PopUpsController _popUpsController;
        [Inject] private AndroidDeviceHelper _androidDeviceHelper;
        [Inject] private PlayerVolumeController _playerVolumeController;

        private StartedScreenController _startedScreenController;
        private VideoWindowController _videoWindowController;
        private CancellationTokenSource _wifiWifiCheckCancellationTokenSource;
        private Player _player;
        private MediaManagement _mediaManagement;
        private string _getVolumeCached = "";
        private string _domainPath = "https://dev.vrvr.global/api";
        private string _cachedCode = "";
        private bool _isPossibleShowWifiPopup = true;
        private bool _contentIsDownloading;
        private bool _tokenIsRegistered;


        private readonly Dictionary<string, VideoPlaybackStates> _videoPlaybackStatesDictionary =
            new Dictionary<string, VideoPlaybackStates>()
            {
                { "paused", VideoPlaybackStates.Paused },
                { "playing", VideoPlaybackStates.Playing },
                { "stopped", VideoPlaybackStates.Stopped }
            };

        private readonly Dictionary<string, VideoManagementAction> _videoManagementActionsDictionary =
            new Dictionary<string, VideoManagementAction>()
            {
                { "sync", VideoManagementAction.Sync },
                { "rewind", VideoManagementAction.Rewind },
                { "upload", VideoManagementAction.Download },
                { "stop upload", VideoManagementAction.StopDownload }
            };
        
        private const string DevPath = "https://dev.vrvr.global/api";
        private const string ProdPath = "https://st.vrvr.global/api";
        private const string PostDevicesPath = "/remote-devices";
        private const string ExchangeInfoPath = "/remote-devices/exchange-info";
        private const string ContentToDownloadPath = "/remote-devices/content-to-upload";
        private const string GenerateCodePath = "/remote-devices/generate-code";
        private const float RequestDelayTime = 3f;
        private const float RequestRepeatTime = 3f;

        public bool IsProductionBuild => _isProductionBuild;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            DelayWiFiCheck();
        }

        public void Initialize()
        {
            SetDomainPath();

            _startedScreenController = _uiManager.Load<StartedScreenController>();
            _videoWindowController = _uiManager.Load<VideoWindowController>();
            
            _startedScreenController.Close();
            
            _playerVolumeController.Initialize(_videoWindowController);
            _playerVolumeController.StartCheckSystemVolume();

            InvokeRepeating(nameof(RequestData), RequestDelayTime, RequestRepeatTime);
        }

        public async void DelayWiFiCheck()
        {
            _wifiWifiCheckCancellationTokenSource?.Cancel();
            _wifiWifiCheckCancellationTokenSource = new CancellationTokenSource();
    
            _isPossibleShowWifiPopup = false;

            try
            {
                await UniTask.Delay(30000, cancellationToken: _wifiWifiCheckCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            
            _isPossibleShowWifiPopup = true;
        }

        private void SetDomainPath() => _domainPath = _isProductionBuild ? ProdPath : DevPath;

        private async UniTask<bool> TryRegisterTokenAsync()
        {
            if (string.IsNullOrEmpty(_androidDeviceHelper.GetDeviceId()))
            {
                Debug.Log($"Device ID is null");
                return false;
            }
            
            var token = await RegisterDeviceAsync(_androidDeviceHelper.GetDeviceId());

            if (string.IsNullOrEmpty(token))
            {
                Debug.Log($"Device token is null");
                return false;
            }

            PlayerPrefs.SetString(GameGlobalConsts.DeviceTokenSaveKay, token);
            _startedScreenController.UpdateDeviceToken(token);
            _tokenIsRegistered = true;

            Debug.Log($"Serial number value - {_androidDeviceHelper.GetDeviceId()}");
            Debug.Log($"TestBuild Device token {token}");

            return true;
        }

        private async void RequestData()
        {
            _popUpsController.TryShowChargeErrorPopUp();

            if (!_androidDeviceHelper.IsWifiConnected())
            {
                _videoDownloader.StopDownloading();
                _popUpsController.TryShowWifiErrorPopUp(_isPossibleShowWifiPopup);
                _androidDeviceHelper.TryReconnectWifi();
                _startedScreenController.ShowTechnologyPanel();
                return;
            }
            
            if (!_tokenIsRegistered)
            {
                if(!await TryRegisterTokenAsync())
                {
                    _startedScreenController.ShowTechnologyPanel();
                    Debug.LogError("Token is null! (RequestData)");
                    return;
                }
            }

            var generatedCode = await GetGeneratedCode(_domainPath + GenerateCodePath,
                PlayerPrefs.GetString(GameGlobalConsts.DeviceTokenSaveKay));
            
            if (generatedCode?.code != null && generatedCode.code != _cachedCode)
            {
                _popUpsController.ShowVerificationPopup(generatedCode.code);
                _cachedCode = generatedCode.code;
            }

            var exchangeInfo = await SendExchangeDataAsync();

            if (exchangeInfo == default)
                return;

            _player = exchangeInfo.player;
            _mediaManagement = exchangeInfo.management;
            
            if (_mediaManagement.need_centering)
                _popUpsController.ShowWebCenteringPopUp();

            if (!string.IsNullOrEmpty(exchangeInfo.volume))
            {
                if (_getVolumeCached != exchangeInfo.volume)
                    _playerVolumeController.SetVolume(exchangeInfo.volume.ToInt32());

                _getVolumeCached = exchangeInfo.volume;
            }

            if (exchangeInfo.session_title == null)
            {
                _startedScreenController.ShowNoSession();
                _startedScreenController.ShowTechnologyPanel();
                _videoWindowController.ClearContent();
            }
            else
            {
                _startedScreenController.ShowSessionName(exchangeInfo.session_title);
                _startedScreenController.CloseTechnologyPanel();

                await _deviceStorageManager.CleanFolderExceptAsync(await GetContentToDownload(_domainPath + ContentToDownloadPath,
                    PlayerPrefs.GetString(GameGlobalConsts.DeviceTokenSaveKay)));
            }

            if (_popUpsController.IsContentShowingBlocked)
                return;

            if (exchangeInfo.session_title != null && _player == null && _mediaManagement?.action == null 
                //|| exchangeInfo.session_title != null && _player == null && _mediaManagement?.action == "stop upload"
                )
            {
                _videoWindowController.ClearContent();
                
                if (_appFsm.GetCurrentState().GetType() != typeof(ConnectToWiFiState))
                {
                    _appFsm.SetState<WaitingForContentState>();
                    return;
                }
            }

            if (_player != null)
            {
                _remoteContentContainer.CurrentDuration = _player.current_duration.ToInt32();

                if (_player.title != null)
                    _remoteContentContainer.VideoTitle = _player.title;

                if (_player.playback_state == null)
                {
                    _remoteContentContainer.VideoPlaybackState =
                        _videoPlaybackStatesDictionary["paused"];
                }
                else if (_player.playback_state == "playing")
                {
                    _remoteContentContainer.VideoPlaybackState =
                        _videoPlaybackStatesDictionary["playing"];
                }
                else if (_player.playback_state == "paused")
                {
                    _remoteContentContainer.VideoPlaybackState =
                        _videoPlaybackStatesDictionary["paused"];
                }

                _appFsm.SetState<VideoShowingState>();
                _videoWindowController.UpdateVideoData(_player);
            }

            if (_mediaManagement?.action != null)
            {
                _remoteContentContainer.VideoAction = _videoManagementActionsDictionary[_mediaManagement.action];

                if (_remoteContentContainer.VideoAction == VideoManagementAction.Download)
                {
                    StartDownloading();
                }
                
                if (_remoteContentContainer.VideoAction == VideoManagementAction.StopDownload)
                {
                    _videoWindowController.ClearContent();
                    if (_appFsm.GetCurrentState().GetType() != typeof(ConnectToWiFiState)) 
                        _appFsm.SetState<WaitingForContentState>();
                    
                    StopDownloading();
                }
                
                if (_player == null) 
                    return;
                
                if (_remoteContentContainer.VideoAction == VideoManagementAction.Rewind 
                    || _remoteContentContainer.VideoAction == VideoManagementAction.Sync)
                {
                    _videoWindowController.SyncVideo();
                }
            }
        }

        private void StartDownloading()
        {
            _contentIsDownloading = true;
            
            if (_appFsm.GetCurrentState().GetType() != typeof(ConnectToWiFiState))
                _appFsm.SetState<ContentDownloadState>();
        }

        private void StopDownloading()
        {
            if (!_contentIsDownloading)
                return;

            _contentIsDownloading = false;
            
            if (_appFsm.GetCurrentState().GetType() != typeof(ConnectToWiFiState))
                _appFsm.SetState<WaitingForContentState>();

            _videoDownloader.StopDownloading();
        }

        public async UniTask<VideoInfos> GetVideoToDownloadAsync()
        {
            try
            {
                var deviceToken = PlayerPrefs.GetString(GameGlobalConsts.DeviceTokenSaveKay);
                var videoInfos = new VideoInfos();

                videoInfos.VideoToDownloadInfos = await GetContentToDownload(_domainPath + ContentToDownloadPath,
                    deviceToken);

                Debug.Log("TestBuild Content to download");

                return videoInfos;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Операция отменена.");
                return default;
            }
        }

        private async UniTask<List<VideoInfo>> GetContentToDownload(string url, string deviceToken)
        {
            var request = new UnityWebRequest(url, "GET");
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("device_token", deviceToken);
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var videoToDownloadInfos = JsonUtility
                        .FromJson<Wrapper<VideoInfo>>(WrapArray(request.downloadHandler.text)).Items;
                    return videoToDownloadInfos;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Ошибка при обработке JSON: {ex.Message}");
                }
            }
            else
            {
                Debug.Log("Error: " + request.error);
                Debug.Log("Response Body: " + request.downloadHandler.text);
            }

            return default;
        }

        private string WrapArray(string jsonArray)
        {
            return "{\"Items\":" + jsonArray + "}";
        }

        private async UniTask<VerificationCodeData> GetGeneratedCode(string url, string deviceToken)
        {
            var request = new UnityWebRequest(url, "GET");
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("device_token", deviceToken);
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
                return JsonConvert.DeserializeObject<VerificationCodeData>(request.downloadHandler.text);
            else
                Debug.LogError("Error: " + request.error + "\nResponse Body: " + request.downloadHandler.text);

            return null;
        }

        private async UniTask<string> RegisterDeviceAsync(string serialNumber)
        {
            var deviceData = new DeviceData();

            deviceData.serial_number = serialNumber;
            deviceData.model = "DeviceModel";
            deviceData.charge = _androidDeviceHelper.GetBatteryLevel();
            deviceData.volume = _playerVolumeController.GetConvertedVolume();
            deviceData.free_memory = _deviceStorageManager.GetFreeSpaceInBytes();

            var jsonData = JsonConvert.SerializeObject(deviceData);

            return await PostDeviceRequest(_domainPath + PostDevicesPath, jsonData);
        }

        public async UniTask<ExchangeInfo> SendExchangeDataAsync()
        {
            var deviceToken = PlayerPrefs.GetString(GameGlobalConsts.DeviceTokenSaveKay);
            
            var deviceDataExchange = new DeviceDataExchange
            {
                settings = new Settings
                {
                    free_memory = _deviceStorageManager.GetFreeSpaceInBytes(),
                    charge = _androidDeviceHelper.GetBatteryLevel(),
                    volume = _playerVolumeController.GetConvertedVolume(true)
                },
                player = null,
                loading_info = null
            };
            
            if (_videoWindowController.MediaPlayer != null)
            {
                if (_remoteContentContainer.HasLoadedContentInMediaPlayer)
                    deviceDataExchange.player = new PostExchange.Player
                    {
                        current_duration = (int)_remoteContentContainer.CurrentDuration,
                        current_content_id = _videoWindowController.GetCurrentContentID().ToInt32(),

                        playback_state = _videoWindowController.CurrentVideoPlaybackState switch
                        {
                            VideoPlaybackStates.Playing => "playing",
                            VideoPlaybackStates.Paused => "paused",
                            VideoPlaybackStates.Stopped => "stopped",
                            _ => "unknown"
                        }
                    };
            }
    
            if (_videoDownloader.IsDownloading)
            {
                deviceDataExchange.loading_info = new LoadingInfo
                {
                    loading_content_id = _videoDownloader.CurrentDownloadContentID.ToInt32(),
                    last_content = _videoDownloader.IsLastContent,
                    loaded_bytes_of_total = _videoDownloader.DownloadedSize
                };
            }

            var jsonData = JsonConvert.SerializeObject(deviceDataExchange);
            
            Debug.Log($"json debug {_videoDownloader.IsDownloading} {jsonData}");
            return await PostExchangeRequest(_domainPath + ExchangeInfoPath, jsonData, deviceToken);
        }

        private async UniTask<string> PostDeviceRequest(string url, string jsonData)
        {
            var bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var deviceToken = JsonUtility.FromJson<DeviceToken>(request.downloadHandler.text);
                    return deviceToken.device_token;
                }
                else
                {
                    Debug.LogError("Error: " + request.error);
                    Debug.LogError("Response Body: " + request.downloadHandler.text);
                    return null;
                }
            }
        }

        private async UniTask<ExchangeInfo> PostExchangeRequest(string url, string jsonData, string deviceToken)
        {
            var bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            var request = new UnityWebRequest(url, "POST");

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("device_token", deviceToken);
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var exchangeInfo = JsonConvert.DeserializeObject<ExchangeInfo>(request.downloadHandler.text);

                Debug.Log(request.downloadHandler.text);

                if (string.IsNullOrEmpty(exchangeInfo.session_title))
                {
                    if (_popUpsController.IsContentShowingBlocked)
                        _popUpsController.UnblockDevice();

                    if (_appFsm.GetCurrentState().GetType() != typeof(ConnectToWiFiState))
                        _appFsm.SetState<StartedState>();
                    
                    await _deviceStorageManager.CleanDownloadFolderAsync();
                }

                return exchangeInfo;
            }

            _appFsm.SetState<StartedState>();
            
            Debug.Log("Error: " + request.error);
            Debug.Log("Response Body: " + request.downloadHandler.text);

            return default;
        }
    }
}