using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts.UI.Hover
{
    public class HoverPreviewItem : HoverBase
    {
        [SerializeField] private float _selfScaleAnimationDuration = 0.5f;
        [SerializeField] private float _previewScaleAnimationDuration = 1f;
        [SerializeField] private float _previewMoveToNormalDuration = 0.5f;

        [SerializeField] private float _movingSpeed = 5f;
        [SerializeField] private float _movingDistance = 50f;
    
        [SerializeField] private RectTransform _previewImageTransform;
        [SerializeField] private RectTransform _selfTransform;

        [SerializeField] private Image _playIcon;

        [SerializeField] private HoverValues<float> _selfScale = new HoverValues<float>(1, 1.05f);
        [SerializeField] private HoverValues<float> _previewScale = new HoverValues<float>(1, 1.2f);

        private Vector2? _normalSelfSizeDelta;
        private Vector2? _normalPreviewSizeDelta;
    
        private Vector2 _position;
    
        private bool _move;
        // при наведении на картинку скорость ее движения за курсором повышается постепенно
        private float _previewMoveAcceleration = 0; // иначе бывает эффект когда видны белые полосы на краях,
        // т.к. картинка двигается дальше чем надо не успев принять нужный размер с DOSizeDelta

        void FixedUpdate()
        {
            if (!_move) return;

            if (!CheckHover())
            {
                OnPointerExit(null);
                return;
            }
        
            if(_previewMoveAcceleration < 1f)
                _previewMoveAcceleration += Time.deltaTime * 2f;
            else
                _previewMoveAcceleration = 1f;

            if (GetUIElementLocalPointerPosition(out var point))
            {
                _position = point / _selfTransform.sizeDelta; // _position == центр (0,0) левый верхний угол (-1, 1) и т.д.
                _position.x = Mathf.Clamp(_position.x, -1f, 1f);
                _position.y = Mathf.Clamp(_position.y, -1f, 1f);

                _previewImageTransform.localPosition = 
                    Vector2.Lerp(_previewImageTransform.localPosition, _position * -_movingDistance, _movingSpeed * _previewMoveAcceleration * Time.deltaTime);
            }
        }

        protected override void EnterAnimation()
        {
            if(_move) return;
        
            _move = true;
        
            _normalSelfSizeDelta ??= _selfTransform.sizeDelta;
            _normalPreviewSizeDelta ??= _previewImageTransform.sizeDelta;

            _selfTransform.DOSizeDelta(_normalSelfSizeDelta.Value * _selfScale._hover, _selfScaleAnimationDuration);
            _previewImageTransform.DOSizeDelta(_normalPreviewSizeDelta.Value * _previewScale._hover, _previewScaleAnimationDuration);

            if(_playIcon != null)
                _playIcon.DOFade(1f, _animationDuration);
        }

        protected override void ExitAnimation()
        {
            _previewMoveAcceleration = 0;
        
            if(!_move) return;
        
            _move = false;
        
            if(_normalSelfSizeDelta.HasValue)
                _selfTransform.DOSizeDelta(_normalSelfSizeDelta.Value * _selfScale._normal, _selfScaleAnimationDuration);
        
            if(_normalPreviewSizeDelta.HasValue)
                _previewImageTransform.DOSizeDelta(_normalPreviewSizeDelta.Value * _previewScale._normal, _previewScaleAnimationDuration);
        
            _previewImageTransform.DOLocalMove(Vector3.zero, _previewMoveToNormalDuration);
        
            if(_playIcon != null)
                _playIcon.DOFade(0, _animationDuration);
        }

        // возвращает true если курсор на элементе, point это локальные координаты позиции курсора на UI элементе в пикселях где (0, 0) это центр элемента
        bool GetUIElementLocalPointerPosition(out Vector2 point)
        {
            var data = CurvedUIInputModule.Instance.GetLastPointerEventDataPublic(-1);

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_selfTransform, data.position, data.enterEventCamera, out point);
        }
    
        public void EnablePlayIcon(bool state)
        {
            if(_playIcon != null)
                _playIcon.gameObject.SetActive(state);
        }
    }
}