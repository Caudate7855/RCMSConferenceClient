using System;
using System.Linq;
using System.Threading.Tasks;
using UIManager.Canvas;
using UIManager.UISystem.Attributes;
using UIManager.UISystem.Factories;
using Object = UnityEngine.Object;

namespace UIManager.UISystem.Abstracts
{
    public abstract class UIControllerBase<TView> : IUIController where TView : UIViewBase
    {
        protected TView View;
        
        private bool _isViewLoaded;
        

        protected UIControllerBase()
        {
            //как варик можно повесить на канвас какой-нибудь тег, MainCanvas например, и по нему искать объект, а скрипт MainCanvas вообще удалить
            //ну или биндить через зенджект
            MainCanvas = Object.FindObjectOfType<MainCanvas>();
            
#if ADDRESSABLES
            LoadFromAddressables();
#else
            LoadFromResources();
#endif
        }
        
        public bool IsOpened { get; private set; }
        protected MainCanvas MainCanvas { get; }

#if ADDRESSABLES
        private async void LoadFromAddressables()
        {
            View = await UIViewFactory.LoadFromAddressablesAsync<TView>(MainCanvas, GetViewAssetAddress());
            
            View.gameObject.SetActive(false);
            _isViewLoaded = true;

            Initialize();
        }
#endif
        private void LoadFromResources()
        {
            View = UIViewFactory.LoadFromResources<TView>(MainCanvas, GetViewAssetAddress());
            
            View.gameObject.SetActive(false);
            _isViewLoaded = true;

            Initialize();
        }

        public async void Open()
        {
            if (IsOpened)
                return;
            
            if (!_isViewLoaded)
                await WaitForViewToLoad();

            View.gameObject.SetActive(true);
            IsOpened = true;
            OnOpen();
        }

        public async void Close()
        {
            if(!IsOpened)
                return;
            
            if (!_isViewLoaded)
                await WaitForViewToLoad();

            View.gameObject.SetActive(false);
            IsOpened = false;
            OnClose();
        }

        protected abstract void Initialize();

        protected virtual void OnOpen() { }

        protected virtual void OnClose() { }

        private async Task WaitForViewToLoad()
        {
            while (!_isViewLoaded) 
                await Task.Yield();
        }

        private string GetViewAssetAddress()
        {
            var type = GetType();

            var attribute = type.GetCustomAttributes(typeof(AssetAddress), false)
                .FirstOrDefault() as AssetAddress;

            return attribute?.Address ?? throw new ArgumentException($"Cannot find Address of {type}");
        }
    }
}