// The picture through 07's own eye (docs/design/cutscenes.md §1), drawn over the whole frame by a quad the lens view
// keeps in front of the view camera (the vertices are placed straight in clip space from their UVs). Premultiplied:
// the colour is light added (the warm lift, the speck's pale rim), the alpha is light taken away (the curved soft
// lid, the exposure dip, the roll band, the speck's dark centre). Every value comes from MoonProject.World.LensView
// through a MaterialPropertyBlock.
Shader "MoonProject/World/LofiLensView"
{
    Properties
    {
        _LidEdge ("Lid edge (viewport heights)", Float) = 1.3
        _LidSoftness ("Lid softness", Float) = 0.14
        _LidCurve ("Lid curve", Float) = 0.12
        _Dip ("Exposure dip", Float) = 0
        _RollBand ("Roll band centre", Float) = 1.1
        _RollHeight ("Roll band height", Float) = 0.1
        _RollDarkness ("Roll band darkness", Float) = 0.6
        _Warm ("Warm lift (rgb, premultiplied)", Color) = (0, 0, 0, 0)
        _Speck ("Speck centre (xy) radius (z) sharpness (w)", Vector) = (0, 0, 0.03, 0)
        _SpeckLook ("Speck darkness (x) rim (y) blur growth (z)", Vector) = (0.45, 0.12, 3.5, 0)
        _SpeckRimColor ("Speck rim colour", Color) = (0.93, 0.9, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _LidEdge;
                float _LidSoftness;
                float _LidCurve;
                float _Dip;
                float _RollBand;
                float _RollHeight;
                float _RollDarkness;
                float4 _Warm;
                float4 _Speck;
                float4 _SpeckLook;
                float4 _SpeckRimColor;
            CBUFFER_END

            struct Attributes
            {
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 clip = input.uv * 2.0 - 1.0;
                if (_ProjectionParams.x < 0.0)
                {
                    clip.y = -clip.y;
                }

                output.positionCS = float4(clip, UNITY_NEAR_CLIP_VALUE * 0.5, 1.0);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 uv = input.uv;
                float across = uv.x * 2.0 - 1.0;

                // The brow-shutter's edge arcs higher in the middle; the dark is above it.
                float edge = _LidEdge - _LidCurve * across * across;
                float lid = smoothstep(edge - _LidSoftness, edge, uv.y);

                float band = 1.0 - smoothstep(0.0, _RollHeight, abs(uv.y - _RollBand));
                float roll = band * _RollDarkness * _Dip;

                float sharp = saturate(_Speck.w);
                float radius = _Speck.z * lerp(_SpeckLook.z, 1.0, sharp);
                float2 offset = float2((uv.x - 0.5) * aspect, uv.y - 0.5) - float2(_Speck.x * aspect, _Speck.y);
                float r = length(offset) / max(radius, 1e-4);
                float softness = lerp(1.0, 0.25, sharp);
                float disc = 1.0 - smoothstep(1.0 - softness, 1.0, r);
                float rimBand = smoothstep(0.55, 0.85, r) * (1.0 - smoothstep(0.85, 1.05, r));
                float speckWeight = lerp(0.25, 1.0, sharp);
                float speckDark = disc * _SpeckLook.x * speckWeight * (1.0 - rimBand * sharp);
                float3 rim = _SpeckRimColor.rgb * (rimBand * _SpeckLook.y * sharp);

                float keep = (1.0 - lid) * (1.0 - _Dip) * (1.0 - roll) * (1.0 - speckDark);
                float3 added = (_Warm.rgb + rim) * (1.0 - lid);
                return half4(added, 1.0 - keep);
            }
            ENDHLSL
        }
    }
}
