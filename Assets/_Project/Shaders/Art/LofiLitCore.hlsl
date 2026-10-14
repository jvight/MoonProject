// How the Lofi lit shaders light a flat palette face (M3-18 V2): URP's Blinn-Phong path with no specular (ambient,
// SSAO, shadows, additional lights), a thin cool rim on the silhouette, then the basin's air (MoonAtmosphere). The
// rim leans from the earthlight's hue toward the sky's horizon glow and fades out on up-facing faces, so tops stay
// the earthlight's and only edges against the dark ground catch it. Include after LofiLitInput.hlsl.
#ifndef MOON_LOFI_LIT_CORE_INCLUDED
#define MOON_LOFI_LIT_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "../World/LofiAtmosphere.hlsl"

// World's sky (SkyShaderGlobals): rgb is the horizon glow (linear), a its height.
float4 _MoonHorizonGlow;
// Soft low-poly look variant depth toggle (0 in V0/V1, 1 in V2/V3).
float _MoonLookDepth;

struct LofiSurface
{
    float3 positionWS;
    half3 normalWS;
    float4 positionCS;
    half3 albedo;
    half3 emission;
    half3 vertexLight;
    half fogFactor;
};

// The rim's colour at full strength: the earthlight's hue leaning toward the horizon glow's, at brightness 1.
half3 LofiRimHue(half3 lightColor)
{
    half3 earth = lightColor / max(Max3(lightColor.r, lightColor.g, lightColor.b), HALF_MIN);
    half3 horizon = (half3)_MoonHorizonGlow.rgb;
    horizon /= max(Max3(horizon.r, horizon.g, horizon.b), HALF_MIN);
    return lerp(earth, horizon, _MoonRim.w);
}

static const float2 kVogelDisk[8] =
{
    float2(0.1768, 0.0000),
    float2(-0.2261, 0.2071),
    float2(0.0361, -0.4067),
    float2(0.3204, 0.3694),
    float2(-0.5467, -0.1045),
    float2(0.4534, -0.4152),
    float2(-0.0804, 0.6699),
    float2(-0.5798, -0.4581)
};

half SampleLofiSoftShadow(float4 shadowCoord, float2 pixelPos, half baseShadow)
{
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && !defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    if (_MoonLookDepth > 0.5 && shadowCoord.w > 0.0)
    {
        float2 tileMin = floor(shadowCoord.xy * 2.0) * 0.5 + 0.001;
        float2 tileMax = tileMin + 0.498;
        float angle = frac(52.9829189 * frac(dot(pixelPos, float2(0.06711056, 0.00583715)))) * 6.2831853;
        float s = sin(angle);
        float c = cos(angle);
        float radius = 0.00048;
        half accum = baseShadow;
        UNITY_UNROLL
        for (int i = 0; i < 8; i++)
        {
            float2 d = kVogelDisk[i];
            float2 rotated = float2(d.x * c - d.y * s, d.x * s + d.y * c) * radius;
            float4 tap = shadowCoord;
            tap.xy = clamp(shadowCoord.xy + rotated, tileMin, tileMax);
            accum += MainLightRealtimeShadow(tap);
        }
        return accum / 9.0;
    }
    #endif
    return baseShadow;
}

half3 LofiLit(LofiSurface surface)
{
    InputData inputData = (InputData)0;
    inputData.positionWS = surface.positionWS;
    inputData.normalWS = NormalizeNormalPerPixel(surface.normalWS);
    inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(surface.positionWS));
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(surface.positionWS);
    #else
    inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif
    inputData.fogCoord = InitializeInputDataFog(float4(surface.positionWS, 1.0), surface.fogFactor);
    inputData.vertexLighting = surface.vertexLight;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(surface.positionCS);
    inputData.bakedGI = SampleSH(inputData.normalWS);
    inputData.shadowMask = half4(1, 1, 1, 1);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = surface.albedo;
    surfaceData.emission = surface.emission;
    surfaceData.alpha = 1.0;
    surfaceData.occlusion = 1.0;

    half3 color = UniversalFragmentBlinnPhong(inputData, surfaceData).rgb;

    if (_MoonLookDepth > 0.5)
    {
        Light mainLight = GetMainLight(inputData.shadowCoord, surface.positionWS, half4(1, 1, 1, 1));
        half softShadow = SampleLofiSoftShadow(inputData.shadowCoord, surface.positionCS.xy, mainLight.shadowAttenuation);
        half ndotl = dot(inputData.normalWS, mainLight.direction);
        half hardLambert = saturate(ndotl);
        half softWrap = smoothstep(-0.10, 0.92, ndotl) * 0.96;
        half deltaDirect = softWrap * softShadow - hardLambert * mainLight.shadowAttenuation;

        half ssaoDirect = 1.0;
        half ssaoIndirect = 1.0;
        #if defined(_SCREEN_SPACE_OCCLUSION)
        AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
        ssaoDirect = aoFactor.directAmbientOcclusion;
        ssaoIndirect = aoFactor.indirectAmbientOcclusion;
        #endif

        color += surface.albedo * mainLight.color * (deltaDirect * mainLight.distanceAttenuation * ssaoDirect);
        // Subtle hemisphere cavity darkening on downward-facing undersides in V2/V3
        half cavity = lerp(0.86, 1.0, saturate(inputData.normalWS.y * 0.5 + 0.5)) * lerp(1.0, ssaoIndirect, 0.35);
        color *= cavity;
    }

    half facing = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half fresnel = PositivePow(1.0 - facing, _MoonRim.y);
    half side = 1.0 - saturate(inputData.normalWS.y / max(_MoonRim.z, HALF_MIN));
    color += LofiRimHue(GetMainLight().color) * (_MoonRim.x * fresnel * side);

    return MoonAtmosphere(color, surface.positionWS, surface.fogFactor);
}

#endif
