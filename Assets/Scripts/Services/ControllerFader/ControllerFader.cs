using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VIAVR.Scripts.Core;

namespace Services.ControllerFader
{
    public class ControllerFader : MonoBehaviour
    {
        [SerializeField] private ControllersHandler _controllersHandler;
        [SerializeField] private GameObject _dot;
        [SerializeField] private GameObject _rayLine;
        [SerializeField] private GameObject _controller;

        private Pvr_ControllerInit _pvrControllerInit;
        private Material _controllerMaterial;
        private Image _controllerPowerImage;
        private GameObject _controllerTipObject;
        private GameObject _controllerTouchObject;
        private bool _isControllerInitialized;

        private const float FadeDuration = 1f;
        
        private void Awake()
        {
            _pvrControllerInit = _controller.GetComponent<Pvr_ControllerInit>();
            _pvrControllerInit.OnControllerInitialized += InitializeController;
            
            _controllersHandler.OnControllerConnectChanged += state =>
            {
                if (state == ControllersHandler.ControllerConnectState.CHANGED_TO_DISCONNECTED)
                    _isControllerInitialized = false;
            };
        }

        public void FadeController(bool condition)
        {
            if (!_isControllerInitialized)
                return;

            float endAlpha = condition ? 1 : 0;

            _dot.SetActive(condition);
            _rayLine.SetActive(condition);
            _controllerTipObject.SetActive(condition);
            _controllerTouchObject.SetActive(condition);

            _controllerMaterial?.DOFade(endAlpha, FadeDuration);
            _controllerPowerImage?.DOFade(endAlpha, FadeDuration);
        }

        private void InitializeController()
        {
            _controllerMaterial = GetComponentInChildren<MeshRenderer>(true).material;
            _controllerPowerImage = GetComponentInChildren<Pvr_ControllerPower>(true).gameObject.GetComponent<Image>();
            _controllerTipObject = GetComponentInChildren<Pvr_ToolTips>(true).gameObject;
            _controllerTouchObject = GetComponentInChildren<Pvr_TouchVisual>(true).gameObject;

            _isControllerInitialized = true;
        }
    }
}