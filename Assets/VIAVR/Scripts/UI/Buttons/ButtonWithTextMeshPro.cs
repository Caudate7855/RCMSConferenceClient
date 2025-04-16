using TMPro;
using UnityEngine;

namespace VIAVR.Scripts.UI.Buttons
{
    public class ButtonWithTextMeshPro : ButtonBase
    {
        [SerializeField] private TextMeshProUGUI _text;

        public TextMeshProUGUI TextMeshComponent => _text;
    
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
            _text.SetMaterialDirty();
        }

        public string GetText()
        {
            return _text.text;
        }
    }
}