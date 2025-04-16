using TMPro;
using UnityEngine;
using VIAVR.Scripts.Data;

namespace VIAVR.Scripts.UI.Buttons
{
    public class ButtonBaseHotelData : ButtonBaseData<VrTourGroup>
    {
        [SerializeField] private TextMeshProUGUI _text;

        public void SetData(VrTourGroup data)
        {
            Data = data;
        
            if(data != null && _text != null)
                _text.text = data.Name;
        }
    }
}