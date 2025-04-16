using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using RenderHeads.Media.AVProVideo;
using Services;
using Services.GetExchange;
using TMPro;
using UIManager.Enums;
using UIManager.UISystem.Abstracts;
using UIManager.UISystem.Attributes;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UIManager.Windows.VideoWindow
{
    [AssetAddress("VideoWindow"), UsedImplicitly]
    public class VideoWindowController : UIControllerBase<VideoWindowView>
    {
        private const float FadeDuration = 0.5f;
        private const float MaxImagePanelResolution = 1300f;

        public VideoPlaybackStates? CurrentVideoPlaybackState = VideoPlaybackStates.Stopped;
        public float StartupVolume = 3;
        public MediaPlayer MediaPlayer;
        public string CurrentContentID;

        [Inject] private DeviceStorageManager _deviceStorageManager;
        [Inject] private PopUpsController _popUpsController;

        private TMP_Text _videoTitle;
        private Slider _videoSlider;
        private ButtonBase _pauseButton;
        private ButtonBase _playButton;
        private ButtonBase _hideUIButton;
        private GameObject _videoPlayerPartsContainer;
        private GameObject _playButtonLowAlpha;
        private bool _isControllerShowing = true;
        private bool _isPausedManually;
        private VideoSliderController _videoSliderController;
        private VideoWindowPointerHandler _videoWindowPointerHandler;
        private CancellationTokenSource _cancellationTokenSource;
        private RemoteContentContainer _remoteContainer;
        private bool _isPossibleUnpauseManually;
        private MediaPath _currentMediaPath;
        private Image[] _volumeImages;
        private GameObject _videoSphere;
        private DisplayUGUI _videoDisplay;
        private ApplyToMaterial _applyToMaterialComponent;
        private Room _roomObject;
        private Image _imagePanel;
        private Image _imageMask;
        private Image _imageSpherePanel;
        private Image _imageSphereFader;
        private Texture2D _oldTexture;

        public VideoWindowController(RemoteContentContainer remoteContainer)
        {
            _remoteContainer = remoteContainer;
        }

        protected override void Initialize()
        {
            _roomObject = Object.FindObjectOfType<Room>();

            InitializeViewProperties();
            InitializeButtons();

            _videoSphere = View.VideoSphere;
            _videoDisplay = View.VideoDisplay;
            _imagePanel = View.ImagePanel;
            _imageMask = View.ImageMask;

            _applyToMaterialComponent = View.gameObject.GetComponentInChildren<ApplyToMaterial>();

            MediaPlayer.Events.AddListener(OnMediaPlayerEventHandler);

            _videoSliderController = View.GetComponent<VideoSliderController>();
            _videoWindowPointerHandler = View.GetComponent<VideoWindowPointerHandler>();

            _volumeImages = View.VolumeImages;
            _playButtonLowAlpha = View.PlayButtonLowAlpha;
            
            _imageSpherePanel = View.ImageSpherePanel;
            _imageSphereFader = View.ImageSphereFader;
            
            _imageSpherePanel.DOFade(0, 0);

            _videoWindowPointerHandler.Initialize(this);

            _videoSlider.onValueChanged.AddListener(delegate { SeekVideo(); });

            HideUI();
        }

        protected override void OnClose()
        {
            _imageSpherePanel.DOFade(0, 0);
            _imageSpherePanel.sprite = null;
        }

        private void InitializeViewProperties()
        {
            MediaPlayer = View.MediaPlayer;

            _videoTitle = View.VideoTitle;
            _videoSlider = View.VideoSlider;

            _pauseButton = View.PauseButton;
            _playButton = View.PlayButton;

            _hideUIButton = View.HideUIButton;

            _videoPlayerPartsContainer = View.VideoPlayerPartsContainer;
        }

        private void InitializeButtons()
        {
            _hideUIButton.OnClick += HideUI;

            InitializePausePlayButtons();
        }

        private void InitializePausePlayButtons()
        {
            _pauseButton.OnClick += () => ChangeVideoState(false);
            _playButton.OnClick += () => ChangeVideoState(true);

            var images = _pauseButton.GetComponentsInChildren<Image>();

            foreach (var image in images) 
                image.DOFade(0, 0f);
        }

        private void ChangeDisplayType(DisplayType displayType)
        {
            switch (displayType)
            {
                case DisplayType.Flat:
                    _imagePanel.gameObject.SetActive(false);
                    _imageMask.gameObject.SetActive(false);
                    _applyToMaterialComponent.enabled = false;
                    _videoSphere.gameObject.SetActive(false);
                    _videoDisplay.gameObject.SetActive(true);
                    _roomObject.gameObject.SetActive(true);
                    _imageSpherePanel.gameObject.SetActive(false);
                    break;

                case DisplayType.Sphere:
                    _imagePanel.gameObject.SetActive(false);
                    _imageMask.gameObject.SetActive(false);
                    _videoDisplay.gameObject.SetActive(false);
                    _videoSphere.gameObject.SetActive(true);
                    _applyToMaterialComponent.enabled = true;
                    _roomObject.gameObject.SetActive(false);
                    _imageSpherePanel.gameObject.SetActive(false);
                    break;

                case DisplayType.Image:
                    _imagePanel.gameObject.SetActive(true);
                    _imageMask.gameObject.SetActive(true);
                    _videoDisplay.gameObject.SetActive(false);
                    _videoSphere.gameObject.SetActive(false);
                    _applyToMaterialComponent.enabled = false;
                    _roomObject.gameObject.SetActive(true);
                    _imageSpherePanel.gameObject.SetActive(false);
                    break;
                case DisplayType.SphereImage:
                    _imagePanel.gameObject.SetActive(false);
                    _imageMask.gameObject.SetActive(false);
                    _videoDisplay.gameObject.SetActive(false);
                    
                    
                    break;
            }
        }

        public void ClearContent()
        {
            _imagePanel.sprite = default;
            
            foreach (var materials in _applyToMaterialComponent.Materials) 
                materials.mainTexture = _applyToMaterialComponent.DefaultTexture;
            
            MediaPlayer?.Control?.Stop();
            MediaPlayer?.Control?.CloseMedia();
            _videoDisplay?.Player?.Control?.Stop();

            _remoteContainer.VideoTitle = null;
            _remoteContainer.HasLoadedContentInMediaPlayer = false;

        }

        public void ChangeVolumeSprite(float volumeValue)
        {
            int spriteIndex = 0;

            if (volumeValue <= 0.25)
                spriteIndex = 0;
            else if (volumeValue >= 0.25 && volumeValue <= 0.50)
                spriteIndex = 1;
            else if (volumeValue >= 0.50 && volumeValue <= 0.75)
                spriteIndex = 2;
            else if (volumeValue >= 0.75 && volumeValue <= 1) 
                spriteIndex = 3;

            foreach (var volumeImage in _volumeImages) 
                volumeImage.gameObject.SetActive(false);

            _volumeImages[spriteIndex].gameObject.SetActive(true);
        }

        private void OnMediaPlayerEventHandler(MediaPlayer mp, MediaPlayerEvent.EventType eventType,
            ErrorCode errorCode)
        {
            if (eventType == MediaPlayerEvent.EventType.Started)
            {
                _videoSlider.maxValue = (float)MediaPlayer.Info.GetDuration();
                _videoSliderController.Initialize();
            }
        }

        public void ShowUI()
        {
            if (_isControllerShowing == false) 
                ChangeControllerVisibility(true);
        }

        private void DisableUI() => _videoPlayerPartsContainer.gameObject.SetActive(false);

        private void HideUI() => ChangeControllerVisibility(false);

        private async UniTask DelayHideUI(CancellationToken cancellationToken)
        {
            await UniTask.Delay(5000, cancellationToken: cancellationToken);
            HideUI();
        }

        public void StartHideInterfaceDelay()
        {
            CancelHideUIDelay();

            _cancellationTokenSource = new CancellationTokenSource();

            DelayHideUI(_cancellationTokenSource.Token).Forget();
        }

        public void CancelHideUIDelay()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void SeekVideo()
        {
            if (_videoSliderController.IsDragging) 
                MediaPlayer.Control.Seek(_videoSlider.value);
        }

        private void UnpauseContentRemote()
        {
            if (MediaPlayer != null
                && (MediaPlayer.gameObject.activeSelf && MediaPlayer.Info != null)
                && MediaPlayer.Info.HasVideo())
            {
                if (MediaPlayer.Control.IsPlaying())
                    return;

                _isPossibleUnpauseManually = true;

                SyncVideo();
                MediaPlayer.Play();

                _playButton.gameObject.SetActive(false);
                _pauseButton.gameObject.SetActive(true);
                _playButtonLowAlpha.SetActive(false);
            }
        }

        private void PauseContentRemote()
        {
            if (MediaPlayer != null
                && (MediaPlayer.gameObject.activeSelf && MediaPlayer.Info != null)
                && MediaPlayer.Info.HasVideo())
            {
                if (MediaPlayer.Control.IsPaused())
                    return;

                _isPossibleUnpauseManually = false;
                MediaPlayer.Pause();
                SyncVideo();

                _playButton.gameObject.SetActive(false);
                _pauseButton.gameObject.SetActive(false);
                _playButtonLowAlpha.SetActive(true);
            }
        }

        private void ChangeVideoState(bool condition)
        {
            if (_isPossibleUnpauseManually == false)
                return;

            if (condition)
            {
                SyncVideo();

                MediaPlayer.Control.Play();

                _pauseButton.gameObject.SetActive(true);
                _playButton.gameObject.SetActive(false);

                _isPausedManually = false;
            }
            else
            {
                MediaPlayer.Control.Pause();

                _pauseButton.gameObject.SetActive(false);
                _playButton.gameObject.SetActive(true);

                _isPausedManually = true;
            }
        }

        private void ChangeControllerVisibility(bool condition)
        {
            if (condition) 
                _videoPlayerPartsContainer.gameObject.SetActive(true);

            _isControllerShowing = condition;

            var fadeSequence = DOTween.Sequence();

            var texts = _videoPlayerPartsContainer.GetComponentsInChildren<TMP_Text>();
            var images = _videoPlayerPartsContainer.GetComponentsInChildren<Image>();

            var conditionValue = condition ? 1 : 0;

            for (int i = 0; i < texts.Length; i++) 
                fadeSequence.Join(texts[i].DOFade(conditionValue, FadeDuration));

            for (int i = 0; i < images.Length; i++) 
                fadeSequence.Join(images[i].DOFade(conditionValue, FadeDuration));

            if (condition == false) 
                fadeSequence.OnComplete(DisableUI);
        }

        public void UpdateVideoData(Player player)
        {
            if (!LoadContent(player))
            {
                _popUpsController.ShowContentErrorPopUp();
                return;
            }

            UpdateVideoTitle();
            UpdateVideoState();
            
            _remoteContainer.HasLoadedContentInMediaPlayer = true;
        }

        public void SyncVideo()
        {
            Debug.Log("Start sync video");
            if (_remoteContainer.CurrentDuration != null)
            {
                Debug.Log($"Sync Video on {_remoteContainer.CurrentDuration}");

                var seekTiming = (double)_remoteContainer.CurrentDuration;
                MediaPlayer.Control.Seek(seekTiming / 1000);
            }
        }

        private void UpdateVideoTitle() => _videoTitle.SetText(_remoteContainer.VideoTitle);

        public void UpdateVideoState() => ChangeVideoPlaybackState(_remoteContainer.VideoPlaybackState);

        private bool LoadContent(Player player)
        {
            if (player.title == null)
            {
                MediaPlayer.Stop();

                return false;
            }

            var pictureFile = _deviceStorageManager.ContentLocation + player.title + ".png";
            var videoFile = _deviceStorageManager.ContentLocation + player.title + ".mp4";

            CurrentContentID = player.id;
            
            if (!File.Exists(videoFile) && !File.Exists(pictureFile))
                return false;

            var mediaPath = new MediaPath(videoFile, MediaPathType.AbsolutePathOrURL);

            ChangeContentView(player);
            
            if (_currentMediaPath == mediaPath)
                return true;

            OpenMedia(player, mediaPath);
            
            _currentMediaPath = mediaPath;

            return true;
        }

        public void ResetContentData() => _currentMediaPath = null;

        private void ChangeContentView(Player player)
        {
            switch (player.format)
            {
                case "2D":
                    ChangeDisplayType(DisplayType.Flat);
                    HideUI();
                    break;
                case "3D":
                    ChangeDisplayType(DisplayType.Sphere);
                    HideUI();
                    break;
                case "Image":
                    HideUI();
                    ChangeDisplayType(DisplayType.Image);
                    break;
                case "Image 360":
                    HideUI();
                    ChangeDisplayType(DisplayType.SphereImage);
                    break;
            }
        }

        private async void OpenMedia(Player player, MediaPath mediaPath)
        {
            if (player.format == "Image")
            {
                MediaPlayer.gameObject.SetActive(false);
                MediaPlayer.CloseMedia();
                _imagePanel.sprite = LoadImageFromFile(player.title, _imagePanel);
                Debug.Log("Try opening picture file path:  " + mediaPath);
            }
            else if (player.format == "Image 360")
            {
                
                _imageSphereFader.gameObject.SetActive(true);
                await _imageSphereFader.DOFade(1, 1f).AsyncWaitForCompletion();
                _roomObject.gameObject.SetActive(false);
                await _imageSpherePanel.DOFade(1, 0).AsyncWaitForCompletion();
                await UniTask.Delay(500);
                _videoSphere.gameObject.SetActive(false);
                _applyToMaterialComponent.enabled = false;
                MediaPlayer.gameObject.SetActive(false);
                MediaPlayer.CloseMedia();
                _imageSpherePanel.gameObject.SetActive(true);
                _imageSpherePanel.sprite = LoadImageFromFile(player.title, _imageSpherePanel);
                _imageSphereFader.DOFade(0, 1f).OnComplete(() => _imageSphereFader.gameObject.SetActive(false));
                
            }
            else
            {
                Debug.Log("Try opening video file path:  " + mediaPath);
                MediaPlayer.gameObject.SetActive(true);
                MediaPlayer.OpenMedia(mediaPath);
            }
        }


        private Sprite LoadImageFromFile(string fileName, Image image)
        {
            var path = $"{_deviceStorageManager.ContentLocation + fileName}.png";

            if (File.Exists(path))
            {
                var imageData = File.ReadAllBytes(path);
                if (_oldTexture != null)
                {
                    Object.Destroy(_oldTexture);
                    _oldTexture = null;
                }
                _oldTexture = new Texture2D(1, 1);

                if (_oldTexture.LoadImage(imageData))
                {
                    float aspectRatio = (float)_oldTexture.width / _oldTexture.height;

                    float width, height;

                    if (aspectRatio > 1)
                    {
                        width = MaxImagePanelResolution;
                        height = MaxImagePanelResolution / aspectRatio;
                    }
                    else
                    {
                        height = MaxImagePanelResolution;
                        width = MaxImagePanelResolution * aspectRatio;
                    }


                    if (image != null)
                    {
                        var rectTransform = image.rectTransform;
                        rectTransform.sizeDelta = new Vector2(width, height);
                        _imageMask.rectTransform.sizeDelta = new Vector2(width, height);
                    }

                    return Sprite.Create(_oldTexture, new Rect(0, 0, _oldTexture.width, _oldTexture.height),
                        new Vector2(0.5f, 0.5f));
                }
            }
            else
            {
                Debug.LogError($"Файл не найден: {path}");
            }

            return default;
        }

        public string GetCurrentContentID()
        {
            if (string.IsNullOrEmpty(CurrentContentID)) 
                Debug.LogError("Current content id is NULL");

            return CurrentContentID;
        }

        private void ChangeVideoPlaybackState(VideoPlaybackStates? newVideoPlaybackStates)
        {
            switch (newVideoPlaybackStates)
            {
                case VideoPlaybackStates.Paused:
                    PauseContentRemote();
                    break;

                case VideoPlaybackStates.Playing:
                    UnpauseContentRemote();
                    break;

                case VideoPlaybackStates.Stopped:
                    PauseContentRemote();
                    break;
            }
        }
    }
}