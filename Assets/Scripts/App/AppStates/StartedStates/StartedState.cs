using Common;
using UIManager.UISystem.UIManager;
using UIManager.Windows;
using UnityEngine;

public class StartedState : FsmStateBase
{
    private StartedScreenController _startedScreenController;
    private IUIManager _uiManager;

    public StartedState(ISceneLoader sceneLoader, IUIManager uiManager) : base(sceneLoader)
    {
        _uiManager = uiManager;
    }

    public override void Enter()
    {
        SceneLoader.LoadScene(SceneType.MainScene);
        
        _startedScreenController = _uiManager.Load<StartedScreenController>();
        _startedScreenController.Open();

        Room room = Object.FindObjectOfType<Room>(true);

        if (room != null)
            room.gameObject.SetActive(true);
    }


    public override void Exit() { }
}