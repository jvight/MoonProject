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

            // The lander's blast-swept zone (World's BlastZoneShaderGlobals): xy = centre (world x, z), z = radius,
            // w = edge band (m). Look: x = albedo lift, y = ray lift, z = ray count, w = ray length (m).
            float4 _MoonBlastZone;
            float4 _MoonBlastLook;

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

            // Diffuse response for the main directional light (Earthshine vs DirectSun),
            // incorporating lunar Lommel-Seeliger slope irradiance balance and micro-cavity self-shadowing.
            half3 MainSunDiffuse(Light light, half3 normalWS, half3 viewDirWS, half sunlit, half microAo, half slopeFactor)
            {
                half ndotl = dot(normalWS, light.direction);
                half wrapped = saturate(ndotl * 0.85h + 0.15h);
                half crisp = saturate(ndotl);

                // Lunar regolith backscatter (Lommel-Seeliger reflectance) keeps sun-facing hills and crater walls
                // from blowing out to 3x the brightness of the flat mare floor like smooth snow dunes.
                half ndotv = max(0.20h, saturate(dot(normalWS, viewDirWS)));
                half lommel = crisp / max(0.32h, crisp + ndotv * 0.54h);
                half lunarCrisp = lerp(crisp, lommel * 0.96h, slopeFactor * 0.36h);

                half diffuse = lerp(wrapped, lunarCrisp, sunlit);

                // Micro-cavity AO self-shadows pebbles, vesicle pits, and micro-craters under raking light.
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

            float BlastRayHash(float around, float along)
            {
                return frac(sin(dot(float2(around, along), float2(12.9898, 78.233))) * 43758.5453);
            }

            // Value noise round the circle (period = ray count, so the rays close without a seam) and along each ray.
            float BlastRayNoise(float around, float along, float rays)
            {
                float2 cell = floor(float2(around, along));
                float2 t = frac(float2(around, along));
                t = t * t * (3.0 - 2.0 * t);
                float a0 = fmod(cell.x + rays, rays);
                float a1 = fmod(cell.x + 1.0 + rays, rays);
                float near = lerp(BlastRayHash(a0, cell.y), BlastRayHash(a1, cell.y), t.x);
                float far = lerp(BlastRayHash(a0, cell.y + 1.0), BlastRayHash(a1, cell.y + 1.0), t.x);
                return lerp(near, far, t.y);
            }

            // The lander's descent engine swept the regolith outward: lighter dust in long rays, fingering out at the
            // zone's edge (the Apollo sites' halo). 1 = untouched.
            half BlastSweepTone(float3 positionWS)
            {
                float2 fromLander = positionWS.xz - _MoonBlastZone.xy;
                float radius = length(fromLander);
                if (_MoonBlastZone.z <= 0.001 || radius > _MoonBlastZone.z + _MoonBlastZone.w)
                {
                    return 1.0h;
                }

                float rays = max(1.0, _MoonBlastLook.z);
                float around = (atan2(fromLander.y, fromLander.x) * 0.15915494 + 0.5) * rays;
                float along = radius / max(0.1, _MoonBlastLook.w);
                float ray = smoothstep(0.2, 0.8, BlastRayNoise(around, along, rays) * 0.65
                    + BlastRayNoise(around * 2.0, along * 0.5 + 17.0, rays * 2.0) * 0.35);
                float edge = _MoonBlastZone.z - _MoonBlastZone.w;
                half swept = 1.0h - (half)smoothstep(edge, _MoonBlastZone.z, radius - (ray - 0.5) * _MoonBlastZone.w);
                return 1.0h + swept * (half)(_MoonBlastLook.x + _MoonBlastLook.y * (ray * 2.0 - 1.0));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half3 geomNormalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float camDist = distance(_WorldSpaceCameraPos, input.positionWS);

                // Fade high-frequency regolith grain out smoothly across medium range so distant hills stay calm.
                half nearWeight = 1.0h - (half)smoothstep(_NearFadeStart, max(_NearFadeStart + 1.0, _NearFadeEnd), camDist);
                half macroWeight = 1.0h - (half)smoothstep(180.0, 420.0, camDist);
                half hillDetailWeight = 1.0h - (half)smoothstep(260.0, 950.0, camDist);

                // Slope factor begins on moderate crater walls and hill flanks (~22-35 deg) so slopes get lunar character.
                half slopeFactor = 1.0h - (half)smoothstep(_SlopeThreshold - _SlopeBlendRange, _SlopeThreshold + _SlopeBlendRange, geomNormalWS.y);
                half steepOutcropFactor = 1.0h - (half)smoothstep(0.70h, 0.86h, geomNormalWS.y);

                // Vertex colors come from Mesh.colors32 (sRGB calibrated palette from TerrainPainter).
                half3 vertexLinear = SRGBToLinear(input.color.rgb) * _BaseColor.rgb;
                half3 normalWS = geomNormalWS;
                half3 baseAlbedo = vertexLinear;
                half microAo = 1.0h;
                half smoothness = 0.035h;

                // Construct world-space tangent frame aligned with +X (U) and +Z (V) around the smooth terrain normal.
                half3 tangentWS = normalize(half3(1.0h, 0.0h, 0.0h) - geomNormalWS * geomNormalWS.x);
                half3 bitangentWS = normalize(cross(geomNormalWS, tangentWS));

                // Slope geological structure: calm downslope striations, discontinuous slumped terrace pockets, and triplanar dark rock outcrops.
                // Parameterized strictly from continuous world position wp3 (never geomNormalWS * wp3) so curved mountain
                // ridges never exhibit wood-grain or moire rings.
                UNITY_BRANCH
                if (slopeFactor > 0.01h && hillDetailWeight > 0.01h)
                {
                    float3 wp3 = input.positionWS;
                    half3 triWeights = abs(geomNormalWS);
                    triWeights = pow(max(triWeights, 0.001h), 4.0h);
                    triWeights /= (triWeights.x + triWeights.y + triWeights.z);

                    // 1. Calm downslope striations: sample seamless regolith texture with 4:1 vertical stretch in world space
                    // so talus chutes and downslope rills streak naturally down every slope without periodic sine waves.
                    float2 striaUV_X = float2(wp3.z * 0.024, wp3.y * 0.0055);
                    float2 striaUV_Z = float2(wp3.x * 0.024, wp3.y * 0.0055);
                    half striaSampleX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, striaUV_X).g;
                    half striaSampleZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, striaUV_Z).g;
                    half horizWeightSum = max(0.001h, triWeights.x + triWeights.z);
                    half striationRaw = (striaSampleX * triWeights.x + striaSampleZ * triWeights.z) / horizWeightSum;
                    half talusChute = saturate((striationRaw - 0.750h) * 5.2h + 0.5h);

                    // 2. Discontinuous slumped terrace ledges: broken lateral pockets warped strongly by local geology
                    // so they form lens-shaped lunar slump benches rather than continuous horizontal stripes across a wall.
                    half slumpNoise1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp3.xz * 0.0038 + float2(0.17, 0.43)).r;
                    half slumpNoise2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp3.zx * 0.0062 + float2(0.61, 0.29)).g;
                    half slumpPocket = (half)smoothstep(0.735, 0.785, slumpNoise1 * 0.65h + slumpNoise2 * 0.35h);
                    float terraceCoord = wp3.y * 0.052 + (slumpNoise1 - 0.750) * 6.5 + (striationRaw - 0.750) * 2.4;
                    float terraceFrac = frac(terraceCoord);
                    half terraceRiser = slumpPocket * (half)(smoothstep(0.48, 0.74, terraceFrac) * (1.0 - smoothstep(0.74, 0.96, terraceFrac)));
                    half terraceBench = slumpPocket * (half)(smoothstep(0.06, 0.28, terraceFrac) * (1.0 - smoothstep(0.28, 0.48, terraceFrac)));

                    // 3. Exposed dark basalt rock outcrops on the steepest parts, rocky spurs, and slump risers (triplanar).
                    half outcropMask = saturate((steepOutcropFactor * 0.82h + terraceRiser * 0.34h * slopeFactor + (1.0h - talusChute) * 0.28h * slopeFactor) * hillDetailWeight);

                    float3 rockUV = wp3 * _RockTiling;
                    half3 rockAlbedoX = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV.zy).rgb;
                    half3 rockAlbedoY = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV.xz).rgb;
                    half3 rockAlbedoZ = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV.xy).rgb;
                    half3 rockTex = rockAlbedoX * triWeights.x + rockAlbedoY * triWeights.y + rockAlbedoZ * triWeights.z;

                    half3 rockNormX = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUV.zy), _RockBumpScale);
                    half3 rockNormZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, rockUV.xy), _RockBumpScale);
                    half2 triTsXY = (rockNormX.xy * triWeights.x + rockNormZ.xy * triWeights.z) * (0.38h * slopeFactor + 0.62h * outcropMask) * hillDetailWeight;

                    // Gentle normal tilt on slumped terrace risers/benches + triplanar rock normal relief.
                    half terraceTilt = (terraceRiser * 0.14h - terraceBench * 0.10h) * slopeFactor * hillDetailWeight;
                    normalWS = normalize(
                        geomNormalWS +
                        half3(0.0h, -terraceTilt, 0.0h) +
                        tangentWS * triTsXY.x -
                        bitangentWS * triTsXY.y);

                    // Calm value contrast: darker exposed rock outcrops on steepest parts, downslope talus streaks,
                    // and slightly lighter dust-mantled slump benches.
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
                        // Dual-scale rotated near-ground sampling eliminates periodic grid chatter across 2-15 m.
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
                        // Rotate tsNormal2 back from the 37-degree UV frame into world XZ tangent frame.
                        half2 tsRot2 = half2(
                            tsNormal2Raw.x * 0.7986h + tsNormal2Raw.y * 0.6018h,
                            -tsNormal2Raw.x * 0.6018h + tsNormal2Raw.y * 0.7986h);
                        half2 combinedTsXY = tsNormal1.xy * 0.74h + tsRot2 * 0.58h + macroNormal.xy * 0.22h;

                        // Procedural non-repeating near-field micro-craters, dual-scale grit clasts, and soft 3D tyre ruts.
                        half4 microRelief = SampleNearMicroRelief(wp, camDist);
                        combinedTsXY += microRelief.xy * (nearWeight * (1.0h - slopeFactor * 0.65h));

                        half3 tsNormal = normalize(half3(combinedTsXY, 1.0h));
                        regolithTex = lerp(regolithTex, albedoNear, nearWeight * 0.80h);
                        regolithTex *= lerp(1.0h, microRelief.z, nearWeight);

                        normalWS = normalize(
                            tangentWS * tsNormal.x -
                            bitangentWS * tsNormal.y +
                            normalWS * tsNormal.z);

                        // Mask.g is micro-cavity AO (~0.87 mean); normalize around 0.87 so average ground brightness stays true.
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

                    // Normalize regolith albedo around its mean (~0.525 linear) so painted vertex brightness is preserved.
                    half3 regolithCentered = regolithTex / 0.525h;
                    half detailStrength = _DetailAlbedoStrength * macroWeight * (0.35h + 0.65h * nearWeight);
                    baseAlbedo *= lerp(1.0h.xxx, regolithCentered, detailStrength);
                }

                baseAlbedo *= BlastSweepTone(input.positionWS);

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
