using DG.Tweening;
using UnityEngine;

namespace VIAVR.Scripts.UI.Hover
{
    public class HoverCategoryPreview : HoverBase
    {
        [Header(nameof(HoverCategoryPreview))]
        [SerializeField] private RectTransform _frontTransform;
        [SerializeField] private RectTransform _middleTransform;
        [SerializeField] private RectTransform _backTransform;

        [SerializeField] private HoverValues<float> _movingDistance = new HoverValues<float>(0, 15f);

        private Vector3? _frontNormalTransform;
        private Vector3? _middleNormalTransform;
        private Vector3? _backNormalTransform;
        protected override void EnterAnimation()
        {
            _frontNormalTransform ??= _frontTransform.localPosition;
            _middleNormalTransform ??= _middleTransform.localPosition;
            _backNormalTransform ??= _backTransform.localPosition;

            _frontTransform.DOLocalMove(_frontNormalTransform.Value + new Vector3(-_movingDistance._hover, _movingDistance._hover), _animationDuration);
            _middleTransform.DOLocalMove(_middleNormalTransform.Value + new Vector3(_movingDistance._hover, -_movingDistance._hover), _animationDuration);
            _backTransform.DOLocalMove(_backNormalTransform.Value + new Vector3(_movingDistance._hover, -_movingDistance._hover) * 2f, _animationDuration);
        }

        protected override void ExitAnimation()
        {
            if(_frontNormalTransform.HasValue)
                _frontTransform.DOLocalMove(_frontNormalTransform.Value, _animationDuration);
        
            if(_middleNormalTransform.HasValue)
                _middleTransform.DOLocalMove(_middleNormalTransform.Value, _animationDuration);
        
            if(_backNormalTransform.HasValue)
                _backTransform.DOLocalMove(_backNormalTransform.Value, _animationDuration);
        }
    }
}