using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using VIAVR.Scripts.UI.Paginator;

namespace VIAVR.Scripts.Data
{
    public static class ContentPath
    {
        public enum Storage
        {
            INTERNAL, SDCARD
        }
    
        public static string CONTENT_FOLDER_EDITOR = GlobalPersonalConsts.CONTENT_FOLDER_EDITOR;
        public static string INTERNAL_FOLDER_EDITOR = GlobalPersonalConsts.INTERNAL_FOLDER_EDITOR;

        private static string _cachedInternalPath;
        private static string _cachedSDPath;

        public static string GetPlatformContentPath(Storage storage = Storage.SDCARD)
        {
            //if (storage == Storage.SDCARD && !SDCardHandler.IsSDCardExists()) storage = Storage.INTERNAL;
        
#if UNITY_EDITOR
            return storage == Storage.SDCARD ? CONTENT_FOLDER_EDITOR : INTERNAL_FOLDER_EDITOR;
#else
        //_cachedSDPath ??= Singleton<AndroidDeviceHelper>.Instance.GetSDCardPath();
        //_cachedInternalPath ??= Application.persistentDataPath.Substring(0, Application.persistentDataPath.IndexOf("Android", StringComparison.Ordinal));

       // return storage == Storage.SDCARD ? _cachedSDPath : _cachedInternalPath;
            return "";
#endif
        }
    
        public static string GetFullPath(string relativePath, Storage storage = Storage.SDCARD)
        {
            return GetPlatformContentPath(storage).TrimEnd('/','\\', ' ') + "/" + relativePath.TrimStart('/','\\', ' ');
        }

        // заместо Path.Combine который пихает обратные слеши
        public static string Combine(params string[] relativePathes)
        {
            string fullPath = "";
        
            foreach (var path in relativePathes)
            {
                fullPath += "/" + path.TrimStart('/', '\\', ' ').TrimEnd('/', '\\', ' ');
            }

            return fullPath.TrimStart('/', '\\', ' ').TrimEnd('/','\\', ' ');;
        }
    }


    [Serializable]
    public class VrTourRegion
    {
        [JsonProperty("map_image")] public string MapImage;
        [JsonProperty("groups")] public List<VrTourGroup> VrTourGroups;
    }

    [Serializable]
    public class VrTourGroup
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("map_position")] public Vector2 MapPosition;
    
        [JsonProperty("apartments")] public List<VrTourApartment> VrTours;
    }

    public class VrTourApartment : IPaginableData
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("info_status")] public string InfoStatus;
        [JsonProperty("info_header")] public string InfoHeader;
        [JsonProperty("info_description")] public string InfoDescription;
        [JsonProperty("info_previews")] public string[] InfoPreviewImages;
    
        [JsonProperty("preview_image")] public string MainPreview;
    
        [JsonProperty("start_panorama")] public string StartPanorama;
        [JsonProperty("start_head_rotation")] public int? StartHeadRotation = null;
        [JsonProperty("panoramas")] public Dictionary<string, VrTourPanorama> Panoramas;
    }

    [Serializable]
    public class VrTourPanorama
    {
        [JsonProperty("texture_r")] public string TextureR;
        [JsonProperty("texture_l")] public string TextureL;
        [JsonProperty("image_mode")] public string ImageMode;
        [JsonProperty("buttons")] public List<SceneButton> Buttons;
    }

    [Serializable]
    public class SceneButton
    {
        [JsonProperty("text")] public string Text;
        [JsonProperty("position")] public Vector3 Position;
        [JsonProperty("head_rotation")] public int? HeadRotation = null;
        [JsonProperty("goto")] public string GotoPanorama;
    }
}