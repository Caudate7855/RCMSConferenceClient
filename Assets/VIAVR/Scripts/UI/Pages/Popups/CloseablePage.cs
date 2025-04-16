using UnityEngine;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI.Pages.Popups
{
    public class CloseablePage : PageBase
    {
        [Header(nameof(CloseablePage))]
        [SerializeField] private ButtonBase _buttonClose;
    
        public override bool InitializePage()
        {
            if(base.InitializePage()) return PAGE_INITIALIZED;
        
            _buttonClose.OnClick += () => Singleton<AppCore>.Instance.UIManager.ClosePage(this);

            return PAGE_CONTINUE_INITIALIZATION;
        }
    }
}