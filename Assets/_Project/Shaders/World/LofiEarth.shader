// Stylized-real painterly Lunar Earth: rendered on a camera-facing quad by intersecting the view ray with a unit sphere in
// Earth's tilted frame. Gives a perfectly round anti-aliased silhouette at any size, exact per-pixel sphere normals,
// equirectangular continents and oceans with stepped bathymetry shelves, coral/turquoise reefs, color-blocked painterly
// biomes, and polar ice caps, a separate slowly drifting swirling cloud layer casting drop shadows, ocean specular sun
// glint, a dark unpeopled night hemisphere (no city lights — humanity left in the 80s) with a faint slow polar aurora,
// a thin blue Fresnel atmospheric limb, and a smooth red-orange refracted ring during eclipse.
Shader "MoonProject/World/LofiEarth"
{
    Properties
    {
        [MainTexture] _EarthAlbedoMap("Earth Albedo (RGB) Elevation/Mask (A)", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-200"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Any distance inside the camera's clip range works: depth is overridden below.
            #define EARTH_DISTANCE 100.0
            #define FAR_PLANE_INSET 1e-5
            #define INV_PI 0.31830988618
            #define INV_TWO_PI 0.15915494309

            TEXTURE2D(_EarthAlbedoMap);
            SAMPLER(sampler_EarthAlbedoMap);

            float4 _MoonEarthDirection;  // xyz unit direction to Earth's centre, w angular radius (radians)
            float4 _MoonEarthAxisX;      // xyz Earth frame X in world space, w tan(angular radius)
            float4 _MoonEarthAxisY;      // xyz Earth frame Y (spin axis)
            float4 _MoonEarthAxisZ;      // xyz Earth frame Z (-toEarth, toward viewer)
            float4 _MoonEarthSun;        // xyz world direction toward the sun lighting Earth, w night brightness
            float4 _MoonEarthGlow;       // x lit-side emissive boost, y spin speed (rad/s), z spin angle, w eclipse rim blend
            float4 _MoonEarthAtmosphere; // rgb rim colour (linear), a fresnel exponent
            float4 _MoonEarthOcean;      // rgb palette ocean colour (linear)
            float4 _MoonEarthLand;       // rgb palette land colour (linear)
            float4 _MoonEarthCloud;      // rgb palette cloud/ice colour (linear)
            float4 _MoonEarthAurora;     // rgb night-side polar aurora colour * intensity (linear), a drift speed (rad/s)
            float4 _MoonEarthAuroraBand; // x aurora latitude (abs y), y band half-width, z drift angle
            float4 _MoonEclipseRing;     // rgb refracted eclipse ring colour (linear), a ring width

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 discUV : TEXCOORD0;
            };

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float n000 = Hash13(i);
                float n100 = Hash13(i + float3(1.0, 0.0, 0.0));
                float n010 = Hash13(i + float3(0.0, 1.0, 0.0));
                float n110 = Hash13(i + float3(1.0, 1.0, 0.0));
                float n001 = Hash13(i + float3(0.0, 0.0, 1.0));
                float n101 = Hash13(i + float3(1.0, 0.0, 1.0));
                float n011 = Hash13(i + float3(0.0, 1.0, 1.0));
                float n111 = Hash13(i + float3(1.0, 1.0, 1.0));
                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            float Fbm3(float3 p)
            {
                float value = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += amp * ValueNoise3D(p);
                    p = p * 2.07 + float3(13.7, 7.3, 19.1);
                    amp *= 0.5;
                }
                return value;
            }

            float3 Spin(float3 v, float s, float c)
            {
                return float3(c * v.x + s * v.z, v.y, -s * v.x + c * v.z);
            }

            float3 ToWorld(float3 v)
            {
                return v.x * _MoonEarthAxisX.xyz + v.y * _MoonEarthAxisY.xyz + v.z * _MoonEarthAxisZ.xyz;
            }

            Varyings Vert(Attributes input)
            {
                float3 planeOffset = (input.positionOS.x * _MoonEarthAxisX.xyz
                    + input.positionOS.y * _MoonEarthAxisY.xyz) * _MoonEarthAxisX.w;
                float3 offset = (_MoonEarthDirection.xyz + planeOffset) * EARTH_DISTANCE;

                Varyings output;
                output.positionCS = TransformWorldToHClip(_WorldSpaceCameraPos + offset);
                #if UNITY_REVERSED_Z
                output.positionCS.z = output.positionCS.w * FAR_PLANE_INSET;
                #else
                output.positionCS.z = output.positionCS.w * (1.0 - FAR_PLANE_INSET);
                #endif
                output.discUV = input.positionOS.xy;
                return output;
            }

            float3 ShadeAuroraAndEclipseRim(
                float3 color,
                float2 disc,
                float3 normalLocal,
                float viewFacing,
                float lat,
                float ndotl,
                float spinAngle,
                float3 sunLocal,
                float3 normalRim,
                float rim)
            {
                // Faint, slow natural polar aurora curtains near the poles on Earth's dark night hemisphere.
                float nightMask = smoothstep(0.02, -0.06, ndotl);
                float auroraPhase = abs(_MoonEarthAuroraBand.z) > 1e-5
                    ? _MoonEarthAuroraBand.z
                    : _Time.y * _MoonEarthAurora.a;
                float as, ac;
                sincos(spinAngle * 0.35 + auroraPhase, as, ac);
                float3 ap = Spin(normalLocal, as, ac);
                float ribbonWave = (Fbm3(ap * 2.4 + float3(2.1, 7.4, 5.3)) - 0.5) * 0.045;
                float auroraLat = lat + viewFacing * 0.06 + ribbonWave;
                float bandDelta = (auroraLat - _MoonEarthAuroraBand.x) / max(_MoonEarthAuroraBand.y, 1e-3);
                float lowerEdge = smoothstep(-1.1, -0.1, bandDelta);
                float upperFade = exp(-max(bandDelta, 0.0) * max(bandDelta, 0.0) * 1.6);
                float auroraOval = lowerEdge * upperFade * smoothstep(0.68, 0.78, lat);
                float3 curtainCoord = float3(ap.x * 13.0, normalLocal.y * 1.4, ap.z * 13.0);
                float curtainRays = Fbm3(curtainCoord + float3(auroraPhase * 0.8, 0.0, -auroraPhase * 0.6));
                float longitudinalArc = smoothstep(0.32, 0.68, Fbm3(ap * 2.2 + float3(5.1, auroraPhase * 0.25, 8.7)));
                float curtains = (0.20 + 0.80 * smoothstep(0.38, 0.74, curtainRays)) * longitudinalArc;
                float limbFade = smoothstep(0.05, 0.26, viewFacing);
                float auroraMask = auroraOval * curtains * nightMask * limbFade;
                float3 auroraTint = lerp(_MoonEarthAurora.rgb, _MoonEarthAurora.rgb * float3(0.76, 1.0, 1.16),
                    saturate(bandDelta * 0.5 + 0.5));
                color += auroraTint * auroraMask;

                float2 sun2D = normalize(sunLocal.xy + 1e-6);
                float sunward = dot(normalize(disc + 1e-6), sun2D);
                float3 eclipseRim = _MoonEclipseRing.rgb * (0.45 + 0.45 * saturate(0.5 + 0.5 * sunward));
                color += lerp(normalRim, eclipseRim, _MoonEarthGlow.w) * rim;
                return color;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.discUV;
                float r2 = dot(uv, uv);
                float r = sqrt(r2);
                float edgeWidth = max(fwidth(r), 1e-4);
                float alpha = 1.0 - smoothstep(1.0 - edgeWidth * 0.85, 1.0 + edgeWidth * 0.85, r);
                if (alpha <= 1e-4)
                {
                    discard;
                }

                float2 disc = uv / max(r, 1.0);
                float z = sqrt(max(0.0, 1.0 - dot(disc, disc)));
                float3 normalLocal = float3(disc.x, disc.y, z);
                float3 normalWS = normalize(ToWorld(normalLocal));
                float spinAngle = abs(_MoonEarthGlow.z) > 1e-5 ? _MoonEarthGlow.z : _Time.y * _MoonEarthGlow.y;

                // Spin phase aligned so at opening earthlit night (t = 4816s) and static edit-mode preview,
                // the sunlit crescent frames the Americas, Atlantic Ocean, Europe, Mediterranean, and Africa.
                float globeAngle = abs(_MoonEarthGlow.z) > 1e-5 ? (_MoonEarthGlow.z - 2.68) : (_Time.y * _MoonEarthGlow.y);
                float gs, gc;
                sincos(globeAngle, gs, gc);
                float3 gp = Spin(normalLocal, gs, gc);

                float lon = atan2(gp.x, gp.z);
                float latSin = clamp(gp.y, -1.0, 1.0);
                float lat = abs(latSin);
                float2 equirectUV = float2(
                    frac(lon * INV_TWO_PI + 0.44),
                    clamp(0.5 + asin(latSin) * INV_PI, 0.001, 0.999));

                // Seam-free mip LOD from continuous unit-sphere derivatives (avoids atan2/frac wrap mip spike).
                float3 dpdx = ddx(gp);
                float3 dpdy = ddy(gp);
                float texelFootprint = max(length(dpdx), length(dpdy)) * 320.0;
                float mipLod = clamp(log2(max(texelFootprint, 1.0)), 0.0, 6.0);
                float4 earthTex = SAMPLE_TEXTURE2D_LOD(_EarthAlbedoMap, sampler_EarthAlbedoMap, equirectUV, mipLod);

                float3 sunLocal = float3(
                    dot(_MoonEarthSun.xyz, _MoonEarthAxisX.xyz),
                    dot(_MoonEarthSun.xyz, _MoonEarthAxisY.xyz),
                    dot(_MoonEarthSun.xyz, _MoonEarthAxisZ.xyz));

                // Fine 3D spherical micro-detail so coastlines, desert ergs, and mountain ranges stay crisp at any size.
                float microA = Fbm3(gp * 16.0 + float3(2.3, 7.1, 4.9)) - 0.5;
                float microB = Fbm3(gp * 38.0 + float3(9.4, 3.2, 11.8)) - 0.5;
                float painterStroke = Fbm3(gp * 9.5 + float3(microA * 1.4, microB * 1.4, -microA * 1.2)) - 0.5;

                float elevNorm;
                float isLand;
                float mountainHeight;
                float polarIce;
                float3 surfaceColor;

                if (earthTex.a > 0.5)
                {
                    elevNorm = saturate((earthTex.a - 0.88) / 0.12);
                    float coastEdge = 0.012;
                    isLand = smoothstep(0.495 - coastEdge, 0.495 + coastEdge, elevNorm + microA * 0.018);
                    mountainHeight = saturate((elevNorm - 0.54) / 0.32) * isLand;
                    polarIce = smoothstep(0.84, 0.90, elevNorm + painterStroke * 0.03);

                    // Stylized Subnautica-like painterly surface on real continents and oceans
                    float3 texAlbedo = earthTex.rgb;
                    float oceanDepth01 = saturate(elevNorm / 0.48);
                    float steppedShelf = smoothstep(0.46, 0.58, oceanDepth01 + painterStroke * 0.08) * 0.55
                        + smoothstep(0.70, 0.80, oceanDepth01 + painterStroke * 0.06) * 0.45;
                    float coastReef = smoothstep(0.39, 0.485, elevNorm + painterStroke * 0.025) * (1.0 - isLand);
                    float3 abyssOcean = float3(0.022, 0.082, 0.265);
                    float3 ceruleanOcean = float3(0.048, 0.235, 0.535);
                    float3 turquoiseShelf = float3(0.085, 0.545, 0.695);
                    float3 aquaReef = float3(0.210, 0.815, 0.845);
                    float3 oceanStylized = lerp(abyssOcean, ceruleanOcean, smoothstep(0.18, 0.62, oceanDepth01));
                    oceanStylized = lerp(oceanStylized, turquoiseShelf, steppedShelf * 0.82);
                    oceanStylized = lerp(oceanStylized, aquaReef, coastReef * 0.78);

                    // Painterly color-blocked biomes derived from real continent chrominance + elevation terraces
                    float greenDom = saturate((texAlbedo.g - texAlbedo.r * 0.78) * 4.2);
                    float aridDom = saturate((texAlbedo.r - texAlbedo.g * 0.92) * 3.8);
                    float aridTerrace = smoothstep(0.22, 0.36, aridDom + painterStroke * 0.12) * 0.52
                        + smoothstep(0.52, 0.66, aridDom + painterStroke * 0.10) * 0.48;
                    float3 kelpJungle = float3(0.055, 0.345, 0.185);
                    float3 lushSteppe = float3(0.185, 0.465, 0.225);
                    float3 terracotta = float3(0.715, 0.395, 0.215);
                    float3 goldenDune = float3(0.915, 0.665, 0.355);
                    float3 highlandPlum = float3(0.385, 0.315, 0.335);

                    float3 landStylized = lerp(lushSteppe, kelpJungle, smoothstep(0.25, 0.68, greenDom + painterStroke * 0.10));
                    float3 desertStylized = lerp(terracotta, goldenDune, smoothstep(-0.08, 0.12, painterStroke + microA * 0.5));
                    landStylized = lerp(landStylized, desertStylized, aridTerrace);
                    float mtnTerrace = smoothstep(0.16, 0.28, mountainHeight) * 0.55 + smoothstep(0.42, 0.58, mountainHeight) * 0.45;
                    landStylized = lerp(landStylized, highlandPlum, mtnTerrace * 0.80);
                    float3 snowStylized = float3(0.92, 0.97, 1.02);
                    surfaceColor = lerp(oceanStylized, landStylized * (0.96 + 0.12 * smoothstep(-0.15, 0.15, painterStroke)), isLand);
                    surfaceColor = lerp(surfaceColor, snowStylized, polarIce * 0.94);
                }
                else
                {
                    // Fallback if material texture is unbound in an isolated unit test
                    elevNorm = Fbm3(gp * 2.1 + float3(0.4, -0.3, 0.8));
                    isLand = smoothstep(0.48, 0.52, elevNorm);
                    mountainHeight = saturate((elevNorm - 0.54) * 2.5) * isLand;
                    polarIce = smoothstep(0.78, 0.88, lat);
                    surfaceColor = lerp(_MoonEarthOcean.rgb * 0.55, _MoonEarthLand.rgb, isLand);
                    surfaceColor = lerp(surfaceColor, _MoonEarthCloud.rgb, polarIce);
                }

                // Topographic normal relief on continents so mountain ranges catch grazing low sun near the terminator.
                float3 landGrad = float3(ddx(mountainHeight + microA * 0.25), ddy(mountainHeight + microA * 0.25), 0.0);
                float3 topoNormalLocal = normalize(normalLocal + float3(-landGrad.x, -landGrad.y, 0.0) * (isLand * 1.8));
                float3 topoNormalWS = normalize(ToWorld(topoNormalLocal));

                // Separate slowly drifting cloud layer with Coriolis cyclone vortices and trade-wind bands.
                float cs, cc;
                sincos(spinAngle * 1.34 + 0.65, cs, cc);
                float2 cloudDisc = disc * 0.985;
                float3 cloudLocal = float3(cloudDisc, sqrt(max(0.0, 1.0 - dot(cloudDisc, cloudDisc))));
                float3 cp = Spin(cloudLocal, cs, cc);

                float hemiSign = cp.y >= 0.0 ? 1.0 : -1.0;
                float swirlA = Fbm3(cp * 2.15 + float3(13.1, 4.7, 8.2)) - 0.5;
                float swirlB = Fbm3(cp * 2.15 + float3(2.9, 17.5, 6.4)) - 0.5;
                float3 cycloneWarp = float3(swirlA - swirlB * 0.35 * hemiSign, swirlB * 0.45, swirlB + swirlA * 0.35 * hemiSign);
                float3 cloudCoord = cp * 2.75 + cycloneWarp * 1.22;

                float cloudMacro = Fbm3(cloudCoord);
                float cloudDetail = Fbm3(cloudCoord * 2.55 + float3(7.7, 3.1, 11.4));
                float tradeBelts = 0.065 * cos(cp.y * 8.8) + 0.035 * cos(cp.y * 17.0);

                // Sculpted painterly swirling cloud ribbons with crisp two-tier cel edges.
                float ribbonDensity = cloudMacro * 0.68 + cloudDetail * 0.24 + tradeBelts * 1.35;
                float cloudRibbonOuter = smoothstep(0.51, 0.565, ribbonDensity);
                float cloudRibbonInner = smoothstep(0.60, 0.655, ribbonDensity);
                float cloudMask = saturate(cloudRibbonOuter * 0.58 + cloudRibbonInner * 0.42);
                float cloudCore = cloudRibbonInner;

                // Cloud drop shadow offset away from the Sun so the cloud deck visibly floats above ocean and land.
                float3 shadowProbe = Spin(normalize(normalLocal + sunLocal * 0.038), cs, cc);
                float3 shadowCoord = shadowProbe * 2.75 + cycloneWarp * 1.22;
                float shadowDensity = Fbm3(shadowCoord) * 0.58 + Fbm3(shadowCoord * 2.55 + float3(7.7, 3.1, 11.4)) * 0.42 + tradeBelts;
                float shadowStep = smoothstep(0.51, 0.58, shadowDensity);
                float cloudShadow = shadowStep * (1.0 - cloudMask * 0.72);
                surfaceColor *= 1.0 - cloudShadow * 0.50;

                // 3D cloud-top self-shading along the sun vector.
                float cloudTopBump = (shadowDensity - ribbonDensity) * 2.4;
                float cloudShade01 = saturate(0.58 + cloudCore * 0.35 - cloudTopBump * 0.28);
                float3 cloudTint = lerp(_MoonEarthCloud.rgb * float3(0.68, 0.84, 1.02), _MoonEarthCloud.rgb * float3(1.12, 1.08, 1.00),
                    smoothstep(0.38, 0.68, cloudShade01));
                float3 albedo = lerp(surfaceColor, cloudTint, cloudMask * 0.96);

                // Painterly soft-stepped cel bands across the sunlit crescent with Lommel-Seeliger phase lighting.
                float ndotl = dot(normalWS, _MoonEarthSun.xyz);
                float topoNdotL = dot(topoNormalWS, _MoonEarthSun.xyz);
                float viewFacing = saturate(z);
                float posNdotL = max(lerp(ndotl, topoNdotL, isLand * (1.0 - cloudMask) * 0.55), 0.0);
                float seeliger = posNdotL / (posNdotL + viewFacing * 0.30 + 0.032);
                float terminator = smoothstep(-0.065, 0.12, ndotl);
                float celBand1 = smoothstep(0.00, 0.14, posNdotL);
                float celBand2 = smoothstep(0.24, 0.48, posNdotL);
                float lit = terminator * lerp(seeliger, 0.52 * celBand1 + 0.48 * celBand2, 0.58);

                // Subtle Rayleigh twilight scattering band across the terminator (warm coral-amber in clouds, cobalt/cyan in upper air).
                float twilightBand = smoothstep(-0.09, 0.02, ndotl) * (1.0 - smoothstep(0.01, 0.22, ndotl));
                float3 twilightColor = lerp(float3(0.12, 0.58, 0.96), float3(0.96, 0.48, 0.26), cloudMask * 0.65);
                float3 twilightScatter = twilightColor * (twilightBand * 0.22);

                // Ocean specular glint on open water clear of land, ice, and dense clouds.
                float3 viewDir = _MoonEarthAxisZ.xyz;
                float3 halfVec = normalize(_MoonEarthSun.xyz + viewDir);
                float3 wavePerturb = float3(microA, microB, (swirlA - 0.5) * 0.5);
                float3 waveNormal = normalize(normalWS + ToWorld(wavePerturb) * 0.055);
                float specCos = max(dot(waveNormal, halfVec), 1e-4);
                float openWater = (1.0 - isLand) * (1.0 - polarIce) * (1.0 - cloudMask * 0.88) * smoothstep(-0.02, 0.18, ndotl);
                float oceanGlint = (smoothstep(0.72, 0.90, specCos) * 0.18 + smoothstep(0.91, 0.985, specCos) * 0.42) * openWater;
                float3 glintColor = lerp(
                    lerp(_MoonEarthCloud.rgb, float3(1.0, 0.96, 0.88), 0.52),
                    float3(0.72, 0.98, 1.00),
                    0.55);

                // Dark unpeopled night side (no city lights — humanity left in the 80s).
                float3 color = (albedo * lerp(_MoonEarthSun.w, 1.0, lit) + twilightScatter) * _MoonEarthGlow.x;
                color += glintColor * (oceanGlint * _MoonEarthGlow.x);

                // Thin blue Fresnel atmospheric limb hugging Earth's edge + soft inner Rayleigh atmospheric veil.
                float oneMinusV = max(1.0 - viewFacing, 1e-4);
                float thinFresnel = lerp(pow(oneMinusV, 3.6), smoothstep(0.42, 0.92, oneMinusV) * 1.15, 0.65);
                float innerRayleigh = pow(oneMinusV, 1.85) * 0.32;
                float rim = thinFresnel + innerRayleigh;
                float3 blueAtmo = lerp(_MoonEarthAtmosphere.rgb, float3(0.18, 0.78, 1.05), 0.64);
                float limbLit = 0.07 + 0.93 * smoothstep(-0.14, 0.22, ndotl);
                float3 normalRim = blueAtmo * limbLit;

                color = ShadeAuroraAndEclipseRim(color, disc, normalLocal, viewFacing, lat, ndotl, spinAngle,
                    sunLocal, normalRim, rim);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
