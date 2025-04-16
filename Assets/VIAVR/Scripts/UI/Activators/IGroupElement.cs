namespace VIAVR.Scripts.UI.Activators
{
    public interface IGroupElement<T>
    {
        public void Activate(T data, bool withAnimation);
        public void Deactivate(bool withAnimation);
    }
}