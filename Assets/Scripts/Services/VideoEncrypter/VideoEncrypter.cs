using System;
using System.IO;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Services
{
    [UsedImplicitly]
    public class VideoEncrypter
    {
        private DeviceStorageManager _deviceStorageManager;
        public VideoEncrypter(DeviceStorageManager deviceStorageManager)
        {
            _deviceStorageManager = deviceStorageManager;
        }
        
        public async UniTask EncryptFile(string fileToEncryptPath)
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    if (!File.Exists(fileToEncryptPath))
                    {
                        Debug.Log($"Файл {fileToEncryptPath} не найден.");
                        return;
                    }

                    var tempFilePath = fileToEncryptPath + ".tmp";
            
                    using (var inputStream = new FileStream(fileToEncryptPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var outputStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        outputStream.WriteByte(0x01);
                        await inputStream.CopyToAsync(outputStream);
                    }
            
                    File.Delete(fileToEncryptPath);
                    File.Move(tempFilePath, fileToEncryptPath);
            
                    Debug.Log($"Файл успешно обновлен: {fileToEncryptPath}");
                }
                catch (Exception ex)
                {
                    Debug.Log($"Произошла ошибка: {ex.Message}");
                }
            });
        }
    }
}