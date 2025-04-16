using DG.Tweening;
using UnityEngine;

namespace UIManager.Windows.VideoWindow
{
    public class LoadingCircle : MonoBehaviour
    {
        private void OnEnable()
        {
            transform.DORotate(new Vector3(0, 0, -360), 2, RotateMode.FastBeyond360).SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }   
    }
}