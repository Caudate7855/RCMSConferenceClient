using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace VIAVR.Scripts.UI.Activators
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupFaderActivator : ActivatorBase
    {
        [SerializeField] private bool _useSetActive = true;
        [SerializeField] private float _fadeDuration = .3f;

        public float FadeDuration => _fadeDuration;
    
        private CanvasGroup _canvasGroup;
    
        public override void Activate(bool state, bool withAnimation = false)
        {
            base.Activate(state, withAnimation);
        
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            float fadeDuration = withAnimation ? _fadeDuration : 0;
        
            if (state)
            {
                if (_canvasGroup.alpha < 1f || !gameObject.activeSelf)
                {
                    gameObject.SetActive(true);

                    if (fadeDuration > 0)
                    {
                        _canvasGroup.alpha = 0;
                        _canvasGroup.DOFade(1f, fadeDuration);
                    }
                    else
                    {
                        _canvasGroup.alpha = 1;
                    }
                }
            }
            else
            {
                if (_canvasGroup.alpha > 0 || (_useSetActive && gameObject.activeSelf))
                {
                    if(fadeDuration > 0)
                    {
                        _canvasGroup.alpha = 1f;
                    
                        _canvasGroup.DOFade(0, fadeDuration).OnComplete(() =>
                        {
                            if (_useSetActive)
                                gameObject.SetActive(false);
                        });
                    }
                    else
                    {
                        _canvasGroup.alpha = 0;
                    
                        if(_useSetActive)
                            gameObject.SetActive(false);
                    }
                }
            }
        }
    
        public async UniTask ActivateAsync(bool state, bool withAnimation = false)
        {
            base.Activate(state, withAnimation);
        
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            float fadeDuration = withAnimation ? _fadeDuration : 0;
        
            if (state)
            {
                if (_canvasGroup.alpha < 1f || !gameObject.activeSelf)
                {
                    gameObject.SetActive(true);

                    if (fadeDuration > 0)
                    {
                        _canvasGroup.alpha = 0;
                        await _canvasGroup.DOFade(1f, fadeDuration).AsyncWaitForCompletion();
                    }
                    else
                    {
                        _canvasGroup.alpha = 1;
                    }
                }
            }
            else
            {
                if (_canvasGroup.alpha > 0 || (_useSetActive && gameObject.activeSelf))
                {
                    if(fadeDuration > 0)
                    {
                        _canvasGroup.alpha = 1f;
                    
                        await _canvasGroup.DOFade(0, fadeDuration).OnComplete(() =>
                        {
                            if (_useSetActive)
                                gameObject.SetActive(false);
                        }).AsyncWaitForCompletion();
                    }
                    else
                    {
                        _canvasGroup.alpha = 0;
                    
                        if(_useSetActive)
                            gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}