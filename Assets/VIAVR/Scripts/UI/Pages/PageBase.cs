using System;
using NaughtyAttributes;
using UnityEngine;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.UI.Activators;

namespace VIAVR.Scripts.UI.Pages
{
    [Serializable]
    public enum PageLayer
    {
        DEFAULT, POPUP, STATIC_POPUP
    }

    public interface IPage
    {
        public bool InitializePage();

        public void OnPageOpen();
        public void OnPageClose();
    
        public void OnPageShowInLayer();
        public void OnPageHideInLayer();
    
        public bool ValidatePage();

        public Type GetPageType();
    
        public PageBase GetPageBase();
    }

    public class PageBase : MonoBehaviour, IPage
    {
        public const bool PAGE_INITIALIZED = true;
        public const bool PAGE_CONTINUE_INITIALIZATION = false;
    
        [Header(nameof(PageBase))]
        [SerializeField] protected PageLayer _pageLayer = PageLayer.DEFAULT;
        [SerializeField] protected bool _hideOtherPages;
        [SerializeField] protected bool _hideOtherLayers;
        [SerializeField] protected int _priority;
    
        protected CanvasGroupFaderActivator _activator;

        protected bool _initialized = false;
    
        private bool _opened = false;
        private bool _hidden = false;
        private bool _visible = false;

        protected AppCore _appCore;

        public bool HideOtherPages => _hideOtherPages;
        public bool HideOtherLayers => _hideOtherLayers;
        public int Priority => _priority;

        [ShowNativeProperty] public bool Opened => _opened;
        [ShowNativeProperty] public bool Hidden => _hidden;
        public bool Visible => gameObject.activeSelf;
    
        public PageLayer PageLayer => _pageLayer;
    
        public virtual bool InitializePage()
        {
            if(_initialized)
                return PAGE_INITIALIZED;

            _initialized = true;

            _appCore = Singleton<AppCore>.Instance;
        
            _activator = GetComponent<CanvasGroupFaderActivator>();
        
            gameObject.SetActive(true); // для правильной инициализации

            return PAGE_CONTINUE_INITIALIZATION;
        }

        public virtual void OnPageOpen()
        {
            _opened = true;
        }

        public virtual void OnPageClose()
        {
            _opened = false;
        }

        public void OnPageShowInLayer()
        {
            if(!_hidden)
                transform.SetAsLastSibling();
        
            _activator.Activate(true, true);
            //gameObject.SetActive(true);
            _hidden = false;
        }

        public void OnPageHideInLayer()
        {
            _activator.Activate(false, true);
            //gameObject.SetActive(false);
            _hidden = true;
        }

        public virtual bool ValidatePage()
        {
            return true;
        }

        public Type GetPageType()
        {
            return GetType();
        }

        public PageBase GetPageBase()
        {
            return this;
        }
    }
}