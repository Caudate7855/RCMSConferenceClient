Shader "Custom/Waves-Masked" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Mask ("Base (RGB)", 2D) = "white" {}
        _SpeedX("SpeedX", float) = 3.0
        _SpeedY("SpeedY", float) = 3.0
        _Scale("Scale", range(0.001, 0.2)) = 0.03
        _TileX("TileX", float) = 5
        _TileY("TileY", float) = 5
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
       
        CGPROGRAM
        #pragma surface surf Unlit noshadow nofog
 
        sampler2D _MainTex;
        sampler2D _Mask;
        float4 uv_MainTex_ST;
 
        float _SpeedX;
        float _SpeedY;
        float _Scale;
        float _TileX;
        float _TileY;
 
        struct Input {
            float2 uv_MainTex;
        };

        inline half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
		{
			return half4 ( 0, 0, 0, 1 );
		}
 
        void surf (Input IN, inout SurfaceOutput o)
        {
            float2 uv = IN.uv_MainTex;

            const half4 mask = tex2D (_Mask, uv);

            uv.x += sin((uv.x + uv.y) * _TileX + _Time.g *_SpeedX) *_Scale * mask;
            uv.x += cos(uv.y * _TileY + _Time.g *_SpeedY) *_Scale * mask;
 
            half4 c = tex2D (_MainTex, uv);
            o.Albedo = c.rgb * UNITY_LIGHTMODEL_AMBIENT;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Unlit"
}