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
        _NearBumpScale("Near Normal Scale", Range(0.0, 2.0)) = 0.78
        _MacroBumpScale("Macro Normal Scale", Range(0.0, 2.0)) = 0.50
        _RockBumpScale("Triplanar Rock Normal Scale", Range(0.0, 2.0)) = 0.88
        _NearFadeStart("Near Detail Fade Start (m)", Float) = 38.0
        _NearFadeEnd("Near Detail Fade End (m)", Float) = 120.0
        _DetailAlbedoStrength("Regolith Albedo Modulation", Range(0.0, 0.6)) = 0.36
        _SmoothnessScale("Smoothness Scale", Range(0.0, 0.5)) = 0.05
        _OcclusionStrength("Micro-Cavity AO Strength", Range(0.0, 1.0)) = 0.68
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

            // Deterministic 3D hash for isotropic 3D world-space crater-rim noise (zero horizontal banding).
            float RimHash31(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // Smooth quintic C2 3D value noise in world metres (0..1) so steep faces and rounded shoulders
            // vary isotropically in all three dimensions without horizontal elevation stripes.
            float RimNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float n000 = RimHash31(i + float3(0.0, 0.0, 0.0));
                float n100 = RimHash31(i + float3(1.0, 0.0, 0.0));
                float n010 = RimHash31(i + float3(0.0, 1.0, 0.0));
                float n110 = RimHash31(i + float3(1.0, 1.0, 0.0));
                float n001 = RimHash31(i + float3(0.0, 0.0, 1.0));
                float n101 = RimHash31(i + float3(1.0, 0.0, 1.0));
                float n011 = RimHash31(i + float3(0.0, 1.0, 1.0));
                float n111 = RimHash31(i + float3(1.0, 1.0, 1.0));

                float nx00 = lerp(n000, n100, u.x);
                float nx10 = lerp(n010, n110, u.x);
                float nx01 = lerp(n001, n101, u.x);
                float nx11 = lerp(n011, n111, u.x);

                float nxy0 = lerp(nx00, nx10, u.y);
                float nxy1 = lerp(nx01, nx11, u.y);
                return lerp(nxy0, nxy1, u.z);
            }

            // 3-octave 3D FBM with full 3D orthogonal octave rotations so no axis-aligned or horizontal planes align.
            float RimFbm3D(float3 p)
            {
                float3 p1 = p;
                float3 p2 = float3(
                    p.x * 0.80 - p.y * 0.36 + p.z * 0.48,
                    p.x * 0.48 + p.y * 0.84 - p.z * 0.25,
                   -p.x * 0.36 + p.y * 0.41 + p.z * 0.84) * 2.17 + float3(13.7, 29.1, 7.3);
                float3 p3 = float3(
                    p.x * 0.64 + p.y * 0.60 - p.z * 0.48,
                   -p.x * 0.48 + p.y * 0.76 + p.z * 0.43,
                    p.x * 0.60 - p.y * 0.25 + p.z * 0.76) * 4.53 + float3(41.2, 11.8, 53.9);
                return RimNoise3D(p1) * 0.56 + RimNoise3D(p2) * 0.30 + RimNoise3D(p3) * 0.14;
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
            // with soft rounded lump shading and dusty hollow AO so regolith relief reads as powdery in eclipse lamplight.
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
                half ndotl = saturate(dot(normalWS, light.direction) + 0.06h * regolithFade120);
                half deltaNdotL = dot(normalWS, light.direction) - dot(geomNormalWS, light.direction);

                half grazingRatio = deltaNdotL / max(geomNdotL * 0.65h + 0.085h, 0.11h);
                half shadowSignal = grazingRatio + (regolithHeight - 0.50h) * 0.42h - (1.0h - microAo) * 0.55h;
                half microShadow = lerp(1.0h, smoothstep(-0.78h, 0.42h, shadowSignal), regolithFade120 * 0.72h);
                half clodHighlight = 1.0h + saturate(grazingRatio) * (0.20h + 0.14h * clodProminence) * regolithFade120;

                float rawAtten = min(light.distanceAttenuation, 16.0) * light.shadowAttenuation;
                half lampWeight = saturate((half)(rawAtten * 1.8)) * regolithFade120;
                ambientShadowCarve = min(ambientShadowCarve, lerp(1.0h, lerp(0.56h, 1.0h, microShadow) * lerp(1.0h, microAo, 0.55h), lampWeight));

                float atten = rawAtten * (float)(microShadow * clodHighlight * lerp(1.0h, microAo, 0.58h));
                return light.color * (half)(ndotl * atten);
            }

            // Ultra-matte powdery mineral response on lunar regolith (prevents shiny wet-scale glints under lamps or low sun).
            half3 RegolithMicroSpecular(Light light, half3 normalWS, half3 viewDirWS, half smoothness)
            {
                half3 halfDir = SafeNormalize(float3(light.direction) + float3(viewDirWS));
                half ndoth = saturate(dot(normalWS, halfDir));
                half ndotl = saturate(dot(normalWS, light.direction));
                half specPower = exp2(4.5h + smoothness * 10.0h);
                half spec = PositivePow(max(ndoth, 0.0001h), specPower) * (smoothness * 0.032h) * ndotl;
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

            // Samples one 2D projection plane of the regolith detail maps with rotated dual-scale anti-tiling.
            void SampleRegolithPlane(
                float2 p2,
                float2 warp2,
                half bumpScale,
                out half3 albedoOut,
                out half2 normXYOut,
                out half4 maskOut)
            {
                float2 pA = float2(p2.x * 0.8944 - p2.y * 0.4472, p2.x * 0.4472 + p2.y * 0.8944) + warp2;
                float2 pB = float2(p2.x * 0.6247 + p2.y * 0.7809, -p2.x * 0.7809 + p2.y * 0.6247) - warp2.yx * 0.82;
                float2 uvA = pA * 0.165 + float2(0.19, 0.53);
                float2 uvB = pB * 0.310 + float2(0.67, 0.29);

                half4 maskA = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvA);
                half4 maskB = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvB);
                maskOut = lerp(maskA, maskB, 0.50h);

                half3 albA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvA).rgb;
                half3 albB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB).rgb;
                albedoOut = lerp(albA, albB, 0.50h);

                half3 nARaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvA), bumpScale);
                half3 nBRaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvB), bumpScale * 0.85h);
                half2 nA = half2(nARaw.x * 0.8944h + nARaw.y * 0.4472h, -nARaw.x * 0.4472h + nARaw.y * 0.8944h);
                half2 nB = half2(nBRaw.x * 0.6247h - nBRaw.y * 0.7809h, nBRaw.x * 0.7809h + nBRaw.y * 0.6247h);
                normXYOut = lerp(nA, nB, 0.50h);
            }

            // World-space triplanar regolith detail for steep slopes and near-vertical faces (zero UV stretching).
            void SampleTriplanarRegolithOnSlope(
                float3 wp3,
                half3 geomNormalWS,
                half bumpScale,
                out half3 triAlbedo,
                out half3 triNormalOffsetWS,
                out half4 triMask)
            {
                half3 triW = abs(geomNormalWS);
                triW = pow(max(triW, 0.0001h), 4.0h);
                triW /= max(triW.x + triW.y + triW.z, 1e-4h);

                float3 warp3 = float3(
                    RimNoise3D(wp3 * 0.22 + float3(5.3, 13.7, 9.1)) - 0.5,
                    RimNoise3D(wp3 * 0.22 + float3(19.4, 7.2, 27.8)) - 0.5,
                    RimNoise3D(wp3 * 0.22 + float3(31.1, 23.5, 4.6)) - 0.5) * 0.48;

                half3 albX, albY, albZ;
                half2 nX, nY, nZ;
                half4 mX, mY, mZ;

                SampleRegolithPlane(wp3.zy, warp3.zy, bumpScale, albX, nX, mX);
                SampleRegolithPlane(wp3.xz, warp3.xz, bumpScale, albY, nY, mY);
                SampleRegolithPlane(wp3.xy, warp3.xy, bumpScale, albZ, nZ, mZ);

                triAlbedo = albX * triW.x + albY * triW.y + albZ * triW.z;
                triMask = mX * triW.x + mY * triW.y + mZ * triW.z;

                half3 worldPerturb =
                    half3(0.0h, nX.y, nX.x) * triW.x +
                    half3(nY.x, 0.0h, nY.y) * triW.y +
                    half3(nZ.x, nZ.y, 0.0h) * triW.z;
                triNormalOffsetWS = worldPerturb - geomNormalWS * dot(worldPerturb, geomNormalWS);
            }

            // Procedural crater normal bowls are disabled in the shader so no fields of round depressions or pocks
            // are stamped across the plain (height relief is owned by GroundShaping).
            half4 SampleWeatheredCraters(float3 wp3, half3 geomNormalWS, half slopeFactor, float camDist)
            {
                return half4(0.0h, 0.0h, 1.0h, 1.0h);
            }

            // Large-scale organic noise-warped albedo patches (50–300 m): darker basalt-mare regolith regions and
            // lighter highland-ejecta regions with soft gradients and low contrast. On slopes, uses isotropic 3D
            // domain-warped world-space noise so steep crater-rim faces never stretch or form horizontal bands.
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
                float rawField = (n260 - 0.50) * 0.48 + (n130 - 0.50) * 0.36 + (n65 - 0.50) * 0.28;
                float patch01 = smoothstep(-0.135, 0.135, rawField);

                // Low-contrast modulation between darker basalt-mare regolith and lighter highland-ejecta
                float floorPatchMod = lerp(0.75, 1.23, patch01);

                float mountainMod;
                // True 3D domain-warped macro noise across crater-rim massifs (zero horizontal banding or XZ stretching)
                float3 warp3D = float3(
                    RimNoise3D(wp3 * 0.0085 + float3(5.2, 17.9, 11.4)) - 0.5,
                    RimNoise3D(wp3 * 0.0085 + float3(31.7, 8.3, 23.1)) - 0.5,
                    RimNoise3D(wp3 * 0.0085 + float3(19.4, 43.2, 3.8)) - 0.5) * 32.0;
                float rimBroad3D = RimFbm3D((wp3 + warp3D) * 0.0135);
                float rimMed3D   = RimFbm3D((wp3 - warp3D * 0.60) * 0.0340 + float3(17.3, 9.1, 41.6));
                float rimMacro01 = smoothstep(0.22, 0.78, rimBroad3D * 0.62 + rimMed3D * 0.38);
                mountainMod = lerp(0.88, 1.15, rimMacro01);


                half patchScalar = (half)clamp(lerp(floorPatchMod, mountainMod, slopeFactor * 0.82), 0.75, 1.24);

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

            // Soft R1 stylized-realism lunar shading on mountain ridges and regolith-mantled slopes, plus soft rounded
            // lump shadows and dusty hollows across near-field regolith.
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

                // Soft, rounded grazing lump response: leeward sides of rounded clods and soft dimples dip smoothly into shadow
                // without sharp contour steps or crinkly flake edges.
                half grazingDenom = max(abs(geomNdotL) * 0.68h + 0.065h, 0.085h);
                half grazingSlope = deltaNdotL / grazingDenom;
                half dimpleDepth = saturate((0.49h - regolithHeight) * 1.6h);
                half cavityDepth = saturate((0.82h - microAo) * 1.7h);
                half shadowSignal = grazingSlope - dimpleDepth * 0.18h - cavityDepth * 0.16h;
                half clodLeeShadow = smoothstep(0.14h, -0.72h, shadowSignal) * regolithShadowWeight;
                half clodSunCatch = smoothstep(-0.18h, 0.68h, grazingSlope + saturate(regolithHeight - 0.50h) * 0.28h) * regolithShadowWeight;

                // In Earthlight / eclipse (sunlit == 0), keep soft clod/dimple relief and slope forms readable.
                half earthWrap = saturate(ndotl * 0.84h + 0.16h);
                half earthReliefGain = lerp(0.42h + 0.38h * regolithShadowWeight, 0.78h, slopeFactor);
                half earthRelief = clamp(deltaNdotL * earthReliefGain, -0.32h, 0.38h);
                half earthDiffuse = saturate(pow(earthWrap, 1.10h) * 1.05h + earthRelief);

                // Soft R1 stylized-realism terminator across both plain and regolith-mantled slopes (no harsh binary step bands).
                half plainKnee = smoothstep(-0.065h, 0.175h, ndotl);
                half slopeKnee = smoothstep(-0.048h, 0.145h, ndotl);
                half terminatorKnee = lerp(plainKnee, slopeKnee, slopeFactor);
                half grazingContrast = pow(saturate(ndotl + lerp(0.038h, 0.022h, slopeFactor)), 1.04h) * terminatorKnee;

                // Lunar opposition / Lommel-Seeliger powdery balance on flat floors while slopes keep soft volumetric relief.
                half ndotv = max(0.22h, saturate(dot(normalWS, viewDirWS)));
                half lommel = grazingContrast / max(0.36h, grazingContrast + ndotv * 0.40h);
                half sunDiffuse = lerp(lommel * 1.04h, grazingContrast * 1.18h, saturate(slopeFactor * 0.65h + 0.25h));

                // Accentuate grazing sunlit ridge crests and softly rounded sunlit lump tops.
                half terminatorZone = smoothstep(-0.04h, 0.06h, geomNdotL) * (1.0h - smoothstep(0.06h, 0.40h, geomNdotL));
                half ridgeCrestBoost = saturate(deltaNdotL) * (0.26h + terminatorZone * 0.38h) * lerp(0.14h + 0.22h * regolithShadowWeight, 0.62h, slopeFactor);
                sunDiffuse += ridgeCrestBoost + clodSunCatch * 0.14h;

                half highlandDiffuse = lerp(earthDiffuse, sunDiffuse, sunlit);

                half sunStep = lerp(smoothstep(-0.050h, 0.165h, ndotl), smoothstep(-0.038h, 0.140h, ndotl), slopeFactor);
                half earthStep = smoothstep(0.02h, 0.12h, earthWrap);
                half terminatorStep = lerp(earthStep, sunStep, sunlit);
                half rampInput = lerp(earthWrap, saturate(ndotl + lerp(0.025h, 0.015h, slopeFactor)), sunlit);
                half midtoneRoll = smoothstep(0.04h, 0.48h, rampInput);
                half highlightRoll = smoothstep(0.38h, 0.88h, rampInput);
                half painterlyRamp = terminatorStep * (0.40h + midtoneRoll * 0.36h + highlightRoll * 0.26h);

                // Soft rim-light along convex ridge shoulders
                half rimGrazing = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 2.2h) * smoothstep(-0.04h, 0.14h, ndotl);
                half painterlyRidgeStroke = (ridgeCrestMask * smoothstep(-0.02h, 0.12h, ndotl) * 0.18h) + rimGrazing * slopeFactor * 0.12h;
                half stylizedDiffuse = lerp(highlandDiffuse, painterlyRamp, lerp(0.26h, 0.34h, slopeFactor)) + painterlyRidgeStroke + clodSunCatch * 0.08h * sunlit;

                // Soft leeward shadow attenuation behind rounded clods and inside soft-edged dimples
                half clodShadowAtten = lerp(1.0h, lerp(0.62h, 0.28h, sunlit), clodLeeShadow);

                // Soft hollow AO darkens dust-filled low spots and clod bases
                half directCavity = lerp(1.0h, microAo, 0.44h + 0.24h * sunlit);
                half diffuse = stylizedDiffuse * clodShadowAtten * directCavity;

                // Crisp lunar shadows in DirectSun (shadowFloor = 0.048) so shadowed slopes stay dark while retaining subtle form
                half shadowFloor = lerp(0.18h, 0.048h, sunlit);
                half atten = max(light.shadowAttenuation, shadowFloor) * light.distanceAttenuation;

                // Neutralize warm golden-hour / dawn sun tint so lunar regolith reads as subtle cool grey (no beige, no blue tint)
                half lightLuma = dot(light.color, half3(0.2126h, 0.7152h, 0.0722h));
                half3 lunarLightColor = lerp(light.color, lightLuma * half3(0.985h, 0.998h, 1.010h), lerp(0.36h, 0.94h, sunlit));
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
                // Also activate on any test or world mesh whose slope angle is >= 18 deg (rawGeomNormalWS.y < 0.95).
                half slopeGeomMask = 1.0h - (half)smoothstep(0.88h, 0.96h, rawGeomNormalWS.y);
                half mountainMask = saturate(
                    (half)smoothstep(5.5, 16.0, input.positionWS.y) +
                    (half)smoothstep(285.0, 345.0, length(input.positionWS.xz)) +
                    slopeGeomMask);
                half3 geomNormalWS = normalize(lerp(rawGeomNormalWS, half3(0.0h, 1.0h, 0.0h), (1.0h - mountainMask) * 0.78h));

                // Regolith near-field weights:
                // - Strong near-field regolith relief in 0–40 m (where low sun and 07's headlamps make rounded clods, soft dimples, and powder grain read)
                // - Smooth fade to the existing macro look by ~120 m
                // - Extra fine isotropic sand/powder grain clarity within 0–22 m for low close-up views (1.5 m up) and immediate chase foreground
                half regolithClose20 = 1.0h - (half)smoothstep(6.0, 22.0, camDist);
                half regolithNear40 = 1.0h - (half)smoothstep(28.0, 46.0, camDist);
                half regolithFade120 = 1.0h - (half)smoothstep(38.0, 120.0, camDist);
                half nearWeight = max(
                    1.0h - (half)smoothstep(_NearFadeStart, max(_NearFadeStart + 1.0, _NearFadeEnd), camDist),
                    regolithFade120);
                half midDetailWeight = 1.0h - (half)smoothstep(220.0, 720.0, camDist);
                half macroWeight = 1.0h - (half)smoothstep(720.0, 1550.0, camDist);
                half hillDetailWeight = 1.0h - (half)smoothstep(900.0, 1850.0, camDist);

                // Slope masks: gentle/moderate slopes (25-35 deg) hold regolith-mantled grey; only the steepest parts expose darker fresh rock.
                half slopeFactor = (1.0h - (half)smoothstep(0.74h, 0.945h, geomNormalWS.y)) * mountainMask;

                // Neutral cool grey lunar regolith base reflectance (~0.12-0.16 linear).
                // Preserve home-pad and Kestrel-scorch vertex darkening on the basin floor, while suppressing periodic
                // elevation vertex-color terrace bands on crater-rim slopes.
                half3 vertexLinear = SRGBToLinear(input.color.rgb) * _BaseColor.rgb;
                half vLuma = dot(vertexLinear, half3(0.2126h, 0.7152h, 0.0722h));
                half padOrScorchWeight = max(
                    (half)(1.0 - smoothstep(18.0, 32.0, length(input.positionWS.xz))),
                    (half)(1.0 - smoothstep(8.0, 16.0, length(input.positionWS.xz - float2(-12.0, 16.0))))) * (1.0h - slopeGeomMask);
                half vNorm;
                half floorVNorm = lerp(0.66h, smoothstep(0.025h, 0.22h, vLuma), padOrScorchWeight);
                // Cohesive regolith-mantled highland grey on rim slopes (zero 22 m vertex-color horizontal stripe bands)
                half rimMantleVNorm = lerp(0.76h, smoothstep(0.025h, 0.22h, vLuma), 0.10h * (1.0h - slopeFactor));
                vNorm = lerp(floorVNorm, rimMantleVNorm, mountainMask * (1.0h - padOrScorchWeight));


                half lunarReflectance = lerp(0.118h, 0.160h, vNorm);

                // Calibrated neutral-cool grey compensation: counteracts post-processing warm split-toning (_splitHighlights / _gain)
                // without over-pushing blue, so regolith stays a subtle cool grey with no beige and no blue tint.
                half3 coolNeutralTint = lerp(half3(0.975h, 0.995h, 1.005h), half3(0.935h, 0.992h, 1.042h), sunlit);
                half3 baseAlbedo = lunarReflectance * coolNeutralTint;

                // Large-scale 50–300 m organic mare/ejecta albedo patches + 3D rim macro variation + faint broken crater rays.
                baseAlbedo *= SampleMacroRegolithVariation(input.positionWS, slopeFactor);
                baseAlbedo *= SampleDistantCraterRays(input.positionWS, geomNormalWS.y);

                half3 normalWS = geomNormalWS;
                half microAo = 1.0h;
                half smoothness = 0.025h;
                half ridgeCrestMask = 0.0h;
                half regolithShadowWeight = 0.0h;
                half regolithHeight = 0.50h;
                half clodProminence = 0.0h;

                // Singularity-free world-space tangent frame for any slope or vertical wall orientation.
                half3 refAxis = (abs(geomNormalWS.x) > 0.82h)
                    ? half3(0.0h, 0.0h, 1.0h)
                    : half3(1.0h, 0.0h, 0.0h);
                half3 tangentWS = normalize(refAxis - geomNormalWS * dot(geomNormalWS, refAxis));
                half3 bitangentWS = normalize(cross(geomNormalWS, tangentWS));

                half4 weatheredCraters = SampleWeatheredCraters(input.positionWS, geomNormalWS, slopeFactor, camDist);
                baseAlbedo *= weatheredCraters.z;
                microAo = min(microAo, weatheredCraters.w);

                // Slope geological structure:
                // - Triplanar / 3D world-space crater-rim slope material inspired by
                //   Apollo / LRO oblique photos of Tycho, Copernicus, and Shackleton rims:
                //   * Regolith-mantled grey across 25–35 deg slopes, rounded shoulders, and talus aprons.
                //   * Subtle darker fresh-rock exposures ONLY on the steepest parts (steep scarps & near-vertical faces),
                //     broken up by 3D world-space macro noise so there is ZERO horizontal stripe banding or UV stretching.
                UNITY_BRANCH
                if (slopeFactor > 0.005h && hillDetailWeight > 0.005h)
                {
                    float3 wp3 = input.positionWS;

                    // 1. Tightened triplanar weights from world-space geometry normal (zero UV stretching on steep faces)
                    half3 blendWeights = abs(geomNormalWS);
                    blendWeights = pow(max(blendWeights, 0.0001h), 4.0h);
                    blendWeights /= max(blendWeights.x + blendWeights.y + blendWeights.z, 1e-4h);

                    // 2. 3D isotropic domain-warped coordinates in world metres (zero horizontal stratification)
                    float3 warp3 = float3(
                        RimNoise3D(wp3 * 0.016 + float3(11.3, 27.9, 5.4)) - 0.5,
                        RimNoise3D(wp3 * 0.016 + float3(41.8, 7.2, 19.6)) - 0.5,
                        RimNoise3D(wp3 * 0.016 + float3(23.5, 15.1, 37.8)) - 0.5) * 16.0;
                    float3 pWarped = wp3 + warp3;

                    // Broad 3D regolith mantle variation across rounded shoulders and talus aprons (~45-120 m lobes)
                    float mantleNoise3D = RimFbm3D(pWarped * 0.024);
                    half mantlePatch = (half)smoothstep(0.22, 0.78, mantleNoise3D);

                    // Medium 3D bedrock outcrop selector (~15-35 m isotropic 3D patches, never horizontal bands)
                    float outcropNoise3D = RimFbm3D(pWarped * 0.062 + float3(19.7, 33.4, 7.9));
                    half outcropPatch = (half)smoothstep(0.30, 0.74, outcropNoise3D);

                    // Fine 3D breccia/basalt texture variation (~4-9 m)
                    float detailNoise3D = RimFbm3D(wp3 * 0.18 + warp3 * 0.20 + float3(7.1, 53.2, 29.4));

                    // 3. Steepest-parts-only fresh-rock exposure mask:
                    //    - On 25-35 deg slopes (geomNormalWS.y in 0.819..0.906), steepestSlopeMask is 0 -> regolith-mantled grey!
                    //    - Above ~43 deg (geomNormalWS.y < 0.73), ramping strongly toward 65-88 deg near-vertical faces
                    //      (geomNormalWS.y < 0.42), fresh-rock exposures emerge in organic 3D patches.
                    half steepestSlopeMask = 1.0h - (half)smoothstep(0.24h, 0.73h, geomNormalWS.y);
                    half upperShoulderPeek = (1.0h - (half)smoothstep(0.70h, 0.83h, geomNormalWS.y))
                        * (half)smoothstep(0.68, 0.90, outcropNoise3D) * 0.16h;
                    half exposedFreshRockMask = saturate(
                        (steepestSlopeMask * (0.46h + 0.54h * outcropPatch) + upperShoulderPeek)
                        * hillDetailWeight);

                    // 4. Triplanar basalt rock texture + normal map on steep fresh-rock exposures
                    float3 rockCoord = (wp3 + warp3 * 0.12) * _RockTiling;
                    half3 rockAlbX = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockCoord.zy).rgb;
                    half3 rockAlbY = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockCoord.xz).rgb;
                    half3 rockAlbZ = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockCoord.xy).rgb;
                    half3 rockTexAlbedo = rockAlbX * blendWeights.x + rockAlbY * blendWeights.y + rockAlbZ * blendWeights.z;
                    half rockTexLuma = dot(rockTexAlbedo, half3(0.2126h, 0.7152h, 0.0722h));
                    half rockTexDetail = clamp(rockTexLuma / 0.48h, 0.82h, 1.18h);

                    half rockBumpScale = _RockBumpScale * (0.22h + 0.78h * exposedFreshRockMask) * hillDetailWeight;
                    half3 rockNormX = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockCoord.zy), rockBumpScale);
                    half3 rockNormY = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockCoord.xz), rockBumpScale);
                    half3 rockNormZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockCoord.xy), rockBumpScale);
                    half3 triplanarRockNormalWS =
                        half3(0.0h, rockNormX.y, rockNormX.x) * blendWeights.x +
                        half3(rockNormY.x, 0.0h, rockNormY.y) * blendWeights.y +
                        half3(rockNormZ.x, rockNormZ.y, 0.0h) * blendWeights.z;
                    triplanarRockNormalWS -= geomNormalWS * dot(triplanarRockNormalWS, geomNormalWS);

                    // 5. Isotropic 3D world-space normal relief (soft pillowed regolith slumps on 25-35 deg slopes,
                    //    crisper 3D fractured rock facets on steep fresh-rock exposures, zero horizontal slab lines)
                    float eps3 = 0.55;
                    float3 nPos = wp3 * 0.14 + warp3 * 0.18;
                    float h0 = RimFbm3D(nPos);
                    float hx = RimFbm3D(nPos + float3(eps3 * 0.14, 0.0, 0.0));
                    float hy = RimFbm3D(nPos + float3(0.0, eps3 * 0.14, 0.0));
                    float hz = RimFbm3D(nPos + float3(0.0, 0.0, eps3 * 0.14));
                    half3 grad3D = half3((half)(hx - h0), (half)(hy - h0), (half)(hz - h0));
                    grad3D -= geomNormalWS * dot(grad3D, geomNormalWS);

                    half reliefStrength = lerp(1.05h, 2.95h, exposedFreshRockMask) * slopeFactor * hillDetailWeight;
                    normalWS = normalize(geomNormalWS + grad3D * reliefStrength + triplanarRockNormalWS * 0.78h);

                    // 6. Tonal balance:
                    //    - Regolith-mantled grey on 25-35 deg slopes (soft macro variation 0.94..1.09)
                    //    - Subtle darker fresh-rock exposures only on the steepest parts (0.56..0.72 of mantled grey)
                    half mantledGreyMod = lerp(0.94h, 1.09h, mantlePatch) * lerp(0.97h, 1.03h, (half)detailNoise3D);
                    half freshRockMod = lerp(0.56h, 0.72h, saturate(rockTexDetail * 0.55h + (half)detailNoise3D * 0.45h));
                    half slopeAlbedoMod = lerp(mantledGreyMod, freshRockMod, exposedFreshRockMask);

                    // Subtle cool-slate basaltic-anorthosite tint on fresh-rock exposures
                    half3 freshRockTint = lerp(
                        half3(1.0h, 1.0h, 1.0h),
                        half3(0.965h, 0.990h, 1.020h),
                        exposedFreshRockMask * 0.65h);

                    half3 dNdx = ddx(geomNormalWS);
                    half3 dNdy = ddy(geomNormalWS);
                    half geomCurvature = saturate((length(dNdx) + length(dNdy)) * 14.0h);
                    ridgeCrestMask = saturate(geomCurvature * 0.45h * slopeFactor) * hillDetailWeight;

                    baseAlbedo *= lerp(1.0h, slopeAlbedoMod, slopeFactor * hillDetailWeight) * freshRockTint;
                    microAo = min(microAo, lerp(1.0h, 0.84h, exposedFreshRockMask * (1.0h - (half)detailNoise3D * 0.45h)));
                    smoothness = max(smoothness, exposedFreshRockMask * 0.065h);

                }

                // Multi-scale powdery regolith: rounded soft clods, soft-edged dust-filled dimples, and fine isotropic sand/powder grain
                // (0–40 m strong, fading smoothly to the macro look by ~120 m)
                UNITY_BRANCH
                if (macroWeight > 0.001h)
                {
                    float2 wp = input.positionWS.xz;
                    float2 wpRot = float2(wp.x * 0.80 - wp.y * 0.60, wp.x * 0.60 + wp.y * 0.80);
                    float2 wpRotB = float2(wp.x * 0.55 + wp.y * 0.835, -wp.x * 0.835 + wp.y * 0.55);

                    // Macro / mid-distance domain-warped regolith grain (>120 m)
                    float2 grainWarp = float2(
                        BlastNoise(wp * 0.36 + float2(4.1, 13.7)) - 0.5,
                        BlastNoise(wpRot * 0.36 + float2(19.3, 6.8)) - 0.5) * 0.55;
                    float2 uvMacroA = (wpRot + grainWarp) * 0.165 + float2(0.37, 0.71);
                    float2 uvMacroB = (wpRotB - grainWarp.yx) * 0.112 + float2(0.14, 0.52);

                    half3 regolithTexA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMacroA).rgb;
                    half3 regolithTexB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMacroB).rgb;
                    half3 regolithTex = lerp(regolithTexA, regolithTexB, 0.50h);

                    // Isotropic multi-octave regolith undulation normal for mid/far distances (zero directional streaks)
                    float gEps = 0.35;
                    float2 wpIsoA = wpRot * 0.55;
                    float2 wpIsoB = wpRotB * 1.05;
                    float g0 = (BlastNoise(wpIsoA + grainWarp * 0.40) - 0.5) * 0.62
                             + (BlastNoise(wpIsoB - grainWarp.yx * 0.55 + float2(11.3, 7.9)) - 0.5) * 0.38;
                    float gx = (BlastNoise(wpIsoA + float2(gEps * 0.55, 0.0) + grainWarp * 0.40) - 0.5) * 0.62
                             + (BlastNoise(wpIsoB + float2(gEps * 1.05, 0.0) - grainWarp.yx * 0.55 + float2(11.3, 7.9)) - 0.5) * 0.38;
                    float gz = (BlastNoise(wpIsoA + float2(0.0, gEps * 0.55) + grainWarp * 0.40) - 0.5) * 0.62
                             + (BlastNoise(wpIsoB + float2(0.0, gEps * 1.05) - grainWarp.yx * 0.55 + float2(11.3, 7.9)) - 0.5) * 0.38;
                    half grainNormalStrength = max(_MacroBumpScale, 0.30h) * macroWeight * lerp(0.14h, 0.22h, slopeFactor) * (0.45h + 0.55h * midDetailWeight);
                    half2 organicGrainXY = half2(gx - g0, gz - g0) * grainNormalStrength;

                    half fineMidScale = 0.18h * midDetailWeight * lerp(0.45h, 0.70h, slopeFactor);
                    half3 midNormalA = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMacroA), fineMidScale);
                    half3 midNormalB = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMacroB), fineMidScale);
                    half2 midRotA = half2(midNormalA.x * 0.80h + midNormalA.y * 0.60h, -midNormalA.x * 0.60h + midNormalA.y * 0.80h);
                    half2 midRotB = half2(midNormalB.x * 0.55h - midNormalB.y * 0.835h, midNormalB.x * 0.835h + midNormalB.y * 0.55h);

                    // Evaluate home-pad tyre ruts only (no small crater stamps)
                    half4 tyreRuts = SampleNearTyreRuts(wp, camDist);
                    baseAlbedo *= tyreRuts.z;
                    microAo = min(microAo, tyreRuts.w);

                    // Damp planar XZ disturbance on steep faces when triplanar slope mapping is active
                    half planarWeight = saturate(geomNormalWS.y * geomNormalWS.y);
                    half2 distNormalXY = (organicGrainXY + (midRotA + midRotB) * 0.32h
                        + weatheredCraters.xy + tyreRuts.xy * (1.0h - slopeFactor * 0.45h)) * planarWeight;

                    UNITY_BRANCH
                    if (nearWeight > 0.001h)
                    {
                        float2 medWarp = float2(
                            BlastNoise(wp * 0.20 + float2(3.7, 9.2)) - 0.5,
                            BlastNoise(wpRot * 0.20 + float2(14.6, 5.1)) - 0.5) * 0.55;
                        float2 fineWarp = float2(
                            BlastNoise(wp * 0.62 + float2(7.3, 2.9)) - 0.5,
                            BlastNoise(wpRot * 0.62 + float2(15.1, 11.4)) - 0.5) * 0.18;

                        float2 wpMedA = float2(wp.x * 0.8944 - wp.y * 0.4472, wp.x * 0.4472 + wp.y * 0.8944) + medWarp;
                        float2 wpMedB = float2(wp.x * 0.6247 + wp.y * 0.7809, -wp.x * 0.7809 + wp.y * 0.6247) - medWarp.yx * 0.85;
                        float2 uvMedA = wpMedA * 0.152 + float2(0.13, 0.47);
                        float2 uvMedB = wpMedB * 0.102 + float2(0.61, 0.29);

                        float2 wpFineA = float2(wp.x * 0.9799 - wp.y * 0.1994, wp.x * 0.1994 + wp.y * 0.9799) + fineWarp;
                        float2 wpFineB = float2(wp.x * 0.6704 - wp.y * 0.7420, wp.x * 0.7420 + wp.y * 0.6704) - fineWarp.yx * 0.80;
                        float2 uvFineA = wpFineA * 0.425 + float2(0.07, 0.83);
                        float2 uvFineB = wpFineB * 0.280 + float2(0.41, 0.19);

                        half medNoiseBlend = (half)smoothstep(0.22, 0.78, BlastNoise(wp * 0.080 + medWarp * 0.25 + float2(21.4, 8.7)));
                        half fineNoiseBlend = (half)smoothstep(0.22, 0.78, BlastNoise(wpRot * 0.24 + fineWarp * 0.35 + float2(9.3, 31.2)));

                        half4 maskMedA = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvMedA);
                        half4 maskMedB = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvMedB);
                        half medBlend = saturate(medNoiseBlend + (maskMedB.r - maskMedA.r) * 0.14h);
                        half4 maskMed = lerp(maskMedA, maskMedB, medBlend);

                        half4 maskFineA = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvFineA);
                        half4 maskFineB = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvFineB);
                        half fineBlend = saturate(fineNoiseBlend + (maskFineB.r - maskFineA.r) * 0.14h);
                        half4 maskFine = lerp(maskFineA, maskFineB, fineBlend);

                        half3 albedoMedA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMedA).rgb;
                        half3 albedoMedB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMedB).rgb;
                        half3 albedoMed = lerp(albedoMedA, albedoMedB, medBlend);

                        half3 albedoFineA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvFineA).rgb;
                        half3 albedoFineB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvFineB).rgb;
                        half3 albedoFine = lerp(albedoFineA, albedoFineB, fineBlend);

                        half nearPlainBoost = lerp(1.0h, 0.58h, slopeFactor);
                        half medBumpScale = (0.48h * regolithFade120 + 0.28h * regolithNear40) * nearPlainBoost;
                        half fineBumpScale = (0.36h * regolithFade120 + 0.30h * regolithNear40) * nearPlainBoost;

                        half3 nMedARaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMedA), medBumpScale);
                        half3 nMedBRaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMedB), medBumpScale);
                        half2 nMedA = half2(nMedARaw.x * 0.8944h + nMedARaw.y * 0.4472h, -nMedARaw.x * 0.4472h + nMedARaw.y * 0.8944h);
                        half2 nMedB = half2(nMedBRaw.x * 0.6247h - nMedBRaw.y * 0.7809h, nMedBRaw.x * 0.7809h + nMedBRaw.y * 0.6247h);
                        half2 tsMedXY = lerp(nMedA, nMedB, medBlend);

                        half3 nFineARaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvFineA), fineBumpScale);
                        half3 nFineBRaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvFineB), fineBumpScale);
                        half2 nFineA = half2(nFineARaw.x * 0.9799h + nFineARaw.y * 0.1994h, -nFineARaw.x * 0.1994h + nFineARaw.y * 0.9799h);
                        half2 nFineB = half2(nFineBRaw.x * 0.6704h + nFineBRaw.y * 0.7420h, -nFineARaw.x * 0.7420h + nFineBRaw.y * 0.6704h);
                        half2 tsFineXY = lerp(nFineA, nFineB, fineBlend);

                        half2 tsGrainXY = half2(0.0h, 0.0h);
                        half grainAlbedoMod = 1.0h;
                        half grainHeightDelta = 0.0h;
                        half grainAo = 1.0h;
                        half grazingViewDamp = saturate(dot(geomNormalWS, viewDirWS) * 3.0h);
                        half grainWeight = saturate(regolithNear40 * 0.68h + regolithClose20 * 0.32h) * nearPlainBoost;
                        if (grainWeight > 0.005h)
                        {
                            float2 wpGrainA = float2(wp.x * 0.3907 + wp.y * 0.9205, -wp.x * 0.9205 + wp.y * 0.3907) + fineWarp * 0.22;
                            float2 wpGrainB = float2(wp.x * 0.7660 - wp.y * 0.6428, wp.x * 0.6428 + wp.y * 0.7660) - fineWarp.yx * 0.14;
                            float2 uvGrainA = wpGrainA * 0.98 + float2(0.73, 0.19);
                            float2 uvGrainB = wpGrainB * 2.22 + float2(0.29, 0.63);

                            half grainScaleA = (0.22h * regolithNear40 + 0.16h * regolithClose20) * nearPlainBoost * lerp(0.55h, 1.0h, grazingViewDamp);
                            half grainScaleB = (0.14h * regolithNear40 + 0.22h * regolithClose20) * nearPlainBoost * lerp(0.35h, 1.0h, grazingViewDamp);

                            half3 nGrainARaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvGrainA), grainScaleA);
                            half3 nGrainBRaw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvGrainB), grainScaleB);
                            half2 nGrainA = half2(nGrainARaw.x * 0.3907h - nGrainARaw.y * 0.9205h, nGrainARaw.x * 0.9205h + nGrainARaw.y * 0.3907h);
                            half2 nGrainB = half2(nGrainBRaw.x * 0.7660h + nGrainBRaw.y * 0.6428h, -nGrainBRaw.x * 0.6428h + nGrainBRaw.y * 0.7660h);

                            tsGrainXY = nGrainA * 0.65h + nGrainB * 0.55h;

                            half4 maskGrainA = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvGrainA);
                            half4 maskGrainB = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvGrainB);
                            grainHeightDelta = ((maskGrainA.r - 0.50h) * 0.65h + (maskGrainB.r - 0.50h) * 0.35h) * grainWeight * 0.45h;
                            grainAo = lerp(1.0h, saturate(lerp(maskGrainA.g, maskGrainB.g, 0.40h) / 0.865h), grainWeight * 0.42h);

                            half3 albGrainA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvGrainA).rgb;
                            half3 albGrainB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvGrainB).rgb;
                            half grainLuma = dot(lerp(albGrainA, albGrainB, 0.48h), half3(0.2126h, 0.7152h, 0.0722h));
                            grainAlbedoMod = 1.0h + clamp((grainLuma - 0.525h) * 0.32h, -0.036h, 0.036h) * grainWeight;
                        }

                        half3 albedoNear = lerp(albedoMed, albedoFine, 0.50h) * grainAlbedoMod;
                        half4 maskCombined = lerp(maskMed, maskFine, 0.50h);

                        // On slopes & vertical faces, blend in world-space triplanar regolith so UVs never stretch vertically
                        half triSlopeBlend = saturate(slopeFactor * 1.15h);
                        half3 triNormalOffsetWS = half3(0.0h, 0.0h, 0.0h);
                        if (triSlopeBlend > 0.01h)
                        {
                            half3 triAlb;
                            half4 triMask;
                            SampleTriplanarRegolithOnSlope(
                                input.positionWS,
                                geomNormalWS,
                                medBumpScale + fineBumpScale * 0.65h,
                                triAlb,
                                triNormalOffsetWS,
                                triMask);
                            albedoNear = lerp(albedoNear, triAlb, triSlopeBlend);
                            maskCombined = lerp(maskCombined, triMask, triSlopeBlend);
                            regolithTex = lerp(regolithTex, triAlb, triSlopeBlend);
                        }

                        half2 combinedTsXY = (tsMedXY + tsFineXY + tsGrainXY) * (1.0h - triSlopeBlend) + distNormalXY;
                        half3 tsNormal = normalize(half3(combinedTsXY, 1.0h));

                        half lumpHeightAvg = saturate(maskCombined.r + grainHeightDelta * (1.0h - triSlopeBlend));
                        half hollowAoAvg = maskCombined.g * lerp(grainAo, 1.0h, triSlopeBlend);
                        half lumpTopLift = saturate(lumpHeightAvg - 0.50h) * 0.16h - saturate(0.84h - hollowAoAvg) * 0.14h;
                        albedoNear *= 1.0h + lumpTopLift * (0.58h + 0.42h * regolithNear40);

                        regolithTex = lerp(regolithTex, albedoNear, saturate(regolithFade120 * 0.85h + regolithNear40 * 0.15h));

                        normalWS = normalize(
                            tangentWS * tsNormal.x -
                            bitangentWS * tsNormal.y +
                            normalWS * tsNormal.z +
                            triNormalOffsetWS * triSlopeBlend);

                        half combinedMaskG = maskCombined.g;
                        half texAo = saturate(combinedMaskG / 0.865h) * lerp(grainAo, 1.0h, triSlopeBlend);
                        half aoWeight = saturate(regolithFade120 * 0.58h + regolithNear40 * 0.36h) * max(_OcclusionStrength, 0.65h);
                        microAo = min(microAo, saturate(lerp(1.0h, texAo, aoWeight)));

                        half maskSmooth = maskCombined.a;
                        smoothness = saturate(maskSmooth * max(_SmoothnessScale, 0.05h) * regolithFade120 * (1.0h + slopeFactor * 0.25h));
                        regolithShadowWeight = saturate(regolithFade120 * 0.58h + regolithNear40 * 0.42h) * nearPlainBoost;
                        regolithHeight = lumpHeightAvg;
                        clodProminence = maskCombined.b;
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
                    half plainDetailStrength = lerp(0.12h, 0.38h, saturate(regolithFade120 * 0.65h + regolithNear40 * 0.35h));
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

                // Soft directional ambient relief: leeward sides of rounded lumps and soft dimples dip gently in ambient fill
                half geomNdotLMain = dot(geomNormalWS, mainLight.direction);
                half deltaNdotLAmb = dot(normalWS, mainLight.direction) - geomNdotLMain;
                half ambGrazingSlope = deltaNdotLAmb / max(abs(geomNdotLMain) * 0.68h + 0.065h, 0.085h);
                half ambDimpleDepth = saturate((0.49h - regolithHeight) * 1.6h);
                half ambCavityDepth = saturate((0.82h - microAo) * 1.7h);
                half ambShadowSignal = ambGrazingSlope - ambDimpleDepth * 0.18h - ambCavityDepth * 0.16h;
                half ambClodShadow = smoothstep(0.14h, -0.72h, ambShadowSignal) * regolithShadowWeight;
                half ambReliefGain = lerp(0.14h + 0.26h * regolithShadowWeight, 0.34h, slopeFactor);
                half craterAmbRelief = clamp(1.0h + deltaNdotLAmb * ambReliefGain, 0.76h, 1.16h) * lerp(1.0h, lerp(0.68h, 0.42h, sunlit), ambClodShadow);
                half skyLedgeRelief = lerp(craterAmbRelief, clamp(1.0h + (normalWS.y - geomNormalWS.y) * 0.45h + deltaNdotLAmb * 0.30h - (1.0h - microAo) * 0.30h, 0.76h, 1.16h), slopeFactor * 0.75h);
                half3 rawSH = SampleSH(normalWS);
                half shLuma = dot(rawSH, half3(0.2126h, 0.7152h, 0.0722h));
                half3 coolSH = lerp(rawSH, shLuma * half3(0.985h, 0.998h, 1.010h), lerp(0.32h, 0.90h, sunlit));
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
