using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VIAVR.Scripts.Core;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.Data;
using VIAVR.Scripts.UI.Activators;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI.ViewControls
{
    public class RoomPreviewViewControls : ViewControls<VrTourRegion>
    {
        public event Action OnBackClicked;
        public event Action<VrTourApartment> OnRoomViewClicked;
        public event Action<VrTourApartment> OnRoomBookClicked;
    
        [SerializeField] private ButtonBase _buttonBack;
        [SerializeField] private ButtonBase _buttonViewApartment;
        [SerializeField] private ButtonBase _buttonBookApartment;

        [SerializeField] private Texture2D _defaultPreviewTexture;
    
        [SerializeField] private RawImage _imagePreviewBig;
        [SerializeField] private RawImage _imagePreviewBig2ndLayer;
        [SerializeField] private CanvasGroupFaderActivator _imagePreviewActivator;
    
        [SerializeField] private RawImage[] _imagePreviewsMini;
        [SerializeField] private CanvasGroupFaderActivator[] _imagePreviewsMiniActivators;
        [SerializeField] private ButtonBase[] _previewsMiniButtons;

        [SerializeField] private string _modeSuffix;
        [SerializeField] private TextMeshProUGUI _roomTitleText;
        [SerializeField] private TextMeshProUGUI _roomModeText;
        [SerializeField] private TextMeshProUGUI _roomInfoHeaderText;
        [SerializeField] private TextMeshProUGUI _roomInfoText;
    
        [Header("Activator")]
        [SerializeField] private CanvasGroupFaderActivator _interactableCanvasGroupFader;

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
    
        private VrTourApartment _currentRoomData;

        public async void ShowRoom(VrTourApartment room)
        {
            _currentRoomData = room;

            _imagePreviewBig2ndLayer.texture = _defaultPreviewTexture;
            _imagePreviewActivator.Activate(false, false);

            foreach (var miniActivator in _imagePreviewsMiniActivators)
                miniActivator.Activate(false, false);

            if (room.InfoPreviewImages != null && room.InfoPreviewImages.Length > 0)
            {
                for (int i = 0; i < _imagePreviewsMini.Length; i++)
                {
                    _previewsMiniButtons[i].gameObject.SetActive(i < room.InfoPreviewImages.Length);
            
                    if(i >= room.InfoPreviewImages.Length) continue;

                    _imagePreviewsMini[i].texture = await GraphicsLoaderCache.LoadTexture2DAsync(room.InfoPreviewImages[i], TextureFormat.RGB565);
                
                    _imagePreviewsMiniActivators[i].Activate(true, true);
                }
            
                _imagePreviewBig.texture = await GraphicsLoaderCache.LoadTexture2DAsync(room.InfoPreviewImages[0], TextureFormat.RGB565);
            
                _imagePreviewActivator.Activate(true, true);
            }

            _roomTitleText.text = room.Name;
            _roomInfoHeaderText.text = room.InfoHeader;
            _roomInfoText.text = room.InfoDescription;
            _roomModeText.text = room.InfoStatus.FirstCharToUpper() + _modeSuffix;
        }
    
        public override void Activate(VrTourRegion data, bool withAnimation)
        {
            _interactableCanvasGroupFader.Activate(true, withAnimation);
        
            if(_buttonBack != null)
                _buttonBack.OnClick += BackButtonHandler;
        
            _buttonViewApartment.OnClick += ButtonViewApartmentClickHandler;
            _buttonBookApartment.OnClick += ButtonBookApartmentClickHandler;

            if (!Initialized)
            {
                for (int i = 0; i < _previewsMiniButtons.Length; i++)
                {
                    var previewIndex = i;
                
                    _previewsMiniButtons[i].OnClick += async () =>
                    {
                        if (_currentRoomData != null)
                        {
                            _imagePreviewBig2ndLayer.texture = _imagePreviewBig.texture;
                        
                            await _imagePreviewActivator.ActivateAsync(false, false);
                        
                            _imagePreviewBig.texture = await GraphicsLoaderCache.LoadTexture2DAsync(_currentRoomData.InfoPreviewImages[previewIndex], TextureFormat.RGB565);
                        
                            await _imagePreviewActivator.ActivateAsync(true, true);
                        }
                    };
                }

                Initialized = true;
            }
        }
    
        public override void Deactivate(bool withAnimation)
        {
            _interactableCanvasGroupFader.Activate(false, withAnimation);
        
            if(_buttonBack != null)
                _buttonBack.OnClick -= BackButtonHandler;
        
            _buttonViewApartment.OnClick -= ButtonViewApartmentClickHandler;
            _buttonBookApartment.OnClick -= ButtonBookApartmentClickHandler;
        }

        public override void UpdateLocalization()
        {
            //
        }
    
        private void ButtonViewApartmentClickHandler()
        {
            OnRoomViewClicked?.Invoke(_currentRoomData);
        }
    
        private void ButtonBookApartmentClickHandler()
        {
            OnRoomBookClicked?.Invoke(_currentRoomData);
        }
    
        void BackButtonHandler()
        {
            OnBackClicked?.Invoke();
        }
    }
}