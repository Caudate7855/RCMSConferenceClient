using TMPro;
using UnityEngine;

namespace Services
{
    public class LocalizableText : MonoBehaviour
    {
        public bool IsLocalizable = true;
        
        [SerializeField] private string _localizationCode;
        
        private TMP_Text _text;

        private async void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _text.text = await LocalizationManager.GetLocalizedTextAsync(_localizationCode);
        }

        private async void OnEnable() => _text.text = await LocalizationManager.GetLocalizedTextAsync(_localizationCode);

        public async void UpdateText() => _text.text = await LocalizationManager.GetLocalizedTextAsync(_localizationCode);
    }
}