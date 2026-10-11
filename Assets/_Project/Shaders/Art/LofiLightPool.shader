// A warm pool of light on the ground (M3-18 V3), projected onto whatever is already drawn. The renderer is a unit
// prism (radius 1, half-height _Band) around the pool's centre; its back faces read the scene's depth, rebuild the
// surface behind each pixel and light it with a soft (1 - r^2)^falloff fade across the prism, eased out toward its
// top and bottom and on faces turned away from the sky. Projecting instead of laying a flat disc means the pool
// hugs every facet and slope and never fights the ground's depth: a disc a hair under or over the dust shows a hard
// straight cut wherever the polygon offset stops winning (the wake's dock pool did). _EmissionColor is the
// per-renderer linear multiplier of ARCHITECTURE.md "Glow modulation" (SetVector, 1 = authored, 0 = off), so World
// can grow and fade each pool with the power stages; the transform's uniform scale sets its size. It sinks into the
// basin's haze and fog. Needs the pipeline's depth texture (the PC renderer copies it after opaques).
Shader "MoonProject/Art/LofiLightPool"
{
    Properties
    {
        [HDR] _BaseColor("Pool Colour (linear, at full strength)", Color) = (0.25, 0.12, 0.04, 1)
        [HDR] _EmissionColor("Strength (per renderer)", Color) = (1, 1, 1, 1)
        _Falloff("Falloff", Range(1, 8)) = 2
        _Band("Height Band (prism half-height, object units)", Range(0.02, 1)) = 0.12
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

            // Back faces behind the scene's surface: the pixel's surface may lie inside the prism. They still draw
            // with the camera inside the prism.
            Blend One One
            ZWrite Off
            ZTest GEqual
            Cull Front

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../World/LofiAtmosphere.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _Falloff;
                half _Band;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                #if UNITY_REVERSED_Z
                float depth = SampleSceneDepth(uv);
                #else
                float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, SampleSceneDepth(uv));
                #endif
                float3 positionWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 positionOS = TransformWorldToObject(positionWS);

                half across = saturate(1.0 - dot(positionOS.xz, positionOS.xz));
                half height = saturate(1.0 - abs(positionOS.y) / _Band);

                // The world is flat-shaded, so the surface's screen derivatives give its facet exactly: the pool
                // lights the ground and tops, not the walls standing in it.
                float3 facet = normalize(cross(ddy(positionWS), ddx(positionWS)));
                facet *= dot(facet, GetWorldSpaceViewDir(positionWS)) < 0.0 ? -1.0 : 1.0;
                half sky = saturate(facet.y);

                half pool = PositivePow(across, _Falloff) * height * sky;
                half3 light = _BaseColor.rgb * _EmissionColor.rgb * pool;
                light *= 1.0 - MoonHazeAmount(positionWS);
                half fogFactor = ComputeFogFactor(TransformWorldToHClip(positionWS).z);
                light = MixFogColor(light, half3(0, 0, 0), InitializeInputDataFog(float4(positionWS, 1.0), fogFactor));
                return half4(light, 0.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
