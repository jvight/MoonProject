// Lofi Lunar ground: the generated terrain, flat-shaded (every triangle carries its own face normal) and matte. Its
// colour is the vertex colour (sRGB, painted by MoonProject.World.TerrainPainter): one flat tone per rock facet,
// gentle continuous tones on the dust, so large patches carry soft value changes instead of paper-cut edges.
// Lighting is plain Lambert from the earthlight with its soft shadows, the trilight ambient (with SSAO), Forward+
// additional lights (07's lamp, the base) and the scene's fog, which fades distant ground toward the horizon.
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

            half3 Diffuse(Light light, float3 normal)
            {
                half attenuation = light.distanceAttenuation * light.shadowAttenuation;
                return light.color * (saturate(dot(normal, light.direction)) * attenuation);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 positionWS = input.positionWS;

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
                ambient *= occlusion.indirectAmbientOcclusion;
                directOcclusion = occlusion.directAmbientOcclusion;
                #endif

                Light mainLight = GetMainLight(inputData.shadowCoord, positionWS, half4(1, 1, 1, 1));
                half3 light = ambient + Diffuse(mainLight, normal) * directOcclusion;

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    light += Diffuse(GetAdditionalLight(lightIndex, positionWS, half4(1, 1, 1, 1)), normal);
                LIGHT_LOOP_END
                #endif

                half3 color = input.albedo * light;
                color = MixFog(color, InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactor));
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
