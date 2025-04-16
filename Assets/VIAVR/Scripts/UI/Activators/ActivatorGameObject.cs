namespace VIAVR.Scripts.UI.Activators
{
    public class ActivatorGameObject : ActivatorBase
    {
        public override void Activate(bool state, bool withAnimation = false)
        {
            base.Activate(state, withAnimation);
        
            gameObject.SetActive(state);
        }
    }
}