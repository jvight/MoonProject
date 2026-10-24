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

            // Non-periodic hash for fine electrostatic dust striation (no circular dots).
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
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

                // Horizontal span envelope (soft fade at left/right ends of the terminator band)
                float spanEdge = saturate(sin(saturate(uv.x) * 3.14159265));
                spanEdge = smoothstep(0.0, 0.75, spanEdge);

                // Vertical horizon haze profile: luminous band just above the horizon ridge, fading skyward
                float bottomFade = smoothstep(0.0, 0.16, uv.y);
                float topFade = pow(saturate(1.0 - uv.y), max((float)_BandSoftness, 0.5));
                float bandMask = bottomFade * topFade * spanEdge;

                // Fine stratified horizontal shear + slow upward electrostatic drift
                float driftY = uv.y * (float)_StreakScale * 0.5 - _Phase * 0.45;
                float strata = 0.75 + 0.25 * sin(driftY * 6.2831853 + uv.x * 11.0);
                // Bright horizon core line near uv.y = 0.22
                float coreBand = exp(-pow((uv.y - 0.22) * 5.5, 2.0)) * spanEdge * 0.55;

                // Column & crystalline mote shard mask (uv.x is 0..1 across each column; never round dots)
                float2 shardUv = uv * 2.0 - 1.0;
                float colSeed = input.vertexColor.b;
                float shaftProfile = saturate(1.0 - abs(shardUv.x) * 1.15);
                shaftProfile = shaftProfile * shaftProfile;

                // Rising column filament modulation + discrete rising diamond-spindle motes inside each column
                float colWave = 0.55 + 0.45 * sin((uv.y * 3.5 - _Phase * (0.75 + colSeed * 0.45)) * 6.2831853 + colSeed * 6.28);
                float moteCellY = frac(uv.y * 5.5 - _Phase * (0.55 + colSeed * 0.35) + colSeed * 4.13) * 2.0 - 1.0;
                float moteCellX = (shardUv.x + sin(uv.y * 12.0 + colSeed * 6.28) * 0.22);
                // Anisotropic diamond spindle: |x|*2.4 + |y|*1.1
                float spindleDist = abs(moteCellX) * 2.4 + abs(moteCellY) * 1.1;
                float spindleMote = saturate(1.0 - smoothstep(0.18, 0.88, spindleDist));

                float columnMask = bottomFade * topFade * saturate(shaftProfile * 0.68 * colWave + spindleMote * 0.95);

                half mode = saturate(_ColumnMode);
                float horizonShape = saturate(bandMask * strata + coreBand);
                float shapeMask = lerp(horizonShape, columnMask, mode);

                // Grazing view enhancement so the haze band reads like forward-scattered lunar terminator dust
                float3 viewRaw = GetWorldSpaceViewDir(input.positionWS);
                float viewLenSq = dot(viewRaw, viewRaw);
                float3 viewDir = viewLenSq > 1e-6 ? viewRaw * rsqrt(viewLenSq) : float3(0.0, 0.0, 1.0);
                float normLenSq = dot(input.normalWS, input.normalWS);
                float3 normalWS = normLenSq > 1e-6 ? input.normalWS * rsqrt(normLenSq) : float3(0.0, 1.0, 0.0);
                half grazing = (half)saturate(0.72 + 0.28 * abs(dot(normalWS, viewDir)));

                half shimmerMix = (half)saturate(0.28 * colWave + 0.48 * spindleMote * mode + coreBand * 0.45);
                half3 tint = lerp(baseTint.rgb, _RimColor.rgb, shimmerMix);
                half intensity = max(_Intensity, 0.0h) * grazing;

                // Note: Blend SrcAlpha One multiplies rgb * alpha in the output merger, so keep shapeMask in alpha
                // and a gentle boost in rgb so the glow isn't quadratically attenuated.
                half3 rgb = tint * intensity * (half)lerp(0.65, 1.15, shapeMask);
                half alpha = saturate(baseTint.a * input.vertexColor.a * (half)shapeMask * saturate(_Intensity * 1.35h));

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
