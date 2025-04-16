using System.Collections;
using Services;
using UnityEngine;
using UnityEngine.Android;
using Zenject;

namespace Boot
{
    public class AppBoot : MonoBehaviour
    {
        [Inject] private AppFSM _appFsm;
        [Inject] private RemoteRequestController _remoteRequestController;
        
        private void Awake()
        {
            StartCoroutine(PermissionsCoroutine());
            DontDestroyOnLoad(this);

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            Initialize();
        }

        private void Initialize()
        {
            _remoteRequestController.Initialize();
            _appFsm.SetState<StartedState>();
        }


        private IEnumerator PermissionsCoroutine()
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);

                while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                    yield return null;
            }
        }
    }
}