using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// класс элемента который может иметь кастомные состояния "активен/неактивен"
namespace VIAVR.Scripts.UI.Activators
{
    public class ActivatorBase : MonoBehaviour, IGroupElement<bool>
    {
        public event Action<bool> OnActivationChanged; 
    
        // связанные с объектом элементы (например его тень или текст под кнопкой), которые можно присвоить в эдиторе
        public List<ActivatorBase> _linkedActivators = new List<ActivatorBase>();

        public bool ActiveState { get; private set; }

        public virtual void Activate(bool state, bool withAnimation = false)
        {
            foreach (var activator in _linkedActivators.Where(activator => activator != this && activator != null))
                activator.Activate(state, withAnimation);

            ActiveState = state;
        
            OnActivationChanged?.Invoke(state);
        }

        // пройтись только по связанным активаторам
        public virtual void ActivateLinked(bool state, bool withAnimation = false)
        {
            foreach (var activator in _linkedActivators.Where(activator => activator != this && activator != null))
                activator.Activate(state, withAnimation);
        }

        public void Deactivate(bool withAnimation)
        {
            Activate(false, withAnimation);
        }
    }
}