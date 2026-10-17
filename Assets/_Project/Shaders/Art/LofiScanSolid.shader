// A scannable fragment's solid (0.5.5 owner feedback §1, D2): LofiLit's look and light, plus the scan driven per
// renderer with a MaterialPropertyBlock. A cyan sweep climbs the object as _ScanProgress rises; what it has passed pales
// toward Kenji's white paper so the line drawing reads over it. _Dissolve's first third dithers the solid away (its
// shadow and depth with it), and _Fade does the same for the dust outline left behind.
Shader "MoonProject/Art/LofiScanSolid"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _SpecColor("Specular Color", Color) = (0, 0, 0, 0)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _Surface("Surface", Float) = 0.0
        _MoonRim("Rim (brightness, exponent, up cutoff, horizon lean)", Vector) = (0, 3, 0.6, 0.6)
        _MoonAoTint("Foot occlusion tint", Color) = (0.3, 0.3, 0.4, 1)
        [HDR] _SweepColor("Sweep (the scan's line, linear)", Color) = (0.06, 0.47, 0.52, 1)
        _DrawnColor("Drawn (paper the scanned part pales toward)", Color) = (0.86, 0.88, 0.92, 1)
        _SweepWidth("Sweep width (share of the height)", Range(0.005, 0.2)) = 0.022
        _ScanProgress("Scan progress", Range(0, 1)) = 0
        _Dissolve("Dissolve", Range(0, 1)) = 0
        _Fade("Fade", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "LofiScanSolidInput.hlsl"
            #include "LofiLitCore.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 occlusion : TEXCOORD1;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 fogFactorAndVertexLight : TEXCOORD3;
                half occlusion : TEXCOORD4;
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
                half3 normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normalWS;
                output.occlusion = (half)input.occlusion.x;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                half3 vertexLight = half3(0, 0, 0);
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                vertexLight = VertexLighting(positions.positionWS, normalWS);
                #endif
                output.fogFactorAndVertexLight = half4(ComputeFogFactor(positions.positionCS.z), vertexLight);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                clip(MoonScanDither(input.positionCS.xy) - MoonScanSolidFade(_Dissolve, _Fade));

                float height = MoonScanHeight(input.positionWS);
                half activeScan = step(0.001, _ScanProgress);
                half drawn = step(height, _ScanProgress) * activeScan;
                half sweep = (1.0 - smoothstep(0.0, _SweepWidth, abs(height - _ScanProgress)))
                    * activeScan * step(_ScanProgress, 0.999);

                // Fine horizontal 1980s instrument phosphor raster lines (cyan + warm amber) building up with progress
                float worldRow = input.positionWS.y * 95.0;
                float screenRow = input.positionCS.y * 0.42;
                half rasterWave = 0.5 + 0.5 * sin(worldRow * 6.2831853);
                half screenWave = 0.5 + 0.5 * sin(screenRow * 6.2831853);
                half rasterMask = smoothstep(0.26, 0.80, lerp(rasterWave, screenWave, 0.55));
                half amberBand = smoothstep(0.25, 0.75, 0.5 + 0.5 * sin(input.positionWS.y * 24.0 * 6.2831853));
                half3 amberPhosphor = half3(0.82, 0.52, 0.14);
                half3 rasterColor = lerp(_SweepColor.rgb, amberPhosphor, amberBand * 0.55);
                half rasterBuild = drawn * rasterMask * lerp(0.48, 0.88, saturate(_ScanProgress));

                // Faint edge outline that builds with progress and completes at 100%, plus completion ping
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half ndotv = saturate(dot(normalize(input.normalWS), viewDirWS));
                half rimEdge = pow(1.0 - ndotv, 2.4);
                half outlineWeight = rimEdge * activeScan * lerp(0.28 * drawn, 0.72, saturate(_ScanProgress * _ScanProgress));
                half ping = smoothstep(0.0, 0.04, _Dissolve) * (1.0 - smoothstep(0.06, 0.26, _Dissolve));
                half3 outlineEmission = lerp(_SweepColor.rgb, half3(0.84, 0.70, 0.34), ping * 0.55)
                    * (outlineWeight + rimEdge * ping * 0.85);

                LofiSurface surface;
                surface.positionWS = input.positionWS;
                surface.normalWS = input.normalWS;
                surface.positionCS = input.positionCS;
                surface.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                surface.albedo = lerp(surface.albedo, _MoonAoTint.rgb, input.occlusion);
                surface.albedo = lerp(surface.albedo, surface.albedo * 0.36, drawn * (0.62 - 0.42 * rasterMask));
                surface.albedo = lerp(surface.albedo, _DrawnColor.rgb, drawn * 0.28 * rasterMask) * (1.0 - 0.35 * sweep);
                half3 scanEmission = _SweepColor.rgb * sweep + rasterColor * rasterBuild + outlineEmission;
                scanEmission = min(scanEmission, half3(0.88, 0.88, 0.88));
                surface.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb
                    * _EmissionColor.rgb + scanEmission;
                surface.vertexLight = input.fogFactorAndVertexLight.yzw;
                surface.fogFactor = input.fogFactorAndVertexLight.x;
                return half4(LofiLit(surface), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ScanShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "LofiScanSolidInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            half4 ScanShadowFragment(Varyings input) : SV_TARGET
            {
                clip(MoonScanDither(input.positionCS.xy) - MoonScanSolidFade(_Dissolve, _Fade));
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment ScanDepthFragment
            #pragma multi_compile_instancing

            #include "LofiScanSolidInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            half ScanDepthFragment(Varyings input) : SV_TARGET
            {
                clip(MoonScanDither(input.positionCS.xy) - MoonScanSolidFade(_Dissolve, _Fade));
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
