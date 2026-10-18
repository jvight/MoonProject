// The scan's shared helpers (LofiScanSolid, LofiScanLines, R1Lit; SCAN v2). Each renderer is driven with
// a MaterialPropertyBlock: _ScanProgress (0..1, the laser line climbing the renderer's world bounds) and _Dissolve (0..1,
// the 900 ms close: the warm-amber completion ping traces the object's edges, then fades as the object dissolves).
#ifndef MOON_LOFI_SCAN_INPUT_INCLUDED
#define MOON_LOFI_SCAN_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Hold the solid intact during the initial completion ping peak, then dither out before the close ends.
#define MOON_SCAN_PING_HOLD 0.20
#define MOON_SCAN_SOLID_END 0.58
#define MOON_SCAN_LINES_END 0.89

// Global tuning parameters populated by ScanTuning.ApplyShaderGlobals (zero-GC):
// _MoonScanLineColor:  linear RGB colour for the surface scan line and raster band (cyan)
// _MoonScanPingColor:  linear RGB colour for the completion ping edge (warm amber, matching CRT face)
// _MoonScanParams:     x = lineWidthMeters, y = rasterSpacingMeters, z = rasterContrast, w = fadeBandShare
// _MoonScanPingParams: x = lineIntensity,   y = pingDurationSeconds, z = pingEdgeThickness, w = pingEdgeIntensity
// _MoonScanVariantParams: x = variant (0=ProfilerSweep, 1=WireframeReveal, 2=PointScan),
//                         y = surfaceGridPitchMeters, z = wireframeRevealIntensity, w = pointGridRadiusShare
half4 _MoonScanLineColor;
half4 _MoonScanPingColor;
float4 _MoonScanParams;
float4 _MoonScanPingParams;
float4 _MoonScanVariantParams;

half3 MoonScanGetLineColor()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanLineColor.rgb : half3(0.033h, 0.715h, 0.913h);
}

half3 MoonScanGetPingColor()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanPingColor.rgb : half3(1.000h, 0.431h, 0.040h);
}

float4 MoonScanGetParams()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanParams : float4(0.014, 0.048, 0.32, 0.3333);
}

float4 MoonScanGetPingParams()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanPingParams : float4(1.15, 0.38, 1.35, 1.25);
}

float4 MoonScanGetVariantParams()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanVariantParams : float4(0.0, 0.044, 1.18, 0.28);
}

// Computes the camera-tilted height coordinate scanY (tilted ~33 deg toward the view so both flat horizontal
// tops and vertical faces are swept at a healthy angle with uniform line width) and normalized progress h in [0, 1].
void MoonScanSurfaceCoords(
    float3 positionWS,
    out float scanY,
    out float minScanY,
    out float maxScanY,
    out float h,
    out float2 toCamXZ)
{
    float3 boundsMin = unity_RendererBounds_Min.xyz;
    float3 boundsMax = unity_RendererBounds_Max.xyz;
    float3 centre = (boundsMin + boundsMax) * 0.5;
    float3 viewVec = GetWorldSpaceViewDir(centre);
    toCamXZ = normalize(viewVec.xz + float2(1e-5, 0.0));
    float2 halfExtentXZ = max((boundsMax.xz - boundsMin.xz) * 0.5, float2(1e-3, 1e-3));
    float tiltExtent = dot(halfExtentXZ, abs(toCamXZ)) * 0.58;
    minScanY = boundsMin.y - tiltExtent * 0.82;
    maxScanY = boundsMax.y + tiltExtent * 0.82;
    float spanY = max(maxScanY - minScanY, 0.05);
    scanY = positionWS.y - dot(positionWS.xz - centre.xz, toCamXZ) * 0.58;
    h = saturate((scanY - minScanY) / spanY);
}

// The point's height in its renderer's world bounds (0 at the bottom, 1 at the top).
float MoonScanHeight(float3 positionWS)
{
    float scanY, minScanY, maxScanY, h;
    float2 toCamXZ;
    MoonScanSurfaceCoords(positionWS, scanY, minScanY, maxScanY, h, toCamXZ);
    return h;
}

// How far a point lies from its renderer's centre, 0 at the centre and 1 at the bounds' corner.
float MoonScanReach(float3 positionWS)
{
    float3 centre = (unity_RendererBounds_Max.xyz + unity_RendererBounds_Min.xyz) * 0.5;
    float radius = max(length(unity_RendererBounds_Max.xyz - unity_RendererBounds_Min.xyz) * 0.5, 1e-4);
    return saturate(length(positionWS - centre) / radius);
}

// An ordered 4x4 screen dither (0..1), so a solid fades without sorting.
half MoonScanDither(float2 pixel)
{
    const half bayer[16] = { 0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0 };
    uint2 cell = uint2(pixel) % 4;
    return (bayer[cell.y * 4 + cell.x] + 0.5) / 16.0;
}

// How faded a solid is (0 whole, 1 gone): held intact during the initial completion ping, then dithered out.
half MoonScanSolidFade(half dissolve, half fade)
{
    half closeFade = saturate((dissolve - MOON_SCAN_PING_HOLD) / (MOON_SCAN_SOLID_END - MOON_SCAN_PING_HOLD));
    return max(fade, closeFade);
}

// Completion ping envelope (0 during the sweep, rises at completion and fades out over pingDurationSeconds).
half MoonScanPingEnvelope(half dissolve)
{
    float4 pingParams = MoonScanGetPingParams();
    float pingSpan = clamp(pingParams.y / 0.9, 0.16, 0.75);
    return step(0.001h, dissolve)
        * smoothstep(0.0h, 0.04h, dissolve)
        * (1.0h - smoothstep(0.12h, (half)pingSpan, dissolve));
}

// How far the lines have unthreaded (0 standing, 1 gone).
half MoonScanUnthread(half dissolve)
{
    float4 pingParams = MoonScanGetPingParams();
    float pingSpan = clamp(pingParams.y / 0.9, 0.16, 0.75);
    return saturate((dissolve - (half)(pingSpan * 0.35)) / max(0.15h, (half)pingSpan * 0.75h));
}

// Anti-aliased geometric silhouette, normal-crease bevel, vertex-curvature ridge, and bounds edge detector.
float MoonScanComputeEdgeMask(
    float3 positionWS,
    float3 n,
    half edgeCurvature,
    float h,
    float edgeThickness)
{
    float3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
    float ndotv = saturate(dot(n, viewDirWS));
    float fwNdotv = fwidth(ndotv);
    float silPx = max(0.0, ndotv - 0.20) / max(fwNdotv, 1e-4);
    float silEdge = (1.0 - smoothstep(edgeThickness * 0.5, edgeThickness * 1.45, silPx))
        * smoothstep(0.01, 0.035, fwNdotv);

    float normGrad = length(fwidth(n));
    float bevelEdge = smoothstep(0.16, 0.40, normGrad * edgeThickness);

    float curv = (float)edgeCurvature;
    float fwCurv = fwidth(curv);
    float curvPx = abs(curv - 0.52) / max(fwCurv, 1e-4);
    float ridgeEdge = (1.0 - smoothstep(edgeThickness * 0.45, edgeThickness * 1.35, curvPx))
        * smoothstep(0.012, 0.055, fwCurv);

    float fwH = max(fwidth(h), 1e-4);
    float boundPx = min(h, 1.0 - h) / fwH;
    float boundEdge = 1.0 - smoothstep(edgeThickness * 0.55, edgeThickness * 1.45, boundPx);

    return max(max(silEdge, bevelEdge), max(ridgeEdge, boundEdge));
}

// Applies Variant A ("Profiler sweep") to the lit surface colour:
// 1. Thin bright laser line sweeps across the object's surface, conforming to its 3D facets and bevels.
// 2. Anti-aliased fading raster of fine contour lines behind the sweep, strictly locked to the 3D surface.
// 3. At completion, the object's edges and blueprint contours light up in a clean blueprint line drawing that
//    collapses inward toward the lens-facing focal point as the solid dissolves.
void MoonApplySurfaceScan(
    inout half3 color,
    float3 positionWS,
    float3 normalWS,
    half edgeCurvature,
    half scanProgress,
    half dissolve)
{
    if (scanProgress <= 0.0005h && dissolve <= 0.0005h)
    {
        return;
    }

    float4 scanParams = MoonScanGetParams();
    float4 pingParams = MoonScanGetPingParams();
    half3 lineColor = MoonScanGetLineColor();
    half3 pingColor = MoonScanGetPingColor();

    float lineWidthMeters = max(scanParams.x, 0.004);
    float rasterSpacing = max(scanParams.y, 0.025);
    float rasterContrast = clamp(scanParams.z, 0.0, 0.50);
    float fadeBandShare = max(scanParams.w, 0.08);

    float lineIntensity = max(pingParams.x, 0.1);
    float edgeThickness = max(pingParams.z, 0.8);
    float pingEdgeIntensity = max(pingParams.w, 0.1);

    float scanY, minScanY, maxScanY, h;
    float2 toCamXZ;
    MoonScanSurfaceCoords(positionWS, scanY, minScanY, maxScanY, h, toCamXZ);

    float3 n = normalize(normalWS);
    float3 planeN = normalize(float3(-0.58 * toCamXZ.x, 1.0, -0.58 * toCamXZ.y));
    float cosPlane = dot(n, planeN);
    float sinSurface = max(sqrt(saturate(1.0 - cosPlane * cosPlane)), 0.42);

    // 3D surface contour warp so the laser profile line bends across facets, bevels, and curvature on the mesh
    float contourOffset = (n.x * toCamXZ.y - n.z * toCamXZ.x) * 0.012 + ((float)edgeCurvature - 0.5) * 0.015;
    float contouredScanY = scanY + contourOffset;

    float sweepY = lerp(minScanY, maxScanY, saturate((float)scanProgress));
    float surfaceDistMeters = abs(contouredScanY - sweepY) / sinSurface;
    float fwSurface = max(fwidth(contouredScanY) / sinSurface, 1e-4);

    // Gently compress extreme close-range headlamp glare on the scanned target so scan lines stay crisp
    float activeScan = saturate(max((float)scanProgress * 8.0, (float)dissolve * 4.0));
    half peakChannel = max(color.r, max(color.g, color.b));
    if (peakChannel > 0.85h)
    {
        half compressed = 0.85h + (peakChannel - 0.85h) * 0.25h;
        color *= lerp(1.0h, compressed / max(peakChannel, 1e-4h), (half)(activeScan * 0.85));
    }

    float thinEdge = MoonScanComputeEdgeMask(positionWS, n, edgeCurvature, h, edgeThickness);

    // 1. Anti-aliased world-space fading raster of fine contour lines in the swept band behind the laser line
    float phase = (scanY + contourOffset * 0.65) / rasterSpacing;
    float fwPhase = max(fwidth(phase), 1e-4);
    float distToGrid = abs(frac(phase - 0.5) - 0.5);
    float halfGrid = max(0.13, fwPhase * 0.70);
    float aaGrid = max(fwPhase * 1.12, 0.045);
    float rasterLine = 1.0 - smoothstep(halfGrid - aaGrid * 0.5, halfGrid + aaGrid * 0.5, distToGrid);
    float nyquist = 1.0 - smoothstep(0.16, 0.36, fwPhase);
    rasterLine *= nyquist;

    float signedBehindMeters = (sweepY - contouredScanY) / sinSurface;
    float sweptMask = smoothstep(-fwSurface, fwSurface, signedBehindMeters);
    float behindShare = ((float)scanProgress - h) + (float)dissolve * 1.4;
    float bandFade = (1.0 - smoothstep(0.0, fadeBandShare, max(0.0, behindShare))) * sweptMask;
    bandFade *= smoothstep(0.001, 0.02, (float)scanProgress) * (1.0 - smoothstep(0.05, 0.32, (float)dissolve));

    half surfLuma = clamp(dot(color, half3(0.299h, 0.587h, 0.114h)), 0.12h, 0.72h);
    half bandCool = (half)saturate(bandFade * 0.24);
    color = lerp(color, surfLuma * half3(0.48h, 0.88h, 0.96h), bandCool);

    float halfWidth = max(lineWidthMeters * 0.5, fwSurface * 1.25);
    float suppressUnderLine = saturate(surfaceDistMeters / max(halfWidth * 1.5, 1e-4));
    float rasterFeature = max(rasterLine, thinEdge * 0.52 * rasterLine + thinEdge * 0.26);
    half rasterWeight = (half)saturate(rasterFeature * bandFade * suppressUnderLine * rasterContrast * 2.35);
    half3 rasterTarget = surfLuma * half3(0.28h, 0.92h, 1.00h) + lineColor * (half)(0.42 + 0.22 * thinEdge);
    color = lerp(color, rasterTarget, rasterWeight);

    // 2. Crisp bright laser profile line conforming to the 3D surface
    float activeSweep = smoothstep(0.001, 0.015, (float)scanProgress) * (1.0 - smoothstep(0.0, 0.06, (float)dissolve));
    float aaWidth = fwSurface * 1.05;
    float lineCore = (1.0 - smoothstep(halfWidth - aaWidth * 0.5, halfWidth + aaWidth * 0.65, surfaceDistMeters)) * activeSweep;
    float lineSkirt = (1.0 - smoothstep(halfWidth, halfWidth * 3.1 + aaWidth, surfaceDistMeters)) * 0.32 * activeSweep;
    float edgeCrossBoost = lineCore * thinEdge * 0.35;
    half lineReplace = (half)saturate(lineCore * 0.95 + lineSkirt * 1.65);
    half3 lineTarget = lineColor * (half)(lineIntensity * (0.68 + 0.55 * lineCore + edgeCrossBoost))
        + half3(0.18h, 0.46h, 0.50h) * (half)(lineCore * lineCore);
    color = lerp(color, lineTarget, lineReplace);

    // 3. Completion blueprint line drawing that traces the object's edges and collapses into the lens
    if (dissolve > 0.001h)
    {
        float3 boundsMin = unity_RendererBounds_Min.xyz;
        float3 boundsMax = unity_RendererBounds_Max.xyz;
        float3 centre = (boundsMin + boundsMax) * 0.5;
        float radius = max(length(boundsMax - boundsMin) * 0.5, 1e-4);
        float3 focalPoint = centre + float3(toCamXZ.x, 0.12, toCamXZ.y) * (radius * 0.40);
        float collapseDist = saturate(length(positionWS - focalPoint) / max(radius * 1.15, 1e-4));

        float diss = (float)dissolve;
        float collapseFront = 1.0 - smoothstep(0.14, 0.74, diss);
        float insideCollapse = 1.0 - smoothstep(collapseFront - 0.04, collapseFront + 0.04, collapseDist);
        float collapseRing = (1.0 - smoothstep(0.0, 0.11, abs(collapseDist - collapseFront)))
            * smoothstep(0.08, 0.20, diss) * (1.0 - smoothstep(0.62, 0.78, diss));

        half pingEnv = MoonScanPingEnvelope(dissolve);
        float blueprintEnv = max((float)pingEnv, smoothstep(0.01, 0.08, diss) * (1.0 - smoothstep(0.55, 0.76, diss)))
            * insideCollapse;

        float blueprintLines = max(thinEdge, rasterLine * 0.55);
        half edgeMask = (half)saturate(blueprintLines * blueprintEnv + collapseRing * 0.85);
        half3 blueprintTint = lerp(lineColor * 1.10h + half3(0.14h, 0.22h, 0.24h), pingColor, (half)saturate(thinEdge * 0.75 + collapseRing));
        half3 edgeTarget = blueprintTint * (half)(pingEdgeIntensity * (0.76 + 0.44 * blueprintLines));
        color = lerp(color, edgeTarget, edgeMask * 0.92h) + blueprintTint * (edgeMask * (half)(pingEdgeIntensity * 0.28));
    }
}

#endif

