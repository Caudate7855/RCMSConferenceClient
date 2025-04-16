using System;
using System.Collections;
using Pvr_UnitySDKAPI;
using UnityEngine;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.Logger;

namespace VIAVR.Scripts.Core
{
    public class BatteryHandler : MonoBehaviour, ILogHeader
    {
        public event Action<int> OnControllerPowerChanged; // 0 - 100 (с шагом 25!)
        public event Action<int> OnHelmetPowerChanged; // 0 - 100

        [SerializeField] [Range(0,100)] private int _debugHelmetPower = 100;
        [SerializeField] [Range(0,100)] private int _debugControllerPower = 100;

        private int _helmetPower = 100;
        private int _controllerPower = 100;
    
        public int HelmetPower
        {
            get => _helmetPower;
        
            private set
            {
                if(value <= 0) return; // 0 может быть только при криво полученных данных или при отсутствии данных

                int powerClamped = Mathf.Clamp(value, 0, 100);
            
                if(_helmetPower != powerClamped)
                    OnHelmetPowerChanged?.Invoke(powerClamped);
            
                _helmetPower = powerClamped;
            }
        }
        public int ControllerPower
        {
            get => _controllerPower;
        
            private set
            {
                if(value == 0) return; // 0 может быть только при отключенном контроллере
            
                int powerClamped = Mathf.Clamp(value, 0, 100); // бывало что контроллер показывал 125%
            
                if(_controllerPower != powerClamped)
                    OnControllerPowerChanged?.Invoke(powerClamped);
                
                _controllerPower = powerClamped;
            }
        }

        public void Initialize()
        {
            StopAllCoroutines();
        
            StartCoroutine(UpdateBatteryValues());
        }
    
        IEnumerator UpdateBatteryValues()
        {
            var wfs = new WaitForSeconds(5);

            while (true)
            {
                ControllerPower = GetControllerPower();
                HelmetPower = GetHelmetPower();

                yield return wfs;
            }
        }

        public int GetControllerPower()
        {
#if UNITY_EDITOR
            return _debugControllerPower;
#endif
            return Controller.UPvr_GetControllerPower(0) * 25;
        }

        public int GetHelmetPower()
        {
#if UNITY_EDITOR
            return _debugHelmetPower;
#endif
            return Singleton<AndroidDeviceHelper>.Instance.GetBatteryLevel();
        }
    
        public string GetLogHeader()
        {
            return $"Battery: HMD {HelmetPower}%, Controller {ControllerPower}%";
        }
    }
}