using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VIAVR.Scripts.Core;
using VIAVR.Scripts.Core.SerializableDictionary;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.UI;
using VIAVR.Scripts.UI.Pages;

namespace VIAVR.Scripts
{
    public class UIManager : MonoBehaviour
    {
        public event Action<PageBase, bool> OnPageOpenStateChanged; 
        public event Action<PageBase, bool> OnPageShowStateChanged; 
    
        [SerializeField] private SerializableDictionary<PageLayer, GameObject> _layerParents;
        [SerializeField] private TopPanel _topPanel;

        [SerializeField] private float _recenterUIFollowDelay = 1f;
        [SerializeField] private float _recenterUIFollowSpeed = 3f;
        [SerializeField] private Transform _popupsCanvas;
        [SerializeField] private Transform _recenterParent;
        [SerializeField] private Transform _head;
    
        private readonly Dictionary<Type, IPage> _pages = new Dictionary<Type, IPage>();
        private List<IPage> _activePages = new List<IPage>();
    
        private readonly List<MonoBehaviour> _recenterPauseFollowClients = new List<MonoBehaviour>();

        [ShowNativeProperty] private int ActivePagesCount => _activePages.Count;
    
        public string HelmetName
        {
            get => _topPanel.HelmetName;
            set => _topPanel.HelmetName = value;
        }
    
        public int HelmetPower {
            get => _topPanel.HelmetPower;
            set => _topPanel.HelmetPower = value;
        }
    
        private AppCore _appCore;
        private AppCore AppCore
        {
            get
            {
                if (_appCore == null)
                    _appCore = Singleton<AppCore>.Instance;
            
                return _appCore;
            }
        }

        public bool Initialized { get; private set; } = false;
    
        public void Initialize()
        {
            if(Initialized)
                return;

            InitializeAvailablePages();
        
            _topPanel.Initialize();
        
            _recenterParent.gameObject.SetActive(AppCore.UsingNoControllerMode);

            if (AppCore.UsingNoControllerMode)
                _popupsCanvas.SetParent(_recenterParent);

            AppCore.ControllersHandler.OnControllerConnectChanged += controllerState =>
            {
                switch (controllerState)
                {
                    case ControllersHandler.ControllerConnectState.CHANGED_TO_DISCONNECTED:
                        if (AppCore.UsingNoControllerMode)
                        {
                            _popupsCanvas.SetParent(_recenterParent);
                            _recenterParent.gameObject.SetActive(true);
                        }
                        break;
                
                    case ControllersHandler.ControllerConnectState.CHANGED_TO_CONNECTED:
                        if (AppCore.UsingNoControllerMode)
                        {
                            _popupsCanvas.SetParent(_head);
                            _recenterParent.gameObject.SetActive(false);
                        }
                        break;
                
                    default:
                        break;
                }
            };
        
            Initialized = true;
        }

        [ShowNonSerializedField] private float _recenterFollowDelay;

        private void Update()
        {
            if(_recenterFollowDelay > 0)
                _recenterFollowDelay -= Time.deltaTime;
        
            // recenter интерфейс поворачивается вместе с камерой
            if (!AppCore.UsingNoControllerMode || _recenterPauseFollowClients.Count > 0 || _recenterFollowDelay > 0) return;
        
            var angles = _recenterParent.eulerAngles;
            angles.y = Mathf.LerpAngle(angles.y, _head.transform.eulerAngles.y, Time.deltaTime * _recenterUIFollowSpeed);
            
            _recenterParent.eulerAngles = angles;
        }

        public void RequestPauseRecenterFollowView(MonoBehaviour client)
        {
            if (_recenterPauseFollowClients.Contains(client)) return;
        
            _recenterPauseFollowClients.Add(client);
            
            Debug.Log($"Request PauseRecenterFollowView from '{client.name}'");
        }
    
        public void RemovePauseRecenterFollowView(MonoBehaviour client)
        {
            if (!_recenterPauseFollowClients.Contains(client)) return;
        
            _recenterPauseFollowClients.Remove(client);

            if (_recenterPauseFollowClients.Count == 0)
            {
                _recenterFollowDelay = _recenterUIFollowDelay; // нужна задержка перед тем как интерфейс снова начнет двигаться, на время фейда
            }
            
            Debug.Log($"Remove PauseRecenterFollowView from '{client.name}'");
        }

        private void InitializeAvailablePages()
        {
            if (_layerParents == null || _layerParents.Count == 0)
            {
                Debug.LogError("UIManager.InitializeAvailablePages(): layers dictonary is null or empty");
                return;
            }
        
            // упрощенная система без конфигов, проходимся по всем объектам в слое окон
            foreach (var layer in _layerParents)
            {
                for (int i = 0; i < layer.Value.transform.childCount; i++)
                {
                    var pageObject = layer.Value.transform.GetChild(i);
            
                    var pageInterface = pageObject.GetComponent<IPage>();

                    var pageType = pageInterface.GetPageType();

                    if (!_pages.ContainsKey(pageType))
                    {
                        _pages.Add(pageType, pageInterface);
                    }
                    else
                    {
                        Destroy(pageObject.gameObject);
                
                        Debug.Log($"{Utils.ErrorPrefix}Page duplicate of type '{pageType}' destroyed!");
                    }
                }
            }
        
            // инициализация найденных окон
            foreach (var page in _pages)
            {
                var iPage = page.Value;
            
                // валидация
                if (!iPage.ValidatePage())
                {
                    Destroy(iPage.GetPageBase().gameObject);
                
                    Debug.Log($"{Utils.ErrorPrefix}Page '{page.Key}' not validated!");
                }
                else
                {
                    // окна при старте находятся в закрытом состоянии
                    iPage.InitializePage();
                    iPage.GetPageBase().gameObject.SetActive(false);
                
                    Debug.Log($"{Utils.OkPrefix}Page '{page.Key}' initialized!");
                }
            }
        }

        public void OpenPage<T>(bool onlyIfNotOpened = true) where T : PageBase
        {
            if (!GetPage<T>(out var page))
            {
                Debug.LogError($"Can't open page with type '{typeof(T)}'");
                return;
            }
        
            if(onlyIfNotOpened && IsPageActive<T>()) return;

            var pageBase = page.GetPageBase();
        
            GetPageLayer(pageBase)?.SetActive(true);
        
            if (_activePages.Contains(pageBase))
                _activePages.Remove(pageBase);

            _activePages.Add(pageBase);
        
            pageBase.OnPageOpen();

            // если нет активные страниц скрывающих другие слои
            if (!_activePages.Any(page1 => page1.GetPageBase().PageLayer != pageBase.PageLayer && page1.GetPageBase().HideOtherLayers) &&
                // если есть активные страницы с бОльшим приоритетом то не показываем открытую страницу
                !_activePages
                    .Where(activePage => activePage.GetPageBase() != pageBase && activePage.GetPageBase().Visible && activePage.GetPageBase().PageLayer == pageBase.PageLayer)
                    .Any(activePage1 => activePage1.GetPageBase().HideOtherPages && activePage1.GetPageBase().Priority > pageBase.Priority))
            {
                pageBase.OnPageShowInLayer();
            
                OnPageShowStateChanged?.Invoke(pageBase, true);
            }
            else
            {
                Debug.Log($"Can't show '{page.name}'");
            }
        
            OnPageOpenStateChanged?.Invoke(pageBase, true);

            UpdatePageStack(PageOperation.OPEN, page);
        }

        public void CloseAllPages(PageLayer pageLayer)
        {
            List<IPage> pages = new List<IPage>(_activePages);
        
            foreach (var activePage in pages)
            {
                if(activePage.GetPageBase().PageLayer != pageLayer) continue;
            
                ClosePage(activePage.GetPageBase());
            }

            pages = null;
        }

        public void ClosePage(PageBase pageBase)
        {
            if (pageBase == null)
            {
                Debug.LogError($"Can't close NULL page!");
                return;
            }

            if (!_activePages.Contains(pageBase))
            {
                //Debug.Log($"Page '{pageBase.name}' not active!");
                return;
            }
        
            _activePages.Remove(pageBase);
        
            pageBase.OnPageHideInLayer();
            pageBase.OnPageClose();
        
            OnPageOpenStateChanged?.Invoke(pageBase, false);
            OnPageShowStateChanged?.Invoke(pageBase, false);
        
            UpdatePageStack(PageOperation.CLOSE, pageBase);
        }
    
        public void ClosePage<T>() where T : PageBase
        {
            /*if (!GetPage<T>(out var page))
            {
                Debug.LogError($"Can't close page with type '{typeof(T)}'");
                return;
            }

            ClosePage(page.GetPageBase());*/
        }

        public bool IsPageActive<T>() where T : PageBase
        {
            return GetPage<T>(out var page) && _activePages.Contains(page);
        }
    
        public enum PageOperation
        {
            OPEN, CLOSE
        }

        /// <summary>
        /// рассчет видимости окон после того как одно из них открылось или закрылось
        /// </summary>
        /// <param name="targetPage"></param>
        public void UpdatePageStack(PageOperation pageOperation, PageBase targetPage) // TODO поддержка слоев и приоритетов
        {
            switch (pageOperation)
            {
                case PageOperation.CLOSE:
                
                    if (_activePages.Count > 0)
                    {
                        for (int i = _activePages.Count - 1; i >= 0; i--)
                        {
                            var page = _activePages[i];
                        
                            if(!targetPage.HideOtherLayers && page.GetPageBase().PageLayer != targetPage.PageLayer) continue;
                        
                            page.OnPageShowInLayer();
                        
                            OnPageShowStateChanged?.Invoke(page.GetPageBase(), true);
                        
                            if(!targetPage.HideOtherLayers && page.GetPageBase().HideOtherPages) break;
                        }
                    }
                    break;
            
                case PageOperation.OPEN:
                
                    for (int i = 0; i < _activePages.Count; i++)
                    {
                        var page = _activePages[i].GetPageBase();

                        if (targetPage.HideOtherPages && targetPage != page && (targetPage.HideOtherLayers || targetPage.PageLayer == page.PageLayer))
                        {
                            _activePages[i].OnPageHideInLayer();
                        
                            OnPageShowStateChanged?.Invoke(_activePages[i].GetPageBase(), false);
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(pageOperation), pageOperation, null);
            }
        }
    
        public bool GetPage<T>(out T page) where T : PageBase
        {
            page = default;

            if (!_pages.ContainsKey(typeof(T)))
                return false;
        
            page = (T)_pages[typeof(T)].GetPageBase();
            return true;
        }

        public T GetPage<T>() where T : PageBase
        {
            if (!_pages.ContainsKey(typeof(T)))
                return null;
        
            return (T)_pages[typeof(T)].GetPageBase();
        }

        private GameObject GetPageLayer(PageBase pageBase)
        {
            if (pageBase == null) return null;

            foreach (var layer in _layerParents)
                if (pageBase.PageLayer == layer.Key) return layer.Value;

            return null;
        }
    
        public void ShowWifiSignal(int wifiSignal)
        {
            _topPanel.WiFiSignalLevel = wifiSignal;
        }

        public void ShowNoWifiSignal()
        {
            _topPanel.WiFiSignalLevel = 0;
        }
    }
}