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

        _NearTiling("Near Tiling (uv/m)", Float) = 0.44
        _MacroTiling("Macro Tiling (uv/m)", Float) = 0.042
        _RockTiling("Triplanar Rock Tiling (uv/m)", Float) = 0.12
        _NearBumpScale("Near Normal Scale", Range(0.0, 2.0)) = 1.05
        _MacroBumpScale("Macro Normal Scale", Range(0.0, 2.0)) = 0.24
        _RockBumpScale("Triplanar Rock Normal Scale", Range(0.0, 2.0)) = 0.88
        _NearFadeStart("Near Detail Fade Start (m)", Float) = 12.0
        _NearFadeEnd("Near Detail Fade End (m)", Float) = 32.0
        _DetailAlbedoStrength("Regolith Albedo Modulation", Range(0.0, 0.6)) = 0.30
        _SmoothnessScale("Smoothness Scale", Range(0.0, 0.5)) = 0.13
        _OcclusionStrength("Micro-Cavity AO Strength", Range(0.0, 1.0)) = 0.52
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

            // Evaluates non-tiling craters (irregular 2x2 cell scatter, power-law size spread, crisp bowl + soft raised rim).
            // rotCosSin = (cos(theta), sin(theta)) of the coordinate rotation so normal perturbations un-rotate to world XZ.
            void AccumulateScatteredCraters2x2(
                float2 coord,
                float2 rotCosSin,
                float occupancy,
                float radMin,
                float radMax,
                float powerExp,
                float normalStrength,
                float rimBright,
                float bowlAo,
                inout half2 normDelta,
                inout half albedoMod,
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

                        // Full-cell 2D random offset (82% of cell) so craters scatter organically, never on a grid
                        float2 jitter = float2(frac(h.x * 13.37 + h.y * 5.19), frac(h.y * 17.73 + h.z * 9.41)) - 0.5;
                        float2 craterCenter = cell + 0.5 + jitter * 0.82;
                        float2 d = coord - craterCenter;

                        // Subtle organic ellipticity
                        float angle = h.x * 6.2831853;
                        float ca = cos(angle), sa = sin(angle);
                        float2 dRot = float2(d.x * ca - d.y * sa, d.x * sa + d.y * ca);
                        dRot.x *= lerp(0.86, 1.14, frac(h.y * 11.13));

                        float r = max(length(dRot), 1e-4);
                        float sizePick = pow(frac(h.x * 7.13 + h.y * 3.71 + h.z * 5.39), powerExp);
                        float rad = lerp(radMin, radMax, sizePick);
                        float u = r / rad;
                        if (u >= 1.92)
                        {
                            continue;
                        }

                        // Un-rotate 2D outward radial vector back to world XZ so lighting direction is exact
                        float2 dirLocal = d / max(length(d), 1e-4);
                        float2 dirWS = float2(
                            dirLocal.x * rotCosSin.x + dirLocal.y * rotCosSin.y,
                            -dirLocal.x * rotCosSin.y + dirLocal.y * rotCosSin.x);

                        // Paraboloid concave bowl (u < 0.86) smoothly rounding over the raised rim crest (u = 1.0)
                        // onto a soft outer ejecta flank (1.0 < u < 1.85). Linear slope (-u) near the center prevents
                        // any flat central dot or dark bullseye ring.
                        float bowlSlope;
                        if (u < 0.86)
                        {
                            bowlSlope = -u * (1.05 - 0.18 * u);
                        }
                        else if (u < 1.0)
                        {
                            float tRim = (u - 0.86) / 0.14;
                            float smoothRim = 1.0 - tRim * tRim * (3.0 - 2.0 * tRim);
                            bowlSlope = -0.86 * (1.05 - 0.18 * 0.86) * smoothRim;
                        }
                        else
                        {
                            float w = saturate((u - 1.0) / 0.85);
                            bowlSlope = sin(w * 3.14159265) * (1.0 - w * 0.48) * 0.44;
                        }

                        float sizeWeight = lerp(0.60, 1.0, saturate((rad - radMin) / max(radMax - radMin, 1e-3)));
                        normDelta += (half2)(dirWS * (bowlSlope * normalStrength * sizeWeight));

                        // Subtle raised rim brightening around u ~ 1.05 and gentle bowl cavity AO (never dark polka-dot rings)
                        float softRim = exp(-(u - 1.05) * (u - 1.05) * 9.0);
                        float bowlInterior = saturate(1.0 - u * u);
                        albedoMod *= (half)(1.0 + (softRim * rimBright - bowlInterior * bowlAo * 0.18) * sizeWeight);
                        aoMod *= (half)(1.0 - bowlInterior * bowlAo * sizeWeight);
                    }
                }
            }

            // Diffuse response for punctual lights (lander floodlights, beacons, pylon lamps).
            half3 PunctualDiffuse(Light light, half3 normalWS, half microAo)
            {
                half ndotl = saturate(dot(normalWS, light.direction));
                float atten = min(light.distanceAttenuation, 16.0) * light.shadowAttenuation * lerp(1.0, (float)microAo, 0.35);
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
                half3 coolLight = lerp(light.color, lightLuma * half3(0.92h, 0.98h, 1.10h), 0.85h);
                return coolLight * (half)(spec * atten);
            }

            float BlastHash(float2 cell)
            {
                return frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Smooth value noise in world metres (0..1), for the halo's blotchy edge and mottle.
            float BlastNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 t = frac(p);
                t = t * t * (3.0 - 2.0 * t);
                float a = BlastHash(cell);
                float b = BlastHash(cell + float2(1.0, 0.0));
                float c = BlastHash(cell + float2(0.0, 1.0));
                float d = BlastHash(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, t.x), lerp(c, d, t.x), t.y);
            }

            // Near-to-chase-field craters (0-58 m): irregular clustered small craters + a few medium ones with soft raised rims,
            // clearly readable from 07's chase camera height, with calm sparse stretches between clusters.
            half4 SampleNearMicroRelief(float2 wp, float camDist)
            {
                half microFade = 1.0h - (half)smoothstep(26.0, 58.0, camDist);
                if (microFade <= 0.001h)
                {
                    return half4(0.0h, 0.0h, 1.0h, 1.0h);
                }

                half2 normDelta = half2(0.0h, 0.0h);
                half albedoMod = 1.0h;
                half aoMod = 1.0h;

                // Domain warp and multi-scale clustering so craters group irregularly in pockets and leave sparse open patches
                float2 warp = float2(
                    BlastNoise(wp * 0.16 + float2(3.7, 11.3)) - 0.5,
                    BlastNoise(wp * 0.16 + float2(19.1, 7.9)) - 0.5) * 0.34;
                float c1 = BlastNoise(wp * 0.055 + float2(15.4, 23.1));
                float c2 = BlastNoise(wp * 0.024 + float2(37.8, 11.6));
                float c3 = BlastNoise(wp * 0.125 + float2(9.2, 47.5));
                float clusterField = c1 * 0.46 + c2 * 0.34 + c3 * 0.20;
                float clusterMask = smoothstep(0.28, 0.70, clusterField);
                float densePocket = smoothstep(0.46, 0.76, clusterField);

                // Scale A: Small craters (~1.85 m cells, diameter ~0.18–0.52 m), sparse in open dust (0.08) and clustered in pockets (0.44)
                AccumulateScatteredCraters2x2(
                    wp * 0.54 + warp + float2(6.3, 14.2),
                    float2(1.0, 0.0),
                    lerp(0.08, 0.44, clusterMask),
                    0.048, 0.138, 1.72,
                    0.85, 0.044, 0.068,
                    normDelta, albedoMod, aoMod);

                // Scale B: Clustered tiny secondary pits (~1.05 m rotated cells, diameter ~0.10–0.28 m) inside pockets
                float2 wpRotA = float2(wp.x * 0.7986 - wp.y * 0.6018, wp.x * 0.6018 + wp.y * 0.7986);
                half pocketFade = 1.0h - (half)smoothstep(18.0, 42.0, camDist);
                if (pocketFade > 0.01h)
                {
                    AccumulateScatteredCraters2x2(
                        wpRotA * 0.95 + warp * 1.10 + float2(17.3, 31.9),
                        float2(0.7986, 0.6018),
                        lerp(0.03, 0.38, densePocket),
                        0.044, 0.125, 1.82,
                        0.70 * pocketFade, 0.035 * pocketFade, 0.054 * pocketFade,
                        normDelta, albedoMod, aoMod);
                }

                // Scale C: A few medium craters with soft raised rims (~4.5 m rotated cells, diameter ~0.55–1.30 m)
                float2 wpRotB = float2(wp.x * 0.5878 + wp.y * 0.8090, -wp.x * 0.8090 + wp.y * 0.5878);
                AccumulateScatteredCraters2x2(
                    wpRotB * 0.22 + warp * 0.50 + float2(27.8, 13.4),
                    float2(0.5878, -0.8090),
                    lerp(0.06, 0.24, clusterMask),
                    0.058, 0.140, 1.62,
                    0.90, 0.052, 0.075,
                    normDelta, albedoMod, aoMod);

                // Soft 3D tyre ruts where 07 drives off the ChargingPad toward the lander and basin.
                if (dot(wp, wp) < 260.0)
                {
                    AccumulateHomeTyreRut(wp, float2(1.80, -5.50), float2(1.10, -12.80), 0.18, normDelta, albedoMod, aoMod);
                    AccumulateHomeTyreRut(wp, float2(1.75, -3.35), float2(0.15, 0.60), -0.16, normDelta, albedoMod, aoMod);
                }

                normDelta *= microFade;
                albedoMod = lerp(1.0h, albedoMod, microFade);
                aoMod = lerp(1.0h, aoMod, microFade);
                return half4(normDelta, albedoMod, aoMod);
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

            // Multi-scale crater decals across BOTH the flat plain and the mountain slopes:
            // irregular clustered small craters + a few medium ones with soft raised rims.
            // Uses slope-compensated coordinates so craters on inclined 30-40 deg slopes remain round and crisp.
            // Returns: xy = world XZ normal perturbation, z = albedo multiplier, w = cavity AO [0..1].
            half4 SampleSlopeCraterDecals(float3 wp3, half3 geomNormalWS, half slopeFactor, float camDist)
            {
                half rangeFade = 1.0h - (half)smoothstep(320.0, 620.0, camDist);
                if (rangeFade <= 0.001h)
                {
                    return half4(0.0h, 0.0h, 1.0h, 1.0h);
                }

                // Slope-compensated projection prevents foreshortening into horizontal slits on steep mountain walls
                float2 sp = wp3.xz - geomNormalWS.xz * (wp3.y * 0.48);
                float2 warpLarge = float2(
                    BlastNoise(sp * 0.032 + float2(3.1, 9.7)) - 0.5,
                    BlastNoise(sp * 0.032 + float2(14.3, 2.9)) - 0.5);
                float2 warpSmall = float2(
                    BlastNoise(sp * 0.11 + float2(21.7, 5.3)) - 0.5,
                    BlastNoise(sp * 0.11 + float2(8.9, 31.1)) - 0.5);
                float2 warp = warpLarge * 0.45 + warpSmall * 0.18;

                // Irregular spatial clustering: dense clusters in some regions, sparse open regolith in others
                float clusterA = BlastNoise(sp * 0.028 + float2(11.3, 24.1));
                float clusterB = BlastNoise(sp * 0.0115 + float2(33.4, 8.7));
                float clusterC = BlastNoise(sp * 0.062 + float2(47.1, 15.3));
                float clusterField = clusterA * 0.46 + clusterB * 0.34 + clusterC * 0.20;
                float clusterGate = smoothstep(0.28, 0.70, clusterField);
                float denseGate = smoothstep(0.46, 0.76, clusterField);

                // Keep craters clearly visible on both flat plains (1.0) and mountain slopes (0.92)
                half slopePresence = lerp(1.0h, 0.92h, slopeFactor);

                half2 normDelta = half2(0.0h, 0.0h);
                half albedoMod = 1.0h;
                half aoMod = 1.0h;

                // Scale 1 (Small Craters on Plain & Slopes): ~5.0 m warped cells, diameter ~0.48–1.35 m
                float2 spRotA = float2(sp.x * 0.7986 - sp.y * 0.6018, sp.x * 0.6018 + sp.y * 0.7986);
                half fadeSmall = (1.0h - (half)smoothstep(190.0, 350.0, camDist)) * slopePresence;
                if (fadeSmall > 0.01h)
                {
                    float occSmall = lerp(0.08, 0.44, clusterGate);
                    AccumulateScatteredCraters2x2(
                        spRotA * 0.198 + warp * 0.95 + float2(15.7, 29.3),
                        float2(0.7986, 0.6018),
                        occSmall, 0.048, 0.132, 1.72,
                        0.85 * fadeSmall,
                        0.044 * fadeSmall, 0.068 * fadeSmall,
                        normDelta, albedoMod, aoMod);
                }

                // Scale 1B (Clustered Small Slope/Plain Pits): ~3.0 m warped cells, diameter ~0.28–0.80 m, active at 20-230 m
                float2 spRotC = float2(sp.x * 0.9063 + sp.y * 0.4226, -sp.x * 0.4226 + sp.y * 0.9063);
                half fadeClusterSmall = (1.0h - (half)smoothstep(165.0, 250.0, camDist)) * slopePresence;
                if (fadeClusterSmall > 0.01h)
                {
                    float occCluster = lerp(0.04, 0.38, denseGate);
                    AccumulateScatteredCraters2x2(
                        spRotC * 0.33 + warp * 1.10 + float2(31.2, 14.8),
                        float2(0.9063, -0.4226),
                        occCluster, 0.045, 0.125, 1.80,
                        0.76 * fadeClusterSmall,
                        0.038 * fadeClusterSmall, 0.058 * fadeClusterSmall,
                        normDelta, albedoMod, aoMod);
                }

                // Scale 2 (A Few Medium Craters with Soft Raised Rims): ~12.2 m warped cells, diameter ~1.15–2.9 m
                float2 spRotB = float2(sp.x * 0.5878 + sp.y * 0.8090, -sp.x * 0.8090 + sp.y * 0.5878);
                half fadeMed = (1.0h - (half)smoothstep(240.0, 480.0, camDist)) * slopePresence;
                if (fadeMed > 0.01h)
                {
                    float occMed = lerp(0.045, 0.22, smoothstep(0.34, 0.74, clusterB * 0.6 + clusterA * 0.4));
                    AccumulateScatteredCraters2x2(
                        spRotB * 0.082 + warp * 0.58 + float2(21.4, 9.2),
                        float2(0.5878, -0.8090),
                        occMed, 0.050, 0.124, 1.65,
                        0.88 * fadeMed,
                        0.048 * fadeMed, 0.070 * fadeMed,
                        normDelta, albedoMod, aoMod);
                }

                return half4(normDelta, albedoMod, aoMod);
            }

            // Multi-scale non-repeating macro albedo variation across 25 m – 1200 m (ejecta blankets, mare/highland patches).
            half SampleMacroRegolithVariation(float3 wp3, half slopeFactor)
            {
                float2 wp = wp3.xz;
                float2 wpRot1 = float2(wp.x * 0.80 - wp.y * 0.60, wp.x * 0.60 + wp.y * 0.80);
                float2 wpRot2 = float2(wp.x * 0.60 + wp.y * 0.80, -wp.x * 0.80 + wp.y * 0.60);

                // Domain-warped coordinates so broad regolith patches flow organically
                float2 macroWarp = float2(
                    BlastNoise(wp * 0.0085 + float2(13.2, 7.9)) - 0.5,
                    BlastNoise(wp * 0.0085 + float2(29.1, 18.4)) - 0.5) * 18.0;
                float2 wpWarped = wp + macroWarp;

                // Scale 1 (~42 m): ejecta blanket & regolith drift mottling
                float n38 = BlastNoise(wpRot1 * 0.024 + float2(5.3, 11.7)) * 0.65
                    + BlastNoise(wpWarped * 0.052 + float2(17.1, 4.9)) * 0.35;
                // Scale 2 (~150 m): highland anorthosite vs mature mare regolith patches
                float n140 = BlastNoise((wpRot2 + macroWarp * 0.6) * 0.0068 + float2(31.4, 19.2));
                // Scale 3 (~420 m): broad mountain/basin tonal variation surviving past 1 km
                float n420 = BlastNoise(wp * 0.0024 + float2(7.8, 43.1));

                float combined = (n38 - 0.5) * 0.24 + (n140 - 0.5) * 0.38 + (n420 - 0.5) * 0.26;
                float heightLift = saturate(wp3.y / 115.0) * (1.0 - slopeFactor * 0.75) * 0.06;

                // Soft painterly S-curve shaping on broad regolith drifts (no periodic contour rings or hard steps)
                float softShaped = smoothstep(-0.34, 0.34, combined) - 0.5;
                float broadDrift = (smoothstep(0.28, 0.72, n140) - 0.5) * 0.12;
                return (half)clamp(1.0 + softShaped * 0.26 + broadDrift + heightLift, 0.82, 1.20);
            }

            // Crisp lunar terminator shading on ridges, rock ledges, and craters at low sun / grazing earthlight.
            half3 MainSunDiffuseStylized(
                Light light,
                half3 normalWS,
                half3 geomNormalWS,
                half3 viewDirWS,
                half sunlit,
                half microAo,
                half slopeFactor,
                half ridgeCrestMask)
            {
                half ndotl = dot(normalWS, light.direction);
                half geomNdotL = dot(geomNormalWS, light.direction);
                half deltaNdotL = ndotl - geomNdotL;

                // In Earthlight (sunlit == 0), signed normal relief (deltaNdotL) carves both sunlit rims and shadowed bowls
                // so craters and broken rock ledges read crisply even under broad Earthshine.
                half earthWrap = saturate(ndotl * 0.84h + 0.16h);
                half earthRelief = clamp(deltaNdotL * 1.55h, -0.38h, 0.55h);
                half earthDiffuse = saturate(pow(earthWrap, 1.15h) * 1.06h + earthRelief);

                // At low sun (sunlit > 0), the 0.5-deg solar disc carves a razor-crisp terminator across ridges, ledges, and craters.
                half terminatorKnee = smoothstep(-0.006h, 0.032h, ndotl);
                half grazingContrast = pow(saturate(ndotl + 0.005h), 1.08h) * terminatorKnee;

                // Lunar opposition / Lommel-Seeliger balance on flat floors while slopes and craters keep crisp relief contrast.
                half ndotv = max(0.22h, saturate(dot(normalWS, viewDirWS)));
                half lommel = grazingContrast / max(0.36h, grazingContrast + ndotv * 0.40h);
                half sunDiffuse = lerp(lommel * 1.04h, grazingContrast * 1.24h, saturate(slopeFactor * 0.75h + 0.25h));

                // Accentuate grazing sunlit ridge crests, crater rims, and rock-ledge lips near the terminator.
                half terminatorZone = smoothstep(-0.04h, 0.06h, geomNdotL) * (1.0h - smoothstep(0.06h, 0.40h, geomNdotL));
                half ridgeCrestBoost = saturate(deltaNdotL) * (0.35h + terminatorZone * 0.52h);
                sunDiffuse += ridgeCrestBoost;

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
                half stylizedDiffuse = lerp(highlandDiffuse, painterlyRamp, 0.45h) + painterlyRidgeStroke;

                // Micro-cavity AO self-shadows craters, broken rock ledges, and cleavage joints.
                half directCavity = lerp(1.0h, microAo, 0.44h + 0.26h * sunlit);
                half diffuse = stylizedDiffuse * directCavity;

                // Crisp lunar shadows in DirectSun (shadowFloor = 0.042) so shadowed slopes stay dark while retaining subtle form
                half shadowFloor = lerp(0.18h, 0.042h, sunlit);
                half atten = max(light.shadowAttenuation, shadowFloor) * light.distanceAttenuation;

                // Neutralize warm golden-hour / dawn sun tint so lunar regolith always reads as neutral cool grey, never beige-brown
                half lightLuma = dot(light.color, half3(0.2126h, 0.7152h, 0.0722h));
                half3 lunarLightColor = lerp(light.color, lightLuma * half3(0.89h, 0.98h, 1.13h), 0.90h);
                return lunarLightColor * (diffuse * atten);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half3 geomNormalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float camDist = distance(_WorldSpaceCameraPos, input.positionWS);
                half sunlit = saturate((half)_MoonSunlitGround);

                // Detail normal & slope geology weights extended across 500 m – 1800 m so distant rim slopes and The Peak
                // retain crisp surface normal relief.
                half nearWeight = 1.0h - (half)smoothstep(_NearFadeStart, max(_NearFadeStart + 1.0, _NearFadeEnd), camDist);
                half midDetailWeight = 1.0h - (half)smoothstep(220.0, 720.0, camDist);
                half macroWeight = 1.0h - (half)smoothstep(720.0, 1550.0, camDist);
                half hillDetailWeight = 1.0h - (half)smoothstep(900.0, 1850.0, camDist);

                // Slope masks: gentle slopes keep lighter regolith, steep faces expose darker basalt rock and broken ledges.
                half slopeFactor = 1.0h - (half)smoothstep(0.75h, 0.945h, geomNormalWS.y);
                half steepFaceMask = 1.0h - (half)smoothstep(0.68h, 0.855h, geomNormalWS.y);
                half gentleSlopeMask = (half)smoothstep(0.80h, 0.92h, geomNormalWS.y);

                // Neutral cool grey lunar regolith base reflectance (~0.12-0.16 linear).
                // Normalized smoothly so baked dark mountain vertex colors don't create a pale collar at the base of the slopes.
                half3 vertexLinear = SRGBToLinear(input.color.rgb) * _BaseColor.rgb;
                half vLuma = dot(vertexLinear, half3(0.2126h, 0.7152h, 0.0722h));
                half vNorm = smoothstep(0.025h, 0.22h, vLuma);
                half lunarReflectance = lerp(0.124h, 0.158h, vNorm);

                // Cool neutral silver-slate compensation neutralizes post-processing warm split-toning (_splitHighlights / _gain)
                // so regolith is true neutral cool grey with zero beige-brown cast.
                half3 coolNeutralTint = lerp(half3(0.89h, 0.96h, 1.10h), half3(0.81h, 0.95h, 1.18h), sunlit);
                half3 baseAlbedo = lunarReflectance * coolNeutralTint;

                // Multi-scale macro albedo variation across 25 m – 1200 m.
                baseAlbedo *= SampleMacroRegolithVariation(input.positionWS, slopeFactor);

                half3 normalWS = geomNormalWS;
                half microAo = 1.0h;
                half smoothness = 0.04h;
                half ridgeCrestMask = 0.0h;

                // Construct world-space tangent frame aligned with +X (U) and +Z (V) around the smooth terrain normal.
                half3 tangentWS = normalize(half3(1.0h, 0.0h, 0.0h) - geomNormalWS * geomNormalWS.x);
                half3 bitangentWS = normalize(cross(geomNormalWS, tangentWS));

                // Multi-scale craters on both flat plain and slopes (small irregular clusters + medium soft-rimmed craters).
                half4 slopeCraters = SampleSlopeCraterDecals(input.positionWS, geomNormalWS, slopeFactor, camDist);
                baseAlbedo *= slopeCraters.z;
                microAo = min(microAo, slopeCraters.w);

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

                    // Cool neutral slate-basalt tint on dark exposed rock (never warm brown)
                    half3 darkRockTint = lerp(half3(1.0h, 1.0h, 1.0h), half3(0.89h, 0.94h, 1.05h), saturate(exposedRockMask * 0.70h + terraceRiser * 0.45h));

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

                // Multi-scale regolith detail normal and near-to-chase-field crater relief
                UNITY_BRANCH
                if (macroWeight > 0.001h)
                {
                    float2 wp = input.positionWS.xz;
                    float2 wpRot = float2(wp.x * 0.80 - wp.y * 0.60, wp.x * 0.60 + wp.y * 0.80);
                    float2 uvMacro = wpRot * _MacroTiling + float2(0.37, 0.71);
                    float2 uvFar = wp * (_MacroTiling * 0.38) + float2(0.61, 0.23);
                    float2 uvMid = wp * 0.115 + float2(0.14, 0.52);

                    half3 regolithTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMacro).rgb;
                    half3 regolithMid = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMid).rgb;
                    regolithTex = lerp(regolithTex, regolithMid, midDetailWeight * 0.42h);

                    // Keep _BumpMap gentle so its baked circular bubbles only provide fine dust grain
                    half slopeNormalBoost = lerp(0.18h, 0.22h, slopeFactor);
                    half macroBumpStrength = max(_MacroBumpScale, 0.44h) * macroWeight * slopeNormalBoost;
                    half3 macroNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMacro), macroBumpStrength);
                    half3 farNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvFar), macroBumpStrength * 0.80h);
                    half3 midNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMid), 0.44h * midDetailWeight * slopeNormalBoost);

                    // Evaluate near-to-chase-field craters (0-58 m) without double-multiplying by nearWeight
                    half4 microRelief = SampleNearMicroRelief(wp, camDist);
                    baseAlbedo *= microRelief.z;
                    microAo = min(microAo, microRelief.w);

                    half2 distNormalXY = macroNormal.xy * 0.70h + farNormal.xy * 0.60h + midNormal.xy * 0.65h
                        + slopeCraters.xy + microRelief.xy * (1.0h - slopeFactor * 0.45h);

                    UNITY_BRANCH
                    if (nearWeight > 0.001h)
                    {
                        float2 uvNear1 = wp * _NearTiling;
                        float2 wpNearRot = float2(wp.x * 0.7986 - wp.y * 0.6018, wp.x * 0.6018 + wp.y * 0.7986);
                        float2 uvNear2 = wpNearRot * (_NearTiling * 0.47) + float2(0.19, 0.63);

                        half3 albedoNear1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvNear1).rgb;
                        half3 albedoNear2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvNear2).rgb;
                        half3 albedoNear = lerp(albedoNear1, albedoNear2, 0.48h);

                        half4 maskNear1 = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvNear1);
                        half4 maskNear2 = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvNear2);
                        half4 maskNear = lerp(maskNear1, maskNear2, 0.48h);

                        half bumpScale = (_NearBumpScale * 0.20h + _MacroBumpScale * 0.12h) * nearWeight;
                        half3 tsNormal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvNear1), bumpScale);
                        half3 tsNormal2Raw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvNear2), bumpScale * 0.82h);
                        half2 tsRot2 = half2(
                            tsNormal2Raw.x * 0.7986h + tsNormal2Raw.y * 0.6018h,
                            -tsNormal2Raw.x * 0.6018h + tsNormal2Raw.y * 0.7986h);
                        half2 combinedTsXY = tsNormal1.xy * 0.65h + tsRot2 * 0.55h + distNormalXY;

                        half3 tsNormal = normalize(half3(combinedTsXY, 1.0h));
                        regolithTex = lerp(regolithTex, albedoNear, nearWeight * 0.45h);

                        normalWS = normalize(
                            tangentWS * tsNormal.x -
                            bitangentWS * tsNormal.y +
                            normalWS * tsNormal.z);

                        half texAo = lerp(1.0h, saturate(maskNear.g / 0.870h), 0.25h);
                        microAo = min(microAo, saturate(lerp(1.0h, texAo, _OcclusionStrength * nearWeight)));
                        smoothness = saturate(maskNear.a * _SmoothnessScale * nearWeight * (1.0h + slopeFactor * 0.25h));
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
                    half detailStrength = lerp(0.14h, 0.30h, slopeFactor) * macroWeight * (0.55h + 0.45h * max(nearWeight, midDetailWeight));
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

                // Subtle sky-dome / Earthshine directional normal relief so craters and ledges read cleanly without glowing bars in shadow
                half deltaNdotLAmb = dot(normalWS, mainLight.direction) - dot(geomNormalWS, mainLight.direction);
                half craterAmbRelief = clamp(1.0h + deltaNdotLAmb * 0.42h, 0.72h, 1.22h);
                half skyLedgeRelief = lerp(craterAmbRelief, clamp(1.0h + (normalWS.y - geomNormalWS.y) * 0.65h + deltaNdotLAmb * 0.35h - (1.0h - microAo) * 0.35h, 0.72h, 1.18h), slopeFactor * 0.80h);
                half3 rawSH = SampleSH(normalWS);
                half shLuma = dot(rawSH, half3(0.2126h, 0.7152h, 0.0722h));
                half3 coolSH = lerp(rawSH, shLuma * half3(0.90h, 0.97h, 1.10h), 0.80h);
                half3 ambient = coolSH * ambientAo * skyLedgeRelief;
                half3 lighting = ambient + MainSunDiffuseStylized(mainLight, normalWS, geomNormalWS, viewDirWS, sunlit, microAo, slopeFactor, ridgeCrestMask);
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
                        lighting += PunctualDiffuse(light, normalWS, microAo);
                        specular += RegolithMicroSpecular(light, normalWS, viewDirWS, smoothness);
                    LIGHT_LOOP_END
                #endif

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
