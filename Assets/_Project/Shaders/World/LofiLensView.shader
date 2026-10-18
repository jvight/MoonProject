// 07's lid darkening the picture (docs/design/cutscenes.md §1), drawn over the whole frame by a quad the lens view
// keeps in front of the view camera (the vertices are placed straight in clip space from their UVs). Premultiplied
// black: the alpha is the light the curved soft lid takes away. Every value comes from MoonProject.World.LensView
// through a MaterialPropertyBlock.
Shader "MoonProject/World/LofiLensView"
{
    Properties
    {
        _LidEdge ("Lid edge (viewport heights)", Float) = 1.3
        _LidSoftness ("Lid softness", Float) = 0.14
        _LidCurve ("Lid curve", Float) = 0.12
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
                float2 uv = input.uv;
                float across = uv.x * 2.0 - 1.0;

                // The brow-shutter's edge arcs higher in the middle; the dark is above it.
                float edge = _LidEdge - _LidCurve * across * across;
                float lid = smoothstep(edge - _LidSoftness, edge, uv.y);
                return half4(0.0, 0.0, 0.0, lid);
            }
            ENDHLSL
        }
    }
}
