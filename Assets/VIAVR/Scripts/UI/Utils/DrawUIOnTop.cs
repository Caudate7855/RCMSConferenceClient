using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts.UI.Utils
{
    public class DrawUIOnTop : MonoBehaviour
    {
        private static readonly int UnityGuiZTestMode = Shader.PropertyToID("unity_GUIZTestMode");
    
        [SerializeField] private UnityEngine.Rendering.CompareFunction _comparison = UnityEngine.Rendering.CompareFunction.Always;
    
        [SerializeField] public bool _apply = false;

        private Graphic[] _graphics;
        private TMP_Text[] _texts;
        
        private void Awake()
        {
            _graphics = GetComponentsInChildren<Graphic>();
            _texts = GetComponentsInChildren<TMP_Text>();
        }

        private void OnEnable()
        {
            if (_apply) Apply();
        }

        void Apply()
        {
            foreach (var graphic in _graphics)
            {
                Material existingGlobalMat = graphic?.materialForRendering;
                Material updatedMaterial = new Material(existingGlobalMat);
            
                updatedMaterial.SetInt(UnityGuiZTestMode, (int) _comparison);

                if (graphic != null) graphic.material = updatedMaterial;
            }
        
            foreach (var text in _texts)
            {
                text.isOverlay = true;
            }
        }
    }
}