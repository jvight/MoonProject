// Per-material values of the Lofi lit shaders (LofiLit, LofiWeather): URP Simple Lit's layout plus the rim, declared
// once so the colour pass and the shadow and depth passes share one SRP Batcher layout.
#ifndef MOON_LOFI_LIT_INPUT_INCLUDED
#define MOON_LOFI_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseMap_TexelSize;
    half4 _BaseColor;
    half4 _SpecColor;
    half4 _EmissionColor;
    half _Cutoff;
    half _Surface;
    // x: rim brightness (linear), y: fresnel exponent, z: up-facing normal (world y) where the rim is gone,
    // w: how far the rim's hue leans from the earthlight toward the sky's horizon glow (0..1).
    half4 _MoonRim;
    UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

#endif
