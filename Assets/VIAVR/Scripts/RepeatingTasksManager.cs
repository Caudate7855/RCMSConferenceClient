using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using VIAVR.Scripts.Logger;
using Debug = CustomDebug.Debug;

namespace VIAVR.Scripts
{
    public class RepeatingTasksManager : MonoBehaviour, ILogHeader
    {
        public class RepeatingTask
        {
            public string Name; // идентификатор, для каждой отдельной задачи должен быть уникальным
            public Func<UniTask<bool>> TaskFunction;
            public int IntervalSeconds; // задача будет добавляться в очередь на выполнение каждые IntervalSeconds секунд
            public int IntervalSecondsFailed; // если предыдущая попытка выполнить задачу вернула ошибку, повторить её через IntervalSecondsFailed секунд
            public bool OnlyOneInQueue; // при OnlyOneInQueue == true задача не будет добавляться в очередь, если она уже стоит в очереди
            public bool WaitForComplete; // при WaitForComplete == false асинхронная задача не будет блокировать (await) очередь пока не выполнится (длительным задачам рекомендуется ставить false)

            public bool Enabled = false; // задача не будет добавляться в очередь выполнения если Enabled == false
            public bool LastSuccessState = true; // результат предыдущего выполнения
            public bool CheckFailed = false; // отличаются ли IntervalSeconds и IntervalSecondsFailed

            public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

            public string DisplayedName => $"{(WaitForComplete ? "" : "~")}'{Name}'";

            public RepeatingTask(string taskName, Func<UniTask<bool>> taskFunction, int intervalSeconds, int intervalSecondsFailed, bool onlyOneInQueue = true, bool waitForComplete = true)
            {
                Name = taskName;
                TaskFunction = taskFunction;
                IntervalSeconds = intervalSeconds;
                IntervalSecondsFailed = intervalSecondsFailed;
                OnlyOneInQueue = onlyOneInQueue;
                WaitForComplete = waitForComplete;
            }
        }

        [SerializeField] private int _delayBetweenTasksMs = 1000; // пауза между выполнением задач
        [SerializeField] private int _taskTimeoutSeconds = 10;

        [ShowNativeProperty] public int TasksAddedCount => _repeatingTasks.Count;
        [ShowNativeProperty] public int TasksQueueCount => _repeatingTasksQueue.Count;
        [ShowNativeProperty] public int TasksRunningCount => _currentRunningTasks.Count;

        [SerializeField] [ReadOnly] [TextArea] private string _currentRunningTasksNames;

        private readonly Dictionary<string, RepeatingTask> _repeatingTasks = new Dictionary<string, RepeatingTask>();
        private readonly Queue<RepeatingTask> _repeatingTasksQueue = new Queue<RepeatingTask>();
        private readonly List<RepeatingTask> _currentRunningTasks = new List<RepeatingTask>(); // Name'ы задач которые выполняются в данный момент

        private TimeSpan _timeoutTimeSpan;

        public bool Initialized { get; private set; } = false;

        public void Initialize()
        {
            if (Initialized) return;

            Initialized = true;
        
            _timeoutTimeSpan = TimeSpan.FromSeconds(_taskTimeoutSeconds);
        
            DoTasks();
        }

        /// <summary>
        /// Создать задачу. Задача встанет в очередь на выполнение в момент её создания.
        /// Выполнится в первый раз сразу после создания + время ожидания в очереди
        /// </summary>
        /// <param name="taskName">Любое уникальное имя</param>
        /// <param name="taskFunction">Собственно задача</param>
        /// <param name="intervalSeconds">Периодичность выполнения в секундах (значение приблизительное!)</param>
        /// <param name="intervalSecondsFailed">Периодичность выполнения задачи вернувшей ошибку в секундах (значение приблизительное!)</param>
        /// <param name="onlyOneInQueue">Не добавлять задачу в очередь если она там уже стоит (по умолчанию false)</param>
        /// <param name="waitForComplete">Ждать выполнение задачи прежде чем выполнять последующую в очереди (по умолчанию true)</param>
        public void CreateTask(string taskName, Func<UniTask<bool>> taskFunction, int intervalSeconds, int? intervalSecondsFailed = null, bool onlyOneInQueue = true, bool waitForComplete = true)
        {
            if (_repeatingTasks.ContainsKey(taskName))
            {
                Debug.LogError($"RepeatingTasksManager.AddTask(): task '{taskName}' already exists!");
                return;
            }
        
            var task = new RepeatingTask(taskName, taskFunction, intervalSeconds, intervalSecondsFailed ?? intervalSeconds, onlyOneInQueue, waitForComplete);

            task.CheckFailed = intervalSecondsFailed.HasValue;
        
            _repeatingTasks.Add(taskName, task);

            RunTask(task);
        }

        /// <summary>
        /// Создать "простую" задачу, которая будет сразу выполняться, а не ставиться в очередь выполнения
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="taskFunction"></param>
        /// <param name="intervalSeconds"></param>
        /// <param name="intervalSecondsFailed"></param>
        public void CreateSimpleTask(string taskName, Func<UniTask<bool>> taskFunction, int intervalSeconds, int? intervalSecondsFailed = null)
        {
            if (_repeatingTasks.ContainsKey(taskName))
            {
                Debug.LogError($"RepeatingTasksManager.AddTask(): task '{taskName}' already exists!");
                return;
            }
        
            var task = new RepeatingTask(taskName, taskFunction, intervalSeconds, intervalSecondsFailed ?? intervalSeconds, false, false);
        
            task.CheckFailed = intervalSecondsFailed.HasValue;
        
            _repeatingTasks.Add(taskName, task);

            RunTask(task, true);
        }
    
        /// <summary>
        /// Удалить задачу.
        /// </summary>
        /// <param name="taskName">Уникальное имя добавленной ранее задачи</param>
        public void DeleteTask(string taskName)
        {
            if (_repeatingTasks.ContainsKey(taskName))
            {
                _repeatingTasks[taskName].Enabled = false;
            
                _repeatingTasks.Remove(taskName);
            }
        }

        // Процедура периодического добавления созданной задачи в очередь выполнения, или выполнения задачи если simpleRun = true
        private async void RunTask(RepeatingTask repeatingTask, bool simpleRun = false)
        {
            repeatingTask.Enabled = true;
        
            while (repeatingTask.Enabled)
            {
                if (simpleRun)
                {
                    // сразу выполнить задачу
                    _ = repeatingTask.TaskFunction().ContinueWith(success =>
                    {
                        repeatingTask.LastSuccessState = success;
                    
                        if(!success && repeatingTask.CheckFailed)
                            repeatingTask.CancellationTokenSource.Cancel();
                    });
                }
                else
                {
                    // ставим в очередь если нет ограничения на количество копий в очереди, или если в очереди нет копий и копия не выполняется
                    if (!repeatingTask.OnlyOneInQueue ||
                        !(_repeatingTasksQueue.Any(task => task.Name == repeatingTask.Name) || _currentRunningTasks.Any(task => task.Name == repeatingTask.Name)))
                    {
                        _repeatingTasksQueue.Enqueue(repeatingTask);
                    }
                }
            
                await UniTask.Delay(repeatingTask.IntervalSeconds * 1000, cancellationToken: repeatingTask.CancellationTokenSource.Token).SuppressCancellationThrow();
            
                if (repeatingTask.CancellationTokenSource.IsCancellationRequested)
                {
                    repeatingTask.CancellationTokenSource = new CancellationTokenSource();
                
                    if(repeatingTask.LastSuccessState == false)
                        Debug.Log($"Failed task '{repeatingTask.Name}' delayed by {repeatingTask.IntervalSecondsFailed}s", Debug.LoggerBehaviour.ADD);
                
                    await UniTask.Delay(repeatingTask.IntervalSecondsFailed * 1000);
                }
            }
        }

        // Процедура выполнения задач по очереди
        private async void DoTasks()
        {
            while (Initialized)
            {
                await UniTask.Delay(_delayBetweenTasksMs);
            
                if(_repeatingTasksQueue.Count == 0) continue;

                var task = _repeatingTasksQueue.Dequeue();
            
                if(task.Enabled == false) continue;
            
                if(_currentRunningTasks.Any(currentTask => currentTask.Name == task.Name))
                {
                    Debug.Log($"RepeatingTasksManager.DoTasks() before TaskFunction(): same task '{task.Name}' is not completed yet...", Debug.LoggerBehaviour.ADD);
                    continue; // не запускаем новую если точно такая же таска еще не выполнилась
                }
            
                AddCurrentRunningTask(task);

                if (task.WaitForComplete)
                {
                    // если таска каким-то образом заглохнет то сработает таймаут
                    try
                    {
                        var success = await task.TaskFunction().Timeout(_timeoutTimeSpan);
                    
                        task.LastSuccessState = success;
                    
                        if(!success && task.CheckFailed)
                            task.CancellationTokenSource.Cancel();
                    }
                    catch (TimeoutException)
                    {
                        Debug.LogError($"Task '{task.Name}' was cancelled by timeout ({_taskTimeoutSeconds}s)");
                    }

                    /*await UniTask.WhenAny(
                    task.TaskFunction(),
                    UniTask.Delay(_taskTimeoutSeconds * 1000));*/

                    await UniTask.SwitchToMainThread();
                
                    RemoveCurrentRunningTask(task);
                }
                else
                {
                    _ = task.TaskFunction().ContinueWith(async success =>
                    {
                        task.LastSuccessState = success;
                    
                        await UniTask.SwitchToMainThread();
                    
                        RemoveCurrentRunningTask(task);
                    
                        if(!success && task.CheckFailed)
                            task.CancellationTokenSource.Cancel();
                    });
                }
            }
        }

        private void AddCurrentRunningTask(RepeatingTask task)
        {
            _currentRunningTasks.Add(task);

#if UNITY_EDITOR
            _currentRunningTasksNames = string.Join(", ", _currentRunningTasks.Select(repeatingTask => repeatingTask.DisplayedName));
#endif
        }
    
        private void RemoveCurrentRunningTask(RepeatingTask task)
        {
            _currentRunningTasks.Remove(task);

#if UNITY_EDITOR
            _currentRunningTasksNames = string.Join(", ", _currentRunningTasks.Select(repeatingTask => repeatingTask.DisplayedName));
#endif
        }

        /// <summary>
        /// Запустить выполнение задачи, ранее добавленной через CreateTask
        /// </summary>
        /// <param name="taskName">Имя задачи, заданное через CreateTask</param>
        /// <param name="runIfAlreadyRunning">Запустить копию задачи если она выполняется в этот момент</param>
        public async UniTask ForceRunTask(string taskName, bool runIfAlreadyRunning = false)
        {
            if (!_repeatingTasks.TryGetValue(taskName, out var task))
            {
                Debug.LogError($"RepeatingTasksManager.ForceRunTask(): task with name '{taskName}' not found!");
                return;
            }

            if (!runIfAlreadyRunning && _currentRunningTasks.Any(currentTask => currentTask.Name == taskName))
            {
                Debug.LogError($"RepeatingTasksManager.ForceRunTask(): task with name '{taskName}' already running, set runIfAlreadyRunning = true to run several tasks");
                return;
            }
        
            AddCurrentRunningTask(task);
        
            await task.TaskFunction();

            await UniTask.SwitchToMainThread();
                
            RemoveCurrentRunningTask(task);
        }

        public string GetLogHeader()
        {
            return $"RepeatingTasksManager: tasks created: {_repeatingTasks.Count}, queue: {_repeatingTasksQueue.Count}, running: [{_currentRunningTasksNames}]";
        }
    }
}