// The call's point of light beside dark Earth (opening v3 purpose §2), unmistakably a signal: a warm amber-white core
// pinned to the sky at _SkyPointDirection, drawn round the camera at the far plane like Earth, so the ground hides it
// and no camera's distance changes it; a thin ring that blooms out of it as each tone begins (_Pulse 0 to 1); and a
// deeper amber ember between the tones (_Key 0). Sizes are fixed in screen pixels, so it reads the same from the wake
// shot's long lens and from 07's chase camera. Additive, and core plus ring stay under the bloom threshold
// (MoonProject.World.CallSkyPointSettings), so it never spikes into a flare. _SkyPointDirection, _Level, _Key and
// _Pulse come per frame from MoonProject.World.CallSkyPoint; the rest from the material the World builder writes.
Shader "MoonProject/World/LofiSkyPoint"
{
    Properties
    {
        _Color ("Tone colour", Color) = (1, 0.82, 0.56, 1)
        _EmberColor ("Ember colour", Color) = (1, 0.58, 0.26, 1)
        _Peak ("Peak brightness", Float) = 0.85
        _CorePixels ("Core radius (px)", Float) = 2.2
        _GlowPixels ("Glow radius (px)", Float) = 12
        _GlowShare ("Glow share", Float) = 0.3
        _RingPixels ("Ring reach (px)", Float) = 26
        _RingWidthPixels ("Ring width (px)", Float) = 1.6
        _RingBrightness ("Ring brightness", Float) = 0.38
        _SkyPointDirection ("Direction (set by code)", Vector) = (0, 0, 1, 0)
        _Level ("Level (set by code)", Float) = 0
        _Key ("Keying (set by code)", Float) = 0
        _Pulse ("Ring progress (set by code)", Float) = 1
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
                float4 _EmberColor;
                float _Peak;
                float _CorePixels;
                float _GlowPixels;
                float _GlowShare;
                float _RingPixels;
                float _RingWidthPixels;
                float _RingBrightness;
                float4 _SkyPointDirection;
                float _Level;
                float _Key;
                float _Pulse;
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
                float reach = max(_GlowPixels, _RingPixels + _RingWidthPixels * 2.0);
                float2 pixels = input.positionOS.xy * reach;
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
                float r = sqrt(r2);
                float core = exp(-r2 / max(_CorePixels * _CorePixels, 1e-4));
                float glowR = max(_GlowPixels * 0.45, 1e-3);
                float glow = exp(-r2 / (glowR * glowR)) * saturate(1.0 - r2 / (_GlowPixels * _GlowPixels));
                float shape = (core + _GlowShare * glow) / (1.0 + _GlowShare);
                float3 tint = lerp(_EmberColor.rgb, _Color.rgb, saturate(_Key));

                // The ring eases out from the core and fades as it goes; it is gone at _Pulse 1.
                float grow = 1.0 - (1.0 - _Pulse) * (1.0 - _Pulse);
                float ringRadius = lerp(_CorePixels, _RingPixels, grow);
                float band = (r - ringRadius) / max(_RingWidthPixels, 1e-3);
                float fade = (1.0 - _Pulse) * (1.0 - _Pulse);
                float ring = exp(-band * band) * fade * _RingBrightness;

                return half4(tint * (_Peak * _Level * shape) + _Color.rgb * ring, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
