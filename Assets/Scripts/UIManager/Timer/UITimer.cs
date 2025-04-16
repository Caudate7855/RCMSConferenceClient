using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager.Windows
{
    public class UITimer : MonoBehaviour
    {
        public event Action OnTimerFinished;
        
        private enum CountType { Asc, Desc }
        
        [SerializeField] private int secondsAmount;
        [SerializeField] private CountType countType;
        [SerializeField] private Image displayImage;
        [SerializeField] private Sprite[] displaySprites;
        
        public async UniTask StartTimer(Action<Image> showAnimation = null)
        {
            if (countType == CountType.Asc)
            {
                for (int i = 0; i < secondsAmount; i++)
                {
                    displayImage.sprite = displaySprites[i];
                    if(showAnimation != null)
                        showAnimation(displayImage);
                    await UniTask.Delay(1000);
                }
            }
            else
            {
                for (int i = secondsAmount - 1; i >= 0; i--)
                {
                    displayImage.sprite = displaySprites[i];
                    if(showAnimation != null)
                        showAnimation(displayImage);
                    await UniTask.Delay(1000);
                }
            }
            OnTimerFinished?.Invoke();
        }
    }
}