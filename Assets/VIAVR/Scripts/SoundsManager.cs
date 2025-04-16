using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using VIAVR.Scripts.Data;

namespace VIAVR.Scripts
{
    public class SoundsManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioSource _audioSourceMusic;
        [SerializeField] private AudioClip[] _audioClips;

        [SerializeField] private string _musicPath;

        [SerializeField] private string _defaultClickSoundName = "click";
        [SerializeField] [Range(0, 1)] private float _defaultClickSoundVolume = 1f;

        private readonly Dictionary<string, AudioClip> _audioClipsDictonary = new Dictionary<string, AudioClip>();
    
        private float _maxMusicVolume;

        private bool _initialized;

        public float Volume
        {
            get => _audioSource.volume;
            set => _audioSource.volume = Mathf.Clamp01(value);
        }
    
        public void Initialize()
        {
            if(_initialized) return;
        
            _initialized = true;
        
            _maxMusicVolume = _audioSourceMusic.volume;
        
            if(!_audioClips.Any()) return;

            foreach (var audioClip in _audioClips)
            {
                if (string.IsNullOrEmpty(audioClip.name))
                {
                    Debug.LogError("Audio clip name is empty", this);
                    continue;
                }

                _audioClipsDictonary[audioClip.name] = audioClip;
            }

            _audioSourceMusic.Play();
        }

        public void PlaySound(string soundName)
        {
            if (!_initialized)
            {
                Debug.LogError($"Need Initialize() before using {nameof(SoundsManager)}", this);
                return;
            }
        
            if (!_audioClipsDictonary.ContainsKey(soundName))
            {
                Debug.LogError($"Sound with name '{soundName}' not found");
                return;
            }

            _audioSource.PlayOneShot(_audioClipsDictonary[soundName]);
        }

        public void PlayDefaultClickSound()
        {
            if(_defaultClickSoundVolume == 0) return;
        
            float lastVolume = Volume;
            Volume = _defaultClickSoundVolume;
        
            PlaySound(_defaultClickSoundName);

            Volume = lastVolume;
        }
    
        string _currentMusicPath = "";
    
        IEnumerator PlayMusicCoroutine(string path){

            float volume = 0;

            if(_audioSourceMusic.isPlaying){
                volume = 1f;

                while(volume > 0)
                {
                    volume -= Time.deltaTime;
                    _audioSourceMusic.volume = volume;
                
                    yield return null;
                }

                _audioSourceMusic.Stop();
            }

            using UnityWebRequest unityWebRequest = UnityWebRequestMultimedia.GetAudioClip("file:///" + ContentPath.GetFullPath(path, ContentPath.Storage.INTERNAL), AudioTypeFromPath(path));
        
            yield return unityWebRequest.SendWebRequest();

            if (unityWebRequest.isHttpError || unityWebRequest.isNetworkError)
            {
                Debug.Log(unityWebRequest.error);
            }
            else
            {
                _audioSourceMusic.clip = DownloadHandlerAudioClip.GetContent(unityWebRequest);
                _audioSourceMusic.clip.name = Path.GetFileName(path);
                _audioSourceMusic.loop = true;

                while (_audioSourceMusic.clip.loadState != AudioDataLoadState.Loaded)
                {
                    yield return null;
                }

                _audioSourceMusic.Play();

                while (volume <_maxMusicVolume)
                {
                    volume += Time.deltaTime;
                    _audioSourceMusic.volume = volume;
                
                    yield return null;
                }

                _audioSourceMusic.volume = _maxMusicVolume;
            }
        }

        public void PlayDefaultMusic()
        {
            PlayMusic(_musicPath);
        }

        private void PlayMusic(string path)
        {
            if(_currentMusicPath.Equals(path)) return;

            if (!File.Exists(ContentPath.GetFullPath(path, ContentPath.Storage.INTERNAL)))
            {
                Debug.LogError($"Music file '{path}' not exists!");
                return;
            }

            _currentMusicPath = path;

            StartCoroutine(PlayMusicCoroutine(path));
        }
    
        AudioType AudioTypeFromPath(string path){

            AudioType type = AudioType.UNKNOWN;

            string extension = Path.GetExtension(path).ToLower();

            switch(extension){
                case "mp3":
                    type = AudioType.MPEG;
                    break;

                case "ogg":
                    type = AudioType.OGGVORBIS;
                    break;

                case "wav":
                    type = AudioType.WAV;
                    break;
            }

            return type;
        }
    }
}