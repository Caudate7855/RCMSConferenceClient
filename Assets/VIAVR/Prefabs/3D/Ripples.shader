Shader "Custom/Ripples"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Speed("Speed", Float) = 100
        _Dist("Dist", Float) = 3
        _Size1("Size1", Range(0, 2)) = 0
        _LerpFrom("_LerpFrom", Range(0, 1)) = 0
        _LerpTo("_LerpTo", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
        }
        LOD 200

        CGPROGRAM
        #pragma surface surf Unlit alpha:blend

        struct Input
        {
            float3 worldPos;
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        float _Speed;
        float _Dist;
        float _Size1;
        float _LerpFrom;
        float _LerpTo;

        half4 LightingUnlit (SurfaceOutput s, half3 lightDir, half atten) {
           half4 c;
           c.rgb = s.Albedo;
           c.a = s.Alpha;
           return c;
         }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float dist = 1 - 2 * distance(IN.uv_MainTex, fixed2(.5, .5));
            
            const float val = abs(sin(dist * _Dist - _Time * -_Speed));

            o.Albedo = IN.color;
            o.Alpha = lerp(_LerpFrom, _LerpTo, clamp(val * _Size1, 0, 1)) * dist * IN.color.a;
        }
        ENDCG
    }
}