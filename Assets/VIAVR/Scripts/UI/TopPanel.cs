using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI
{
    public class TopPanel : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _helmetPowerText;
        [SerializeField] Image _helmetPowerFillImage;
    
        [SerializeField] TextMeshProUGUI _helmetNameText;
        [SerializeField] int _helmetNameMaxSymbols = 10;
    
        [SerializeField] ButtonBase _settingsButton;
    
        [SerializeField] private ImageWithStates _wifiImage;
    
        private Sprite _currentWifiSprite;

        private bool _initialized;

        private string _helmetName;
        public string HelmetName
        {
            get => _helmetName;
            set {
                if (string.IsNullOrEmpty(value))
                {
                    _helmetNameText.text = _helmetName = "---";
                }
                else if (value.Length > _helmetNameMaxSymbols)
                {
                    _helmetNameText.text = _helmetName = $"{value.Substring(0,_helmetNameMaxSymbols).Trim()}...";
                }
                else
                {
                    _helmetNameText.text = _helmetName = value;
                }
            }
        }

        private int _helmetPower;
        public int HelmetPower
        {
            get => _helmetPower;
            set
            {
                _helmetPower = value;
            
                _helmetPowerText.text = $"{value}%";
                _helmetPowerFillImage.fillAmount = (float) value / 100f;
            }
        }
    
        public int WiFiSignalLevel
        {
            set => _wifiImage.SetByValue01(value / 100f);
        }

        public void Initialize()
        {
            if(_initialized)
                return;
        
            _initialized = true;
        
            _settingsButton.OnClick += () =>
            {
                _settingsButton.enabled = false;
            
                Singleton<AppCore>.Instance.OpenSettings();
            
                _settingsButton.enabled = true;
            };
        }
    }
}