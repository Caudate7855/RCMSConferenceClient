using UnityEngine;

namespace UIManager.Windows
{
    public class CanvasRotator : MonoBehaviour
    {
        [SerializeField] private Camera _headCamera;

        private void Update()
        {
            var angles = transform.eulerAngles;
            angles.y = _headCamera.transform.eulerAngles.y;
            transform.eulerAngles = angles;
        }
    }
}