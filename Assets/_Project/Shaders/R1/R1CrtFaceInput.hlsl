#ifndef MOONPROJECT_R1_CRT_FACE_INPUT_INCLUDED
#define MOONPROJECT_R1_CRT_FACE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _ScreenBaseColor;
    half4 _ScreenEdgeColor;
    half4 _PhosphorColor;
    half4 _PhosphorGlowColor;
    half4 _HotCoreColor;
    half _BloomSafeMax;
    half _HotCorePeak;
    half _Smoothness;
    half _Curvature;
    half _ScanlineCount;
    half _ScanlineDepth;
    half _GlowRadius;
    half _TrailAlpha;

    float4 _EyeLeftRect;
    float4 _EyeLeftShape;
    float4 _EyeLeftMode;
    float4 _EyeRightRect;
    float4 _EyeRightShape;
    float4 _EyeRightMode;

    float4 _GhostLeftRect;
    float4 _GhostLeftShape;
    float4 _GhostLeftMode;
    float4 _GhostRightRect;
    float4 _GhostRightShape;
    float4 _GhostRightMode;

    float4 _FaceSymbol;
    float4 _FaceCrtState;
    float4 _FacePhases;
    float4 _FaceContentState;
CBUFFER_END

TEXTURE2D(_ContentTex);
SAMPLER(sampler_ContentTex);

#endif
