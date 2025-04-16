using DG.Tweening;
using UnityEngine;

namespace VIAVR.Scripts.UI.Hover
{
    public class HoverRoomPointButton : HoverBase
    {
        [SerializeField] private GameObject _text;
        [SerializeField] private GameObject _effect;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [SerializeField] private float _idleSize;
        [SerializeField] private float _hoverSize;

        private Tweener _tweener;
    
        protected override void EnterAnimation()
        {
            _tweener?.Kill();

            float y = _spriteRenderer.size.y;
        
            _effect.SetActive(false);
        
            _tweener = DOTween.To(value => _spriteRenderer.size = new Vector2(value, y), _idleSize, _hoverSize, _animationDuration).OnComplete(() =>
            {
                _text.SetActive(true);
            });
        }

        protected override void ExitAnimation()
        {
            _tweener?.Kill();

            _text.SetActive(false);
        
            float y = _spriteRenderer.size.y;
        
            _tweener = DOTween.To(value => _spriteRenderer.size = new Vector2(value, y), _hoverSize, _idleSize, _animationDuration).OnComplete(() =>
            {
                _effect.SetActive(true);
            });
        }

        public void SetHoverState()
        {
        
        }
    }
}