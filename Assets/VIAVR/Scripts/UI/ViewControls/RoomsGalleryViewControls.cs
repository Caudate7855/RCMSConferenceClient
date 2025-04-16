using System;
using System.Linq;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using VIAVR.Scripts.Data;
using VIAVR.Scripts.UI.Activators;
using VIAVR.Scripts.UI.Buttons;
using VIAVR.Scripts.UI.Paginator;

namespace VIAVR.Scripts.UI.ViewControls
{
    public class RoomsGalleryViewControls : ViewControls<VrTourRegion>
    {
        public event Action OnBackClicked;
        public event Action<VrTourApartment> OnRoomOpened; 

        [InfoBox("Кнопку Button Back можно не ставить если она не предусмотрена")]
        [SerializeField] private ButtonBase _buttonBack;
        [SerializeField] private ButtonBase _nextPageButton;
        [SerializeField] private ButtonBase _prevPageButton;
    
        [SerializeField] private TextMeshProUGUI _pageTitle;

        [Header("Activator")]
        [SerializeField] private CanvasGroupFaderActivator _interactableCanvasGroupFader;
    
        [Header("Paginator")]
        [SerializeField] private Paginator.Paginator _videosPaginator;
        [FormerlySerializedAs("_hotelsContentProcessor")] [FormerlySerializedAs("_videosContentProcessor")] [SerializeField] private RoomsContentProcessor _roomsContentProcessor;
    
        private RoomPreviewElement[] _elements;

        public void SetTitle(string title)
        {
            _pageTitle.text = title;
        }
    
        public async void ShowHotelRooms(VrTourGroup hotel, Func<VrTourApartment, bool> optionalFilter = null)
        {
            SetTitle(hotel.Name);
        
            _elements = await _roomsContentProcessor.InitializePaginator(_videosPaginator, optionalFilter == null ? hotel.VrTours : hotel.VrTours.Where(optionalFilter));
        
            foreach (var element in _elements)
            {
                element.PlayButton.OnClick -= OpenRoom;
                element.PlayButton.OnClick += OpenRoom;
            }
        }
    
        public override void Activate(VrTourRegion provider, bool withAnimation = false)
        {
            _interactableCanvasGroupFader.Activate(true, withAnimation);

            if(_buttonBack != null)
                _buttonBack.OnClick += BackButtonHandler;
        
            _nextPageButton.OnClick += _videosPaginator.NextPage;
            _prevPageButton.OnClick += _videosPaginator.PrevPage;
        
            if(_elements == null) return;
        
            foreach (var element in _elements)
            {
                element.PlayButton.OnClick -= OpenRoom;
                element.PlayButton.OnClick += OpenRoom;
            }
        }

        public override void Deactivate(bool withAnimation = false)
        {
            _interactableCanvasGroupFader.Activate(false, withAnimation);
        
            if(_buttonBack != null)
                _buttonBack.OnClick -= BackButtonHandler;
        
            _nextPageButton.OnClick -= _videosPaginator.NextPage;
            _prevPageButton.OnClick -= _videosPaginator.PrevPage;

            if(_elements == null) return;
        
            foreach (var element in _elements)
                element.PlayButton.OnClick -= OpenRoom;
        }
    
        public override void UpdateLocalization()
        {
            if (_elements == null) return;
        
            foreach (var videoPreviewElement in _elements)
                videoPreviewElement.UpdateLocalization();
        }

        public void OnLoadedPreviewImage(string loadedPreviewImagePath)
        {
            if(_elements == null) return;

            foreach (var videoPreviewElement in _elements)
                videoPreviewElement.OnLoadedPreviewImage(loadedPreviewImagePath);
        }

        void BackButtonHandler()
        {
            OnBackClicked?.Invoke();
        }
    
        void OpenRoom(VrTourApartment roomData)
        {
            OnRoomOpened?.Invoke(roomData);
        }
    }
}