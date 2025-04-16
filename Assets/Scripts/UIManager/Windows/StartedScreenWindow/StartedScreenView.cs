using System;
using System.Collections.Generic;
using TMPro;
using UIManager.UISystem.Abstracts;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager.Windows
{
    public class StartedScreenView : UIViewBase
    {
        [SerializeField] private ButtonBase _russianLanguageButton;
        [SerializeField] private ButtonBase _englishLanguageButton;

        [SerializeField] private TMP_Text _tokenValue;

        [SerializeField] private GameObject _russianFlagOutline;
        [SerializeField] private GameObject englishFlagOutline;

        [SerializeField] private TMP_Text _controllerChargeValue;
        [SerializeField] private TMP_Text _deviceChargeValue;
        [SerializeField] private TMP_Text _wifiConnectionValue;
        [SerializeField] private TMP_Text _sessionTitle;
        
        [SerializeField] private ButtonBase _wifiButton;

        [SerializeField] private DownloadInfoWindow downloadContentWindow;

        [SerializeField] private Slider _headsetChargeSlider;
        [SerializeField] private Slider _controllerChargeSlider;
        [SerializeField] private List<Sprite> wifiSignalStrengthSprites;
        [SerializeField] private Image _wifiSignalImage; 
        [SerializeField] private GameObject _chargeControllerGameObject;
        [SerializeField] private GameObject _waitingForContentWindow;
        [SerializeField] private CanvasGroup _techPanel;
        [SerializeField] private ButtonBase _techPanelButton;
        
        public ButtonBase WiFiButton => _wifiButton;
        public ButtonBase RussianLanguageButton => _russianLanguageButton;
        public ButtonBase EnglishLanguageButton => _englishLanguageButton;

        public TMP_Text TokenValue => _tokenValue;

        public GameObject RussianFlagOutline => _russianFlagOutline;
        public GameObject EnglishFlagOutline => englishFlagOutline;

        public TMP_Text DeviceWiFiConnectionValue => _wifiConnectionValue;
        public TMP_Text DeviceChargeValue => _deviceChargeValue;
        public TMP_Text ControllerChargeValue => _controllerChargeValue;
        public TMP_Text SessionTitle => _sessionTitle;

        public DownloadInfoWindow DownloadContentWindow => downloadContentWindow;

        public Slider HeadsetChargeSlider => _headsetChargeSlider;
        public Slider ControllerChargeSlider => _controllerChargeSlider;
        public List<Sprite> WifiSignalStrengthSprites => wifiSignalStrengthSprites;
        public Image WifiSignalImage => _wifiSignalImage;
        public GameObject ChargeControllerGameObject => _chargeControllerGameObject;

        public GameObject WaitingForContentWindow => _waitingForContentWindow;

        public CanvasGroup TechPanel => _techPanel;

        public ButtonBase TechPanelButton => _techPanelButton;
    }
}