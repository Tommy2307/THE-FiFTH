Shader "Custom/PSXPostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Resolution ("Render Resolution", Vector) = (320, 240, 0, 0)
        _ColorDepth ("Color Depth", Float) = 32
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.5
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
            float2 _Resolution;
            float _ColorDepth;
            float _DitherStrength;

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

            float Dither(float2 uv)
            {
                float2 pixelPos = uv * _Resolution;
                float pattern = frac(sin(dot(pixelPos, float2(12.9898, 78.233))) * 43758.5453);
                return pattern;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Pixelate by snapping UVs to low resolution grid
                float2 pixelatedUV = floor(i.uv * _Resolution) / _Resolution;
                fixed4 col = tex2D(_MainTex, pixelatedUV);

                // Reduce color depth (like PS1's 15-bit color)
                float levels = _ColorDepth;
                col.rgb = floor(col.rgb * levels) / levels;

                // Add dithering noise
                float dither = Dither(i.uv) * _DitherStrength * 0.05;
                col.rgb += dither;

                return col;
            }
            ENDHLSL
        }
    }
}