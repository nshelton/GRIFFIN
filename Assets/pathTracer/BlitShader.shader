﻿Shader "Hidden/CustomBlitShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _Depth;
			float4 _MainTex_TexelSize;
			float _Exposure;

			float4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;
				uv.y = 1.0 - uv.y;
				float4 thisPixel = tex2D(_MainTex, uv);

				return  float4(saturate(thisPixel.rgb), 1.0);
			}
			ENDCG
		}
	}
}