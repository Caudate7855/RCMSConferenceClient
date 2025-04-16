using System.Collections.Generic;
using JetBrains.Annotations;

namespace UIManager.UISystem.UIManager
{
    [UsedImplicitly]
    public class UIManager : IUIManager
    {
        private List<object> _controllers = new List<object>();
        
#if EXTENJECT        
        private readonly Zenject.DiContainer _diContainer;
        public UIManager(Zenject.DiContainer diContainer)
        {
            _diContainer = diContainer;
        }
#endif
        public T Load<T>() where T : class
        {
            var potentialController = _controllers.Find(c => c is T);

            if (potentialController != null)
                return potentialController as T;

#if EXTENJECT
            var controller = _diContainer.Instantiate<T>();
#else 
            var controller = System.Activator.CreateInstance<T>();
#endif
            
            _controllers.Add(controller);
    
            return controller;
        }
    }
}