// ScannerHighlight.shader
// Renders a coloured highlight overlay on top of everything (ZTest Always).
// The highlight is invisible in the main camera via _Alpha being set to 0
// through a MaterialPropertyBlock (see HighlightVisibilityController.cs).
//
// Usage:
//  1. Create a new Material → assign this shader.
//  2. In VentController, assign this material to highlightMaterial.
//  3. VentController will instance it per vent and set _HighlightColor.

Shader "ScannerSystem/VentHighlight"
{
    Properties
    {
        _HighlightColor ("Highlight Color", Color) = (0, 1, 0.2, 0.55)
        // _Alpha is controlled at runtime via MaterialPropertyBlock.
        // Value of -1 means "use the alpha from _HighlightColor".
        // Value of  0 means fully transparent (hidden in main camera).
        _Alpha ("Alpha Override (-1 = use color alpha)", Float) = -1
        
        // Edge pulse for a scan-line feel (optional, set by script)
        _PulseSpeed ("Pulse Speed", Float) = 0.0
        _PulseMin   ("Pulse Min Alpha", Float) = 0.3
    }

    SubShader
    {
        // Render after all opaque and transparent geometry
        Tags
        {
            "Queue"             = "Overlay+100"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
        }

        Pass
        {
            Name "HIGHLIGHT_OVERLAY"

            // KEY: ignore depth buffer → renders through walls
            ZTest    Always
            ZWrite   Off

            // Standard alpha blending
            Blend    SrcAlpha OneMinusSrcAlpha
            Cull     Back

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4  _HighlightColor;
            float   _Alpha;
            float   _PulseSpeed;
            float   _PulseMin;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _HighlightColor;

                // Alpha override: 0 = invisible (main camera hides via MPB)
                if (_Alpha >= 0.0)
                    col.a = _Alpha;

                // Optional pulse animation
                if (_PulseSpeed > 0.0)
                {
                    float pulse = lerp(_PulseMin, 1.0, (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5));
                    col.a *= pulse;
                }

                return col;
            }
            ENDCG
        }
    }

    // Fallback: invisible if shader not supported
    Fallback Off
}
