using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VIAVR.Scripts.Core;
using VIAVR.Scripts.UI.Activators;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI.Pages
{
    public class VrTourUIPage : PageBase
    {
        public event Action<bool> OnUISetActive;
        public event Action<bool> OnUIStereoSettingChanged;
    
        [SerializeField] private bool _alwaysShowControls;
        [SerializeField] private float _openMenuAngleVertical = 20f;
        [SerializeField] private float _openMenuAngleHorizontal = 40f;
    
        [SerializeField] private CanvasGroupFaderActivator _controlsActivator;
    
        //[SerializeField] private ButtonBase _buttonShowUI;
        [SerializeField] private ButtonBase _buttonExit;
    
        [SerializeField] private ButtonBase _volumePlus;
        [SerializeField] private ButtonBase _volumeMinus;
        [SerializeField] private ImageWithStates _volumeIcon;

        [SerializeField] private GameObject _buttonStereoOnOffGameObject;
        [SerializeField] private ButtonOnOffImageSwitch _buttonStereoOnOff;
    
        [SerializeField] private float _startupVolume = 0.5f;
        [SerializeField] private float _volumeChangeStep = 0.2f;
    
        private const float AutoHideSeconds = 3f;
        private float _autoHideUI;
    
        private float _volume;
        private int _maxVolumeNumber;
    
        public float Volume {
            get => _volume;
        
            set {
                _volume = Mathf.Clamp01(value);
            
#if !UNITY_EDITOR
            //VolumePowerBrightness.UPvr_SetVolumeNum(Mathf.FloorToInt(Volume * _maxVolumeNumber));
#else
                _appCore.SoundsManager.Volume = _maxVolumeNumber == 0 ? Volume : Volume / _maxVolumeNumber;
#endif

                _volumeIcon.SetByValue01(_volume);
            }
        }
    
        public override bool InitializePage()
        {
            if(base.InitializePage()) return PAGE_INITIALIZED;

            _buttonExit.OnClick += () =>
            {
                if (gameObject.activeSelf)
                {
                    _appCore.UIManager.RemovePauseRecenterFollowView(this);
                
                    _appCore.EndVrTour();
                }
            };
        
#if !UNITY_EDITOR
        // https://sdk.picovr.com/docs/sdk/en/chapter_seven.html#power-volume-and-brightness-service-related
       // VolumePowerBrightness.UPvr_StartAudioReceiver(gameObject.name);
       // VolumePowerBrightness.UPvr_InitBatteryVolClass();

       // _maxVolumeNumber = VolumePowerBrightness.UPvr_GetMaxVolumeNumber();
#endif
        
            Volume = _startupVolume;
        
            // Увеличить громкость
            _volumePlus.OnClick += () =>
            {
                Volume += _volumeChangeStep;
            };
        
            // Уменьшить громкость
            _volumeMinus.OnClick += () =>
            {
                Volume -= _volumeChangeStep;
            };

            _buttonStereoOnOff.SetState(true);
        
            _buttonStereoOnOff.OnClick += state =>
            {
                OnUIStereoSettingChanged?.Invoke(state);
            };

            _appCore.UIManager.GetPage<NoControllerUIPage>().OnShowVrTourUIButtonClick += () =>
            {
                _controlsActivator.Activate(!_controlsActivator.ActiveState, true);

                if (_controlsActivator.ActiveState)
                {
                    _appCore.UIManager.RequestPauseRecenterFollowView(this);
                
                    _autoHideUI = AutoHideSeconds;
                }
                else
                {
                    _appCore.UIManager.RemovePauseRecenterFollowView(this);
                }
                    
                OnUISetActive?.Invoke(_controlsActivator.ActiveState);
            };

            _appCore.ControllersHandler.OnControllerButton += (button, action, longpress) =>
            {
                if ((button == ControllersHandler.ButtonType.TRIGGER || button == ControllersHandler.ButtonType.TOUCHPAD)
                    && action == ControllersHandler.ButtonAction.DOWN && longpress == false)
                {
                    if (!_alwaysShowControls && !EventSystem.current.IsPointerOverGameObject())
                    {
                        _controlsActivator.Activate(!_controlsActivator.ActiveState, true);
                    
                        OnUISetActive?.Invoke(_controlsActivator.ActiveState);
                    }
                }
                else if (button == ControllersHandler.ButtonType.APP && action == ControllersHandler.ButtonAction.DOWN && longpress == false)
                {
                    _buttonExit.Click();
                }
            };
        
            return PAGE_CONTINUE_INITIALIZATION;
        }

        public override void OnPageOpen()
        {
            if (!_alwaysShowControls)
                _controlsActivator.Activate(false);
            else
                _controlsActivator.Activate(true, true);
        
            base.OnPageOpen();
        }

        public override void OnPageClose()
        {
            if(!_alwaysShowControls)
                _controlsActivator.Activate(false, false);
        
            base.OnPageClose();
        }


        private void Update()
        {
#if UNITY_EDITOR       
            if(Input.GetKeyDown(KeyCode.E))
                _buttonExit.Click();
        
            if(Input.GetKeyDown(KeyCode.Keypad3))
                _buttonStereoOnOff.Click();
#endif

            if (!_appCore.UsingNoControllerMode) return;
        
            /*bool outOfUiBounds = Mathf.Abs(_appCore.ControllersHandler.HeadAngleVertical) > _openMenuAngleVertical;

        if (!_controlsActivator.ActiveState)
        {
            if (outOfUiBounds)
            {
                _appCore.UIManager.RequestPauseRecenterFollowView(this);

                _controlsActivator.Activate(true, true);
                OnUISetActive?.Invoke(true);

                _autoHideUI = AutoHideSeconds;
            }
        }
        else
        {
            if (outOfUiBounds || _appCore.ControllersHandler.HasUIUnderCursor)
            {
                _autoHideUI = AutoHideSeconds;
            }
            else
            {
                _autoHideUI -= Time.deltaTime;

                if (_autoHideUI < 0)
                {
                    _appCore.UIManager.RemovePauseRecenterFollowView(this);

                    _controlsActivator.Activate(false, true);
                    OnUISetActive?.Invoke(false);
                }
            }
        }*/
        
            /*if (_appCore.ControllersHandler.HasUIUnderCursor)
        {
            _autoHideUI = AutoHideSeconds;
        }
        else
        {
            _autoHideUI -= Time.deltaTime;

            if (_autoHideUI < 0)
            {
                _appCore.UIManager.RemovePauseRecenterFollowView(this);

                _controlsActivator.Activate(false, true);
                OnUISetActive?.Invoke(false);
            }
        }*/
        }


        public void SetupUI(bool stereoSupport)
        {
            _buttonStereoOnOffGameObject.SetActive(stereoSupport);
        }
    }
}