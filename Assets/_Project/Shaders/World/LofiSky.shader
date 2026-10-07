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
            float4 _MoonMilkyWayAxis;    // xyz band plane normal, w intensity
            float4 _MoonMilkyWayColor;   // rgb haze colour (linear), a angular half-width
            float4 _MoonEarthDirection;  // xyz unit direction to Earth, w angular radius (radians)
            float4 _MoonEarthHalo;       // rgb halo colour (linear), a halo size as a fraction of the radius
            float4 _MoonEarthLimb;       // rgb limb colour (linear), a limb width as a fraction of the radius
            float4 _MoonEarthSun;        // xyz world direction toward the sun lighting Earth
            float4 _MoonShootingStars;   // x period, y duration, z brightness

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

            // One layer of stars: at most one star per 3D cell of the direction scaled by `cells`. A star is kept
            // only when its projection on the sphere lies well inside its own cell, so no star is cut by a border.
            // Its brightness follows a power law from `faintest` up (N(>b) ~ b^-exponent, capped), its core grows a
            // little with brightness, the brightest carry a soft glow, and only a few twinkle, slowly.
            float3 StarLayer(float3 dir, float cells, float density, float pixel, float faintest, float seed)
            {
                float3 p = dir * cells;
                float3 cell = floor(p);
                float3 h = Hash33(cell + seed);
                if (h.x > density)
                {
                    return 0;
                }

                float3 star = normalize(cell + 0.2 + 0.6 * Hash33(cell + seed + 19.19)) * cells;
                float3 local = star - cell;
                if (any(local < 0.2) || any(local > 0.8))
                {
                    return 0;
                }

                // The glow is a Gaussian a few pixels wide, so it has faded to nothing long before the cell's edge.
                float brightness = min(faintest * pow(max(h.y, 1e-3), -1.0 / _MoonStarParams.z), 2.5);
                float distancePx = length(p - star) / cells / pixel;
                float radius = _MoonStarParams.w * (0.65 + 0.45 * sqrt(brightness));
                float core = saturate(1.0 - distancePx / radius);
                core *= core;
                float spread = distancePx / (radius * 2.5);
                float glow = exp(-spread * spread) * _MoonStarTwinkle.w * saturate(brightness - 0.8);

                float twinkle = 1.0;
                if (frac(h.z * 7.13) < _MoonStarTwinkle.x)
                {
                    twinkle += _MoonStarTwinkle.z * sin(_Time.y * _MoonStarTwinkle.y * (0.6 + h.z) + h.x * 61.0);
                }

                float3 tint = lerp(float3(0.72, 0.8, 1.0), float3(1.0, 0.88, 0.78), h.z);
                tint = lerp(tint, float3(0.86, 0.78, 1.0), saturate(h.y * 2.0 - 1.0));
                return tint * (core + glow) * brightness * twinkle;
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
                float pixel = max(length(fwidth(dir)), 1e-5);

                float rise = pow(saturate(up), _MoonSkyTop.a);
                float3 color = lerp(unity_FogColor.rgb, _MoonSkyTop.rgb, rise);
                color += _MoonHorizonGlow.rgb * exp(-max(up, 0.0) / _MoonHorizonGlow.a);
                color += _MoonHorizonBand.rgb * exp(-abs(up) / _MoonHorizonBand.a);

                float above = smoothstep(0.0, 0.16, up);
                float band = dot(dir, _MoonMilkyWayAxis.xyz) / _MoonMilkyWayColor.a;
                float bandMask = exp(-band * band);
                float haze = ValueNoise(dir * 7.0) * 0.6 + ValueNoise(dir * 19.0) * 0.4;
                color += _MoonMilkyWayColor.rgb * bandMask * smoothstep(0.25, 0.85, haze) * _MoonMilkyWayAxis.w
                    * above;

                float3 stars = StarLayer(dir, 150.0, _MoonStarParams.x, pixel, 0.08, 0.0);
                stars += StarLayer(dir, 70.0, _MoonStarParams.x * 0.6, pixel, 0.15, 41.0);
                stars += StarLayer(dir, 24.0, _MoonStarParams.x * 0.5, pixel, 0.4, 67.0);
                stars += StarLayer(dir, 260.0, _MoonStarParams.x * bandMask, pixel, 0.06, 83.0);
                color += stars * _MoonStarParams.y * above;

                float earthAngle = acos(clamp(dot(dir, _MoonEarthDirection.xyz), -1.0, 1.0));
                float radius = _MoonEarthDirection.w;
                float outside = max(earthAngle - radius, 0.0);
                float halo = exp(-outside / max(radius * _MoonEarthHalo.a, 1e-4) * 2.2);
                color += _MoonEarthHalo.rgb * halo;

                // The thin atmospheric limb, brightest on Earth's sunlit side.
                float3 across = dir - _MoonEarthDirection.xyz * dot(dir, _MoonEarthDirection.xyz);
                float sunward = dot(normalize(across + 1e-6), _MoonEarthSun.xyz);
                float limb = exp(-outside / max(radius * _MoonEarthLimb.a, 1e-5));
                color += _MoonEarthLimb.rgb * limb * saturate(0.3 + 0.7 * sunward);

                color += ShootingStar(dir, pixel) * above;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
