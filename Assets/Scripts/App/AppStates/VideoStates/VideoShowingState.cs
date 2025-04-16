using System.Collections.Generic;
using System.Linq;
using Common;
using Services;
using Services.ControllerFader;
using UIManager.UISystem.UIManager;
using UIManager.Windows;
using UIManager.Windows.VideoWindow;
using UnityEngine;
using Zenject;

public class VideoShowingState : FsmStateBase
{
#if PICO_G2
    [Inject] private SleepModeSwitcher _sleepModeSwitcher;
#endif
    private VideoWindowController _videoWindowController;
    private IUIManager _uiManager;
    private StartedScreenController _startedScreenController;
    private List<ControllerFader> _controllerFaders;
    private HeadCursorFader _headCursorFader;

    public VideoShowingState(ISceneLoader sceneLoader, IUIManager uiManager) : base(sceneLoader)
    {
        _uiManager = uiManager;
        _controllerFaders = Object.FindObjectsOfType<ControllerFader>(true).ToList();
        _headCursorFader = Object.FindObjectOfType<HeadCursorFader>(true);
    }

    public override void Enter()
    {
#if PICO_G2
        _sleepModeSwitcher.PauseTimer();
#endif  
        _startedScreenController = _uiManager.Load<StartedScreenController>();
        _startedScreenController.Close();
        _videoWindowController = _uiManager.Load<VideoWindowController>();
        _videoWindowController.Open();

        foreach (var controllerFader in _controllerFaders) 
            controllerFader.FadeController(false);
        _headCursorFader.Hide();
    }

    public override void Exit()
    {
#if PICO_G2
        _sleepModeSwitcher.ResumeTimer();
#endif
        foreach (var controllerFader in _controllerFaders) 
            controllerFader.FadeController(true);
        _headCursorFader.Show();
        
        _videoWindowController.ResetContentData();
        _videoWindowController.Close();
    }
}