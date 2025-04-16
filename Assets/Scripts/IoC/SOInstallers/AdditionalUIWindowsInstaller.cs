using UnityEngine;
using Zenject;

namespace IoC
{
    [CreateAssetMenu(fileName = "AdditionalUIWindowsInstaller", menuName = "SO/Installers/AdditionalUIWindowsInstaller")]
    public class AdditionalUIWindowsInstaller : ScriptableObjectInstaller<AdditionalUIWindowsInstaller>
    {
        [SerializeField] private PopUpsContainer _popUpsContainer;

        public override void InstallBindings()
        {
            Container
                .Bind<PopUpsContainer>()
                .FromInstance(_popUpsContainer);
        }
    }
}