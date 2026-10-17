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
half4 _MoonScanLineColor;
half4 _MoonScanPingColor;
float4 _MoonScanParams;
float4 _MoonScanPingParams;

// Global ground scan line parameters populated per frame by ScanBeam (SCAN v3, zero-GC):
// _MoonScanGroundStart:  xyz = world start point under 07's cheek lens (xz = lens ground projection, y = height ceiling near 07), w = active level (0..1)
// _MoonScanGroundEnd:    xyz = world target base point, w = progress (0..1 during sweep, >1 during completion ping)
// _MoonScanGroundParams: x = groundLineWidthMeters, y = groundLineIntensity, z = startOffsetMeters, w = reserved
float4 _MoonScanGroundStart;
float4 _MoonScanGroundEnd;
float4 _MoonScanGroundParams;

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
    return (_MoonScanParams.y > 1e-4) ? _MoonScanParams : float4(0.014, 0.055, 0.18, 0.3333);
}

float4 MoonScanGetPingParams()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanPingParams : float4(1.35, 0.38, 1.35, 1.25);
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
        * smoothstep(0.0h, 0.05h, dissolve)
        * (1.0h - smoothstep(0.09h, (half)pingSpan, dissolve));
}

// How far the lines have unthreaded (0 standing, 1 gone).
half MoonScanUnthread(half dissolve)
{
    float4 pingParams = MoonScanGetPingParams();
    float pingSpan = clamp(pingParams.y / 0.9, 0.16, 0.75);
    return saturate((dissolve - (half)(pingSpan * 0.35)) / max(0.15h, (half)pingSpan * 0.75h));
}

// Applies the physically grounded laser fan intersection line on the terrain and any props between 07's cheek lens
// and the scanned target's base (SCAN v3), strictly along the vertical fan plane's straight ground trace (v = 0).
void MoonApplyGroundScanLine(
    inout half3 color,
    float3 positionWS,
    float3 normalWS,
    bool isTerrain)
{
    float activeLevel = saturate(_MoonScanGroundStart.w);
    if (activeLevel <= 0.001)
    {
        return;
    }

    float2 startXZ = _MoonScanGroundStart.xz;
    float2 endXZ = _MoonScanGroundEnd.xz;
    float2 seg = endXZ - startXZ;
    float segLen = length(seg);
    if (segLen <= 0.05)
    {
        return;
    }

    float2 forwardXZ = seg / segLen;
    float2 rightXZ = float2(forwardXZ.y, -forwardXZ.x);
    float2 relXZ = positionWS.xz - startXZ;
    float u = dot(relXZ, forwardXZ);
    float v = dot(relXZ, rightXZ);

    // Protect 07's own body/head/wheels when evaluating on non-terrain meshes
    if (!isTerrain)
    {
        if (u < 1.15 || positionWS.y > _MoonScanGroundStart.y)
        {
            return;
        }
    }

    float startOffset = max(_MoonScanGroundParams.z, 0.35);
    if (u < startOffset - 0.05 || u > segLen + 0.15)
    {
        return;
    }

    float startEdge = isTerrain
        ? smoothstep(startOffset, startOffset + 0.22, u)
        : smoothstep(1.15, 1.32, u);
    float endEdge = isTerrain
        ? (1.0 - smoothstep(segLen - 0.36, segLen - 0.14, u))
        : (1.0 - smoothstep(segLen - 0.35, segLen - 0.08, u));
    float alongMask = startEdge * endEdge;
    if (alongMask <= 0.001)
    {
        return;
    }

    // Strict vertical plane intersection with the 3D terrain surface: v = 0 (zero sideways arc).
    float distToLine = abs(v);
    float fwV = max(fwidth(v), 1e-4);
    float lineWidthMeters = clamp(_MoonScanGroundParams.x, 0.008, 0.028);
    float halfWidth = max(lineWidthMeters * 0.5, fwV * 0.95);
    if (distToLine > halfWidth * 2.6 + fwV * 1.5)
    {
        return;
    }

    half3 cyanColor = MoonScanGetLineColor();
    half3 amberColor = MoonScanGetPingColor();
    float rawProgress = _MoonScanGroundEnd.w;
    float pingBlend = saturate(rawProgress - 1.0);
    half3 lineColor = lerp(cyanColor, amberColor, (half)(pingBlend * 0.85));

    float lineIntensity = max(_MoonScanGroundParams.y, 0.4);
    float aaWidth = fwV * 0.95;
    float lineCore = (1.0 - smoothstep(halfWidth - aaWidth * 0.5, halfWidth + aaWidth * 0.5, distToLine))
        * alongMask * activeLevel;
    float lineGlow = (1.0 - smoothstep(halfWidth * 0.45, halfWidth * 2.15 + aaWidth, distToLine))
        * alongMask * activeLevel;

    // Modulate by surface incidence and underlying regolith luminance so craters and slopes remain visible
    // inside the laser trace and the line reads as projected light rather than an opaque cable.
    float3 emitterWS = _MoonScanGroundStart.xyz;
    float3 toEmitterDir = normalize(emitterWS - positionWS);
    float incidence = saturate(dot(normalWS, toEmitterDir) * 0.65 + normalWS.y * 0.55);
    float t = saturate((u - startOffset) / max(segLen - startOffset, 0.1));
    float distanceAtten = lerp(1.0, 0.72, t);
    float reliefMod = clamp(0.62 + 0.58 * incidence, 0.45, 1.22);

    half surfLuma = clamp(dot(color, half3(0.299h, 0.587h, 0.114h)), 0.08h, 0.82h);
    half3 laserTint = surfLuma * half3(0.16h, 0.80h, 0.92h)
        + lineColor * (half)(lineIntensity * 0.40 * distanceAtten * reliefMod);
    half blendWeight = (half)saturate((lineCore * 0.56 + lineGlow * 0.18) * reliefMod);
    color = lerp(color, laserTint, blendWeight);
}

// Applies the surface scan look (SCAN v2) directly to the lit surface colour:
// 1. Crisp bright cyan scan line where the laser sweep plane touches the object, conforming to its 3D surface.
// 2. Anti-aliased world-space low-contrast cyan raster lines only in the swept band behind the line, fading over ~1 s.
// 3. Thin, anti-aliased warm-amber emissive edge that traces the object ONLY at the completion ping, then fades.
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
        MoonApplyGroundScanLine(color, positionWS, normalWS, false);
        return;
    }

    float4 scanParams = MoonScanGetParams();
    float4 pingParams = MoonScanGetPingParams();
    half3 lineColor = MoonScanGetLineColor();
    half3 pingColor = MoonScanGetPingColor();

    float lineWidthMeters = max(scanParams.x, 0.004);
    float rasterSpacing = max(scanParams.y, 0.025);
    float rasterContrast = clamp(scanParams.z, 0.0, 0.45);
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

    float sweepY = lerp(minScanY, maxScanY, saturate((float)scanProgress));
    float surfaceDistMeters = abs(scanY - sweepY) / sinSurface;
    float fwSurface = max(fwidth(scanY) / sinSurface, 1e-4);

    // Gently compress extreme close-range headlamp glare on the scanned target so scan lines stay crisp
    float activeScan = saturate(max((float)scanProgress * 8.0, (float)dissolve * 4.0));
    half peakChannel = max(color.r, max(color.g, color.b));
    if (peakChannel > 0.85h)
    {
        half compressed = 0.85h + (peakChannel - 0.85h) * 0.25h;
        color *= lerp(1.0h, compressed / max(peakChannel, 1e-4h), (half)(activeScan * 0.85));
    }

    // 1. Anti-aliased world-space low-contrast raster lines in the swept band (fading over ~1 s behind the line)
    float phase = scanY / rasterSpacing;
    float fwPhase = max(fwidth(phase), 1e-4);
    float distToGrid = abs(frac(phase - 0.5) - 0.5);
    float halfGrid = max(0.14, fwPhase * 0.72);
    float aaGrid = max(fwPhase * 1.15, 0.05);
    float rasterLine = 1.0 - smoothstep(halfGrid - aaGrid * 0.5, halfGrid + aaGrid * 0.5, distToGrid);
    // Pre-filter near Nyquist so grazing angles never moire or shimmer
    float nyquist = 1.0 - smoothstep(0.16, 0.36, fwPhase);
    rasterLine *= nyquist;

    float signedBehindMeters = (sweepY - scanY) / sinSurface;
    float sweptMask = smoothstep(-fwSurface, fwSurface, signedBehindMeters);
    float behindShare = ((float)scanProgress - h) + (float)dissolve * 1.4;
    float bandFade = (1.0 - smoothstep(0.0, fadeBandShare, max(0.0, behindShare))) * sweptMask;
    bandFade *= smoothstep(0.001, 0.02, (float)scanProgress) * (1.0 - smoothstep(0.05, 0.32, (float)dissolve));

    // Subtle cool-cyan wash in the swept band + low-contrast anti-aliased cyan raster lines
    half surfLuma = clamp(dot(color, half3(0.299h, 0.587h, 0.114h)), 0.10h, 0.72h);
    half bandCool = (half)saturate(bandFade * 0.22);
    color = lerp(color, surfLuma * half3(0.52h, 0.88h, 0.96h), bandCool);

    float halfWidth = max(lineWidthMeters * 0.5, fwSurface * 1.25);
    float suppressUnderLine = saturate(surfaceDistMeters / max(halfWidth * 1.6, 1e-4));
    half rasterWeight = (half)saturate(rasterLine * bandFade * suppressUnderLine * rasterContrast * 2.2);
    half3 rasterTarget = surfLuma * half3(0.32h, 0.90h, 0.98h) + lineColor * 0.28h;
    color = lerp(color, rasterTarget, rasterWeight);

    // 2. Crisp bright cyan scan line conforming to the 3D surface
    float activeSweep = smoothstep(0.001, 0.015, (float)scanProgress) * (1.0 - smoothstep(0.0, 0.06, (float)dissolve));
    float aaWidth = fwSurface * 1.05;
    float lineCore = (1.0 - smoothstep(halfWidth - aaWidth * 0.5, halfWidth + aaWidth * 0.7, surfaceDistMeters)) * activeSweep;
    float lineSkirt = (1.0 - smoothstep(halfWidth, halfWidth * 3.0 + aaWidth, surfaceDistMeters)) * 0.28 * activeSweep;
    half lineReplace = (half)saturate(lineCore * 0.94 + lineSkirt * 1.65);
    half3 lineTarget = lineColor * (half)(lineIntensity * (0.65 + 0.55 * lineCore))
        + half3(0.12h, 0.42h, 0.46h) * (half)(lineCore * lineCore);
    color = lerp(color, lineTarget, lineReplace);

    // 3. Thin, anti-aliased warm-amber edge tracing the object ONLY at the completion ping, then fading
    half pingEnv = MoonScanPingEnvelope(dissolve);
    if (pingEnv > 0.001h)
    {
        float3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
        float ndotv = saturate(dot(n, viewDirWS));
        float fwNdotv = fwidth(ndotv);
        float silPx = max(0.0, ndotv - 0.18) / max(fwNdotv, 1e-4);
        float silEdge = (1.0 - smoothstep(edgeThickness * 0.5, edgeThickness * 1.45, silPx))
            * smoothstep(0.01, 0.035, fwNdotv);

        float normGrad = length(fwidth(n));
        float bevelEdge = smoothstep(0.18, 0.42, normGrad * edgeThickness);

        float curv = (float)edgeCurvature;
        float fwCurv = fwidth(curv);
        float curvPx = abs(curv - 0.52) / max(fwCurv, 1e-4);
        float ridgeEdge = (1.0 - smoothstep(edgeThickness * 0.45, edgeThickness * 1.35, curvPx))
            * smoothstep(0.015, 0.06, fwCurv);

        float fwH = max(fwidth(h), 1e-4);
        float boundPx = min(h, 1.0 - h) / fwH;
        float boundEdge = 1.0 - smoothstep(edgeThickness * 0.55, edgeThickness * 1.45, boundPx);

        float thinEdge = max(max(silEdge, bevelEdge), max(ridgeEdge, boundEdge));
        half edgeMask = (half)saturate(thinEdge * (float)pingEnv);
        half3 edgeTarget = pingColor * (half)(pingEdgeIntensity * (0.75 + 0.45 * thinEdge));
        color = lerp(color, edgeTarget, edgeMask * 0.90h) + pingColor * (edgeMask * (half)(pingEdgeIntensity * 0.30));
    }
}

#endif
