using DG.Tweening;
using UnityEngine;

namespace VIAVR.Scripts.UI.Hover
{
    public enum SizeChangeMethod
    {
        DOSCALE, DOSIZEDELTA
    }

    public class HoverCommonButtonOptionalShadow : HoverBase
    {
        [Header(nameof(HoverCommonButtonOptionalShadow))]

        [Tooltip("Для _selfTransform")]
        [SerializeField] private SizeChangeMethod _sizeChangeMethod = SizeChangeMethod.DOSIZEDELTA;
    
        [SerializeField] private RectTransform _selfTransform; // Button который ловит эвент OnPointerEnter/Exit обычно является составным объектом, в поле указываем объект который содержит все элементы кнопки
    
        [Tooltip("Shadow Transform можно не ставить")]
        [SerializeField] private RectTransform _shadowTransform;
    
        [SerializeField] private HoverValues<float> _scaleXY = new HoverValues<float>(1, 1.2f);
        [SerializeField] private HoverValues<float> _moveZ = new HoverValues<float>(0, -10);
        [SerializeField] private HoverValues<float> _shadowMoveY = new HoverValues<float>(0, -4);
        [SerializeField] private HoverValues<float> _shadowScaleXY = new HoverValues<float>(1, 1.04f);

        private Vector2? _normalSelfSizeDelta;
        private Vector2? _normalShadowSizeDelta;
        private Vector2? _normalShadowLocalPosition;

        protected override void EnterAnimation()
        {
            _normalSelfSizeDelta ??= _selfTransform.sizeDelta;

            switch (_sizeChangeMethod)
            {
                case SizeChangeMethod.DOSCALE:
                    _selfTransform.DOScale(_scaleXY._hover, _animationDuration);
                    break;
                case SizeChangeMethod.DOSIZEDELTA:
                    _selfTransform.DOSizeDelta(_normalSelfSizeDelta.Value * _scaleXY._hover, _animationDuration);
                    break;
                default:
                    Debug.LogError($"{nameof(_sizeChangeMethod)} out of switch range!");
                    break;
            }

            if(_moveZ._hover != _moveZ._normal)
                _selfTransform.DOLocalMoveZ(_moveZ._hover, _animationDuration);     // двигаем кнопку по направлению к юзеру
        
            if(_shadowTransform == null) return;
        
            _normalShadowSizeDelta ??= _shadowTransform.sizeDelta;
            _normalShadowLocalPosition ??= _shadowTransform.localPosition;

            _shadowTransform.DOSizeDelta(_normalShadowSizeDelta.Value * _shadowScaleXY._hover, _animationDuration);     // увеличиваем размер тени
        
            if(_shadowTransform.parent == _selfTransform && (_moveZ._hover != _moveZ._normal))
                _shadowTransform.DOLocalMoveZ(-_moveZ._hover, _animationDuration);                                      // чтоб оставить тень "на месте" надо двигать ее в противоположном направлении
        
            _shadowTransform.DOLocalMoveY(_normalShadowLocalPosition.Value.y + _shadowMoveY._hover, _animationDuration);// смещаем тень вниз
        }

        protected override void ExitAnimation()
        {
            switch (_sizeChangeMethod)
            {
                case SizeChangeMethod.DOSCALE:
                    _selfTransform.DOScale(_scaleXY._normal, _animationDuration);
                    break;
                case SizeChangeMethod.DOSIZEDELTA:
                    if(_normalSelfSizeDelta.HasValue)
                        _selfTransform.DOSizeDelta(_normalSelfSizeDelta.Value, _animationDuration);
                    break;
                default:
                    Debug.LogError($"{nameof(_sizeChangeMethod)} out of switch range!");
                    break;
            }

            if(_moveZ._hover != _moveZ._normal)
                _selfTransform.DOLocalMoveZ(_moveZ._normal, _animationDuration);
        
            if(_shadowTransform == null) return;
        
            if(_normalShadowSizeDelta.HasValue)
                _shadowTransform.DOSizeDelta(_normalShadowSizeDelta.Value, _animationDuration);
        
            if(_shadowTransform.parent == _selfTransform && (_moveZ._hover != _moveZ._normal))
                _shadowTransform.DOLocalMoveZ(_moveZ._normal, _animationDuration);
        
            if(_normalShadowLocalPosition.HasValue)
                _shadowTransform.DOLocalMoveY(_normalShadowLocalPosition.Value.y + _shadowMoveY._normal, _animationDuration);
        }
    }
}