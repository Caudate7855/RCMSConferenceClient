using System;
using UnityEngine;
using VIAVR.Scripts.UI.Buttons;

namespace VIAVR.Scripts.UI
{
    public class KeyboardControl : MonoBehaviour
    {
        public enum Mode
        {
            LETTERS, SYMBOLS
        }
    
        public event Action<string> OnStringSend;
        public event Action<string> OnStringUpdated;

        [SerializeField] private int _maxSymbols = 50;

        [SerializeField] private GameObject _panelLetters;
        [SerializeField] private GameObject _panelSymbols;

        [Header("Buttons")]
        [SerializeField] private ButtonBase[] _buttonsBackspace;
        [SerializeField] private ButtonBase[] _buttonsEnter;
        [SerializeField] private ButtonBase[] _buttonSwitchSymbols;
        [SerializeField] private ButtonOnOffImageSwitch[] _buttonsShift;

        private ButtonBaseDataString[] _buttonsKeys;

        private Mode _mode;
    
        private bool _shift = false; // режим Шифта

        private string _printedString;

        public string PrintedString
        {
            get => _printedString;
            set
            {
                string lastString = _printedString;
            
                _printedString = value;

                if (_printedString.Length > _maxSymbols)
                    _printedString = lastString;
            
                OnStringUpdated?.Invoke(_printedString);
            }
        }

        private bool _initialized;

        public void Initialize(bool canUseSymbols = true)
        {
            PrintedString = "";
        
            if(_initialized) return;
        
            _initialized = true;

            _buttonsKeys = GetComponentsInChildren<ButtonBaseDataString>();

            foreach (var key in _buttonsKeys)
            {
                key.OnClick += character =>
                {
                    if (_shift)
                        character = character.ToUpper();
                
                    PrintedString += character;
                };
            }

            foreach (var buttonBackspace in _buttonsBackspace)
                buttonBackspace.OnClick += () =>
                {
                    if (PrintedString.Length > 0)
                        PrintedString = PrintedString.Remove(PrintedString.Length - 1);
                };

            foreach (var buttonEnter in _buttonsEnter)
                buttonEnter.OnClick += () =>
                {
                    OnStringSend?.Invoke(PrintedString);
                };

            foreach (var buttonShift in _buttonsShift)
                buttonShift.OnClick += state =>
                {
                    SwitchShift();
                };
        
            foreach (var buttonSymbol in _buttonSwitchSymbols)
            {
                buttonSymbol.OnClick += () =>
                {
                    SwitchMode();
                };
            
                buttonSymbol.Interactable = canUseSymbols;
            }
        
            PrintedString = "";

            SwitchMode(Mode.LETTERS);
        }

        public void Clear()
        {
            PrintedString = "";
        }

        void SwitchMode(Mode? mode = null)
        {
            if (mode.HasValue)
                _mode = mode.Value;
            else
                _mode = _mode == Mode.SYMBOLS ? Mode.LETTERS : Mode.SYMBOLS;

            _panelLetters.SetActive(_mode == Mode.LETTERS);
            _panelSymbols.SetActive(_mode == Mode.SYMBOLS);
        }

        void SwitchShift()
        {
            _shift = !_shift;

            foreach (var button in _buttonsKeys)
                button.ActivateLinked(_shift);
        
            foreach (var button in _buttonsShift)
                button.SetState(_shift);
        }
    }
}