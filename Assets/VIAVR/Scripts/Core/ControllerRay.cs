using UnityEngine;

namespace VIAVR.Scripts.Core
{
    public class ControllerRay : MonoBehaviour
    {
        public Transform RayDot => _dot;
        public Transform RayStart => _start;
        public Transform RayAdaptive => _ray_LengthAdaptive;
    
        [SerializeField] private Transform _dot;
        [SerializeField] private Transform _start;
        [SerializeField] private Transform _ray_LengthAdaptive;

        public void ShowRay()
        {
            SetRayVisibility(true);
        }

        public void HideRay()
        {
            SetRayVisibility(false);
        }

        public void ShowDot()
        {
            SetDotVisibility(true);
        }

        public void HideDot()
        {
            SetDotVisibility(false);
        }

        public void SetDotVisibility(bool state)
        {
            _dot.gameObject.SetActive(state);
        }

        public void SetRayVisibility(bool state)
        {
            _dot.gameObject.SetActive(state);
            _start.gameObject.SetActive(state);
            _ray_LengthAdaptive.gameObject.SetActive(state);
        }
    }
}