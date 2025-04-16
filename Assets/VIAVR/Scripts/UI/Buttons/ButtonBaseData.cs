using System;

namespace VIAVR.Scripts.UI.Buttons
{
    public class ButtonBaseData<T> : ButtonBase
    {
        public T Data { get; set; }
    
        public event Action<T> OnClick;
    
        protected override void OnClickHandler()
        {
            OnClick?.Invoke(Data);
        }
    }
}