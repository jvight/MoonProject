// Lofi Lunar night sky (skybox): zenith-to-horizon gradient whose horizon is the fog colour, a soft violet
// horizon glow with a thin band of light along the horizon line, a hash-based starfield whose brightnesses follow a
// power law (faint dust, a handful of bright stars with a soft glow, a few slow twinklers), a faint milky-way band,
// a soft halo and a thin atmospheric limb around Earth, and an occasional slow shooting star. Every parameter is a
// _Moon* global set from WorldSettings by MoonProject.World.SkyShaderGlobals, so this shader has no material
// properties of its own.
Shader "MoonProject/World/LofiSky"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _MoonSkyTop;          // rgb zenith colour (linear), a gradient exponent
            float4 _MoonHorizonGlow;     // rgb glow (linear), a glow height
            float4 _MoonHorizonBand;     // rgb thin band along the horizon (linear), a band height
            float4 _MoonStarParams;      // x density, y brightness, z power-law exponent, w core size in pixels
            float4 _MoonStarTwinkle;     // x share that twinkles, y speed (rad/s), z depth, w glow of bright stars
            float4 _MoonStarShape;       // x size spread (power of brightness), y brightest cap, z glow from
            float4 _MoonMilkyWayAxis;    // xyz band plane normal, w intensity
            float4 _MoonMilkyWayColor;   // rgb haze colour (linear), a angular half-width
            float4 _MoonEarthDirection;  // xyz unit direction to Earth, w angular radius (radians)
            float4 _MoonEarthHalo;       // rgb halo colour (linear), a halo size as a fraction of the radius
            float4 _MoonEarthLimb;       // rgb limb colour (linear), a limb width as a fraction of the radius
            float4 _MoonEarthSun;        // xyz world direction toward the sun lighting Earth
            float4 _MoonShootingStars;   // x period, y duration, z brightness
            float4 _MoonSunDirection;    // xyz unit direction to the Sun, w angular radius (radians)
            float4 _MoonSunColor;        // rgb hot core colour (linear), a soft corona strength (linear)
            float4 _MoonEclipseRing;     // rgb refracted eclipse ring colour (linear), a ring width fraction of radius

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            // Hash without sine (Dave Hoskins): stable across GPUs, no precision cliffs at these magnitudes.
            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float3 Hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = lerp(Hash13(i), Hash13(i + float3(1, 0, 0)), f.x);
                float b = lerp(Hash13(i + float3(0, 1, 0)), Hash13(i + float3(1, 1, 0)), f.x);
                float c = lerp(Hash13(i + float3(0, 0, 1)), Hash13(i + float3(1, 0, 1)), f.x);
                float d = lerp(Hash13(i + float3(0, 1, 1)), Hash13(i + float3(1, 1, 1)), f.x);
                return lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
            }

            // Projects a 3D unit direction onto an equal-angle 2D cubemap face (face index 0..5, uv in 0..1) and
            // reconstructs the 3D unit direction from (face, uv). Unlike a 3D cubic voxel grid sliced by the unit
            // sphere, every 2D face cell is a well-proportioned square patch on the sky, so stars and their glows are
            // never clipped into tiny triangles, squares or diamonds.
            void DirectionToCubeFace(float3 dir, out float face, out float2 uv)
            {
                float3 a = abs(dir);
                float ma;
                float2 sc;
                if (a.x >= a.y && a.x >= a.z)
                {
                    ma = a.x;
                    sc = float2(dir.x >= 0.0 ? -dir.z : dir.z, dir.y);
                    face = dir.x >= 0.0 ? 0.0 : 1.0;
                }
                else if (a.y >= a.z)
                {
                    ma = a.y;
                    sc = float2(dir.x, dir.y >= 0.0 ? -dir.z : dir.z);
                    face = dir.y >= 0.0 ? 2.0 : 3.0;
                }
                else
                {
                    ma = a.z;
                    sc = float2(dir.z >= 0.0 ? dir.x : -dir.x, dir.y);
                    face = dir.z >= 0.0 ? 4.0 : 5.0;
                }

                uv = atan(sc / max(ma, 1e-6)) * (2.0 / 3.14159265) + 0.5;
            }

            float3 CubeFaceToDirection(float face, float2 uv)
            {
                float2 t = tan((uv - 0.5) * (3.14159265 * 0.5));
                float3 v;
                if (face < 0.5)
                {
                    v = float3(1.0, t.y, -t.x);
                }
                else if (face < 1.5)
                {
                    v = float3(-1.0, t.y, t.x);
                }
                else if (face < 2.5)
                {
                    v = float3(t.x, 1.0, -t.y);
                }
                else if (face < 3.5)
                {
                    v = float3(t.x, -1.0, t.y);
                }
                else if (face < 4.5)
                {
                    v = float3(t.x, t.y, 1.0);
                }
                else
                {
                    v = float3(-t.x, t.y, -1.0);
                }

                return normalize(v);
            }

            // One layer of stars on the equal-angle cubemap grid: at most one star per 2D face cell, kept well inside
            // [0.32, 0.68]^2 of its cell, with true 3D angular distance in screen pixels so every star is a soft round
            // point with a crisp circular core and never clips against cell boundaries.
            float3 StarLayer(float3 dir, float cells, float density, float pixel, float faintest, float maxBright, float seed)
            {
                float face;
                float2 uv;
                DirectionToCubeFace(dir, face, uv);

                float faceCells = max(floor(cells * 0.75), 4.0);
                float2 grid = uv * faceCells;
                float2 cell = floor(grid);
                float2 local = grid - cell;

                float3 cellId = float3(cell, face * 17.0 + seed);
                float3 h = Hash33(cellId);
                if (h.x > density)
                {
                    return 0;
                }

                float2 starLocal = 0.32 + 0.36 * Hash33(cellId + 19.19).xy;
                float3 starDir = CubeFaceToDirection(face, (cell + starLocal) / faceCells);

                float brightness = min(faintest * pow(max(h.y, 1e-3), -1.0 / _MoonStarParams.z), min(_MoonStarShape.y, maxBright));
                float distancePx = length(dir - starDir) / pixel;

                // Minimum distance in screen pixels from the star center to the nearest cell edge.
                float starMargin = min(min(starLocal.x, 1.0 - starLocal.x), min(starLocal.y, 1.0 - starLocal.y));
                float maxCellRadiusPx = max((0.785398 / faceCells) * starMargin / pixel - 0.85, 0.38);

                // Sizes are relative to a faint star of brightness 0.1, the sky's typical dust.
                float radius = min(_MoonStarParams.w * 1.42 * pow(max(brightness * 10.0, 1e-4), _MoonStarShape.x), maxCellRadiusPx);
                // A star smaller than a pixel lights one pixel in proportion to its area: faint stars stay fine dust.
                float coverage = saturate(radius * 2.0);
                float coreRadius = clamp(radius, 0.52, maxCellRadiusPx);
                float core = smoothstep(coreRadius + 0.62, max(coreRadius - 0.35, 0.0), distancePx)
                    * coverage * coverage;
                float spread = distancePx / max(radius * 1.85, 0.65);
                float borderDist = min(min(local.x, 1.0 - local.x), min(local.y, 1.0 - local.y));
                float borderFade = smoothstep(0.04, 0.18, borderDist);
                float glow = exp(-spread * spread) * _MoonStarTwinkle.w * saturate(brightness - _MoonStarShape.z);

                float twinkle = 1.0;
                if (frac(h.z * 7.13) < _MoonStarTwinkle.x)
                {
                    twinkle += _MoonStarTwinkle.z * sin(_Time.y * _MoonStarTwinkle.y * (0.6 + h.z) + h.x * 61.0);
                }

                float3 tint = lerp(float3(0.72, 0.8, 1.0), float3(1.0, 0.88, 0.78), h.z);
                tint = lerp(tint, float3(0.86, 0.78, 1.0), saturate(h.y * 2.0 - 1.0));
                return tint * (core + glow) * borderFade * brightness * twinkle;
            }

            float3 ShootingStar(float3 dir, float pixel)
            {
                float period = max(_MoonShootingStars.x, 0.001);
                float duration = _MoonShootingStars.y;
                float slot = floor(_Time.y / period);
                float local = _Time.y - slot * period;

                // Each period starts at a random offset so the rhythm never feels mechanical.
                float start = Hash11(slot * 7.31) * max(period - duration, 0.0);
                float t = (local - start) / duration;
                if (t < 0.0 || t > 1.0)
                {
                    return 0;
                }

                float azimuth = Hash11(slot * 3.17 + 1.0) * 6.2831853;
                float elevation = lerp(0.45, 1.1, Hash11(slot * 5.71 + 2.0));
                float3 origin = float3(sin(azimuth) * cos(elevation), sin(elevation), cos(azimuth) * cos(elevation));
                float heading = Hash11(slot * 9.13 + 3.0) * 6.2831853;
                float3 east = normalize(cross(float3(0, 1, 0), origin));
                float3 north = cross(origin, east);
                float3 tangent = cos(heading) * east + sin(heading) * north;
                tangent = normalize(tangent - float3(0, 0.6, 0) * saturate(dot(tangent, float3(0, 1, 0))));
                float3 axis = normalize(cross(origin, tangent));

                const float arc = 0.55;
                const float tail = 0.16;
                float along = atan2(dot(cross(origin, dir), axis), dot(origin, dir));
                float head = t * arc;
                float across = abs(dot(dir, axis));
                float trail = saturate(1.0 - (head - along) / tail) * step(along, head) * step(head - tail, along);
                float halfWidth = across / (pixel * 1.6);
                float width = exp(-halfWidth * halfWidth);
                float fade = sin(t * 3.14159265);
                return float3(0.85, 0.9, 1.0) * trail * trail * width * fade * _MoonShootingStars.z;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float up = dir.y;
                float pixel = max(max(length(ddx(dir)), length(ddy(dir))), 1e-5);

                float rise = up > 1e-4 ? pow(saturate(up), _MoonSkyTop.a) : 0.0;
                float3 color = lerp(unity_FogColor.rgb, _MoonSkyTop.rgb, rise);
                color += _MoonHorizonGlow.rgb * exp(-max(up, 0.0) / _MoonHorizonGlow.a);
                color += _MoonHorizonBand.rgb * exp(-abs(up) / _MoonHorizonBand.a);

                float above = smoothstep(0.0, 0.16, up);
                float band = dot(dir, _MoonMilkyWayAxis.xyz) / _MoonMilkyWayColor.a;
                float bandMask = exp(-band * band);
                float haze = ValueNoise(dir * 7.0) * 0.6 + ValueNoise(dir * 19.0) * 0.4;
                color += _MoonMilkyWayColor.rgb * bandMask * smoothstep(0.25, 0.85, haze) * _MoonMilkyWayAxis.w
                    * above;

                float3 stars = StarLayer(dir, 140.0, _MoonStarParams.x, pixel, 0.08, 1.5, 0.0);
                stars += StarLayer(dir, 68.0, _MoonStarParams.x * 0.6, pixel, 0.15, 4.2, 41.0);
                stars += StarLayer(dir, 24.0, _MoonStarParams.x * 0.5, pixel, 0.4, 8.0, 67.0);
                stars += StarLayer(dir, 220.0, _MoonStarParams.x * bandMask, pixel, 0.06, 0.95, 83.0);
                color += stars * _MoonStarParams.y * above;

                float earthAngle = acos(clamp(dot(dir, _MoonEarthDirection.xyz), -1.0, 1.0));
                float radius = _MoonEarthDirection.w;
                float outside = max(earthAngle - radius, 0.0);

                // 3D outward normal at Earth's visible limb in direction `dir`, so the outer limb and halo follow
                // Earth's real sunlit crescent/half/full phase.
                float3 across = dir - _MoonEarthDirection.xyz * dot(dir, _MoonEarthDirection.xyz);
                float3 acrossUnit = normalize(across + 1e-6);
                float3 limbNormal = normalize(acrossUnit * 0.98 - _MoonEarthDirection.xyz * 0.20);
                float limbLit = smoothstep(-0.03, 0.22, dot(limbNormal, _MoonEarthSun.xyz));
                float sunward = dot(acrossUnit, _MoonEarthSun.xyz);

                float halo = exp(-outside / max(radius * _MoonEarthHalo.a, 1e-4) * 2.2);
                color += _MoonEarthHalo.rgb * halo * (0.12 + 0.88 * limbLit);

                float limb = exp(-outside / max(radius * _MoonEarthLimb.a, 1e-5));
                color += _MoonEarthLimb.rgb * limb * (0.03 + 0.97 * limbLit);

                if (_MoonEclipseRing.a > 0.0)
                {
                    float ringBand = abs(earthAngle - radius) / max(radius * _MoonEclipseRing.a, 1e-4);
                    float eclipseRing = exp(-ringBand * ringBand * 1.6);
                    color += _MoonEclipseRing.rgb * eclipseRing * (0.65 + 0.35 * saturate(sunward));
                }

                if (_MoonSunDirection.w > 0.0)
                {
                    float sunRadius = _MoonSunDirection.w;
                    float sunAngle = acos(clamp(dot(dir, _MoonSunDirection.xyz), -1.0, 1.0));
                    float sunOutside = max(sunAngle - sunRadius, 0.0);
                    float earthMask = smoothstep(radius * 0.992, radius * 1.006, earthAngle);
                    float horizonMask = smoothstep(-sunRadius, sunRadius * 0.5, up);
                    float core = smoothstep(sunRadius, sunRadius * 0.35, sunAngle);
                    float corona = exp(-sunOutside / max(sunRadius * 2.2, 1e-4));
                    float3 sunTone = _MoonSunColor.rgb;
                    float3 coronaTone = sunTone / max(max(sunTone.r, max(sunTone.g, sunTone.b)), 1.0);
                    color += (sunTone * core + coronaTone * (_MoonSunColor.a * corona)) * earthMask * horizonMask;
                }

                color += ShootingStar(dir, pixel) * above;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
