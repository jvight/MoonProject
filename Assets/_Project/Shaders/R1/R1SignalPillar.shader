// R1 stylized vacuum signal pillar shader for B11 (MoonProject/R1/SignalPillar).
// Renders a thin, faint, cool-white vertical telemetry beam with a soft top fade and smooth radial edge falloff
// (vacuum logic: stylized beam, no fog volume or particle cloud).
Shader "MoonProject/R1/SignalPillar"
{
    Properties
    {
        [MainColor] _BeamColor("Beam Cool White Color", Color) = (0.76, 0.88, 1.00, 1.0)
        _CoreColor("Inner Core White Color", Color) = (0.94, 0.98, 1.00, 1.0)
        _Intensity("Beam Intensity", Range(0.0, 2.0)) = 0.52
        _PillarHeight("Pillar Height (m)", Float) = 60.0
        _BottomFadeMeters("Bottom Soft Fade (m)", Range(0.05, 5.0)) = 0.85
        _TopFadePower("Top Fade Power", Range(0.5, 6.0)) = 1.85
        _FresnelPower("Radial Edge Softness", Range(0.5, 8.0)) = 2.40
        _BloomSafeMax("Bloom Safe Ceiling", Range(0.5, 1.24)) = 1.08
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "SignalPillarForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SignalPillarVert
            #pragma fragment SignalPillarFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BeamColor;
                half4 _CoreColor;
                half  _Intensity;
                float _PillarHeight;
                float _BottomFadeMeters;
                half  _TopFadePower;
                half  _FresnelPower;
                half  _BloomSafeMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  heightM    : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                half4  vcol       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings SignalPillarVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                float3 originWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.heightM = max(0.0, posInputs.positionWS.y - originWS.y);
                output.uv = input.uv;
                output.vcol = input.color;
                return output;
            }

            half4 SignalPillarFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Reject flat end-caps
                if (abs(input.normalWS.y) > 0.85)
                {
                    return half4(0.0h, 0.0h, 0.0h, 0.0h);
                }

                float h01 = saturate(input.heightM / max(_PillarHeight, 1.0));
                half botFade = (half)smoothstep(0.0, max(_BottomFadeMeters, 0.05), input.heightM);
                half topFade = pow(max(1.0h - (half)h01, 0.0h), _TopFadePower);

                // Horizontal view-to-normal alignment so the cylinder silhouette edges fade smoothly to zero from any elevation angle
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float2 nHoriz = input.normalWS.xz;
                float2 vHoriz = viewDirWS.xz;
                float nLen = max(length(nHoriz), 1e-4);
                float vLen = max(length(vHoriz), 1e-4);
                half ndotv = (half)saturate(abs(dot(nHoriz / nLen, vHoriz / vLen)));
                half radialFade = pow(ndotv, _FresnelPower);

                half shellWeight = lerp(0.45h, 1.0h, saturate(input.vcol.g));
                half3 tint = lerp(_BeamColor.rgb, _CoreColor.rgb, saturate(input.vcol.g * ndotv));
                half3 finalRgb = tint * (_Intensity * botFade * topFade * radialFade * shellWeight);
                finalRgb = min(finalRgb, half3(_BloomSafeMax, _BloomSafeMax, _BloomSafeMax));

                return half4(finalRgb, saturate(botFade * topFade * radialFade));
            }
            ENDHLSL
        }
    }
}
