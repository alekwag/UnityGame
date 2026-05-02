// ScannerScreen.shader
// Unlit shader for the screen quad on the scanner gun.
// Samples the RenderTexture produced by the scanner camera.
// Adds a subtle scanline and vignette effect to sell the "monitor" look.

Shader "ScannerSystem/ScannerScreen"
{
    Properties
    {
        _MainTex        ("Render Texture",      2D)    = "black" {}
        _ScanlineCount  ("Scanline Count",      Float) = 80
        _ScanlineAlpha  ("Scanline Strength",   Range(0,1)) = 0.15
        _VignetteStr    ("Vignette Strength",   Range(0,1)) = 0.4
        _Tint           ("Screen Tint",         Color) = (0.75, 1.0, 0.8, 1.0)
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            ZWrite On
            ZTest  LEqual
            Cull   Back

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float     _ScanlineCount;
            float     _ScanlineAlpha;
            float     _VignetteStr;
            fixed4    _Tint;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Tint;

                // Horizontal scanlines
                float scanline = frac(i.uv.y * _ScanlineCount);
                float darkLine = step(0.5, scanline) * _ScanlineAlpha;
                col.rgb -= darkLine;

                // Vignette
                float2 vig = i.uv * 2.0 - 1.0;
                float  vigFactor = 1.0 - dot(vig, vig) * _VignetteStr;
                col.rgb *= saturate(vigFactor);

                return col;
            }
            ENDCG
        }
    }
}
