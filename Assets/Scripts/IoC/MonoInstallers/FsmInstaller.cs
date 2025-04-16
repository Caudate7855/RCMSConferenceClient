using App.AppStates;
using Zenject;

namespace IoC
{
    public class FsmInstaller : MonoInstaller<FsmInstaller>
    {
        public override void InstallBindings()
        {
            InstallFsm();
            InstallStates();
        }

        private void InstallStates()
        {
            Container
                .Bind<BootState>()
                .AsSingle();
        
            Container
                .Bind<StartedState>()
                .AsSingle();
        
            Container
                .Bind<VideoShowingState>()
                .AsSingle();

            Container
                .Bind<ConnectToWiFiState>()
                .AsSingle();

            Container
                .Bind<ContentDownloadState>()
                .AsSingle();
        
            Container
                .Bind<WaitingForContentState>()
                .AsSingle();
        }

        private void InstallFsm()
        {
            Container
                .Bind<AppFSM>()
                .AsSingle();
        }  
    }
}