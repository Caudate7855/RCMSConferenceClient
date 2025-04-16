using UnityEditor;
using UnityEngine;

public static class EditorTooling
{
    [MenuItem("EditorTooling/ClearPrefs")]
    public static void ClearPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
