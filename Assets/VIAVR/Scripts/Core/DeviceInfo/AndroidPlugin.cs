using System;
using UnityEngine;

namespace VIAVR.Scripts.Core.DeviceInfo
{
#if UNITY_ANDROID
    public static class AndroidPlugin
    {
        public const string ANDROID_PACKAGE = "jp.fantom1x.plugin.android.fantomPlugin";
        public const string ANDROID_SYSTEM = ANDROID_PACKAGE + ".AndroidSystem";

        public static void StartBatteryStatusListening(string callbackGameObject, string callbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startBatteryListening",
                    context,
                    callbackGameObject,
                    callbackMethod
                );
            }));
        }
        public static void StopBatteryStatusListening()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "stopBatteryListening"
                );
            }
        }
        public static void StartCpuRateListening(string callbackGameObject, string callbackMethod, float interval)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startCpuRateListening",
                    context,
                    callbackGameObject,
                    callbackMethod,
                    interval
                );
            }));
        }
        public static void StartCpuRateListening(string callbackGameObject, string callbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startCpuRateListening",
                    context,
                    callbackGameObject,
                    callbackMethod
                );
            }));
        }
        public static void StopCpuRateListening()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "stopCpuRateListening"
                );
            }
        }
    }
#endif
    [Serializable]
    public class BatteryInfo
    {
        public string timestamp;    //Time when information was obtained.           
        public int level;           //The remaining battery capacity.               
        public int scale;           //Maximum amount of battery.                    
        public int percent;         //％（level/scale*100）(= UnityEngine.SystemInfo.batteryLevel*100)
        public string status;       //Charge state (= UnityEngine.BatteryStatus)    
        public string health;       //Battery condition.                            
        public float temperature;   //Battery temperature (℃).                     
        public float voltage;       //The current battery voltage level.(V)         

        public override string ToString()
        {
            return JsonUtility.ToJson(this);    //for debug
        }
    }
    [Serializable]
    public class CpuRateInfo
    {
        public string name;         //"cpu0"~
        public float ratio;         //The ratio of each core when the total utilization of all cores is taken as 100%. [%]    
        public float user;          //CPU utilization used at the user level (application).[%]                                 
        public float nice;          //Priority (nice value) The CPU usage rate due to execution of the set user process.        
        public float system;        //CPU utilization used at the system level (kernel).                                        
        public float idle;          //Percentage of CPU not being used.                                                         

        public override string ToString()
        {
            return JsonUtility.ToJson(this);    //for debug
        }
    }
}

