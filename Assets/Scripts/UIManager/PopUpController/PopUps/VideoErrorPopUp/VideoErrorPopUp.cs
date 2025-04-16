using TMPro;
using UnityEngine;

namespace UIManager.VideoErrorPopUp
{
    public class VideoErrorPopUp : ErrorPopUpBase
    {
        [SerializeField] private TMP_Text _tokenValue;

        protected override void OnOpen()
        {
            base.OnOpen();

            if (PlayerPrefs.HasKey(GameGlobalConsts.DeviceTokenSaveKay))
                _tokenValue.text = PlayerPrefs.GetString(GameGlobalConsts.DeviceTokenSaveKay);
        }
    }
}