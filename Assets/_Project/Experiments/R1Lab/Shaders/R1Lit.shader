Shader "MoonProject/R1/Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo (RGB)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0.0, 2.5)) = 1.0

        [NoScaleOffset] _MaskMap("Mask (R=Metal, G=AO, B=Curv, A=Smooth)", 2D) = "white" {}
        _Metallic("Metallic Multiplier", Range(0.0, 1.5)) = 1.0
        _Smoothness("Smoothness Multiplier", Range(0.0, 1.5)) = 1.0
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [NoScaleOffset] _BakeMap("Baked Map (R=Curv, G=AO, B=Cavity)", 2D) = "gray" {}
        _UseUV1ForBake("Use UV1 For BakeMap", Float) = 1.0

        [Header(Procedural Edge Wear)]
        _EdgeWearColor("Edge Wear Color", Color) = (0.64, 0.65, 0.69, 1.0)
        _EdgeWearStrength("Edge Wear Strength", Range(0.0, 2.0)) = 0.85
        _EdgeWearThreshold("Edge Wear Curvature Threshold", Range(0.5, 0.9)) = 0.56
        _EdgeWearMetallic("Edge Wear Metallic", Range(0.0, 1.0)) = 0.78
        _EdgeWearSmoothness("Edge Wear Smoothness", Range(0.0, 1.0)) = 0.62

        [Header(Procedural Lunar Dust)]
        _DustColor("Dust Color", Color) = (0.58, 0.59, 0.62, 1.0)
        _DustStrength("Dust Strength", Range(0.0, 1.5)) = 0.65
        _DustUpThreshold("Dust World-Up Threshold", Range(-0.2, 0.9)) = 0.25
        _DustCreaseWeight("Dust Crease & Occlusion Weight", Range(0.0, 2.0)) = 0.85
        _DustSmoothness("Dust Smoothness", Range(0.05, 0.35)) = 0.14
        _DustGroundHeight("Dust Ground Skirt Height (Y)", Float) = 0.22
        _DustGroundFade("Dust Ground Skirt Fade", Range(0.05, 1.5)) = 0.35
        _DustGroundStrength("Dust Ground Skirt Strength", Range(0.0, 1.0)) = 0.55

        [Header(Large Scale Color Variation)]
        _ColorVarScale("Color Variation World Scale", Range(0.05, 2.0)) = 0.45
        _ColorVarStrength("Color Variation Strength", Range(0.0, 0.25)) = 0.06

        [Header(Bloom Safe Emission)]
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionStrength("Emission Strength", Range(0.0, 3.0)) = 1.0
        _BloomSafeMax("Bloom Safe Body Max", Range(0.5, 1.24)) = 1.05
        _HotCoreThreshold("Hot Core Threshold", Range(0.5, 0.99)) = 0.86
        _HotCorePeak("Hot Core Peak", Range(1.0, 2.0)) = 1.42
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex R1LitVert
            #pragma fragment R1LitFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/_Project/Shaders/World/LofiAtmosphere.hlsl"

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);     SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MaskMap);     SAMPLER(sampler_MaskMap);
            TEXTURE2D(_BakeMap);     SAMPLER(sampler_BakeMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _Metallic;
                half _Smoothness;
                half _OcclusionStrength;
                half _UseUV1ForBake;

                half4 _EdgeWearColor;
                half _EdgeWearStrength;
                half _EdgeWearThreshold;
                half _EdgeWearMetallic;
                half _EdgeWearSmoothness;

                half4 _DustColor;
                half _DustStrength;
                half _DustUpThreshold;
                half _DustCreaseWeight;
                half _DustSmoothness;
                float _DustGroundHeight;
                float _DustGroundFade;
                half _DustGroundStrength;

                float _ColorVarScale;
                half _ColorVarStrength;

                half4 _EmissionColor;
                half _EmissionStrength;
                half _BloomSafeMax;
                half _HotCoreThreshold;
                half _HotCorePeak;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                half4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS    : TEXCOORD1;
                half4 tangentWS   : TEXCOORD2;
                float4 uv01       : TEXCOORD3;
                half4 vertexColor : TEXCOORD4;
                half fogFactor    : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0, 0, 0));
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            float Fbm3D(float3 p)
            {
                float v = 0.0;
                v += 0.55 * ValueNoise3D(p);
                v += 0.30 * ValueNoise3D(p * 2.13 + 11.7);
                v += 0.15 * ValueNoise3D(p * 4.41 + 29.3);
                return v;
            }

            Varyings R1LitVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = half4(normInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv01.xy = TRANSFORM_TEX(input.uv0, _BaseMap);
                output.uv01.zw = input.uv1;
                output.vertexColor = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 R1LitFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv0 = input.uv01.xy;
                float2 uvBake = (_UseUV1ForBake > 0.5h) ? input.uv01.zw : uv0;

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv0);
                half3 albedo = baseSample.rgb * _BaseColor.rgb;

                // MaskMap: R=Metallic, G=DetailAO, B=DetailCurvature, A=Smoothness
                half4 maskSample = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv0);
                half metallic = saturate(maskSample.r * _Metallic);
                half detailAO = maskSample.g;
                half detailCurv = maskSample.b;
                half smoothness = saturate(maskSample.a * _Smoothness);

                // BakeMap: R=MeshCurvature (0.5 flat, >0.5 convex, <0.5 concave), G=MeshAO, B=Cavity
                half4 bakeSample = SAMPLE_TEXTURE2D(_BakeMap, sampler_BakeMap, uvBake);
                half meshCurv = bakeSample.r;
                half meshAO = bakeSample.g;
                half cavity = bakeSample.b;

                // Combine curvature and AO
                half combinedCurv = saturate(meshCurv + (detailCurv - 0.5h) * 0.45h);
                half combinedAO = lerp(1.0h, saturate(meshAO * detailAO), _OcclusionStrength);

                // World-space noise for organic breakup and large-scale variation
                float3 posWS = input.positionWS;
                half macroNoise = (half)ValueNoise3D(posWS * _ColorVarScale);
                half breakupNoise = (half)Fbm3D(posWS * 6.5);
                half fineBreakup = (half)ValueNoise3D(posWS * 18.0);

                // 1. Subtle large-scale colour variation
                half colorShift = (macroNoise - 0.5h) * 2.0h * _ColorVarStrength;
                albedo = saturate(albedo * (1.0h + colorShift));

                // 2. Normal mapping
                half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv0);
                half3 normalTS = UnpackNormalScale(packedNormal, _BumpScale);

                half3 nWS = normalize(input.normalWS);
                half3 tWS = normalize(input.tangentWS.xyz);
                half3 bWS = cross(nWS, tWS) * input.tangentWS.w;
                half3x3 tbn = half3x3(tWS, bWS, nWS);

                // 3. Procedural Edge Wear from baked curvature
                half rawConvex = saturate((combinedCurv - _EdgeWearThreshold) / max(1.0h - _EdgeWearThreshold, 0.001h));
                half wearNoiseMod = saturate(breakupNoise * 0.75h + fineBreakup * 0.45h - 0.15h);
                half edgeWear = saturate(rawConvex * (0.45h + 0.85h * wearNoiseMod) * _EdgeWearStrength);

                albedo = lerp(albedo, _EdgeWearColor.rgb, edgeWear);
                metallic = lerp(metallic, _EdgeWearMetallic, edgeWear);
                smoothness = lerp(smoothness, _EdgeWearSmoothness, edgeWear);

                // 4. Procedural Lunar Dust from world-up, creases/occlusion, and ground contact skirt
                half3 mappedNormalWS = normalize(mul(normalTS, tbn));
                half upFactor = saturate((mappedNormalWS.y - _DustUpThreshold) / max(1.0h - _DustUpThreshold, 0.001h));
                upFactor = upFactor * upFactor * (3.0h - 2.0h * upFactor);

                half concaveCrease = saturate((0.5h - combinedCurv) * 2.4h);
                half occlusionCrease = saturate((1.0h - combinedAO) * 1.35h + (1.0h - cavity) * 0.5h);
                half creaseFactor = saturate(max(concaveCrease, occlusionCrease) * _DustCreaseWeight);

                // Ground contact dust skirt (settles around the base of objects resting in regolith)
                half groundSkirt = saturate((_DustGroundHeight - (half)posWS.y) / max((half)_DustGroundFade, 0.01h));
                groundSkirt = groundSkirt * groundSkirt * _DustGroundStrength * saturate(mappedNormalWS.y + 0.65h);

                half dustNoise = saturate(0.65h + (breakupNoise - 0.5h) * 0.7h);
                half dustMask = saturate((upFactor * 0.72h + creaseFactor * 0.65h + groundSkirt) * dustNoise * _DustStrength);

                albedo = lerp(albedo, _DustColor.rgb * (0.94h + 0.12h * fineBreakup), dustMask);
                metallic = lerp(metallic, 0.0h, dustMask * 0.92h);
                smoothness = lerp(smoothness, _DustSmoothness, dustMask);

                // Slightly soften surface normal under settled dust
                normalTS.xy *= (1.0h - dustMask * 0.45h);
                normalTS = normalize(normalTS);
                half3 finalNormalWS = normalize(mul(normalTS, tbn));

                // 5. Bloom-safe Emission (warm lamps #FFB547 & cyan seam #5FF3FF)
                half3 rawEmission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv0).rgb * _EmissionColor.rgb * _EmissionStrength;
                half emLum = max(rawEmission.r, max(rawEmission.g, rawEmission.b));
                half3 safeEmission = rawEmission;
                if (emLum > 0.0001h)
                {
                    half3 emDir = rawEmission / emLum;
                    half bodyLum = min(emLum, _BloomSafeMax);
                    half hotT = saturate((emLum - _HotCoreThreshold) / max(1.0h - _HotCoreThreshold, 0.001h));
                    hotT = hotT * hotT;
                    half finalLum = lerp(bodyLum, _HotCorePeak, hotT);
                    safeEmission = emDir * finalLum;
                }

                // 6. URP PBR Evaluation
                InputData inputData = (InputData)0;
                inputData.positionWS = posWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = finalNormalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(posWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(posWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(finalNormalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = safeEmission;
                surfaceData.occlusion = combinedAO;
                surfaceData.alpha = 1.0h;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MoonAtmosphere(color.rgb, posWS, input.fogFactor);
                return half4(color.rgb, 1.0h);
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

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
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

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS    : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0h);
            }
            ENDHLSL
        }
    }
}
