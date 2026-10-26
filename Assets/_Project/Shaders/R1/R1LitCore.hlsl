#ifndef MOONPROJECT_R1_LIT_CORE_INCLUDED
#define MOONPROJECT_R1_LIT_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "R1LitInput.hlsl"
#include "../World/LofiAtmosphere.hlsl"
#include "../Art/LofiScanInput.hlsl"

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

half3 R1ThermalGlow(half heat)
{
    half h = saturate(heat);
    half3 charColor   = half3(0.06h, 0.04h, 0.03h);
    half3 redColor    = half3(0.62h, 0.09h, 0.02h);
    half3 orangeColor = half3(1.18h, 0.44h, 0.06h);
    half3 whiteColor  = half3(1.58h, 1.44h, 1.08h);

    half3 c = lerp(charColor, redColor, saturate(h / 0.32h));
    c = lerp(c, orangeColor, saturate((h - 0.32h) / 0.38h));
    c = lerp(c, whiteColor, saturate((h - 0.70h) / 0.30h));
    return c;
}

void R1ApplyBuildEffect(
    inout half3 color,
    float3 positionWS,
    float3 normalWS,
    half edgeCurvature)
{
    if (_BuildPrint < 0.5h && _BuildStageHeat <= 0.001h)
    {
        return;
    }

    float normGrad = length(fwidth(normalWS));
    half bevelLine = (half)smoothstep(0.09, 0.32, normGrad);
    half convexEdge = saturate((edgeCurvature - 0.25h) * 2.2h);
    half featureLine = saturate(max(bevelLine, convexEdge * 0.85h));

    float3 seamPos = _BuildSeamPointWS.xyz;
    float seamRadius = max(_BuildSeamPointWS.w, 0.15);
    float distToSeam = length(positionWS - seamPos);
    half seamProximity = (half)saturate(1.0 - distToSeam / seamRadius);

    // Printed in place: layered build-up from the ground slice by slice, with a bright rising layer line and a
    // cooling terrace band below.
    if (_BuildPrint > 0.5h)
    {
        float minY = _BuildBoundsY.x;
        float spanY = max(_BuildBoundsY.y - minY, 0.05);
        float h = (positionWS.y - minY) / spanY;
        float rawProgress = (float)_BuildRevealProgress;

        // Quantize into subtle additive layers (40 slices) with smooth sub-slice rise
        float sliceCount = 40.0;
        float sliceIdx = floor(rawProgress * sliceCount);
        float sliceFrac = smoothstep(0.0, 1.0, frac(rawProgress * sliceCount));
        float printFrontH = (sliceIdx + sliceFrac) / sliceCount;

        if (h > printFrontH + 0.004)
        {
            discard;
        }

        float distBelow = max(0.0, printFrontH - h);
        float layerBand = 0.13;
        if (distBelow < layerBand && rawProgress < 0.998)
        {
            // Razor-sharp incandescent layer line at the print plane + cooling horizontal layer terraces below
            float lineCore = 1.0 - smoothstep(0.0, 0.022, distBelow);
            float coolBand = 1.0 - smoothstep(0.015, layerBand, distBelow);
            float layerStriation = 0.78 + 0.22 * sin(positionWS.y * 220.0);
            half layerHeat = (half)saturate(max(lineCore, coolBand * 0.72 * layerStriation) + seamProximity * 0.22 * coolBand);
            half3 glow = R1ThermalGlow(layerHeat);
            color = lerp(color, glow, saturate(layerHeat * 0.94h)) + glow * (half)(lineCore * 0.42);
        }
    }

    // The seam's residual heat: a bright weld core at the seam point cooling white -> orange -> red -> dark along
    // the stage bevels.
    if (_BuildStageHeat > 0.001h)
    {
        float seamYDist = abs(positionWS.y - _BuildBoundsY.z);
        half seamBand = (half)saturate(1.0 - seamYDist / 0.16);
        half localHeat = saturate(
            seamProximity * _BuildStageHeat * 1.15h +
            max(featureLine, seamBand * 0.65h) * _BuildStageHeat * 0.75h);
        if (localHeat > 0.02h)
        {
            half3 thermal = R1ThermalGlow(localHeat);
            color = lerp(color, thermal, saturate(localHeat * 0.85h)) + thermal * (localHeat * localHeat * 0.30h);
        }
    }
}

// A soft band of light travelling inside a cable's rubber (Gameplay's power pulse sets the block per renderer): uv0.x
// is metres along the lead, vertex alpha masks it to the lead. The tube's middle, where the light crosses the most
// rubber towards the eye, glows brighter than its rim, so it reads as lit from within rather than painted on.
half3 R1EvaluateCablePulse(float uvX, half leadMask, half3 normalWS, half3 viewDirWS)
{
    half peak = max(_PulseColor.r, max(_PulseColor.g, _PulseColor.b));
    if (peak < 1e-4h || leadMask < 1e-3h)
    {
        return half3(0.0h, 0.0h, 0.0h);
    }

    float along = (uvX - _BaseMap_ST.z) / max(abs(_BaseMap_ST.x), 1e-4);
    float offset = (along - _PulseAlong) / max((float)_PulseWidth, 1e-3);
    half band = (half)exp(-offset * offset * 2.0);
    half facing = saturate(dot(normalWS, viewDirWS));
    half core = lerp(0.35h, 1.0h, facing * facing);
    half3 glow = _PulseColor.rgb * (band * core * leadMask);
    half glowMax = max(glow.r, max(glow.g, glow.b));
    return glowMax > _BloomSafeMax ? glow * (_BloomSafeMax / glowMax) : glow;
}

half4 R1LitFrag(R1LitVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    R1DistanceFadeClip(input.positionWS, input.positionCS);

    float normLenSq = dot(input.normalWS, input.normalWS);
    float3 normalWS = normLenSq > 1e-6 ? input.normalWS * rsqrt(normLenSq) : float3(0.0, 1.0, 0.0);

    float2 uv = input.uv;
    half4 baseSample;
    half4 maskSample;
    half3 normalTS;
    float3 finalNormalWS = normalWS;

    if (_TriplanarMapping > 0.5h)
    {
        float3 triW = pow(max(abs(normalWS), 1e-4), 4.0);
        triW /= max(triW.x + triW.y + triW.z, 1e-4);

        float3 wpWarp = float3(
            R1ValueNoise3D(input.positionWS * 0.16 + float3(3.7, 11.2, 7.9)) - 0.5,
            R1ValueNoise3D(input.positionWS * 0.16 + float3(19.3, 5.1, 23.4)) - 0.5,
            R1ValueNoise3D(input.positionWS * 0.16 + float3(13.1, 29.7, 2.8)) - 0.5) * 0.38;
        float3 triCoord = (input.positionWS + wpWarp) * max((float)_TriplanarScale, 0.05);

        half4 baseX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, triCoord.zy);
        half4 baseY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, triCoord.xz);
        half4 baseZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, triCoord.xy);
        baseSample = baseX * (half)triW.x + baseY * (half)triW.y + baseZ * (half)triW.z;

        half4 maskX = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, triCoord.zy);
        half4 maskY = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, triCoord.xz);
        half4 maskZ = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, triCoord.xy);
        maskSample = maskX * (half)triW.x + maskY * (half)triW.y + maskZ * (half)triW.z;

        half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, triCoord.zy), _BumpScale);
        half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, triCoord.xz), _BumpScale);
        half3 nZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, triCoord.xy), _BumpScale);
        normalTS = nX * (half)triW.x + nY * (half)triW.y + nZ * (half)triW.z;

        float3 worldPerturb =
            float3(0.0, nX.y, nX.x) * triW.x +
            float3(nY.x, 0.0, nY.y) * triW.y +
            float3(nZ.x, nZ.y, 0.0) * triW.z;
        worldPerturb -= normalWS * dot(worldPerturb, normalWS);
        finalNormalWS = normalize(normalWS + worldPerturb);
    }
    else
    {
        baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
        maskSample = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
        half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
        normalTS = UnpackNormalScale(packedNormal, _BumpScale);

        float tanLenSq = dot(input.tangentWS.xyz, input.tangentWS.xyz);
        if (tanLenSq > 1e-6)
        {
            float3 tangentWS = input.tangentWS.xyz * rsqrt(tanLenSq);
            float3 bitanRaw = cross(normalWS, tangentWS) * input.tangentWS.w;
            float bitanLenSq = dot(bitanRaw, bitanRaw);
            if (bitanLenSq > 1e-6)
            {
                float3 bitangentWS = bitanRaw * rsqrt(bitanLenSq);
                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);
                float3 mapped = mul(normalTS, tangentToWorld);
                float mappedLenSq = dot(mapped, mapped);
                if (mappedLenSq > 1e-6)
                {
                    finalNormalWS = mapped * rsqrt(mappedLenSq);
                }
            }
        }
    }

    half3 albedo = baseSample.rgb * _BaseColor.rgb;
    half alpha = baseSample.a * _BaseColor.a;

    // Per-material MaskMap: R = Metallic, G = Micro-AO, B = Micro-Detail, A = Smoothness.
    // Per-model Vertex Colour: R = Ambient Occlusion (1=open), G = Convex Edge Wear (0=none), B = Crease/Cavity (0=none).
    half4 vcol = saturate(input.vertexColor);
    half metallic = saturate(maskSample.r * _MetallicScale);
    half microAo = lerp(1.0h, maskSample.g, 0.35h);
    half occlusion = lerp(1.0h, saturate(vcol.r * microAo), _OcclusionStrength);
    half microDetail = maskSample.b;
    half smoothness = lerp(_SmoothnessMin, _SmoothnessMax, saturate(maskSample.a * _SmoothnessScale));

    // Optional steepest-part fresh-rock darkening for crater-rim slope and talus materials
    if (_SteepRockDarkening > 0.001h)
    {
        float steepNoise = R1Fbm3D(input.positionWS * 0.24 + float3(11.3, 27.1, 5.9));
        half steepSlope = 1.0h - (half)smoothstep(0.22, 0.68, normalWS.y);
        half steepPatch = saturate(steepSlope * (0.45h + 0.55h * (half)smoothstep(0.28, 0.72, steepNoise)));
        albedo *= lerp(1.0h, 1.0h - _SteepRockDarkening, steepPatch);
    }

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

    // 4. Bloom-safe emission with small hot core + optional Build Mode Glow (_BuildModeGlow)
    half3 emission = R1EvaluateBloomSafeEmission(uv);
    if (_BuildModeGlow > 1e-4h)
    {
        half3 buildGlow = _EmissionColor.rgb * (_BuildModeGlow * 0.55h);
        emission = max(emission, min(buildGlow, half3(_BloomSafeMax, _BloomSafeMax, _BloomSafeMax)));
    }

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = finalNormalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    emission += R1EvaluateCablePulse(uv.x, vcol.a, (half3)normalWS, (half3)inputData.viewDirectionWS);
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
    R1ApplyBuildEffect(color.rgb, input.positionWS, finalNormalWS, vcol.g);
    MoonApplySurfaceScan(color.rgb, input.positionWS, finalNormalWS, vcol.g, _ScanProgress, _Dissolve);
    color.rgb = MoonAtmosphere(color.rgb, input.positionWS, input.fogFactor);
    if (any(isnan(color.rgb)) || any(isinf(color.rgb)))
    {
        color.rgb = half3(0.0h, 0.0h, 0.0h);
    }

    return color;
}

#endif
