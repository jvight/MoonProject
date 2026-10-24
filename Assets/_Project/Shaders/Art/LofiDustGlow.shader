// Levitating dust glow (0.6 Living Moon §2): soft terminator horizon haze band and thin columns of slowly rising
// electrostatically levitated dust motes. Supports both Day C (warm grazing terminator scatter) and Night A (cool
// earthshine scatter), with elongated faceted/streaked mote geometry (zero repeated round-dot patterns) and a strict
// bloom ceiling below Palette.BloomThreshold (1.25).
Shader "MoonProject/Art/LofiDustGlow"
{
    Properties
    {
        [MainColor] _BaseColor("Day Terminator Tint (sRGB)", Color) = (0.95, 0.83, 0.64, 0.78)
        _NightColor("Night Earthshine Tint (sRGB)", Color) = (0.64, 0.78, 0.98, 0.74)
        _RimColor("Electrostatic Shimmer Tint (sRGB)", Color) = (1.00, 0.92, 0.78, 1.0)
        _Intensity("Envelope Intensity", Range(0.0, 1.2)) = 0.88
        _NightBlend("Night A Blend (0=DayC, 1=NightA)", Range(0.0, 1.0)) = 0.0
        _Phase("Drift Phase", Float) = 0.0
        _ColumnMode("Mode (0=HorizonBand, 1=MoteColumn, 2=EarthDisc)", Range(0.0, 2.0)) = 0.0
        _BandSoftness("Vertical Edge Softness", Range(0.5, 6.0)) = 1.65
        _StreakScale("Anisotropic Streak Scale", Range(2.0, 40.0)) = 14.0
        _BloomCeiling("Bloom Safe Ceiling", Range(0.5, 1.24)) = 1.15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _NightColor;
                half4 _RimColor;
                half _Intensity;
                half _NightBlend;
                float _Phase;
                half _ColumnMode;
                half _BandSoftness;
                half _StreakScale;
                half _BloomCeiling;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 vertexColor : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Non-periodic hash and smooth 2D value noise for organic electrostatic haze and wisps (zero bars/stripes).
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float GlowNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.vertexColor = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                half nightBlend = saturate(_NightBlend);
                half4 baseTint = lerp(_BaseColor, _NightColor, nightBlend);

                // Mode 2: Faceted Earthrise celestial disc + atmospheric limb glow
                if (_ColumnMode > 1.5h)
                {
                    float2 p = uv * 2.0 - 1.0;
                    float r = length(p);
                    float discMask = saturate(1.0 - smoothstep(0.84, 1.0, r));
                    // Crescent phase illumination from upper-left + subtle cloud band swirl
                    float2 sunDir2D = normalize(float2(-0.55, 0.83));
                    float crescent = saturate(dot(p, sunDir2D) * 0.85 + 0.55);
                    float cloudSwirl = 0.82 + 0.18 * sin(p.y * 9.5 + sin(p.x * 5.0) * 1.6);
                    float limbHalo = smoothstep(0.55, 0.92, r) * saturate(1.0 - smoothstep(0.90, 1.0, r));

                    half3 earthRgb = lerp(baseTint.rgb * 0.35h, lerp(baseTint.rgb, _RimColor.rgb, (half)(crescent * 0.55)), (half)(crescent * cloudSwirl));
                    earthRgb += _RimColor.rgb * (half)(limbHalo * 0.55);
                    earthRgb *= max(_Intensity, 0.0h);

                    half peakEarth = max(earthRgb.r, max(earthRgb.g, earthRgb.b));
                    if (peakEarth > _BloomCeiling)
                    {
                        earthRgb *= (_BloomCeiling / max(peakEarth, 0.001h));
                    }

                    half alphaEarth = saturate((half)discMask * saturate(_Intensity * 1.15h));
                    return half4(max(earthRgb, 0.0h), alphaEarth);
                }

                // Horizontal span envelope (soft continuous fade at far left/right ends of the terminator band)
                float spanEdge = saturate(sin(saturate(uv.x) * 3.14159265));
                spanEdge = smoothstep(0.0, 0.65, spanEdge);

                // Smooth organic multi-octave drift noise (no periodic horizontal or vertical sine bars)
                float nLow = GlowNoise(float2(uv.x * 2.8 + _Phase * 0.07, uv.y * 1.6 - _Phase * 0.18));
                float nMid = GlowNoise(float2(uv.x * 6.3 - _Phase * 0.11, uv.y * 3.1 - _Phase * 0.29));
                float nFine = GlowNoise(float2(uv.x * 13.7 + _Phase * 0.16, uv.y * 5.8 - _Phase * 0.43));
                float organicVeil = 0.64 + 0.36 * (nLow * 0.55 + nMid * 0.30 + nFine * 0.15);

                // Gently undulating horizon ridge glow core + soft skyward exponential/power falloff
                float ridgeCenterY = 0.19 + (nLow - 0.5) * 0.08;
                float bottomFade = smoothstep(0.0, 0.18, uv.y);
                float topFade = pow(saturate(1.0 - uv.y), max((float)_BandSoftness, 0.5));
                float bandMask = bottomFade * topFade * spanEdge * organicVeil * 0.82;
                float coreBand = exp(-pow((uv.y - ridgeCenterY) * 4.2, 2.0)) * spanEdge * (0.36 + 0.24 * nMid);

                // Mode 1: Irregular, sparse drifting mote wisps & crystalline mote glints (never uniform vertical bars)
                float2 shardUv = uv * 2.0 - 1.0;
                float colSeed = input.vertexColor.b;
                float heightVar = max(0.35, (float)input.vertexColor.g);

                // Meandering lateral warp so wisps drift as soft curved veils instead of straight columns
                float wispWarp = (GlowNoise(float2(uv.y * 1.8 - _Phase * 0.28, colSeed * 17.3)) - 0.5) * 0.42;
                float warpedX = shardUv.x + wispWarp * (1.0 - abs(shardUv.x) * 0.5);
                float lateralSoft = exp(-warpedX * warpedX * 2.4) * smoothstep(1.0, 0.06, abs(shardUv.x));

                // Sparse, non-periodic vertical puff modulation (1-2 soft wisps along the height, no repeating ladder)
                float wispNoiseA = GlowNoise(float2(uv.y * 1.6 - _Phase * (0.34 + colSeed * 0.24), colSeed * 31.7));
                float wispNoiseB = GlowNoise(float2(uv.y * 3.4 - _Phase * (0.48 + colSeed * 0.30), colSeed * 53.1 + 4.2));
                float sparseWisp = smoothstep(0.12, 0.72, wispNoiseA * 0.65 + wispNoiseB * 0.35);

                // Two sparse, irregularly spaced elongated mote glints drifting within the wisp
                float glint1Y = frac(colSeed * 3.71 + _Phase * (0.18 + colSeed * 0.12)) * 0.70 + 0.15;
                float glint2Y = frac(colSeed * 7.19 + 0.43 + _Phase * (0.24 + colSeed * 0.09)) * 0.64 + 0.18;
                float d1 = abs(warpedX) * 1.65 + abs(uv.y - glint1Y) / max(0.18, 0.28 * heightVar);
                float d2 = abs(warpedX - 0.08) * 1.85 + abs(uv.y - glint2Y) / max(0.15, 0.22 * heightVar);
                float spindleMote = max(
                    saturate(1.0 - smoothstep(0.08, 0.92, d1)),
                    saturate(1.0 - smoothstep(0.08, 0.88, d2)) * 0.85);

                float wispBottom = smoothstep(0.0, 0.12, uv.y);
                float wispTopFade = pow(saturate(1.0 - uv.y), 0.78 + (1.0 - heightVar) * 0.65);
                float ribbonWispMask = wispBottom * wispTopFade * lateralSoft * saturate(0.52 + sparseWisp * 0.85 + spindleMote * 1.15);
                // Discrete 3D mote shard particles carry vertexColor.r >= 0.5 (from particle startColor), whereas wisp ribbons encode r < 0.5
                float particleShardMask = saturate(1.0 - length(shardUv) * 0.75) * 0.95;
                float columnMask = (input.vertexColor.r < 0.5h) ? ribbonWispMask : particleShardMask;

                half mode = saturate(_ColumnMode);
                float horizonShape = saturate(bandMask + coreBand);
                float shapeMask = lerp(horizonShape, columnMask, mode);

                // Grazing view enhancement so the haze band reads like forward-scattered lunar terminator dust
                float3 viewRaw = GetWorldSpaceViewDir(input.positionWS);
                float viewLenSq = dot(viewRaw, viewRaw);
                float3 viewDir = viewLenSq > 1e-6 ? viewRaw * rsqrt(viewLenSq) : float3(0.0, 0.0, 1.0);
                float normLenSq = dot(input.normalWS, input.normalWS);
                float3 normalWS = normLenSq > 1e-6 ? input.normalWS * rsqrt(normLenSq) : float3(0.0, 1.0, 0.0);
                half grazing = (half)saturate(0.82 + 0.18 * abs(dot(normalWS, viewDir)));

                half shimmerMix = (half)saturate(0.32 * sparseWisp + 0.62 * spindleMote * mode + coreBand * 0.42);
                half3 tint = lerp(baseTint.rgb, _RimColor.rgb, shimmerMix);
                half intensity = max(_Intensity, 0.0h) * grazing;

                // Note: Blend SrcAlpha One multiplies rgb * alpha in the output merger, so boost wisp mode (mode=1)
                // so sparse drifting mote wisps read clearly above the horizon glow band while respecting _BloomCeiling.
                half modeBoost = lerp(1.0h, 1.55h, mode);
                half3 rgb = tint * intensity * modeBoost * (half)lerp(0.75, 1.25, shapeMask);
                half alpha = saturate(baseTint.a * input.vertexColor.a * (half)shapeMask * saturate(_Intensity * lerp(1.35h, 1.95h, mode)));

                if (any(isnan(rgb)) || any(isinf(rgb)))
                {
                    rgb = half3(0.0h, 0.0h, 0.0h);
                }

                rgb = max(rgb, 0.0h);
                half peak = max(rgb.r, max(rgb.g, rgb.b));
                if (peak > _BloomCeiling)
                {
                    rgb *= (_BloomCeiling / max(peak, 0.001h));
                }

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
