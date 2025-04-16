using RenderHeads.Media.AVProVideo;
using TMPro;
using UIManager.UISystem.Abstracts;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UIManager.Windows.VideoWindow
{
    public class VideoWindowView : UIViewBase
    {
        [SerializeField] private MediaPlayer _mediaPlayer;
        
        [SerializeField] private TMP_Text _videoTitle;
        
        [SerializeField] private TMP_Text _currentTime;
        [SerializeField] private TMP_Text _maxTime;

        [SerializeField] private Slider _videoSlider;
        
        [SerializeField] private ButtonBase _minusSoundButton;
        [SerializeField] private ButtonBase _plusSoundButton;
        
        [SerializeField] private ButtonBase _playButton;
        [SerializeField] private ButtonBase _pauseButton;
        
        [SerializeField] private ButtonBase _hideUIButton;

        [SerializeField] private GameObject _videoPlayerPartsContainer;
        
        [SerializeField] private Image[] _volumeImages;

        [SerializeField] private GameObject _videoSphere;
        [SerializeField] private DisplayUGUI _videoDisplay;
        [SerializeField] private Image _imagePanel;
        [SerializeField] private Image _imageMask;
        [SerializeField] private GameObject _playButtonLowAlpha;
        [SerializeField] private Image _imageSpherePanel;
        [SerializeField] private Image _imageSphereFader;
        
        public MediaPlayer MediaPlayer => _mediaPlayer;
        
        public TMP_Text VideoTitle => _videoTitle;
        
        public Slider VideoSlider => _videoSlider;
        
        public ButtonBase MinusSoundButton => _minusSoundButton;
        public ButtonBase PlusSoundButton => _plusSoundButton;
        
        public ButtonBase PauseButton => _pauseButton;
        public ButtonBase PlayButton => _playButton;
        
        public ButtonBase HideUIButton => _hideUIButton;
        
        public GameObject VideoPlayerPartsContainer => _videoPlayerPartsContainer;
        
        public Image[] VolumeImages => _volumeImages;

        public GameObject VideoSphere => _videoSphere;
        public DisplayUGUI VideoDisplay => _videoDisplay;
        public Image ImagePanel => _imagePanel;
        public Image ImageMask => _imageMask;

        public GameObject PlayButtonLowAlpha => _playButtonLowAlpha;

        public Image ImageSpherePanel => _imageSpherePanel;

        public Image ImageSphereFader => _imageSphereFader;
    }
}