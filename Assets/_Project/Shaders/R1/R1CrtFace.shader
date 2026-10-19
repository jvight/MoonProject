Shader "MoonProject/R1/CrtFace"
{
    Properties
    {
        _ScreenBaseColor ("Screen Base Color", Color) = (0.125, 0.078, 0.039, 1)
        _ScreenEdgeColor ("Screen Edge Color", Color) = (0.071, 0.043, 0.020, 1)
        _PhosphorColor ("Phosphor Amber Color", Color) = (1.000, 0.682, 0.220, 1)
        _PhosphorGlowColor ("Phosphor Halo Color", Color) = (0.784, 0.416, 0.078, 1)
        _HotCoreColor ("Hot Core Color", Color) = (1.000, 0.953, 0.800, 1)
        _BloomSafeMax ("Bloom Safe Max", Range(0.5, 1.2)) = 1.06
        _HotCorePeak ("Hot Core Peak", Range(1.0, 2.5)) = 1.65
        _Smoothness ("Glass Smoothness", Range(0.0, 1.0)) = 0.86
        _ScanlineCount ("Scanline Count", Range(60.0, 480.0)) = 280.0
        _ScanlineDepth ("Scanline Depth", Range(0.0, 0.5)) = 0.15
        _GlowRadius ("Phosphor Halo Radius", Range(0.005, 0.08)) = 0.028
        _TrailAlpha ("Phosphor Trail Alpha", Range(0.0, 0.8)) = 0.28
        _ContentGlowIntensity ("Content Phosphor Glow Intensity", Range(0.0, 0.5)) = 0.20
        _ContentGlowMip ("Content Phosphor Glow Mip", Range(0.5, 4.0)) = 1.25
        _ContentScanlineAtten ("Content Scanline Attenuation", Range(0.0, 1.0)) = 0.60
        _ContentMinStrokeLuma ("Content Min Stroke Luma", Range(0.0, 1.0)) = 0.0

        _EyeLeftRect ("Left Eye Rect (cx, cy, w, h)", Vector) = (0.31, 0.50, 0.2006, 0.4012)
        _EyeLeftShape ("Left Eye Shape (tilt, cut, squint, open)", Vector) = (0, 0, 0, 1)
        _EyeLeftMode ("Left Eye Mode (arc, ring, closed, scale)", Vector) = (0, 0, 0, 1)
        _EyeRightRect ("Right Eye Rect (cx, cy, w, h)", Vector) = (0.69, 0.50, 0.2006, 0.4012)
        _EyeRightShape ("Right Eye Shape (tilt, cut, squint, open)", Vector) = (0, 0, 0, 1)
        _EyeRightMode ("Right Eye Mode (arc, ring, closed, scale)", Vector) = (0, 0, 0, 1)

        _GhostLeftRect ("Ghost Left Eye Rect", Vector) = (0.31, 0.50, 0.2006, 0.4012)
        _GhostLeftShape ("Ghost Left Eye Shape", Vector) = (0, 0, 0, 1)
        _GhostLeftMode ("Ghost Left Eye Mode", Vector) = (0, 0, 0, 1)
        _GhostRightRect ("Ghost Right Eye Rect", Vector) = (0.69, 0.50, 0.2006, 0.4012)
        _GhostRightShape ("Ghost Right Eye Shape", Vector) = (0, 0, 0, 1)
        _GhostRightMode ("Ghost Right Eye Mode", Vector) = (0, 0, 0, 1)

        _FaceSymbol ("Face Symbol (id, alpha, time, 0)", Vector) = (0, 0, 0, 0)
        _FaceCrtState ("CRT State (boot, brightness, lowPower, jitter)", Vector) = (1, 1, 0, 0)
        _FacePhases ("CRT Phases (scanline, band, bootLineEnd, bootOpenSpan)", Vector) = (0, 0, 0.45, 0.30)
        _ContentTex ("Screen Content Texture", 2D) = "black" {}
        _FaceContentState ("Screen Content State (mix, pinch, lineGlow, 0)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CrtFacePassVertex
            #pragma fragment CrtFacePassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "R1CrtFaceCore.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "R1CrtFaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings ShadowPassVertex(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "R1CrtFaceInput.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings DepthOnlyVertex(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing

            #include "R1CrtFaceInput.hlsl"

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthNormalsVaryings DepthNormalsVertex(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output = (DepthNormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFragment(DepthNormalsVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                return half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
