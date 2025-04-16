using Services;
using UIManager.WiFiErrorPopUp;
using UnityEngine;
using Zenject;

public class DebugTooling : MonoBehaviour
{
    private PopUpService _popUpService;

    [Inject]
    private void Construct(PopUpService popUpService)
    {
        _popUpService = popUpService;
    }

    [ContextMenu("ShowNoWifiPopup")]
    private void ShowNoWifiPopUp()
    {
        _popUpService.OpenPopup<WiFiErrorPopUp>();
    }
}