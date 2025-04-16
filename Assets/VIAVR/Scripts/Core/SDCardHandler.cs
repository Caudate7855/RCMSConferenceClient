using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VIAVR.Scripts.Core.Singleton;
using VIAVR.Scripts.Data;
using VIAVR.Scripts.Logger;

namespace VIAVR.Scripts.Core
{
    public class SDCardHandler : MonoBehaviour, ILogHeader
    {
        public enum SDCardState { OK, REMOVED, CHANGED_TO_OK, CHANGED_TO_REMOVED }

        public event Action<SDCardState> OnSDCardStateChanged; // обрабатывать желательно только стейты CHANGED_TO_... т.к. остальными срёт при пинге

        [SerializeField] private float _pingTimeSeconds = 1f;

        private SDCardState _currentSDCardState;

        private bool _initialized = false;

        public SDCardState CurrentSDCardState
        {
            get => _currentSDCardState;

            set
            {
                SDCardState toSet;
            
                switch (value)
                {
                    case SDCardState.REMOVED:
                        toSet = _currentSDCardState != SDCardState.REMOVED && _currentSDCardState != SDCardState.CHANGED_TO_REMOVED ? SDCardState.CHANGED_TO_REMOVED : value;
                        break;
                
                    case SDCardState.CHANGED_TO_REMOVED:
                        toSet = _currentSDCardState == SDCardState.CHANGED_TO_REMOVED ? SDCardState.REMOVED : value;
                        break;
                
                    case SDCardState.OK:
                        toSet = _currentSDCardState != SDCardState.OK && _currentSDCardState != SDCardState.CHANGED_TO_OK ? SDCardState.CHANGED_TO_OK : value;
                        break;
                
                    case SDCardState.CHANGED_TO_OK:
                        toSet = _currentSDCardState == SDCardState.CHANGED_TO_OK ? SDCardState.OK : value;
                        break;
                
                    default:
                        Debug.LogError($"SDCardState set: value == {value} is out of switch range! SDCardState value will not be updated.");
                        return;
                }

                _currentSDCardState = toSet;
            
                OnSDCardStateChanged?.Invoke(_currentSDCardState);
            }
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
        
            CurrentSDCardState = SDCardState.OK;
        
            PingSDCard();
        }

        async void PingSDCard()
        {
            if (_pingTimeSeconds < 0.1f)
            {
                Debug.LogError($"SD Card ping time == {_pingTimeSeconds} is too low!, ping time set to 1f");
                _pingTimeSeconds = 1f;
            }
        
            while (true)
            {
                //CurrentSDCardState = IsSDCardExists() ? SDCardState.OK : SDCardState.REMOVED;

                await UniTask.Delay((int)(_pingTimeSeconds * 1000));
            }
        }

        /// <summary>
        /// Проверить вставлена ли в девайс SD карта
        /// </summary>
        /// <returns>true если SD карта есть, false если нет</returns>
        public static bool IsSDCardExists()
        {
            // В юнити эдиторе можно переименовать папку Content чтоб сэмулировать вытаскивание SD карты
            return Directory.Exists(ContentPath.GetPlatformContentPath());
        }

        public string GetLogHeader()
        {
            return "Storages (Size Used Avail Use% Mounted on): " + Singleton<AndroidDeviceHelper>.Instance.GetStoragesInfo(false);
        }
    }
}