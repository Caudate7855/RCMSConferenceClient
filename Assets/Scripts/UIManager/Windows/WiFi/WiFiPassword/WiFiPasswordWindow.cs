using DG.Tweening;
using TMPro;
using UIManager.Keyboard;
using UIManager.UISystem.Abstracts;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager.Windows
{
    public class WiFiPasswordWindow : UIViewBase
    {
        [SerializeField] private KeyboardControl _keyboardControl;
        [SerializeField] private ButtonBase _backButton;
        [SerializeField] private TMP_Text _password;
        [SerializeField] private ButtonBase _enterPasswordButton;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private Image _loader;

        public KeyboardControl KeyboardControl => _keyboardControl;
        public TMP_Text Password => _password;
        public TMP_Text ErrorText => _errorText;

        public ButtonBase BackButton=> _backButton;
        public ButtonBase EnterPasswordButton => _enterPasswordButton;

        private void OnEnable() => _keyboardControl.OnStringUpdated += UpdatePasswordText;

        private void OnDisable() => _keyboardControl.OnStringUpdated -= UpdatePasswordText;

        private void UpdatePasswordText(string newText) => _password.text = newText;

        public void StartLoading()
        {
            _enterPasswordButton.Interactable = false;

            _loader.transform.rotation = Quaternion.identity;
            _loader.gameObject.SetActive(true);

            _loader.transform.DORotate(new Vector3(0, 0, -360), 1, RotateMode.FastBeyond360).SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        public void StopLoading()
        {
            _enterPasswordButton.Interactable = true;

            _loader.transform.DOKill();
            _loader.gameObject.SetActive(false);
        }
    }
}