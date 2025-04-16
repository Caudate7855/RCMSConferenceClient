using System.Threading.Tasks;
using UnityEngine;

#if ADDRESSABLES
public class UIAssetLoader
{
    public async Task<T> Load<T>(string path)
    {
        var handle = await UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(path).Task;
        var result = handle.GetComponent<T>();
            
        return result;
    }
}

#endif