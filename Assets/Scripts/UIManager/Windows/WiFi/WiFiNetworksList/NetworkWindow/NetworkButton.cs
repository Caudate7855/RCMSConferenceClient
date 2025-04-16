using System;
using TMPro;
using UnityEngine;

namespace UIManager.Windows
{
    public class NetworkButton : MonoBehaviour
    {
        private TMP_Text _networkName;
        private ButtonBase _button;
        
        public ButtonBase Button => _button;
        
        public event Action<string> OnNetworkButtonPressed;

        private void Awake()
        {
            _button = gameObject.GetComponent<ButtonBase>();
            _networkName = GetComponentInChildren<TMP_Text>();
        }

        public void SetNetworkName(string newName) => _networkName.text = newName;

        private void OnEnable() => _button.OnClick += OnButtonPressedHandler;

        private void OnDisable() => _button.OnClick -= OnButtonPressedHandler;

        private void OnButtonPressedHandler() => OnNetworkButtonPressed?.Invoke(_networkName.text);
    }
}