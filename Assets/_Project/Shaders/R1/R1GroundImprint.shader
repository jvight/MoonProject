// R1 ground imprint (Decal_BootprintTrail): shallow relief pressed into the regolith, projected onto whatever ground
// is already drawn. The renderer is a thin box around the trail; its back faces read the scene's depth, rebuild the
// surface behind each pixel and re-shade only that surface's sunlight with the imprint's normal map, as a 2x multiply
// (0.5 = untouched). It paints no colour of its own, so it keeps the ground's albedo, dust and haze and has no quad
// edge: the mask fades it out at the strip's sides and ends, and outside the box nothing changes. The relief fades as
// the sun climbs, so the prints read in grazing light and all but vanish at high sun.
// Needs the pipeline's depth texture (the PC renderer copies it after opaques).
Shader "MoonProject/R1/GroundImprint"
{
    Properties
    {
        [MainTexture] _BaseMap("Compaction Tint (R) Mask (A)", 2D) = "white" {}
        [Normal] _BumpMap("Imprint Normal", 2D) = "bump" {}
        _ImprintSize("Imprint Size (object X, object Y, metres)", Vector) = (0.95, 3.0, 0, 0)
        _BandBelow("Reach Below Origin (m)", Range(0.0, 0.5)) = 0.10
        _BandAbove("Reach Above Origin (m)", Range(0.0, 0.5)) = 0.02
        _ReliefStrength("Relief Strength", Range(0.0, 2.0)) = 0.45
        _MaxShade("Max Shade Change", Range(0.0, 0.9)) = 0.25
        _GrazeStart("Full Relief Below Sun Height (flat N.L)", Range(0.0, 1.0)) = 0.35
        _GrazeEnd("Faint Relief Above Sun Height (flat N.L)", Range(0.0, 1.0)) = 0.75
        _HighSunRelief("Relief Left At High Sun", Range(0.0, 1.0)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-60"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GroundImprint"
            Tags { "LightMode" = "UniversalForward" }

            // Back faces behind the scene's surface: the pixel's surface may lie inside the box.
            Blend DstColor SrcColor
            ZWrite Off
            ZTest GEqual
            Cull Front

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../World/LofiAtmosphere.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BumpMap_ST;
                float4 _ImprintSize;
                half _BandBelow;
                half _BandAbove;
                half _ReliefStrength;
                half _MaxShade;
                half _GrazeStart;
                half _GrazeEnd;
                half _HighSunRelief;
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
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                #if UNITY_REVERSED_Z
                float depth = SampleSceneDepth(screenUV);
                #else
                float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, SampleSceneDepth(screenUV));
                #endif
                float3 positionWS = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                float3 positionOS = TransformWorldToObject(positionWS);

                // The strip lies in the object's XY plane; its up is whichever side of that plane faces the sky, so
                // the band test holds whatever axis conversion the model was imported with.
                float3 tangentWS = TransformObjectToWorldDir(float3(1.0, 0.0, 0.0));
                float3 bitangentWS = TransformObjectToWorldDir(float3(0.0, 1.0, 0.0));
                float3 stripUpWS = normalize(cross(tangentWS, bitangentWS));
                stripUpWS *= stripUpWS.y < 0.0 ? -1.0 : 1.0;
                float rise = dot(positionWS - TransformObjectToWorld(float3(0.0, 0.0, 0.0)), stripUpWS);

                // The world is flat-shaded, so the surface's screen derivatives give its facet exactly; prints press
                // only into ground that faces the sky, never into a wheel or rock standing in the box. Taken before
                // the early-out so the derivatives stay in uniform control flow.
                float3 facet = normalize(cross(ddy(positionWS), ddx(positionWS)));
                facet *= dot(facet, GetWorldSpaceViewDir(positionWS)) < 0.0 ? -1.0 : 1.0;
                half groundness = smoothstep(0.70, 0.90, facet.y);

                float2 uv = positionOS.xy / _ImprintSize.xy + 0.5;
                bool inside = all(uv >= 0.0) && all(uv <= 1.0) && rise >= -_BandBelow && rise <= _BandAbove;
                if (!inside)
                {
                    return half4(0.5, 0.5, 0.5, 1.0);
                }

                half4 tintMask = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half3 imprintTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv));
                float3 t = normalize(tangentWS - facet * dot(tangentWS, facet));
                float3 b = normalize(bitangentWS - facet * dot(bitangentWS, facet) - t * dot(bitangentWS, t));
                float3 imprintWS = normalize(imprintTS.x * t + imprintTS.y * b + imprintTS.z * facet);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                half flatNL = dot(facet, mainLight.direction);
                half relief = 0.0;
                if (flatNL > 0.0)
                {
                    half ratio = saturate(dot(imprintWS, mainLight.direction)) / max(flatNL, 0.04);
                    half graze = lerp(_HighSunRelief, 1.0, 1.0 - smoothstep(_GrazeStart, _GrazeEnd, flatNL));
                    relief = clamp(_ReliefStrength * (ratio - 1.0) * graze, -_MaxShade, _MaxShade);
                    relief *= mainLight.shadowAttenuation;
                }

                half fade = tintMask.a * groundness * (1.0 - MoonHazeAmount(positionWS));
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                half fogFactor = ComputeFogFactor(TransformWorldToHClip(positionWS).z);
                fade *= ComputeFogIntensity(InitializeInputDataFog(float4(positionWS, 1.0), fogFactor));
                #endif
                half shade = lerp(1.0, (1.0 + relief) * tintMask.r, fade);
                return half4(0.5 * shade.xxx, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
