// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Environment Background"
{
	Properties
	{
		[NoScaleOffset]_MainTexture("MainTexture", 2D) = "white" {}
		[NoScaleOffset]_Mask("Mask", 2D) = "white" {}
		_ScaleNoise("Scale Noise", Float) = 0
		_Speed("Speed", Float) = 0
		_Tilling("Tilling", Vector) = (0,0,0,0)
		_Float1("Float 1", Float) = 0
		_Float0("Float 0", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit noshadow 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _MainTexture;
		uniform float _Float1;
		uniform float _Speed;
		uniform float2 _Tilling;
		uniform float _ScaleNoise;
		uniform sampler2D _Mask;
		uniform float _Float0;


		float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }

		float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }

		float snoise( float3 v )
		{
			const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
			float3 i = floor( v + dot( v, C.yyy ) );
			float3 x0 = v - i + dot( i, C.xxx );
			float3 g = step( x0.yzx, x0.xyz );
			float3 l = 1.0 - g;
			float3 i1 = min( g.xyz, l.zxy );
			float3 i2 = max( g.xyz, l.zxy );
			float3 x1 = x0 - i1 + C.xxx;
			float3 x2 = x0 - i2 + C.yyy;
			float3 x3 = x0 - 0.5;
			i = mod3D289( i);
			float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
			float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
			float4 x_ = floor( j / 7.0 );
			float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
			float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 h = 1.0 - abs( x ) - abs( y );
			float4 b0 = float4( x.xy, y.xy );
			float4 b1 = float4( x.zw, y.zw );
			float4 s0 = floor( b0 ) * 2.0 + 1.0;
			float4 s1 = floor( b1 ) * 2.0 + 1.0;
			float4 sh = -step( h, 0.0 );
			float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
			float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
			float3 g0 = float3( a0.xy, h.x );
			float3 g1 = float3( a0.zw, h.y );
			float3 g2 = float3( a1.xy, h.z );
			float3 g3 = float3( a1.zw, h.w );
			float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
			g0 *= norm.x;
			g1 *= norm.y;
			g2 *= norm.z;
			g3 *= norm.w;
			float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
			m = m* m;
			m = m* m;
			float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
			return 42.0 * dot( m, px);
		}


		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, 1 );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_TextureSample061 = i.uv_texcoord;
			float mulTime58 = _Time.y * _Float1;
			float2 temp_cast_0 = (_Speed).xx;
			float2 uv_TexCoord44 = i.uv_texcoord * _Tilling;
			float simplePerlin3D28 = snoise( float3( uv_TexCoord44 ,  0.0 )*_ScaleNoise );
			simplePerlin3D28 = simplePerlin3D28*0.5 + 0.5;
			float2 temp_cast_2 = (simplePerlin3D28).xx;
			float2 panner18 = ( mulTime58 * temp_cast_0 + temp_cast_2);
			float2 uv_Mask5 = i.uv_texcoord;
			float4 lerpResult60 = lerp( tex2D( _MainTexture, uv_TextureSample061 ) , tex2D( _MainTexture, panner18 ) , ( tex2D( _Mask, uv_Mask5 ) * _Float0 ));
			o.Emission = lerpResult60.rgb * UNITY_LIGHTMODEL_AMBIENT;
			o.Alpha = 1;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}