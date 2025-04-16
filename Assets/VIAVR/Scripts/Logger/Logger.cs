using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using VIAVR.Scripts.Data;
using Debug = CustomDebug.Debug;

namespace VIAVR.Scripts.Logger
{
    public interface ILogHeader
    {
        public string GetLogHeader();
    }

    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object _syncObject = new object();
    
        public int Size { get; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
        
            lock (_syncObject)
            {
                while (base.Count > Size)
                {
                    base.TryDequeue(out _);
                }
            }
        }
    }

    public class Logger : MonoBehaviour
    {
        enum SendResult
        {
            NONE, IN_PROGRESS, OK, ERROR
        }
    
        struct FullLogMessage
        {
            [JsonProperty("log")] public string Log;

            public FullLogMessage(string fullLog)
            {
                Log = fullLog;
            }
        }

        private const string LOG_FILENAME = "launcher_logs.txt";
    
        private string _logFileFullPath = "";

        [SerializeField] private int _logQueueSize = 100;
    
        private SendResult _lastSendResult = SendResult.NONE;
    
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private FixedSizedQueue<string> _logQueue;
    
        private bool _initialized;

        public void Initialize()
        {
            if(_initialized) return;

            _initialized = true;
        
            _logFileFullPath = ContentPath.GetFullPath(LOG_FILENAME, ContentPath.Storage.INTERNAL);
        
            _lastSendResult = SendResult.NONE;

            _logQueue = new FixedSizedQueue<string>(_logQueueSize);
        
            Debug.OnLogMessage += LogMessageReceivedHandler;
        
            RestoreSavedLogs();
        
            SendSavedLogs();

            StartCoroutine(SendCoroutine());
        }

        IEnumerator SendCoroutine()
        {
            WaitForSeconds waitForSeconds = new WaitForSeconds(5 * 60);

            for (;;)
            {
                yield return waitForSeconds;
            
                if(!_initialized) continue;
            
                SaveAndSendLogs();
            }
        }
    
#if UNITY_EDITOR
        // эмуляция OnApplicationPause в эдиторе (Ctrl + Shift + P)
        private void Awake()
        {
            UnityEditor.EditorApplication.pauseStateChanged += pauseState =>
            {
                OnApplicationPause(pauseState == UnityEditor.PauseState.Paused);
            };
        }
#endif

        private void OnApplicationPause(bool pauseStatus)
        {
            if(!_initialized) return;

            if (!pauseStatus)
            {
                Debug.Log("- Device Awake -", Debug.LoggerBehaviour.ADD);
            
                SaveAndSendLogs();
            }
        }

        private void OnApplicationQuit()
        {
            if(!_initialized) return;
        
            SaveAndSendLogs();
        }

        private string _lastDate;

        private void LogMessageReceivedHandler(LogType logType, string message, Debug.LoggerBehaviour loggerBehaviour)
        {
            if(loggerBehaviour == Debug.LoggerBehaviour.SKIP) return;
        
            string messageType = logType switch
            {
                LogType.Error => "[E]: ",
                LogType.Assert => "[A]: ",
                LogType.Warning => "[W]: ",
                LogType.Log => "",
                LogType.Exception => "[EX]: ",
                _ => throw new ArgumentOutOfRangeException(nameof(logType), logType, null)
            };

            _logQueue.Enqueue($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss UTCz}] {messageType}{message}");

            if (loggerBehaviour == Debug.LoggerBehaviour.IMPORTANT)
            {
                SaveAndSendLogs();
            }
        }

        /*void SendLogs(string logsText)
        {
            if(!_initialized) return;

            var appCore = Singleton<AppCore>.Instance;

            if(appCore == null || !appCore.WebServiceInitialized) return;

            if(_lastSendResult == SendResult.IN_PROGRESS) return;

            _lastSendResult = SendResult.IN_PROGRESS;

            _ = HttpClient.Instance.DoRoutine<bool>(HttpClient.RoutineType.SEND_LOGS, JsonConvert.SerializeObject(new FullLogMessage(logsText))).ContinueWith(result =>
            {
                _lastSendResult = result.IsSuccess ? SendResult.OK : SendResult.ERROR;
            });
        }*/

        private void RestoreSavedLogs()
        {
            if(!_initialized) return;
        
            if(!File.Exists(_logFileFullPath))
                return;

            var lines = File.ReadAllLines(_logFileFullPath);

            foreach (var line in lines)
                _logQueue.Enqueue(line);
        
            UnityEngine.Debug.Log($"LOGGER: logs restored: {lines.Length} lines");
        }

        private void SendSavedLogs()
        {
            if(!_initialized) return;
        
            if(!File.Exists(_logFileFullPath))
                return;
        
            //SendLogs(File.ReadAllText(_logFileFullPath));
        }

        public void SaveAndSendLogs()
        {
            if(!_initialized) return;
        
            string fullLog = GetLog();

            if (!_writing)
            {
                _writing = true;
            
                WriteLogAsync(fullLog).ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsCompleted || task.IsFaulted)
                    {
                        _writing = false;
                    
                        UnityEngine.Debug.Log($"LOGGER: '{_logFileFullPath}' saved!");
                    }
                });
            }
        
            //SendLogs(fullLog);
        }

        private IEnumerable<ILogHeader> _headers;

        private string GetLog()
        {
            if(!_initialized)
                return string.Empty;

            _stringBuilder.Clear();
        
            _stringBuilder.AppendLine($"# Logs updated at {DateTime.Now:dd.MM.yyyy HH:mm:ss UTCz}");

            _headers ??= FindObjectsOfType<MonoBehaviour>().OfType<ILogHeader>();

            if(_headers == null)
                return string.Empty;

            foreach (var header in _headers)
                _stringBuilder.AppendLine($"# {header.GetLogHeader()}");
        
            _stringBuilder.AppendLine("...");

            foreach (var logLine in _logQueue)
                _stringBuilder.AppendLine(logLine);

            return _stringBuilder.ToString();
        }
    
        private bool _writing;
    
        private async Task WriteLogAsync(string text)
        {
            using StreamWriter outputFile = new StreamWriter(_logFileFullPath);
        
            await outputFile.WriteAsync(text);
        }
    }
}