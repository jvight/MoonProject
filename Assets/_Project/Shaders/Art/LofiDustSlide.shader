// Gentle moonquake dust slide (0.6 Living Moon §2): vacuum-honest ballistic regolith sheeting slowly down a slope and
// settling in a low ground-skimming veil at the toe. Zero billowing smoke, zero repeated round-dot patterns, lit by
// the scene's directional light + ambient trilight in both Night A and Day C, and clamped below Palette.BloomThreshold.
Shader "MoonProject/Art/LofiDustSlide"
{
    Properties
    {
        [MainColor] _BaseColor("Day C Regolith Dust Color (sRGB)", Color) = (0.76, 0.72, 0.66, 0.86)
        _NightColor("Night A Earthlit Dust Color (sRGB)", Color) = (0.54, 0.62, 0.78, 0.82)
        _ShadowColor("Regolith Shadow Tint (sRGB)", Color) = (0.28, 0.32, 0.42, 1.0)
        _Opacity("Slide Envelope Opacity (0..1)", Range(0.0, 1.0)) = 0.82
        _ScrollOffset("Down-Slope Scroll Distance", Float) = 0.0
        _NightBlend("Night A Blend (0=DayC, 1=NightA)", Range(0.0, 1.0)) = 0.0
        _SlideMode("Mode (0=SlopeSheet, 1=ToeSettle, 2=BallisticGrain)", Range(0.0, 2.0)) = 0.0
        _StreakDensity("Longitudinal Stream Density", Range(4.0, 32.0)) = 14.0
        _BloomCeiling("Bloom Safe Ceiling", Range(0.4, 1.20)) = 1.05
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
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _NightColor;
                half4 _ShadowColor;
                half _Opacity;
                float _ScrollOffset;
                half _NightBlend;
                half _SlideMode;
                half _StreakDensity;
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(234.56, 789.12));
                p += dot(p, p + 34.56);
                return frac(p.x * p.y);
            }

            // 1D/2D value noise for non-periodic longitudinal mineral streaks
            float StreakNoise(float2 p)
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
                half4 baseDust = lerp(_BaseColor, _NightColor, nightBlend);

                float mask = 1.0;
                half grainFacet = 0.5h;

                if (_SlideMode < 0.5h)
                {
                    // Mode 0: Down-slope sheeting regolith streams (uv.x = across ribbon, uv.y = down slope)
                    float edgeX = smoothstep(0.0, 0.16, uv.x) * smoothstep(1.0, 0.84, uv.x);
                    float edgeY = smoothstep(0.0, 0.12, uv.y) * smoothstep(1.0, 0.85, uv.y);

                    float2 flowUv = float2(uv.x * (float)_StreakDensity * 0.65, uv.y * 4.2 - _ScrollOffset);
                    float s1 = StreakNoise(flowUv);
                    float s2 = StreakNoise(flowUv * float2(2.4, 1.8) + float2(7.1, -_ScrollOffset * 0.55));
                    // Angular saltating mineral grains embedded along the flowing streams (diamond metric, no round dots)
                    float2 grainCell = frac(float2(uv.x * 8.0, uv.y * 14.0 - _ScrollOffset * 1.8)) * 2.0 - 1.0;
                    float diamondGrain = saturate(1.0 - (abs(grainCell.x) * 1.8 + abs(grainCell.y) * 0.95));

                    float streamMask = saturate(0.42 + 0.58 * (s1 * 0.60 + s2 * 0.40) + diamondGrain * 0.45);
                    mask = edgeX * edgeY * streamMask;
                    grainFacet = (half)saturate(s1 * 0.65 + diamondGrain * 0.55);
                }
                else if (_SlideMode < 1.5h)
                {
                    // Mode 1: Low vacuum ballistic settle apron at the toe of the slope (hugs the ground, never billows up)
                    float radialX = abs(uv.x * 2.0 - 1.0);
                    float spanMask = saturate(1.0 - radialX * radialX);
                    float verticalSkim = smoothstep(0.0, 0.18, uv.y) * pow(saturate(1.0 - uv.y), 1.8);
                    float ripple = StreakNoise(float2(uv.x * 9.0, uv.y * 4.5 - _ScrollOffset * 0.35));
                    mask = spanMask * verticalSkim * (0.62 + 0.38 * ripple);
                    grainFacet = (half)ripple;
                }
                else
                {
                    // Mode 2: Discrete angular regolith grain cluster (faceted polygon mask, never round dot)
                    float2 p = uv * 2.0 - 1.0;
                    float hexA = abs(p.x) * 0.866025 + abs(p.y) * 0.5;
                    float hexB = abs(p.y);
                    float diag45 = (abs(p.x) + abs(p.y)) * 0.72;
                    float polyDist = max(max(hexA, hexB), diag45);
                    mask = saturate(1.0 - smoothstep(0.45, 0.92, polyDist));
                    grainFacet = (half)saturate(0.5 + 0.5 * (p.x - p.y));
                }

                float normLenSq = dot(input.normalWS, input.normalWS);
                float3 normalWS = normLenSq > 1e-6 ? input.normalWS * rsqrt(normLenSq) : float3(0.0, 1.0, 0.0);

                // Trilight ambient fallback so unbaked preview scenes and runtime scenes both light the dust sheet
                half upWeight = saturate((half)normalWS.y);
                half3 trilightAmbient = lerp(unity_AmbientEquator.rgb, unity_AmbientSky.rgb, upWeight) * 1.45h;
                half3 ambient = max(SampleSH(normalWS), trilightAmbient);

                Light mainLight = GetMainLight();
                half ndotl = saturate((half)dot(normalWS, mainLight.direction) * 0.65h + 0.35h);
                half3 directLight = mainLight.color * (ndotl * 0.85h);

                #if defined(_ADDITIONAL_LIGHTS)
                uint addCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < addCount; ++li)
                {
                    Light addLight = GetAdditionalLight(li, input.positionWS);
                    half addNdotL = saturate((half)dot(normalWS, addLight.direction) * 0.65h + 0.35h);
                    directLight += addLight.color * (addNdotL * addLight.distanceAttenuation * 0.65h);
                }
                #endif

                half3 dustAlbedo = lerp(_ShadowColor.rgb, baseDust.rgb, saturate(ndotl * 0.65h + grainFacet * 0.45h))
                    * input.vertexColor.rgb;
                // Forward-scatter mineral silt sheen so the regolith sheet reads clearly against dark Mare basalt
                half3 siltSheen = baseDust.rgb * (0.28h + 0.24h * grainFacet);
                half3 litColor = dustAlbedo * (ambient + directLight) + siltSheen;

                if (any(isnan(litColor)) || any(isinf(litColor)))
                {
                    litColor = baseDust.rgb;
                }

                litColor = max(litColor, 0.0h);
                half peak = max(litColor.r, max(litColor.g, litColor.b));
                if (peak > _BloomCeiling)
                {
                    litColor *= (_BloomCeiling / max(peak, 0.001h));
                }

                half alpha = saturate(baseDust.a * _Opacity * input.vertexColor.a * (half)mask);
                return half4(litColor, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
