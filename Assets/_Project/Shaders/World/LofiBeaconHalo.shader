// Soft additive glow around The Peak's beacon lamp. Drawn on a unit quad (corners at +-1 in object XY): the quad
// faces the camera, is sized max(_Radius, distance * _MinAngleTan) so the beacon never shrinks below a readable
// dot, and is pulled toward the camera by its own size so the summit it sits on never cuts it. Unfogged: the
// beacon is a promise on the horizon. _Color/_Intensity/_Radius/_MinAngleTan come from MoonProject.World.PeakBeacon
// through a MaterialPropertyBlock.
Shader "MoonProject/World/LofiBeaconHalo"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.353, 0.431, 1)
        _Intensity ("Intensity", Float) = 1
        _Radius ("Radius (m)", Float) = 2
        _MinAngleTan ("Min apparent radius (tan)", Float) = 0.007
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
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _Radius;
                float _MinAngleTan;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                float3 center = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 toCamera = _WorldSpaceCameraPos - center;
                float distance = max(length(toCamera), 1e-3);
                float size = max(_Radius, distance * _MinAngleTan);
                float3 right = UNITY_MATRIX_V[0].xyz;
                float3 up = UNITY_MATRIX_V[1].xyz;
                float3 world = center + (right * input.positionOS.x + up * input.positionOS.y) * size
                    + toCamera / distance * min(size, distance * 0.5);

                Varyings output;
                output.positionCS = TransformWorldToHClip(world);
                output.corner = input.positionOS.xy;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float r2 = dot(input.corner, input.corner);
                float edge = saturate(1.0 - r2);
                float glow = edge * edge * (0.25 + 0.75 * exp(-r2 * 10.0));
                return half4(_Color.rgb * (_Intensity * glow), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
