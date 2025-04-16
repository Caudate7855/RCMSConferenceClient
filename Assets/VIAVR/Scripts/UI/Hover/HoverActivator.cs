using UnityEngine;
using VIAVR.Scripts.UI.Activators;

namespace VIAVR.Scripts.UI.Hover
{
    public class HoverActivator : HoverBase
    {
        [SerializeField] private ActivatorBase _activator;
    
        protected override void EnterAnimation()
        {
            _activator.Activate(true, true);
        }

        protected override void ExitAnimation()
        {
            _activator.Activate(false, true);
        }
    }
}