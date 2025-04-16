using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Services;
using UnityEditor;
using UnityEngine;
using VIAVR.Scripts.Core;
using Object = UnityEngine.Object;

namespace EditorHelpers
{
    public class BuilderPipelineMenu
    {
#if PICO_G2
        private const string APKPrefix = "G2_Remote_CMS_";
#elif PICO_G3
        private const string APKPrefix = "G3_Remote_CMS_";
#endif
        
        [MenuItem(@"VIAVR/Build signed + Install", false, -100000)]
        public static void BuildAndSignWithVersion()
        {
            string region = "";
#if REGION_ESP
            region = "ESP_";
#elif REGION_FR
            region = "FR_";
#endif

#if PICO_G2
            if (!EditorUtility.DisplayDialog("Билд для модели PICO G2", "Для G2 билдить только из ветки 'main_g2' или производных от неё!", "Ветка проекта - main_g2", "Отмена"))
                return;
#elif PICO_G3
            if (!EditorUtility.DisplayDialog("Билд для модели PICO G3", "Для G3 билдить только из ветки 'main' или производных от неё!", "Ветка проекта - main", "Отмена"))
                return;
#endif
            string isNoControllerMode =
                Object.FindObjectOfType<ControllersHandler>().NoControllerMode ? "no_controller_" : "";
            
            string isProdBuild = Resources.Load<RemoteRequestController>("RemoteController").IsProductionBuild
                ? "prod_"
                : "dev_";
            
            if (!EditorUtility.DisplayDialog(
                    title: "Сборка приложения",
                    message: $"Сборка \"{region}{APKPrefix}{isProdBuild}{isNoControllerMode}{PlayerSettings.bundleVersion}\"",
                    ok: "OK",
                    cancel: "Отмена"))
                return;

            string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "");

            if (!Directory.Exists(path))
                return;

            var saveApkPath = $"{path}/{APKPrefix}{isProdBuild}{isNoControllerMode}{PlayerSettings.bundleVersion}_unsigned.apk";
            var isBuildCompletedSuccessful = false;
            
            try
            {

                var buildReport = BuildPipeline.BuildPlayer(
                    levels: EditorBuildSettings.scenes.Where(s => s.enabled).ToArray(),
                    locationPathName: saveApkPath,
                    target: BuildTarget.Android,
                    options: BuildOptions.None);

                isBuildCompletedSuccessful = buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Build not Success with error : \n{e}");
            }

            if (!isBuildCompletedSuccessful)
            {
                UnityEngine.Debug.LogError("Build not Success. See Unity Console.");
                return;
            }

            try
            {
                var saveSignedWithTargetBundleVersionAPK = $"{path}/{region}{APKPrefix}{isProdBuild}{isNoControllerMode}{PlayerSettings.bundleVersion}.apk";

                var processStartInfo = new ProcessStartInfo(
                    fileName: "java",
                    arguments: string.Join(" ", new string[]
                    {
                    "-jar",
                    $"{Application.dataPath}/Editor/Build/signapk.jar",
                    $"{Application.dataPath}/Editor/Build/platform.x509.pem", // Для G3 эти ключи не являются системными
                    $"{Application.dataPath}/Editor/Build/platform.pk8",
                    saveApkPath,
                    saveSignedWithTargetBundleVersionAPK,
                    }));
                // Чтобы юзать java из PATH.
                processStartInfo.UseShellExecute = false;

                if (Process.Start(processStartInfo).WaitForExit(5 * 60 * 1000))
                    UnityEngine.Debug.Log($"Success built and signed");
                else
                    throw new Exception($"Too long build time.");
                
                EditorUtility.RevealInFinder(saveSignedWithTargetBundleVersionAPK);
                
               if(File.Exists(saveApkPath)) File.Delete(saveApkPath);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Success built, but Not Succes signed.\n{e}");
            }
        }

#region ADB_commands

        [MenuItem("VIAVR/Команды adb/Удалить {applicationIdentifier}", false, 100)]
        public static void UninstallApk()
        {
            string package = PlayerSettings.applicationIdentifier;

            if (!EditorUtility.DisplayDialog("APK", $"Удалить {package} с девайса?", "Удалить", "Отмена"))
                return;
            
            bool uninstallResult = AdbCommand($"uninstall {package}");

            if (!uninstallResult)
                return;
            
            if (EditorUtility.DisplayDialog("APK",$"'{package}' удалён. Ребутнуть девайс?", "Ребутнуть","Отмена"))
            {
                AdbCommand("shell setprop ctl.restart zygote", 2);
            }
        }
        
        [MenuItem("VIAVR/Команды adb/Запустить {applicationIdentifier}", false, 100)]
        public static void LaunchApp()
        {
            string package = PlayerSettings.applicationIdentifier;

            if (!EditorUtility.DisplayDialog("APK", $"Запустить {package} если установлено?", "Запустить", "Отмена"))
                return;
            
            AdbCommand($"shell am start -n {package}/com.unity3d.player.UnityPlayerNativeActivityPico", 2);
        }

        [MenuItem("VIAVR/Команды adb/Ребут девайса (холодный)", false, 120)]
        public static void RebootCold()
        {
            if (!EditorUtility.DisplayDialog("APK", $"Перезагрузить девайс? После перезапуска перетыкните USB шнур, иначе adb не обнаружит девайс", "Ребут", "Отмена"))
                return;

            AdbCommand("reboot", 2);
        }
        
        [MenuItem("VIAVR/Команды adb/Ребут девайса (горячий)", false, 120)]
        public static void RebootHot()
        {
            if (!EditorUtility.DisplayDialog("APK", $"Перезагрузить девайс? После перезапуска можно не перетыкивать USB шнур", "Ребут", "Отмена"))
                return;

            AdbCommand("shell setprop ctl.restart zygote", 2);
        }

        [MenuItem("VIAVR/Команды adb/Открыть настройки Pico", false, 140)]
        public static void OpenPicoSettings()
        {
            if (!EditorUtility.DisplayDialog("APK", $"Открыть настройки PICO? Для системных Developer Options нужно выбрать 'Открыть [Режим Разработчика]'", "Открыть", "Отмена"))
                return;

            AdbCommand("shell am start -n com.picovr.settings/com.picovr.vrsettingslib.UnityActivity", 2);
        }
        
        [MenuItem("VIAVR/Команды adb/Открыть [Режим Разработчика]", false, 140)]
        public static void OpenDevSettings()
        {
            if (!EditorUtility.DisplayDialog("APK", $"Открыть [Режим Разработчика] (Developer Options)?", "Открыть", "Отмена"))
                return;

            AdbCommand(@"shell am start -S com.android.settings/.Settings\$DevelopmentSettingsActivity", 2);
        }
        
        [MenuItem("VIAVR/Команды adb/Остановить watchdog", false, 160)]
        public static void StopWatchdog()
        {
            AdbCommand(@"shell am force-stop com.viavr.watchdog", 2);
        }
        
        [MenuItem("VIAVR/Команды adb/(Пере)запустить watchdog", false, 160)]
        public static void RestartWatchdog()
        {
            StopWatchdog();
            
            AdbCommand(@"shell am start-foreground-service com.viavr.watchdog/.WatchdogService", 2);
        }
        
#endregion

        [MenuItem("VIAVR/Unity/Очистить токен и сессию", false, 160)]
        public static void ClearTokenSessionPrefs()
        {
            PlayerPrefs.DeleteKey("tokenV2");
            PlayerPrefs.DeleteKey("sessionV2");
            PlayerPrefs.Save();

            EditorUtility.DisplayDialog("VIAVR/Unity", "Очистить токен и сессию: команда выполнена", "OK");
        }

        static bool AdbCommand(string adbArgumentsString, int timeoutSeconds = 60)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo("adb", adbArgumentsString);
            
            if (Process.Start(processStartInfo).WaitForExit(timeoutSeconds * 1000))
            {
                UnityEngine.Debug.Log($"'adb {adbArgumentsString}' command success");
                return true;
            }
            else
            {
                UnityEngine.Debug.Log($"'adb {adbArgumentsString}' command failed!");
                return false;
            }
        }
    }
}