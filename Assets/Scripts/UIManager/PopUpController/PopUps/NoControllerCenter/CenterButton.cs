using System;
using Cysharp.Threading.Tasks;
using Services;
using UIManager.UISystem.Abstracts;
using UnityEngine;
using UnityEngine.Serialization;
using VIAVR.Scripts.Core;
using Zenject;

namespace UIManager.Windows
{
    public class CenterButton : UIViewBase
    {
        [SerializeField] ButtonBase _button;
        [SerializeField] ControllersHandler _controllersHandler;
        
        [Inject] PopUpService _popUpService;

        private NoControllerLauncherCenterPopUp _centerPopUp;

        private async void Awake()
        {
            //если не режим без контроллера - отключаем кнопку
            if (!_controllersHandler.NoControllerMode)
            {
                gameObject.SetActive(false);
                return;
            }

            await UniTask.Delay(1000);
            
            _centerPopUp = _popUpService.GetPopUp<NoControllerLauncherCenterPopUp>();
            _centerPopUp.Close();
            _centerPopUp.OnClosed += () => gameObject.SetActive(true);
            
            _button.OnClick += () =>
            {
                _popUpService.OpenPopup<NoControllerLauncherCenterPopUp>();
                if(_centerPopUp.gameObject.activeSelf)
                    gameObject.SetActive(false);
            };
            
            //делаем кнопку полупрозрачной чтобы не бросалась в глаза
            _button.OnPointerEnterEventHandler += () => SetButtonAlpha(1);
            
            _button.OnPointerExitEventHandler += () => SetButtonAlpha(0.5f);
            
            gameObject.SetActive(false);
        }

        private void SetButtonAlpha(float alpha)
        {
            var mat = _button.Button.image.color;
            mat.a = alpha;
            _button.Button.image.color = mat;
        }
    }
}