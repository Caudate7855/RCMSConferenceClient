using Common;
using UIManager.UISystem.UIManager;
using UIManager.Windows;

namespace App.AppStates
{
    public class ConnectToWiFiState : FsmStateBase
    {
        private IUIManager _uiManager;
        private WiFiNetworksListController _wiFiNetworksListController;
        private WiFiPasswordController _wiFiPasswordController;
        private StartedScreenController _startedScreenController;
        
        public ConnectToWiFiState(ISceneLoader sceneLoader, IUIManager uiManager) : base(sceneLoader)
        {
            _uiManager = uiManager;
        }

        public override void Enter()
        {
            _startedScreenController = _uiManager.Load<StartedScreenController>();
            _startedScreenController.Close();
            _wiFiPasswordController = _uiManager.Load<WiFiPasswordController>();
            _wiFiPasswordController.Close();
            _wiFiNetworksListController = _uiManager.Load<WiFiNetworksListController>();
            _wiFiNetworksListController.Open();
        }

        public override void Exit()
        {
            _wiFiPasswordController.Close();
            _wiFiNetworksListController.Close();
        }
    }
}