using System.Collections.Generic;
using System.Linq;
using Services.ControllerFader;
using UnityEngine;

namespace UIManager
{
    public abstract class ErrorPopUpBase : PopUpBase
    { 
        [SerializeField] private ButtonBase _acceptButton;
        
        private List<ControllerFader> _controllerFaders;
        
        private void Awake()
        {
            _controllerFaders = FindObjectsOfType<ControllerFader>(true).ToList();
        }

        private void Start()
        {
            _acceptButton.OnClick += Close;
        }

        protected override void OnOpen()
        {
            foreach (var controllerFader in _controllerFaders)
                controllerFader.FadeController(true);
        }

        protected override void OnClose()
        {
            if (AppFsm.GetCurrentState().GetType() == typeof(VideoShowingState))
            {
                foreach (var controllerFader in _controllerFaders)
                    controllerFader.FadeController(false);
            }
        }
    }
}