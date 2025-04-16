using UnityEngine;

namespace VIAVR.Scripts.UI.Buttons
{
    public class ButtonBaseDataInt : ButtonBaseData<int>
    {
        [SerializeField] private int _initialData;

        public override void Awake()
        {
            base.Awake();

            Data = _initialData;
        }
    }
}