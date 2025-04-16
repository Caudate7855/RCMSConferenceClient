using UnityEngine;
using VIAVR.Scripts.UI.Activators;

namespace VIAVR.Scripts.UI.ViewControls
{
    public abstract class ViewControls<T> : MonoBehaviour, IGroupElement<T>
    {
        // TODO переделать все ViewControlsы с использованием Initialized
        public bool Initialized { get; protected set; }
    
        public abstract void Activate(T data, bool withAnimation);
        public abstract void Deactivate(bool withAnimation);

        // тут можно управлять сменой текста на динамически создаваемых элементах страницы у которых нет предопределенных ключей для локализации
        public abstract void UpdateLocalization();
    }
}