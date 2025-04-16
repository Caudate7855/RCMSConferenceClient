using System;
using UnityEngine;

namespace VIAVR.Scripts.UI.Loader
{
    public interface ILoaderAnimationAttachable
    {
        public Transform Transform { get; }
    
        public event Action<ILoaderAnimationAttachable, bool> OnRemoveLoaderRequest;
    }

// юзать через Singletone<LoaderBuilder>.Instance
    public class LoaderBuilder : MonoBehaviour
    {
        [SerializeField] private GameObject _loaderPrefab;

        public LoaderAnimation AttachLoader(ILoaderAnimationAttachable control, LoaderAnimation.BackgroundTransparency backgroundTransparency, Vector3 spinnerSize, Sprite customBackground = null)
        {
            var loader = Instantiate(_loaderPrefab, control.Transform);
        
            loader.transform.localScale = Vector3.one;
        
            var loaderAnimation = loader.GetComponent<LoaderAnimation>();
        
            loaderAnimation.Show(control, backgroundTransparency, spinnerSize, customBackground);

            return loaderAnimation;
        }
    }
}