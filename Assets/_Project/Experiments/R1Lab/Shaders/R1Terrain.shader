Shader "MoonProject/R1/Terrain"
{
    Properties
    {
        [MainTexture] _BaseMap("Regolith Albedo (RGB)", 2D) = "gray" {}
        _BaseColor("Regolith Base Tint", Color) = (1, 1, 1, 1)
        _SlopeColor("Slope Subsurface Tint", Color) = (0.78, 0.79, 0.82, 1.0)

        [NoScaleOffset] _BumpMap("Regolith Normal Map", 2D) = "bump" {}
        [NoScaleOffset] _MaskMap("Regolith Mask (R=Metal, G=AO, B=Height, A=Smooth)", 2D) = "white" {}

        [Header(Multi Scale and Distance Falloff)]
        _NearTiling("Near Pebble Tiling (cycles/m)", Range(0.1, 2.0)) = 0.58
        _MacroTiling("Macro Regolith Tiling (cycles/m)", Range(0.02, 0.4)) = 0.11
        _NearBumpScale("Near Normal Strength", Range(0.0, 2.5)) = 1.35
        _MacroBumpScale("Macro Normal Strength", Range(0.0, 2.0)) = 0.65
        _NearFadeStart("Near Detail Fade Start (m)", Float) = 4.5
        _NearFadeEnd("Near Detail Fade End (m)", Float) = 19.0
        _FineGrainStrength("Close Fine Grain Strength", Range(0.0, 0.25)) = 0.08
        _SmoothnessScale("Smoothness Multiplier", Range(0.0, 2.0)) = 0.95
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 0.90

        [Header(Triplanar Slope Blending)]
        _TriplanarSharpness("Triplanar Blend Sharpness", Range(1.0, 12.0)) = 5.0
        _SlopeThreshold("Slope Angle Threshold", Range(0.5, 0.98)) = 0.88
        _SlopeBlendRange("Slope Blend Range", Range(0.02, 0.4)) = 0.14

        [Header(Tyre Tracks)]
        _TrackStrength("Tyre Track Normal Strength", Range(0.0, 3.0)) = 1.65
        _TrackDarken("Tyre Track Albedo Compression", Range(0.0, 0.35)) = 0.14
        _TrackWidth("Single Wheel Track Width (m)", Range(0.15, 0.6)) = 0.34
        _TrackGauge("Half Wheel Gauge (m)", Range(0.4, 1.2)) = 0.68
        _TrackOriginAndYaw("Primary Track End (X, Z, YawRad, Length)", Vector) = (0.2, -0.8, 0.28, 18.0)
        _TrackCurve("Primary Track Curvature & Secondary Toggle", Vector) = (0.045, 1.0, -2.2, -0.45)
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

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex R1TerrainVert
            #pragma fragment R1TerrainFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/_Project/Shaders/World/LofiAtmosphere.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _SlopeColor;
                float _NearTiling;
                float _MacroTiling;
                half _NearBumpScale;
                half _MacroBumpScale;
                float _NearFadeStart;
                float _NearFadeEnd;
                half _FineGrainStrength;
                half _SmoothnessScale;
                half _OcclusionStrength;

                half _TriplanarSharpness;
                half _SlopeThreshold;
                half _SlopeBlendRange;

                half _TrackStrength;
                half _TrackDarken;
                float _TrackWidth;
                float _TrackGauge;
                float4 _TrackOriginAndYaw;
                float4 _TrackCurve;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS    : TEXCOORD1;
                half fogFactor    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise2D(float2 p)
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

            void SampleTriplanar(
                float3 posWS,
                half3 normalWS,
                float tiling,
                half bumpScale,
                out half3 outAlbedo,
                out half4 outMask,
                out half3 outNormalWS)
            {
                half3 blend = pow(abs(normalWS), _TriplanarSharpness);
                blend /= max(blend.x + blend.y + blend.z, 0.0001h);

                float2 uvX = posWS.zy * tiling;
                float2 uvY = posWS.xz * tiling;
                float2 uvZ = posWS.xy * tiling;

                half3 colX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb;
                half3 colY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb;
                half3 colZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb;
                outAlbedo = colX * blend.x + colY * blend.y + colZ * blend.z;

                half4 mX = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvX);
                half4 mY = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvY);
                half4 mZ = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvZ);
                outMask = mX * blend.x + mY * blend.y + mZ * blend.z;

                half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvX), bumpScale);
                half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvY), bumpScale);
                half3 nZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvZ), bumpScale);

                // Whiteout-style world-space triplanar normal blend
                half3 worldNX = half3(normalWS.x + nX.z * sign(normalWS.x), nX.y, nX.x);
                half3 worldNY = half3(nY.x, normalWS.y + nY.z * sign(normalWS.y), nY.y);
                half3 worldNZ = half3(nZ.x, nZ.y, normalWS.z + nZ.z * sign(normalWS.z));

                outNormalWS = normalize(worldNX * blend.x + worldNY * blend.y + worldNZ * blend.z);
            }

            // Evaluates a single dual-wheel rover track path in world XZ, returning signed height displacement and mask
            float EvalSingleTrackPair(float2 xz, float2 originXZ, float yaw, float trackLen, float curvature, out float outRutMask)
            {
                float2 rel = xz - originXZ;
                float c = cos(yaw);
                float s = sin(yaw);
                // Local coordinates: localZ >= 0 extends backward along the rover's incoming trail
                float localX = rel.x * c - rel.y * s;
                float localZ = rel.x * s + rel.y * c;

                // Smooth fade at both ends of the trail
                float alongMask = smoothstep(-0.35, 0.45, localZ) * (1.0 - smoothstep(trackLen - 2.5, trackLen, localZ));
                if (alongMask <= 0.001)
                {
                    outRutMask = 0.0;
                    return 0.0;
                }

                // Gentle quadratic curve of the rover path
                float curveOffset = curvature * localZ * localZ * 0.08 + sin(localZ * 0.35) * 0.08;
                float xCentered = localX - curveOffset;

                // Distance to closest of the two wheel tracks (left at -_TrackGauge, right at +_TrackGauge)
                float distToWheelCenter = abs(abs(xCentered) - _TrackGauge);
                float halfW = _TrackWidth * 0.5;
                float uWheel = distToWheelCenter / max(halfW, 0.01);

                // Depressed rut inside uWheel < 1, plus displaced regolith berm around uWheel in [1.0..1.45]
                float rutBowl = saturate(1.0 - uWheel * uWheel);
                rutBowl = rutBowl * rutBowl;
                float berm = exp(-((uWheel - 1.18) * (uWheel - 1.18)) / 0.065) * 0.28;

                // Moulded chevron tread lugs inside the rut
                float chevronPhase = frac(localZ * 5.2 + uWheel * 0.45);
                float chevronLug = smoothstep(0.18, 0.42, chevronPhase) * (1.0 - smoothstep(0.62, 0.86, chevronPhase));
                // Center rib groove down the middle of the tire
                float centerChannel = smoothstep(0.06, 0.18, uWheel);
                chevronLug *= centerChannel;

                float heightDisp = (-0.032 * rutBowl + 0.018 * rutBowl * chevronLug + 0.012 * berm) * alongMask;
                outRutMask = saturate((rutBowl + berm * 0.5) * alongMask);
                return heightDisp;
            }

            float EvalAllTracks(float2 xz, out float totalMask)
            {
                float m1 = 0.0;
                float h1 = EvalSingleTrackPair(
                    xz,
                    _TrackOriginAndYaw.xy,
                    _TrackOriginAndYaw.z,
                    _TrackOriginAndYaw.w,
                    _TrackCurve.x,
                    m1);

                float m2 = 0.0;
                float h2 = 0.0;
                if (_TrackCurve.y > 0.5)
                {
                    // Second older rover pass curving past the rocks and crate
                    h2 = EvalSingleTrackPair(
                        xz,
                        float2(_TrackCurve.z, 2.8),
                        _TrackCurve.w,
                        22.0,
                        -0.03,
                        m2) * 0.72;
                }

                totalMask = saturate(m1 + m2 * 0.8);
                return h1 + h2;
            }

            Varyings R1TerrainVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 R1TerrainFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 posWS = input.positionWS;
                half3 geomNormalWS = normalize(input.normalWS);

                // Camera distance falloff: crisp pebbles near camera, calm falloff at distance
                float camDist = distance(_WorldSpaceCameraPos, posWS);
                half nearWeight = (half)(1.0 - smoothstep(_NearFadeStart, _NearFadeEnd, camDist));

                // Slope factor: 0 on flat ground, 1 on steep crater walls / mounds
                half slopeFactor = saturate((_SlopeThreshold - abs(geomNormalWS.y)) / max(_SlopeBlendRange, 0.01h));

                // Sample Near & Macro scales using Triplanar projection
                half3 nearAlbedo, macroAlbedo;
                half4 nearMask, macroMask;
                half3 nearNormalWS, macroNormalWS;

                SampleTriplanar(posWS, geomNormalWS, _NearTiling, _NearBumpScale * nearWeight, nearAlbedo, nearMask, nearNormalWS);
                SampleTriplanar(posWS, geomNormalWS, _MacroTiling, _MacroBumpScale * (0.35h + 0.65h * nearWeight), macroAlbedo, macroMask, macroNormalWS);

                // Calm macro albedo contrast at distance so far ridges read as smooth velvety lunar regolith
                half3 calmMacroAlbedo = lerp(half3(0.55h, 0.56h, 0.59h), macroAlbedo, 0.25h);
                half3 nearCombinedAlbedo = lerp(calmMacroAlbedo, nearAlbedo, 0.72h);
                half3 albedo = lerp(calmMacroAlbedo, nearCombinedAlbedo, nearWeight) * _BaseColor.rgb;
                half4 mask = lerp(lerp(half4(0.0h, 0.92h, 0.5h, 0.10h), macroMask, 0.35h), nearMask, nearWeight * 0.80h);

                // Slope tinting & rocky subsurface exposure on steeper angles
                albedo = lerp(albedo, albedo * _SlopeColor.rgb, slopeFactor);

                // Subtle macro world-space tonal variation (prevents repetition across 40x40m patch)
                half broadTone = (half)ValueNoise2D(posWS.xz * 0.085);
                albedo *= (0.94h + 0.12h * broadTone);

                // Close-range fine velvety regolith grain
                if (nearWeight > 0.01h)
                {
                    half fineGrain = (half)ValueNoise2D(posWS.xz * 28.0);
                    albedo += (fineGrain - 0.5h) * _FineGrainStrength * nearWeight;
                }

                // Combine near + macro normals
                half3 combinedNormalWS = normalize(lerp(macroNormalWS, normalize(nearNormalWS + (macroNormalWS - geomNormalWS)), nearWeight));

                // Evaluate procedural tyre tracks in world XZ with central-difference normal
                float trackMask = 0.0;
                float hCenter = EvalAllTracks(posWS.xz, trackMask);
                if (trackMask > 0.001 && _TrackStrength > 0.001h)
                {
                    float dummyM;
                    float eps = 0.022;
                    float hRight = EvalAllTracks(posWS.xz + float2(eps, 0.0), dummyM);
                    float hForward = EvalAllTracks(posWS.xz + float2(0.0, eps), dummyM);
                    float dhdx = (hRight - hCenter) / eps;
                    float dhdz = (hForward - hCenter) / eps;

                    half3 trackNormalOffset = half3(-dhdx, 0.0, -dhdz) * _TrackStrength * (0.45h + 0.55h * nearWeight);
                    combinedNormalWS = normalize(combinedNormalWS + trackNormalOffset);

                    // Compressed regolith inside the tyre track: slightly darker in ruts, paler on raised lug crests
                    half crestLift = saturate((half)(hCenter * 18.0) * 0.045h);
                    half rutDarken = saturate((half)(-hCenter * 22.0) * _TrackDarken);
                    albedo = albedo * (1.0h - rutDarken) + half3(0.06h, 0.06h, 0.07h) * crestLift;
                }

                half metallic = saturate(mask.r);
                half occlusion = lerp(1.0h, saturate(mask.g * (1.0h - (half)trackMask * 0.14h)), _OcclusionStrength);
                half smoothness = saturate(mask.a * _SmoothnessScale + (half)trackMask * 0.04h);

                InputData inputData = (InputData)0;
                inputData.positionWS = posWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = combinedNormalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(posWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(posWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(combinedNormalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = saturate(albedo);
                surfaceData.metallic = metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = occlusion;
                surfaceData.alpha = 1.0h;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MoonAtmosphere(color.rgb, posWS, input.fogFactor);
                return half4(color.rgb, 1.0h);
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

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

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

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
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

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
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

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
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

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
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
                half3 normalWS    : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0h);
            }
            ENDHLSL
        }
    }
}
