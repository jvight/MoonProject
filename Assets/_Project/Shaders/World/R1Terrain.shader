Shader "MoonProject/World/R1Terrain"
{
    Properties
    {
        [MainTexture] _BaseMap("Regolith Albedo (RGB)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _BumpMap("Regolith Normal Map", 2D) = "bump" {}
        [NoScaleOffset] _MaskMap("Regolith Mask (R=M, G=AO, B=Detail, A=Smoothness)", 2D) = "white" {}

        [NoScaleOffset] _RockMap("Exposed Basalt Albedo (RGB)", 2D) = "white" {}
        [NoScaleOffset] _RockBumpMap("Exposed Basalt Normal Map", 2D) = "bump" {}
        [NoScaleOffset] _RockMaskMap("Exposed Basalt Mask", 2D) = "white" {}

        _NearTiling("Near Tiling (uv/m)", Float) = 0.56
        _MacroTiling("Macro Tiling (uv/m)", Float) = 0.16
        _RockTiling("Triplanar Rock Tiling (uv/m)", Float) = 0.12
        _NearBumpScale("Near Normal Scale", Range(0.0, 2.0)) = 1.45
        _MacroBumpScale("Macro Normal Scale", Range(0.0, 2.0)) = 0.75
        _RockBumpScale("Triplanar Rock Normal Scale", Range(0.0, 2.0)) = 0.88
        _NearFadeStart("Near Detail Fade Start (m)", Float) = 38.0
        _NearFadeEnd("Near Detail Fade End (m)", Float) = 120.0
        _DetailAlbedoStrength("Regolith Albedo Modulation", Range(0.0, 0.6)) = 0.42
        _SmoothnessScale("Smoothness Scale", Range(0.0, 0.5)) = 0.15
        _OcclusionStrength("Micro-Cavity AO Strength", Range(0.0, 1.0)) = 0.78
        _SlopeThreshold("Steep Slope Threshold (Normal Y)", Range(0.3, 0.98)) = 0.88
        _SlopeBlendRange("Steep Slope Blend Width", Range(0.02, 0.4)) = 0.10
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Assets/_Project/Shaders/World/LofiAtmosphere.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);

            TEXTURE2D(_RockMap);
            SAMPLER(sampler_RockMap);
            TEXTURE2D(_RockBumpMap);
            SAMPLER(sampler_RockBumpMap);
            TEXTURE2D(_RockMaskMap);
            SAMPLER(sampler_RockMaskMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _NearTiling;
                float _MacroTiling;
                float _RockTiling;
                half _NearBumpScale;
                half _MacroBumpScale;
                half _RockBumpScale;
                float _NearFadeStart;
                float _NearFadeEnd;
                half _DetailAlbedoStrength;
                half _SmoothnessScale;
                half _OcclusionStrength;
                half _SlopeThreshold;
                half _SlopeBlendRange;
            CBUFFER_END

            // Power overlay reach ring set globally while the player holds a cable or build preview.
            // xyz = world center, w = radius (<= 0 disables the ring).
            float4 _MoonReachRing;
            half4 _MoonReachRingColor;

            // The lander's blast-swept zone (World's BlastZoneShaderGlobals). Zone: xy = centre (world x, z),
            // z = radius, w = edge wander (m). Halo: lift, falloff power, blotch size (m), blotch contrast. Rays: lift,
            // sectors, chance, long chance. Ray shape: length min, max, long (m), width min (m). Ground: width max (m),
            // level ground (normal y).
            float4 _MoonBlastZone;
            float4 _MoonBlastHalo;
            float4 _MoonBlastRays;
            float4 _MoonBlastRayShape;
            float4 _MoonBlastGround;

            // 0 in Earthlight (soft wrapped fill so craters don't go pitch black),
            // 1 in DirectSun (crisp lunar shadow contrast). Driven by SkyController.
            float _MoonSunlitGround;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.color = input.color;
                return output;
            }

            // Fast deterministic hash for non-repeating near-field micro-craters and grit clasts.
            float3 Hash23(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yxz + 33.33);
                return frac((p3.xxy + p3.yzz) * p3.zyx);
            }

            // Evaluates a soft 3D rover tyre rut pair along segment A -> B (gauge 1.36 m, rut half-width 0.21 m).
            void AccumulateHomeTyreRut(float2 wp, float2 a, float2 b, float curveAmp,
                inout half2 normDelta, inout half albedoMod, inout half aoMod)
            {
                float2 ab = b - a;
                float lenSq = max(dot(ab, ab), 1e-4);
                float len = sqrt(lenSq);
                float t = saturate(dot(wp - a, ab) / lenSq);
                float along = t * len;
                float endEnv = smoothstep(0.0, 0.45, along) * smoothstep(0.0, 1.4, len - along);
                if (endEnv <= 0.001)
                {
                    return;
                }

                float2 dir = ab / len;
                float2 side = float2(dir.y, -dir.x);
                float2 pLocal = wp - (a + ab * t);
                float crossDist = dot(pLocal, side) + sin(t * 3.14159265) * curveAmp;
                float sideSign = crossDist >= 0.0 ? 1.0 : -1.0;
                float signedFromRut = (abs(crossDist) - 0.68) / 0.20;
                float dRut = abs(signedFromRut);
                if (dRut < 1.45)
                {
                    // Depressed trough inside rut (dRut < 1) and soft pushed-up regolith berm on edges (1 <= dRut < 1.45)
                    float crossSlope = dRut < 1.0
                        ? (-signedFromRut * (1.0 - dRut * 0.35) * 0.90)
                        : (signedFromRut > 0.0 ? 1.0 : -1.0) * exp(-(dRut - 1.12) * (dRut - 1.12) * 18.0) * 0.30;
                    float cleatSlope = cos(along * 42.0) * exp(-dRut * dRut * 2.6) * 0.18;
                    float2 rutNormal2D = side * (sideSign * crossSlope) + dir * cleatSlope;
                    normDelta += (half2)(rutNormal2D * (0.42 * endEnv));

                    float trough = saturate(1.0 - dRut * dRut) * endEnv;
                    float berm = exp(-(dRut - 1.10) * (dRut - 1.10) * 20.0) * endEnv;
                    albedoMod *= (half)(1.0 - trough * 0.022 + berm * 0.016);
                    aoMod *= (half)(1.0 - trough * 0.06);
                }
            }

            // Diffuse response for punctual lights (07's headlamps, lander floodlights, beacons, pylon lamps)
            // with grazing micro-shadow and clod-face highlight so regolith relief reads crisply in eclipse lamplight.
            half3 PunctualDiffuse(
                Light light,
                half3 normalWS,
                half3 geomNormalWS,
                half microAo,
                half regolithHeight,
                half clodProminence,
                half regolithFade120,
                inout half ambientShadowCarve)
            {
                half geomNdotL = saturate(dot(geomNormalWS, light.direction));
                half ndotl = saturate(dot(normalWS, light.direction));
                half deltaNdotL = dot(normalWS, light.direction) - dot(geomNormalWS, light.direction);

                half grazingRatio = deltaNdotL / max(geomNdotL * 0.52h + 0.035h, 0.055h);
                half shadowSignal = grazingRatio + (regolithHeight - 0.50h) * 0.68h - (1.0h - microAo) * 0.88h;
                half microShadow = lerp(1.0h, smoothstep(-0.62h, 0.22h, shadowSignal), regolithFade120);
                half clodHighlight = 1.0h + saturate(grazingRatio) * (0.68h + 0.48h * clodProminence) * regolithFade120;

                float rawAtten = min(light.distanceAttenuation, 16.0) * light.shadowAttenuation;
                half lampWeight = saturate((half)(rawAtten * 2.2)) * regolithFade120;
                ambientShadowCarve = min(ambientShadowCarve, lerp(1.0h, lerp(0.28h, 1.0h, microShadow) * microAo, lampWeight));

                float atten = rawAtten * (float)(microShadow * clodHighlight * lerp(1.0h, microAo, 0.72h));
                return light.color * (half)(ndotl * atten);
            }

            // Subtle direct-light mineral micro-specular glint on lunar regolith.
            half3 RegolithMicroSpecular(Light light, half3 normalWS, half3 viewDirWS, half smoothness)
            {
                half3 halfDir = SafeNormalize(float3(light.direction) + float3(viewDirWS));
                half ndoth = saturate(dot(normalWS, halfDir));
                half ndotl = saturate(dot(normalWS, light.direction));
                half specPower = exp2(6.0h + smoothness * 18.0h);
                half spec = PositivePow(max(ndoth, 0.0001h), specPower) * (smoothness * 0.16h) * ndotl;
                float atten = min(light.distanceAttenuation, 16.0) * light.shadowAttenuation;
                half lightLuma = dot(light.color, half3(0.2126h, 0.7152h, 0.0722h));
                half3 coolLight = lerp(light.color, lightLuma * half3(0.95h, 0.99h, 1.03h), 0.88h);
                return coolLight * (half)(spec * atten);
            }

            float BlastHash(float2 cell)
            {
                return frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Smooth quintic C2 value noise in world metres (0..1), for organic patches, rays, and halo warping.
            float BlastNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 t = frac(p);
                t = t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
                float a = BlastHash(cell);
                float b = BlastHash(cell + float2(1.0, 0.0));
                float c = BlastHash(cell + float2(0.0, 1.0));
                float d = BlastHash(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, t.x), lerp(c, d, t.x), t.y);
            }

            // Soft 3D tyre ruts where 07 drives off the ChargingPad toward the lander and basin.
            // (Dense near-field small-crater stamps removed completely so no field of round holes appears.)
            half4 SampleNearTyreRuts(float2 wp, float camDist)
            {
                half microFade = 1.0h - (half)smoothstep(26.0, 58.0, camDist);
                if (microFade <= 0.001h || dot(wp, wp) >= 260.0)
                {
                    return half4(0.0h, 0.0h, 1.0h, 1.0h);
                }

                half2 normDelta = half2(0.0h, 0.0h);
                half albedoMod = 1.0h;
                half aoMod = 1.0h;

                AccumulateHomeTyreRut(wp, float2(1.80, -5.50), float2(1.10, -12.80), 0.18, normDelta, albedoMod, aoMod);
                AccumulateHomeTyreRut(wp, float2(1.75, -3.35), float2(0.15, 0.60), -0.16, normDelta, albedoMod, aoMod);

                normDelta *= microFade;
                albedoMod = lerp(1.0h, albedoMod, microFade);
                aoMod = lerp(1.0h, aoMod, microFade);
                return half4(normDelta, albedoMod, aoMod);
            }

            // Evaluates a sparse, ancient weathered medium/large lunar depression in a 2x2 cell neighborhood:
            // - Wide power-law size range (most cells empty, steep powerExp)
            // - Strongly noise-warped, irregular outline (never circular)
            // - Shallow bowl with BROKEN, discontinuous low rim segments (never a full ring or bright polka-dot)
            // - Soft, low-contrast normal shading and zero dark cavity dots.
            void AccumulateWeatheredCrater2x2(
                float2 coord,
                float2 wpWorld,
                float2 rotCosSin,
                float occupancy,
                float radMin,
                float radMax,
                float powerExp,
                float normalStrength,
                inout half2 normDelta,
                inout half aoMod)
            {
                float2 startCell = floor(coord - 0.5);
                [unroll]
                for (int iy = 0; iy <= 1; iy++)
                {
                    [unroll]
                    for (int ix = 0; ix <= 1; ix++)
                    {
                        float2 cell = startCell + float2(ix, iy);
                        float3 h = Hash23(cell);
                        if (h.z >= occupancy)
                        {
                            continue;
                        }

                        // Full-cell random offset so sparse craters never align to a grid
                        float2 jitter = float2(frac(h.x * 13.37 + h.y * 5.19), frac(h.y * 17.73 + h.z * 9.41)) - 0.5;
                        float2 craterCenter = cell + 0.5 + jitter * 0.78;
                        float2 d = coord - craterCenter;

                        // Asymmetric elongation + random orientation
                        float angle = h.x * 6.2831853;
                        float ca = cos(angle), sa = sin(angle);
                        float2 dRot = float2(d.x * ca - d.y * sa, d.x * sa + d.y * ca);
                        dRot.x *= lerp(0.68, 1.38, frac(h.y * 11.13));
                        dRot.y *= lerp(0.74, 1.28, frac(h.z * 7.91));

                        // Power-law radius selection across a wide size range
                        float sizePick = pow(frac(h.x * 7.13 + h.y * 3.71 + h.z * 5.39), powerExp);
                        float rad = lerp(radMin, radMax, sizePick);

                        // Domain-warp the local coordinates with multi-octave world noise so the outline is scalloped and irregular
                        float2 noiseSeed = cell * 7.13 + float2(h.x * 19.3, h.y * 31.7);
                        float warpA = BlastNoise(wpWorld * 0.14 + noiseSeed) - 0.5;
                        float warpB = BlastNoise(wpWorld * 0.31 + noiseSeed.yx + float2(11.4, 5.8)) - 0.5;
                        float warpC = BlastNoise(wpWorld * 0.065 + noiseSeed * 0.5) - 0.5;
                        dRot += float2(warpA * 0.34 + warpB * 0.16, warpC * 0.34 - warpA * 0.18) * rad;

                        // Also warp effective radius around the perimeter so it is never a circle
                        float shapeMod = 1.0 + warpA * 0.32 + warpB * 0.22 + warpC * 0.24;
                        float r = max(length(dRot), 1e-4);
                        float u = r / max(rad * shapeMod, 1e-3);
                        if (u >= 1.65)
                        {
                            continue;
                        }

                        // Broken rim mask: over half of the perimeter is slumped/eroded flat (no continuous ring)
                        float rimNoise = BlastNoise(wpWorld * 0.19 + noiseSeed * 1.3) * 0.62
                                       + BlastNoise(wpWorld * 0.43 + noiseSeed.yx * 1.9) * 0.38;
                        float brokenRimMask = smoothstep(0.52, 0.78, rimNoise);

                        // Also break up the inner wall slope irregularly so shading is soft and non-uniform
                        float wallSlump = lerp(0.42, 1.0, smoothstep(0.25, 0.80, warpC + 0.5));

                        // Gently dished shallow profile: soft broad interior depression (u < 0.92),
                        // transitioning into a low, broken outer rim (0.92 <= u < 1.65) that only exists where brokenRimMask > 0.
                        float bowlSlope = 0.0;
                        if (u < 0.92)
                        {
                            float t = u / 0.92;
                            // Smooth bell derivative: 0 at center, gentle peak mid-wall, 0 at rim crest
                            bowlSlope = -sin(t * 3.14159265) * (1.0 - 0.25 * t) * 0.52 * wallSlump;
                        }
                        else
                        {
                            float w = saturate((u - 0.92) / 0.73);
                            bowlSlope = sin(w * 3.14159265) * (1.0 - w * 0.45) * 0.24 * brokenRimMask;
                        }

                        // Un-rotate local gradient direction back to world XZ
                        float2 dirLocal = dRot / r;
                        float2 dirCell = float2(dirLocal.x * ca + dirLocal.y * sa, -dirLocal.x * sa + dirLocal.y * ca);
                        float2 dirWS = float2(
                            dirCell.x * rotCosSin.x + dirCell.y * rotCosSin.y,
                            -dirCell.x * rotCosSin.y + dirCell.y * rotCosSin.x);

                        float sizeWeight = lerp(0.55, 1.0, sizePick);
                        normDelta += (half2)(dirWS * (bowlSlope * normalStrength * sizeWeight));

                        // Very gentle interior AO only (no albedo rim brightening, no dark cavity dots)
                        float softInterior = saturate(1.0 - u * u) * wallSlump;
                        aoMod *= (half)(1.0 - softInterior * 0.035 * sizeWeight);
                    }
                }
            }

            // One faint ray in sector `sector` of `sectors` round the lander, or 0: some sectors hold one, at a random
            // bearing in the sector, starting at a random radius, a random length (mostly short, a few long) and
            // width, tapered at both ends and across.
            float BlastRay(float sector, float sectors, float bearing, float radius, float zoneRadius)
            {
                float wrapped = fmod(sector + sectors, sectors);
                float seed = wrapped * 7.31 + 3.7;
                if (BlastHash(float2(seed, 1.0)) > _MoonBlastRays.z)
                {
                    return 0.0;
                }

                float rayBearing = (sector + BlastHash(float2(seed, 2.0))) / sectors * 6.2831853;
                float span = rayBearing - bearing;
                span -= 6.2831853 * round(span / 6.2831853);
                float lengthPick = BlastHash(float2(seed, 3.0));
                bool longRay = BlastHash(float2(seed, 4.0)) < _MoonBlastRays.w;
                float rayLength = longRay ? _MoonBlastRayShape.z * (0.75 + 0.5 * lengthPick)
                    : lerp(_MoonBlastRayShape.x, _MoonBlastRayShape.y, lengthPick);
                float start = BlastHash(float2(seed, 5.0)) * max(0.0, zoneRadius * 0.85 - rayLength);
                float halfWidth = 0.5 * lerp(_MoonBlastRayShape.w, _MoonBlastGround.x, BlastHash(float2(seed, 6.0)));
                float along = saturate((radius - start) / max(rayLength, 0.01));
                float taper = sin(along * 3.14159265) * step(start, radius) * step(radius, start + rayLength);
                float across = abs(span) * radius / max(halfWidth * taper, 0.001);
                return taper * saturate(1.0 - across);
            }

            // The lander's descent engine smoothed the regolith round it (World's BlastZoneSettings): a soft, diffuse
            // brightening toward the lander with a blotchy, never-round edge, crossed by a few faint, short, uneven
            // rays; only on level ground, nothing beyond the zone. 1 = untouched.
            half BlastSweepTone(float3 positionWS, half normalY)
            {
                float2 fromLander = positionWS.xz - _MoonBlastZone.xy;
                float radius = length(fromLander);
                float reach = _MoonBlastZone.z + _MoonBlastZone.w;
                if (_MoonBlastZone.z <= 0.001 || radius >= reach)
                {
                    return 1.0h;
                }

                float2 noiseAt = positionWS.xz / max(_MoonBlastHalo.z, 0.1);
                float blotch = BlastNoise(noiseAt) * 0.65 + BlastNoise(noiseAt * 2.3 + 11.0) * 0.35;
                float edge = _MoonBlastZone.z + (blotch - 0.5) * 2.0 * _MoonBlastZone.w;
                float share = saturate(1.0 - radius / max(edge, 0.1));
                float halo = pow(share, _MoonBlastHalo.y) * (1.0 - _MoonBlastHalo.w * (1.0 - blotch));

                float sectors = max(1.0, round(_MoonBlastRays.y));
                float bearing = atan2(fromLander.x, fromLander.y);
                float sector = floor((bearing < 0.0 ? bearing + 6.2831853 : bearing) / 6.2831853 * sectors);
                float ray = max(BlastRay(sector, sectors, bearing, radius, edge),
                    max(BlastRay(sector - 1.0, sectors, bearing, radius, edge),
                        BlastRay(sector + 1.0, sectors, bearing, radius, edge)));

                half level = (half)smoothstep(_MoonBlastGround.y, 1.0, normalY);
                return 1.0h + level * (half)(_MoonBlastHalo.x * halo + _MoonBlastRays.x * ray * share);
            }

            // Scatters discrete, short, discontinuous angular broken rocky ledges in a 2x2 cell neighborhood.
            // Each rock outcrop is an asymmetric stepped rock block (upward-tilted upper bench + steep outward-tilted
            // rock scarp broken by vertical cleavage joints), with NO dark perimeter ring so it reads as 3D fractured rock.
            void AccumulateBrokenRockLedges2x2(
                float2 ledgeCoord,
                float2 wpRot,
                float occupancy,
                inout half scarpFaceOut,
                inout half ledgeLipOut,
                inout half creviceOut)
            {
                float2 startCell = floor(ledgeCoord - 0.5);
                [unroll]
                for (int iy = 0; iy <= 1; iy++)
                {
                    [unroll]
                    for (int ix = 0; ix <= 1; ix++)
                    {
                        float2 cell = startCell + float2(ix, iy);
                        float3 h = Hash23(cell);
                        if (h.z >= occupancy)
                        {
                            continue;
                        }

                        // Full 2D random offset in both strike and elevation so ledges stagger organically
                        float2 jitter = float2(frac(h.x * 13.37 + h.y * 5.19), frac(h.y * 17.73 + h.z * 9.41)) - 0.5;
                        float2 center = cell + 0.5 + jitter * float2(0.72, 0.80);
                        float2 d = ledgeCoord - center;

                        // Per-outcrop dip tilt (-9 deg to +9 deg) + angular craggy rock displacement
                        float tilt = (h.x - 0.5) * 0.30;
                        float cragA = BlastNoise(wpRot * 0.22 + cell * 3.7) - 0.5;
                        float cragB = BlastNoise(wpRot * 0.55 + cell * 7.3) - 0.5;
                        float2 dTilted = float2(d.x + cragA * 0.08, d.y + d.x * tilt + cragA * 0.12 + cragB * 0.08);

                        float radX = lerp(0.22, 0.42, frac(h.y * 9.3));
                        float uX = dTilted.x / radX;
                        if (abs(uX) >= 1.0)
                        {
                            continue;
                        }

                        // Blocky trapezoidal horizontal slab envelope broken by sharp vertical cleavage joints
                        float envX = saturate((1.0 - abs(uX)) * 2.6);
                        float jointWave = abs(frac(uX * lerp(1.2, 2.2, h.z) + h.x * 3.1 + cragB * 0.35) - 0.5);
                        half jointBreak = (half)smoothstep(0.05, 0.14, jointWave);

                        float radY = lerp(0.14, 0.26, frac(h.x * 11.7));
                        float localHalfY = radY * (0.45 + 0.55 * envX);
                        float uY = dTilted.y / max(localHalfY, 1e-3);
                        if (abs(uY) >= 1.25)
                        {
                            continue;
                        }

                        // Asymmetric stepped rock profile:
                        // - Upper shelf/lip (uY in [0.05, 0.95]) tilts upward to catch grazing sunlight
                        // - Lower steep scarp face (uY in [-0.95, 0.25]) tilts outward/downward as exposed rock
                        half lip = (half)(smoothstep(0.05, 0.38, uY) * (1.0 - smoothstep(0.55, 1.05, uY)) * envX) * jointBreak;
                        half scarp = (half)(smoothstep(-0.95, -0.35, uY) * (1.0 - smoothstep(0.05, 0.45, uY)) * envX) * jointBreak;
                        half jointCrevice = (half)(smoothstep(-0.60, 0.20, uY) * (1.0 - smoothstep(0.20, 0.70, uY)) * envX) * (1.0h - jointBreak);

                        scarpFaceOut = max(scarpFaceOut, scarp);
                        ledgeLipOut = max(ledgeLipOut, lip);
                        creviceOut = max(creviceOut, jointCrevice);
                    }
                }
            }

            // Procedural crater normal bowls are disabled in the shader so no fields of round depressions or pocks
            // are stamped across the plain (height relief is owned by GroundShaping).
            half4 SampleWeatheredCraters(float3 wp3, half3 geomNormalWS, half slopeFactor, float camDist)
            {
                return half4(0.0h, 0.0h, 1.0h, 1.0h);
            }

            // Large-scale organic noise-warped albedo patches (50–300 m): darker basalt-mare regolith regions and
            // lighter highland-ejecta regions with soft gradients and low contrast.
            half3 SampleMacroRegolithVariation(float3 wp3, half slopeFactor)
            {
                float2 wp = wp3.xz;

                // Three incommensurate rotated frames prevent axis alignment or grid patterns
                float2 wpRotA = float2(wp.x * 0.8290 - wp.y * 0.5592, wp.x * 0.5592 + wp.y * 0.8290);
                float2 wpRotB = float2(wp.x * 0.2924 + wp.y * 0.9563, -wp.x * 0.9563 + wp.y * 0.2924);
                float2 wpRotC = float2(wp.x * 0.6293 + wp.y * 0.7771, -wp.x * 0.7771 + wp.y * 0.6293);

                // Two-stage domain warp so 50–300 m mare and ejecta regions flow organically with soft gradients
                float2 warpBroad = float2(
                    BlastNoise(wpRotA * 0.0052 + float2(13.2, 7.9)) - 0.5,
                    BlastNoise(wpRotB * 0.0052 + float2(29.1, 18.4)) - 0.5) * 98.0;
                float2 warpMed = float2(
                    BlastNoise((wpRotB + warpBroad * 0.45) * 0.0130 + float2(41.7, 11.3)) - 0.5,
                    BlastNoise((wpRotC + warpBroad * 0.45) * 0.0130 + float2(6.4, 53.8)) - 0.5) * 44.0;

                float2 pLarge = wp + warpBroad + warpMed * 0.40;
                float2 pMed = wpRotA + warpBroad * 0.65 + warpMed;
                float2 pSmall = wpRotC + warpBroad * 0.35 + warpMed * 0.85;

                // Scale 1 (~240–300 m): broad basalt-mare vs highland-ejecta provinces
                float n260 = BlastNoise(float2(pLarge.x * 0.0042, pLarge.y * 0.0052) + float2(14.8, 26.3));
                // Scale 2 (~110–150 m): intermediate organic regolith units
                float n130 = BlastNoise(float2(pMed.x * 0.0086, pMed.y * 0.0106) + float2(19.6, 8.3));
                // Scale 3 (~55–75 m): smaller organic ejecta lobes and mare bays
                float n65 = BlastNoise(float2(pSmall.x * 0.0165, pSmall.y * 0.0205) + float2(7.2, 34.1));
                // Scale 4 (~420 m): broad mountain/backdrop tonal variation surviving past 1 km
                float n420 = BlastNoise(wp * 0.0024 + float2(7.8, 43.1));

                float rawField = (n260 - 0.50) * 0.48 + (n130 - 0.50) * 0.36 + (n65 - 0.50) * 0.28;
                float patch01 = smoothstep(-0.135, 0.135, rawField);

                // Low-contrast modulation between darker basalt-mare regolith and lighter highland-ejecta
                float floorPatchMod = lerp(0.75, 1.23, patch01);
                float mountainMod = 1.0 + (n420 - 0.5) * 0.14 + saturate(wp3.y / 115.0) * 0.06;
                half patchScalar = (half)clamp(lerp(floorPatchMod, mountainMod, slopeFactor * 0.68), 0.75, 1.24);

                // Subtle cool-grey mineral nuance (no beige, no blue tint)
                half3 patchTone = lerp(half3(0.992h, 0.998h, 1.006h), half3(1.004h, 1.001h, 0.995h), (half)patch01);
                return patchScalar * lerp(patchTone, half3(1.0h, 1.0h, 1.0h), slopeFactor * 0.70h);
            }

            // Evaluates a single faint, long, slightly wavy, tapering, broken bright ray from a far-away (off-map)
            // fresh crater (~2.16 km WSW at (-2100, -520); no ring and no centre exist or are ever visible).
            float EvaluateDistantCraterRay(
                float2 wp,
                float rayAngle,
                float startDist,
                float endDist,
                float maxHalfWidth,
                float seed)
            {
                float2 dir = float2(cos(rayAngle), sin(rayAngle));
                float2 perp = float2(-dir.y, dir.x);
                float2 rel = wp - float2(-2100.0, -520.0);

                float along = dot(rel, dir);
                if (along <= startDist || along >= endDist)
                {
                    return 0.0;
                }

                float across = dot(rel, perp);
                float u = saturate((along - startDist) / (endDist - startDist));
                float lengthEnv = sin(pow(u, 0.78) * 3.14159265);

                // Gentle multi-scale lateral waviness along the ray's path
                float wave = (BlastNoise(float2(along * 0.0036 + seed, seed * 1.7)) - 0.5) * 26.0
                           + (BlastNoise(float2(along * 0.0088 + seed * 2.3, seed * 3.1)) - 0.5) * 9.0;
                float dAcross = across - wave;

                // Width tapers from WSW toward ENE and breathes gently along its length
                float widthMod = 0.80 + 0.42 * BlastNoise(float2(along * 0.0068 + seed * 4.1, seed * 0.9));
                float taper = lerp(1.10, 0.36, u) * smoothstep(0.0, 0.10, u) * smoothstep(1.0, 0.85, u);
                float halfWidth = max(maxHalfWidth * taper * widthMod, 1.5);

                float normAcross = abs(dAcross) / halfWidth;
                if (normAcross >= 1.0)
                {
                    return 0.0;
                }

                // Discontinuous breaks along the ray's length plus 10:1 anisotropic feathery ejecta streaks (never dots)
                float segNoise = BlastNoise(float2(along * 0.0115 + seed * 5.3, dAcross * 0.015 + seed * 2.1)) * 0.65
                               + BlastNoise(float2(along * 0.0240 + seed * 8.7, dAcross * 0.032 - seed * 1.4)) * 0.35;
                float brokenMask = smoothstep(0.36, 0.54, segNoise);

                float filament = BlastNoise(float2(along * 0.012 + seed * 11.1, dAcross * 0.13 + seed * 6.7)) * 0.65
                               + BlastNoise(float2(along * 0.026 - seed * 3.9, dAcross * 0.24 + seed * 4.3)) * 0.35;
                float streakDetail = 0.78 + 0.32 * smoothstep(0.28, 0.72, filament);

                float crossProfile = 1.0 - normAcross;
                crossProfile = crossProfile * crossProfile * (3.0 - 2.0 * crossProfile);
                return lengthEnv * crossProfile * brokenMask * streakDetail;
            }

            // Four faint, broken bright rays crossing the basin from a far-away off-map fresh crater.
            half SampleDistantCraterRays(float3 wp3, half normalY)
            {
                float2 wp = wp3.xz;
                // Only on the basin plain and gentle lower slopes; nothing on steep mountain walls or far backdrop
                half levelGround = (half)smoothstep(0.74, 0.92, normalY);
                half basinEnv = (half)(1.0 - smoothstep(320.0, 460.0, length(wp)));
                if (levelGround * basinEnv <= 0.001h)
                {
                    return 1.0h;
                }

                float r0 = EvaluateDistantCraterRay(wp, 0.1750, 1700.0, 2580.0, 26.0, 5.8);
                float r1 = EvaluateDistantCraterRay(wp, 0.2580, 1720.0, 2600.0, 25.0, 12.3);
                float r2 = EvaluateDistantCraterRay(wp, 0.3080, 1710.0, 2620.0, 28.0, 7.1);
                float r3 = EvaluateDistantCraterRay(wp, 0.3540, 1740.0, 2580.0, 22.0, 1.3);
                float rayMask = max(max(r0, r1), max(r2, r3));

                // Softly attenuate across the compacted home pad center
                float padFade = lerp(0.45, 1.0, smoothstep(8.0, 24.0, length(wp)));
                return 1.0h + (half)(rayMask * 0.25 * padFade) * levelGround * basinEnv;
            }

            // Crisp lunar terminator shading on ridges and rock ledges, plus rich grazing micro-shadows across near-field regolith clods and dimples.
            half3 MainSunDiffuseStylized(
                Light light,
                half3 normalWS,
                half3 geomNormalWS,
                half3 viewDirWS,
                half sunlit,
                half microAo,
                half slopeFactor,
                half ridgeCrestMask,
                half regolithShadowWeight,
                half regolithHeight)
            {
                half ndotl = dot(normalWS, light.direction);
                half geomNdotL = dot(geomNormalWS, light.direction);
                half deltaNdotL = ndotl - geomNdotL;

                // Relative grazing slope tilt: leeward sides of clods, crusty ridges, and dimple walls tilt away from the sun (grazingSlope < 0)
                // and cast rich, deep little micro-shadows when the sun is low.
                half grazingDenom = max(abs(geomNdotL) * 0.52h + 0.032h, 0.048h);
                half grazingSlope = deltaNdotL / grazingDenom;
                half dimpleDepth = saturate((0.49h - regolithHeight) * 2.2h);
                half cavityDepth = saturate((0.84h - microAo) * 2.4h);
                half shadowSignal = grazingSlope - dimpleDepth * 0.22h - cavityDepth * 0.20h;
                half clodLeeShadow = smoothstep(0.04h, -0.38h, shadowSignal) * regolithShadowWeight;
                half clodSunCatch = smoothstep(-0.02h, 0.48h, grazingSlope + saturate(regolithHeight - 0.50h) * 0.35h) * regolithShadowWeight;

                // In Earthlight / eclipse (sunlit == 0), keep subtle clod/dimple relief readable while mountain slopes stay crisp.
                half earthWrap = saturate(ndotl * 0.84h + 0.16h);
                half earthReliefGain = lerp(0.55h + 0.60h * regolithShadowWeight, 1.55h, slopeFactor);
                half earthRelief = clamp(deltaNdotL * earthReliefGain, -0.46h, 0.58h);
                half earthDiffuse = saturate(pow(earthWrap, 1.15h) * 1.06h + earthRelief);

                // At low sun (sunlit > 0), the 0.5-deg solar disc carves a crisp terminator across ridges, ledges, and regolith clods.
                half terminatorKnee = smoothstep(-0.006h, 0.032h, ndotl);
                half grazingContrast = pow(saturate(ndotl + 0.005h), 1.08h) * terminatorKnee;

                // Lunar opposition / Lommel-Seeliger balance on flat floors while slopes keep crisp relief contrast.
                half ndotv = max(0.22h, saturate(dot(normalWS, viewDirWS)));
                half lommel = grazingContrast / max(0.36h, grazingContrast + ndotv * 0.40h);
                half sunDiffuse = lerp(lommel * 1.04h, grazingContrast * 1.24h, saturate(slopeFactor * 0.75h + 0.25h));

                // Accentuate grazing sunlit ridge crests, rock-ledge lips, and sunlit clod tops.
                half terminatorZone = smoothstep(-0.04h, 0.06h, geomNdotL) * (1.0h - smoothstep(0.06h, 0.40h, geomNdotL));
                half ridgeCrestBoost = saturate(deltaNdotL) * (0.35h + terminatorZone * 0.52h) * lerp(0.22h + 0.48h * regolithShadowWeight, 1.0h, slopeFactor);
                sunDiffuse += ridgeCrestBoost + clodSunCatch * 0.24h;

                half highlandDiffuse = lerp(earthDiffuse, sunDiffuse, sunlit);

                // Crisp terminator step with smooth highlight roll-off
                half sunStep = smoothstep(0.001h, 0.034h, ndotl);
                half earthStep = smoothstep(0.02h, 0.12h, earthWrap);
                half terminatorStep = lerp(earthStep, sunStep, sunlit);
                half rampInput = lerp(earthWrap, saturate(ndotl), sunlit);
                half midtoneRoll = smoothstep(0.04h, 0.46h, rampInput);
                half highlightRoll = smoothstep(0.38h, 0.86h, rampInput);
                half painterlyRamp = terminatorStep * (0.38h + midtoneRoll * 0.38h + highlightRoll * 0.28h);

                // Graphic rim-light along convex ridge crests and broken ledge lips
                half rimGrazing = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 2.2h) * smoothstep(-0.04h, 0.14h, ndotl);
                half painterlyRidgeStroke = (ridgeCrestMask * smoothstep(-0.02h, 0.10h, ndotl) * 0.28h) + rimGrazing * slopeFactor * 0.16h;
                half stylizedDiffuse = lerp(highlandDiffuse, painterlyRamp, 0.42h) + painterlyRidgeStroke + clodSunCatch * 0.12h * sunlit;

                // Attenuate leeward flanks of clods and dimples so low sun carves rich, deep little shadows across the regolith plain.
                half clodShadowAtten = lerp(1.0h, lerp(0.48h, 0.08h, sunlit), clodLeeShadow);

                // Micro-cavity AO self-shadows clod bases, irregular dimples, broken rock ledges, and cleavage joints.
                half directCavity = lerp(1.0h, microAo, 0.52h + 0.32h * sunlit);
                half diffuse = stylizedDiffuse * clodShadowAtten * directCavity;

                // Crisp lunar shadows in DirectSun (shadowFloor = 0.042) so shadowed slopes stay dark while retaining subtle form
                half shadowFloor = lerp(0.18h, 0.042h, sunlit);
                half atten = max(light.shadowAttenuation, shadowFloor) * light.distanceAttenuation;

                // Neutralize warm golden-hour / dawn sun tint so lunar regolith reads as subtle cool grey (no beige, no blue tint)
                half lightLuma = dot(light.color, half3(0.2126h, 0.7152h, 0.0722h));
                half3 lunarLightColor = lerp(light.color, lightLuma * half3(0.985h, 0.998h, 1.010h), 0.94h);
                return lunarLightColor * (diffuse * atten);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half3 rawGeomNormalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float camDist = distance(_WorldSpaceCameraPos, input.positionWS);
                half sunlit = saturate((half)_MoonSunlitGround);

                // Separate the flat interior basin floor from the surrounding mountain rim / hills / canyon walls so
                // small mesh depressions on the basin floor never trigger steep rock-scarp darkening or sharp hole shadows.
                half mountainMask = saturate(
                    (half)smoothstep(5.5, 16.0, input.positionWS.y) +
                    (half)smoothstep(285.0, 345.0, length(input.positionWS.xz)));
                half3 geomNormalWS = normalize(lerp(rawGeomNormalWS, half3(0.0h, 1.0h, 0.0h), (1.0h - mountainMask) * 0.78h));

                // Regolith near-field weights:
                // - Strong near-field regolith relief in 0–40 m (where low sun and 07's headlamps make clods/dimples read)
                // - Smooth fade to the existing macro look by ~120 m
                // - Extra fine micro-crust clarity within 0–20 m for low close-up views (1.5 m up) and immediate chase foreground
                half regolithClose20 = 1.0h - (half)smoothstep(6.0, 20.0, camDist);
                half regolithNear40 = 1.0h - (half)smoothstep(28.0, 46.0, camDist);
                half regolithFade120 = 1.0h - (half)smoothstep(38.0, 120.0, camDist);
                half nearWeight = max(
                    1.0h - (half)smoothstep(_NearFadeStart, max(_NearFadeStart + 1.0, _NearFadeEnd), camDist),
                    regolithFade120);
                half midDetailWeight = 1.0h - (half)smoothstep(220.0, 720.0, camDist);
                half macroWeight = 1.0h - (half)smoothstep(720.0, 1550.0, camDist);
                half hillDetailWeight = 1.0h - (half)smoothstep(900.0, 1850.0, camDist);

                // Slope masks: gentle slopes keep lighter regolith, steep faces expose darker basalt rock and broken ledges.
                half slopeFactor = (1.0h - (half)smoothstep(0.75h, 0.945h, geomNormalWS.y)) * mountainMask;
                half steepFaceMask = (1.0h - (half)smoothstep(0.68h, 0.855h, geomNormalWS.y)) * mountainMask;
                half gentleSlopeMask = (half)smoothstep(0.80h, 0.92h, geomNormalWS.y);

                // Neutral cool grey lunar regolith base reflectance (~0.12-0.16 linear).
                // Preserve home-pad and Kestrel-scorch vertex darkening while suppressing CraterField vertex pockmarks on the open plain.
                half3 vertexLinear = SRGBToLinear(input.color.rgb) * _BaseColor.rgb;
                half vLuma = dot(vertexLinear, half3(0.2126h, 0.7152h, 0.0722h));
                half padOrScorchWeight = max(
                    (half)(1.0 - smoothstep(18.0, 32.0, length(input.positionWS.xz))),
                    (half)(1.0 - smoothstep(8.0, 16.0, length(input.positionWS.xz - float2(-12.0, 16.0)))));
                half vertexToneWeight = max(padOrScorchWeight, mountainMask);
                half vNorm = lerp(0.66h, smoothstep(0.025h, 0.22h, vLuma), vertexToneWeight);
                half lunarReflectance = lerp(0.118h, 0.160h, vNorm);

                // Calibrated neutral-cool grey compensation: counteracts post-processing warm split-toning (_splitHighlights / _gain)
                // without over-pushing blue, so regolith stays a subtle cool grey with no beige and no blue tint.
                half3 coolNeutralTint = lerp(half3(0.975h, 0.995h, 1.005h), half3(0.935h, 0.992h, 1.042h), sunlit);
                half3 baseAlbedo = lunarReflectance * coolNeutralTint;

                // Large-scale 50–300 m organic mare/ejecta albedo patches + faint broken rays from a far-away off-map crater.
                baseAlbedo *= SampleMacroRegolithVariation(input.positionWS, slopeFactor);
                baseAlbedo *= SampleDistantCraterRays(input.positionWS, geomNormalWS.y);

                half3 normalWS = geomNormalWS;
                half microAo = 1.0h;
                half smoothness = 0.04h;
                half ridgeCrestMask = 0.0h;
                half regolithShadowWeight = 0.0h;
                half regolithHeight = 0.50h;
                half clodProminence = 0.0h;

                // Construct world-space tangent frame aligned with +X (U) and +Z (V) around the smooth terrain normal.
                half3 tangentWS = normalize(half3(1.0h, 0.0h, 0.0h) - geomNormalWS * geomNormalWS.x);
                half3 bitangentWS = normalize(cross(geomNormalWS, tangentWS));

                half4 weatheredCraters = SampleWeatheredCraters(input.positionWS, geomNormalWS, slopeFactor, camDist);
                baseAlbedo *= weatheredCraters.z;
                microAo = min(microAo, weatheredCraters.w);

                // Slope geological structure:
                // - Gentle slopes and talus chutes hold lighter cool-grey regolith.
                // - Steep faces expose darker fractured basalt rock.
                // - Strata appear as BROKEN rocky ledges (short, discontinuous 2x2 scattered rock outcrops, never continuous contour lines).
                UNITY_BRANCH
                if (slopeFactor > 0.005h && hillDetailWeight > 0.005h)
                {
                    float3 wp3 = input.positionWS;
                    float2 wp2 = wp3.xz;
                    float2 wpRot1 = float2(wp2.x * 0.8192 - wp2.y * 0.5736, wp2.x * 0.5736 + wp2.y * 0.8192);
                    float2 wpRot2 = float2(wp2.x * 0.5299 + wp2.y * 0.8480, -wp2.x * 0.8480 + wp2.y * 0.5299);

                    float radialAngle = atan2(wp2.x, wp2.y);

                    // 1. Downslope regolith talus aprons & chutes vs rocky spur ribs
                    float chuteAzimuth = radialAngle * 24.0
                        + (BlastNoise(wpRot1 * 0.016 + float2(7.1, 19.3)) - 0.5) * 3.6
                        + (BlastNoise(wpRot2 * 0.038 + float2(13.4, 5.8)) - 0.5) * 1.6;
                    float chuteNoise = BlastNoise(float2(chuteAzimuth, wp3.y * 0.012 + 3.7)) * 0.62
                        + BlastNoise(float2(chuteAzimuth * 2.1 + 11.4, wp3.y * 0.028)) * 0.38;
                    half talusChute = (half)smoothstep(0.42, 0.70, chuteNoise);

                    // Irregular rocky outcrop patches on steeper faces
                    float patchNoise = BlastNoise(wpRot1 * 0.030 + float2(wp3.y * 0.024, 11.7)) * 0.60
                                     + BlastNoise(wpRot2 * 0.072 + float2(19.3, wp3.y * 0.052)) * 0.40;
                    half rockPatch = (half)smoothstep(0.34, 0.64, patchNoise);

                    // 2. Short, discontinuous, staggered broken rocky ledges via 2x2 cell scatter (zero continuous lines)
                    half strataDistFade = 1.0h - (half)smoothstep(220.0, 520.0, camDist);
                    float faultBroad = (BlastNoise(wpRot1 * 0.012 + float2(13.7, 29.3)) - 0.5) * 14.0;
                    float faultMid = (BlastNoise(wpRot2 * 0.028 + float2(41.9, 7.1)) - 0.5) * 5.8;
                    float yBeddingBase = wp3.y + wp3.x * 0.04 - wp3.z * 0.03 + faultBroad + faultMid;

                    float ledgeOccupancy = lerp(0.16, 0.54, saturate((float)rockPatch * 0.65 + (float)steepFaceMask * 0.55));

                    half majScarp = 0.0h, majLip = 0.0h, majCrevice = 0.0h;
                    // Family A: Major short broken rock ledges (~16 m horizontal cell x ~7.5 m vertical cell -> 5-10 m ledge segments)
                    AccumulateBrokenRockLedges2x2(
                        float2(wpRot1.x * 0.062 + wpRot2.y * 0.026 + faultMid * 0.04, yBeddingBase * 0.134),
                        wpRot1,
                        ledgeOccupancy,
                        majScarp, majLip, majCrevice);

                    half minScarp = 0.0h, minLip = 0.0h, minCrevice = 0.0h;
                    // Family B: Secondary short broken rock ledges (~9.5 m horizontal cell x ~4.4 m vertical cell -> 3-6 m ledge segments)
                    AccumulateBrokenRockLedges2x2(
                        float2(wpRot2.x * 0.104 - wpRot1.y * 0.038 + 19.7, (yBeddingBase + faultMid * 0.65) * 0.228 + 7.3),
                        wpRot2,
                        ledgeOccupancy * 0.88,
                        minScarp, minLip, minCrevice);

                    half minorWeight = strataDistFade * 0.78h;
                    // Ledges crop out on rocky/steep faces and break up inside regolith-filled talus chutes
                    half ledgeSlopeGate = (half)smoothstep(0.14, 0.48, slopeFactor)
                        * (half)smoothstep(0.18, 0.58, rockPatch + steepFaceMask * 0.50h)
                        * (1.0h - talusChute * 0.80h);

                    half terraceRiser = saturate(majScarp + minScarp * minorWeight) * ledgeSlopeGate;
                    half terraceBench = saturate(majLip + minLip * minorWeight) * ledgeSlopeGate * (1.0h - terraceRiser * 0.40h);
                    half rockJoint = saturate(majCrevice + minCrevice * minorWeight) * ledgeSlopeGate;

                    // 3. Exposed dark rock mask on steep faces/ledges vs lighter regolith mask on gentle slopes/chutes
                    half exposedRockMask = saturate((steepFaceMask * (0.76h + 0.24h * rockPatch) + terraceRiser * 0.85h + slopeFactor * rockPatch * (1.0h - gentleSlopeMask) * 0.54h) * (1.0h - talusChute * 0.68h) * hillDetailWeight);
                    half gentleRegolithMask = saturate((gentleSlopeMask * 0.88h + talusChute * 0.78h) * (1.0h - exposedRockMask * 0.86h) * hillDetailWeight);

                    // Triplanar basalt rock texture + normal map on exposed steep rock and broken ledges
                    half3 blendWeights = abs(geomNormalWS);
                    blendWeights = pow(max(blendWeights, 0.0001h), 4.0h);
                    blendWeights /= max(blendWeights.x + blendWeights.y + blendWeights.z, 1e-4h);

                    float3 rockUV = wp3 * _RockTiling;
                    half3 rockAlbX = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV.zy).rgb;
                    half3 rockAlbZ = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV.xy).rgb;
                    half3 rockTexAlbedo = rockAlbX * blendWeights.x + rockAlbZ * blendWeights.z + SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV.xz).rgb * blendWeights.y;
                    half rockTexLuma = dot(rockTexAlbedo, half3(0.2126h, 0.7152h, 0.0722h));
                    half rockTexDetail = clamp(rockTexLuma / 0.48h, 0.74h, 1.26h);

                    half rockBumpScale = _RockBumpScale * (0.40h + 0.60h * exposedRockMask) * hillDetailWeight;
                    half3 rockNormX = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUV.zy), rockBumpScale);
                    half3 rockNormZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUV.xy), rockBumpScale);
                    half2 triplanarRockXY = rockNormX.xy * blendWeights.x + rockNormZ.xy * blendWeights.z;

                    // Angular procedural rock-cleavage facet normals on outcrops and ledges
                    float rEps = 0.40;
                    float rH0 = (BlastNoise(wpRot1 * 0.16 + float2(wp3.y * 0.10, 3.1)) - 0.5) * 0.60
                              + (BlastNoise(wpRot2 * 0.38 + float2(11.7, wp3.y * 0.22)) - 0.5) * 0.40;
                    float rHx = (BlastNoise((wpRot1 + float2(rEps, 0.0)) * 0.16 + float2(wp3.y * 0.10, 3.1)) - 0.5) * 0.60
                              + (BlastNoise((wpRot2 + float2(rEps, 0.0)) * 0.38 + float2(11.7, wp3.y * 0.22)) - 0.5) * 0.40;
                    float rHz = (BlastNoise((wpRot1 + float2(0.0, rEps)) * 0.16 + float2(wp3.y * 0.10, 3.1)) - 0.5) * 0.60
                              + (BlastNoise((wpRot2 + float2(0.0, rEps)) * 0.38 + float2(11.7, wp3.y * 0.22)) - 0.5) * 0.40;
                    half2 rockFractureXY = half2(rHx - rH0, rHz - rH0) * (2.80h * (0.28h * slopeFactor + 0.72h * exposedRockMask) * hillDetailWeight) + triplanarRockXY * 0.70h;

                    // 3D stepped rock-ledge normal bevels: scarp faces tilt outward/downward, upper lips tilt upward to catch sunlight
                    half strataFadeWeight = lerp(0.35h, 1.0h, strataDistFade) * hillDetailWeight;
                    half3 horizOutward = SafeNormalize(float3(geomNormalWS.x, 0.0, geomNormalWS.z));
                    half3 ledgeNormalOffset = (
                        horizOutward * (terraceRiser * 0.78h - terraceBench * 0.36h) +
                        half3(0.0h, terraceBench * 0.86h - terraceRiser * 0.62h, 0.0h) -
                        tangentWS * (rockJoint * 0.42h)
                    ) * (saturate(slopeFactor * 1.35h) * strataFadeWeight);

                    // Clear geological tonal contrast: lighter regolith on gentle slopes vs darker exposed rock on steep faces & broken ledges
                    half slopeAlbedoMod = 1.0h;
                    slopeAlbedoMod = lerp(slopeAlbedoMod, 1.20h, gentleRegolithMask);
                    slopeAlbedoMod = lerp(slopeAlbedoMod, 0.58h * rockTexDetail, exposedRockMask);
                    slopeAlbedoMod = lerp(slopeAlbedoMod, 0.64h * rockTexDetail, terraceRiser * strataFadeWeight);
                    slopeAlbedoMod = lerp(slopeAlbedoMod, 1.08h, terraceBench * strataFadeWeight * 0.65h);
                    slopeAlbedoMod = lerp(slopeAlbedoMod, 0.56h * rockTexDetail, rockJoint * strataFadeWeight * 0.65h);

                    // Subtle neutral cool-grey basalt tint on dark exposed rock (no beige, no blue cast)
                    half3 darkRockTint = lerp(half3(1.0h, 1.0h, 1.0h), half3(0.98h, 0.995h, 1.010h), saturate(exposedRockMask * 0.70h + terraceRiser * 0.45h));

                    half3 dNdx = ddx(geomNormalWS);
                    half3 dNdy = ddy(geomNormalWS);
                    half geomCurvature = saturate((length(dNdx) + length(dNdy)) * 16.0h);
                    half ridgeRib = (half)smoothstep(0.45, 0.82, 1.0 - chuteNoise);

                    half heightCrest = saturate((half)((wp3.y - 18.0) / 55.0));
                    ridgeCrestMask = saturate((geomCurvature * 0.50h + ridgeRib * 0.35h + terraceBench * 0.45h * strataFadeWeight) * (0.45h + 0.55h * heightCrest) * slopeFactor) * hillDetailWeight;

                    normalWS = normalize(
                        geomNormalWS +
                        ledgeNormalOffset +
                        tangentWS * rockFractureXY.x -
                        bitangentWS * rockFractureXY.y);

                    baseAlbedo *= slopeAlbedoMod * darkRockTint;
                    half strataCavityAo = lerp(1.0h, 0.72h, saturate(terraceRiser * 0.45h + rockJoint * 0.75h) * strataFadeWeight);
                    microAo = min(microAo, min(lerp(1.0h, 0.82h, exposedRockMask * (1.0h - terraceBench)), strataCavityAo));
                    smoothness = max(smoothness, exposedRockMask * 0.080h);
                }

                // Multi-scale lumpy compacted regolith, clods, irregular soft dimples, and crusty surface (0–40 m strong, fading to macro look by ~120 m)
                UNITY_BRANCH
                if (macroWeight > 0.001h)
                {
                    float2 wp = input.positionWS.xz;
                    float2 wpRot = float2(wp.x * 0.80 - wp.y * 0.60, wp.x * 0.60 + wp.y * 0.80);
                    float2 wpRotB = float2(wp.x * 0.55 + wp.y * 0.835, -wp.x * 0.835 + wp.y * 0.55);

                    // Macro / mid-distance domain-warped regolith grain (>120 m)
                    float2 grainWarp = float2(
                        BlastNoise(wp * 0.42 + float2(4.1, 13.7)) - 0.5,
                        BlastNoise(wpRot * 0.42 + float2(19.3, 6.8)) - 0.5) * 0.55;
                    float2 uvMacroA = (wpRot + grainWarp) * 0.165 + float2(0.37, 0.71);
                    float2 uvMacroB = (wpRotB - grainWarp.yx) * 0.112 + float2(0.14, 0.52);

                    half3 regolithTexA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMacroA).rgb;
                    half3 regolithTexB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMacroB).rgb;
                    half3 regolithTex = lerp(regolithTexA, regolithTexB, 0.50h);

                    // Continuous, anisotropic multi-octave regolith undulation normal for mid/far distances
                    float gEps = 0.28;
                    float2 wpAnisoA = float2(wpRot.x * 0.42, wpRot.y * 0.78);
                    float2 wpAnisoB = float2(wpRotB.x * 1.35, wpRotB.y * 0.82);
                    float g0 = (BlastNoise(wpAnisoA + grainWarp * 0.45) - 0.5) * 0.62
                             + (BlastNoise(wpAnisoB - grainWarp.yx * 0.65 + float2(11.3, 7.9)) - 0.5) * 0.38;
                    float gx = (BlastNoise(wpAnisoA + float2(gEps * 0.42, 0.0) + grainWarp * 0.45) - 0.5) * 0.62
                             + (BlastNoise(wpAnisoB + float2(gEps * 1.35, 0.0) - grainWarp.yx * 0.65 + float2(11.3, 7.9)) - 0.5) * 0.38;
                    float gz = (BlastNoise(wpAnisoA + float2(0.0, gEps * 0.78) + grainWarp * 0.45) - 0.5) * 0.62
                             + (BlastNoise(wpAnisoB + float2(0.0, gEps * 0.82) - grainWarp.yx * 0.65 + float2(11.3, 7.9)) - 0.5) * 0.38;
                    half grainNormalStrength = max(_MacroBumpScale, 0.35h) * macroWeight * lerp(0.18h, 0.30h, slopeFactor) * (0.45h + 0.55h * midDetailWeight);
                    half2 organicGrainXY = half2(gx - g0, gz - g0) * grainNormalStrength;

                    half fineMidScale = 0.22h * midDetailWeight * lerp(0.50h, 0.85h, slopeFactor);
                    half3 midNormalA = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMacroA), fineMidScale);
                    half3 midNormalB = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMacroB), fineMidScale);
                    half2 midRotA = half2(midNormalA.x * 0.80h + midNormalA.y * 0.60h, -midNormalA.x * 0.60h + midNormalA.y * 0.80h);
                    half2 midRotB = half2(midNormalB.x * 0.55h - midNormalB.y * 0.835h, midNormalB.x * 0.835h + midNormalB.y * 0.55h);

                    // Evaluate home-pad tyre ruts only (no small crater stamps)
                    half4 tyreRuts = SampleNearTyreRuts(wp, camDist);
                    baseAlbedo *= tyreRuts.z;
                    microAo = min(microAo, tyreRuts.w);

                    half2 distNormalXY = organicGrainXY + (midRotA + midRotB) * 0.35h
                        + weatheredCraters.xy + tyreRuts.xy * (1.0h - slopeFactor * 0.45h);

                    UNITY_BRANCH
                    if (nearWeight > 0.001h)
                    {
                        // Non-repeating two-scale regolith blend with organic domain warp & noise-driven rotation/offset:
                        // Scale 1 (Medium lumpy mounds, coarse clod clusters & irregular soft dimples: ~6.4 m and ~9.6 m tiles)
                        // Scale 2 (Fine compacted clods, small rubble & crusty regolith: ~2.3 m and ~3.5 m tiles)
                        // Plus close micro-crust pass (<20 m: ~1.1 m tile) so 1.5 m close-up views match Apollo 11 granularity.
                        float2 medWarp = float2(
                            BlastNoise(wp * 0.22 + float2(3.7, 9.2)) - 0.5,
                            BlastNoise(wpRot * 0.22 + float2(14.6, 5.1)) - 0.5) * 0.65;
                        float2 fineWarp = float2(
                            BlastNoise(wp * 0.72 + float2(7.3, 2.9)) - 0.5,
                            BlastNoise(wpRot * 0.72 + float2(15.1, 11.4)) - 0.5) * 0.22;

                        // Rotated frames for Medium Scale: A (~26.6 deg: c=0.8944, s=0.4472), B (~-51.3 deg: c=0.6247, s=-0.7809)
                        float2 wpMedA = float2(wp.x * 0.8944 - wp.y * 0.4472, wp.x * 0.4472 + wp.y * 0.8944) + medWarp;
                        float2 wpMedB = float2(wp.x * 0.6247 + wp.y * 0.7809, -wp.x * 0.7809 + wp.y * 0.6247) - medWarp.yx * 0.85;
                        float2 uvMedA = wpMedA * 0.156 + float2(0.13, 0.47);
                        float2 uvMedB = wpMedB * 0.104 + float2(0.61, 0.29);

                        // Rotated frames for Fine Scale: A (~11.5 deg: c=0.9799, s=0.1994), B (~47.9 deg: c=0.6704, s=0.7420)
                        float2 wpFineA = float2(wp.x * 0.9799 - wp.y * 0.1994, wp.x * 0.1994 + wp.y * 0.9799) + fineWarp;
                        float2 wpFineB = float2(wp.x * 0.6704 - wp.y * 0.7420, wp.x * 0.7420 + wp.y * 0.6704) - fineWarp.yx * 0.80;
                        float2 uvFineA = wpFineA * 0.435 + float2(0.07, 0.83);
                        float2 uvFineB = wpFineB * 0.285 + float2(0.41, 0.19);

                        // Spatially varying noise blend masks eliminate any periodic repetition across the plain
                        half medNoiseBlend = (half)smoothstep(0.28, 0.72, BlastNoise(wp * 0.085 + medWarp * 0.3 + float2(21.4, 8.7)));
                        half fineNoiseBlend = (half)smoothstep(0.26, 0.74, BlastNoise(wpRot * 0.26 + fineWarp * 0.5 + float2(9.3, 31.2)));

                        half4 maskMedA = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvMedA);
                        half4 maskMedB = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvMedB);
                        // Height-preserving blend between MedA and MedB so clods and dimples stay crisp instead of averaging flat
                        half medBlend = saturate(medNoiseBlend + (maskMedB.r - maskMedA.r) * 0.48h);
                        half4 maskMed = lerp(maskMedA, maskMedB, medBlend);

                        half4 maskFineA = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvFineA);
                        half4 maskFineB = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvFineB);
                        half fineBlend = saturate(fineNoiseBlend + (maskFineB.r - maskFineA.r) * 0.48h);
                        half4 maskFine = lerp(maskFineA, maskFineB, fineBlend);

                        half3 albedoMedA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMedA).rgb;
                        half3 albedoMedB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMedB).rgb;
                        half3 albedoMed = lerp(albedoMedA, albedoMedB, medBlend);

                        half3 albedoFineA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvFineA).rgb;
                        half3 albedoFineB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvFineB).rgb;
                        half3 albedoFine = lerp(albedoFineA, albedoFineB, fineBlend);

                        // Strong near-field relief in 0–40 m fading smoothly to macro by ~120 m
                        half nearPlainBoost = lerp(1.0h, 0.55h, slopeFactor);
                        half medBumpScale = (0.94h * regolithFade120 + 0.72h * regolithNear40) * nearPlainBoost;
                        half fineBumpScale = (0.54h * regolithFade120 + 0.66h * regolithNear40 + 0.28h * regolithClose20) * nearPlainBoost;

                        half3 nMedARaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMedA), medBumpScale);
                        half3 nMedBRaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMedB), medBumpScale);
                        // Rotate tangent normals back into world (+X, +Z) tangent frame
                        half2 nMedA = half2(nMedARaw.x * 0.8944h + nMedARaw.y * 0.4472h, -nMedARaw.x * 0.4472h + nMedARaw.y * 0.8944h);
                        half2 nMedB = half2(nMedBRaw.x * 0.6247h - nMedBRaw.y * 0.7809h, nMedBRaw.x * 0.7809h + nMedBRaw.y * 0.6247h);
                        half2 tsMedXY = lerp(nMedA, nMedB, medBlend);

                        half3 nFineARaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvFineA), fineBumpScale);
                        half3 nFineBRaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvFineB), fineBumpScale);
                        half2 nFineA = half2(nFineARaw.x * 0.9799h + nFineARaw.y * 0.1994h, -nFineARaw.x * 0.1994h + nFineARaw.y * 0.9799h);
                        half2 nFineB = half2(nFineBRaw.x * 0.6704h + nFineBRaw.y * 0.7420h, -nFineARaw.x * 0.7420h + nFineBRaw.y * 0.6704h);
                        half2 tsFineXY = lerp(nFineA, nFineB, fineBlend);

                        // Close-range (<20 m) micro-crust & pebble pass for the 1.5 m close-up camera
                        half2 tsCloseXY = half2(0.0h, 0.0h);
                        half closeAlbedoMod = 1.0h;
                        half closeAo = 1.0h;
                        if (regolithClose20 > 0.005h)
                        {
                            float2 wpClose = float2(wp.x * 0.3907 + wp.y * 0.9205, -wp.x * 0.9205 + wp.y * 0.3907) + fineWarp * 0.35;
                            float2 uvClose = wpClose * 0.92 + float2(0.73, 0.19);
                            half closeScale = 0.38h * regolithClose20 * nearPlainBoost;
                            half3 nCloseRaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvClose), closeScale);
                            tsCloseXY = half2(nCloseRaw.x * 0.3907h - nCloseRaw.y * 0.9205h, nCloseRaw.x * 0.9205h + nCloseRaw.y * 0.3907h);
                            half4 maskClose = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvClose);
                            closeAlbedoMod = lerp(1.0h, 0.93h + 0.14h * maskClose.r + 0.05h * maskClose.b, regolithClose20 * 0.45h);
                            closeAo = lerp(1.0h, saturate(maskClose.g / 0.85h), regolithClose20 * 0.38h);
                        }

                        half2 combinedTsXY = tsMedXY + tsFineXY + tsCloseXY + distNormalXY;
                        half3 tsNormal = normalize(half3(combinedTsXY, 1.0h));

                        // Combine medium and fine regolith albedo & clod/dimple height contrast
                        half3 albedoNear = lerp(albedoMed, albedoFine, 0.52h) * closeAlbedoMod;
                        half clodReliefTone = 0.91h + 0.17h * lerp(maskMed.r, maskFine.r, 0.50h) + 0.07h * lerp(maskMed.b, maskFine.b, 0.55h);
                        albedoNear *= lerp(1.0h, clodReliefTone, 0.58h + 0.36h * regolithNear40);

                        regolithTex = lerp(regolithTex, albedoNear, saturate(regolithFade120 * 0.85h + regolithNear40 * 0.15h));

                        normalWS = normalize(
                            tangentWS * tsNormal.x -
                            bitangentWS * tsNormal.y +
                            normalWS * tsNormal.z);

                        // Multi-scale micro-cavity AO (mask.g = baked horizon/cavity AO around clods and inside irregular dimples)
                        half combinedMaskG = min(lerp(maskMed.g, maskFine.g, 0.54h), min(maskMed.g, maskFine.g) * 0.5h + 0.42h);
                        half texAo = saturate(combinedMaskG / 0.855h) * closeAo;
                        half aoWeight = saturate(regolithFade120 * 0.62h + regolithNear40 * 0.38h) * max(_OcclusionStrength, 0.78h);
                        microAo = min(microAo, saturate(lerp(1.0h, texAo, aoWeight)));

                        half maskSmooth = lerp(maskMed.a, maskFine.a, 0.60h);
                        smoothness = saturate(maskSmooth * max(_SmoothnessScale, 0.25h) * regolithFade120 * (1.0h + slopeFactor * 0.25h));
                        regolithShadowWeight = saturate(regolithFade120 * 0.62h + regolithNear40 * 0.38h) * nearPlainBoost;
                        regolithHeight = lerp(maskMed.r, maskFine.r, 0.52h);
                        clodProminence = lerp(maskMed.b, maskFine.b, 0.56h);
                    }
                    else
                    {
                        half3 tsDist = normalize(half3(distNormalXY, 1.0h));
                        normalWS = normalize(
                            tangentWS * tsDist.x -
                            bitangentWS * tsDist.y +
                            normalWS * tsDist.z);
                    }

                    half3 regolithCentered = regolithTex / 0.525h;
                    half plainDetailStrength = lerp(0.12h, 0.44h, saturate(regolithFade120 * 0.65h + regolithNear40 * 0.35h));
                    half detailStrength = lerp(plainDetailStrength, 0.30h, slopeFactor) * macroWeight;
                    baseAlbedo *= lerp(1.0h.xxx, regolithCentered, detailStrength);
                }

                // Keep the lander's blast-zone halo & rays working.
                baseAlbedo *= BlastSweepTone(input.positionWS, geomNormalWS.y);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
                    mainLight.color *= aoFactor.directAmbientOcclusion;
                    half ambientAo = aoFactor.indirectAmbientOcclusion * microAo;
                #else
                    half ambientAo = microAo;
                #endif

                // Directional ambient relief: leeward sides of clods and dimples also dip in ambient fill so low-sun micro-shadows stay rich
                half geomNdotLMain = dot(geomNormalWS, mainLight.direction);
                half deltaNdotLAmb = dot(normalWS, mainLight.direction) - geomNdotLMain;
                half ambGrazingSlope = deltaNdotLAmb / max(abs(geomNdotLMain) * 0.52h + 0.032h, 0.048h);
                half ambDimpleDepth = saturate((0.49h - regolithHeight) * 2.2h);
                half ambCavityDepth = saturate((0.84h - microAo) * 2.4h);
                half ambShadowSignal = ambGrazingSlope - ambDimpleDepth * 0.22h - ambCavityDepth * 0.20h;
                half ambClodShadow = smoothstep(0.04h, -0.38h, ambShadowSignal) * regolithShadowWeight;
                half ambReliefGain = lerp(0.18h + 0.42h * regolithShadowWeight, 0.42h, slopeFactor);
                half craterAmbRelief = clamp(1.0h + deltaNdotLAmb * ambReliefGain, 0.68h, 1.22h) * lerp(1.0h, lerp(0.50h, 0.22h, sunlit), ambClodShadow);
                half skyLedgeRelief = lerp(craterAmbRelief, clamp(1.0h + (normalWS.y - geomNormalWS.y) * 0.65h + deltaNdotLAmb * 0.35h - (1.0h - microAo) * 0.35h, 0.72h, 1.18h), slopeFactor * 0.80h);
                half3 rawSH = SampleSH(normalWS);
                half shLuma = dot(rawSH, half3(0.2126h, 0.7152h, 0.0722h));
                half3 coolSH = lerp(rawSH, shLuma * half3(0.985h, 0.998h, 1.010h), 0.90h);
                half3 ambient = coolSH * ambientAo * skyLedgeRelief;
                half ambientShadowCarve = 1.0h;
                half3 directLighting = MainSunDiffuseStylized(mainLight, normalWS, geomNormalWS, viewDirWS, sunlit, microAo, slopeFactor, ridgeCrestMask, regolithShadowWeight, regolithHeight);
                half3 specular = RegolithMicroSpecular(mainLight, normalWS, viewDirWS, smoothness);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = normalizedScreenSpaceUV;

                    #if USE_FORWARD_PLUS
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        // Secondary directional lights are intentionally skipped on terrain so
                        // dune/crater shadow sides keep their deep lunar contrast.
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                        #if defined(_SCREEN_SPACE_OCCLUSION)
                            light.color *= aoFactor.directAmbientOcclusion;
                        #endif
                        directLighting += PunctualDiffuse(light, normalWS, geomNormalWS, microAo, regolithHeight, clodProminence, regolithShadowWeight, ambientShadowCarve);
                        specular += RegolithMicroSpecular(light, normalWS, viewDirWS, smoothness);
                    LIGHT_LOOP_END
                #endif

                half3 lighting = ambient * ambientShadowCarve + directLighting;
                half3 color = baseAlbedo * lighting + specular;

                // Keep the power overlay reach ring working.
                if (_MoonReachRing.w > 0.001)
                {
                    float dist = distance(input.positionWS.xz, _MoonReachRing.xy);
                    float delta = dist - _MoonReachRing.w;
                    float ring = 1.0 - smoothstep(0.0, 0.14, abs(delta));
                    float fill = step(dist, _MoonReachRing.w) * 0.07;
                    half mask = saturate(ring * 0.85 + fill) * _MoonReachRingColor.a;
                    color = lerp(color, _MoonReachRingColor.rgb, mask);
                }

                // Airless Moon: no MoonAtmosphere / no aerial haze or base-of-slope fog band.
                color = clamp(color, 0.0h, 32.0h);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0h);
            }
            ENDHLSL
        }
    }
}
