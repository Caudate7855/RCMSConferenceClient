using CustomDebug;

namespace VIAVR.Scripts.Core
{
    public static class JsonHelper
    {
        public static bool TryParseJson<T>(string json, out T result, Newtonsoft.Json.TypeNameHandling? typeNameHandling = null)
        {
            result = default(T);

            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                if (typeNameHandling.HasValue)
                     result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json,
                        new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = typeNameHandling.Value });
                else
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"JsonConvert exception: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Распарсить json текст в объект result. Если была ошибка, то записать в лог ошибку url.
        /// </summary>
        /// <typeparam name="T">Тип объекта result.</typeparam>
        /// <param name="url">Url запроса, откуда был получен json.</param>
        /// <param name="json">Json текст.</param>
        /// <param name="result">Результат.</param>
        /// <param name="typeNameHandling">Применять TypeNameHandling.</param>
        /// <returns>Возвращает true, если успешно распарсилось. Иначе false и в лог запишется url.</returns>
        public static bool TryParseJson<T>(string url, string json, out T result, Newtonsoft.Json.TypeNameHandling? typeNameHandling = null)
        {
            if (TryParseJson<T>(json, out result, typeNameHandling))
                return true;

            UnityEngine.Debug.LogError($"Error parsing content from url : {url}");

            return false;
        }
    }
}