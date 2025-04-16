namespace UIManager.UISystem.UIManager
{
    public interface IUIManager
    {
        public T Load<T>() where T : class;
    }
}