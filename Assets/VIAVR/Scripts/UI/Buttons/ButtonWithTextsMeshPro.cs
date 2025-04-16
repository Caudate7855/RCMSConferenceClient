using TMPro;
using UnityEngine;

namespace VIAVR.Scripts.UI.Buttons
{
    public class ButtonWithTextsMeshPro : ButtonBase
    {
        [SerializeField] private TextMeshProUGUI[] _texts;
        public override void Awake()
        {
            base.Awake();

            if (_texts == null)
                Debug.LogError("_text == null", gameObject);
        }

        public void SetText(params string[] text)
        {
            if (_texts == null)
            {
                Debug.LogError("_text == null", gameObject);
                return;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if(i >= _texts.Length) break;
            
                _texts[i].text = text[i];
                _texts[i].SetMaterialDirty();
            }
        }

        public string GetText(int index)
        {
            return index >= _texts.Length ? null : _texts[index].text;
        }
    }
}