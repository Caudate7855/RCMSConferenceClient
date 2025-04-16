using System;
using UnityEngine;
using VIAVR.Scripts.Core.SerializableDictionary;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI.Pages
{
    public class NoControllerUIPage : PageBase
    {
        public event Action OnShowVrTourUIButtonClick;

        [SerializeField] private SerializableDictionary<bool, string> _buttonShowVrTourUITextByState;
    
        [SerializeField] private ButtonWithTextMeshPro _buttonShowVrTourUI;
        [SerializeField] private ButtonBase _buttonResetView;
    
        public override bool InitializePage()
        {
            if (base.InitializePage()) return PAGE_INITIALIZED;

            _appCore.UIManager.OnPageOpenStateChanged += (page, state) =>
            {
                if (page is VrTourUIPage)
                {
                    _buttonShowVrTourUI.gameObject.SetActive(_appCore.UsingNoControllerMode && state);
                    _buttonResetView.gameObject.SetActive(_appCore.UsingNoControllerMode && !state);

                    if (!state)
                    {
                        if(_buttonShowVrTourUITextByState.TryGetValue(false, out string text))
                            _buttonShowVrTourUI.SetText(text);
                    }
                }
            };

            _appCore.UIManager.GetPage<VrTourUIPage>().OnUISetActive += state =>
            {
                if(_buttonShowVrTourUITextByState.TryGetValue(state, out string text))
                    _buttonShowVrTourUI.SetText(text);
            };

            _buttonShowVrTourUI.OnClick += () =>
            {
                OnShowVrTourUIButtonClick?.Invoke();
            };
        
            _buttonResetView.OnClick += () =>
            {
                _appCore.RecenterViewRequest();
            };

            return PAGE_CONTINUE_INITIALIZATION;
        }

        public override void OnPageOpen()
        {
            base.OnPageOpen();
        
            _buttonShowVrTourUI.gameObject.SetActive(false);
        }
    }
}