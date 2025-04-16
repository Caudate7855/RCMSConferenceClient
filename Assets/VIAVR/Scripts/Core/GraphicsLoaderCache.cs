using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Networking;
using VIAVR.Scripts.Core.SerializableDictionary;
using Debug = CustomDebug.Debug;

namespace VIAVR.Scripts.Core
{
    public class GraphicsLoaderCache : MonoBehaviour
    {
        public enum LogLevel
        {
            NONE, ERRORS, ALL
        }
    
        public enum LoaderError
        {
            NOT_FOUND, FILE_FORMAT, LOADING
        }

        [SerializeField] private SerializableDictionary<LoaderError, Texture2D> _placeholderOnErrorImages;

        [SerializeField] private LogLevel logLevel = LogLevel.ERRORS; 
    
        [SerializeField] private int _cacheAwaitTimeout = 5000; //в миллисекундах
        [SerializeField] private bool _useCache = true;
    
        [HorizontalLine]
        [SerializeField] [ReadOnly] private int _cachedTexturesCount;
        [SerializeField] [ReadOnly] private int _cachedSpritesCount;
        [SerializeField] [ReadOnly] private int _cachingCount;
    
        private ConcurrentDictionary<string, Texture2D> _cachedTexturesDictonary =
            new ConcurrentDictionary<string, Texture2D>();
    
        private ConcurrentDictionary<string, Sprite> _cachedSpritesDictonary =
            new ConcurrentDictionary<string, Sprite>();

        // список путей к файлам, которые находятся в процессе загрузки и кэширования
        private HashSet<string> _cachingList = new HashSet<string>();

        private bool _allowWaitCache = true;

        public bool UseCache => _useCache;

        private void Awake()
        {
            // атрибут [ReadOnly] иногда не обнуляет значения в полях после стопа сцены
            _cachedTexturesCount = 0;
            _cachedSpritesCount = 0;
            _cachingCount = 0;
        }

        void Log(string methodName, string message, LogLevel level)
        {
            if (logLevel < level) return;

            if (level == LogLevel.ERRORS)
                Debug.LogError(methodName + ": " + message);
            else
                Debug.Log(methodName + ": " + message);
        }

        // закэшировать текстуру
        void CacheAdd(string path, Texture2D texture2D)
        {
            if (!_useCache) return;
        
            if (string.IsNullOrEmpty(path))
            {
                Log(nameof(CacheAdd),"path == null", LogLevel.ERRORS);
                return;
            }

            if (_cachedTexturesDictonary.TryAdd(path, texture2D))
            {
                _cachedTexturesCount++;
            
                Log(nameof(CacheAdd),$"'{path}' cached!", LogLevel.ALL);
            }
            else
            {
                Log(nameof(CacheAdd),$"'{path}' cached already!", LogLevel.ALL);
            }
        }

        // загрузить текстуру из кэша если она там есть
        Texture2D CacheTryGet(string path)
        {
            if (!_useCache) return null;
        
            if (string.IsNullOrEmpty(path))
            {
                Log(nameof(CacheTryGet),"path == null", LogLevel.ERRORS);
                return null;
            }
        
            _cachedTexturesDictonary.TryGetValue(path, out Texture2D fromCache);
        
            if(fromCache != null)
                Log(nameof(CacheTryGet),"Texture2D from cache:" + path, LogLevel.ALL);
        
            return fromCache;
        }

        // если текстура уже грузится в момент вызова её повторной загрузки, ждем загрузки и берем её из кэша
        private async UniTask<Texture2D> CacheAwait(string path)
        {
            if (!_useCache) return null;
        
            Texture2D cached = null;

            int checkDelay = 50;
        
            int checkTime = 0;
        
            while (_allowWaitCache)
            {
                await UniTask.Delay(checkDelay);

                if (!IsCaching(path))
                {
                    cached = CacheTryGet(path);
                    break;
                }

                checkTime += checkDelay;

                if (checkTime > _cacheAwaitTimeout)
                {
                    Log("CacheAwait",$"timeout while waiting '{path}'", LogLevel.ALL);
                    break;
                }
            }

            return cached;
        }

        // грузится ли текстура?
        bool IsCaching(string path)
        {
            if (!_useCache) return false;
        
            if (string.IsNullOrWhiteSpace(path)) return false;
        
            lock (_cachingList)
            {
                return _cachingList.Contains(path);
            }
        }

        // добавить путь текстуры в список загружаемых в данный момент
        void AddToCaching(string path)
        {
            if (!_useCache) return;
        
            lock (_cachingList)
            {
                if (!_cachingList.Contains(path))
                {
                    _cachingList.Add(path);
                    _cachingCount++;
                }
            }
        }

        // убрать путь текстуры из списка загружаемых в данный момент
        void RemoveFromCaching(string path)
        {
            lock (_cachingList)
            {
                if (_cachingList.Contains(path))
                {
                    _cachingList.Remove(path);
                    _cachingCount--;
                }
            }
        }

        Sprite CreateSprite(Texture2D texture2D, string fullPath = "")
        {
            Sprite sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), Vector2.one * 0.5f);
            sprite.name = fullPath;

            if (string.IsNullOrEmpty(fullPath) && !string.IsNullOrEmpty(texture2D.name))
                fullPath = texture2D.name;

            if (_cachedSpritesDictonary.TryAdd(fullPath, sprite))
            {
                _cachedSpritesCount++;
            }
            else
            {
                Log(nameof(CreateSprite),$"Sprite '{fullPath}' cached already!", LogLevel.ALL);
            }

            return sprite;
        }
    
        // загрузка текстуры из файловой системы по абсолютному пути с возвращением спрайта из этой текстуры + упрощенное кеширование
        public async UniTask<Sprite> LoadSpriteAsync(string fullPath)
        {
            _cachedSpritesDictonary.TryGetValue(fullPath, out Sprite fromCache);

            if (fromCache)
            {
                Log(nameof(LoadSpriteAsync),"Sprite from cache:" + fromCache.name, LogLevel.ALL);
                return fromCache;
            }
        
            Texture2D texture2D = await LoadTexture2DAsync(fullPath);

            if (texture2D == null)
            {
                Log(nameof(LoadSpriteAsync),$"texture '{fullPath}' is null", LogLevel.ERRORS);
                return null;
            }

            Sprite sprite = CreateSprite(texture2D, fullPath);

            return sprite;
        }

        // проверка есть ли текстура в кэше, если нет то создается новый спрайт
        public async UniTask<Sprite> LoadSpriteAsync(Texture2D texture2D)
        {
            if (texture2D == null)
            {
                Log(nameof(LoadSpriteAsync),$"texture is null", LogLevel.ERRORS);
                return null;
            }
        
            // у всех текстур загруженных через GraphicsLoaderCache name это полный путь до файла текстуры
            Sprite fromCache = string.IsNullOrEmpty(texture2D.name) ? null : await LoadSpriteAsync(texture2D.name);

            if (fromCache == null)
            {
                foreach (var cachedTexture in _cachedTexturesDictonary)
                {
                    if (cachedTexture.Value == texture2D)
                    {
                        fromCache = await LoadSpriteAsync(cachedTexture.Key);
                        break;
                    }
                }
            }

            if (fromCache == null)
            {
                fromCache = CreateSprite(texture2D);

                string randomName = RandomString(10);
            
                CacheAdd(randomName, texture2D);
            
                Log(nameof(LoadSpriteAsync),$"new Sprite with autogenerated  name {randomName} added", LogLevel.ALL);
            }

            return fromCache;
        }

        public async UniTask DownloadTexture2DAsync(string url, bool returnNullIfError, Action<Texture2D> callback)
        {
            if (IsCaching(url))
                await UniTask.WaitWhile(() => IsCaching(url));

            callback?.Invoke(await DownloadTexture2DAsync(url, returnNullIfError));
        }

        public async UniTask<Texture2D> DownloadTexture2DAsync(string url, bool returnNullIfError = false)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                Log(nameof(DownloadTexture2DAsync),$"URL '{url}' is not valid", LogLevel.ERRORS);
                return returnNullIfError ? null : _placeholderOnErrorImages[LoaderError.NOT_FOUND];
            }
        
            if (IsCaching(url))
                await UniTask.WaitWhile(() => IsCaching(url));
        
            Texture2D cached = CacheTryGet(url);

            if (cached != null)
                return cached;
        
            AddToCaching(url);
        
            var request = UnityWebRequestTexture.GetTexture(url);
            await request.SendWebRequest();
        
            RemoveFromCaching(url);

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

                if (texture == null)
                    return returnNullIfError ? null : _placeholderOnErrorImages[LoaderError.NOT_FOUND];

                CacheAdd(url, texture);
            
                return texture;
            }
        
            Log(nameof(DownloadTexture2DAsync),$"Cannot load from URL '{url}: {request.error}'", LogLevel.ERRORS);
        
            return returnNullIfError ? null : _placeholderOnErrorImages[LoaderError.NOT_FOUND];
        }
    
        // загрузка текстуры из файловой системы по абсолютному пути
        public async UniTask<Texture2D> LoadTexture2DAsync(string fullPath, TextureFormat format = TextureFormat.RGBA32, int maxSide = int.MaxValue, bool mipMaps = true, bool returnNullIfNotFound = false)
        {
            //Debug.Log($"LoadTexture2DAsync '{fullPath}'");

            if (!IsValidImagePath(fullPath, out string whyNotValid))
            {
                if(!returnNullIfNotFound)
                    Log(nameof(LoadTexture2DAsync),whyNotValid, LogLevel.ERRORS);
            
                return returnNullIfNotFound ? null : _placeholderOnErrorImages[LoaderError.NOT_FOUND];
            }
        
            Texture2D cached = IsCaching(fullPath) ? await CacheAwait(fullPath) : CacheTryGet(fullPath);

            if (cached != null)
            {
                return cached;
            }
        
            bool loadFromStreamingAssets = fullPath.StartsWith("sa://");
            string streamingAssetsPath = "";

            if (loadFromStreamingAssets)
            {
                streamingAssetsPath = fullPath.Replace("sa://", "");

                if (!BetterStreamingAssets.FileExists(streamingAssetsPath))
                {
                    Log(nameof(LoadTexture2DAsync),$"file '{fullPath}' not found in StreamingAssets!", returnNullIfNotFound ? LogLevel.ALL : LogLevel.ERRORS);
                    return returnNullIfNotFound ? null : _placeholderOnErrorImages[LoaderError.NOT_FOUND];
                }
            }
            else if (!File.Exists(fullPath))
            {
                Log(nameof(LoadTexture2DAsync),$"file '{fullPath}' not found!", returnNullIfNotFound ? LogLevel.ALL : LogLevel.ERRORS);
                return returnNullIfNotFound ? null : _placeholderOnErrorImages[LoaderError.NOT_FOUND];
            }

            AddToCaching(fullPath);

            byte[] data;

            if (loadFromStreamingAssets)
            {
                using (Stream stream = BetterStreamingAssets.OpenRead(streamingAssetsPath))
                {
                    data = new byte[stream.Length];
                    await stream.ReadAsync(data, 0, (int) stream.Length);
                }
            }
            else
            {
                using (FileStream stream = File.Open(fullPath, FileMode.Open))
                {
                    data = new byte[stream.Length];
                    await stream.ReadAsync(data, 0, (int) stream.Length);
                }
            }

            MemoryStream memoryStream = new MemoryStream(data);
            BinaryReader binaryReader = new BinaryReader(memoryStream);

            Size size;

            try
            {
                size = ImageHelper.GetDimensions(binaryReader);
            }
            catch (ArgumentException e)
            {
                RemoveFromCaching(fullPath);
            
                return _placeholderOnErrorImages[LoaderError.FILE_FORMAT];
            }
            finally
            {
                binaryReader.Dispose();
                memoryStream.Dispose();
            }

            Texture2D texture = new Texture2D(size.width, size.height, format, mipMaps)
            {
                filterMode = FilterMode.Bilinear, name = fullPath, wrapMode = TextureWrapMode.Clamp
            };

            texture.LoadImage(data);
        
            if (maxSide > 0 && texture.height > 0 && (texture.width > maxSide || texture.height > maxSide))
            {
                float aspect = texture.width / (float)texture.height;

                int w = texture.width > texture.height ? maxSide : Mathf.RoundToInt(maxSide / aspect);
                int h = texture.height > texture.width ? maxSide : Mathf.RoundToInt(maxSide / aspect);
            
                //Debug.Log($"Scaled from {texture.width}x{texture.height} to: w:{w}, h:{h}, a:{aspect}");
            
                var scaled = new Texture2D(w, h, format, mipMaps);
                
                Graphics.ConvertTexture(texture, scaled);
                
                Destroy(texture);

                texture = scaled;
            }

            CacheAdd(fullPath, texture);

            RemoveFromCaching(fullPath);

            return texture;
        }

        public bool ClearFromCache(Texture2D texture2D)
        {
            if (texture2D == null)
            {
                Log(nameof(ClearFromCache),"texture == null", LogLevel.ALL);
                return false;
            }
        
            if (!_cachedTexturesDictonary.Values.Contains(texture2D))
            {
                Log(nameof(ClearFromCache),texture2D.name + " not found in cache!", LogLevel.ALL);
                return false;
            }
        
            var textureKey = _cachedTexturesDictonary.FirstOrDefault(tex => tex.Value == texture2D).Key;

            bool removed = _cachedTexturesDictonary.TryRemove(textureKey, out var texture);
        
            _cachedTexturesCount--;
            
            Log(nameof(ClearFromCache),texture.name + " DESTROYED", LogLevel.ALL);
            
            Destroy(texture);

            return removed;
        }

        public Texture2D GetLoadingPlaceholderTexture()
        {
            return _placeholderOnErrorImages[LoaderError.LOADING];
        }

        private Sprite _loadingPlaceholderSprite;
    
        public Sprite GetLoadingPlaceholderSprite()
        {
            if (_loadingPlaceholderSprite == null)
                _loadingPlaceholderSprite = CreateSprite(_placeholderOnErrorImages[LoaderError.LOADING]);
        
            return _loadingPlaceholderSprite;
        }

        public static bool IsValidImagePath(string fullPath, out string whyNotValid)
        {
            whyNotValid = "";
        
            if (string.IsNullOrEmpty(fullPath))
            {
                whyNotValid = "fullPath == null";
                return false;
            }

            if (string.IsNullOrEmpty(Path.GetExtension(fullPath)))
            {
                whyNotValid = $"Illegal image fullPath == '{fullPath}'";
                return false;
            }

            if (fullPath.StartsWith("http"))
            {
                whyNotValid = $"fullPath is URL == '{fullPath}'";
                return false;
            }

            return true;
        }
    
        public static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        
            if (byteCount == 0)
                return "0" + suf[0];
        
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        
            return (Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture) + suf[place];
        }
    
        private static readonly System.Random random = new System.Random();
    
        static string RandomString(int size)
        {
            const string pool = "abcdefghijklmnopqrstuvwyxzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
            char[] chars = new char[size];
            for (int i = 0; i < size; i++)
            {
                chars[i] = pool[random.Next(pool.Length)];
            }
            return new string(chars);
        }
    }
}