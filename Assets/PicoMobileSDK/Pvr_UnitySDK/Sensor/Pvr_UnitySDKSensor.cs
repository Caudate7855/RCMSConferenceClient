﻿// Copyright  2015-2020 Pico Technology Co., Ltd. All Rights Reserved.


using Pvr_UnitySDKAPI;
using UnityEngine;
using System;

public class Pvr_UnitySDKSensor
{
    public event Action OnResetUnitySDKSensor;

    private static Pvr_UnitySDKSensor instance = null;
    public static Pvr_UnitySDKSensor Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Pvr_UnitySDKSensor();
            }

            return instance;
        }
        set { instance = value; }
    }
    public Pvr_UnitySDKSensor()
    {
        Init();
    }

    /************************************    Properties  *************************************/
    #region Properties

    bool SensorStart = false;
    bool SensorInit = false;

    Quaternion UnityQuaternion = Quaternion.identity;
    Vector3 UnityPosition = Vector3.zero;

    Pvr_UnitySDKAPI.Sensorindex sensorIndex = Pvr_UnitySDKAPI.Sensorindex.Default;

    private bool dofClock = false;
    public static Action EyeFovChanged;

    public Pvr_UnitySDKPose HeadPose;

    #endregion

    /************************************   Public Interfaces **********************************/
    #region Public Interfaces

    public delegate void Enter3DofModel();
    public static event Enter3DofModel Enter3DofModelEvent;

    public delegate void Exit3DofModel();
    public static event Exit3DofModel Exit3DofModelEvent;

    public void Init()
    {
        InitUnitySDK6DofSensor();
        SensorInit = InitUnitySDKSensor();
        SensorStart = StartUnitySDKSensor();
        HeadPose = new Pvr_UnitySDKPose(Vector3.zero, Quaternion.identity);
    }

    public void SensorUpdate()
    {
        if (GetUnitySDKSensorState())
        {
            HeadPose.Set(UnityPosition, UnityQuaternion);
        }
    }

    public bool InitUnitySDKSensor()
    {
        bool enable = false;
        try
        {
            if (Pvr_UnitySDKAPI.Sensor.UPvr_Init((int)sensorIndex) == 0)
                enable = true;
        }
        catch (System.Exception e)
        {
            PLOG.E("InitUnitySDKSensor ERROR! " + e.Message);
            throw;
        }
        return enable;
    }

    public bool InitUnitySDK6DofSensor()
    {
        bool enable = false;
#if !UNITY_EDITOR
        try
        {
            int ability6dof = 0;
            int enumindex = (int)Pvr_UnitySDKAPI.GlobalIntConfigs.ABILITY6DOF;
            Pvr_UnitySDKAPI.Render.UPvr_GetIntConfig(enumindex, ref ability6dof);
            if (ability6dof == 1)
            {
                if (Pvr_UnitySDKAPI.Sensor.UPvr_Enable6DofModule(!Pvr_UnitySDKManager.SDK.HmdOnlyrot) == 0)
                {
                    if (!Pvr_UnitySDKManager.SDK.HmdOnlyrot)
                    {
                        enable = true;
                        Pvr_UnitySDKManager.SDK.PVRNeck = false;
                    }
                }
            }
            else
            {
                Debug.LogWarning("This platform does not support 6 Dof !");
            }
        }
        catch (System.Exception e)
        {
            PLOG.E("InitUnity6DofSDKSensor ERROR! " + e.Message);
            throw;
        }
#endif
        return enable;
    }

    public bool StartUnitySDKSensor()
    {
        bool enable = false;
        try
        {
            if (Pvr_UnitySDKAPI.Sensor.UPvr_StartSensor((int)sensorIndex) == 0)
                enable = true;
        }
        catch (System.Exception e)
        {
            PLOG.E("StartUnitySDKSensor ERROR! " + e.Message);
            throw;
        }
        return enable;
    }

    public bool StopUnitySDKSensor()
    {
        bool enable = false;
        try
        {
            if (Pvr_UnitySDKAPI.Sensor.UPvr_StopSensor((int)sensorIndex) == 0)
                enable = true;

        }
        catch (System.Exception e)
        {
            PLOG.E("StopUnitySDKSensor ERROR! " + e.Message);
            throw;
        }
        return enable;
    }

    public bool ResetUnitySDKSensor()
    {
        Debug.Log("RECENTER - ResetUnitySDKSensor");
        bool enable = false;
        try
        {
            if (Pvr_UnitySDKAPI.Sensor.UPvr_ResetSensor((int)sensorIndex) == 0)
            {
                enable = true;
                //PLOG.E("ResetUnitySDKSensor OK! ");
                
                OnResetUnitySDKSensor?.Invoke();
            }
        }
        catch (System.Exception e)
        {
            PLOG.E("ResetUnitySDKSensor ERROR! " + e.Message);
            throw;
        }
        return enable;
    }

    public bool OptionalResetUnitySDKSensor(int resetRot, int resetPos)
    {
        bool enable = false;
        try
        {
            if (Pvr_UnitySDKAPI.Sensor.UPvr_OptionalResetSensor((int)sensorIndex, resetRot, resetPos) == 0)
            {
                enable = true;
                Debug.Log("PvrLog OptionalResetUnitySDKSensor OK!" + resetRot + resetPos);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("OptionalResetUnitySDKSensor ERROR! " + e.Message);
            throw;
        }
        return enable;
    }

    float vfov = 102, hfov = 102;
    float w = 0, x = 0, y = 0, z = 0, px = 0, py = 0, pz = 0;
    public bool GetUnitySDKSensorState()
    {
        bool enable = false;
        if (SensorInit && SensorStart)
        {
            try
            {
                int returns = -1;
                if (Pvr_UnitySDKManager.SDK.ShowVideoSeethrough)
                {
                    BoundarySystem_Ext.Pvr_BoundarySystem.Instance.CameraFramePtr = BoundarySystem_Ext.Pvr_BoundaryAPI.UPvr_GetCameraData_Ext();
                    returns = BoundarySystem_Ext.Pvr_BoundaryAPI.UPvr_GetMainSensorStateExt(ref x, ref y, ref z, ref w, ref px, ref py, ref pz, ref vfov, ref hfov, ref Pvr_UnitySDKRender.Instance.RenderviewNumber);
                }
                else
                {
                    returns = Pvr_UnitySDKAPI.Sensor.UPvr_GetMainSensorState(ref x, ref y, ref z, ref w, ref px, ref py, ref pz, ref vfov, ref hfov, ref Pvr_UnitySDKRender.Instance.RenderviewNumber);
                }

                Pvr_UnitySDKManager.SDK.posStatus = Sensor.UPvr_Get6DofSensorQualityStatus();
                if (returns == 0)
                {
                    if (!Convert.ToBoolean(Pvr_UnitySDKManager.SDK.posStatus & 0x2))
                    {
                        if (!dofClock)
                        {
                            if (Enter3DofModelEvent != null)
                                Enter3DofModelEvent();
                            dofClock = true;
                        }
                    }
                    else
                    {
                        if (dofClock)
                        {
                            if (Exit3DofModelEvent != null)
                                Exit3DofModelEvent();
                            dofClock = false;
                        }
                    }

                    RefreshHeadData(x, y, z, w, px, py, pz);
                    UnityQuaternion.Set(x, y, -z, -w);
                    if (Pvr_UnitySDKRender.Instance.EyeVFoV != vfov)
                    {
                        Pvr_UnitySDKRender.Instance.EyeVFoV = vfov;
                        if (EyeFovChanged != null)
                        {
                            EyeFovChanged();
                        }
                    }
                    Pvr_UnitySDKRender.Instance.EyeHFoV = hfov;
                    Pvr_UnitySDKManager.SDK.EyesAspect = hfov / vfov;
                    enable = true;

                    if (Pvr_UnitySDKManager.SDK.HmdOnlyrot)
                    {
                        if (Pvr_UnitySDKManager.SDK.PVRNeck)
                        {
                            if (Pvr_UnitySDKManager.SDK.TrackingOrigin == TrackingOrigin.FloorLevel)
                            {
                                UnityPosition.Set(0, py, 0);
                                UnityPosition += UnityQuaternion * Pvr_UnitySDKManager.SDK.neckOffset -
                                                Pvr_UnitySDKManager.SDK.neckOffset.y * Vector3.up;
                            }
                            else
                            {
                                UnityPosition = UnityQuaternion * Pvr_UnitySDKManager.SDK.neckOffset -
                                                Pvr_UnitySDKManager.SDK.neckOffset.y * Vector3.up;
                            }
                        }
                    }
                    else
                    {
                        UnityPosition.Set(px, py, -pz);
                    }
                    if (PLOG.logLevel > 2)
                    {
                        PLOG.D("posStatus=" + Pvr_UnitySDKManager.SDK.posStatus);
                        PLOG.D("PvrLog 6DoFHead" + "Rotation:" + x + "," + y + "," + -z + "," + -w + "," + "Position:" + px + "," + py + "," + -pz + "," + "eulerAngles:" + UnityQuaternion.eulerAngles);
                    }
                }
                if (returns == -1)
                    PLOG.I("PvrLog Sensor update --- GetUnitySDKSensorState  -1 ");
            }
            catch (System.Exception e)
            {
                PLOG.E("GetUnitySDKSensorState ERROR! " + e.Message);
                throw;
            }
        }
        return enable;
    }

    public bool GetUnitySDKPSensorState()
    {
        bool enable = false;
        try
        {
            if (Pvr_UnitySDKAPI.Sensor.UPvr_ResetSensor((int)sensorIndex) == 0)
                enable = true;

        }
        catch (System.Exception e)
        {
            PLOG.E("GetUnitySDKPSensorState ERROR! " + e.Message);
            throw;
        }
        return enable;
    }

    private void RefreshHeadData(float x, float y, float z, float w, float px, float py, float pz)
    {
        Pvr_UnitySDKManager.SDK.headData[0] = x;
        Pvr_UnitySDKManager.SDK.headData[1] = y;
        Pvr_UnitySDKManager.SDK.headData[2] = z;
        Pvr_UnitySDKManager.SDK.headData[3] = w;
        Pvr_UnitySDKManager.SDK.headData[4] = px;
        Pvr_UnitySDKManager.SDK.headData[5] = py;
        Pvr_UnitySDKManager.SDK.headData[6] = pz;
    }

    #endregion
}
