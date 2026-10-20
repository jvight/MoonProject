// A small, sharp four-point sparkle: sunlight caught on glass (the fallen array's cells, opening v3 R4). A camera-facing
// quad at the object's origin, its size fixed in screen pixels (a bright centre and four short, thin arms along the
// screen's axes), drawn a little toward the camera so the panel under it never cuts it. Additive and depth-tested, so
// the ground hides it. _Intensity comes per frame from MoonProject.World.ArrayGlint; the rest from the material the
// World builder writes.
Shader "MoonProject/World/LofiGlintStar"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.96, 0.88, 1)
        _ArmPixels ("Arm length (px)", Float) = 8
        _WidthPixels ("Arm width (px)", Float) = 0.7
        _CorePixels ("Core radius (px)", Float) = 1.3
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
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // How far toward the camera the sparkle sits off its glass, metres.
            #define TOWARD_CAMERA 0.25

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _ArmPixels;
                float _WidthPixels;
                float _CorePixels;
                float _Intensity;
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
                float3 center = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 toCamera = _WorldSpaceCameraPos - center;
                float distance = max(length(toCamera), 1e-3);
                center += toCamera / distance * min(TOWARD_CAMERA, distance * 0.5);
                float4 positionCS = TransformWorldToHClip(center);
                float2 pixels = input.positionOS.xy * (_ArmPixels + _WidthPixels * 2.0);
                positionCS.xy += pixels * 2.0 / _ScreenParams.xy * positionCS.w;

                Varyings output;
                output.positionCS = positionCS;
                output.pixels = pixels;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.pixels;
                float core = exp(-dot(p, p) / max(_CorePixels * _CorePixels, 1e-4));
                float width2 = max(_WidthPixels * _WidthPixels, 1e-4);
                float alongX = saturate(1.0 - abs(p.x) / _ArmPixels);
                float alongY = saturate(1.0 - abs(p.y) / _ArmPixels);
                float arms = exp(-p.y * p.y / width2) * alongX * alongX + exp(-p.x * p.x / width2) * alongY * alongY;
                float star = core + 0.7 * arms;
                return half4(_Color.rgb * (_Intensity * star), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
