using System;
using System.IO;
using NaughtyAttributes;
using UnityEngine;

namespace VIAVR.Scripts
{
    public class ScreenshotCapture : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private int _upscale = 4;
        [SerializeField] private bool _alphaBackground = false;

        private void Awake()
        {
            if (!Application.isEditor)
                Destroy(this);
        }

        Texture2D Screenshot()
        {
            int w = _camera.pixelWidth * _upscale;
            int h = _camera.pixelHeight * _upscale;
        
            var rt = new RenderTexture(w, h, 32);
            _camera.targetTexture = rt;
        
            var screenShot = new Texture2D(w, h, TextureFormat.ARGB32, false);
        
            var clearFlags = _camera.clearFlags;
        
            if (_alphaBackground)
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(0, 0, 0, 0);
            }

            _camera.Render();
        
            RenderTexture.active = rt;
        
            screenShot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            screenShot.Apply();
        
            _camera.targetTexture = null;
            RenderTexture.active = null;
        
            DestroyImmediate(rt);
        
            _camera.clearFlags = clearFlags;
        
            return screenShot;
        }

        [Button("Make Screenshot")]
        public void SaveScreenshot()
        {
            var filename = "SS-" + DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss") + ".png";
        
            File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename), Screenshot().EncodeToPNG());
        
            Debug.Log($"Screenshot saved '{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename)}'");
        }
    }
}