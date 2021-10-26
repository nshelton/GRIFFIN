Shader "Hidden/tonemapper"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float _Gamma;
            float _Exposure;

            float3 lumaBasedReinhardToneMapping(float3 color)
            {
                float luma = dot( color, float3(0.2126, 0.7152, 0.0722));
                float toneMappedLuma = luma / (1. + luma);
                color *= toneMappedLuma / luma;
                color = pow(color, (float3)(1. / _Gamma));
                return color;

            }

            float3 simpleReinhardToneMapping(float3 color)
            {
                float exposure = 1.5;
                color *= _Exposure / (1. + color / _Exposure);
                color = pow(color, (float3)(1. / _Gamma));
                return color;
            }




            sampler2D _MainTex;


            fixed4 frag(v2f i) : SV_Target
            {
                float3 color = simpleReinhardToneMapping(tex2D(_MainTex, i.uv).rgb);
                fixed4 col = float4(color, 1.0);
                
            
            return col;

            }
            ENDCG
        }
    }
}
