#ifndef MOONPROJECT_R1_LIT_INPUT_INCLUDED
#define MOONPROJECT_R1_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
CBUFFER_END

#endif
