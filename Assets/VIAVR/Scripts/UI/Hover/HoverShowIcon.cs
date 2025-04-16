using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VIAVR.Scripts.UI.Activators;

namespace VIAVR.Scripts.UI.Hover
{
    public class HoverShowIcon : HoverBase
    {
        [SerializeField] private ActivatorBase[] _hoverIfActive; // ховер сработает только если эти элементы активны
        [SerializeField] private ActivatorBase[] _hoverIfNonActive; // ховер срадботает только если эти элементы не активны
    
        [SerializeField] private Image _icon;
    
        protected override void EnterAnimation()
        {
            if (_hoverIfActive.Any(checkActive => !checkActive.ActiveState))
                return;

            if (_hoverIfNonActive.Any(checkNonActive => checkNonActive.ActiveState))
                return;

            _icon.DOFade(1f, _animationDuration);
        }

        protected override void ExitAnimation()
        {
            _icon.DOFade(0f, _animationDuration);
        }
    }
}