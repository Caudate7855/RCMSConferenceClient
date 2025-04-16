using UnityEngine;
using VIAVR.Scripts.Core;
using VIAVR.Scripts.Core.Singleton;
using Debug = CustomDebug.Debug;

namespace VIAVR.Scripts
{
    public class IdleScreenChecker : MonoBehaviour
    {
        [SerializeField] private int _turnOffScreenAfterSeconds = 60;
        [SerializeField] private ControllersHandler _controllersHandler;
        //[SerializeField] private VideoPlayerController _videoPlayerController;

        private float _timer;

        private bool _running;
        private bool _paused;

        private bool _initialized;
    
        // Start is called before the first frame update
        public void Initialize(VrTourController vrTourController, bool run = true)
        {
            if(_initialized) return;

            _initialized = true;

            _controllersHandler.OnHeadControlClick += hierarchy =>
            {
                Reset();
            };
        
            // при коннекте контроллера, нажатии любой кнопки контроллера сбрасываем таймер
            _controllersHandler.OnControllerButton += (type, action, longpress) =>
            {
                if (action == ControllersHandler.ButtonAction.DOWN)
                {
                    Reset();
                }
            };
        
            _controllersHandler.OnControllerConnectChanged += state =>
            {
                if (state == ControllersHandler.ControllerConnectState.CHANGED_TO_CONNECTED)
                {
                    Reset();
                }
            };

            vrTourController.OnVrTourStart += () =>
            {
                Reset();
                Pause();
            };
        
            vrTourController.OnVrTourEnd += () =>
            {
                Reset();
                Resume();
            };

            if (run)
            {
                Run();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            Reset();
        }

        // Update is called once per frame
        void Update()
        {
            if(!_running || _paused) return;

            _timer += Time.deltaTime;

            if (_timer > _turnOffScreenAfterSeconds)
            {
                Debug.Log("IdleScreenChecker: screen turn OFF", Debug.LoggerBehaviour.ADD);
            
                Reset();
            
                // выключаем экран
                Singleton<AndroidDeviceHelper>.Instance.ExecuteShellSync("input keyevent 26");
            }
        }

        public void Run()
        {
            _running = true;
            _paused = false;
        
            Reset();
        }

        public void Stop()
        {
            _running = false;
        
            Reset();
        }

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        public void Reset()
        {
            _timer = 0;
        }
    }
}