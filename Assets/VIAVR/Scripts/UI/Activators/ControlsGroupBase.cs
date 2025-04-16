using System.Linq;
using UnityEngine;

// Класс для группы элементов в которой при SelectElement выбранный элемент активируется, а остальные элементы группы деактивируются
namespace VIAVR.Scripts.UI.Activators
{
    public class ControlsGroupBase<T, D> : MonoBehaviour where T : IGroupElement<D>
    {
        [SerializeField] protected T[] _controls;

        private T _activeElement;

        public virtual void ActivateElement(T element, D data, bool withAnimation, bool force = false)
        {
            if(!force && _activeElement != null && _activeElement.Equals(element)) return;
        
            _activeElement = element;
        
            foreach (var control in _controls)
            {
                if(!control.Equals(element))
                    control.Deactivate(withAnimation);
            }
        
            element?.Activate(data, withAnimation);
        }
    
        public virtual void SetControls(params T[] controls)
        {
            _controls = controls;
        }

        public virtual void AddControl(T control)
        {
            _controls = _controls == null
                ? new[] { control }
                : _controls.Append(control).ToArray();
        }
    }
}