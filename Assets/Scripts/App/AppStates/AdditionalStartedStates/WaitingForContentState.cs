using Common;
using UIManager.UISystem.UIManager;
using UIManager.Windows;
using UnityEngine;

namespace App.AppStates
{
    public class WaitingForContentState : FsmStateBase
    {
        private StartedScreenController _startedScreenController;
        private IUIManager _uiManager;
    
        public WaitingForContentState(ISceneLoader sceneLoader, IUIManager uiManager) : base(sceneLoader)
        {
            _uiManager = uiManager;
        }

        public override void Enter()
        {
            _startedScreenController = _uiManager.Load<StartedScreenController>();
            _startedScreenController.Open();
            _startedScreenController.ShowWaitingForContentWindow();

            Room room = Object.FindObjectOfType<Room>(true);

            if (room != null)
                room.gameObject.SetActive(true);
        }

        public override void Exit() => _startedScreenController.CloseWaitingForContentWindow();
    }
}