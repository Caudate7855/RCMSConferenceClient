using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using VIAVR.Scripts.Core;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.Data;
using VIAVR.Scripts.UI.Buttons;
using VIAVR.Scripts.UI.Hover;
using VIAVR.Scripts.UI.Pages;

namespace VIAVR.Scripts
{
    public class VrTourController : MonoBehaviour
    {
        public event Action OnVrTourStart; 
        public event Action OnVrTourEnd; 
    
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int MainTex2 = Shader.PropertyToID("_MainTex2");
        private static readonly int Blend = Shader.PropertyToID("_Blend");

        [SerializeField] private bool _forceNoStereo;
        [SerializeField] private bool _offStereoWhenUi;

        [SerializeField] private Transform _spheresContainer;
    
        [SerializeField] private MeshRenderer _meshRendererR;
        [SerializeField] private MeshRenderer _meshRendererL;
    
        [SerializeField] private Texture2D _defaultTextureR;
        [SerializeField] private Texture2D _defaultTextureL;

        [SerializeField] private Transform _headParentTransform;
        [SerializeField] private Transform _headTransform;
        [SerializeField] private float _sphereYRotationOffset;
    
        [SerializeField] private GameObject _roomPointButtonPrefab;
        [SerializeField] private Transform _roomPointButtonsContainer;
    
        [SerializeField] private float _changeDurationSeconds = 1f;
        [SerializeField] private float _rotationDurationSeconds = 1f;
    
        private GraphicsLoaderCache _graphicsLoaderCache;

        private GraphicsLoaderCache GraphicsLoaderCache
        {
            get
            {
                if (_graphicsLoaderCache == null)
                    _graphicsLoaderCache = Singleton<GraphicsLoaderCache>.Instance;
            
                return _graphicsLoaderCache;
            }
        }
    
        private bool _changeable = true;

        private VrTourUIPage _vrTourUIPage;
        private VrTourApartment _currentApartment;
        private VrTourPanorama _currentPanorama;
        private string _currentPanoramaKey;

        [ShowNonSerializedField]
        private bool _uiActive = false;
        private bool _stereoEnabled = true;
    
        private bool _showImgui = true;

        private readonly Dictionary<SceneButton, GameObject> _panoramaButtonsDictonary = new Dictionary<SceneButton, GameObject>();

        public void Initialize(VrTourUIPage vrTourUIPage)
        {
            _vrTourUIPage = vrTourUIPage;
        
            vrTourUIPage.OnUISetActive += state =>
            {
                _uiActive = state;
            
                if (_forceNoStereo || !_offStereoWhenUi) return;

                SetStereo(_stereoEnabled && !state);
            };

            vrTourUIPage.OnUIStereoSettingChanged += state =>
            {
                if (_forceNoStereo) return;
            
                _stereoEnabled = state;

                SetStereo(_stereoEnabled);
            };

            if (_forceNoStereo)
            {
                SetStereo(false);
            }
        }
    
        private void SetStereo(bool state)
        {
            _meshRendererR.gameObject.layer = LayerMask.NameToLayer(state ? "EyeR" : "EyeBoth");
            _meshRendererL.gameObject.SetActive(state);
        }

        public void StartVrTour(VrTourApartment apartment)
        {
            _currentApartment = apartment;

            _currentPanoramaKey = apartment.StartPanorama;

            SetStereo(_stereoEnabled);
        
            GotoPanorama(apartment.Panoramas[apartment.StartPanorama], (int)(apartment.StartHeadRotation.Value - _headTransform.localEulerAngles.y));
        
            OnVrTourStart?.Invoke();
        }

        public async void EndVrTour()
        {
            await UniTask.WhenAll(
                ChangeSphereTexture(_defaultTextureR, _defaultTextureL, 0),
                ClearButtons());

            _currentApartment = null;
            _currentPanorama = null;
            _currentPanoramaKey = string.Empty;

            _spheresContainer.rotation = Quaternion.Euler(0, _sphereYRotationOffset, 0);
            _headParentTransform.localRotation = Quaternion.identity;
        
            SetStereo(true);
        
            OnVrTourEnd?.Invoke();
        }

        private async UniTask ClearButtons()
        {
            float fadeDuration = .4f;

            var tasks = new List<UniTask>();
        
            for (int i = 0; i < _roomPointButtonsContainer.childCount; i++)
            {
                var button = _roomPointButtonsContainer.GetChild(i).gameObject;
            
                var spriteRenderers = button.GetComponentsInChildren<SpriteRenderer>();
            
                foreach (var spriteRenderer in spriteRenderers)
                    tasks.Add(spriteRenderer.DOFade(0f, fadeDuration).AsyncWaitForCompletion().AsUniTask());
            
                var text = button.GetComponentInChildren<TextMeshPro>();
            
                if(text == null) continue;
            
                var textColor = text.color;
            
                tasks.Add(DOTween.To(value => text.color = new Color(textColor.r,textColor.g,textColor.b, value), 1f, 0f, fadeDuration).AsyncWaitForCompletion().AsUniTask());
            }

            await UniTask.WhenAll(tasks);

            for (int i = 0; i < _roomPointButtonsContainer.childCount; i++)
                Destroy(_roomPointButtonsContainer.GetChild(i).gameObject);

            await UniTask.NextFrame();
        }

        async UniTask GotoPanorama(VrTourPanorama panorama, int newSphereRotation, bool clearTextures = true)
        {
            if (_currentApartment == null)
            {
                Debug.LogError("_currentRoom == null!");
                return;
            }

#if UNITY_EDITOR
            _editButton = null;
#endif

            List<UniTask> tasks = new List<UniTask>();

            tasks.Add(ClearButtons());
            tasks.Add(ChangePanorama(panorama, newSphereRotation, clearTextures));

            await UniTask.WhenAll(tasks);
        
            _panoramaButtonsDictonary.Clear();

            foreach (var panoramaButton in panorama.Buttons)
            {
                var button = Instantiate(_roomPointButtonPrefab, _roomPointButtonsContainer);
            
                button.name = panoramaButton.Text;
            
                button.transform.localPosition = panoramaButton.Position;
                button.transform.LookAt(_headTransform);
            
                var spriteRenderers = button.GetComponentsInChildren<SpriteRenderer>();

                foreach (var spriteRenderer in spriteRenderers)
                {
                    spriteRenderer.color = new Color(1, 1, 1, 0);
                    spriteRenderer.DOFade(1f, 1f);
                }

                var buttonWithText = button.GetComponentInChildren<ButtonWithText>();
                buttonWithText.SetText(panoramaButton.Text);

                if (!string.IsNullOrWhiteSpace(panoramaButton.GotoPanorama))
                {
                    buttonWithText.OnClick += () =>
                    {
                        if (_currentApartment.Panoramas.TryGetValue(panoramaButton.GotoPanorama, out var gotoPanorama))
                        {
                            Debug.Log($"<color=lime>GOTO</color> from '<b>{_currentPanoramaKey}</b>' to '<b>{panoramaButton.GotoPanorama}'</b> by button '<b>{panoramaButton.Text}</b>'");
                        
                            _currentPanoramaKey = panoramaButton.GotoPanorama;

                            int rotation = (int)((panoramaButton.HeadRotation ?? _headParentTransform.eulerAngles.y) - _headTransform.localEulerAngles.y);
                        
                            GotoPanorama(gotoPanorama, rotation);
                        }
                    };
                }
            
                _panoramaButtonsDictonary.Add(panoramaButton, button);
            }
        }

        private async UniTask ChangePanorama(VrTourPanorama newPanorama, int newSphereRotation, bool clearTextures = true)
        {
            if(!_changeable) return;

            _changeable = false;

            if (_currentPanorama != null)
            {
                _meshRendererR.material.SetTexture(MainTex, _meshRendererR.material.GetTexture(MainTex2));
                _meshRendererL.material.SetTexture(MainTex, _meshRendererL.material.GetTexture(MainTex2));
            }
        
            _vrTourUIPage.SetupUI(_forceNoStereo ? false : !string.IsNullOrEmpty(newPanorama.TextureL));

            _currentPanorama = newPanorama;
        
            var newRoomTextureR = !string.IsNullOrEmpty(newPanorama.TextureR) ? await GraphicsLoaderCache.LoadTexture2DAsync(newPanorama.TextureR, mipMaps: false, format: TextureFormat.RGB565) : _defaultTextureR;
            var newRoomTextureL = !_forceNoStereo && !string.IsNullOrEmpty(newPanorama.TextureL) ? await GraphicsLoaderCache.LoadTexture2DAsync(newPanorama.TextureL, mipMaps: false, format: TextureFormat.RGB565) : newRoomTextureR;

            await UniTask.NextFrame();
        
            await ChangeSphereTexture(newRoomTextureR, newRoomTextureL, newSphereRotation, clearTextures);
        
            _changeable = true;
        }

        private async UniTask ChangeSphereTexture(Texture2D textureR, Texture2D textureL, int newSphereRotation, bool clearTextures = true)
        {
            _meshRendererR.material.SetTexture(MainTex2, textureR);
            _meshRendererL.material.SetTexture(MainTex2, textureL != null ? textureL : textureR);
        
            _meshRendererR.material.SetFloat(Blend, 0);
            _meshRendererL.material.SetFloat(Blend, 0);
        
            _headParentTransform.rotation = Quaternion.Euler(0, newSphereRotation, 0);

            await UniTask.WhenAll(
                _meshRendererR.material.DOFloat(1, Blend, _changeDurationSeconds).AsyncWaitForCompletion().AsUniTask(),
                _meshRendererL.material.DOFloat(1, Blend, _changeDurationSeconds).AsyncWaitForCompletion().AsUniTask()/*,
            _headParentTransform.DORotate(new Vector3(0, newSphereRotation, 0), _rotationDurationSeconds).AsyncWaitForCompletion().AsUniTask()*/
            );

            if(!clearTextures) return;
        
            var texR = _meshRendererR.material.GetTexture(MainTex) as Texture2D;
            var texL = _meshRendererL.material.GetTexture(MainTex) as Texture2D;

            if (texR != _defaultTextureR) GraphicsLoaderCache.ClearFromCache(texR);
            if (texL != texR && texL != _defaultTextureL) GraphicsLoaderCache.ClearFromCache(texL);
        
            GC.Collect();
        }
    
#if UNITY_EDITOR
        // "Редактор" P - вкл/вкл интерфейс редактора, Delete - удалить активную кнопку, Space - создать новую кнопку, Shift - "развернуть" все кнопки на экране
        [HorizontalLine]
        [SerializeField] private GUIStyle _currentSceneStyle;
    
        [SerializeField] [TextArea] private string _apartmentJson;

        private SceneButton _editButton;
    
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
                _showImgui = !_showImgui;
        
            if(_currentPanorama == null) return;
        
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var button = Instantiate(_roomPointButtonPrefab, _roomPointButtonsContainer);
            
                button.transform.localPosition = new Vector3(_headTransform.position.x, _headTransform.position.y, _headTransform.position.z);
                _headTransform.localEulerAngles = new Vector3(Input.GetKey(KeyCode.LeftControl) ? _headTransform.localEulerAngles.x : 0, _headTransform.localEulerAngles.y, 0);
            
                button.transform.Translate(_headTransform.forward.normalized * 5f, Space.World);
                button.transform.LookAt(_headTransform);

                var buttonWithText = button.GetComponentInChildren<ButtonWithText>();
                buttonWithText.SetText("BUTTON " + _currentPanorama.Buttons.Count);

                button.name = buttonWithText.Text;

                SceneButton sceneButton = new SceneButton
                {
                    GotoPanorama = "???",
                    Position = button.transform.localPosition,
                    Text = buttonWithText.Text
                };

                _editButton = sceneButton;
            
                _currentPanorama.Buttons.Add(sceneButton);
            
                _apartmentJson = JsonConvert.SerializeObject(_currentApartment, Formatting.Indented);
            }
        
            if(_editButton == null || _currentPanorama?.Buttons == null || _currentPanorama.Buttons.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                var buttons = FindObjectsOfType<HoverRoomPointButton>();

                foreach (var button in buttons)
                    button.OnPointerEnter(null);
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                var buttons = FindObjectsOfType<HoverRoomPointButton>();

                foreach (var button in buttons)
                    button.OnPointerExit(null);
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (_currentPanorama.Buttons.Count > 0)
                {
                    if(_panoramaButtonsDictonary.TryGetValue(_editButton, out var buttonGameObject))
                        Destroy(buttonGameObject);
                
                    _currentPanorama.Buttons.Remove(_editButton);

                    if (_currentPanorama.Buttons.Count > 0)
                    {
                        _editButton = _currentPanorama.Buttons.Last();
                    }
                    else
                    {
                        _editButton = null;
                    }
                
                    _apartmentJson = JsonConvert.SerializeObject(_currentApartment, Formatting.Indented);
                
                    GotoPanorama(_currentPanorama, 0);
                }
            }
        }
    
        private readonly Rect _rectCurrent = new Rect(Screen.width / 2 - 100, 10, 200, 24);
    
        // выводим подсказу как подрубить контроллер в режиме симуляции (галка _sameAsDeviceControllerBehaviour)
        private void OnGUI()
        {
            if(!_showImgui) return;
        
            if(_currentApartment?.Panoramas == null || _currentApartment.Panoramas.Count == 0) return;
        
            GUI.Box(_rectCurrent, _currentPanoramaKey, _currentSceneStyle);
        
            GUI.Box(new Rect(8, 10, 124, _currentApartment.Panoramas.Count * 24 + 26), string.Empty);
            GUI.Label(new Rect(8, 10, 124, 24), "PANORAMAS", _currentSceneStyle);

            int index = 0;
        
            foreach (var panorama in _currentApartment.Panoramas)
            {
                var label = _currentPanorama == panorama.Value ? $"> <b>{panorama.Key}</b> <" : panorama.Key;
            
                if (GUI.Button(new Rect(10, 10 + index * 24 + 26, 120, 22), label))
                {
                    _currentPanoramaKey = panorama.Key;
                    GotoPanorama(panorama.Value, 0);
                }
            
                index++;
            }
        
            if(_currentPanorama?.Buttons == null || _currentPanorama.Buttons.Count == 0) return;

            _editButton ??= _currentPanorama.Buttons.First();

            GUI.Box(new Rect(138, 10, 164, _currentPanorama.Buttons.Count * 24 + 180), string.Empty);
            GUI.Label(new Rect(138, 10, 164, 24), "BUTTONS", _currentSceneStyle);
        
            index = 0;

            foreach (var button in _currentPanorama.Buttons)
            {
                var label = _editButton == button ? $"<b>{button.Text}</b> > '{button.GotoPanorama}'" : $"{button.Text} > '{button.GotoPanorama}'";
            
                if (GUI.Button(new Rect(140, 10 + index * 24 + 26, 160, 22), label))
                {
                    _editButton = button;
                
                    var buttons = FindObjectsOfType<HoverRoomPointButton>();

                    foreach (var b in buttons)
                    {
                        if (b.gameObject.name.Equals(_editButton.Text))
                        {
                            b.OnPointerEnter(null);
                        }
                        else
                        {
                            b.OnPointerExit(null);
                        }
                    }
                }

                index++;
            }

            if (_editButton != null)
            {
                _editButton.Text = GUI.TextField(new Rect(140, 10 + index * 24 + 26 + 10, 160, 22), _editButton.Text);
                _editButton.GotoPanorama = GUI.TextField(new Rect(140, 10 + index * 24 + 26 + 34, 160, 22), _editButton.GotoPanorama);
            
                _editButton.Position.x = float.Parse(GUI.TextField(new Rect(140, 10 + (index + 1) * 24 + 60, 52, 22), _editButton.Position.x.ToString(CultureInfo.InvariantCulture)), NumberStyles.Any, CultureInfo.InvariantCulture);
                _editButton.Position.y = float.Parse(GUI.TextField(new Rect(194, 10 + (index + 1) * 24 + 60, 52, 22), _editButton.Position.y.ToString(CultureInfo.InvariantCulture)), NumberStyles.Any, CultureInfo.InvariantCulture);
                _editButton.Position.z = float.Parse(GUI.TextField(new Rect(248, 10 + (index + 1) * 24 + 60, 52, 22), _editButton.Position.z.ToString(CultureInfo.InvariantCulture)), NumberStyles.Any, CultureInfo.InvariantCulture);

                if (_editButton.HeadRotation.HasValue)
                {
                    _editButton.HeadRotation = int.Parse(GUI.TextField(new Rect(194, 10 + (index + 2) * 24 + 60, 52, 22),  _editButton.HeadRotation.Value.ToString(CultureInfo.InvariantCulture)), NumberStyles.Any, CultureInfo.InvariantCulture);
                }
            
                GUI.Label(new Rect(248, 10 + (index + 2) * 24 + 60, 52, 22), Mathf.RoundToInt(_headTransform.eulerAngles.y).ToString());

                if (GUI.Button(new Rect(140, 10 + (index + 2) * 24 + 60, 52, 22), "HEAD"))
                {
                    _editButton.HeadRotation = Mathf.RoundToInt(_spheresContainer.localEulerAngles.y - _headTransform.localEulerAngles.y);
                }
            
                if (GUI.Button(new Rect(160, 10 + (index + 2) * 24 + 84, 120, 22), $"GOTO '{_editButton.GotoPanorama}'"))
                {
                    if (_currentApartment.Panoramas.TryGetValue(_editButton.GotoPanorama, out var gotoPoint))
                    {
                        _currentPanoramaKey = _editButton.GotoPanorama;
                        GotoPanorama(gotoPoint, (int)((_editButton.HeadRotation ?? 0) - _headTransform.localEulerAngles.y));
                    }
                }
            
                if (GUI.Button(new Rect(160, 10 + (index + 2) * 24 + 108, 120, 22), "UPDATE"))
                {
                    _apartmentJson = JsonConvert.SerializeObject(_currentApartment, Formatting.Indented);
                    File.WriteAllText(Path.Combine(Application.dataPath, "editor.json"), _apartmentJson, Encoding.UTF8);
                
                    GotoPanorama(_currentPanorama, (int)_headParentTransform.eulerAngles.y, false);
                }
            }
        }
#endif
    }
}