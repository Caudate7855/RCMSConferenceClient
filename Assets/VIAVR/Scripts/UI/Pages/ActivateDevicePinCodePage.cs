using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VIAVR.Scripts.Network;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI.Pages
{
    /// <summary>
    /// Страница активации устройства(ввода 6 цифр для привязки к отелю).
    /// </summary>
    public sealed class ActivateDevicePinCodePage : PageBase
    {
        [SerializeField] private PincodeControl _pincodeControl;
        [SerializeField] private ButtonBase _buttonOpenWifiPage;

        public override bool InitializePage()
        {
            if(base.InitializePage()) return PAGE_INITIALIZED;
        
            _pincodeControl.Initialize();

            _pincodeControl.OnPincodeSend += async pincode =>
            {
                await SendPinCode(pincode);
            };
        
            _buttonOpenWifiPage.OnClick += () =>
            {
                _appCore.OpenWifiPage();
            };

            return PAGE_CONTINUE_INITIALIZATION;
        }

        public override void OnPageOpen()
        {
            _pincodeControl.Clear();
        }

        async UniTask SendPinCode(string pincode)
        {
            try
            {
                _pincodeControl.Interactable = false;

                var resultTokenModel = await HttpClient.Instance.DoRoutine<LinkDeviceResult>(HttpClient.RoutineType.LINK_DEVICE, pincode, _appCore.SERIAL);

                if (resultTokenModel.IsSuccess && resultTokenModel.Data != null)
                {
                    var tokenModel = resultTokenModel.Data;

                    //_appCore.SetNewToken(tokenModel.Token);
                
                    _appCore.UIManager.ClosePage<ActivateDevicePinCodePage>();
                }
                else
                {
                    _pincodeControl.DisplayPincodeValidation(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _pincodeControl.Interactable = true;
            }
        }
    }
}