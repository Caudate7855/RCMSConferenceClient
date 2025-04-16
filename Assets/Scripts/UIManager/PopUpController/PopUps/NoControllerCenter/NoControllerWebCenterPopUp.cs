using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UIManager.Windows
{
    public class NoControllerWebCenterPopUp : PopUpBase
    {
        [SerializeField] private CanvasGroup[] _canvasGroups;
        [SerializeField] private UITimer _step1Timer;
        [SerializeField] private UITimer _step2Timer;

        private int _currentCanvasGroupIndex;
        
        private const float AnimationDuration = 1f;
        private const float FadeDuration = 1f;

        private void Awake()
        {
            _step1Timer.OnTimerFinished += async () =>
            {
                await SetStepAsync(1);
                _step2Timer.StartTimer(img =>
                {
                    img.transform.localScale = Vector3.one * 0.7f;
                    img.transform.DOScale(Vector3.one, AnimationDuration);
                }).Forget();
            };
            
            _step2Timer.OnTimerFinished += Close;
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            _step1Timer.StartTimer().Forget();
        }

        protected override void OnClose()
        {
            Debug.Log("RECENTER - OnClose");
            ResetTracking();
            SetStepAsync(0).Forget();
        }

        private async UniTask SetStepAsync(int index)
        {
            if(index == _currentCanvasGroupIndex) 
                return;
            
            _canvasGroups[_currentCanvasGroupIndex].DOFade(0, FadeDuration);

            await UniTask.Delay(500);
            _currentCanvasGroupIndex = index;
            _canvasGroups[index].DOFade(1, FadeDuration);
        }

        private void ResetTracking()
        {
            Debug.Log("RECENTER - NoControllerWebCenterPopUp");
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