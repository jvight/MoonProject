// A scannable fragment's feature-edge trace (SCAN v2): hidden during the scan sweep so only the crisp cyan scan line
// and low-contrast raster band show on the surface; at the completion ping (_Dissolve), a thin anti-aliased warm-amber
// emissive edge traces the object's feature edges, then fades.
Shader "MoonProject/Art/LofiScanLines"
{
    Properties
    {
        [HDR] _LineColor("Line drawing (linear)", Color) = (0.88, 0.92, 0.96, 1)
        _ScanProgress("Scan progress", Range(0, 1)) = 0
        _Dissolve("Dissolve", Range(0, 1)) = 0
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
            Name "ScanLines"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend One One

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
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
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
                output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half pingEnv = MoonScanPingEnvelope(_Dissolve);
                clip(pingEnv - 0.002h);
                clip(1.0h - MoonScanUnthread(_Dissolve) - (half)MoonScanReach(input.positionWS) * 0.65h);

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half centerProfile = smoothstep(0.35h, 0.85h, saturate(dot(normalize(input.normalWS), viewDirWS)));
                half4 pingParams = (half4)MoonScanGetPingParams();
                half3 pingColor = MoonScanGetPingColor();
                half3 emission = pingColor * (pingEnv * centerProfile * pingParams.w * 0.65h);
                return half4(MixFogColor(emission, half3(0.0h, 0.0h, 0.0h), input.fogFactor), 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
