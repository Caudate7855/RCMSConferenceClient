using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using CustomDebug;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UIManager.Windows;
using UnityEngine.Networking;
using Zenject;

namespace Services
{
    [UsedImplicitly]
    public class VideoDownloader
    {
        [Inject] private RemoteRequestController _remoteRequestController;
        [Inject] private VideoEncrypter _videoEncrypter;
        [Inject] private UrlConverter _urlConverter;
        [Inject] private DeviceStorageManager _deviceStorageManager;
        [Inject] private AppFSM _appFsm;
        
        private int _downloadedContentCount;
        private StartedScreenController _startedScreenController;
        private CancellationTokenSource _cancellationTokenSource;
        private VideoInfos _videoInfos;
        private VideoInfos _currentVideoInfos;
        private bool _isNeedRestartDownloading;
        private long _lastDownloadedFileSize;
        private bool _isDownloading;
        
        private const int DownloadMaxTimeInMinutes = 1440;
        private const int ChunkSize = 10 * 1024 * 1024; // 10 mb
        private const string VideoDownloaderDebugPrefix = "Video Downloader debug - ";

        public event Action<DownloadInfoData> OnDownloadInfoChanged;
        public event Action OnDownloadingStarted;

        public bool IsLastContent { get; private set; }
        public long DownloadedSize { get; private set; }
        public bool IsDownloading { get; private set; }
        public string CurrentDownloadContentID { get; set; }
        

        public async void DownloadRemoteContentAsync()
        {
            if(_isDownloading)
                return;
            
            _isDownloading = true;
            
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            try
            {
                _videoInfos = await _remoteRequestController.GetVideoToDownloadAsync();
                _currentVideoInfos = _videoInfos;
                Debug.Log($"VideoDownloader - DownloadRemoteContentAsync() _videoInfos.VideoToDownloadInfos.Count- " +
                          $"{_videoInfos.VideoToDownloadInfos.Count}");

                await _deviceStorageManager.CleanFolderExceptAsync(_videoInfos.VideoToDownloadInfos);
                
                for (int i = 0; i < _videoInfos.VideoToDownloadInfos.Count; i++)
                {
                    IsLastContent = false;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.Log($"{VideoDownloaderDebugPrefix}Загрузка отменена");
                        break;
                    }

                    _downloadedContentCount = GetDownloadedContentCount(_videoInfos);
                    
                    IsLastContent = (i == _videoInfos.VideoToDownloadInfos.Count - 1);
                    Debug.Log($"IsLastContent - {IsLastContent}. _downloadedContentCount {_downloadedContentCount}" +
                              $"_videoInfos.VideoToDownloadInfos.Count - 1 {_videoInfos.VideoToDownloadInfos.Count - 1}");
                    
                    var contentPath = _videoInfos.VideoToDownloadInfos[i].format == "2D" || _videoInfos.VideoToDownloadInfos[i].format == "3D"
                        ? _deviceStorageManager.ContentLocation + _videoInfos.VideoToDownloadInfos[i].title + ".mp4"
                        : _deviceStorageManager.ContentLocation + _videoInfos.VideoToDownloadInfos[i].title + ".png";
                    
                    if (File.Exists(contentPath))
                    {
                        IsDownloading = true;
                        CurrentDownloadContentID = _videoInfos.VideoToDownloadInfos[i].id;
                    
                        DownloadedSize = new FileInfo(contentPath).Length;

                        if (_videoInfos.VideoToDownloadInfos[i].format == "2D" || _videoInfos.VideoToDownloadInfos[i].format == "3D")
                            DownloadedSize -= 1; //Не считаем один байт, использованный для шифрования видео контента
                        
                        await _remoteRequestController.SendExchangeDataAsync();
                        
                        Debug.Log($"Файл {contentPath} уже создан");
                        IsDownloading = false;
                    
                        continue;
                    }
                    
                    await DownloadSpecificContentAsync(_videoInfos.VideoToDownloadInfos[i], cancellationToken);
                    
                    if (_isNeedRestartDownloading)
                    {
                        Debug.Log($"{VideoDownloaderDebugPrefix}Перезапуск загрузки...");
                        i = -1;
                        _isNeedRestartDownloading = false;
                        _videoInfos = await _remoteRequestController.GetVideoToDownloadAsync();
                        _currentVideoInfos = _videoInfos;
                    }
                }
            }
            catch
            {
                Debug.Log($"{VideoDownloaderDebugPrefix}Загрузка была отменена.");
                _appFsm.SetState<StartedState>();
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _isDownloading = false;
        }

        public void StopDownloading()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _isDownloading = false;
        }

        private int GetDownloadedContentCount(VideoInfos videoInfos)
        {
            int downloadedContentCount = 0;

            for (int i = 0; i < _videoInfos.VideoToDownloadInfos.Count; i++)
            {
                var videoFilePath = _deviceStorageManager.ContentLocation + videoInfos.VideoToDownloadInfos[i].title +
                                    ".mp4";
                var imageFilePath = _deviceStorageManager.ContentLocation + videoInfos.VideoToDownloadInfos[i].title +
                                    ".png";

                if (File.Exists(videoFilePath) || File.Exists(imageFilePath))
                    downloadedContentCount++;
            }

            return downloadedContentCount;
        }

        private async UniTask<bool> IsContentChangedOnServer()
        {
            VideoInfos infos = await _remoteRequestController.GetVideoToDownloadAsync();
            
            if (infos.VideoToDownloadInfos.Count != _currentVideoInfos.VideoToDownloadInfos.Count)
            {
                return true;
            }
            
            foreach (var info in infos.VideoToDownloadInfos)
            {
                if (_currentVideoInfos.VideoToDownloadInfos.FirstOrDefault(v => v.title == info.title) == null)
                {
                    return true;
                }
            }

            return false;
        }

        public async UniTask DownloadSpecificContentAsync(VideoInfo videoInfo, CancellationToken cancellationToken)
        {
            DownloadedSize = 0;
            CurrentDownloadContentID = videoInfo.id;
            IsDownloading = true;

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(DownloadMaxTimeInMinutes);

                var tempFilePattern = _deviceStorageManager.ContentLocation + videoInfo.title + "_part{0}.tmp";
                var tempFiles = new List<string>();

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (videoInfo.url.StartsWith("https://disk.yandex.ru"))
                        videoInfo.processed_url = await _urlConverter.ConvertYandexUrl(videoInfo.url, client, cancellationToken);

                    long totalBytes = await IdentifyFileSizeBeforeDownload(videoInfo.processed_url);

                    Debug.Log($"{VideoDownloaderDebugPrefix}<color=yellow> Размер файла для закачивания - {totalBytes} </color>");

                    var contentPath = videoInfo.format == "2D" || videoInfo.format == "3D"
                        ? _deviceStorageManager.ContentLocation + videoInfo.title + ".mp4"
                        : _deviceStorageManager.ContentLocation + videoInfo.title + ".png";

                    if (File.Exists(contentPath))
                    {
                        UnityEngine.Debug.Log($"{VideoDownloaderDebugPrefix}Файл уже скачан");
                        DownloadedSize = totalBytes;
                        await _remoteRequestController.SendExchangeDataAsync();
                        return;
                    }
                    
                    var chunkIndex = 0;
                    OnDownloadingStarted?.Invoke();
                    var startInfo = new DownloadInfoData()
                    {
                        CurrentContentName = videoInfo.title,
                        CurrentContentValue = _downloadedContentCount,
                        MaxContentValue = _videoInfos.VideoToDownloadInfos.Count,
                        DownloadedBytes = DownloadedSize,
                        TotalBytes = totalBytes
                    };
                    OnDownloadInfoChanged?.Invoke(startInfo);
                    
                    for (long offset = 0; offset < totalBytes; offset += ChunkSize)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var end = Math.Min(offset + ChunkSize - 1, totalBytes - 1);
                        var tempFilePath = string.Format(tempFilePattern, chunkIndex++);

                        if (File.Exists(tempFilePath))
                        {
                            var fileInfo = new FileInfo(tempFilePath);

                            if (fileInfo.Length == (end - offset + 1))
                            {
                                Debug.Log($"{VideoDownloaderDebugPrefix}Часть уже существует: {tempFilePath}");
                                tempFiles.Add(tempFilePath);
                                DownloadedSize += ChunkSize;
                                continue;
                            }
                        }
                        
                        if (await IsContentChangedOnServer())
                        {
                            Debug.Log($"{VideoDownloaderDebugPrefix}Контент изменился на сервере.");
                            _isNeedRestartDownloading = true;
                            IsDownloading = false;
                            return;
                        }
                        
                        if (await TryDownloadChunkAsync(client, videoInfo.processed_url, tempFilePath, offset, end))
                        {
                            tempFiles.Add(tempFilePath);
                            DownloadedSize += _lastDownloadedFileSize;
                            var info = new DownloadInfoData()
                            {
                                CurrentContentName = videoInfo.title,
                                CurrentContentValue = _downloadedContentCount,
                                MaxContentValue = _videoInfos.VideoToDownloadInfos.Count,
                                DownloadedBytes = DownloadedSize,
                                TotalBytes = totalBytes
                            };
                            OnDownloadInfoChanged?.Invoke(info);
                            Debug.Log($"{VideoDownloaderDebugPrefix}Часть {chunkIndex} успешно загружена: {tempFilePath}");
                        }
                        else
                        {
                            _isNeedRestartDownloading = true;
                            return;
                        }
                    }

                    if (DownloadedSize != totalBytes)
                    {
                        UnityEngine.Debug.Log($"{VideoDownloaderDebugPrefix}Вес файлов не совпадает, соединение tmp файлов невозможно");
                        UnityEngine.Debug.Log($"{VideoDownloaderDebugPrefix}Скачано (DownloadSize) - {DownloadedSize}");
                        UnityEngine.Debug.Log($"{VideoDownloaderDebugPrefix}Общий вес контента (totalBytes) - {totalBytes}");
                        _isNeedRestartDownloading = true;
                        _deviceStorageManager.DeleteTmpFiles();
                        return;
                    }

#if !UNITY_EDITOR
                    if (videoInfo.format == "2D" || videoInfo.format == "3D")
                    {
                        Debug.Log(tempFiles[0] + " firstFile stream");
                        await _videoEncrypter.EncryptFile(tempFiles[0]);
                    }
#endif

                    MergeDownloadedChunksAsync(tempFiles, contentPath);
                    DownloadedSize = totalBytes;
                    await _remoteRequestController.SendExchangeDataAsync();
                }
                catch (OperationCanceledException)
                {
                    Debug.Log($"{VideoDownloaderDebugPrefix}Загрузка была отменена.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{VideoDownloaderDebugPrefix}Ошибка при скачивании файла: {ex.Message}");
                }
                finally
                {
                    IsDownloading = false;
                    CurrentDownloadContentID = "";
                }
            }
        }

        private async UniTask<bool> TryDownloadChunkAsync(HttpClient client, string url, string tempFilePath, long start,
            long end)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.LogWarning($"{VideoDownloaderDebugPrefix}Ошибка загрузки: {response.StatusCode}");
                            return false;
                        }

                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        {
                            using (var tempFile = new FileStream(tempFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, 8192, true))
                            {
                                await responseStream.CopyToAsync(tempFile);
                                _lastDownloadedFileSize = tempFile.Length;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{VideoDownloaderDebugPrefix}Ошибка при загрузке чанка: {ex.Message}");
                _isNeedRestartDownloading = true;
                return false;
            }
        }


        private async void MergeDownloadedChunksAsync(List<string> tempFiles, string destinationPath)
        {
            const int bufferSize = 81920;

            using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write,
                       FileShare.None, bufferSize, true))
            {
                for (int i = 0; i < tempFiles.Count; i++)
                {
                    using (var sourceStream = new FileStream(tempFiles[i], FileMode.Open, FileAccess.Read,
                               FileShare.Read, bufferSize, true))
                    {
                        await sourceStream.CopyToAsync(destinationStream, bufferSize);
                    }

                    if (i % 10 == 0)
                        await UniTask.Yield(PlayerLoopTiming.PreLateUpdate); // Разрыв на 10 файлов
                }

                Debug.Log($"{VideoDownloaderDebugPrefix}Файл успешно загружен и сохранен в {destinationPath}");

                foreach (var tempFile in tempFiles)
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
        }

        private async UniTask<long> IdentifyFileSizeBeforeDownload(string processedUrl)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(processedUrl))
            {
                request.SetRequestHeader("Range", "bytes=0-1023");
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string contentRange = request.GetResponseHeader("Content-Range");
                    if (!string.IsNullOrEmpty(contentRange))
                    {
                        string[] parts = contentRange.Split('/');
                        if (parts.Length == 2 && long.TryParse(parts[1], out long fileSize))
                        {
                            Debug.Log($"{VideoDownloaderDebugPrefix}Размер файла: {fileSize} байт ({fileSize / (1024f * 1024f):F2} MB)");
                            return fileSize;
                        }
                    }
                }

                Debug.LogError($"{VideoDownloaderDebugPrefix}Не удалось определить размер файла. Ошибка: {request.error}");
                return 0;
            }
        }
    }
}