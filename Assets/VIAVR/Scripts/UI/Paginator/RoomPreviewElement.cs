using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VIAVR.Scripts.Core;
using VIAVR.Scripts.Core.SerializableDictionary;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.Data;
using VIAVR.Scripts.UI.Buttons;
using VIAVR.Scripts.UI.Hover;
using VIAVR.Scripts.UI.Loader;

// скрипт для префаба превьюхи, для интерфейса указываем структуру содержащую данные для загрузки превьюхи
namespace VIAVR.Scripts.UI.Paginator
{
    public class RoomPreviewElement : MonoBehaviour, IPaginableElement<VrTourApartment>, ILoaderAnimationAttachable
    {
        // (лоадер)
        private static Vector3 LoaderSize = new Vector3(.5f, .5f, 1);
        // (лоадер)
        #region ILoaderAnimationAttachable

        public Transform Transform => _previewImage.transform.parent;
        public event Action<ILoaderAnimationAttachable, bool> OnRemoveLoaderRequest;

        #endregion
    
        public ButtonBaseData<VrTourApartment> PlayButton => AppCore.UsingNoControllerMode && _separatePlayButton != null ? _separatePlayButton : _button;

        [SerializeField] private TextureFormat _textureFormat = TextureFormat.RGBA32;
        [SerializeField] private int _maxPreviewImageSidePixels = 256;
        [SerializeField] private bool _forceClearTextures = false;

        [HorizontalLine]
        [SerializeField] private SerializableDictionary<string, Color> _colorByRoomMode;
        [SerializeField] private string _modeSuffix;
    
        [HorizontalLine]
        [SerializeField] private ButtonBaseRoomData _button;
        [SerializeField] private ButtonBaseRoomData _separatePlayButton;
        [SerializeField] private GameObject _separateButtonGameObject;

        [SerializeField] private RawImage _previewImage;
        [SerializeField] private RectTransform _previewImageTransfom;
    
        [SerializeField] private HoverPreviewItem _hoverPreviewItem;

        [SerializeField] private TextMeshProUGUI _videoTitleText;
        [SerializeField] private TextMeshProUGUI _roomModeText;

        [SerializeField] private bool _previewDynamicLoading;

        private string _previewImagePath = null;

        // (лоадер)
        private LoaderAnimation _loader;
    
        private GraphicsLoaderCache _graphicsLoaderCache;
        public GraphicsLoaderCache GraphicsLoaderCache
        {
            get
            {
                if (_graphicsLoaderCache == null)
                    _graphicsLoaderCache = Singleton<GraphicsLoaderCache>.Instance;
            
                return _graphicsLoaderCache;
            }
        }
    
        private AppCore _appCore;
        private AppCore AppCore
        {
            get
            {
                if (_appCore == null)
                    _appCore = Singleton<AppCore>.Instance;
            
                return _appCore;
            }
        }
    
        public VrTourApartment Data { get; set; }

        public async UniTask SetupFromData(VrTourApartment data)
        {
            // (лоадер)
            if (_loader != null)
            {
                OnRemoveLoaderRequest?.Invoke(this, false);
                _loader = null;
            }
        
            Data = _button.Data = data;

            if (_separatePlayButton != null)
                _separatePlayButton.Data = data;

            if (AppCore.UsingNoControllerMode && _separatePlayButton != null && _separateButtonGameObject != null)
            {
                _separateButtonGameObject.SetActive(true);
                _hoverPreviewItem.EnablePlayIcon(false);

                // TODO Explicit не обрабатывается в рейкасте (ControllersHandler.cs)
                var buttonNavigation = _button.Button.navigation;
                buttonNavigation.mode = Navigation.Mode.Explicit;
                _button.Button.navigation = buttonNavigation;
            }
            else
            {
                if(_hoverPreviewItem != null)
                    _hoverPreviewItem.EnablePlayIcon(true);
            }
        
            _videoTitleText.text = !string.IsNullOrEmpty(data.Name) ? data.Name : "<NO NAME>";
            _roomModeText.text = data.InfoStatus.FirstCharToUpper() + _modeSuffix;
        
            if (_colorByRoomMode != null && _colorByRoomMode.TryGetValue(data.InfoStatus.ToLower(), out var color))
                _roomModeText.color = color;

            /*_previewImagePath = data.PreviewImage;

        var texture = await GraphicsLoaderCache.LoadTexture2DAsync(_previewImagePath, _textureFormat, _maxPreviewImageSidePixels, _previewDynamicLoading/* && !Data.DynamicImageLoaded#1#);*/

            if(_forceClearTextures && _previewImage.texture != null)
                Destroy(_previewImage.texture);

            _previewImage.texture = await GraphicsLoaderCache.LoadTexture2DAsync(data.MainPreview, _textureFormat, _maxPreviewImageSidePixels);

            if (_previewImage.texture == null)
            {
                if (_previewDynamicLoading)
                {
                    _previewImage.texture = GraphicsLoaderCache.GetLoadingPlaceholderTexture();
                
                    // (лоадер)
                    _loader = Singleton<LoaderBuilder>.Instance.AttachLoader(
                        this, LoaderAnimation.BackgroundTransparency.TRANSPARENT, LoaderSize);
                
                    _loader.transform.SetSiblingIndex(1);
                }
                else
                {
                    Debug.LogError($"Preview texture [{_previewImagePath}] == null", this);
                    _previewImage.enabled = false;
                }
            
                return;
            }
        
            _previewImage.enabled = true;
        
            _previewImage.FitInContainerHeight();
        }

        // оптимизация: во время слайдинга отключать некоторые элементы, т.к. CurvedUI при перемещении страницы перестраивает меши элементов что жрет фпс
        // больше всего фпс жрет текст с искривлением
        public void ChangePageAnimationStarted(float duration)
        {
            if(!gameObject.activeSelf) return;
        
            _videoTitleText.gameObject.SetActive(false);
            // (лоадер)
            if(_loader != null && !_loader.Removing) _loader.gameObject.SetActive(false);
        }

        public void ChangePageAnimationFinished()
        {
            if(!gameObject.activeSelf) return;
        
            _videoTitleText.gameObject.SetActive(true);

            var c = _videoTitleText.color; c.a = 0; _videoTitleText.color = c;
            _videoTitleText.SetMaterialDirty(); // иначе текст "мигает" при старте анимации фейда

            _videoTitleText.DOFade(1f, 0.5f);
        
            // (лоадер)
            if(_loader != null && !_loader.Removing) _loader.gameObject.SetActive(true);
        }
    
        public void UpdateLocalization()
        {
            if(Data != null)
                _videoTitleText.text = !string.IsNullOrEmpty(Data.Name) ? Data.Name : "<NO NAME>";
        }

        public async void OnLoadedPreviewImage(string loadedPreviewImagePath, bool force = false)
        {
            if(!_previewDynamicLoading) return;
        
            if (!force && (string.IsNullOrEmpty(_previewImagePath) || !_previewImagePath.Contains(loadedPreviewImagePath))) return;
        
            var texture = await GraphicsLoaderCache.LoadTexture2DAsync(_previewImagePath, _textureFormat, _maxPreviewImageSidePixels);
        
            if(_previewImage == null) return;
            _previewImage.texture = texture;
        
            _previewImage.FitInContainerHeight();
        
            // (лоадер)
            OnRemoveLoaderRequest?.Invoke(this, true);
        }

        private void OnEnable() // ну теперь то FitInContainerHeight всегда срабатывает нормально
        {
            if(_loader == null || _loader.Removing)
                _previewImage.FitInContainerHeight();
        }
    }
}