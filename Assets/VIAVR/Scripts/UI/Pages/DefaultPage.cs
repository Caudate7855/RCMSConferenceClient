using System;
using UnityEngine;
using VIAVR.Scripts.Data;
using VIAVR.Scripts.UI.ViewControls;

namespace VIAVR.Scripts.UI.Pages
{
    public class DefaultPage : PageBase
    {
        public enum ViewMode { NONE, MAP, ROOMS, ROOM_PREVIEW }
    
        [SerializeField] protected ConfigProviderViewControlsGroup _viewControlsGroup;

        [SerializeField] private HotelsMapViewControls _hotelsMapViewControls;
        [SerializeField] private RoomsGalleryViewControls _roomsGalleryViewControls;
        [SerializeField] private RoomPreviewViewControls _roomPreviewViewControls;

        private VrTourRegion _vrTourRegion;

        private ViewMode _currentViewMode = ViewMode.NONE;
    
        public override bool InitializePage()
        {
            if(base.InitializePage()) return PAGE_INITIALIZED;

            _vrTourRegion = _appCore.VrTourRegion;

            _hotelsMapViewControls.OnHotelClicked += hotelData =>
            {
                SetViewMode(ViewMode.ROOMS);
            
                _roomsGalleryViewControls.ShowHotelRooms(hotelData);
            };

            _roomsGalleryViewControls.OnBackClicked += () =>
            {
                SetViewMode(ViewMode.MAP);
            };
        
            _roomsGalleryViewControls.OnRoomOpened += roomData =>
            {
                SetViewMode(ViewMode.ROOM_PREVIEW);
            
                _roomPreviewViewControls.ShowRoom(roomData);
            };
        
            _roomPreviewViewControls.OnBackClicked += () =>
            {
                SetViewMode(ViewMode.ROOMS);
            };

            _roomPreviewViewControls.OnRoomViewClicked += roomData =>
            {
                _appCore.StartVrTour(roomData);
            };
        
            _roomPreviewViewControls.OnRoomBookClicked += roomData =>
            {
                // TODO
            };
        
            SetViewMode(ViewMode.MAP);
        
            return PAGE_CONTINUE_INITIALIZATION;
        }

        private void SetViewMode(ViewMode mode, bool withAnimation = true)
        {
            if(_currentViewMode == mode) return;
        
            _currentViewMode = mode;
        
            switch (mode)
            {
                case ViewMode.MAP:
                    _viewControlsGroup.ActivateElement(_hotelsMapViewControls, _vrTourRegion, withAnimation);
                    break;
            
                case ViewMode.ROOMS:
                    _viewControlsGroup.ActivateElement(_roomsGalleryViewControls, _vrTourRegion, withAnimation);
                    break;

                case ViewMode.ROOM_PREVIEW:
                    _viewControlsGroup.ActivateElement(_roomPreviewViewControls, _vrTourRegion, withAnimation);
                    break;
            
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}