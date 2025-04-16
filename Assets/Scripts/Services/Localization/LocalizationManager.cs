using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Services
{
    public static class LocalizationManager
    {
        private static bool _isInitialized;
        private static LocalizationLanguages _currentLocalizationLanguage = LocalizationLanguages.Russian;
        private static Dictionary<string, string> _englishLocalizedTexts = new Dictionary<string, string>();
        private static Dictionary<string, string> _russianLocalizedTexts = new Dictionary<string, string>();
        
        private const string LocalizationFilePath = "Localization";
        private const string LocalizationIndexEn = "EN";
        private const string LocalizationIndexRu = "RU";

        public static async UniTask<string> GetLocalizedTextAsync(string localizationCode)
        {
            if (!_isInitialized)
                await InitializeAsync();

            if (_currentLocalizationLanguage == LocalizationLanguages.English)
            {
                if (_englishLocalizedTexts.TryGetValue(localizationCode, out var text))
                    return text;
            }
            else
            {
                if (_russianLocalizedTexts.TryGetValue(localizationCode, out var text))
                    return text;
            }

            throw new Exception("Cannot find localized text");
        }

        public static void ChangeLanguage(LocalizationLanguages newLanguage)
        {
            _currentLocalizationLanguage = newLanguage;
            
            var localizableTexts = GameObject.FindObjectsOfType<LocalizableText>();

            for (int i = 0, count = localizableTexts.Length; i < count; i++)
            {
                if (localizableTexts[i].IsLocalizable)
                    localizableTexts[i].UpdateText();
            }
        }

        private static async UniTask InitializeAsync()
        {
            if (_isInitialized) 
                return;

            _englishLocalizedTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(GetLocalizationFile(LocalizationIndexEn));
            _russianLocalizedTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(GetLocalizationFile(LocalizationIndexRu));

            _isInitialized = true;

            await UniTask.Yield();
        }

        private static string GetLocalizationFile(string localizationIndex)
        {
            var files = Resources.LoadAll<TextAsset>(LocalizationFilePath);
            var matchingFile = files.FirstOrDefault(file => file.name.StartsWith(localizationIndex));

            if (matchingFile != null)
                return matchingFile.text;

            throw new Exception("Cannot find file with index");
        }
    }
}
