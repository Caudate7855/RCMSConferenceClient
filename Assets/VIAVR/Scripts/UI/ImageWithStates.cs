using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts.UI
{
    [RequireComponent(typeof(Image))]
    public class ImageWithStates : MonoBehaviour
    {
        [SerializeField] private Sprite[] _statesSprite;
        [SerializeField] private bool _dontRoundToZero;

        private Image _image;

        public Image Image
        {
            get
            {
                if (_image != null)
                    return _image;
            
                _image = GetComponent<Image>();
        
                if (_image == null)
                {
                    Debug.Log("_image == null, adding Image component...", gameObject);
                    _image = gameObject.AddComponent<Image>();
                }

                return _image;
            }
        }

        public void SetByValue01(float value)
        {
            if(_statesSprite.Length < 1) return;

            float rawValue = Mathf.Clamp01(value) * (_statesSprite.Length - (_dontRoundToZero ? 1 : 0));
            int roundedValue = Mathf.CeilToInt(rawValue);

            int state = Mathf.Clamp(roundedValue, 0, _statesSprite.Length - 1);

            Image.sprite = _statesSprite[state];
        }
    }
}