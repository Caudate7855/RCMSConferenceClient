using Common;

public class BootState : FsmStateBase
{   
    public BootState(ISceneLoader sceneLoader) : base(sceneLoader) { }

    public override void Enter() => SceneLoader.LoadScene(SceneType.Boot);

    public override void Exit() { }
}