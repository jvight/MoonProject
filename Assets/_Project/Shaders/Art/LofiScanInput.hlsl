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

// Anti-aliased 3D triplanar CAD wireframe lattice locked to world-space surface geometry.
float MoonScanTriplanarWireframe(float3 positionWS, float3 n, float gridPitch)
{
    float3 p = positionWS / max(gridPitch, 0.015);
    float3 fw = max(fwidth(p), float3(1e-4, 1e-4, 1e-4));
    float3 dist = abs(frac(p - 0.5) - 0.5);
    float3 halfLine = max(float3(0.11, 0.11, 0.11), fw * 0.68);
    float3 aa = max(fw * 1.15, float3(0.04, 0.04, 0.04));
    float3 line3 = (1.0 - smoothstep(halfLine - aa * 0.5, halfLine + aa * 0.5, dist))
        * (1.0 - smoothstep(0.18, 0.38, fw));

    float3 w = abs(n);
    w /= max(w.x + w.y + w.z, 1e-4);
    float planeXY = max(line3.x, line3.y);
    float planeYZ = max(line3.y, line3.z);
    float planeXZ = max(line3.x, line3.z);
    float wire = planeYZ * w.x + planeXZ * w.y + planeXY * w.z;
    float node = (line3.x * line3.y * w.z + line3.y * line3.z * w.x + line3.x * line3.z * w.y);
    return saturate(wire + node * 0.45);
}

// Variant B ("Wireframe reveal"): the object's silhouette and key edges light up as a soft wireframe hologram
// in CRT amber that progressively fills in from bottom to top as scanProgress rises.
void MoonApplyWireframeReveal(
    inout half3 color,
    float3 positionWS,
    float3 n,
    half edgeCurvature,
    float scanY,
    float sweepY,
    float sinSurface,
    float fwSurface,
    float h,
    float2 toCamXZ,
    float lineWidthMeters,
    float edgeThickness,
    float wireframeIntensity,
    half3 lineColor,
    half3 pingColor,
    float gridPitch,
    half scanProgress,
    half dissolve)
{
    float prog = saturate((float)scanProgress);
    float diss = (float)dissolve;
    float signedBehindMeters = (sweepY - scanY) / sinSurface;
    float revealedMask = smoothstep(-fwSurface * 1.5, fwSurface * 1.5, signedBehindMeters);
    float activeReveal = smoothstep(0.001, 0.02, prog) * (1.0 - smoothstep(0.58, 0.82, diss));

    // Progressive fill: once swept, the CRT-amber wireframe stays lit and strengthens as progress rises
    float fillEnvelope = revealedMask * activeReveal * lerp(0.78, 1.0, prog);

    float thinEdge = MoonScanComputeEdgeMask(positionWS, n, edgeCurvature, h, edgeThickness * 1.12);
    float cadWire = MoonScanTriplanarWireframe(positionWS, n, gridPitch);

    // Subtle warm-amber holographic phosphor wash on the revealed mesh surface
    half surfLuma = clamp(dot(color, half3(0.299h, 0.587h, 0.114h)), 0.12h, 0.72h);
    half amberWash = (half)saturate(fillEnvelope * (0.20 + 0.12 * cadWire));
    color = lerp(color, surfLuma * half3(1.00h, 0.62h, 0.18h) + pingColor * 0.08h, amberWash);

    // Soft CRT-amber wireframe hologram combining silhouette/bevel key edges and fine 3D CAD surface mesh
    float wirePattern = max(thinEdge, cadWire * 0.58);
    half wireWeight = (half)saturate(wirePattern * fillEnvelope * 0.92);
    half3 amberCore = lerp(pingColor, half3(1.00h, 0.88h, 0.55h), (half)saturate(thinEdge * 0.55));
    half3 wireTarget = amberCore * (half)(wireframeIntensity * (0.72 + 0.48 * thinEdge));
    color = lerp(color, wireTarget, wireWeight) + pingColor * (wireWeight * (half)(wireframeIntensity * 0.18));

    // Soft rising wavefront at the top edge of the wireframe reveal
    float activeSweep = smoothstep(0.001, 0.015, prog) * (1.0 - smoothstep(0.0, 0.06, diss));
    float surfaceDistMeters = abs(scanY - sweepY) / sinSurface;
    float halfWidth = max(lineWidthMeters * 0.65, fwSurface * 1.35);
    float aaWidth = fwSurface * 1.15;
    float frontCore = (1.0 - smoothstep(halfWidth - aaWidth * 0.5, halfWidth + aaWidth * 0.8, surfaceDistMeters)) * activeSweep;
    float frontGlow = (1.0 - smoothstep(halfWidth, halfWidth * 3.6 + aaWidth, surfaceDistMeters)) * 0.36 * activeSweep;
    half frontMask = (half)saturate(frontCore * 0.92 + frontGlow * 1.45);
    half3 frontColor = lerp(pingColor, lineColor, 0.22h) * (half)(wireframeIntensity * 1.12)
        + half3(0.45h, 0.34h, 0.14h) * (half)(frontCore * frontCore);
    color = lerp(color, frontColor, frontMask);

    // Completion pulse & inward collapse of the amber wireframe hologram
    if (dissolve > 0.001h)
    {
        float3 boundsMin = unity_RendererBounds_Min.xyz;
        float3 boundsMax = unity_RendererBounds_Max.xyz;
        float3 centre = (boundsMin + boundsMax) * 0.5;
        float radius = max(length(boundsMax - boundsMin) * 0.5, 1e-4);
        float3 focalPoint = centre + float3(toCamXZ.x, 0.12, toCamXZ.y) * (radius * 0.40);
        float collapseDist = saturate(length(positionWS - focalPoint) / max(radius * 1.15, 1e-4));
        float collapseFront = 1.0 - smoothstep(0.16, 0.76, diss);
        float insideCollapse = 1.0 - smoothstep(collapseFront - 0.05, collapseFront + 0.05, collapseDist);
        float collapseRing = (1.0 - smoothstep(0.0, 0.12, abs(collapseDist - collapseFront)))
            * smoothstep(0.06, 0.18, diss) * (1.0 - smoothstep(0.64, 0.80, diss));

        half pingEnv = MoonScanPingEnvelope(dissolve);
        half closeMask = (half)saturate((wirePattern * (float)pingEnv + collapseRing * 0.85) * insideCollapse);
        half3 closeTarget = amberCore * (half)(wireframeIntensity * 1.25);
        color = lerp(color, closeTarget, closeMask * 0.92h) + pingColor * (closeMask * 0.32h);
    }
}

// Anti-aliased 3D triplanar point-cloud dot lattice locked strictly to world-space surface geometry.
float MoonScanSurfacePointCloud(float3 samplePosWS, float3 n, float gridPitch, float pointRadiusShare)
{
    float pitch = max(gridPitch, 0.018);
    float3 p = samplePosWS / pitch;
    float3 fw = max(fwidth(p), float3(1e-4, 1e-4, 1e-4));

    float2 uvXY = frac(p.xy - 0.5) - 0.5;
    float2 uvYZ = frac(p.yz - 0.5) - 0.5;
    float2 uvXZ = frac(p.xz - 0.5) - 0.5;

    float dXY = length(uvXY);
    float dYZ = length(uvYZ);
    float dXZ = length(uvXZ);

    float fwXY = max(fw.x, fw.y);
    float fwYZ = max(fw.y, fw.z);
    float fwXZ = max(fw.x, fw.z);

    float r = clamp(pointRadiusShare, 0.14, 0.42);
    float dotXY = (1.0 - smoothstep(max(r, fwXY * 0.75) - fwXY * 0.65, max(r, fwXY * 0.75) + fwXY * 0.85, dXY))
        * (1.0 - smoothstep(0.22, 0.44, fwXY));
    float dotYZ = (1.0 - smoothstep(max(r, fwYZ * 0.75) - fwYZ * 0.65, max(r, fwYZ * 0.75) + fwYZ * 0.85, dYZ))
        * (1.0 - smoothstep(0.22, 0.44, fwYZ));
    float dotXZ = (1.0 - smoothstep(max(r, fwXZ * 0.75) - fwXZ * 0.65, max(r, fwXZ * 0.75) + fwXZ * 0.85, dXZ))
        * (1.0 - smoothstep(0.22, 0.44, fwXZ));

    float3 w = abs(n);
    w /= max(w.x + w.y + w.z, 1e-4);
    return saturate(dotYZ * w.x + dotXZ * w.y + dotXY * w.z);
}

// Variant C ("Point scan"): a scanning dot grid / point-cloud sweeps the object (dots stick to the 3D surface
// and fade behind the sweep); at completion the point cloud contracts toward the lens-facing focal point.
void MoonApplyPointScan(
    inout half3 color,
    float3 positionWS,
    float3 n,
    half edgeCurvature,
    float scanY,
    float sweepY,
    float sinSurface,
    float fwSurface,
    float h,
    float2 toCamXZ,
    float lineWidthMeters,
    float fadeBandShare,
    float lineIntensity,
    float edgeThickness,
    float pingEdgeIntensity,
    half3 lineColor,
    half3 pingColor,
    float gridPitch,
    float pointRadiusShare,
    half scanProgress,
    half dissolve)
{
    float prog = saturate((float)scanProgress);
    float diss = (float)dissolve;

    float3 boundsMin = unity_RendererBounds_Min.xyz;
    float3 boundsMax = unity_RendererBounds_Max.xyz;
    float3 centre = (boundsMin + boundsMax) * 0.5;
    float radius = max(length(boundsMax - boundsMin) * 0.5, 1e-4);
    float3 focalPoint = centre + float3(toCamXZ.x, 0.12, toCamXZ.y) * (radius * 0.40);

    // During completion dissolve, contract the 3D surface point cloud coordinates toward the lens-facing focal point
    float contractShare = smoothstep(0.06, 0.72, diss) * 0.55;
    float3 samplePosWS = lerp(positionWS, focalPoint, -contractShare);

    float dotGrid = MoonScanSurfacePointCloud(samplePosWS, n, gridPitch, pointRadiusShare);
    float thinEdge = MoonScanComputeEdgeMask(positionWS, n, edgeCurvature, h, edgeThickness * 0.95);
    float edgeDots = thinEdge * MoonScanSurfacePointCloud(samplePosWS, n, gridPitch * 0.68, pointRadiusShare * 1.15);
    float pointMask = saturate(max(dotGrid, edgeDots * 1.15));

    float signedBehindMeters = (sweepY - scanY) / sinSurface;
    float sweptMask = smoothstep(-fwSurface * 1.5, fwSurface * 1.5, signedBehindMeters);
    float behindShare = (prog - h) + diss * 1.25;
    float trailShare = clamp(fadeBandShare * 1.35, 0.16, 0.92);
    float bandFade = (1.0 - smoothstep(0.0, trailShare, max(0.0, behindShare))) * sweptMask;
    bandFade *= smoothstep(0.001, 0.02, prog) * (1.0 - smoothstep(0.08, 0.36, diss));

    // Active sweeping front where new point-cloud dots ignite on the 3D surface
    float activeSweep = smoothstep(0.001, 0.015, prog) * (1.0 - smoothstep(0.0, 0.06, diss));
    float surfaceDistMeters = abs(scanY - sweepY) / sinSurface;
    float halfWidth = max(lineWidthMeters * 0.75, fwSurface * 1.35);
    float frontBand = (1.0 - smoothstep(halfWidth * 0.5, halfWidth * 4.2, surfaceDistMeters)) * activeSweep;
    float fineLaserCore = (1.0 - smoothstep(halfWidth * 0.35, halfWidth * 1.15, surfaceDistMeters)) * activeSweep;

    // Subtle cool-dark contrast bed behind the sweep so the glowing surface dots pop in daylight and eclipse
    half surfLuma = clamp(dot(color, half3(0.299h, 0.587h, 0.114h)), 0.10h, 0.70h);
    color = lerp(color, surfLuma * half3(0.42h, 0.82h, 0.94h), (half)saturate(bandFade * 0.18));

    // Surface-locked point-cloud dots that ignite bright cyan-white at the sweep front and fade through warm amber
    float dotEnvelope = saturate(pointMask * (bandFade * 0.92 + frontBand * 0.85));
    float warmShift = saturate(behindShare / max(trailShare * 0.85, 1e-4));
    half3 dotColor = lerp(lineColor * 1.15h + half3(0.20h, 0.35h, 0.38h) * (half)frontBand, pingColor, (half)(warmShift * 0.58));
    half3 dotTarget = dotColor * (half)(lineIntensity * (0.85 + 0.55 * frontBand));
    color = lerp(color, dotTarget, (half)saturate(dotEnvelope * 0.95));

    // Fine laser contact line modulated by the surface point grid
    half laserMask = (half)saturate(fineLaserCore * (0.42 + 0.58 * pointMask));
    half3 laserTarget = lineColor * (half)(lineIntensity * 1.15) + half3(0.22h, 0.48h, 0.52h) * (half)fineLaserCore;
    color = lerp(color, laserTarget, laserMask);

    // Completion: the surface point cloud brightens and contracts into the lens-facing focal point
    if (dissolve > 0.001h)
    {
        float collapseDist = saturate(length(positionWS - focalPoint) / max(radius * 1.15, 1e-4));
        float collapseFront = 1.0 - smoothstep(0.12, 0.74, diss);
        float insideCollapse = 1.0 - smoothstep(collapseFront - 0.05, collapseFront + 0.05, collapseDist);
        float collapseRing = (1.0 - smoothstep(0.0, 0.12, abs(collapseDist - collapseFront)))
            * smoothstep(0.04, 0.16, diss) * (1.0 - smoothstep(0.62, 0.78, diss));

        half pingEnv = MoonScanPingEnvelope(dissolve);
        float closeDots = saturate(max(pointMask, thinEdge * 0.75) * ((float)pingEnv * 1.15 + collapseRing));
        half closeMask = (half)saturate(closeDots * insideCollapse);
        half3 closeTint = lerp(lineColor, pingColor, (half)saturate(0.45 + 0.55 * collapseRing));
        half3 closeTarget = closeTint * (half)(pingEdgeIntensity * 1.22);
        color = lerp(color, closeTarget, closeMask * 0.92h) + closeTint * (closeMask * 0.28h);
    }
}

// Applies the active surface scan variant (A: ProfilerSweep, B: WireframeReveal, C: PointScan) to the lit surface colour.
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
    float4 variantParams = MoonScanGetVariantParams();
    half3 lineColor = MoonScanGetLineColor();
    half3 pingColor = MoonScanGetPingColor();

    int variant = (int)round(variantParams.x);
    float gridPitch = max(variantParams.y, 0.018);
    float wireframeIntensity = max(variantParams.z, 0.2);
    float pointRadiusShare = clamp(variantParams.w, 0.12, 0.45);

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

    // Gently compress extreme close-range headlamp glare on the scanned target so scan lines stay crisp
    float activeScan = saturate(max((float)scanProgress * 8.0, (float)dissolve * 4.0));
    half peakChannel = max(color.r, max(color.g, color.b));
    if (peakChannel > 0.85h)
    {
        half compressed = 0.85h + (peakChannel - 0.85h) * 0.25h;
        color *= lerp(1.0h, compressed / max(peakChannel, 1e-4h), (half)(activeScan * 0.85));
    }

    float sweepY = lerp(minScanY, maxScanY, saturate((float)scanProgress));
    float fwSurface = max(fwidth(scanY) / sinSurface, 1e-4);

    if (variant == 1)
    {
        MoonApplyWireframeReveal(
            color, positionWS, n, edgeCurvature, scanY, sweepY, sinSurface, fwSurface, h, toCamXZ,
            lineWidthMeters, edgeThickness, wireframeIntensity, lineColor, pingColor, gridPitch,
            scanProgress, dissolve);
        return;
    }

    if (variant == 2)
    {
        MoonApplyPointScan(
            color, positionWS, n, edgeCurvature, scanY, sweepY, sinSurface, fwSurface, h, toCamXZ,
            lineWidthMeters, fadeBandShare, lineIntensity, edgeThickness, pingEdgeIntensity,
            lineColor, pingColor, gridPitch, pointRadiusShare, scanProgress, dissolve);
        return;
    }

    // 3D surface contour warp so the laser profile line bends across facets, bevels, and curvature on the mesh
    float contourOffset = (n.x * toCamXZ.y - n.z * toCamXZ.x) * 0.012 + ((float)edgeCurvature - 0.5) * 0.015;
    float contouredScanY = scanY + contourOffset;
    float surfaceDistMeters = abs(contouredScanY - sweepY) / sinSurface;
    fwSurface = max(fwidth(contouredScanY) / sinSurface, 1e-4);

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

