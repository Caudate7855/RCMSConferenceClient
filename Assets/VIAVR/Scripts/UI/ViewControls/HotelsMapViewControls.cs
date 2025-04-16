using System;
using UnityEngine;
using UnityEngine.UI;
using VIAVR.Scripts.Core;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.Data;
using VIAVR.Scripts.UI.Activators;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI.ViewControls
{
    public class HotelsMapViewControls : ViewControls<VrTourRegion>
    {
        public event Action<VrTourGroup> OnHotelClicked; 
    
        [SerializeField] private RawImage _mapRawImage;
        [SerializeField] private GameObject _hotelButtonPrefab;

        [SerializeField] private Transform _hotelButtonsContainer;
    
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

        private bool _mapInitialized;
    
        public override async void Activate(VrTourRegion data, bool withAnimation)
        {
            _interactableCanvasGroupFader.Activate(true, withAnimation);
        
            if(_mapInitialized) return;

            _mapRawImage.texture = await GraphicsLoaderCache.LoadTexture2DAsync(data.MapImage, TextureFormat.RGB565) ;

            foreach (var hotel in data.VrTourGroups)
            {
                var button = Instantiate(_hotelButtonPrefab, _hotelButtonsContainer);
            
                button.transform.localScale = Vector3.one;
                button.transform.localPosition = new Vector3(hotel.MapPosition.x, -hotel.MapPosition.y, 0);

                var buttonBase = button.GetComponentInChildren<ButtonBaseHotelData>();

                if (buttonBase)
                {
                    buttonBase.SetData(hotel);
                    buttonBase.OnClick += hotelData =>
                    {
                        OnHotelClicked?.Invoke(hotelData);
                    };
                }
            }
        
            _mapInitialized = true;
        }

        public override void Deactivate(bool withAnimation)
        {
            _interactableCanvasGroupFader.Activate(false, withAnimation);
        }

        public override void UpdateLocalization()
        {
            //
        }
    }
}