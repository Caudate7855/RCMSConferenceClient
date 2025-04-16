using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BestHTTP;
using BestHTTP.Caching;
using BestHTTP.Logger;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VIAVR.Scripts.Core;
using Debug = CustomDebug.Debug;

namespace VIAVR.Scripts.Network
{
    public class Result<T>
    {
        public readonly bool IsSuccess;
        public readonly T Data;

        public Result(bool isSuccess, T data)
        {
            IsSuccess = isSuccess && data != null;
            Data = data;
        }
    }

    public class RequestResult<T> : Result<T>
    {
        public int HttpStatusCode;
        public bool InternetError;
            
        public RequestResult(bool isSuccess, T data, int httpStatusCode, bool internetError) : base(isSuccess, data)
        {
            HttpStatusCode = httpStatusCode;
            InternetError = internetError;
        }
    }
    
    public class HttpClient
    {
        public static HttpClient Instance { get; private set; }

        private const int RequestMaxRetries = 0;
        private const bool LogOnlyNot200Responces = true;

        public event Action OnNoWifiConnection;
        public event Action On401; 

        public enum RoutineType
        {
            /*PING,
            GET_SESSION_DETAILS,
            GET_LATEST_UPDATE_VERSION,
            GET_ONLINE_CONTENT,
            GET_DEVICE_INFO,
            GET_PROMO_BANNERS_URLS,
            GET_PROMO_VIDEOS_URLS,
            
            SEND_ACTIVATE_DEVICE,
            SEND_ANALYST_USEREVENT,
            SEND_SESSION_RENEW,
            SEND_DEVICE_STATUS,
            SEND_DEVICE_INFO,
            SEND_ACTIVATE_SUBSCRIPTION,
            SEND_ACTIVATE_VIP,
            SEND_START_SESSION,
            SEND_LOGS,
            SEND_HOTEL_PAYMENT,*/
            LINK_DEVICE,
            DEVICE_DETAILS,
            DEVICE_RUNTIME,
            DEVICE_FIRMWARE,
            DEVICE_APPS,
            GET_CMD,
            DELETE_CMD
        }
        
        public struct Header
        {
            public string Key;
            public string Value;

            public Header(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }
        
        public struct RoutineParams
        {
            public string URL;
            public Type ReturnType;
            public int ParamsCount;
            
            public bool UseHttpCaching; // не видно что это работает, но 304 в каких то запросах проскакивало
            
            public RoutineParams(string url, Type returnType, int paramsCount = 0, bool useHttpCaching = true)
            {
                URL = url;
                ReturnType = returnType;
                ParamsCount = paramsCount;
                UseHttpCaching = useHttpCaching;
            }
        }

        private static readonly TimeSpan DefaultRequestConnectTimeout = TimeSpan.FromSeconds(5); // таймаут при попытке соединения
        private static readonly TimeSpan DefaultRequestNonDownloadTimeout = TimeSpan.FromSeconds(10); // таймаут на весь запрос (сорединение + ожидание ответа + скачивание и т.д.)

        // "данные" типичных запросов приложухи
        private static readonly Dictionary<RoutineType, RoutineParams> RoutinesDictonary = new Dictionary<RoutineType, RoutineParams>()
        {
            /*{ RoutineType.PING, new RoutineParams(APIConnectionConfiguration.PingUrl, typeof(bool)) },
            { RoutineType.GET_SESSION_DETAILS, new RoutineParams(APIConnectionConfiguration.CustomSessionUrl, typeof(CustomSessionDetails)) },
            { RoutineType.GET_LATEST_UPDATE_VERSION, new RoutineParams(APIConnectionConfiguration.FirmwaresLatest, typeof(FirmwareInfoModel)) },
            //{ RoutineType.GET_ONLINE_CONTENT, new RoutineParams(APIConnectionConfiguration.CinemaContentUrl, typeof(VrCinemaModels.ServerCinemaContentModel), useHttpCaching: true) },
            { RoutineType.GET_DEVICE_INFO, new RoutineParams(APIConnectionConfiguration.DeviceUrl, typeof(CompanyDeviceDetails)) },
            { RoutineType.GET_PROMO_BANNERS_URLS, new RoutineParams(APIConnectionConfiguration.BannersUrl, typeof(CompanyBanners)) },
            { RoutineType.GET_PROMO_VIDEOS_URLS, new RoutineParams(APIConnectionConfiguration.VideoBannersUrl, typeof(CompanyVideoBannerDetails[])) },
            
            { RoutineType.SEND_ACTIVATE_DEVICE, new RoutineParams(APIConnectionConfiguration.ActivateUrl, typeof(TokenModel), 1) },
            { RoutineType.SEND_ANALYST_USEREVENT, new RoutineParams("URL прокидывается в parameter[0]", typeof(bool), 2) },
            { RoutineType.SEND_SESSION_RENEW, new RoutineParams(APIConnectionConfiguration.SessionUrl, typeof(SessionDetails)) },
            { RoutineType.SEND_DEVICE_STATUS, new RoutineParams(APIConnectionConfiguration.DeviceStatusUrl, typeof(bool)) },
            { RoutineType.SEND_DEVICE_INFO, new RoutineParams(APIConnectionConfiguration.DeviceInfoUrl, typeof(bool)) }, // урл модифицируется: url.Replace("{device_id}",androidHelper.GetDeviceId())
            { RoutineType.SEND_ACTIVATE_SUBSCRIPTION, new RoutineParams(APIConnectionConfiguration.SendPaymentSelecturl, typeof(string), 3) },
            { RoutineType.SEND_ACTIVATE_VIP, new RoutineParams(APIConnectionConfiguration.ActivateVipUrl, typeof(ActivateSubscriptionList)) },
            { RoutineType.SEND_START_SESSION, new RoutineParams(APIConnectionConfiguration.CustomSessionUrl, typeof(bool)) },
            { RoutineType.SEND_LOGS, new RoutineParams(APIConnectionConfiguration.Logs, typeof(bool), 1) },
            { RoutineType.SEND_HOTEL_PAYMENT, new RoutineParams(APIConnectionConfiguration.HotelPayment, typeof(bool), 1) }*/
            
            { RoutineType.LINK_DEVICE, new RoutineParams(APIConnectionConfiguration.LinkDeviceUrl, returnType: typeof(LinkDeviceResult), paramsCount: 2) },
            { RoutineType.DEVICE_DETAILS, new RoutineParams(APIConnectionConfiguration.DeviceDetailsUrl, returnType: typeof(DeviceDetailsResult)) },
            { RoutineType.DEVICE_RUNTIME, new RoutineParams(APIConnectionConfiguration.DeviceRuntimeUrl, returnType: typeof(DeviceRuntimeResult), paramsCount: 3) },
            { RoutineType.DEVICE_FIRMWARE, new RoutineParams(APIConnectionConfiguration.DeviceFirmwareUrl, returnType: typeof(DeviceFirmwareResult)) },
            { RoutineType.DEVICE_APPS, new RoutineParams(APIConnectionConfiguration.DeviceAppsUrl, returnType: typeof(DeviceAppResult[])) },
            { RoutineType.GET_CMD, new RoutineParams(APIConnectionConfiguration.DeviceCmd, returnType: typeof(DeviceCmdResult)) },
            { RoutineType.DELETE_CMD, new RoutineParams(APIConnectionConfiguration.DeviceCmd, returnType: typeof(string)) },
        };

        private AppCore _appCore;
        private AndroidDeviceHelper _androidDeviceHelper;

        public bool BlockRequests { get; set; } = false;

        public bool IsConnectionStable => _timeoutsRow < 3;
        
        public int SuccessRequestsPercentage => TotalRequests == 0 ? 0 : Mathf.RoundToInt(_successRequestCount / (float)(_successRequestCount + _failedRequestCount) * 100);
        public int TotalRequests => _successRequestCount + _failedRequestCount;

        private int _timeoutsRow; // таймаутов подряд
        private int _successRequestCount = 0;
        private int _failedRequestCount = 0;
        
        private readonly Dictionary<string, int> _downloadProgressDictionary = new Dictionary<string, int>();
        
        public void Initialize(AppCore appCore, AndroidDeviceHelper androidDeviceHelper)
        {
            if(Instance != null) return;
            
            Instance = this;

            _appCore = appCore;
            _androidDeviceHelper = androidDeviceHelper;
            
            HTTPManager.ConnectTimeout = DefaultRequestConnectTimeout;
            HTTPManager.KeepAliveDefaultValue = false;
            HTTPManager.IsCachingDisabled = false;
            HTTPManager.Logger.Level = Loglevels.None;
            
            HTTPCacheService.BeginMaintainence(new HTTPCacheMaintananceParams(TimeSpan.FromDays(7), 50 * 1024 * 1024));
        }

        /// <summary>
        /// Выполнить типичный запрос
        /// </summary>
        /// <param name="routineType">Вид запроса из предопределенного списка</param>
        /// <param name="parameters">Строковые параметры (необязательно)</param>
        /// <typeparam name="T">Возвращаемый тип</typeparam>
        /// <returns>UniTask запроса</returns>
        public async UniTask<RequestResult<T>> DoRoutine<T>(RoutineType routineType, params object[] parameters)
        {
            // возвращаемый тип задачи должен соответствовать прописанному в словаре
            if (!RoutinesDictonary.ContainsKey(routineType) || typeof(T) != RoutinesDictonary[routineType].ReturnType)
            {
                Debug.LogError($"HttpClient.DoRoutine(): Type '{typeof(T)}' mismatch for '{routineType}', supported type is '{RoutinesDictonary[routineType].ReturnType}'");
                return new RequestResult<T>(false, default, -1, false);
            }

            // количество передаваемых параметров не может отличаться от прописанного в словаре
            if (RoutinesDictonary[routineType].ParamsCount != parameters.Length)
            {
                Debug.LogError($"HttpClient.DoRoutine(): Parameters count mismatch for '{routineType}', current count '{parameters.Length}', target count '{RoutinesDictonary[routineType].ParamsCount}'");
                return new RequestResult<T>(false, default, -1, false);
            }

            // параметры могут быть пустыми
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] == null)
                    Debug.Log($"HttpClient.DoRoutine(): {routineType} parameter [{i}] is null!");
            }
            
            RequestResult<string> result = null;
            T data;

            string url = RoutinesDictonary[routineType].URL;

            bool useCaching = RoutinesDictonary[routineType].UseHttpCaching;

            // Обработка типичных запросов. Большинство GET запросов выполняются по одному шаблону
            switch (routineType)
            {
                case RoutineType.LINK_DEVICE:

                    string linkModel = new LinkDeviceRequest((string)parameters[0], (string)parameters[1]).ToJson();

                    result = await POST(url, linkModel, useCaching);
                    
                    if (result.IsSuccess && JsonHelper.TryParseJson(result.Data, out data))
                        return new RequestResult<T>(true, data, result.HttpStatusCode, result.InternetError);

                    break;
                
                case RoutineType.DELETE_CMD:

                    result = await DELETE(url, useCaching);
                    
                    if(result.IsSuccess)
                        return new RequestResult<T>(true, (T)Convert.ChangeType(result.Data, typeof(T)), result.HttpStatusCode, result.InternetError); // если data null то IsSuccess = false
                    
                    break;
                
                case RoutineType.DEVICE_DETAILS:
                case RoutineType.DEVICE_FIRMWARE:
                case RoutineType.GET_CMD:
                case RoutineType.DEVICE_APPS:
                    
                    result = await GET(url, useCaching);

                    // Ответ сервера приходит в JSON
                    if (result.IsSuccess && JsonHelper.TryParseJson(result.Data, out data))
                        return new RequestResult<T>(true, data, result.HttpStatusCode, result.InternetError); // (T)Convert.ChangeType(result.Data, typeof(T))
                    
                    break;
                
                case RoutineType.DEVICE_RUNTIME:
                    
                    string runtimeModel = new DeviceRuntimeRequest((int)parameters[0], (string)parameters[1], (bool)parameters[2]).ToJson();

                    result = await POST(url, runtimeModel, useCaching);
                    
                    if (result.IsSuccess && JsonHelper.TryParseJson(result.Data, out data))
                        return new RequestResult<T>(true, data, result.HttpStatusCode, result.InternetError);
                    
                    break;
                
                default:
                    Debug.LogError($"Unhandled RoutineType: {routineType}");
                    break;
            }
            
            return new RequestResult<T>(result != null && result.IsSuccess, default, result?.HttpStatusCode ?? -1, result?.InternetError ?? true);
        }

        // Обертка GET запроса
        public UniTask<RequestResult<string>> GET(string url, bool useCaching = false, params Header[] headers)
        {
            return Request(url, HTTPMethods.Get, null, useCaching, headers);
        }
        
        // Обертка POST запроса
        public UniTask<RequestResult<string>> POST(string url, string data, bool useCaching = false, params Header[] headers)
        {
            return Request(url, HTTPMethods.Post, data, useCaching, headers);
        }
        
        // Обертка DELETE запроса
        public UniTask<RequestResult<string>> DELETE(string url, bool useCaching = false, params Header[] headers)
        {
            return Request(url, HTTPMethods.Delete, null, useCaching, headers);
        }
        
        private static readonly string ReqMethod = $"{Utils.HighlightPrefix} ";
        private static readonly string ReqMethodError = $"{Utils.ErrorPrefix} ";

        // Универсальная процедура запроса
        private UniTask<RequestResult<string>> Request(string url, HTTPMethods httpMethod, string data, bool useCaching, params Header[] headers)
        {
            // У ассета есть своя поддержка await, но функционал у нее победнее в плане отслеживания стейтов запроса
            var completionSource = new UniTaskCompletionSource<RequestResult<string>>();

#if UNITY_EDITOR
            // Примитивная имитация отсутствия инета (точнее связи с сервером) только для юнити редактора, при этом нет влияния на состояния окна вайфая
            if (BlockRequests)
            {
                Debug.Log($"{Utils.WarningPrefix}HttpClient NO INTERNET EMULATION {Utils.WarningPrefix}");

                _timeoutsRow++;
                _failedRequestCount++;
                
                completionSource.TrySetResult(new RequestResult<string>(false, null, -1, true));
                return completionSource.Task;
            }
#endif
            
            var request = new HTTPRequest(new Uri(url), methodType: httpMethod, callback: (request, response) => // надо брабатывать response только когда request.State == Finished
            {
                //https://benedicht.github.io/BestHTTP-Documentation/pages/best_http2/protocols/http/RequestStates.html
                switch (request.State)
                {
                    case HTTPRequestStates.Initial:
                    case HTTPRequestStates.Queued:
                    case HTTPRequestStates.Processing:
                        break;
                    
                    // Запрос прошел, результат надо проверять на ошибки сервера.
                    case HTTPRequestStates.Finished:
                        _timeoutsRow = 0;
                        _successRequestCount++;

                        if(!LogOnlyNot200Responces || response.StatusCode != 200)
                            Debug.Log($"{ReqMethod}{httpMethod.ToString().ToUpper()} '{request.Uri}' -> {response.StatusCode} ({response.IsSuccess})");
                        
                        if(response.StatusCode == 401)
                            On401?.Invoke();
                        
                        completionSource.TrySetResult(new RequestResult<string>(response.IsSuccess, response.DataAsText, response.StatusCode, false));
                        break;
                    
                    // запрос не прошел
                    // https://benedicht.github.io/BestHTTP-Documentation/pages/best_http2/protocols/http/ErrorHandling.html
                    case HTTPRequestStates.Error:
                    case HTTPRequestStates.Aborted:
                    case HTTPRequestStates.ConnectionTimedOut:
                    case HTTPRequestStates.TimedOut:
                        _timeoutsRow++; // Aborted это не таймаут, но т.к. Abort() не юзается то можно игнорить
                        _failedRequestCount++;
                        
                        var deviceParameters = _androidDeviceHelper.GetDeviceParameters();
                        
                        Debug.Log($"{ReqMethodError}'{request.Uri}' state: {request.State}{(request.Exception != null ? $" ({request.Exception.Message})" : "" )}, WIFI: [{deviceParameters.ToWifiInfoString()}]", Debug.LoggerBehaviour.ADD);
                        
                        if(!_androidDeviceHelper.IsWifiConnected())
                            OnNoWifiConnection?.Invoke();

                        completionSource.TrySetResult(new RequestResult<string>(false, null, -1, true));
                        break;
                    
                    default:
                        Debug.Log($"{ReqMethodError}Unhandled HTTPRequestStates: {request.State}");
                        break;
                }
            });

            request.MaxRetries = RequestMaxRetries;
            
            request.SetHeader("token", _appCore.TOKEN);
            request.SetHeader("serial", _appCore.SERIAL);
            
            foreach (var header in headers)
                request.SetHeader(header.Key, header.Value);

            if (data != null)
                request.RawData = Encoding.UTF8.GetBytes(data);

            request.DisableCache = !useCaching;
            request.Timeout = DefaultRequestNonDownloadTimeout;

            request.Send();

            return completionSource.Task;
        }

        private static readonly string DlMethod = $"{Utils.HighlightPrefix}HttpClient.DownloadFile(): ";
        private static readonly string DlMethodError = $"{Utils.ErrorPrefix}HttpClient.DownloadFile(): ";
        
        /// <summary>
        /// Загрузить файл с сервера
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="filePath">Полный путь к сохраняемому файлу</param>
        /// <param name="timeoutSeconds">Таймаут, 10 минут по умолчанию</param>
        /// <returns></returns>
        public UniTask<Result<bool>> DownloadFile(string url, string filePath, int timeoutSeconds = 600, bool progressTracking = false)
        {
            var completionSource = new UniTaskCompletionSource<Result<bool>>();

#if UNITY_EDITOR
            if (BlockRequests)
            {
                Debug.Log($"{Utils.WarningPrefix}HttpClient NO INTERNET EMULATION");
                
                completionSource.TrySetResult(new RequestResult<bool>(false, false, -1, true));
                return completionSource.Task;
            }
#endif
            
            var request = new HTTPRequest(new Uri(url), (request, response) => // надо брабатывать response только когда request.State == Finished
            {
                if (request.Tag is FileStream fs)
                    fs.Dispose();
                
                switch (request.State)
                {
                    case HTTPRequestStates.Initial:
                    case HTTPRequestStates.Queued:
                    case HTTPRequestStates.Processing:
                        break;
                    
                    case HTTPRequestStates.Finished:
                        _successRequestCount++;
                        
                        Debug.Log(response.IsSuccess
                            ? $"{DlMethod}ok '{request.Uri}' saved to '{filePath}'"
                            : $"{DlMethodError}ERROR: '{request.Uri}' '{response.StatusCode}' '{response.Message}'");

                        completionSource.TrySetResult(new RequestResult<bool>(response.IsSuccess, true, response.StatusCode, false));
                        break;
                    
                    case HTTPRequestStates.Error:
                    case HTTPRequestStates.Aborted:
                    case HTTPRequestStates.ConnectionTimedOut:
                    case HTTPRequestStates.TimedOut:
                        _failedRequestCount++;
                        
                        DownloadFailed(request, filePath, request.Exception != null ? request.Exception.Message : $"state: {request.State} (file not downloaded!)");
                        
                        if(!_androidDeviceHelper.IsWifiConnected())
                            OnNoWifiConnection?.Invoke();

                        completionSource.TrySetResult(new RequestResult<bool>(false, false, -1, true));
                        break;
                    
                    default:
                        Debug.Log($"{DlMethodError}Unhandled HTTPRequestStates: {request.State}");
                        
                        if(File.Exists(filePath))
                            File.Delete(filePath);
                        break;
                }
            });

            // Файл сохраняется через стриминг, что экономит оперативную память
            request.OnStreamingData += (httpRequest, response, fragment, length) =>
            {
                if (response.IsSuccess)
                {
                    var fs = httpRequest.Tag as FileStream;

                    if (fs == null)
                    {
                        try
                        {
                            httpRequest.Tag = fs = new FileStream(filePath, FileMode.Create);
                        }
                        catch (Exception e)
                        {
                            DownloadFailed(httpRequest, filePath, e.Message);

                            completionSource.TrySetResult(new RequestResult<bool>(false, false, response.StatusCode, true));
                            return false;
                        }
                    }

                    try
                    {
                        fs.Write(fragment, 0, length);
                    }
                    catch (Exception e)
                    {
                        DownloadFailed(httpRequest, filePath, e.Message);

                        completionSource.TrySetResult(new RequestResult<bool>(false, false, response.StatusCode, true));
                        return false;
                    }
                }

                return true;
            };
            
            if (progressTracking)
                request.OnDownloadProgress = OnDownloadProgress;

            request.MaxRetries = RequestMaxRetries;
            
            request.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            request.Send();
            
            return completionSource.Task;
        }
        
        void OnDownloadProgress(HTTPRequest request, long downloaded, long length)
        {
            string localPath = request.Uri.LocalPath;
            
            if (!_downloadProgressDictionary.ContainsKey(localPath))
                _downloadProgressDictionary.Add(localPath, 0);
            
            int percents = Mathf.RoundToInt(downloaded / (float)length * 100.0f);
            
            if (percents % 20 == 0 && percents != _downloadProgressDictionary[localPath])
            {
                _downloadProgressDictionary[localPath] = percents;
                Debug.Log($"[{DateTime.Now:HH:mm:ss}] '{Path.GetFileName(localPath)}' dl progress: {percents}%", Debug.LoggerBehaviour.ADD);
            }
        }

        // Вместо копипаста кода
        void DownloadFailed(HTTPRequest request, string filePath, string errorMsg)
        {
            var deviceParameters = _androidDeviceHelper.GetDeviceParameters();
            
            Debug.Log($"{DlMethodError}'{request.Uri}' DOWNLOAD ERROR: {errorMsg}, WIFI: [{deviceParameters.ToWifiInfoString()}]", Debug.LoggerBehaviour.ADD);
                        
            if(File.Exists(filePath))
                File.Delete(filePath);
        }
    }
    
#region Classes

    public class CustomSessionDetails
    {
        public string custom_session_id { get; set; }
        public string started_at { get; set; }
        public string expired_at { get; set; }
    }

    public class SendLinkData
    {
        public Guid package_id { get; set; }
        public string email { get; set; }
        public string mobile { get; set; }
    }

    public class CompanyBanners
    {
        public CompanyBannerDetails[] Banners { get; set; }
    }

    public class CompanyBannerDetails
    {
        public string id { get; set; }
        public string banner_type { get; set; }
        public string description { get; set; }
        public string[] images { get; set; }
        public string layout_id { get; set; }
    }

    public class CompanyVideoBannerDetails
    {
        public string id { get; set; }
        public string name { get; set;}
        public string vr_type { get; set; }
        public string vr_format { get; set; }
        public string url { get; set; }
        public string image { get; set; }
        public bool disabled { get; set; }
    }

#endregion

}