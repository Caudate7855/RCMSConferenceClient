using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts.UI.Buttons
{
    ///OnClick is Action&lt;bool&gt;
    public class ButtonOnOffImageSwitch : ButtonBaseData<bool>
    {
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

        void SetOnOffImageSprite(bool state)
        {
            _image.sprite = state ? _onSprite : _offSprite;
        }
    
        protected override void OnClickHandler()
        {
            SetState(!Data);

            base.OnClickHandler();
        }
    }
}