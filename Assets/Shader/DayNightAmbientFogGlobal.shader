Shader "Custom/DayNightAmbientFogGlobal"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        _DayColor("Day Ambient Color", Color) = (1,1,1,1)
        _NightColor("Night Ambient Color", Color) = (0.1,0.1,0.2,1)

        _FogColor("Fog Color", Color) = (0.5,0.6,0.7,1)
        _FogStart("Fog Start Distance", Float) = 3
        _FogEnd("Fog End Distance", Float) = 10
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

            // Global values set from C#
            float _GlobalDayNight;   // 0 = day, 1 = night
            float _GlobalFogToggle;  // 0 = fog off, 1 = fog on

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

                // ----- Fog -----
                // Distance from camera to pixel
                float dist = distance(_WorldSpaceCameraPos, i.worldPos);

                // How strong fog should be based on distance
                float fogFactor = 0.0;

                // Avoid division by zero
                float fogRange = max(_FogEnd - _FogStart, 0.0001);
                fogFactor = saturate((dist - _FogStart) / fogRange);

                // Multiply by global toggle (0 or 1)
                fogFactor *= saturate(_GlobalFogToggle);

                // Mix original color with fog color
                col.rgb = lerp(col.rgb, _FogColor.rgb, fogFactor);

                return col;
            }
            ENDCG
        }
    }
}
