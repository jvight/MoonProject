// The call's point of light beside dark Earth (opening v3 purpose §2): a tiny calm star pinned to the sky at
// _SkyPointDirection, drawn round the camera at the far plane like Earth, so the ground hides it and no camera's
// distance changes it. Its size is fixed in screen pixels (a bright core and a soft glow round it), so it reads the
// same from the wake shot's long lens and from 07's chase camera. Additive, and its peak stays under the bloom
// threshold (MoonProject.World.CallSkyPointSettings), so it never spikes into a flare. _SkyPointDirection and _Level
// come per frame from MoonProject.World.CallSkyPoint; the rest from the material the World builder writes.
Shader "MoonProject/World/LofiSkyPoint"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.95, 0.86, 1)
        _Peak ("Peak brightness", Float) = 1
        _CorePixels ("Core radius (px)", Float) = 1.8
        _GlowPixels ("Glow radius (px)", Float) = 9
        _GlowShare ("Glow share", Float) = 0.3
        _SkyPointDirection ("Direction (set by code)", Vector) = (0, 0, 1, 0)
        _Level ("Level (set by code)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-199"
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
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Any distance inside the camera's clip range works: depth is pinned to the far plane below.
            #define POINT_DISTANCE 100.0
            #define FAR_PLANE_INSET 1e-5

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Peak;
                float _CorePixels;
                float _GlowPixels;
                float _GlowShare;
                float4 _SkyPointDirection;
                float _Level;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 pixels : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                float3 center = _WorldSpaceCameraPos + normalize(_SkyPointDirection.xyz) * POINT_DISTANCE;
                float4 positionCS = TransformWorldToHClip(center);
                float2 pixels = input.positionOS.xy * _GlowPixels;
                positionCS.xy += pixels * 2.0 / _ScreenParams.xy * positionCS.w;
                #if UNITY_REVERSED_Z
                positionCS.z = positionCS.w * FAR_PLANE_INSET;
                #else
                positionCS.z = positionCS.w * (1.0 - FAR_PLANE_INSET);
                #endif

                Varyings output;
                output.positionCS = positionCS;
                output.pixels = pixels;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float r2 = dot(input.pixels, input.pixels);
                float core = exp(-r2 / max(_CorePixels * _CorePixels, 1e-4));
                float glowR = max(_GlowPixels * 0.45, 1e-3);
                float glow = exp(-r2 / (glowR * glowR)) * saturate(1.0 - r2 / (_GlowPixels * _GlowPixels));
                float shape = (core + _GlowShare * glow) / (1.0 + _GlowShare);
                return half4(_Color.rgb * (_Peak * _Level * shape), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
