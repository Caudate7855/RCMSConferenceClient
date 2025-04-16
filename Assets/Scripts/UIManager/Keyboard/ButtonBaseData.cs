using System;

namespace UIManager.Keyboard
{
    public class ButtonBaseData<T> : ButtonBase
    {
        public T Data { get; set; }
    
        public event Action<T> OnClick;
    
        protected override void OnClickHandler() => OnClick?.Invoke(Data);

        public virtual T GetData() { return Data; }
    }
}