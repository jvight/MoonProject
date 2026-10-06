// Small camera-facing stars of light: the glints that make far-away scrap read on the horizon. Every glint of the
// field lives in one dynamic mesh (one draw call): its four vertices share the glint's centre, uv0 is the corner
// (-1..1) the vertex shader expands along the camera axes, uv1.x the half-size in metres and uv1.y the brightness
// (twinkle and distance fade, computed on the CPU). Additive, no depth write.
Shader "MoonProject/Gameplay/Glint"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.37, 0.95, 1, 1)
        _Core ("Core Radius", Range(0.05, 1)) = 0.45
        _Rays ("Ray Strength", Range(0, 1)) = 0.55
        _RayWidth ("Ray Width", Range(0.01, 0.4)) = 0.07
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Glint"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Core;
                half _Rays;
                half _RayWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 corner : TEXCOORD0;
                float2 sizeAndBrightness : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half brightness : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 centre = TransformObjectToWorld(input.positionOS.xyz);
                float3 right = UNITY_MATRIX_V._m00_m01_m02;
                float3 up = UNITY_MATRIX_V._m10_m11_m12;
                float halfSize = input.sizeAndBrightness.x;
                float3 positionWS = centre + (right * input.corner.x + up * input.corner.y) * halfSize;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.corner;
                output.brightness = input.sizeAndBrightness.y;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv;
                float radius = length(p);
                half core = saturate(1.0 - radius / _Core);
                core *= core * core;
                half rayX = saturate(1.0 - abs(p.y) / _RayWidth) * saturate(1.0 - abs(p.x));
                half rayY = saturate(1.0 - abs(p.x) / _RayWidth) * saturate(1.0 - abs(p.y));
                half rays = max(rayX, rayY);
                rays *= rays * _Rays;
                half shape = (core + rays) * saturate(1.0 - radius);
                half3 rgb = _Color.rgb * (shape * input.brightness);
                rgb = MixFogColor(rgb, half3(0.0, 0.0, 0.0), input.fogFactor);
                return half4(rgb, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
