using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IUIAssetLoader
{
    public GameObject CashedObject { get; set; }
    public UniTask<T> Load<T>(string path);
    public void Unload();
}