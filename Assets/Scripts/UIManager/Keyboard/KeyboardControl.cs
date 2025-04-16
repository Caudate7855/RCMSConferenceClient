using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIManager.Keyboard
{
    public class KeyboardControl : MonoBehaviour
    {
        public enum Mode
        {
            LETTERS, SYMBOLS
        }
        
        [SerializeField] private int _maxSymbols = 50;

        [SerializeField] private GameObject _panelLetters;
        [SerializeField] private GameObject _panelSymbols;
        [Header("Buttons")]
        [SerializeField] private ButtonBase[] _buttonsBackspace;
        [SerializeField] private ButtonOnOffImageSwitch[] _buttonsShift;


        private ButtonBaseDataString[] _buttonsKeys;
        private Mode _mode;
        private string _printedString;
        private bool _shift = false;
        private bool _initialized;

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

                MakeAllLettersSmall();
            }
        }

        
        
        public event Action<string> OnStringSend;
        public event Action<string> OnStringUpdated;

        public void Initialize()
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
        
            foreach (var buttonShift in _buttonsShift)
                buttonShift.OnClick += state =>
                {
                    SwitchShift();
                };
        
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

            if (_panelSymbols == null)
                return;

            _panelSymbols.SetActive(_mode == Mode.SYMBOLS);
        }

        private void SwitchShift()
        {
            _shift = !_shift;

            foreach (var button in _buttonsKeys)
                button.ActivateLinked(_shift);
        }

        private async void MakeAllLettersSmall()
        {
            await UniTask.WaitUntil(() => _buttonsKeys != null);

            _shift = false;

            foreach (var button in _buttonsKeys)
                button.ActivateLinked(_shift);
        }
    }
}