Shader "Hidden/MoonProject/Art/LookMaxContactShadows"
{
    // Look spike only: resolves the main light's cascaded shadow to the screen-space shadow texture (as URP's own
    // screen-space shadows do) and darkens it further where a short ray toward the sun hits the depth buffer, so
    // small things (wheels, rocks, panel legs) keep a contact shadow the shadow map is too coarse to give.
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        float _ContactLength;
        float _ContactThickness;
        float _ContactMaxDistance;

        // Steps along the ray toward the sun; fixed so the compiler can bound the loop.
        #define CONTACT_STEPS 20

        half ContactShadow(float3 positionWS, float2 pixel)
        {
            float3 towardLight = _MainLightPosition.xyz;
            float jitter = InterleavedGradientNoise(pixel, 0);
            float startDepth = LinearEyeDepth(positionWS, GetWorldToViewMatrix());
            float bias = 0.02 + startDepth * 0.002;
            [loop]
            for (int i = 1; i <= CONTACT_STEPS; i++)
            {
                float t = (i - jitter) / CONTACT_STEPS * _ContactLength;
                float3 p = positionWS + towardLight * t;
                float4 clip = TransformWorldToHClip(p);
                float2 uv = clip.xy / clip.w * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif
                if (any(uv < 0.0) || any(uv > 1.0))
                {
                    break;
                }

                float sceneDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float behind = clip.w - sceneDepth;
                if (behind > bias && behind < _ContactThickness)
                {
                    return lerp(0.0, 1.0, saturate(startDepth / _ContactMaxDistance));
                }
            }

            return 1.0;
        }

        half4 Fragment(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float deviceDepth = LoadSceneDepth(input.positionCS.xy);
            #if !UNITY_REVERSED_Z
            deviceDepth = deviceDepth * 2.0 - 1.0;
            #endif
            float3 positionWS = ComputeWorldSpacePosition(input.texcoord.xy, deviceDepth, unity_MatrixInvVP);
            half shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(positionWS));
            return min(shadow, ContactShadow(positionWS, input.positionCS.xy));
        }
        ENDHLSL

        Pass
        {
            Name "LookMaxContactShadows"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma vertex Vert
            #pragma fragment Fragment
            ENDHLSL
        }
    }
}
