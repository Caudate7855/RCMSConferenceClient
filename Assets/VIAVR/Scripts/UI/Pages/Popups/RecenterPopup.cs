using Cysharp.Threading.Tasks;

namespace VIAVR.Scripts.UI.Pages.Popups
{
    public class RecenterPopup : PageBase
    {
        public override async void OnPageOpen()
        {
            base.OnPageOpen();

            await UniTask.Delay(3000);
        
#if UNITY_EDITOR
            if (Pvr_UnitySDKManager.SDK.pvr_UnitySDKEditor != null)
                Pvr_UnitySDKManager.SDK.pvr_UnitySDKEditor.ResetUnitySDKSensor();
#else
        if (Pvr_UnitySDKSensor.Instance != null)
            Pvr_UnitySDKSensor.Instance.ResetUnitySDKSensor();
#endif
        
            _appCore.UIManager.ClosePage(this);
        }
    }
}