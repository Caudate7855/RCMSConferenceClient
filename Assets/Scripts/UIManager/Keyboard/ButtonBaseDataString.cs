using UnityEngine;

namespace UIManager.Keyboard
{
    public class ButtonBaseDataString : ButtonBaseData<string>
    {
        [SerializeField] private string _initialData;

        public override void Awake()
        {
            base.Awake();

            if(!string.IsNullOrEmpty(_initialData))
                Data = _initialData;
        }

        public override string GetData()
        {
            if (Data == null && _initialData != null)
                Data = _initialData;
        
            return Data;
        }
    }
}