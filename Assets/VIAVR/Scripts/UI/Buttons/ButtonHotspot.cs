using TMPro;
using UnityEngine;

namespace VIAVR.Scripts.UI.Buttons
{
    public class ButtonHotspot : ButtonBaseData<string>
    {
        [SerializeField] private TextMeshProUGUI _ssidName;

        public ButtonHotspot Initialize(string ssidName)
        {
            Data = ssidName;
        
            _ssidName.text = ssidName;

            return this;
        }
    }
}