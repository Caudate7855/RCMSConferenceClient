using Services;
using UIManager.UISystem.UIManager;
using Zenject;

namespace IoC
{
    public class UIInstaller : MonoInstaller<UIInstaller>
    {
        public override void InstallBindings()
        {
            Container
                .Bind<IUIManager>()
                .To<UIManager.UISystem.UIManager.UIManager>()
                .AsSingle();

            Container
                .Bind<PopUpService>()
                .AsSingle();
        }
    }
}