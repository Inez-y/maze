Shader "Custom/DayNightAmbientGlobal"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _DayColor("Day Ambient Color", Color) = (1,1,1,1)
        _NightColor("Night Ambient Color", Color) = (0.1,0.1,0.2,1)

        // Optional: this is just for preview in the Inspector if you want
        //_DayNightBlend("Day-Night Blend (Local)", Range(0,1)) = 0
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

            // 🔥 GLOBAL value: set from C# with Shader.SetGlobalFloat("_GlobalDayNight", ...)
            float _GlobalDayNight;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv      : TEXCOORD0;
                float4 vertex  : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Clamp global blend 0–1 just in case
                float blend = saturate(_GlobalDayNight);

                float4 ambient = lerp(_DayColor, _NightColor, blend);
                col.rgb *= ambient.rgb;

                return col;
            }
            ENDCG
        }
    }
}
