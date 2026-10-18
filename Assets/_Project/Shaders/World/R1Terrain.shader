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
        _TerrainVariant("Terrain Look Variant (-1 = Global, 0 = Highland Realism A, 1 = Stylized B)", Float) = -1.0
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
                float _TerrainVariant;
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
            float _MoonTerrainVariant; // < -0.5 = legacy current, 0 = Terrain A (Highland realism), 1 = Terrain B (Stylized)

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

            // Evaluates non-tiling near-field micro-craters (sparse, crisp raised rims, no dark circular stains),
            // dual-scale crunchy regolith clasts, and soft 3D rover tyre ruts across the 2-16 m band.
            // Returns: xy = tangent normal perturbation, z = albedo multiplier (centered at 1), w = cavity AO [0..1].
            half4 SampleNearMicroRelief(float2 wp, float camDist)
            {
                half microFade = 1.0h - (half)smoothstep(10.0, 22.0, camDist);
                if (microFade <= 0.001h)
                {
                    return half4(0.0h, 0.0h, 1.0h, 1.0h);
                }

                half2 normDelta = half2(0.0h, 0.0h);
                half albedoMod = 1.0h;
                half aoMod = 1.0h;

                // Layer 1: Sparse fresh small impact micro-craters (~3.4 m cells, 22% occupancy) with crisp raised rims.
                float2 craterWarp = float2(
                    sin(wp.y * 0.37 + wp.x * 0.19),
                    cos(wp.x * 0.33 - wp.y * 0.23)) * 0.14;
                float2 craterGrid = wp * 0.29 + craterWarp;
                float2 craterCell = floor(craterGrid);
                float2 craterFrac = frac(craterGrid) - 0.5;
                float3 hC = Hash23(craterCell);
                if (hC.z < 0.22)
                {
                    float2 center = (hC.xy - 0.5) * 0.56;
                    float2 d = craterFrac - center;
                    float r = max(length(d), 1e-4);
                    float rad = lerp(0.075, 0.145, frac(hC.x * 7.13));
                    float u = r / rad;
                    if (u < 1.75)
                    {
                        float2 dir = d / r;
                        // Inside bowl (u < 1): concave normal pointing inward; on rim (1 <= u < 1.50): crisp outward slope.
                        float bowlSlope = u < 1.0 ? (-u * 0.82) : (exp(-(u - 1.05) * (u - 1.05) * 18.0) * (1.15 - u) * 2.2);
                        normDelta += (half2)(dir * bowlSlope * 0.44);

                        float rimCrest = exp(-(u - 1.02) * (u - 1.02) * 24.0);
                        float bowlInterior = saturate(1.0 - u * u);
                        // Bright raised rim lip; bowl shading comes from the normal map under raking light, never a dark circle stain.
                        albedoMod *= (half)(1.0 + rimCrest * 0.062);
                        aoMod *= (half)(1.0 - bowlInterior * 0.04);
                    }
                }

                // Layer 2: Crunchy embedded grit pebbles & small breccia clasts (~0.52 m cells, radius ~2-5 cm).
                float2 pebbleGrid = wp * 1.92 + float2(17.3, 31.7);
                float2 pebbleCell = floor(pebbleGrid);
                float2 pebbleFrac = frac(pebbleGrid) - 0.5;
                float3 hP = Hash23(pebbleCell);
                if (hP.z < 0.82)
                {
                    float2 center = (hP.xy - 0.5) * 0.54;
                    float2 d = pebbleFrac - center;
                    float angle = hP.z * 6.28318;
                    float ca = cos(angle), sa = sin(angle);
                    float2 dRot = float2(d.x * ca - d.y * sa, d.x * sa + d.y * ca);
                    dRot.x *= lerp(0.72, 1.38, frac(hP.x * 13.7));
                    float r = max(length(dRot), 1e-4);
                    float rad = lerp(0.055, 0.135, frac(hP.y * 9.3));
                    float u = r / rad;
                    if (u < 1.4)
                    {
                        float2 dir = d / max(length(d), 1e-4);
                        float domeSlope = u < 1.0 ? (u * (1.0 - u * 0.35) * 1.10) : (-exp(-(u - 1.05) * (u - 1.05) * 26.0) * 0.30);
                        normDelta += (half2)(dir * domeSlope * 0.44);
                        float stoneMask = saturate(1.0 - u * u);
                        float moatMask = exp(-(u - 1.04) * (u - 1.04) * 28.0);
                        half stoneTone = lerp(-0.068h, 0.058h, (half)frac(hP.x + hP.y));
                        albedoMod *= (half)(1.0 + stoneMask * stoneTone - moatMask * 0.038);
                        aoMod *= (half)(1.0 - moatMask * 0.16);
                    }
                }

                // Layer 3: Secondary fine gravel/grit clasts (~0.27 m rotated cells, radius ~1-3 cm) for close-up crunch.
                float2 wpGritRot = float2(wp.x * 0.6428 + wp.y * 0.7660, -wp.x * 0.7660 + wp.y * 0.6428);
                float2 gritGrid = wpGritRot * 3.65 + float2(43.1, 11.9);
                float2 gritCell = floor(gritGrid);
                float2 gritFrac = frac(gritGrid) - 0.5;
                float3 hG = Hash23(gritCell);
                if (hG.z < 0.85)
                {
                    float2 center = (hG.xy - 0.5) * 0.52;
                    float2 d = gritFrac - center;
                    float r = max(length(d), 1e-4);
                    float rad = lerp(0.07, 0.16, frac(hG.x * 11.3));
                    float u = r / rad;
                    if (u < 1.35)
                    {
                        float2 dir = d / r;
                        float domeSlope = u < 1.0 ? (u * (1.0 - u * 0.35) * 0.95) : (-exp(-(u - 1.04) * (u - 1.04) * 28.0) * 0.24);
                        normDelta += (half2)(dir * domeSlope * 0.34);
                        float stoneMask = saturate(1.0 - u * u);
                        half stoneTone = lerp(-0.055h, 0.048h, (half)frac(hG.y * 5.7));
                        albedoMod *= (half)(1.0 + stoneMask * stoneTone);
                        aoMod *= (half)(1.0 - stoneMask * 0.06);
                    }
                }

                // Layer 4: Soft 3D tyre ruts where 07 drives off the ChargingPad toward the lander and basin.
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

            // Legacy diffuse response for the current (pre-pass) baseline capture.
            half3 MainSunDiffuse(Light light, half3 normalWS, half3 viewDirWS, half sunlit, half microAo, half slopeFactor)
            {
                half ndotl = dot(normalWS, light.direction);
                half wrapped = saturate(ndotl * 0.85h + 0.15h);
                half crisp = saturate(ndotl);

                half ndotv = max(0.20h, saturate(dot(normalWS, viewDirWS)));
                half lommel = crisp / max(0.32h, crisp + ndotv * 0.54h);
                half lunarCrisp = lerp(crisp, lommel * 0.96h, slopeFactor * 0.36h);

                half diffuse = lerp(wrapped, lunarCrisp, sunlit);

                half directCavity = lerp(1.0h, microAo, 0.25h + 0.30h * sunlit);
                diffuse *= directCavity;

                half shadowFloor = lerp(0.18h, 0.03h, sunlit);
                half atten = max(light.shadowAttenuation, shadowFloor) * light.distanceAttenuation;
                return light.color * (diffuse * atten);
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
                return light.color * (half)(spec * atten);
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

            // Exact pre-pass terrain shading preserved for the 'current' baseline comparison capture.
            half4 FragCurrent(Varyings input)
            {
                half3 geomNormalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float camDist = distance(_WorldSpaceCameraPos, input.positionWS);

                half nearWeight = 1.0h - (half)smoothstep(_NearFadeStart, max(_NearFadeStart + 1.0, _NearFadeEnd), camDist);
                half macroWeight = 1.0h - (half)smoothstep(180.0, 420.0, camDist);
                half hillDetailWeight = 1.0h - (half)smoothstep(260.0, 950.0, camDist);

                half slopeFactor = 1.0h - (half)smoothstep(_SlopeThreshold - _SlopeBlendRange, _SlopeThreshold + _SlopeBlendRange, geomNormalWS.y);
                half steepOutcropFactor = 1.0h - (half)smoothstep(0.70h, 0.86h, geomNormalWS.y);

                half3 vertexLinear = SRGBToLinear(input.color.rgb) * _BaseColor.rgb;
                half3 normalWS = geomNormalWS;
                half3 baseAlbedo = vertexLinear;
                half microAo = 1.0h;
                half smoothness = 0.035h;

                half3 tangentWS = normalize(half3(1.0h, 0.0h, 0.0h) - geomNormalWS * geomNormalWS.x);
                half3 bitangentWS = normalize(cross(geomNormalWS, tangentWS));

                UNITY_BRANCH
                if (slopeFactor > 0.01h && hillDetailWeight > 0.01h)
                {
                    float3 wp3 = input.positionWS;
                    half3 triWeights = abs(geomNormalWS);
                    triWeights = pow(max(triWeights, 0.001h), 4.0h);
                    triWeights /= (triWeights.x + triWeights.y + triWeights.z);

                    float2 striaUV_X = float2(wp3.z * 0.024, wp3.y * 0.0055);
                    float2 striaUV_Z = float2(wp3.x * 0.024, wp3.y * 0.0055);
                    half striaSampleX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, striaUV_X).g;
                    half striaSampleZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, striaUV_Z).g;
                    half horizWeightSum = max(0.001h, triWeights.x + triWeights.z);
                    half striationRaw = (striaSampleX * triWeights.x + striaSampleZ * triWeights.z) / horizWeightSum;
                    half talusChute = saturate((striationRaw - 0.750h) * 5.2h + 0.5h);

                    half slumpNoise1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp3.xz * 0.0038 + float2(0.17, 0.43)).r;
                    half slumpNoise2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp3.zx * 0.0062 + float2(0.61, 0.29)).g;
                    half slumpPocket = (half)smoothstep(0.735, 0.785, slumpNoise1 * 0.65h + slumpNoise2 * 0.35h);
                    float terraceCoord = wp3.y * 0.052 + (slumpNoise1 - 0.750) * 6.5 + (striationRaw - 0.750) * 2.4;
                    float terraceFrac = frac(terraceCoord);
                    half terraceRiser = slumpPocket * (half)(smoothstep(0.48, 0.74, terraceFrac) * (1.0 - smoothstep(0.74, 0.96, terraceFrac)));
                    half terraceBench = slumpPocket * (half)(smoothstep(0.06, 0.28, terraceFrac) * (1.0 - smoothstep(0.28, 0.48, terraceFrac)));

                    half outcropMask = saturate((steepOutcropFactor * 0.82h + terraceRiser * 0.34h * slopeFactor + (1.0h - talusChute) * 0.28h * slopeFactor) * hillDetailWeight);

                    float3 rockUV = wp3 * _RockTiling;
                    half3 rockAlbedoX = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV.zy).rgb;
                    half3 rockAlbedoY = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV.xz).rgb;
                    half3 rockAlbedoZ = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV.xy).rgb;
                    half3 rockTex = rockAlbedoX * triWeights.x + rockAlbedoY * triWeights.y + rockAlbedoZ * triWeights.z;

                    half3 rockNormX = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUV.zy), _RockBumpScale);
                    half3 rockNormZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUV.xy), _RockBumpScale);
                    half2 triTsXY = (rockNormX.xy * triWeights.x + rockNormZ.xy * triWeights.z) * (0.38h * slopeFactor + 0.62h * outcropMask) * hillDetailWeight;

                    half terraceTilt = (terraceRiser * 0.14h - terraceBench * 0.10h) * slopeFactor * hillDetailWeight;
                    normalWS = normalize(
                        geomNormalWS +
                        half3(0.0h, -terraceTilt, 0.0h) +
                        tangentWS * triTsXY.x -
                        bitangentWS * triTsXY.y);

                    half3 rockCentered = clamp(rockTex / max(0.08h, dot(rockTex, half3(0.333h, 0.333h, 0.333h))), 0.78h, 1.22h);
                    half slopeValueMod = lerp(1.0h, 0.82h, outcropMask);
                    slopeValueMod *= lerp(0.90h, 1.10h, talusChute * slopeFactor * hillDetailWeight);
                    slopeValueMod *= lerp(1.0h, 1.05h, terraceBench * slopeFactor * hillDetailWeight);

                    baseAlbedo *= slopeValueMod * lerp(1.0h.xxx, rockCentered, outcropMask * 0.42h);
                    microAo = min(microAo, lerp(1.0h, 0.86h, outcropMask));
                }

                UNITY_BRANCH
                if (macroWeight > 0.001h)
                {
                    float2 wp = input.positionWS.xz;
                    float2 wpRot = float2(wp.x * 0.80 - wp.y * 0.60, wp.x * 0.60 + wp.y * 0.80);
                    float2 uvMacro = wpRot * _MacroTiling + float2(0.37, 0.71);

                    half3 regolithTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMacro).rgb;
                    half3 macroNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMacro), _MacroBumpScale * macroWeight);

                    UNITY_BRANCH
                    if (nearWeight > 0.001h)
                    {
                        float2 uvNear1 = wp * _NearTiling;
                        float2 wpNearRot = float2(wp.x * 0.7986 - wp.y * 0.6018, wp.x * 0.6018 + wp.y * 0.7986);
                        float2 uvNear2 = wpNearRot * (_NearTiling * 0.47) + float2(0.19, 0.63);

                        half3 albedoNear1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvNear1).rgb;
                        half3 albedoNear2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvNear2).rgb;
                        half3 albedoNear = lerp(albedoNear1, albedoNear2, 0.44h);

                        half4 maskNear1 = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvNear1);
                        half4 maskNear2 = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvNear2);
                        half4 maskNear = lerp(maskNear1, maskNear2, 0.42h);

                        half bumpScale = (_NearBumpScale * 0.92h + _MacroBumpScale * 0.25h) * nearWeight * (1.0h + slopeFactor * 0.20h);
                        half3 tsNormal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvNear1), bumpScale);
                        half3 tsNormal2Raw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvNear2), bumpScale * 0.76h);
                        half2 tsRot2 = half2(
                            tsNormal2Raw.x * 0.7986h + tsNormal2Raw.y * 0.6018h,
                            -tsNormal2Raw.x * 0.6018h + tsNormal2Raw.y * 0.7986h);
                        half2 combinedTsXY = tsNormal1.xy * 0.74h + tsRot2 * 0.58h + macroNormal.xy * 0.22h;

                        half4 microRelief = SampleNearMicroRelief(wp, camDist);
                        combinedTsXY += microRelief.xy * (nearWeight * (1.0h - slopeFactor * 0.65h));

                        half3 tsNormal = normalize(half3(combinedTsXY, 1.0h));
                        regolithTex = lerp(regolithTex, albedoNear, nearWeight * 0.80h);
                        regolithTex *= lerp(1.0h, microRelief.z, nearWeight);

                        normalWS = normalize(
                            tangentWS * tsNormal.x -
                            bitangentWS * tsNormal.y +
                            normalWS * tsNormal.z);

                        half texAo = saturate(maskNear.g / 0.870h) * microRelief.w;
                        microAo = min(microAo, saturate(lerp(1.0h, texAo, _OcclusionStrength * nearWeight)));
                        smoothness = saturate(maskNear.a * _SmoothnessScale * nearWeight * (1.0h + slopeFactor * 0.25h));
                    }
                    else
                    {
                        normalWS = normalize(
                            tangentWS * macroNormal.x -
                            bitangentWS * macroNormal.y +
                            normalWS * macroNormal.z);
                    }

                    half3 regolithCentered = regolithTex / 0.525h;
                    half detailStrength = _DetailAlbedoStrength * macroWeight * (0.35h + 0.65h * nearWeight);
                    baseAlbedo *= lerp(1.0h.xxx, regolithCentered, detailStrength);
                }

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

                half sunlit = saturate((half)_MoonSunlitGround);
                half3 ambient = SampleSH(normalWS) * ambientAo;
                half3 lighting = ambient + MainSunDiffuse(mainLight, normalWS, viewDirWS, sunlit, microAo, slopeFactor);
                half3 specular = RegolithMicroSpecular(mainLight, normalWS, viewDirWS, smoothness);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = normalizedScreenSpaceUV;

                    #if USE_FORWARD_PLUS
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
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

                if (_MoonReachRing.w > 0.001)
                {
                    float dist = distance(input.positionWS.xz, _MoonReachRing.xy);
                    float delta = dist - _MoonReachRing.w;
                    float ring = 1.0 - smoothstep(0.0, 0.14, abs(delta));
                    float fill = step(dist, _MoonReachRing.w) * 0.07;
                    half mask = saturate(ring * 0.85 + fill) * _MoonReachRingColor.a;
                    color = lerp(color, _MoonReachRingColor.rgb, mask);
                }

                color = clamp(color, 0.0h, 32.0h);
                color = MoonAtmosphere(color, input.positionWS, input.fogFactor);
                return half4(color, 1.0h);
            }

            // Evaluates a single Voronoi/grid layer of small impact craters on slopes and highlands.
            void AccumulateSlopeCraterLayer(
                float2 coord,
                float occupancy,
                float normalStrength,
                float rimBright,
                float bowlAo,
                inout half2 normDelta,
                inout half albedoMod,
                inout half aoMod)
            {
                float2 cell = floor(coord);
                float2 f = frac(coord) - 0.5;
                float3 h = Hash23(cell);
                if (h.z >= occupancy)
                {
                    return;
                }

                float2 center = (h.xy - 0.5) * 0.52;
                float2 d = f - center;
                float r = max(length(d), 1e-4);
                float rad = lerp(0.11, 0.22, frac(h.x * 7.13 + h.y * 3.71));
                float u = r / rad;
                if (u >= 1.85)
                {
                    return;
                }

                float2 dir = d / r;
                // Concave crater bowl inside (u < 1) and crisp raised rim lip outside (1 <= u < 1.65)
                float bowlSlope = u < 1.0
                    ? (-u * (1.15 - 0.25 * u))
                    : (exp(-(u - 1.06) * (u - 1.06) * 16.0) * (1.24 - u) * 2.45);
                normDelta += (half2)(dir * (bowlSlope * normalStrength));

                float rimCrest = exp(-(u - 1.03) * (u - 1.03) * 20.0);
                float ejectaSkirt = exp(-(u - 1.18) * (u - 1.18) * 8.5);
                float bowlInterior = saturate(1.0 - u * u);
                albedoMod *= (half)(1.0 + (rimCrest * 1.15 + ejectaSkirt * 0.45) * rimBright - bowlInterior * (rimBright * 0.42));
                aoMod *= (half)(1.0 - bowlInterior * bowlAo);
            }

            // Multi-scale small crater decals & regolith pitting on slopes and highlands across 8 m – 780 m.
            // Returns: xy = world XZ normal perturbation, z = albedo multiplier, w = cavity AO [0..1].
            half4 SampleSlopeCraterDecals(float3 wp3, half slopeFactor, float camDist)
            {
                half rangeFade = 1.0h - (half)smoothstep(420.0, 820.0, camDist);
                if (rangeFade <= 0.001h)
                {
                    return half4(0.0h, 0.0h, 1.0h, 1.0h);
                }

                // Continuous slope-projected coordinate so craters stay round on inclined mountain flanks.
                float2 sp = wp3.xz + float2(wp3.y * 0.28, -wp3.y * 0.24);
                float2 warp = float2(
                    BlastNoise(sp * 0.045 + float2(3.1, 9.7)) - 0.5,
                    BlastNoise(sp * 0.045 + float2(14.3, 2.9)) - 0.5) * 0.22;

                half2 normDelta = half2(0.0h, 0.0h);
                half albedoMod = 1.0h;
                half aoMod = 1.0h;

                // Layer A: Small slope craters (~5.8 m cells, visible out to 280 m)
                half fadeSmall = 1.0h - (half)smoothstep(160.0, 310.0, camDist);
                if (fadeSmall > 0.01h)
                {
                    AccumulateSlopeCraterLayer(sp * 0.172 + warp, 0.52, 0.62 * fadeSmall, 0.085 * fadeSmall, 0.14 * fadeSmall,
                        normDelta, albedoMod, aoMod);
                }

                // Layer B: Medium slope & highland craters (~14.5 m cells, visible out to 580 m)
                half fadeMed = 1.0h - (half)smoothstep(340.0, 620.0, camDist);
                if (fadeMed > 0.01h)
                {
                    float2 spRotB = float2(sp.x * 0.819 - sp.y * 0.574, sp.x * 0.574 + sp.y * 0.819);
                    AccumulateSlopeCraterLayer(spRotB * 0.069 + warp * 0.6 + float2(19.4, 7.2), 0.58,
                        0.68 * fadeMed, 0.095 * fadeMed, 0.16 * fadeMed, normDelta, albedoMod, aoMod);
                }

                // Layer C: Larger highland slope craters (~34 m cells, visible out to 820 m)
                float2 spRotC = float2(sp.x * 0.643 + sp.y * 0.766, -sp.x * 0.766 + sp.y * 0.643);
                AccumulateSlopeCraterLayer(spRotC * 0.0295 + float2(41.7, 23.8), 0.54,
                    0.58 * rangeFade, 0.088 * rangeFade, 0.15 * rangeFade, normDelta, albedoMod, aoMod);

                // Layer D: Continuous slope regolith pitting & slumped boulder-track micro-relief (stronger on slopes)
                float pit1 = BlastNoise(sp * 0.36 + warp * 2.2) - 0.5;
                float pit2 = BlastNoise(spRotB * 0.88 + float2(11.3, 29.1)) - 0.5;
                float pitEps = 0.65;
                float pitX = (BlastNoise((sp + float2(pitEps, 0.0)) * 0.36 + warp * 2.2) - 0.5) - pit1;
                float pitZ = (BlastNoise((sp + float2(0.0, pitEps)) * 0.36 + warp * 2.2) - 0.5) - pit1;
                half slopeBoost = lerp(0.55h, 1.35h, slopeFactor) * rangeFade;
                normDelta += half2(pitX, pitZ) * (1.55h * slopeBoost);
                albedoMod *= (half)(1.0 + (pit1 * 0.09 + pit2 * 0.06) * slopeBoost);

                return half4(normDelta * lerp(0.62h, 1.25h, slopeFactor), albedoMod, aoMod);
            }

            // Multi-scale non-repeating macro albedo variation across 25 m – 1200 m (ejecta blankets, mare/highland patches).
            half SampleMacroRegolithVariation(float3 wp3, half slopeFactor)
            {
                float2 wp = wp3.xz;
                float2 wpRot1 = float2(wp.x * 0.80 - wp.y * 0.60, wp.x * 0.60 + wp.y * 0.80);
                float2 wpRot2 = float2(wp.x * 0.60 + wp.y * 0.80, -wp.x * 0.80 + wp.y * 0.60);

                // Scale 1 (~38 m): ejecta ray & regolith blanket mottling
                float n38 = BlastNoise(wpRot1 * 0.026 + float2(5.3, 11.7)) * 0.65
                    + BlastNoise(wp * 0.062 + float2(17.1, 4.9)) * 0.35;
                // Scale 2 (~140 m): highland anorthosite vs mature mare regolith patches
                float n140 = BlastNoise(wpRot2 * 0.0072 + float2(31.4, 19.2));
                // Scale 3 (~420 m): broad mountain/basin tonal variation surviving past 1 km
                float n420 = BlastNoise(wp * 0.0024 + float2(7.8, 43.1));

                float combined = (n38 - 0.5) * 0.30 + (n140 - 0.5) * 0.36 + (n420 - 0.5) * 0.24;
                // Higher elevations / highland ridges carry slightly brighter anorthositic regolith on gentle crests,
                // while steeper flanks strip down to darker bedrock in the slope block.
                float heightLift = saturate(wp3.y / 110.0) * (1.0 - slopeFactor * 0.85) * 0.08;
                return (half)clamp(1.0 + combined + heightLift, 0.76, 1.26);
            }

            // Sharper terminator shading on ridges at low sun / grazing earthlight for Terrain A ("Highland realism").
            half3 MainSunDiffuseHighland(
                Light light,
                half3 normalWS,
                half3 geomNormalWS,
                half3 viewDirWS,
                half sunlit,
                half microAo,
                half slopeFactor)
            {
                half ndotl = dot(normalWS, light.direction);
                half geomNdotL = dot(geomNormalWS, light.direction);

                // Sharp terminator knee on ridges and crater walls so low-angle sun and 19-deg earthlight carve
                // crisp light-to-shadow ridge separation instead of soft wrapped clay.
                half terminatorKnee = smoothstep(-0.025h, 0.085h, ndotl);
                half grazingContrast = pow(saturate(ndotl + 0.018h), 1.24h) * terminatorKnee;

                // Lunar opposition / Lommel-Seeliger balance on bright flat floors, while ridges keep high relief contrast.
                half ndotv = max(0.22h, saturate(dot(normalWS, viewDirWS)));
                half lommel = grazingContrast / max(0.42h, grazingContrast + ndotv * 0.34h);
                half highlandDiffuse = lerp(lommel * 1.06h, grazingContrast * 1.18h, saturate(slopeFactor * 0.78h + 0.22h));

                // Accentuate grazing sunlit ridge crests where detail normal catches the low sun near the terminator.
                half terminatorZone = smoothstep(-0.04h, 0.06h, geomNdotL) * (1.0h - smoothstep(0.06h, 0.38h, geomNdotL));
                half ridgeCrestBoost = saturate(ndotl - geomNdotL * 0.85h) * terminatorZone * 0.45h;
                highlandDiffuse += ridgeCrestBoost;

                // Subtle cool fill in deep shadow under Earthlight so unlit craters remain readable without washing out ridges.
                half earthlightTail = smoothstep(-0.22h, 0.02h, ndotl) * 0.045h * (1.0h - sunlit);
                half diffuse = highlandDiffuse + earthlightTail;

                // Micro-cavity AO self-shadows craters, rock ledges, and regolith clasts under raking light.
                half directCavity = lerp(1.0h, microAo, 0.42h + 0.28h * sunlit);
                diffuse *= directCavity;

                half shadowFloor = lerp(0.09h, 0.018h, sunlit);
                half atten = max(light.shadowAttenuation, shadowFloor) * light.distanceAttenuation;
                return light.color * (diffuse * atten);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Preserve exact pre-pass terrain look when _MoonTerrainVariant < -0.5 for baseline comparison captures.
                if (_MoonTerrainVariant < -0.5)
                {
                    return FragCurrent(input);
                }

                half3 geomNormalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float camDist = distance(_WorldSpaceCameraPos, input.positionWS);
                half sunlit = saturate((half)_MoonSunlitGround);

                // Detail normal & slope geology weights extended to 500 m – 1800 m so distant rim slopes and The Peak
                // retain crisp surface normal relief instead of fading to smooth clay at 420 m.
                half nearWeight = 1.0h - (half)smoothstep(_NearFadeStart, max(_NearFadeStart + 1.0, _NearFadeEnd), camDist);
                half midDetailWeight = 1.0h - (half)smoothstep(220.0, 720.0, camDist);
                half macroWeight = 1.0h - (half)smoothstep(720.0, 1550.0, camDist);
                half hillDetailWeight = 1.0h - (half)smoothstep(900.0, 1850.0, camDist);

                // Steeper slopes start revealing rocky regolith from ~20 deg (normal.y < 0.94), reaching dark exposed
                // basalt bedrock on steep crater walls, canyon walls, and mountain ridges.
                half slopeFactor = 1.0h - (half)smoothstep(0.74h, 0.935h, geomNormalWS.y);
                half steepOutcropFactor = 1.0h - (half)smoothstep(0.62h, 0.85h, geomNormalWS.y);

                // Cool neutral grey lunar regolith albedo (lunar albedo ~0.12 linear, counteracting warm beige cast).
                half3 vertexLinear = SRGBToLinear(input.color.rgb) * _BaseColor.rgb;
                half vLuma = dot(vertexLinear, half3(0.2126h, 0.7152h, 0.0722h));
                half lunarReflectance = pow(max(vLuma / 0.225h, 0.01h), 1.16h) * 0.126h;
                // Cool neutral silver-slate compensation neutralizes post-processing warm split-toning & sunlit haze.
                half3 coolNeutralTint = lerp(half3(0.91h, 0.96h, 1.07h), half3(0.86h, 0.95h, 1.12h), sunlit * 0.65h);
                half3 baseAlbedo = lerp(vertexLinear * 0.56h, lunarReflectance * coolNeutralTint, 0.90h);

                // Multi-scale macro albedo variation across 25 m – 1200 m.
                baseAlbedo *= SampleMacroRegolithVariation(input.positionWS, slopeFactor);

                half3 normalWS = geomNormalWS;
                half microAo = 1.0h;
                half smoothness = 0.04h;

                // Construct world-space tangent frame aligned with +X (U) and +Z (V) around the smooth terrain normal.
                half3 tangentWS = normalize(half3(1.0h, 0.0h, 0.0h) - geomNormalWS * geomNormalWS.x);
                half3 bitangentWS = normalize(cross(geomNormalWS, tangentWS));

                // Small crater decals & regolith pitting on slopes and highlands (survives out to 820 m).
                half4 slopeCraters = SampleSlopeCraterDecals(input.positionWS, slopeFactor, camDist);
                baseAlbedo *= slopeCraters.z;
                microAo = min(microAo, slopeCraters.w);

                // Slope geological structure: steeper slopes reveal darker rocky regolith, downslope talus chutes,
                // fractured rock ledges, and multi-scale triplanar rock normals surviving past 500 m+.
                UNITY_BRANCH
                if (slopeFactor > 0.005h && hillDetailWeight > 0.005h)
                {
                    float3 wp3 = input.positionWS;
                    half3 triWeights = abs(geomNormalWS);
                    triWeights = pow(max(triWeights, 0.001h), 4.0h);
                    triWeights /= (triWeights.x + triWeights.y + triWeights.z);

                    // 1. Downslope striations & talus chutes
                    float2 striaUV_X = float2(wp3.z * 0.026, wp3.y * 0.0062);
                    float2 striaUV_Z = float2(wp3.x * 0.026, wp3.y * 0.0062);
                    half striaSampleX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, striaUV_X).g;
                    half striaSampleZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, striaUV_Z).g;
                    half horizWeightSum = max(0.001h, triWeights.x + triWeights.z);
                    half striationRaw = (striaSampleX * triWeights.x + striaSampleZ * triWeights.z) / horizWeightSum;
                    half talusChute = saturate((striationRaw - 0.745h) * 6.0h + 0.5h);

                    // 2. Discontinuous slumped terrace ledges & fractured rock ribs
                    half slumpNoise1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp3.xz * 0.0042 + float2(0.17, 0.43)).r;
                    half slumpNoise2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp3.zx * 0.0068 + float2(0.61, 0.29)).g;
                    half slumpPocket = (half)smoothstep(0.725, 0.780, slumpNoise1 * 0.65h + slumpNoise2 * 0.35h);
                    float terraceCoord = wp3.y * 0.058 + (slumpNoise1 - 0.750) * 6.0 + (striationRaw - 0.750) * 2.2;
                    float terraceFrac = frac(terraceCoord);
                    half terraceRiser = slumpPocket * (half)(smoothstep(0.44, 0.72, terraceFrac) * (1.0 - smoothstep(0.72, 0.96, terraceFrac)));
                    half terraceBench = slumpPocket * (half)(smoothstep(0.05, 0.28, terraceFrac) * (1.0 - smoothstep(0.28, 0.44, terraceFrac)));

                    // 3. Steeper slopes reveal darker rocky regolith and exposed basalt outcrops
                    half outcropMask = saturate((steepOutcropFactor * 0.92h + slopeFactor * 0.38h + terraceRiser * 0.42h * slopeFactor + (1.0h - talusChute) * 0.34h * slopeFactor) * hillDetailWeight);

                    // Dual-scale triplanar rock sampling: fine (_RockTiling) + macro (_RockTiling * 0.26) so normal detail survives at 500 m+
                    float3 rockUVFine = wp3 * _RockTiling;
                    float3 rockUVMacro = wp3 * (_RockTiling * 0.26);

                    half3 rockAlbedoX = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUVFine.zy).rgb;
                    half3 rockAlbedoY = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUVFine.xz).rgb;
                    half3 rockAlbedoZ = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUVFine.xy).rgb;
                    half3 rockTexFine = rockAlbedoX * triWeights.x + rockAlbedoY * triWeights.y + rockAlbedoZ * triWeights.z;

                    half3 rockMacroX = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUVMacro.zy).rgb;
                    half3 rockMacroZ = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUVMacro.xy).rgb;
                    half3 rockTexMacro = (rockMacroX * triWeights.x + rockMacroZ * triWeights.z) / horizWeightSum;
                    half3 rockTex = lerp(rockTexMacro, rockTexFine, midDetailWeight * 0.72h + 0.28h);

                    half fineRockBumpScale = _RockBumpScale * 1.25h * (0.35h + 0.65h * midDetailWeight);
                    half macroRockBumpScale = _RockBumpScale * 1.35h;
                    half3 rockNormFineX = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUVFine.zy), fineRockBumpScale);
                    half3 rockNormFineZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUVFine.xy), fineRockBumpScale);
                    half3 rockNormMacroX = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUVMacro.zy), macroRockBumpScale);
                    half3 rockNormMacroZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUVMacro.xy), macroRockBumpScale);

                    half2 triFineXY = (rockNormFineX.xy * triWeights.x + rockNormFineZ.xy * triWeights.z) / horizWeightSum;
                    half2 triMacroXY = (rockNormMacroX.xy * triWeights.x + rockNormMacroZ.xy * triWeights.z) / horizWeightSum;
                    half2 triTsXY = (triFineXY * 0.65h + triMacroXY * 0.85h) * (0.48h * slopeFactor + 0.72h * outcropMask) * hillDetailWeight;

                    half terraceTilt = (terraceRiser * 0.24h - terraceBench * 0.15h) * slopeFactor * hillDetailWeight;
                    normalWS = normalize(
                        geomNormalWS +
                        half3(0.0h, -terraceTilt, 0.0h) +
                        tangentWS * triTsXY.x -
                        bitangentWS * triTsXY.y);

                    // Darker rocky regolith on steeper slopes (0.54x on sheer basalt outcrops, contrasted by lighter talus chutes)
                    half3 rockCentered = clamp(rockTex / max(0.08h, dot(rockTex, half3(0.333h, 0.333h, 0.333h))), 0.72h, 1.28h);
                    half slopeDarken = lerp(1.0h, 0.54h, smoothstep(0.08h, 0.88h, outcropMask));
                    slopeDarken *= lerp(0.84h, 1.14h, talusChute * slopeFactor * hillDetailWeight);
                    slopeDarken *= lerp(1.0h, 1.10h, terraceBench * slopeFactor * hillDetailWeight);

                    // Cool dark basalt tint on exposed rock so steep ridges read as dark lunar bedrock
                    half3 darkRockTint = lerp(half3(1.0h, 1.0h, 1.0h), half3(0.88h, 0.92h, 1.04h), outcropMask * 0.65h);
                    baseAlbedo *= slopeDarken * darkRockTint * lerp(1.0h.xxx, rockCentered, outcropMask * 0.58h);
                    microAo = min(microAo, lerp(1.0h, 0.76h, outcropMask));
                    smoothness = max(smoothness, outcropMask * 0.075h);
                }

                // Multi-scale regolith detail normal that survives at 500 m+ (out to 1550 m) plus slope crater normals.
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
                    regolithTex = lerp(regolithTex, regolithMid, midDetailWeight * 0.52h);

                    // Boosted macro & far normal scales so terrain detail normal remains crisp at 150 m and 500 m+
                    half macroBumpStrength = max(_MacroBumpScale, 0.48h) * macroWeight * (1.0h + slopeFactor * 0.45h);
                    half3 macroNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMacro), macroBumpStrength);
                    half3 farNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvFar), macroBumpStrength * 0.82h);
                    half3 midNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMid), 0.64h * midDetailWeight);
                    half2 distNormalXY = macroNormal.xy * 0.75h + farNormal.xy * 0.65h + midNormal.xy * 0.70h + slopeCraters.xy;

                    UNITY_BRANCH
                    if (nearWeight > 0.001h)
                    {
                        float2 uvNear1 = wp * _NearTiling;
                        float2 wpNearRot = float2(wp.x * 0.7986 - wp.y * 0.6018, wp.x * 0.6018 + wp.y * 0.7986);
                        float2 uvNear2 = wpNearRot * (_NearTiling * 0.47) + float2(0.19, 0.63);

                        half3 albedoNear1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvNear1).rgb;
                        half3 albedoNear2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvNear2).rgb;
                        half3 albedoNear = lerp(albedoNear1, albedoNear2, 0.44h);

                        half4 maskNear1 = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvNear1);
                        half4 maskNear2 = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvNear2);
                        half4 maskNear = lerp(maskNear1, maskNear2, 0.42h);

                        half bumpScale = (_NearBumpScale * 0.96h + _MacroBumpScale * 0.30h) * nearWeight * (1.0h + slopeFactor * 0.25h);
                        half3 tsNormal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvNear1), bumpScale);
                        half3 tsNormal2Raw = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvNear2), bumpScale * 0.78h);
                        half2 tsRot2 = half2(
                            tsNormal2Raw.x * 0.7986h + tsNormal2Raw.y * 0.6018h,
                            -tsNormal2Raw.x * 0.6018h + tsNormal2Raw.y * 0.7986h);
                        half2 combinedTsXY = tsNormal1.xy * 0.74h + tsRot2 * 0.58h + distNormalXY * 0.65h;

                        half4 microRelief = SampleNearMicroRelief(wp, camDist);
                        combinedTsXY += microRelief.xy * (nearWeight * (1.0h - slopeFactor * 0.55h));

                        half3 tsNormal = normalize(half3(combinedTsXY, 1.0h));
                        regolithTex = lerp(regolithTex, albedoNear, nearWeight * 0.80h);
                        regolithTex *= lerp(1.0h, microRelief.z, nearWeight);

                        normalWS = normalize(
                            tangentWS * tsNormal.x -
                            bitangentWS * tsNormal.y +
                            normalWS * tsNormal.z);

                        half texAo = saturate(maskNear.g / 0.870h) * microRelief.w;
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
                    half detailStrength = max(_DetailAlbedoStrength, 0.36h) * macroWeight * (0.55h + 0.45h * max(nearWeight, midDetailWeight));
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

                half3 ambient = SampleSH(normalWS) * ambientAo * half3(0.92h, 0.96h, 1.06h);
                half3 lighting = ambient + MainSunDiffuseHighland(mainLight, normalWS, geomNormalWS, viewDirWS, sunlit, microAo, slopeFactor);
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

                color = clamp(color, 0.0h, 32.0h);
                half3 preAtmo = color;
                color = MoonAtmosphere(color, input.positionWS, input.fogFactor);
                // Keep distant sunlit horizon haze cool-neutral on lunar terrain so golden hour doesn't turn regolith warm beige.
                half atmoLuma = dot(color, half3(0.2126h, 0.7152h, 0.0722h));
                color = lerp(color, atmoLuma * half3(0.93h, 0.97h, 1.06h), sunlit * saturate((camDist - 120.0) / 450.0) * 0.65h);
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
