using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts.UI.Loader
{
    public class LoaderAnimation : MonoBehaviour
    {
        public event Action OnRemoveCompleted;
    
        public enum BackgroundTransparency { OPAQUE, SEMI_TRANSPARENT, TRANSPARENT } // TRANSPARENT - показать только кружок-спиннер

        private static readonly Dictionary<BackgroundTransparency, float> Transparencies = new Dictionary<BackgroundTransparency, float>
        {
            { BackgroundTransparency.OPAQUE, 1 }, { BackgroundTransparency.SEMI_TRANSPARENT, 0.8f }, { BackgroundTransparency.TRANSPARENT, 0 }
        };

        [SerializeField] private CanvasGroup _canvasGroup;
    
        [SerializeField] private UIGradient.UIGradient _gradient;
        [SerializeField] private RectTransform _spinner;

        [SerializeField] private Image _image;
    
        [SerializeField] private float _fadeDuration = 1f;
        [SerializeField] private float _spinnerSpeed = -360;

        private BackgroundTransparency _backgroundTransparency;
        private bool _customBackgroundUsed = false;
    
        public bool Removing { get; private set; }

        private void Update()
        {
            if(_spinner.gameObject.activeSelf)
                _spinner.Rotate(0, 0,Time.deltaTime * _spinnerSpeed);
        
            if(_backgroundTransparency == BackgroundTransparency.TRANSPARENT || _customBackgroundUsed) return;
        
            _gradient.m_angle += Time.deltaTime * _spinnerSpeed;

            if (_gradient.m_angle > 180)
                _gradient.m_angle -= 360;
            else if (_gradient.m_angle < -180)
                _gradient.m_angle += 360;
        
            _image.SetVerticesDirty();
        }

        public void EnableSpinner(bool state)
        {
            _spinner.gameObject.SetActive(state);
        }

        public void Show(ILoaderAnimationAttachable control, BackgroundTransparency backgroundTransparency, Vector3 spinnerSize, Sprite customBackground = null)
        {
            control.OnRemoveLoaderRequest += Remove;

            _backgroundTransparency = backgroundTransparency;

            _spinner.localScale = spinnerSize;

            _image.enabled = backgroundTransparency != BackgroundTransparency.TRANSPARENT;
            _image.color = new Color(1, 1, 1, Transparencies[backgroundTransparency]);

            if (customBackground != null)
            {
                _customBackgroundUsed = true;
                _gradient.enabled = false;
            
                _image.sprite = customBackground;
            }

            _canvasGroup.alpha = 0;
            _canvasGroup.DOFade(1, _fadeDuration);
        }

        public void Remove(ILoaderAnimationAttachable control, bool withAnimation)
        {
            Removing = true;
        
            control.OnRemoveLoaderRequest -= Remove;
        
            _canvasGroup.DOFade(0, withAnimation ? _fadeDuration : 0).OnComplete(() =>
            {
                OnRemoveCompleted?.Invoke();
            
                Destroy(gameObject);
            });
        }
    }
}