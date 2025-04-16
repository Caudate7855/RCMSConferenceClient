using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Services;
using UnityEngine;
using VIAVR.Scripts;
using Zenject;

[UsedImplicitly]
public class DeviceStorageManager
{
    private long FreeSpaceOffset = 5368709120; // 5 GB
    private const string GalleryFolderName = "RCMS Gallery";
    
    [Inject] private AndroidDeviceHelper _androidDeviceHelper;

    public string ContentLocation => Application.isEditor ? @$"{GlobalPersonalConsts.CONTENT_FOLDER_EDITOR}{GalleryFolderName}\" : GetDownloadPath();

    public long GetFreeSpaceInBytes()
    {
        if (Application.isEditor)
            return long.MaxValue;
        
        if (IsSDCardExist())
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject[] externalDirs = activity.Call<AndroidJavaObject[]>("getExternalFilesDirs", (object)null);

                    if (externalDirs.Length > 1 && externalDirs[1] != null)
                    {
                        string sdCardPath = externalDirs[1].Call<string>("getAbsolutePath");
                        using (AndroidJavaObject statFs = new AndroidJavaObject("android.os.StatFs", sdCardPath))
                        {
                            long blockSize = statFs.Call<long>("getBlockSizeLong");
                            long availableBlocks = statFs.Call<long>("getAvailableBlocksLong");
                            long freeSpace = blockSize * availableBlocks;
                            Debug.Log($"Free space on SD card: {freeSpace} bytes");
                            return freeSpace - FreeSpaceOffset;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while retrieving SD card free space: {e.Message}");
            }
        }

        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using (AndroidJavaObject file = activity.Call<AndroidJavaObject>("getFilesDir"))
                {
                    long freeSpace = file.Call<long>("getFreeSpace");
                    Debug.Log($"Free space: {freeSpace} bytes");
                    return freeSpace - FreeSpaceOffset;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while retrieving free space: {e.Message}");
            return default;
        }
    }

    public async UniTask CleanDownloadFolderAsync()
    {
        try
        {
            string[] files = Directory.GetFiles(ContentLocation);

            List<UniTask> deleteTasks = new List<UniTask>();

            foreach (string file in files)
            {
                deleteTasks.Add(UniTask.Run(() => File.Delete(file)));
                Debug.Log($"Удалён файл: {file}");
            }

            await UniTask.WhenAll(deleteTasks);

            Debug.Log($"Содержимое папки {ContentLocation} успешно удалено.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при очистке папки {ContentLocation}: {ex.Message}");
        }
    }

    public async UniTask CleanFolderExceptAsync(List<VideoInfo> excludedFiles)
    {
        try
        {
            string[] files = Directory.GetFiles(ContentLocation);

            List<UniTask> deleteTasks = new List<UniTask>();

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                bool isExcluded = excludedFiles.Any(video => fileName.StartsWith(video.title));

                if (!isExcluded)
                {
                    deleteTasks.Add(UniTask.Run(() => File.Delete(file)));
                    Debug.Log($"Удалён файл: {file}");
                }
                else
                {
                    //Debug.Log($"Файл не тронут: {file}");
                }
            }

            await UniTask.WhenAll(deleteTasks);

            Debug.Log($"Содержимое папки {ContentLocation} очищено, кроме указанных файлов.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при очистке папки {ContentLocation}: {ex.Message}");
        }
    }

    private bool IsSDCardExist()
    {
        if (string.IsNullOrEmpty(_androidDeviceHelper.GetSDCardPath()))
            return false;

        if (Directory.Exists($"{_androidDeviceHelper.GetSDCardPath().TrimEnd('/', '\\', ' ')}"))
            return true;

        return false;
    }

    private string GetDownloadPath()
    {
        string contentDownloadPath;
        
        if (Application.isEditor)
        {
            CreateDirectoryIfNotExists(@$"{GlobalPersonalConsts.CONTENT_FOLDER_EDITOR}{GalleryFolderName}\");
            return @$"{GlobalPersonalConsts.CONTENT_FOLDER_EDITOR}{GalleryFolderName}\";
        }
        
        if (IsSDCardExist())
        {
            contentDownloadPath =
                $"{_androidDeviceHelper.GetSDCardPath().TrimEnd('/', '\\', ' ')}/{GalleryFolderName}/";
        }
        else
        {
            using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
            {
                using (AndroidJavaObject downloadsDir =
                       environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", "Download"))
                {
                    contentDownloadPath = $"{downloadsDir.Call<string>("getAbsolutePath")}/{GalleryFolderName}/";
                }
            }
        }
        
        CreateDirectoryIfNotExists(contentDownloadPath);

        return contentDownloadPath;
    }

    public void DeleteTmpFiles()
    {
        try
        {
            string[] files = Directory.GetFiles(ContentLocation);

            foreach (string file in files)
            {
                if (Path.GetExtension(file).Equals(".tmp", StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                    Debug.Log($"Удалён TMP файл: {file}");
                }
            }

            Debug.Log($"Содержимое папки {ContentLocation} очищено от .tmp файлов.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при очистке папки {ContentLocation}: {ex.Message}");
        }
    }

    private void CreateDirectoryIfNotExists(string path)
    {
        if (Directory.Exists(path)) 
            return;
        Directory.CreateDirectory(path);
        Debug.Log($"Directory created: {path}");
    }
}