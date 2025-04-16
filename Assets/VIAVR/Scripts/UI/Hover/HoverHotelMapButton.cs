using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;

namespace VIAVR.Scripts.UI.Hover
{
    public class HoverHotelMapButton : HoverBase
    {
        [SerializeField] private GameObject _text;
        [SerializeField] private TextMeshProUGUI _textMesh;
        [SerializeField] private RectTransform _transform;

        [SerializeField] private Vector2 _idleSize;
        [SerializeField] private Vector2 _hoverSize;

        private TweenerCore<Vector2, Vector2, VectorOptions> _scaleTweenerCore;

        private int TextSize => _textMesh == null ? (int)_hoverSize.x : (int)(_textMesh.text.Length * (_textMesh.fontSize * .8f) + _textMesh.fontSize);
    
        protected override void EnterAnimation()
        {
            _scaleTweenerCore?.Kill();

            var textSizedHover = new Vector2(Mathf.Clamp(TextSize, _hoverSize.x, float.MaxValue), _hoverSize.y);
        
            _scaleTweenerCore = _transform.DOSizeDelta(textSizedHover, _animationDuration).OnComplete(() =>
            {
                _text.SetActive(true);
            });
        }

        protected override void ExitAnimation()
        {
            _scaleTweenerCore?.Kill();

            _text.SetActive(false);
        
            _scaleTweenerCore = _transform.DOSizeDelta(_idleSize, _animationDuration);
        }
    }
}