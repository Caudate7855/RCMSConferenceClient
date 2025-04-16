using Services;
using UnityEngine;
using Zenject;

namespace IoC
{
    public class RemoteInstaller : MonoInstaller<RemoteInstaller>
    {
        [SerializeField] private RemoteRequestController _remoteRequestController;
        
        public override void InstallBindings()
        {
            Container
                .Bind<RemoteRequestController>()
                .FromComponentInNewPrefab(_remoteRequestController)
                .AsSingle();

            Container
                .Bind<RemoteContentContainer>()
                .AsSingle();
        }
    }
}