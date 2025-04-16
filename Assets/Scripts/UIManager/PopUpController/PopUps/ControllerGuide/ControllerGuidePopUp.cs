using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Services.ControllerFader;
using UnityEngine;
using UnityEngine.UI;
using VIAVR.Scripts.Core;

namespace UIManager
{
    public class ControllerGuidePopUp : PopUpBase
    {
        [SerializeField] private List<CanvasGroup> _canvasGroups;
        [SerializeField] private ButtonBase _exitButton;
        [SerializeField] private CanvasGroup _mainCanvasGroup;
        [SerializeField] private List<Image> _stepsImages;

        private ControllersHandler _controllersHandler;
        private List<ControllerFader> _controllerFaders;
        private int _currentCanvasGroupIndex = 0;
        private bool _isPossibleChangeStep = true;

        private const float FadeDuration = 1f;
        private const float StepImagesScaleDuration = 0.5f;
        private const float StepImagesFadeDuration = 0.5f;
        
        private void Awake()
        {
            _controllerFaders = FindObjectsOfType<ControllerFader>(true).ToList();
            _controllersHandler = FindObjectOfType<ControllersHandler>(true);
        }

        private async void Start()
        {
            _exitButton.OnClick += FadeClosePopup;
            
            await UniTask.Delay(500);

            _controllersHandler.OnControllerConnectChanged += state =>
            {
                if (state == ControllersHandler.ControllerConnectState.CHANGED_TO_CONNECTED)
                    ChangeStep(1);
            };
        }
        
        protected override void OnOpen()
        {
            if (Application.isEditor)
                return;
            
            foreach (var canvas in _canvasGroups)
                canvas.alpha = 0;

            foreach (var stepImage in _stepsImages)
            {
                var color = stepImage.color;
                color.a = 0.65f;
                stepImage.color = color;
            }

            ChangeStep(0);
            _mainCanvasGroup.DOFade(1, FadeDuration);

            _exitButton.Interactable = false;
            
            Pvr_UnitySDKSensor.Instance.OnResetUnitySDKSensor += OnResetControllerPositionHandler;
        }
        
        protected override void OnClose()
        {
            if (AppFsm.GetCurrentState().GetType() == typeof(VideoShowingState))
            {
                foreach (var controllerFader in _controllerFaders)
                    controllerFader.FadeController(false);
            }

            _exitButton.Interactable = false;
            
            Pvr_UnitySDKSensor.Instance.OnResetUnitySDKSensor -= OnResetControllerPositionHandler;
        }

        private void OnResetControllerPositionHandler() => ChangeStep(2);

        private void FadeClosePopup()
        {
            _mainCanvasGroup.DOFade(0, FadeDuration);
            _canvasGroups[_currentCanvasGroupIndex].DOFade(0, FadeDuration).onComplete += Close;
        }

        private async void ChangeStep(int index)
        {
            if (!_isPossibleChangeStep)
                return;
            
            _isPossibleChangeStep = false;
            await SetStepAsync(index);
            _isPossibleChangeStep = true;
        }

        private async UniTask SetStepAsync(int index)
        {
            _canvasGroups[_currentCanvasGroupIndex].DOFade(0, FadeDuration);
            
            _stepsImages[index]
                .DOFade(1, StepImagesFadeDuration);
            _stepsImages[index].transform
                .DOScale(new Vector2(1.5f, 1.5f), StepImagesScaleDuration)
                .OnComplete(() => 
                    _stepsImages[index].transform
                        .DOScale(new Vector2(1, 1), StepImagesScaleDuration));
            
            _currentCanvasGroupIndex = index;

            if (index == 2)
            {
                _exitButton.Interactable = true;
            }
            
            await UniTask.Delay(500);

            _canvasGroups[index].DOFade(1, FadeDuration);
        }
    }
}