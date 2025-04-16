using DG.Tweening;
using UnityEngine;

namespace VIAVR.Scripts.UI.Activators
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ActivatorSpriteFader : ActivatorBase
    {
        [SerializeField] private bool _useSetActive = true;
        [SerializeField] private float _fadeDuration = 1f;
        [SerializeField] private float _maxAlpha = 1f;
    
        private SpriteRenderer _spriteRenderer;
        private Tweener _tweener;
    
        public override void Activate(bool state, bool withAnimation = false)
        {
            base.Activate(state, withAnimation);
        
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_tweener != null)
            {
                _tweener.Kill();
                _tweener = null;
            }

            float fadeDuration = withAnimation ? _fadeDuration : 0;
        
            if (state)
            {
                if(_useSetActive)
                    gameObject.SetActive(true);

                if(withAnimation)
                    _tweener = _spriteRenderer.DOFade(_maxAlpha, fadeDuration);
            }
            else
            {
                if (withAnimation)
                {
                    _tweener = _spriteRenderer.DOFade(0, fadeDuration).OnComplete(() =>
                    {
                        if(_useSetActive)
                            gameObject.SetActive(false);
                    });
                }
                else
                {
                    if(_useSetActive)
                        gameObject.SetActive(false);
                }
            }
        }
    }
}