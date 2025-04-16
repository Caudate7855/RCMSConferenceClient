using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts.UI.UIGradient
{
    [AddComponentMenu("UI/Effects/Gradient3")]
    public class UIGradient3 : BaseMeshEffect
    {
        public Color m_color1 = Color.white;
        public Color m_color2 = Color.blue;
        public Color m_color3 = Color.red;
    
        [Range(-180f, 180f)]
        public float m_angle = 0f;
        [Range(-1f, 1f)]
        public float m_offsetY = 0f;
        public bool m_ignoreRatio = true;

        public override void ModifyMesh(VertexHelper vh)
        {
            if(enabled)
            {
                Rect rect = graphic.rectTransform.rect;
                Vector2 dir = UIGradientUtils.RotationDir(m_angle);

                if (!m_ignoreRatio)
                    dir = UIGradientUtils.CompensateAspectRatio(rect, dir);

                UIGradientUtils.Matrix2x3 localPositionMatrix = UIGradientUtils.LocalPositionMatrix(rect, dir);

                UIVertex vertex = default(UIVertex);
                for (int i = 0; i < vh.currentVertCount; i++) {
                    vh.PopulateUIVertex (ref vertex, i);
                    Vector2 localPosition = localPositionMatrix * vertex.position;
                    vertex.color *= Lerp3(m_color3, m_color2, m_color1, localPosition.y + m_offsetY);
                    vh.SetUIVertex (vertex, i);
                }
            }
        }
    
        public Color Lerp3(Color a, Color b, Color c, float t)
        {
            if (t < 0.5f)
                return Color.Lerp(a, b, t / 0.5f);
            else
                return Color.Lerp(b, c, (t - 0.5f) / 0.5f);
        }
    }
}