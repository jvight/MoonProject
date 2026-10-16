// Placement ghost and un-welded stage hologram (0.5.7 slice 6, BUILD-MODE): calm translucent cyan (#5FF3FF) body with
// a warm amber (#FFB547) Fresnel rim and horizontal scan-line shimmer, shifting to coral alert (#FF5A6E) when
// _Invalid > 0. Strictly clamped under Palette.BloomThreshold (1.25) so the ghost reads clearly against dark regolith
// without blowing out the bloom pass.
Shader "MoonProject/Art/LofiHologram"
{
    Properties
    {
        [MainColor] _BaseColor("Cyan Body (sRGB)", Color) = (0.372549, 0.952941, 1.0, 1.0)
        _RimColor("Warm Amber Rim (sRGB)", Color) = (1.0, 0.709804, 0.278431, 1.0)
        _InvalidColor("Invalid Tint (sRGB)", Color) = (1.0, 0.352941, 0.431373, 1.0)
        _BodyIntensity("Body Linear Intensity", Range(0.05, 1.0)) = 0.52
        _RimIntensity("Rim Linear Intensity", Range(0.05, 1.15)) = 0.92
        _RimPower("Fresnel Power", Range(0.5, 6.0)) = 2.1
        _ScanDensity("Scan Line Density (lines/m)", Range(4.0, 60.0)) = 24.0
        _ScanSpeed("Scan Shimmer Speed", Range(0.0, 4.0)) = 0.85
        _ScanContrast("Scan Shimmer Contrast", Range(0.0, 0.6)) = 0.24
        _Alpha("Base Alpha", Range(0.05, 0.95)) = 0.44
        _RimAlpha("Rim Alpha", Range(0.1, 1.0)) = 0.84
        _Invalid("Invalid Placement (0..1)", Range(0.0, 1.0)) = 0.0
        _StageFade("Stage Fade (0..1)", Range(0.0, 1.0)) = 1.0
        _BloomCeiling("Bloom Safe Ceiling", Range(0.5, 1.24)) = 1.10
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
            Name "Hologram"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half4 _InvalidColor;
                half _BodyIntensity;
                half _RimIntensity;
                half _RimPower;
                half _ScanDensity;
                half _ScanSpeed;
                half _ScanContrast;
                half _Alpha;
                half _RimAlpha;
                half _Invalid;
                half _StageFade;
                half _BloomCeiling;
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
                float3 normalWS : TEXCOORD1;
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
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float normLenSq = dot(input.normalWS, input.normalWS);
                float3 normal = normLenSq > 1e-6 ? input.normalWS * rsqrt(normLenSq) : float3(0.0, 1.0, 0.0);
                float3 viewRaw = GetWorldSpaceViewDir(input.positionWS);
                float viewLenSq = dot(viewRaw, viewRaw);
                float3 view = viewLenSq > 1e-6 ? viewRaw * rsqrt(viewLenSq) : float3(0.0, 0.0, 1.0);
                half ndotv = clamp((half)dot(normal, view), 0.0h, 0.999h);
                half fresnel = PositivePow(max(1.0h - ndotv, 0.001h), max(_RimPower, 0.5h));

                float scanPhase = input.positionWS.y * _ScanDensity - _Time.y * _ScanSpeed;
                half scanWave = 0.5h + 0.5h * (half)sin(scanPhase * 6.2831853);
                half scanBand = smoothstep(0.2h, 0.9h, scanWave);
                half shimmer = lerp(1.0h - _ScanContrast, 1.0h + _ScanContrast, scanBand);

                half invalid = saturate(_Invalid);
                half3 bodyValid = _BaseColor.rgb * (_BodyIntensity * shimmer);
                half3 rimValid = _RimColor.rgb * _RimIntensity;
                half3 validRgb = lerp(bodyValid, rimValid, fresnel * 0.85h);

                half3 bodyInvalid = _InvalidColor.rgb * (_BodyIntensity * 1.15h * shimmer);
                half3 rimInvalid = lerp(_InvalidColor.rgb, _RimColor.rgb, 0.22h) * (_RimIntensity * 1.05h);
                half3 invalidRgb = lerp(bodyInvalid, rimInvalid, fresnel * 0.85h);

                half3 color = lerp(validRgb, invalidRgb, invalid);
                if (any(isnan(color)) || any(isinf(color)))
                {
                    color = _BaseColor.rgb * _BodyIntensity;
                }

                color = max(color, 0.0h);
                half peak = max(color.r, max(color.g, color.b));
                if (peak > _BloomCeiling)
                {
                    color *= (_BloomCeiling / max(peak, 0.001h));
                }

                half alpha = lerp(_Alpha, _RimAlpha, fresnel) * saturate(0.84h + 0.16h * scanBand) * saturate(_StageFade);
                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
