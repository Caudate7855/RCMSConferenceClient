using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using NaughtyAttributes;
using Pvr_UnitySDKAPI;
using UnityEngine;
using UnityEngine.UI;
using VIAVR.Scripts.Core.SerializableDictionary;
using VIAVR.Scripts.UI.Activators;

namespace VIAVR.Scripts.Core
{
    public class ControllersHandler : MonoBehaviour
    {
        public enum ControllerType { HEADSET, CONTROLLER }
        public enum ControllerConnectState { DISCONNECTED, CHANGED_TO_DISCONNECTED, CONNECTED, CHANGED_TO_CONNECTED, ERROR }

        public enum ButtonType { HOME, APP, TRIGGER, TOUCHPAD } // TODO обрабатывать остальные кнопки
        public enum ButtonAction { DOWN, UP }
    
        // нажата или отпущена кнопка контроллера/шлема, bool == true если долгое нажатие
        // DOWN + true срабатывает в момент когда нажатие начинает считаться долгим (после _longPressTimeSeconds)
        public event Action<ButtonType, ButtonAction, bool> OnControllerButton;

        public event Action<ControllerConnectState> OnControllerConnectChanged; 
    
        public event Action<List<GameObject>> OnUIObjectHoverChanged;
        public event Action<List<GameObject>> OnHeadControlClick;

        [SerializeField] private ControllerType _editorDebugController = ControllerType.HEADSET;

        [SerializeField] private bool _noControllerMode;
        [SerializeField] private bool _disableControllerConnect;
        [SerializeField] private SerializableDictionary<string, float> _clickTimeByTag;

        [SerializeField] private ActivatorSpriteFader _headCursorCircleActivator;
        [SerializeField] private ActivatorSpriteFader _waitAndClickActivator;
        [SerializeField] private Material _waitAndClickMaterial;
        [SerializeField] private float _secondsWhileWaitAndClick = 3f;
    
        [SerializeField] private bool _sameAsDeviceControllerBehaviour; // включив галку в эдиторе будет требовать подключения контроллера, как на шлеме

        [SerializeField] private float _controllerDotCursorSize = 0.2f;
        [SerializeField] private float _rayDefaultLength = 4;
    
        [SerializeField] private Transform _recenterParent;
    
        [SerializeField] private GameObject _head;
        [SerializeField] private GameObject _headsetDotCursor;
        private Transform _headsetDotCursorParent;
    
        [SerializeField] private ControllerRay _controller0; // нет у модели шлема "G2 4K"
        [SerializeField] private ControllerRay _controller1; // основной контроллер под правую руку

        [SerializeField] private float _longPressTimeSeconds = 0.5f;

        [SerializeField] private SpriteRenderer _headsetCursorSpriteRenderer;
        [SerializeField] private SerializableDictionary<bool, Sprite> _headsetCursorGraphicsByNoControllerMode;

        private List<GameObject> _uiHierarchyUnderCursor;
        private GameObject _underCursorTargetButton;
    
        private Coroutine _waitAndClickCoroutine;
        private TweenerCore<float, float, FloatOptions> _waitAndClickAnimation;
    
        private bool _needExecuteClick;

        private readonly Dictionary<ButtonType, KeyCode> _keyCodeByType = new Dictionary<ButtonType, KeyCode> // юзаются и в юнити и на шлеме
        {
            { ButtonType.HOME, KeyCode.Home },
            { ButtonType.APP, KeyCode.Backspace },
            { ButtonType.TRIGGER, KeyCode.Mouse0 },
            { ButtonType.TOUCHPAD, KeyCode.Mouse1 }
        };
    
        private readonly Dictionary<ButtonType, Pvr_KeyCode> _keyCodeNativeByType = new Dictionary<ButtonType, Pvr_KeyCode> // юзаются дополнительно на девайсе
        {
            { ButtonType.HOME, Pvr_KeyCode.HOME }, // чекается по KeyCode.Home и KeyCode.Escape, но по Pvr_KeyCode.HOME не чекается (из-за переназначения в хелпере?)
            { ButtonType.APP, Pvr_KeyCode.APP }, // не чекается по KeyCode
            { ButtonType.TRIGGER, Pvr_KeyCode.TRIGGER }, // не чекается по KeyCode
            { ButtonType.TOUCHPAD, Pvr_KeyCode.TOUCHPAD }, // не чекается по KeyCode
        };

        private readonly Dictionary<ButtonType, float?> _buttonPressTime = new Dictionary<ButtonType, float?>();

        private ButtonType[] _buttonTypes;

        public bool NoControllerMode => _noControllerMode;

        public bool HasUIUnderCursor => _uiHierarchyUnderCursor != null && _uiHierarchyUnderCursor.Count > 0;

        public bool IsConnected
        {
            get
            {
                if (_noControllerMode)
                    return true;
            
                return _currentControllerState == ControllerConnectState.CONNECTED ||
                       _currentControllerState == ControllerConnectState.CHANGED_TO_CONNECTED;
            }
        }
    
        public ControllerConnectState CurrentControllerState
        {
            get => _currentControllerState;
        
            private set
            {
                ControllerConnectState toSet;
            
                switch (value)
                {
                    case ControllerConnectState.DISCONNECTED:
                        toSet = _currentControllerState != ControllerConnectState.DISCONNECTED && _currentControllerState != ControllerConnectState.CHANGED_TO_DISCONNECTED ?
                            ControllerConnectState.CHANGED_TO_DISCONNECTED : value;
                        break;
                
                    case ControllerConnectState.CHANGED_TO_DISCONNECTED:
                        toSet = _currentControllerState == ControllerConnectState.CHANGED_TO_DISCONNECTED ?
                            ControllerConnectState.DISCONNECTED : value;
                        break;
                
                    case ControllerConnectState.CONNECTED:
                        toSet = _currentControllerState != ControllerConnectState.CONNECTED && _currentControllerState != ControllerConnectState.CHANGED_TO_CONNECTED ?
                            ControllerConnectState.CHANGED_TO_CONNECTED : value;
                        break;
                
                    case ControllerConnectState.CHANGED_TO_CONNECTED:
                        toSet = _currentControllerState == ControllerConnectState.CHANGED_TO_CONNECTED ?
                            ControllerConnectState.CONNECTED : value;
                        break;
                
                    case ControllerConnectState.ERROR:
                        toSet = value;
                        break;
                
                    default:
                        Debug.LogError($"CurrentControllerState set: {nameof(value)} == {value} is out of switch range! CurrentControllerState value will not be updated.");
                        return;
                }

                _currentControllerState = toSet;
            
                OnControllerConnectChanged?.Invoke(_currentControllerState);
            }
        }
    
        private ControllerConnectState _currentControllerState = ControllerConnectState.DISCONNECTED;

        public ControllerRay CurrentControllerRay => _currentControllerRay;
        private ControllerRay _currentControllerRay;

        public Transform HeadsetDotCursorTransform => _headsetDotCursor.transform;
    
        private Ray _ray = new Ray();
        private RaycastHit _hit = new RaycastHit();

        private Vector3 _headsetDotDefaultScale;
    
        private int _lastArc2 = 360;
        private static readonly int Arc2ShaderProperty = Shader.PropertyToID("_Arc2");
    
        [ShowNativeProperty] public float HeadAngleVertical => Vector3.SignedAngle(_recenterParent.forward, _head.transform.forward, _recenterParent.right);
        public float HeadAngleHorizontal => Vector3.SignedAngle(transform.forward, _head.transform.forward, transform.up);

#if UNITY_EDITOR
        private bool _wasPaused = false; // для эмуляции отключения контроллера во время сна шлема
#endif 
    
        void Start()
        {
            _buttonTypes = (ButtonType[])Enum.GetValues(typeof(ButtonType));
        
            _headsetDotDefaultScale = _headsetDotCursor.transform.localScale;
            _headsetDotCursorParent = _headsetDotCursor.transform.parent;
        
#if !UNITY_EDITOR     
        SetActiveHeadCursor(_noControllerMode);
#endif
        
            if (!_disableControllerConnect && Pvr_UnitySDKManager.SDK.isHasController)
            {
                Pvr_ControllerManager.PvrServiceStartSuccessEvent += ServiceStartSuccess;
                Pvr_ControllerManager.SetControllerStateChangedEvent += ControllerStateListener;
                Pvr_ControllerManager.ControllerStatusChangeEvent += CheckControllerStateForGoblin;
            }

            _headsetCursorSpriteRenderer.sprite = _headsetCursorGraphicsByNoControllerMode[NoControllerMode];
        
#if UNITY_EDITOR
            /*SetDebugController(_editorDebugController);
        
            // эмуляция отключения контроллера во время сна шлема
            Singleton<AppCore>.Instance.OnApplicationPaused += paused =>
            {
                if (paused)
                {
                    _wasPaused = true;
                }
                else
                {
                    // правильно эмулировать выключение контроллера при выходе из паузы,
                    // т.к. на шлеме отвал контроллера чекнется тоже при выходе шлема из сна, а не при уходе в него
                    if (_sameAsDeviceControllerBehaviour && _wasPaused)
                        SetDebugController(ControllerType.HEADSET);

                    _wasPaused = false;
                }
            };*/
#endif
        
            if (_disableControllerConnect)
            {
                _controller0.gameObject.SetActive(false);
                _controller1.gameObject.SetActive(false);
            
                _headsetDotCursor.SetActive(true);
            }
            
            StartControllerHandling();
        }
    
#if UNITY_EDITOR
        private readonly Rect _rect = new Rect(10, 10, 500, 24);
    
        // выводим подсказу как подрубить контроллер в режиме симуляции (галка _sameAsDeviceControllerBehaviour)
        private void OnGUI()
        {
            if (_sameAsDeviceControllerBehaviour && CurrentControllerState == ControllerConnectState.DISCONNECTED)
            {
                GUI.Box(_rect, "Вкл/откл контроллер - клавиши W/Q (_sameAsDeviceControllerBehaviour == true)");
            }
        }
#endif

        public async void StartControllerHandling()
        {
            if(!_disableControllerConnect)
                FindCurrentController();

#if !UNITY_EDITOR
        if(!_disableControllerConnect)
            await SearchController();
#endif

            if (_noControllerMode)
            {
                OnUIObjectHoverChanged += hoverHierarchy =>
                {
                    if (hoverHierarchy != null && hoverHierarchy.Any(uiObject => uiObject.TryGetComponent<Button>(out var selectable) && selectable.interactable && selectable.navigation.mode != Navigation.Mode.Explicit))
                    {
                        var target = hoverHierarchy.First(uiObject2 => uiObject2.GetComponent<Button>() != null); // Button или Selectable

                        bool targetButtonChanged = target != _underCursorTargetButton;
                        _underCursorTargetButton = target;

                        if (targetButtonChanged)
                        {
                            if (_waitAndClickCoroutine != null)
                                StopCoroutine(_waitAndClickCoroutine);
                        
                            _waitAndClickCoroutine = StartCoroutine(WaitAndClickFocusedUIElement());
                        }
                    }
                    else
                    {
                        if (_waitAndClickCoroutine != null)
                            StopCoroutine(_waitAndClickCoroutine);
                    
                        _underCursorTargetButton = null;
                    
                        _lastArc2 = _waitAndClickMaterial.GetInt(Arc2ShaderProperty);
                        _waitAndClickAnimation?.Kill(true);
                    }
                };
            }
        }

        void OnDestroy()
        {
            if (Pvr_UnitySDKManager.SDK != null && Pvr_UnitySDKManager.SDK.isHasController)
            {
                Pvr_ControllerManager.PvrServiceStartSuccessEvent -= ServiceStartSuccess;
                Pvr_ControllerManager.SetControllerStateChangedEvent -= ControllerStateListener;
                Pvr_ControllerManager.ControllerStatusChangeEvent -= CheckControllerStateForGoblin;
            }
        }
    
        void Update()
        {
            #region buttons press handling

            // из-за переназначения кнопок Controller.UPvr_GetKeyDown(0, Pvr_KeyCode.HOME) может не работать, так что чекать надо и _keyCodeByType и _keyCodeNativeByType

            foreach (ButtonType buttonType in _buttonTypes)
            {
                if(!_buttonPressTime.ContainsKey(buttonType)) _buttonPressTime.Add(buttonType, 0);
            
                if (Input.GetKeyDown(_keyCodeByType[buttonType]) || Controller.UPvr_GetKeyDown(0, _keyCodeNativeByType[buttonType]))
                {
                    OnControllerButton?.Invoke(buttonType, ButtonAction.DOWN, false);
                }
                else if (Input.GetKeyUp(_keyCodeByType[buttonType]) || Controller.UPvr_GetKeyUp(0, _keyCodeNativeByType[buttonType]))
                {
                    // _buttonPressTime[buttonType] == null только когда лонгпресс
                    OnControllerButton?.Invoke(buttonType, ButtonAction.UP, !_buttonPressTime[buttonType].HasValue);

                    _buttonPressTime[buttonType] = 0;
                }

                if (Input.GetKey(_keyCodeByType[buttonType]) || Controller.UPvr_GetKey(0, _keyCodeNativeByType[buttonType]))
                {
                    if (_buttonPressTime[buttonType].HasValue)
                    {
                        _buttonPressTime[buttonType] += Time.deltaTime;
                    
                        if (_buttonPressTime[buttonType] > _longPressTimeSeconds)
                        {
                            // чтоб не срало эвентом каждый кадр
                            _buttonPressTime[buttonType] = null;
                        
                            OnControllerButton?.Invoke(buttonType, ButtonAction.DOWN, true);
                        }
                    }
                }
            }
        
            #endregion

            if (_needExecuteClick)
            {
                CurvedUIInputModule.CustomControllerButtonState = true;
                _needExecuteClick = false;
            }
            else
            {
#if UNITY_EDITOR
                CurvedUIInputModule.CustomControllerButtonState = Input.GetMouseButton(0);
#else
            CurvedUIInputModule.CustomControllerButtonState = Input.GetMouseButton(0) || Controller.UPvr_GetKey(0, Pvr_KeyCode.TRIGGER) || Controller.UPvr_GetKey(0, Pvr_KeyCode.TOUCHPAD) || Input.GetKey(KeyCode.JoystickButton0);
#endif
            }
        
            bool isAdaptiveRayLength = Pvr_ControllerManager.Instance.LengthAdaptiveRay;
        
            // Обработка управления "шлемом с курсором"
            if (_headsetDotCursor.activeSelf)
            {
                Vector3 sensorAngles = Pvr_UnitySDKSensor.Instance.HeadPose.Orientation.eulerAngles;
            
                _headsetDotCursorParent.localRotation = Quaternion.Euler(sensorAngles.x, sensorAngles.y, 0);

                _ray.origin = _head.transform.position;
                _ray.direction = _head.transform.forward;
            
                CurvedUIInputModule.CustomControllerRay = _ray;
            
                if (isAdaptiveRayLength && Physics.Raycast(_ray, out _hit))
                {
                    _headsetDotCursor.transform.position = _hit.point;
                    _headsetDotCursor.transform.position -= (_hit.point - _head.transform.position).normalized * 0.02f;
                    float scale = 0.008f * _hit.distance / 4f;
                    scale = Mathf.Clamp(scale, 0.002f, 0.008f);
                    _headsetDotCursor.transform.localScale = new Vector3(scale, scale, 1);
                }
                else
                {
                    _headsetDotCursor.transform.position = _head.transform.position + _ray.direction.normalized * (0.5f + _rayDefaultLength);
                    _headsetDotCursor.transform.localScale = _headsetDotDefaultScale;
                }
            }
            else
            {
                // Обработка управления контроллером
                if (_currentControllerRay != null)
                {
                    Transform dot = _currentControllerRay.RayDot;
                    Transform start = _currentControllerRay.RayStart;
                    Transform rayAdaptive = _currentControllerRay.RayAdaptive;
                
                    _ray.origin = start.position;
                    _ray.direction = _currentControllerRay.transform.forward - _currentControllerRay.transform.up * 0.25f;
                
                    CurvedUIInputModule.CustomControllerRay = _ray;
                
                    if (Physics.Raycast(_ray, out _hit))
                    {
                        dot.position = _hit.point;
                    
                        if (isAdaptiveRayLength)
                        {
                            float scale = _controllerDotCursorSize * dot.localPosition.z / 3.3f;
                            scale = Mathf.Clamp(scale, 0.05f, _controllerDotCursorSize);
                            dot.localScale = new Vector3(scale, scale, 1);
                        }
                    }
                    else
                    {
                        if (isAdaptiveRayLength)
                        {
                            dot.localScale = new Vector3(_controllerDotCursorSize, _controllerDotCursorSize, 1);
                        }
                    }
                
#if UNITY_EDITOR
                    LineRenderer lineRenderer = rayAdaptive.GetComponent<LineRenderer>();
                    lineRenderer.SetPosition(0,_currentControllerRay.transform.TransformPoint(0, 0, 0.072f));
                    lineRenderer.SetPosition(1, dot.position);
#endif
                }
            }
        }

        private void FixedUpdate()
        {
            if(!_noControllerMode) return;
        
            var pointerEventData = CurvedUIInputModule.Instance.GetLastPointerEventDataPublic(-1);
            
            if(pointerEventData == null) return;

            //var currentHovered = pointerEventData.pointerEnter;
            
            bool hoverChanged = IsHierarchyUnderCursorChanged(_uiHierarchyUnderCursor, pointerEventData.hovered);
            
            _uiHierarchyUnderCursor = new List<GameObject>(pointerEventData.hovered);

            if (hoverChanged)
                OnUIObjectHoverChanged?.Invoke(_uiHierarchyUnderCursor);
        }

        bool IsHierarchyUnderCursorChanged(List<GameObject> a, List<GameObject> b)
        {
            if (a == null && b == null) return false;
        
            if (a == null && b != null) return true;
            if (a != null && b == null) return true;

            if (a.Count != b.Count) return true;

            for (var index = 0; index < a.Count; index++)
            {
                var aElement = a[index];
                var bElement = b[index];

                if (!ReferenceEquals(aElement, bElement)) return true;
            }

            return false;
        }

        IEnumerator WaitAndClickFocusedUIElement()
        {
            float waitSeconds = _secondsWhileWaitAndClick;

            if (_uiHierarchyUnderCursor != null)
            {
                foreach (var uiGameObject in _uiHierarchyUnderCursor)
                {
                    /*var buttonBase = uiGameObject.GetComponent<ButtonBase>(); // TODO вместо GetComponent лучше сопоставлять время с тегом объекта?

                if (buttonBase == null || !buttonBase.CustomHeadClickTime.HasValue) continue;

                waitSeconds = buttonBase.CustomHeadClickTime.Value;
                break;*/

                    string uiGameObjectTag = uiGameObject.tag;
                
                    if (_clickTimeByTag.ContainsKey(uiGameObjectTag))
                    {
                        waitSeconds = _clickTimeByTag[uiGameObjectTag];
                        break;
                    }
                }
            }
        
            _lastArc2 = 0;
            _waitAndClickMaterial.SetInt(Arc2ShaderProperty, 0);
        
            _waitAndClickAnimation?.Kill();
        
            _waitAndClickMaterial.SetInt(Arc2ShaderProperty, 360);
            _waitAndClickActivator.Activate(true, true);

            _waitAndClickAnimation = _waitAndClickMaterial.DOFloat(0, "_Arc2", waitSeconds).SetEase(Ease.Linear).OnComplete(() =>
            {
                _waitAndClickMaterial.SetInt(Arc2ShaderProperty, _lastArc2);
                _waitAndClickActivator.Activate(false, true);
            });

            yield return new WaitForSeconds(waitSeconds);

            _needExecuteClick = true;
        
            OnHeadControlClick?.Invoke(_uiHierarchyUnderCursor);
        }

        async UniTask SearchController()
        {
            if (CurrentControllerState == ControllerConnectState.CONNECTED ||
                CurrentControllerState == ControllerConnectState.CHANGED_TO_CONNECTED)
            {
                CurrentControllerState = ControllerConnectState.CONNECTED;
            
                Debug.Log("SearchController: Controller already connected!");
                return;
            }
        
            Debug.Log("SearchController: start searching...");
        
            bool isControllerConnected = false;
        
            // поиск любого контроллера (внутри работает через java сервис, встроенный в прошивку шлема)
            Pvr_ControllerManager.controllerlink.StartScan();

            do
            {
                await UniTask.Delay(1000);
            
                // попытка подключиться к любому найденному контроллеру
                Pvr_ControllerManager.Instance.ConnectBLE();
            
                await UniTask.Delay(1000);

                isControllerConnected =
                    Controller.UPvr_GetControllerState(0) == ControllerState.Connected ||
                    Controller.UPvr_GetControllerState(1) == ControllerState.Connected;
            
            } while (isControllerConnected == false);
        
            Pvr_ControllerManager.controllerlink.StopScan();
        
            Debug.Log("SearchController: сontroller found!");
        }

        // только для дебага в Editor'е, в шлеме объекты активируются/деактивируются автоматически
        // Это важно, если на шлеме переключить отображение вручную этим методом, то обработка контроллера жестко отвалится
        public void SetDebugController(ControllerType controllerType)
        {
            ShowControllerObjects(controllerType);

            switch (controllerType)
            {
                case ControllerType.HEADSET:
                    if(Application.isEditor && _sameAsDeviceControllerBehaviour)
                        CurrentControllerState = ControllerConnectState.DISCONNECTED;
                    break;
            
                case ControllerType.CONTROLLER:
                    if(Application.isEditor && _sameAsDeviceControllerBehaviour)
                        CurrentControllerState = ControllerConnectState.CONNECTED;
                    break;
            
                default:
                    Debug.LogError($"Unhandled {nameof(controllerType)} {controllerType}");
                    break;
            }
        }

        public void SetActiveHeadCursor(bool state)
        {
            _headCursorCircleActivator.Activate(state, true);
        }

        // только для дебага в Editor'е, в шлеме объекты активируются/деактивируются автоматически
        private void ShowControllerObjects(ControllerType controllerType)
        {
            _headsetDotCursor.SetActive(false);
        
            _controller0.HideRay();
            _controller1.HideRay();
        
            _controller0.gameObject.SetActive(false);
            _controller1.gameObject.SetActive(false);
        
            _currentControllerRay = null;
        
            switch (controllerType)
            {
                case ControllerType.HEADSET:
                    _headsetDotCursor.SetActive(true);
                    break;
            
                case ControllerType.CONTROLLER:
                    _controller1.gameObject.SetActive(true);
                    _currentControllerRay = _controller1;
                    _currentControllerRay.ShowRay();
                    break;
            
                default:
                    Debug.LogError($"Unhandled {nameof(controllerType)} {controllerType}");
                    break;
            }
        }

        private void FindCurrentController()
        {
            bool isConnected = false;

#if UNITY_EDITOR
            if (!_sameAsDeviceControllerBehaviour)
            {
                CurrentControllerState = ControllerConnectState.CONNECTED;
            }
            else
            {
                CurrentControllerState = _editorDebugController == ControllerType.CONTROLLER
                    ? ControllerConnectState.CONNECTED
                    : ControllerConnectState.DISCONNECTED;

                isConnected = _editorDebugController == ControllerType.CONTROLLER;
            }
#else
        isConnected = Controller.UPvr_GetControllerState(0) == ControllerState.Connected ||
                      Controller.UPvr_GetControllerState(1) == ControllerState.Connected;

        var state = Controller.UPvr_GetControllerState(Controller.UPvr_GetMainHandNess());
        
        switch (state)
        {
            case ControllerState.Error:
                CurrentControllerState = ControllerConnectState.ERROR;
                break;
            case ControllerState.DisConnected:
                CurrentControllerState = ControllerConnectState.DISCONNECTED;
                break;
            case ControllerState.Connected:
                CurrentControllerState = ControllerConnectState.CONNECTED;
                break;
            default:
                Debug.LogError($"{nameof(ControllerState)} switch {state} is out of range!");
                break;
        }
#endif

            _headsetDotCursor.SetActive(!isConnected);

            if (!isConnected)
            {
                _currentControllerRay = null;
                return;
            }

#if !UNITY_EDITOR
        // при использовании шлема "G2 4K" UPvr_GetMainHandNess() всегда возвращает 1
        _currentControllerRay = Controller.UPvr_GetMainHandNess() == 0 ? _controller0 : _controller1;
#endif
        }
    
        private void ServiceStartSuccess()
        {
            Debug.Log("ServiceStartSuccess()");

            FindCurrentController();
        }

        // Для модели шлема Pico G2 эвент не вызывается
        private void ControllerStateListener(string data)
        {
            Debug.Log("ControllerStateListener: " + data);

            FindCurrentController();
        }
    
        // Вызывается для модели шлема Pico G2
        private void CheckControllerStateForGoblin(string state)
        {
            Debug.Log("CheckControllerStateForGoblin: " + (state.Equals("1") ? "CONNECTED" : "DISCONNECTED"));
        
            FindCurrentController();
        }
    }
}