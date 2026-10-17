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

half3 LofiDiffuseLight(Light light, float3 normalWS, float3 albedo)
{
    float ndotl = saturate(dot(normalWS, float3(light.direction)));
    if (ndotl <= 0.0)
    {
        return half3(0.0h, 0.0h, 0.0h);
    }

    float atten = min(light.distanceAttenuation, 16.0) * float(light.shadowAttenuation);
    float3 radiance = clamp(float3(light.color) * (ndotl * atten) - 1e-5, 0.0, 8.0);
    return (half3)clamp(radiance * albedo - 1e-5, 0.0, 4.0);
}

half3 LofiLit(LofiSurface surface)
{
    float normLenSq = dot(float3(surface.normalWS), float3(surface.normalWS));
    float3 normalWSFloat = normLenSq > 1e-6 ? float3(surface.normalWS) * rsqrt(normLenSq) : float3(0.0, 1.0, 0.0);

    InputData inputData = (InputData)0;
    inputData.positionWS = surface.positionWS;
    inputData.normalWS = (half3)normalWSFloat;
    inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(surface.positionWS));
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(surface.positionWS);
    #else
    inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif
    inputData.fogCoord = InitializeInputDataFog(float4(surface.positionWS, 1.0), surface.fogFactor);
    inputData.vertexLighting = clamp(surface.vertexLight - 1e-4h, 0.0h, 4.0h);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(surface.positionCS);
    inputData.bakedGI = SampleSH(inputData.normalWS);
    inputData.shadowMask = half4(1, 1, 1, 1);

    float3 albedoFloat = clamp(float3(surface.albedo) - 1e-5, 0.0, 1.0);
    float3 emissionFloat = clamp(float3(surface.emission) - 1e-5, 0.0, 1.24);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = (half3)albedoFloat;
    surfaceData.emission = (half3)emissionFloat;
    surfaceData.alpha = 1.0;
    surfaceData.occlusion = 1.0;

    uint meshRenderingLayers = GetMeshRenderingLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

    float3 totalColor = clamp(float3(inputData.bakedGI) * albedoFloat - 1e-5, 0.0, 2.0) + emissionFloat;
    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    #endif
    {
        totalColor += float3(LofiDiffuseLight(mainLight, normalWSFloat, albedoFloat));
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
        #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
        {
            totalColor += float3(LofiDiffuseLight(light, normalWSFloat, albedoFloat));
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
        #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
        {
            totalColor += float3(LofiDiffuseLight(light, normalWSFloat, albedoFloat));
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    totalColor += float3(inputData.vertexLighting) * albedoFloat;
    #endif

    float facing = saturate(dot(normalWSFloat, float3(inputData.viewDirectionWS)));
    float fresnel = pow(max(1.0 - facing, 1e-4), max((float)_MoonRim.y, 0.1));
    float side = 1.0 - saturate(normalWSFloat.y / max((float)_MoonRim.z, 1e-4));
    totalColor += float3(LofiRimHue(GetMainLight().color)) * ((float)_MoonRim.x * fresnel * side);

    half3 color = (half3)clamp(totalColor - 1e-5, 0.0, 2.0);
    color = MoonAtmosphere(color, surface.positionWS, surface.fogFactor);
    return clamp(color - 1e-4h, 0.0h, 2.0h);
}

#endif
