#ifndef MOONPROJECT_R1_LIT_CORE_INCLUDED
#define MOONPROJECT_R1_LIT_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "R1LitInput.hlsl"
#include "../World/LofiAtmosphere.hlsl"

struct R1LitAttributes
{
    float4 positionOS       : POSITION;
    float3 normalOS         : NORMAL;
    float4 tangentOS        : TANGENT;
    float2 texcoord         : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    half4  color            : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct R1LitVaryings
{
    float4 positionCS               : SV_POSITION;
    float2 uv                       : TEXCOORD0;
    float3 positionWS               : TEXCOORD1;
    float3 normalWS                 : TEXCOORD2;
    float4 tangentWS                : TEXCOORD3;
    half   fogFactor                : TEXCOORD4;
    half4  vertexColor              : COLOR;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float R1Hash31(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.x + p.y) * p.z);
}

float R1ValueNoise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);

    float n000 = R1Hash31(i + float3(0.0, 0.0, 0.0));
    float n100 = R1Hash31(i + float3(1.0, 0.0, 0.0));
    float n010 = R1Hash31(i + float3(0.0, 1.0, 0.0));
    float n110 = R1Hash31(i + float3(1.0, 1.0, 0.0));
    float n001 = R1Hash31(i + float3(0.0, 0.0, 1.0));
    float n101 = R1Hash31(i + float3(1.0, 0.0, 1.0));
    float n011 = R1Hash31(i + float3(0.0, 1.0, 1.0));
    float n111 = R1Hash31(i + float3(1.0, 1.0, 1.0));

    float nx00 = lerp(n000, n100, u.x);
    float nx10 = lerp(n010, n110, u.x);
    float nx01 = lerp(n001, n101, u.x);
    float nx11 = lerp(n011, n111, u.x);

    float nxy0 = lerp(nx00, nx10, u.y);
    float nxy1 = lerp(nx01, nx11, u.y);

    return lerp(nxy0, nxy1, u.z);
}

float R1Fbm3D(float3 p)
{
    float v = 0.0;
    v += 0.60 * R1ValueNoise3D(p);
    v += 0.28 * R1ValueNoise3D(p * 2.13 + 11.7);
    v += 0.12 * R1ValueNoise3D(p * 4.37 + 29.3);
    return v;
}

R1LitVaryings R1LitVert(R1LitAttributes input)
{
    R1LitVaryings output = (R1LitVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = posInputs.positionCS;
    output.positionWS = posInputs.positionWS;
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.normalWS = normInputs.normalWS;
    output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
    output.vertexColor = input.color;

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS, output.vertexSH);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(posInputs);
#endif

    return output;
}

half3 R1EvaluateBloomSafeEmission(float2 uv)
{
    half3 rawTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb;
    half maskLuma = max(rawTex.r, max(rawTex.g, rawTex.b));
    half3 tint = _EmissionColor.rgb * _EmissionStrength;
    half3 rawEmission = rawTex * tint;

    half maxComp = max(rawEmission.r, max(rawEmission.g, rawEmission.b));
    half3 bodyEmission = rawEmission;
    if (maxComp > _BloomSafeMax)
    {
        bodyEmission *= (_BloomSafeMax / max(maxComp, 1e-4h));
    }

    half coreFactor = saturate((maskLuma - _HotCoreThreshold) / max(1.0h - _HotCoreThreshold, 1e-3h));
    coreFactor = coreFactor * coreFactor;
    half3 hotCoreColor = lerp(tint, half3(1.0h, 0.94h, 0.80h), 0.35h) * _HotCorePeak;
    return lerp(bodyEmission, hotCoreColor, coreFactor * saturate(_EmissionStrength));
}

half4 R1LitFrag(R1LitVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.uv;

    half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    half3 albedo = baseSample.rgb * _BaseColor.rgb;
    half alpha = baseSample.a * _BaseColor.a;

    // Per-material MaskMap: R = Metallic, G = Micro-AO, B = Micro-Detail, A = Smoothness.
    // Per-model Vertex Colour: R = Ambient Occlusion (1=open), G = Convex Edge Wear (0=none), B = Crease/Cavity (0=none).
    half4 maskSample = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
    half4 vcol = saturate(input.vertexColor);
    half metallic = saturate(maskSample.r * _MetallicScale);
    half microAo = lerp(1.0h, maskSample.g, 0.35h);
    half occlusion = lerp(1.0h, saturate(vcol.r * microAo), _OcclusionStrength);
    half microDetail = maskSample.b;
    half smoothness = lerp(_SmoothnessMin, _SmoothnessMax, saturate(maskSample.a * _SmoothnessScale));

    // Normal mapping in world space
    half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
    half3 normalTS = UnpackNormalScale(packedNormal, _BumpScale);

    float3 normalWS = normalize(input.normalWS);
    float3 tangentWS = normalize(input.tangentWS.xyz);
    float3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);
    float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);
    float3 finalNormalWS = normalize(mul(normalTS, tangentToWorld));

    // 1. Subtle large-scale world-space colour variation + subtle cavity/AO grounding
    float worldNoise = R1Fbm3D(input.positionWS * _ColorVarScale);
    half colorShift = (half)((worldNoise - 0.5) * 2.0) * _ColorVarAmount;
    albedo = saturate(albedo * (1.0h + colorShift));
    albedo *= lerp(1.0h, occlusion, 0.28h);

    // 2. Procedural edge wear from vertex colour G (0 = none, 1 = convex edge) broken up with 3D noise + micro grain
    float edgeNoise = R1Fbm3D(input.positionWS * 6.5 + 3.1);
    half microEdge = lerp(0.85h, 1.15h, microDetail);
    half convexEdge = saturate((vcol.g - _EdgeWearThreshold) / max(1.0h - _EdgeWearThreshold, 1e-3h));
    half noisyEdge = saturate(convexEdge * (0.72h + 0.56h * (half)edgeNoise) * microEdge);
    half edgeMask = saturate(pow(max(noisyEdge, 0.0h), _EdgeWearSharpness) * _EdgeWearAmount);

    albedo = lerp(albedo, _EdgeWearColor.rgb, edgeMask);
    metallic = lerp(metallic, _EdgeWearMetallic, edgeMask);
    smoothness = lerp(smoothness, _EdgeWearSmoothness, edgeMask);

    // 3. Procedural lunar dust layer from world-up (normalWS.y) + vertex colour B (creases/cavities) and vertex AO (1 - vcol.r)
    float dustNoise = R1Fbm3D(input.positionWS * 3.8 + 17.4);
    half upFactor = saturate(((half)finalNormalWS.y - _DustUpThreshold) / max(1.0h - _DustUpThreshold, 1e-3h));
    half cavityFactor = saturate(vcol.b + (1.0h - vcol.r) * 0.65h);
    half rawDust = saturate(lerp(upFactor, cavityFactor, _DustOcclusionWeight) * (0.75h + 0.50h * (half)dustNoise));
    half dustMask = saturate(pow(max(rawDust, 0.0h), _DustSharpness) * _DustAmount);
    // Reduce dust on worn high-convex edges so bevel highlights stay crisp
    dustMask *= (1.0h - edgeMask * 0.75h);

    albedo = lerp(albedo, _DustColor.rgb, dustMask);
    metallic = lerp(metallic, 0.0h, dustMask * 0.85h);
    smoothness = lerp(smoothness, _DustSmoothness, dustMask);

    // 4. Bloom-safe emission with small hot core
    half3 emission = R1EvaluateBloomSafeEmission(uv);

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = finalNormalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, finalNormalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo;
    surfaceData.metallic = metallic;
    surfaceData.specular = half3(0, 0, 0);
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = normalTS;
    surfaceData.emission = emission;
    surfaceData.occlusion = occlusion;
    surfaceData.alpha = alpha;
    surfaceData.clearCoatMask = 0.0h;
    surfaceData.clearCoatSmoothness = 0.0h;

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MoonAtmosphere(color.rgb, input.positionWS, input.fogFactor);
    return color;
}

#endif
