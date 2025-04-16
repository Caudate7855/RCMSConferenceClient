using System;
using TMPro;
using UIManager;
using UnityEngine;
using UnityEngine.Rendering;

public class VerificationPopup : PopUpBase
{
    [SerializeField] private TMP_Text _codeText;
    [SerializeField] private ButtonBase _acceptButton;

    private string _verificationCode;
    
    public const int CodeLength = 4;

    public string VerificationCode
    {
        get => _verificationCode;
        set
        {
            if (value.Length != CodeLength)
                throw new ArgumentException($"Code length must be equal to {CodeLength}");
            else
                _verificationCode = value;
        }
    }

    private void Start()
    {
        _acceptButton.OnClick += Close;
    }

    protected override void OnOpen() => _codeText.text = VerificationCode;
}
