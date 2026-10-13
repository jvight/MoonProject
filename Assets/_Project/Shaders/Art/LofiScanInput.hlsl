// The scan's shared helpers (LofiScanSolid, LofiScanLines; 0.5.5 owner feedback §1, D2). Each renderer is driven with
// a MaterialPropertyBlock: _ScanProgress (0..1, the sweep climbing the renderer's world bounds) and _Dissolve (0..1, the
// 900 ms close: the solid fades over its first third, the lines unthread over 0.33..0.89, then the lens flares).
#ifndef MOON_LOFI_SCAN_INPUT_INCLUDED
#define MOON_LOFI_SCAN_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Where the dissolve's phases meet (D2): the solid is gone at the first, the lines at the second.
#define MOON_SCAN_SOLID_END 0.33
#define MOON_SCAN_LINES_END 0.89

// The point's height in its renderer's world bounds (0 at the bottom, 1 at the top).
float MoonScanHeight(float3 positionWS)
{
    float span = max(unity_RendererBounds_Max.y - unity_RendererBounds_Min.y, 1e-4);
    return saturate((positionWS.y - unity_RendererBounds_Min.y) / span);
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

// How faded a solid is (0 whole, 1 gone): the close's first third, or a separate fade (the dust outline's).
half MoonScanSolidFade(half dissolve, half fade)
{
    return max(fade, saturate(dissolve / MOON_SCAN_SOLID_END));
}

// How far the lines have unthreaded (0 standing, 1 gone).
half MoonScanUnthread(half dissolve)
{
    return saturate((dissolve - MOON_SCAN_SOLID_END) / (MOON_SCAN_LINES_END - MOON_SCAN_SOLID_END));
}

#endif
