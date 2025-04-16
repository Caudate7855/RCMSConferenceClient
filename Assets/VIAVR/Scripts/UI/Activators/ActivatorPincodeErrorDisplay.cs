using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts.UI.Activators
{
    public class ActivatorPincodeErrorDisplay : ActivatorBase
    {
        [InfoBox("Activate(true) - показать что пинкод неверный")]
    
        [SerializeField] private Color _activatedColor = Color.red;
        [SerializeField] private Color _nonActivatedColor = Color.black;
        [SerializeField] private Color _nonActivatedColorBorder = Color.gray;

        [SerializeField] private Image[] _images;
        [SerializeField] private TextMeshProUGUI[] _texts;

        public override void Activate(bool state, bool withAnimation = false)
        {
            base.Activate(state, withAnimation);
        
            foreach (var text in _texts)
                text.color = state ? _activatedColor : _nonActivatedColor;
        
            foreach (var image in _images)
                image.color = state ? _activatedColor : _nonActivatedColorBorder;
        }
    }
}