// A warm pool of light on the ground (M3-18 V3): a flat disc whose uv runs -1..1 across it, added onto what is
// already drawn with a soft (1 - r^2)^falloff fade to nothing at its rim, so it never shows an edge. One light-decal
// costs a single transparent quad-fan instead of a real point light. _EmissionColor is the per-renderer linear
// multiplier of ARCHITECTURE.md "Glow modulation" (SetVector, 1 = authored, 0 = off), so World can grow and fade
// each pool with the power stages; the transform's scale sets its size. It sinks into the basin's haze and fog.
Shader "MoonProject/Art/LofiLightPool"
{
    Properties
    {
        [HDR] _BaseColor("Pool Colour (linear, at full strength)", Color) = (0.25, 0.12, 0.04, 1)
        [HDR] _EmissionColor("Strength (per renderer)", Color) = (1, 1, 1, 1)
        _Falloff("Falloff", Range(1, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-50"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "LightPool"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "../World/LofiAtmosphere.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _Falloff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half pool = PositivePow(saturate(1.0 - dot(input.uv, input.uv)), _Falloff);
                half3 light = _BaseColor.rgb * _EmissionColor.rgb * pool;
                light *= 1.0 - MoonHazeAmount(input.positionWS);
                light = MixFogColor(light, half3(0, 0, 0), InitializeInputDataFog(float4(input.positionWS, 1.0),
                    input.fogFactor));
                return half4(light, 0.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
