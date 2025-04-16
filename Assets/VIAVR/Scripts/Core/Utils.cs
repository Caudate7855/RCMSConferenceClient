using System;
using System.IO;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace VIAVR.Scripts.Core
{
    public static class Utils
    {
        public const string ReturnSymbol = "⏎ ";
#if UNITY_EDITOR
        public const string TagOpenBoldRed = "<color=red><b>";
        public const string TagCloseBoldColor = "</b></color>";
        private const string EditorLogOk = "<color=green>●</color> ";
        private const string EditorLogError = "<color=red>●</color> ";
        private const string EditorLogWarning = "<color=yellow>●</color> ";
        private const string EditorLogHighlight = "<color=magenta>●</color> ";
#else
    public const string TagOpenBoldRed = "";
    public const string TagCloseBoldColor = "";
    private const string EditorLogOk = "";
    private const string EditorLogError = "";
    private const string EditorLogWarning = "";
    private const string EditorLogHighlight = "";
#endif
    
        public static string OkPrefix => Application.isEditor ? EditorLogOk : "";
        public static string ErrorPrefix => Application.isEditor ? EditorLogError : "";
        public static string WarningPrefix => Application.isEditor ? EditorLogWarning : "";
        public static string HighlightPrefix => Application.isEditor ? EditorLogHighlight : "";
    
        public static async UniTask<string> ReadAllTextAsync(string fullPath)
        {
            using StreamReader reader = File.OpenText(fullPath);

            var text = await reader.ReadToEndAsync();

            return text;
        }

        public static async UniTask FadeCanvasGroupAsync(CanvasGroup canvasGroup, float to, float durationSeconds)
        {
            if (canvasGroup == null)
            {
                Debug.LogError($"Utils.{nameof(FadeCanvasGroupAsync)}: нужен компонент CanvasGroup");
                return;
            }
        
            if(Math.Abs(canvasGroup.alpha - to) < 0.01f) // не уверен в ==
                return;

            canvasGroup.interactable = false;
            await canvasGroup.DOFade(to, durationSeconds).AsyncWaitForCompletion();
            canvasGroup.interactable = true;
        }
    
        public static byte[] GetFileMD5(string path, bool betterStreamingAssets = false)
        {
            bool fileExists = betterStreamingAssets ? BetterStreamingAssets.FileExists(path) : File.Exists(path);

            if (!fileExists)
            {
                Debug.Log($"GetFileMD5 '{path}' not found");
                return null;
            }

            using var md5 = MD5.Create();
            using var stream = betterStreamingAssets ? BetterStreamingAssets.OpenRead(path) : File.OpenRead(path);
        
            return md5.ComputeHash(stream);
        }
    }
}