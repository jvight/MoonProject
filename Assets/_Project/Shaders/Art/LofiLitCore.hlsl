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

    half facing = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half fresnel = PositivePow(1.0 - facing, _MoonRim.y);
    half side = 1.0 - saturate(inputData.normalWS.y / max(_MoonRim.z, HALF_MIN));
    color += LofiRimHue(GetMainLight().color) * (_MoonRim.x * fresnel * side);

    return MoonAtmosphere(color, surface.positionWS, surface.fogFactor);
}

#endif
