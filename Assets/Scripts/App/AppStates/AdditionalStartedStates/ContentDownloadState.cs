using Common;
using Services;
using UIManager.UISystem.UIManager;
using UIManager.Windows;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace App.AppStates
{
    public class ContentDownloadState : FsmStateBase
    {
        [Inject] private VideoDownloader _videoDownloader;

#if PICO_G2
        [Inject] private SleepModeSwitcher _sleepModeSwitcher;
#endif

        private StartedScreenController _startedScreenController;
        private IUIManager _uiManager;

        public ContentDownloadState(ISceneLoader sceneLoader, IUIManager uiManager) : base(sceneLoader)
        {
            _uiManager = uiManager;
        }

        public override void Enter()
        {
#if PICO_G2
            _sleepModeSwitcher.PauseTimer();
#endif
            Debug.Log($"TestBuild Download start");
            
            _startedScreenController = _uiManager.Load<StartedScreenController>();
            _startedScreenController.Open();
            _startedScreenController.ShowDownloadContentWindow();

            Room room = Object.FindObjectOfType<Room>(true);

            if (room != null)
                room.gameObject.SetActive(true);

            _videoDownloader.DownloadRemoteContentAsync();
        }

        public override void Exit()
        {
#if PICO_G2
            _sleepModeSwitcher.ResumeTimer();
#endif
            Debug.Log($"TestBuild Download end");
            _startedScreenController.CloseDownloadContentWindow();
        }
    }
}