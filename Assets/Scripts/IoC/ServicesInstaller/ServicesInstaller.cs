using Services;
using UnityEngine;
using Zenject;

namespace IoC
{
    public class ServicesInstaller : MonoInstaller<ServicesInstaller>
    {
        [SerializeField] AndroidDeviceHelper _androidDeviceHelper;
        [SerializeField] PlayerVolumeController _playerVolumeController;
        
        public override void InstallBindings()
        {
            Container
                .Bind<AndroidDeviceHelper>()
                .FromComponentInNewPrefab(_androidDeviceHelper)
                .AsSingle();
            
            Container
                .Bind<PlayerVolumeController>()
                .FromComponentInNewPrefab(_playerVolumeController)
                .AsSingle();
            
            Container
                .Bind<ISceneLoader>()
                .To<SceneLoader>()
                .AsSingle();

            Container
                .Bind<VideoDownloader>()
                .AsSingle()
                .NonLazy();

            Container
                .Bind<VideoEncrypter>()
                .AsSingle();

            Container
                .Bind<UrlConverter>()
                .AsSingle();

            Container
                .Bind<DeviceStorageManager>()
                .AsSingle();

            Container
                .Bind<PopUpsController>()
                .AsSingle();

#if PICO_G2
            Container
                .BindInterfacesAndSelfTo<SleepModeSwitcher>()
                .AsSingle();
#endif
        }
    }
}