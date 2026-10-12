// The molten seam the cutting beam leaves on a piece (docs/design/feel-pass.md R1, §3.1). The strip driver
// (Gameplay/Feel/HeatSeam) writes each point's heat into uv.x (1 = just cut, 0 = cold) and runs uv.y across the strip.
// Heat maps along four stops: white-yellow (1), orange (2/3), dull red (1/3) and a dark charred line (0), so kept
// progress stays visible as a dark cut. Hot stops are HDR so the fresh seam blooms; the strip's edges are soft.
Shader "MoonProject/Gameplay/HeatSeam"
{
    Properties
    {
        [HDR] _HotColor("Hot (just cut)", Color) = (3.2, 2.56, 1.44, 1)
        [HDR] _OrangeColor("Orange (about 1 s)", Color) = (2.2, 0.9, 0.25, 1)
        [HDR] _RedColor("Dull red (about 3 s)", Color) = (0.55, 0.08, 0.03, 1)
        _CharColor("Charred (cold)", Color) = (0.05, 0.04, 0.045, 0.9)
        _EdgeSoftness("Edge Softness", Range(0.01, 1)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-20"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HeatSeam"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HotColor;
                half4 _OrangeColor;
                half4 _RedColor;
                half4 _CharColor;
                half _EdgeSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half heat = saturate(input.uv.x) * 3.0h;
                half4 color = heat < 1.0h ? lerp(_CharColor, _RedColor, heat)
                    : heat < 2.0h ? lerp(_RedColor, _OrangeColor, heat - 1.0h)
                    : lerp(_OrangeColor, _HotColor, heat - 2.0h);
                half across = 1.0h - abs(input.uv.y * 2.0h - 1.0h);
                half alpha = saturate(across / _EdgeSoftness) * lerp(_CharColor.a, 1.0h, saturate(heat));
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return half4(color.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
