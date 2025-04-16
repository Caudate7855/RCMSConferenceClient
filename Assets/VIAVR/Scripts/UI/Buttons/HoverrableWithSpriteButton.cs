using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts.UI.Buttons
{
    [RequireComponent(typeof(Image))]
    public class HoverrableWithSpriteButton : HoverableButton
    {
        Image _image;
        Image Image {
            get {
                if (_image == null)
                {
                    _image = GetComponent<Image>();

                    if (_image == null)
                        Debug.LogError($"{typeof(HoverrableWithSpriteButton)} has no Image.");
                }

                return _image;
            }
        }

        public void SetSprite(Sprite sprite)
        {
            Image.sprite = sprite;
        }
    }
}