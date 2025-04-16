using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using VIAVR.Scripts.UI.Activators;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI
{
    public class PincodeControl : MonoBehaviour
    {
        private const string PINCODE_EMPTY_SYMBOL = "—";

        public event Action<string> OnPincodeSend;
        public event Action<string> OnPincodeFilled; // введено максимальное число символов (можно использовать для автоотправки)
    
        [ShowNativeProperty] public int MaxNumbers => _displayDigits?.Length ?? 0;
    
        [SerializeField] private TextMeshProUGUI[] _displayDigits;
        [SerializeField] private ButtonBaseData<int>[] _buttonsDigits;
    
        [SerializeField] private ActivatorPincodeErrorDisplay _activatorPincodeErrorDisplay;

        [SerializeField] private ButtonBase _buttonDelete;
        [SerializeField] private ButtonBase _buttonSend;

        private bool _uiInitialized = false;

        private bool _displayPincodeError;

        private string _pincode = "";
        private string Pincode
        {
            get => _pincode;

            set
            {
                string lastPincode = _pincode;
            
                _pincode = value;

                if (_pincode.Length > MaxNumbers)
                    _pincode = lastPincode;
                else if (_pincode.Length == MaxNumbers)
                    OnPincodeFilled?.Invoke(_pincode);
                else
                    HidePincodeNotValid();

                UpdatePincodeDisplay();
            }
        }

        public void Initialize()
        {
            Pincode = "";
        
            if(_uiInitialized) return;

            _uiInitialized = true;

            foreach (var button in _buttonsDigits)
                button.OnClick += ButtonDigitClickHandler;

            _buttonDelete.OnClick += () =>
            {
                if (Pincode.Length > 0)
                    Pincode = Pincode.Remove(Pincode.Length - 1);
            };

            _buttonSend.OnClick += () =>
            {
                Debug.Log("Пинкод: " + Pincode);
            
                OnPincodeSend?.Invoke(Pincode);
            };
        }
    
        public void Clear()
        {
            Pincode = "";
        }

        public bool Interactable {
            set {
                foreach (var buttonDigit in _buttonsDigits)
                    buttonDigit.Interactable = value;

                _buttonDelete.Interactable = value;
                _buttonSend.Interactable = value;
            }
        }

        public void DisplayPincodeValidation(bool isValid)
        {
            if(isValid)
                HidePincodeNotValid();
            else
                ShowPincodeNotValid();
        }

        // команда при неверном пинкоде показать красную обводочку и т.д.
        void ShowPincodeNotValid()
        {
            if(_displayPincodeError) return;

            _displayPincodeError = true;

            _activatorPincodeErrorDisplay.Activate(true);
        }
    
        // показать контролы в обычных цветах
        void HidePincodeNotValid()
        {
            if(!_displayPincodeError) return;

            _displayPincodeError = false;
        
            _activatorPincodeErrorDisplay.Activate(false);
        }

        void ButtonDigitClickHandler(int digit)
        {
            Pincode += digit;
        }

        void UpdatePincodeDisplay()
        {
            if (MaxNumbers > _displayDigits.Length)
                Debug.LogError($"_maxNumbers == {MaxNumbers} and _displayDigits.Length == {_displayDigits.Length} doesn't match!");

            for (var i = 0; i < MaxNumbers; i++)
            {
                if(i < _displayDigits.Length)
                    _displayDigits[i].text = i >= Pincode.Length ? PINCODE_EMPTY_SYMBOL : Pincode[i].ToString();
            }
        
            _buttonSend.Activate(Pincode.Length == MaxNumbers);
            _buttonDelete.Activate(Pincode.Length > 0);
        }
    }
}