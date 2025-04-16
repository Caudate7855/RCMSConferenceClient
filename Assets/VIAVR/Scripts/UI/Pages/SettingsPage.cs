using NaughtyAttributes;
using TMPro;
using UnityEngine;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI.Pages
{
    public class SettingsPage : PageBase
    {
        [HorizontalLine(1)]
        [SerializeField] private ButtonBase _buttonOpenWifiPage;
        [SerializeField] private ButtonBase _closeButton;

        [SerializeField] ButtonWithTextMeshPro _checkUpdateButton;

        [SerializeField] private string _checkUpdateNormalText;
        [SerializeField] private string _checkUpdateWaitText;

        [SerializeField] TextMeshProUGUI _currentVersionText;
        [SerializeField] TextMeshProUGUI _newVersionText;
    
        //[SerializeField] TextMeshProUGUI _hotelNameText;
        [SerializeField] TextMeshProUGUI _helmetLevelText;
        [SerializeField] TextMeshProUGUI _helmetNameText;
    
        private string HelmetName {
            set => _helmetNameText.text = string.IsNullOrEmpty(value) ? "---" : value;
        }
    
        private string HelmetLevel {
            set => _helmetLevelText.text = string.IsNullOrEmpty(value) ? "---" : value + "%";
        }
    
        private string CurrentVersion {
            set => _currentVersionText.text = string.IsNullOrEmpty(value) ? "---" : value;
        }
    
        private string NewVersion {
            set => _newVersionText.text = string.IsNullOrEmpty(value) ? "---" : value;
        }

        public override bool InitializePage()
        {
            if(base.InitializePage()) return PAGE_INITIALIZED;

            _buttonOpenWifiPage.OnClick += () =>
            {
                _appCore.OpenWifiPage();
            };

            _closeButton.OnClick += () =>
            {
                _appCore.UIManager.ClosePage(this);
            };

            _appCore.OnUpdateFound += updateInfo =>
            {
                NewVersion = updateInfo;
            };

            _checkUpdateButton.SetText(_checkUpdateNormalText);
        
            /*_checkUpdateButton.OnClick += async () =>
        {
            _checkUpdateButton.Activate(false);

            _checkUpdateButton.SetText(_checkUpdateWaitText);

            await _appCore.ForceCheckUpdate();

            _checkUpdateButton.SetText(_checkUpdateNormalText);

            _checkUpdateButton.Activate(true);
        };*/

            CurrentVersion = Application.version;
            NewVersion = string.Empty;

            return PAGE_CONTINUE_INITIALIZATION;
        }

        public override void OnPageOpen()
        {
            base.OnPageOpen();
        
            HelmetName = _appCore.UIManager.HelmetName;
            HelmetLevel = _appCore.BatteryHandler.HelmetPower.ToString();
        }
    }
}