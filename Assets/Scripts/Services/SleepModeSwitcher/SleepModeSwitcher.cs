using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Services
{
    [UsedImplicitly]
    public class SleepModeSwitcher : ITickable
    {
        [Inject] private AndroidDeviceHelper _androidDeviceHelper;
        private readonly Transform _headTransform;
        private Queue<float> _angleHistory = new Queue<float>();
        private Quaternion _cashedHeadRotation;

        private float _timerValue;
        private bool _isTimerRunning;
        private bool _isTimerPaused;
        private bool _isInitialized;

        private const int MaxQueueHistory = 10;
        private const float AngleThreshold = 2f;
        private const int ScreenTurnOffTimeIsSeconds = 60;

        public SleepModeSwitcher()
        {
            _headTransform = Object.FindObjectOfType<Pvr_UnitySDKHeadTrack>().transform;

            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            _isTimerRunning = true;
            _isTimerPaused = false;
            _isInitialized = true;

            ResetTimer();
        }

        public void Tick()
        {
            if (Application.isEditor)
                return;
         
#if PICO_G3
            return;
#endif
            
            if (!_isTimerRunning || _isTimerPaused || !_isInitialized) return;

            _timerValue += Time.deltaTime;

            float angleDifference = Quaternion.Angle(_cashedHeadRotation, _headTransform.rotation);
            
            if (IsStableMovement(angleDifference))
            {
                ResetTimer();
                _cashedHeadRotation = _headTransform.rotation;
            }
            
            if (_timerValue > ScreenTurnOffTimeIsSeconds)
            {
                ResetTimer();
                _androidDeviceHelper.ExecuteShellSync("input keyevent 26");
            }
        }

        private bool IsStableMovement(float currentAngle)
        {
            _angleHistory.Enqueue(currentAngle);
            if (_angleHistory.Count > MaxQueueHistory)
                _angleHistory.Dequeue();

            float average = 0f;
            foreach (var angle in _angleHistory)
                average += angle;

            average /= _angleHistory.Count;

            return average > AngleThreshold;
        }

        public void StopTimer()
        {
            _isTimerRunning = false;
            ResetTimer();
        }

        public void PauseTimer()
        {
            _isTimerPaused = true;
            ResetTimer();
        }

        public void ResumeTimer()
        {
            _isTimerPaused = false;
        }

        private void ResetTimer()
        {
            _timerValue = 0;
        }
    }
}