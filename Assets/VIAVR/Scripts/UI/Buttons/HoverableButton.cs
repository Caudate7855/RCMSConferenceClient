using UnityEngine.EventSystems;

namespace VIAVR.Scripts.UI.Buttons
{
    public class HoverableButton : ButtonBase, IPointerEnterHandler, IPointerExitHandler
    {
        // TODO: анимация OnPointerEnter/OnPointerLeft, чтобы "приподнималась" кнопка.
        public void OnPointerEnter(PointerEventData eventData) { }

        public void OnPointerExit(PointerEventData eventData) { }
    }
}