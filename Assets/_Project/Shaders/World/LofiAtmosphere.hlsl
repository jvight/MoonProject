// The lofi atmosphere's shared fog term (M3-18 V6), for every lit surface of the world: a thin basin haze that lies
// in the low ground and thickens with distance, a slow desaturation of what is far away, then URP's exponential
// fog toward the horizon colour. Near ground keeps its contrast and colour; the far floor and the foot of the rim
// sink into a pale violet layer while the rim's crest, above the haze, keeps its lit edge.
// World sets the globals from AtmosphereSettings (WorldAtmosphere.Apply); include after URP's Core.hlsl.
#ifndef MOON_LOFI_ATMOSPHERE_INCLUDED
#define MOON_LOFI_ATMOSPHERE_INCLUDED

// rgb: haze colour (linear). a: most the haze may cover.
float4 _MoonHazeColor;
// x: haze density at its base (per metre of view ray), y: 1 / its height scale (metres), z: base height (world y).
float4 _MoonHaze;
// x: distance where things start to desaturate (metres), y: 1 / the distance over which they reach the most,
// z: the most they desaturate (0..1).
float4 _MoonDesaturate;

// How much of the ray from the camera to positionWS lies in the haze: the integral of density * exp(-height / scale)
// along the ray, analytic, so the haze costs a few instructions and no texture.
half MoonHazeAmount(float3 positionWS)
{
    float3 ray = positionWS - _WorldSpaceCameraPos;
    float distance = length(ray);
    float falloff = _MoonHaze.y;
    float cameraHeight = (_WorldSpaceCameraPos.y - _MoonHaze.z) * falloff;
    float rise = ray.y * falloff;
    // Mean of exp(-h) over the ray; the limit for a level ray is exp(-cameraHeight).
    float mean = abs(rise) > 1e-3
        ? (exp(-cameraHeight) - exp(-cameraHeight - rise)) / rise
        : exp(-cameraHeight);
    return (half)min(1.0 - exp(-_MoonHaze.x * distance * mean), _MoonHazeColor.a);
}

half3 MoonAtmosphere(half3 color, float3 positionWS, half fogFactor)
{
    float distance = length(positionWS - _WorldSpaceCameraPos);
    half desaturate = (half)(saturate((distance - _MoonDesaturate.x) * _MoonDesaturate.y) * _MoonDesaturate.z);
    color = lerp(color, Luminance(color).xxx, desaturate);
    color = lerp(color, (half3)_MoonHazeColor.rgb, MoonHazeAmount(positionWS));
    return MixFog(color, InitializeInputDataFog(float4(positionWS, 1.0), fogFactor));
}

#endif
