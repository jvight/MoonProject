// Lofi Lunar weather skins (VISION ruling 12): the removable layers of decades laid over home and 07 (tired patchwork
// paint, rust runs and blooms, dust settling on top faces and rising from the ground). Each face's colour is its
// vertex colour (sRGB, baked by the Art builders from palette swatches, so a stain can fade down a panel and the dust
// can climb a leg), lit the way M_LowPoly is lit (URP Simple Lit's Blinn-Phong path with no specular, the same
// keywords, ambient, SSAO, shadows and fog): a skin painted in a swatch's surface colour matches the surface under it
// exactly, so a fading stain shows no seam. Shadows and depth come from Simple Lit's own passes.
Shader "MoonProject/Art/LofiWeather"
{
    Properties
    {
        // Simple Lit's per-material values: unused by the colour pass, kept so the borrowed shadow and depth passes
        // share one SRP Batcher layout with it.
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _SpecColor("Specular Color", Color) = (0, 0, 0, 0)
        _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _Surface("Surface", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 albedo : TEXCOORD2;
                half4 fogFactorAndVertexLight : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                half3 normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normalWS;
                output.albedo = SRGBToLinear(input.color.rgb);
                half3 vertexLight = half3(0, 0, 0);
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                vertexLight = VertexLighting(positions.positionWS, normalWS);
                #endif
                output.fogFactorAndVertexLight = half4(ComputeFogFactor(positions.positionCS.z), vertexLight);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0),
                    input.fogFactorAndVertexLight.x);
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = input.albedo;
                surfaceData.alpha = 1.0;
                surfaceData.occlusion = 1.0;

                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return half4(color.rgb, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Simple Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Simple Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Simple Lit/DEPTHNORMALS"
    }

    Fallback Off
}
