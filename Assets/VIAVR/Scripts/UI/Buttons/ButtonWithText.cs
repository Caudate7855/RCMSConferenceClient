using TMPro;
using UnityEngine;

namespace VIAVR.Scripts.UI.Buttons
{
    public class ButtonWithText : ButtonBase
    {
        [SerializeField] private TextMeshPro _text;
    
        public string Text => _text.text;
    
        public override void Awake()
        {
            base.Awake();

            if (_text == null)
                Debug.LogError("_text == null", gameObject);
        }

        public void SetText(string text)
        {
            if (_text == null)
            {
                Debug.LogError("_text == null", gameObject);
                return;
            }

            _text.text = text;
            _text.SetAllDirty();
        }
    }
}