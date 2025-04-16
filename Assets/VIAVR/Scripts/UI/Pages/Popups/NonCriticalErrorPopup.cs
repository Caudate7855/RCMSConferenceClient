using TMPro;
using UnityEngine;

namespace VIAVR.Scripts.UI.Pages.Popups
{
    /// <summary>
    /// Не критическая ошибка на устройстве, при которой дальнейшая работа возможна.
    /// Например, удалено видео.
    /// </summary>
    public class NonCriticalErrorPopup : CloseablePage
    {
        [SerializeField] private TextMeshProUGUI _errorInfoText;
        [SerializeField] private string _infoTemplate;

        public override bool InitializePage()
        {
            if(base.InitializePage()) return PAGE_INITIALIZED;

            _appCore.OnAppLaunchError += package =>
            {
                _errorInfoText.text = _infoTemplate.Replace("%APP%", package);
            };

            return PAGE_CONTINUE_INITIALIZATION;
        }
    }
}