using UIManager.Keyboard;
using UnityEngine;
using UnityEngine.UI;

public class ButtonOnOffImageSwitch : ButtonBaseData<bool>
{
    [SerializeField] private bool _dontSwitchSprites;
    [SerializeField] private Image _image;
    [SerializeField] private Sprite _onSprite;
    [SerializeField] private Sprite _offSprite;
    [SerializeField] private bool _isOn;

    public override void Awake()
    {
        base.Awake();
        
        if (_image == null)
            _image = GetComponentInChildren<Image>();
        
        if (_image == null)
            Debug.Log("_image == null", gameObject);
        
        if(_onSprite == null)
            Debug.LogWarning("_onSprite == null", gameObject);
        
        if(_offSprite == null)
            Debug.LogWarning("_offSprite == null", gameObject);

        SetOnOffImageSprite(_isOn);
    }

    public void SetState(bool state)
    {
        Data = state;
        
        SetOnOffImageSprite(state);
    }

    private void SetOnOffImageSprite(bool state)
    {
        if(_dontSwitchSprites) return;
        
        _image.sprite = state ? _onSprite : _offSprite;
    }
    
    protected override void OnClickHandler()
    {
        SetState(!Data);

        base.OnClickHandler();
    }

}