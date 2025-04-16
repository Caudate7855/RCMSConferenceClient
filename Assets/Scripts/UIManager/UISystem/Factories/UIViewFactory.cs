using System;
using JetBrains.Annotations;
using UIManager.UISystem.Abstracts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIManager.UISystem.Factories
{
    [UsedImplicitly]
    public static class UIViewFactory
    {
#if ADDRESSABLES
        public static async System.Threading.Tasks.Task<T> LoadFromAddressablesAsync<T>(UIManagerCanvasBase parentCanvas, string assetPath) where T : UIViewBase
        {
            var assetLoader = new UIAssetLoader();
            
            var instance = await assetLoader.Load<T>(assetPath);

            instance = instance.GetComponent<T>();
            
            parentCanvas = Object.FindObjectOfType<UIManagerCanvasBase>();

            var viewInstance = Object.Instantiate(instance, parentCanvas.transform);
            
            return viewInstance;
        }
#endif
        //думаю здесь лучше вынуждать пользователя передавать существующий родительский канвас, а не искать его через FindObjectOfType
        // так как это порождает неявное поведение
        public static T LoadFromResources<T>(UIManagerCanvasBase parentCanvas, string assetPath)
            where T : UIViewBase
        {
            var resourceRequest = Resources.LoadAsync<T>(assetPath);

            if (parentCanvas == null)
                throw new Exception($"Parent Canvas is null. Failed to create {typeof(T)}");
            
            T viewInstance = Object.Instantiate(resourceRequest.asset, parentCanvas.transform) as T;

            return viewInstance;
        }
    }
}