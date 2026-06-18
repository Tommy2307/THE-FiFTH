Shader "Custom/PSXVertexJitter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _JitterAmount ("Jitter Amount", Range(0, 0.1)) = 0.02
        _PrecisionLevel ("Vertex Precision", Float) = 100
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _JitterAmount;
            float _PrecisionLevel;

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
                float4 clipPos = UnityObjectToClipPos(v.vertex);

                // Snap vertex positions to simulate low precision (PS1 had no sub-pixel accuracy)
                clipPos.xyz = floor(clipPos.xyz * _PrecisionLevel) / _PrecisionLevel;

                o.vertex = clipPos;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDHLSL
        }
    }
}