using UnityEngine;

namespace VIAVR.Scripts.UI.Pages
{
    public class InitializationPage : PageBase
    {
        [SerializeField] private Transform _loader;

        void Update()
        {
            _loader.Rotate(0,0,-Time.deltaTime * 180);
        }
    }
}