using UnityEngine;
using UnityEngine.EventSystems;

namespace UIManager.Windows.VideoWindow
{
    public class VideoSliderHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private VideoSliderController videoSliderController;
        
        public void OnPointerDown(PointerEventData eventData) => videoSliderController.SliderStartDrag();

        public void OnPointerUp(PointerEventData eventData)
        {
            videoSliderController.SetCurrentSliderValueManual();
            videoSliderController.SliderEndDrag();
        }
    }
}