#ifndef MOONPROJECT_R1_LIT_INPUT_INCLUDED
#define MOONPROJECT_R1_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);

TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);

TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half   _BumpScale;
    half   _MetallicScale;
    half   _SmoothnessScale;
    half   _SmoothnessMin;
    half   _SmoothnessMax;
    half   _OcclusionStrength;

    half4  _EdgeWearColor;
    half   _EdgeWearAmount;
    half   _EdgeWearThreshold;
    half   _EdgeWearSharpness;
    half   _EdgeWearMetallic;
    half   _EdgeWearSmoothness;

    half4  _DustColor;
    half   _DustAmount;
    half   _DustSharpness;
    half   _DustUpThreshold;
    half   _DustOcclusionWeight;
    half   _DustSmoothness;

    half   _ColorVarScale;
    half   _ColorVarAmount;

    half4  _EmissionColor;
    half   _EmissionStrength;
    half   _BloomSafeMax;
    half   _HotCoreThreshold;
    half   _HotCorePeak;
    half   _BuildModeGlow;
    half   _ScanProgress;
    half   _Dissolve;
    half   _BuildPrint;
    half   _BuildRevealProgress;
    half   _BuildStageHeat;
    float4 _BuildBoundsY;
    float4 _BuildSeamPointWS;
    half   _DistanceFade;
    float4 _DistanceFadeRange;
    half   _NearKeep;
CBUFFER_END

// Where the distance fade measures from (World sets it to the view camera every frame), so shadow and depth passes
// fade with the view, not with the light.
float4 _MoonDistanceFadeOrigin;

// A material with _DistanceFade on (the near-field rubble) dithers out between _DistanceFadeRange.x and .y metres from
// the view, in every pass, so its far edge thins away instead of popping.
void R1DistanceFadeClip(float3 positionWS, float4 positionCS)
{
    // A prop the camera is passing close to dithers out (the rig sets _NearKeep per renderer) and back as it leaves,
    // so a panel at the lens never fills the foreground.
    if (_NearKeep < 0.999h)
    {
        clip(_NearKeep - InterleavedGradientNoise(positionCS.xy, 0));
    }

    if (_DistanceFade > 0.5h)
    {
        float span = max(_DistanceFadeRange.y - _DistanceFadeRange.x, 1e-3);
        float keep = 1.0 - saturate((distance(positionWS, _MoonDistanceFadeOrigin.xyz) - _DistanceFadeRange.x) / span);
        clip(keep - InterleavedGradientNoise(positionCS.xy, 0));
    }
}

#endif
