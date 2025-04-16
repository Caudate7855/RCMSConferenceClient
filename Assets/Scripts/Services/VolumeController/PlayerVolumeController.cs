using System.Collections;
using Pvr_UnitySDKAPI;
using UIManager.Windows.VideoWindow;
using UnityEngine;

public class PlayerVolumeController : MonoBehaviour
{
    [SerializeField] public float _volumeChangeStep = 0.15f;
    
    private VideoWindowController _videoWindowController;
    private float _videoPlayerVolume;
    private int _lastConvertedVolume;
    private int _maxVolumeNumber;
    private bool _initialized = false;

    private float VideoPlayerVolumeAvoidSystem 
    {
        set
        {
            _videoPlayerVolume = Mathf.Clamp01(value);
        }
    }
    
    public float VideoPlayerVolume
    {
        get => _videoPlayerVolume;

        set
        {
            _videoPlayerVolume = Mathf.Clamp01(value);

#if !UNITY_EDITOR
            VolumePowerBrightness.UPvr_SetVolumeNum(Mathf.FloorToInt(VideoPlayerVolume * _maxVolumeNumber));
#endif
        }
    }

    
    
    // videoPlayerController нужен для изменения громкости в эдиторе
    public void Initialize(VideoWindowController videoWindowController)
    {
        if (_initialized)
            return;

        _initialized = true;

        _videoWindowController = videoWindowController;

#if !UNITY_EDITOR
        // https://sdk.picovr.com/docs/sdk/en/chapter_seven.html#power-volume-and-brightness-service-related
        VolumePowerBrightness.UPvr_StartAudioReceiver(gameObject.name);
        VolumePowerBrightness.UPvr_InitBatteryVolClass();

        _maxVolumeNumber = VolumePowerBrightness.UPvr_GetMaxVolumeNumber();
#endif

        VideoPlayerVolume = videoWindowController.StartupVolume;
    }

    public int? GetConvertedVolume(bool isNullable = false)
    {
        int volumeLevels = 15;
        int currentLevel = Mathf.RoundToInt(_videoPlayerVolume * (volumeLevels - 1));
        int convertedVolume = Mathf.RoundToInt((float)currentLevel / (volumeLevels - 1) * 100);
        convertedVolume = Mathf.RoundToInt(convertedVolume / 10f) * 10;
        
        if (_lastConvertedVolume == convertedVolume && isNullable)
            return null;

        _lastConvertedVolume = convertedVolume;

        return convertedVolume;
    }

    public void SetVolume(int volume)
    {
        VideoPlayerVolume = (float)volume / 100;
        
        Debug.Log( "Set volume ------ " + (float)volume / 100);
    }

    public void StartCheckSystemVolume()
    {
#if UNITY_EDITOR
        return;
#endif
        StopAllCoroutines();
        StartCoroutine(CheckSystemVolumeChanged());
    }

    // Чек если меняется звук из системы - т.е. надо обновить иконки громкости и т.д. VolumePowerBrightness.UPvr_SetAudio не работает
    private IEnumerator CheckSystemVolumeChanged()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(.2f);

        int lastVolume = -1;

        while (true)
        {
            yield return waitForSeconds;

            int newVolume = VolumePowerBrightness.UPvr_GetCurrentVolumeNumber();

            if (lastVolume == newVolume) 
                continue;

            lastVolume = newVolume;

            VideoPlayerVolumeAvoidSystem = (float)newVolume / (float)_maxVolumeNumber;

            _videoWindowController.ChangeVolumeSprite(_videoPlayerVolume);
        }
    }
}