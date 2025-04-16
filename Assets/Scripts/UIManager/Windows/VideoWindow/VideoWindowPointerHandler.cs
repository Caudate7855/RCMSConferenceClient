using UnityEngine;
using UnityEngine.EventSystems;

namespace UIManager.Windows.VideoWindow
{
    public class VideoWindowPointerHandler : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private bool IsControllerEnabled;
        
        private VideoWindowController _videoWindowController;

        private bool _isEntered;

        public void Initialize(VideoWindowController videoWindowController)
        {
            _videoWindowController = videoWindowController;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsControllerEnabled == false)
                return;
            
            if (_isEntered == false 
                && _videoWindowController.MediaPlayer.Info != null
                && _videoWindowController.MediaPlayer.Info.HasVideo())
            {
                _isEntered = true;
                _videoWindowController.ShowUI();
                _videoWindowController.CancelHideUIDelay();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsControllerEnabled == false)
                return;
            
            if (_videoWindowController.MediaPlayer.Info != null
                && _videoWindowController.MediaPlayer.Info.HasVideo())
            {
                _isEntered = false;
                _videoWindowController.StartHideInterfaceDelay();
            }
        }
    }
}