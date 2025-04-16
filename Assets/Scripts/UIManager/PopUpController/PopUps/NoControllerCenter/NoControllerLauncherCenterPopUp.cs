using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UIManager.Windows
{
    public class NoControllerLauncherCenterPopUp : PopUpBase
    {
        [SerializeField] private CanvasGroup[] _canvasGroups;
        [SerializeField] private ButtonBase _centerButton;
        [SerializeField] private ButtonBase _closeButton;
        [SerializeField] private UITimer _timer;
        
        private int _currentCanvasGroupIndex;
        
        private const float AnimationDuration = 0.8f;

        public event Action OnClosed;

        private void Awake()
        {
            _timer.OnTimerFinished += () =>
            {
                ResetTracking();
                SetStep(2);
            };
            
            _centerButton.OnClick += () =>
            {
                SetStep(1);
                _timer.StartTimer(img =>
                {
                    img.transform.localScale = Vector3.one * 0.7f;
                    img.transform.DOScale(Vector3.one, AnimationDuration);
                }).Forget();
            };

            _closeButton.OnClick += Close;
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            SetStep(0);
        }

        protected override void OnClose() => OnClosed?.Invoke();

        private void SetStep(int index)
        {
            foreach (var canvasGroup in _canvasGroups)
            {
                canvasGroup.gameObject.SetActive(false);
            }
            
            _currentCanvasGroupIndex = index;
            
            _canvasGroups[_currentCanvasGroupIndex].gameObject.SetActive(true);
        }

        private void ResetTracking()
        {
            Debug.Log("RECENTER - NoControllerLauncherCenterPopUp");
#if UNITY_EDITOR
            if (Pvr_UnitySDKManager.SDK.pvr_UnitySDKEditor != null) 
                Pvr_UnitySDKManager.SDK.pvr_UnitySDKEditor.ResetUnitySDKSensor();
#else
            if (Pvr_UnitySDKSensor.Instance != null)
                Pvr_UnitySDKSensor.Instance.ResetUnitySDKSensor();
#endif
        }
    }
}