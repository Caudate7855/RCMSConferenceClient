using System.Collections;
using UnityEngine;

namespace VIAVR.Scripts.Core.DeviceInfo
{
    public class DeviceInfo : MonoBehaviour
    {
        private float _fps;
        private int[] _cpu;
        private float _batteryTemperature;
        public int[] CPU => _cpu;
        public float BatteryTemperature => _batteryTemperature;
        public float FPS => _fps;

        IEnumerator Start()
        {
            _cpu = new int[8];

            WaitForSeconds waitForSeconds = new WaitForSeconds(1);

            while (true)
            {
                yield return waitForSeconds;

                _fps = 1f / Time.deltaTime;
            }
        }

        public void SetCPU(CpuRateInfo[] info)
        {
            if (info == null)
                return;
            for (int i = 0; i < info.Length; i++)
            {
                _cpu[i] = (int)(info[i].system + info[i].user + info[i].nice);
            }
        }

        public void SetTemperature(BatteryInfo info)
        {
            if (info==null)
                return;
            _batteryTemperature = info.temperature;
        }

    }
}