using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ActivatorTMPTextFont : ActivatorBase
{
    [SerializeField] private FontStyles _activeStyle;
    [SerializeField] private FontStyles _nonActiveStyle;
    [SerializeField] private TextMeshProUGUI _text;
    
    public override UniTask Activate(bool state, bool withAnimation = false)
    {
        base.Activate(state, withAnimation);

        _text.fontStyle = state ? _activeStyle : _nonActiveStyle;
        
        // при смене стиля ничего не происходит, SetAllDirty() не работает, но эта шляпа помогает:
        _text.enabled = false;
        _text.enabled = true;
        
        return UniTask.CompletedTask;
    }
}
