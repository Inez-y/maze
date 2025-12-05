Shader "Custom/DayNightAmbientFogFlashlightGlobal"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        _DayColor("Day Ambient Color", Color) = (1,1,1,1)
        _NightColor("Night Ambient Color", Color) = (0.1,0.1,0.2,1)

        _FogColor("Fog Color", Color) = (0.5,0.6,0.7,1)
        _FogStart("Fog Start Distance", Float) = 10
        _FogEnd("Fog End Distance", Float) = 40

        // Flashlight properties
        _FlashColor("Flashlight Color", Color) = (1,1,1,1)
        _FlashIntensity("Flashlight Intensity", Float) = 3.0
        _FlashRange("Flashlight Range", Float) = 20.0
        _FlashInnerAngle("Flash Inner Angle (deg)", Float) = 15.0
        _FlashOuterAngle("Flash Outer Angle (deg)", Float) = 30.0
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

            float4 _DayColor;
            float4 _NightColor;

            float4 _FogColor;
            float  _FogStart;
            float  _FogEnd;

            float4 _FlashColor;
            float  _FlashIntensity;
            float  _FlashRange;
            float  _FlashInnerAngle;
            float  _FlashOuterAngle;

            // Global controls (set from C#)
            float _GlobalDayNight;     // 0 = day, 1 = night
            float _GlobalFogToggle;    // 0 = fog off, 1 = fog on
            float _GlobalFlashlightOn; // 0 = off, 1 = on

            float3 _FlashlightPos;     // world-space position
            float3 _FlashlightDir;     // world-space direction (normalized)

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

                // ----- Day / Night Ambient -----
                float dayNightBlend = saturate(_GlobalDayNight);
                float4 ambient = lerp(_DayColor, _NightColor, dayNightBlend);
                col.rgb *= ambient.rgb;

                // ----- Flashlight (spotlight attached to camera) -----
                float flashOn = saturate(_GlobalFlashlightOn);
                if (flashOn > 0.0)
                {
                    // Vector from fragment to light
                    float3 toLight = _FlashlightPos - i.worldPos;
                    float dist = length(toLight);
                    float3 L = toLight / max(dist, 0.0001);

                    float3 dir = normalize(_FlashlightDir);

                    // Cosine of angle between light direction and pixel direction
                    float spot = dot(L, dir); // 1 = center of beam

                    // Convert angles from degrees to cosine thresholds
                    float innerCos = cos(radians(_FlashInnerAngle));
                    float outerCos = cos(radians(_FlashOuterAngle));

                    // Angle falloff (1 inside inner cone, 0 outside outer cone)
                    float angleT = saturate((spot - outerCos) / max(innerCos - outerCos, 0.0001));

                    // Distance falloff (1 near, 0 at range)
                    float rangeT = saturate(1.0 - dist / max(_FlashRange, 0.0001));

                    // Final flashlight factor
                    float flashFactor = angleT * rangeT * flashOn;

                    // Add flashlight color
                    col.rgb += _FlashColor.rgb * _FlashIntensity * flashFactor;
                }

                // ----- Fog -----
                float distCam = distance(_WorldSpaceCameraPos, i.worldPos);
                float fogRange = max(_FogEnd - _FogStart, 0.0001);
                float fogFactor = saturate((distCam - _FogStart) / fogRange);
                fogFactor *= saturate(_GlobalFogToggle);

                col.rgb = lerp(col.rgb, _FogColor.rgb, fogFactor);

                return col;
            }
            ENDCG
        }
    }
}
