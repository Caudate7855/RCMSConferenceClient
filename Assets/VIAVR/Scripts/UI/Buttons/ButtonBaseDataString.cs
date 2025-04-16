using UnityEngine;

namespace VIAVR.Scripts.UI.Buttons
{
    public class ButtonBaseDataString : ButtonBaseData<string>
    {
        [SerializeField] private string _initialData;

        public override void Awake()
        {
            base.Awake();

            Data = _initialData;
        }
    }
}