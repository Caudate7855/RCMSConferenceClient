using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using VIAVR.Scripts.UI.Activators;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI.Hover
{
    [RequireComponent(typeof(ButtonBase))]
    public abstract class HoverBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Serializable]
        public struct HoverValues<T>
        {
            public T _normal;
            public T _hover;

            public HoverValues(T normal, T hover)
            {
                _normal = normal;
                _hover = hover;
            }
        }
    
        [SerializeField] private bool _hoverOffOnClick = false;
        [SerializeField] protected float _animationDuration = 0.2f;
    
        [SerializeField] private bool _invertStateForLinked;
    
        public List<ActivatorBase> _linkedActivators = new List<ActivatorBase>();
    
        private bool _hover;

        private ButtonBase _buttonBase;
        private ButtonBase ButtonBase
        {
            get
            {
                if (_buttonBase != null) 
                    return _buttonBase;
            
                _buttonBase = GetComponent<ButtonBase>();

                if (_buttonBase == null)
                {
                    Debug.Log($"GameObject must have {nameof(ButtonBase)} component!", gameObject);
                    return null;
                }
                
                _buttonBase.OnActivationChanged += state =>
                {
                    if(!state)
                        OnPointerExit(null);
                };

                return _buttonBase;
            }
        }
    
        protected bool CheckHover()
        {
            var data = CurvedUIInputModule.Instance.GetLastPointerEventDataPublic(-1);

            if (data == null) return false;

            return data.hovered != null && data.hovered.Contains(gameObject);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(_hover)
                return;
        
            if(ButtonBase == null || !ButtonBase.Button.interactable)
                return;

            _hover = true;

            EnterAnimation();
        
            foreach (var activator in _linkedActivators.Where(activator => activator != this && activator != null))
                activator.Activate(_invertStateForLinked ? false : true, _animationDuration > 0);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(!_hover)
                return;
        
            _hover = false;

            ExitAnimation();
        
            foreach (var activator in _linkedActivators.Where(activator => activator != this && activator != null))
                activator.Activate(_invertStateForLinked ? true : false, _animationDuration > 0);
        }
    
        public void OnPointerClick(PointerEventData eventData)
        {
            if(_hoverOffOnClick)
                OnPointerExit(null);
        }

        // В анимациях UI элементов под Курвед канвасом юзать DoSizeDelta вместо DoScale, DoScale всегда искажает элементы если смотреть на них под углом
        protected abstract void EnterAnimation();

        protected abstract void ExitAnimation();
    }
}