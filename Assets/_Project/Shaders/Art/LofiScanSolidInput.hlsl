// Per-material values of LofiScanSolid: LofiLit's layout (so LofiLitCore lights it the same) plus the scan's colours and
// the per-renderer drive, declared once so its colour, shadow and depth passes share one SRP Batcher layout.
#ifndef MOON_LOFI_SCAN_SOLID_INPUT_INCLUDED
#define MOON_LOFI_SCAN_SOLID_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "LofiScanInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseMap_TexelSize;
    half4 _BaseColor;
    half4 _SpecColor;
    half4 _EmissionColor;
    half _Cutoff;
    half _Surface;
    half4 _MoonRim;
    half4 _MoonAoTint;
    half4 _SweepColor;
    half4 _DrawnColor;
    half _SweepWidth;
    half _ScanProgress;
    half _Dissolve;
    half _Fade;
    UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

#endif
