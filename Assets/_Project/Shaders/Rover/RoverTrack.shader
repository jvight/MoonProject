// Tire tracks: darkens the ground underneath by multiplying with a tint. Vertex alpha (from the track ribbon's age)
// blends the tint toward white, so old tracks fade into the dust without sorting artefacts. Polygon offset keeps the
// ribbon off the terrain without lifting it visibly; fog fades the tint out with distance.
Shader "MoonProject/Rover/Track"
{
    Properties
    {
        _Color ("Tint", Color) = (0.27, 0.28, 0.48, 1)
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
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float fogFactor : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half strength = input.color.a * _Color.a;
                half3 tint = lerp(half3(1.0h, 1.0h, 1.0h), _Color.rgb, strength);
                tint = MixFogColor(tint, half3(1.0h, 1.0h, 1.0h), input.fogFactor);
                return half4(tint, 1.0h);
            }
            ENDHLSL
        }
    }
}
