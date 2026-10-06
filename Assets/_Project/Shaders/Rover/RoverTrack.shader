// Tire tracks: darkens the ground underneath by multiplying with a soft tint of the dust colour. Vertex alpha (from
// the track ribbon's age), a feathered edge across the ribbon (u = 0..1) and a fade with camera distance all blend the
// tint toward white, so tracks press gently into the dust and melt away without sorting artefacts. Polygon offset keeps
// the ribbon off the terrain without lifting it visibly; fog fades the tint out too.
Shader "MoonProject/Rover/Track"
{
    Properties
    {
        _Color ("Tint", Color) = (0.49, 0.5, 0.68, 0.95)
        _EdgeSoftness ("Edge Softness (fraction of half width)", Range(0, 1)) = 0.5
        _FadeNear ("Distance Fade Start (m)", Float) = 20
        _FadeFar ("Distance Fade End (m)", Float) = 55
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "TrackMultiply"
            Tags { "LightMode" = "UniversalForward" }

            Blend DstColor Zero
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _EdgeSoftness;
                float _FadeNear;
                float _FadeFar;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float fogFactor : TEXCOORD0;
                float across : TEXCOORD1;
                float distance : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                output.across = input.uv.x;
                output.distance = distance(positions.positionWS, GetCameraPositionWS());
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half fromCentre = abs(input.across * 2.0 - 1.0);
                half edge = 1.0h - smoothstep(1.0h - _EdgeSoftness, 1.0h, fromCentre);
                half near = 1.0h - smoothstep(_FadeNear, _FadeFar, input.distance);
                half strength = input.color.a * _Color.a * edge * near;
                half3 tint = lerp(half3(1.0h, 1.0h, 1.0h), _Color.rgb, strength);
                tint = MixFogColor(tint, half3(1.0h, 1.0h, 1.0h), input.fogFactor);
                return half4(tint, 1.0h);
            }
            ENDHLSL
        }
    }
}
