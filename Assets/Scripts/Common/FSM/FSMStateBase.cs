namespace Common
{
    public abstract class FsmStateBase
    {
        protected readonly ISceneLoader SceneLoader;

        protected FsmStateBase(ISceneLoader sceneLoader)
        {
            SceneLoader = sceneLoader;
        }

        public abstract void Enter();
        public abstract void Exit();
    }
}