using RenderHeads.Media.AVProVideo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager.Windows.VideoWindow
{
    public class VideoSliderController : MonoBehaviour
    {
        public bool IsDragging;

        [SerializeField] private MediaPlayer _mediaPlayer;
        [SerializeField] private Slider _videoSlider;
        [SerializeField] private TMP_Text _currentTimeText;
        [SerializeField] private TMP_Text _maxTimeText;
        
        private bool _isInitialized;
        public void Initialize()
        {
            SetMaxTime();
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized)
                return;
            
            SetCurrentTime();
            SetCurrentSliderValue();
        }

        public void SliderStartDrag() => IsDragging = true;

        public void SliderEndDrag() => IsDragging = false;

        public void SetCurrentSliderValueManual() => _mediaPlayer.Control.Seek(_videoSlider.value);

        private void SetCurrentSliderValue()
        {
            if (!IsDragging) 
                _videoSlider.value = (float)_mediaPlayer.Control.GetCurrentTime();
        }

        private void SetCurrentTime() => _currentTimeText.SetText(CalculateCurrentTime());

        private void SetMaxTime() => _maxTimeText.SetText(CalculateMaxTime());

        private string CalculateCurrentTime()
        {
            var updatedCurrentTime = (int)_videoSlider.value;
            var updatedCurrentTimeMinutes = updatedCurrentTime / 60;
            var updatedCurrentTimeSeconds = updatedCurrentTime % 60;
            
            var newTimeText = $"{updatedCurrentTimeMinutes}:{updatedCurrentTimeSeconds}";
            
            if (updatedCurrentTimeSeconds < 10) 
                newTimeText = $"{updatedCurrentTimeMinutes}:0{updatedCurrentTimeSeconds}";
            
            return newTimeText;
        } 
        
        
        private string CalculateMaxTime()
        {
            var updatedMaxTime = (int)_mediaPlayer.Info.GetDuration();
            var updatedMaxTimeMinutes = updatedMaxTime / 60;
            var updatedMaxTimeSeconds = updatedMaxTime % 60;
            
            var newTimeText = $"{updatedMaxTimeMinutes}:{updatedMaxTimeSeconds}";
            
            if (updatedMaxTimeSeconds < 10) 
                newTimeText = $"{updatedMaxTimeMinutes}:0{updatedMaxTimeSeconds}";
            
            return newTimeText;
        }
    }
}