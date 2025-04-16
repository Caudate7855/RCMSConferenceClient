using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace VIAVR.Scripts
{
    public class SpherePictureChanger : MonoBehaviour
    {
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int MainTex2 = Shader.PropertyToID("_MainTex2");
        private static readonly int Blend = Shader.PropertyToID("_Blend");
    
        [SerializeField] private Texture[] _pictures;

        [SerializeField] private MeshRenderer _meshRenderer;

        [SerializeField] private float _changeDurationSeconds = 1f;

        private int? _currentPictureIndex;

        private bool _changeable = true;
    
        private int PreviousIndex
        {
            get
            {
                if (!_currentPictureIndex.HasValue) return 0;
            
                if (_pictures == null || _pictures.Length == 0) return 0;
            
                int index = _currentPictureIndex.Value - 1;

                if (index < 0) return _pictures.Length - 1;
            
                return index;
            }
        }
    
        private int NextIndex
        {
            get
            {
                if (!_currentPictureIndex.HasValue) return 0;
            
                if (_pictures == null || _pictures.Length == 0) return 0;
            
                int index = _currentPictureIndex.Value + 1;

                if (index >= _pictures.Length) return 0;
            
                return index;
            }
        }
    
        public async UniTask ChangeToNextPicture()
        {
            if(!_changeable) return;
        
            if (_pictures == null || _pictures.Length == 0)
            {
                Debug.LogError("_pictures array is null or empty!");
                return;
            }

            _changeable = false;
        
            if (!_currentPictureIndex.HasValue)
            {
                var texture = _meshRenderer.material.GetTexture(MainTex);

                if (texture == null)
                    _currentPictureIndex = -1;
                else
                    _currentPictureIndex = Array.IndexOf(_pictures, texture);
            }

            _currentPictureIndex = NextIndex;
        
            _meshRenderer.material.SetTexture(MainTex, _pictures[PreviousIndex]);
            _meshRenderer.material.SetTexture(MainTex2, _pictures[_currentPictureIndex.Value]);
        
            _meshRenderer.material.SetFloat(Blend, 0);

            await _meshRenderer.material.DOFloat(1, Blend, _changeDurationSeconds).AsyncWaitForCompletion();

            _changeable = true;
        }

        public async UniTask ChangeToPicture(Texture2D newTexture)
        {
            if(!_changeable) return;
        
            _meshRenderer.material.SetTexture(MainTex, _pictures[PreviousIndex]);
            _meshRenderer.material.SetTexture(MainTex2, _pictures[_currentPictureIndex.Value]);
        
            _meshRenderer.material.SetFloat(Blend, 0);

            await _meshRenderer.material.DOFloat(1, Blend, _changeDurationSeconds).AsyncWaitForCompletion();
        
            _changeable = true;
        }
    }
}