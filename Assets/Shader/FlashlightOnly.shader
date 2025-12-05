Shader "Custom/FlashlightOnly"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        _FlashColor("Flashlight Color", Color) = (1,1,1,1)
        _FlashIntensity("Flashlight Intensity", Float) = 100.0
        _FlashRange("Flashlight Range", Float) = 20.0
        _FlashInnerAngle("Flash Inner Angle (deg)", Float) = 15.0
        _FlashOuterAngle("Flash Outer Angle (deg)", Float) = 30.0

        _FlashOn("Flashlight On (0/1)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _FlashColor;
            float  _FlashIntensity;
            float  _FlashRange;
            float  _FlashInnerAngle;
            float  _FlashOuterAngle;
            float  _FlashOn;

            // Per-material (or global) values set from C#
            float3 _FlashPos;   // world-space position
            float3 _FlashDir;   // world-space direction (normalized)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv       : TEXCOORD0;
                float4 vertex   : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float flashOn = saturate(_FlashOn);
                if (flashOn > 0.0)
                {
                    // From light to pixel
                    float3 toPixel = i.worldPos - _FlashPos;
                    float dist = length(toPixel);
                    float3 L = toPixel / max(dist, 0.0001);

                    float3 dir = normalize(_FlashDir);

                    // 1 when pixel is exactly in front of flashlight
                    float spot = dot(dir, L);

                    float innerCos = cos(radians(_FlashInnerAngle));
                    float outerCos = cos(radians(_FlashOuterAngle));

                    float angleT = saturate((spot - outerCos) / max(innerCos - outerCos, 0.0001));
                    float rangeT = saturate(1.0 - dist / max(_FlashRange, 0.0001));

                    float flashFactor = angleT * rangeT * flashOn;

                    col.rgb += _FlashColor.rgb * _FlashIntensity * flashFactor;
                }

                return col;
            }

            ENDCG
        }
    }
}
