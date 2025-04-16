using UnityEngine;

public class DontDestroyMarker : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}