// The scan's shared helpers (LofiScanSolid, LofiScanLines, R1Lit; SCAN v2). Each renderer is driven with
// a MaterialPropertyBlock: _ScanProgress (0..1, the laser line climbing the renderer's world bounds) and _Dissolve (0..1,
// the 900 ms close: the teal completion ping traces the object's edges, then fades as the object dissolves).
#ifndef MOON_LOFI_SCAN_INPUT_INCLUDED
#define MOON_LOFI_SCAN_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Hold the solid intact during the initial completion ping peak, then dither out before the close ends.
#define MOON_SCAN_PING_HOLD 0.20
#define MOON_SCAN_SOLID_END 0.58
#define MOON_SCAN_LINES_END 0.89

// Global tuning parameters populated by ScanTuning.ApplyShaderGlobals (zero-GC):
// _MoonScanLineColor:  linear RGB colour tinting the wireframe's rising front (cyan)
// _MoonScanPingColor:  linear RGB colour of the faint teal wireframe and its completion ping
// _MoonScanParams:     x = frontWidthMeters, y = wireGridPitchMeters, z = wireIntensity, w = edgeThickness
// _MoonScanPingParams: x = pingDurationSeconds, y = pingEdgeIntensity
half4 _MoonScanLineColor;
half4 _MoonScanPingColor;
float4 _MoonScanParams;
float4 _MoonScanPingParams;

half3 MoonScanGetLineColor()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanLineColor.rgb : half3(0.033h, 0.715h, 0.913h);
}

half3 MoonScanGetPingColor()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanPingColor.rgb : half3(0.073h, 0.955h, 0.604h);
}

float4 MoonScanGetParams()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanParams : float4(0.014, 0.044, 1.18, 1.35);
}

float4 MoonScanGetPingParams()
{
    return (_MoonScanParams.y > 1e-4) ? _MoonScanPingParams : float4(0.38, 1.25, 0.0, 0.0);
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
    float pingSpan = clamp(pingParams.x / 0.9, 0.16, 0.75);
    return step(0.001h, dissolve)
        * smoothstep(0.0h, 0.04h, dissolve)
        * (1.0h - smoothstep(0.12h, (half)pingSpan, dissolve));
}

// How far the lines have unthreaded (0 standing, 1 gone).
half MoonScanUnthread(half dissolve)
{
    float4 pingParams = MoonScanGetPingParams();
    float pingSpan = clamp(pingParams.x / 0.9, 0.16, 0.75);
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

// The wireframe reveal: the object's silhouette and key edges light up as a faint teal wireframe hologram that fills
// in from bottom to top as scanProgress rises, under a soft rising front. Every tint derives from the scan's own
// line and ping colours, so the scan never shares a hue with 07's warm amber lamps.
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

    // Progressive fill: once swept, the teal wireframe stays lit and strengthens as progress rises
    float fillEnvelope = revealedMask * activeReveal * lerp(0.78, 1.0, prog);

    float thinEdge = MoonScanComputeEdgeMask(positionWS, n, edgeCurvature, h, edgeThickness * 1.12);
    float cadWire = MoonScanTriplanarWireframe(positionWS, n, gridPitch);

    // A soft cool phosphor wash on the swept surface: the sweep band
    half surfLuma = clamp(dot(color, half3(0.299h, 0.587h, 0.114h)), 0.12h, 0.72h);
    half sweepWash = (half)saturate(fillEnvelope * (0.12 + 0.08 * cadWire));
    color = lerp(color, surfLuma * (lineColor * 0.85h + 0.15h) + pingColor * 0.06h, sweepWash);

    // The faint teal wireframe hologram combining silhouette/bevel key edges and fine 3D CAD surface mesh
    float wirePattern = max(thinEdge, cadWire * 0.58);
    half wireWeight = (half)saturate(wirePattern * fillEnvelope * 0.92);
    half3 wireCore = lerp(pingColor, lerp(pingColor, half3(1.0h, 1.0h, 1.0h), 0.6h), (half)saturate(thinEdge * 0.55));
    half3 wireTarget = wireCore * (half)(wireframeIntensity * (0.72 + 0.48 * thinEdge));
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
        + (lineColor * 0.3h + 0.12h) * (half)(frontCore * frontCore);
    color = lerp(color, frontColor, frontMask);

    // Completion pulse & inward collapse of the teal wireframe hologram
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
        half3 closeTarget = wireCore * (half)(wireframeIntensity * 1.25);
        color = lerp(color, closeTarget, closeMask * 0.92h) + pingColor * (closeMask * 0.32h);
    }
}

// Applies the wireframe reveal to the lit surface colour.
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
    half3 lineColor = MoonScanGetLineColor();
    half3 pingColor = MoonScanGetPingColor();
    float lineWidthMeters = max(scanParams.x, 0.004);
    float gridPitch = max(scanParams.y, 0.018);
    float wireframeIntensity = max(scanParams.z, 0.2);
    float edgeThickness = max(scanParams.w, 0.8);

    float scanY, minScanY, maxScanY, h;
    float2 toCamXZ;
    MoonScanSurfaceCoords(positionWS, scanY, minScanY, maxScanY, h, toCamXZ);

    float3 n = normalize(normalWS);
    float3 planeN = normalize(float3(-0.58 * toCamXZ.x, 1.0, -0.58 * toCamXZ.y));
    float cosPlane = dot(n, planeN);
    float sinSurface = max(sqrt(saturate(1.0 - cosPlane * cosPlane)), 0.42);

    // Gently compress extreme close-range headlamp glare on the scanned target so the wireframe stays crisp
    float activeScan = saturate(max((float)scanProgress * 8.0, (float)dissolve * 4.0));
    half peakChannel = max(color.r, max(color.g, color.b));
    if (peakChannel > 0.85h)
    {
        half compressed = 0.85h + (peakChannel - 0.85h) * 0.25h;
        color *= lerp(1.0h, compressed / max(peakChannel, 1e-4h), (half)(activeScan * 0.85));
    }

    float sweepY = lerp(minScanY, maxScanY, saturate((float)scanProgress));
    float fwSurface = max(fwidth(scanY) / sinSurface, 1e-4);
    MoonApplyWireframeReveal(
        color, positionWS, n, edgeCurvature, scanY, sweepY, sinSurface, fwSurface, h, toCamXZ,
        lineWidthMeters, edgeThickness, wireframeIntensity, lineColor, pingColor, gridPitch,
        scanProgress, dissolve);
}

#endif

