using System;
using System.Net.Http;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Services
{
    [UsedImplicitly]
    public class UrlConverter
    {
        private const string API_URL_PREFIX = "https://cloud-api.yandex.net/v1/disk/public/resources/download?public_key=";
        
        public async UniTask<string> ConvertYandexUrl(string url,HttpClient client, CancellationToken cancellationToken)
        {
            var apiUrl = $"{API_URL_PREFIX}{url}";
            var apiResponse = await client.GetAsync(apiUrl, cancellationToken);
            apiResponse.EnsureSuccessStatusCode();

            var jsonResponse = await apiResponse.Content.ReadAsStringAsync();
            var yandexResponse = JsonConvert.DeserializeObject<YandexResponse>(jsonResponse);
            
            return yandexResponse.href;
        } 
    }
    
    [Serializable]
    public class YandexResponse
    {
        public string href; 
    }
}