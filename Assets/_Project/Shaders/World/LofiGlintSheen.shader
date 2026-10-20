// Sunlight laid on glass: the fallen array's cell faces catching the Sun (opening v3 R4), in the panel's own shape. A
// copy of a wing's cells floated just off them; additive and depth-tested, only the face toward the camera drawn. Its
// brightness drifts a little across the panel (the cracked cells catch unevenly), never in a pattern of spots.
// _Intensity comes per frame from MoonProject.World.ArrayGlint; the colour from the material the World builder
// writes.
Shader "MoonProject/World/LofiGlintSheen"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.96, 0.88, 1)
        _Intensity ("Intensity (set by code)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // How fast, across the panel (per metre), its catch of light drifts, and how much.
            #define DRIFT_SCALE 0.9
            #define DRIFT_DEPTH 0.35

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 p = input.positionWS * DRIFT_SCALE;
                float drift = 0.5 + 0.25 * sin(p.x * 1.7 + p.z * 0.6) + 0.25 * sin(p.z * 1.3 - p.x * 0.9 + p.y);
                float catchLight = 1.0 - DRIFT_DEPTH * drift;
                return half4(_Color.rgb * (_Intensity * catchLight), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
