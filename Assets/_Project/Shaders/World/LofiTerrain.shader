// Lofi Lunar ground: the generated terrain, flat-shaded (every triangle carries its own face normal) and matte. Its
// colour is the vertex colour (sRGB, painted by MoonProject.World.TerrainPainter): one flat tone per rock facet,
// gentle continuous tones on the dust, so large patches carry soft value changes instead of paper-cut edges.
// Lighting is plain Lambert from the earthlight with its soft shadows, the trilight ambient (with SSAO), Forward+
// additional punctual lights (07's lamp, the base) and the shared lofi atmosphere (LofiAtmosphere.hlsl): a thin basin
// haze, distance desaturation and the scene's fog, which fades distant ground toward the horizon.
// The light loop walks only the punctual lights: the earthlight's directional fill is left out on purpose, so it lifts
// the shadow sides of built things while the dunes' shadow sides and the long shadows stay deep (pillar 6).
Shader "MoonProject/World/LofiTerrain"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "LofiAtmosphere.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half3 albedo : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.albedo = SRGBToLinear(input.color.rgb);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            // The reach ring (World's ReachRingSystem): xyz its centre, w its radius; rgb its warm glow, w its half
            // width. Zero when no ring is rolling out.
            float4 _MoonReachRing;
            float4 _MoonReachRingColor;
            float _MoonLookDepth;
            float _MoonTerrainGrain;

            static const float2 kTerrainVogelDisk[8] =
            {
                float2(0.1768, 0.0000),
                float2(-0.2261, 0.2071),
                float2(0.0361, -0.4067),
                float2(0.3204, 0.3694),
                float2(-0.5467, -0.1045),
                float2(0.4534, -0.4152),
                float2(-0.0804, 0.6699),
                float2(-0.5798, -0.4581)
            };

            half SampleTerrainSoftShadow(float4 shadowCoord, float2 pixelPos, half baseShadow)
            {
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && !defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                if (_MoonLookDepth > 0.5 && shadowCoord.w > 0.0)
                {
                    float2 tileMin = floor(shadowCoord.xy * 2.0) * 0.5 + 0.001;
                    float2 tileMax = tileMin + 0.498;
                    float angle = frac(52.9829189 * frac(dot(pixelPos, float2(0.06711056, 0.00583715)))) * 6.2831853;
                    float s = sin(angle);
                    float c = cos(angle);
                    float radius = 0.00052;
                    half accum = baseShadow;
                    UNITY_UNROLL
                    for (int i = 0; i < 8; i++)
                    {
                        float2 d = kTerrainVogelDisk[i];
                        float2 rotated = float2(d.x * c - d.y * s, d.x * s + d.y * c) * radius;
                        float4 tap = shadowCoord;
                        tap.xy = clamp(shadowCoord.xy + rotated, tileMin, tileMax);
                        accum += MainLightRealtimeShadow(tap);
                    }
                    return accum / 9.0;
                }
                #endif
                return baseShadow;
            }

            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float ValueNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 2.0 - 1.0;
            }

            half3 ReachRing(float3 positionWS)
            {
                float fromCentre = length(positionWS.xz - _MoonReachRing.xz);
                half band = saturate(1.0 - abs(fromCentre - _MoonReachRing.w) / max(_MoonReachRingColor.w, 1e-3));
                return _MoonReachRingColor.rgb * (band * band);
            }

            half3 Diffuse(Light light, float3 normal)
            {
                half attenuation = light.distanceAttenuation * light.shadowAttenuation;
                half ndotl = dot(normal, light.direction);
                half lambert = _MoonLookDepth > 0.5
                    ? lerp(saturate(ndotl), smoothstep(-0.08, 0.94, ndotl), 0.65)
                    : saturate(ndotl);
                return light.color * (lambert * attenuation);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 positionWS = input.positionWS;
                half3 albedo = input.albedo;

                if (_MoonTerrainGrain > 0.5)
                {
                    float camDist = length(positionWS - _WorldSpaceCameraPos);
                    half distFade = (half)(1.0 - smoothstep(4.0, 28.0, camDist));
                    half dustMask = (half)saturate((normal.y - 0.72) * 5.0);
                    half weight = distFade * dustMask;
                    if (weight > 0.001)
                    {
                        float2 uv1 = positionWS.xz * 16.0;
                        float2 uv2 = positionWS.xz * 44.0 + float2(17.3, -9.1);
                        float2 uv3 = positionWS.xz * 95.0 + float2(-31.7, 23.4);
                        float n1 = ValueNoise2D(uv1);
                        float n2 = ValueNoise2D(uv2);
                        float n3 = ValueNoise2D(uv3);
                        float nGradX = ValueNoise2D(uv2 + float2(0.45, 0.0)) - n2;
                        float nGradZ = ValueNoise2D(uv2 + float2(0.0, 0.45)) - n2;
                        half grain = (half)((n1 * 0.30 + n2 * 0.45 + n3 * 0.25) * weight);
                        // Very subtle palette-tinted dust micro-grain between cool dust-shadow and warm dust-light
                        half3 coolShift = albedo * half3(0.980, 0.982, 0.988);
                        half3 warmShift = albedo * half3(1.020, 1.018, 1.012);
                        albedo = lerp(coolShift, warmShift, saturate(0.5 + grain * 0.5));
                        normal = normalize(normal + float3(nGradX, 0.0, nGradZ) * (0.009 * weight));
                    }
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalWS = normal;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                inputData.shadowCoord = float4(inputData.normalizedScreenSpaceUV, 0.0, 1.0);
                #else
                inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
                #endif

                half3 ambient = SampleSH(normal);
                half directOcclusion = 1.0;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor occlusion = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                half aoBoost = _MoonLookDepth > 0.5 ? 1.22 : 1.0;
                ambient *= saturate(1.0 - (1.0 - occlusion.indirectAmbientOcclusion) * aoBoost);
                directOcclusion = saturate(1.0 - (1.0 - occlusion.directAmbientOcclusion) * aoBoost);
                #endif

                Light mainLight = GetMainLight(inputData.shadowCoord, positionWS, half4(1, 1, 1, 1));
                mainLight.shadowAttenuation = SampleTerrainSoftShadow(inputData.shadowCoord, input.positionCS.xy,
                    mainLight.shadowAttenuation);
                half3 light = ambient + Diffuse(mainLight, normal) * directOcclusion;

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    light += Diffuse(GetAdditionalLight(lightIndex, positionWS, half4(1, 1, 1, 1)), normal);
                LIGHT_LOOP_END
                #endif

                half3 color = albedo * light + ReachRing(positionWS);
                color = MoonAtmosphere(color, positionWS, input.fogFactor);
                return half4(color, 1.0);
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
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            // Every facet has its own vertices and normal, so the usual normal-offset bias would push neighbouring
            // facets apart and open cracks in the shadow map (light leaking along every edge): the ground is
            // biased along the light only.
            float4 Vert(float3 positionOS : POSITION) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(positionOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 toLight = normalize(_LightPosition - positionWS);
                #else
                float3 toLight = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(positionWS + toLight * _ShadowBias.x);
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 Frag() : SV_Target
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
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Vert(float3 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);
            }

            half Frag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
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
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
