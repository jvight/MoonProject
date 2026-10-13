// A scannable fragment's line drawing (0.5.5 owner feedback §1, D2): Kenji's white blueprint lines along the object's
// feature edges, unlit and a touch over white so they bloom softly. They appear below the sweep as _ScanProgress rises;
// in the close (_Dissolve) they stand while the solid fades, then unthread from the outermost inward to the object's
// centre, where the ribbon leaves for 07's lens, and are gone before the lens flares.
Shader "MoonProject/Art/LofiScanLines"
{
    Properties
    {
        [HDR] _LineColor("Line drawing (linear)", Color) = (1.2, 1.22, 1.28, 1)
        _ScanProgress("Scan progress", Range(0, 1)) = 0
        _Dissolve("Dissolve", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest+10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ScanLines"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "LofiScanInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LineColor;
                half _ScanProgress;
                half _Dissolve;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
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
                output.positionWS = positions.positionWS;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                clip(_ScanProgress - MoonScanHeight(input.positionWS));
                clip(1.0 - MoonScanUnthread(_Dissolve) - MoonScanReach(input.positionWS)
                    - step(MOON_SCAN_LINES_END, _Dissolve));
                half3 color = MixFog(_LineColor.rgb, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
