// Lofi Lunar weather skins (VISION ruling 12): the removable layers of decades laid over home and 07 (tired patchwork
// paint, rust runs and blooms, dust settling on top faces and rising from the ground). Each face's colour is its
// vertex colour (sRGB, baked by the Art builders from palette swatches, so a stain can fade down a panel and the dust
// can climb a leg), lit exactly as M_LowPoly is lit (LofiLitCore: the same light, rim and air), so a skin painted in a
// swatch's surface colour matches the surface under it and a fading stain shows no seam. Shadows and depth come from
// LofiLit's passes, which share this shader's per-material layout (LofiLitInput).
Shader "MoonProject/Art/LofiWeather"
{
    Properties
    {
        // LofiLit's per-material values: the colour pass reads only the rim; the rest keeps the borrowed shadow and
        // depth passes on one SRP Batcher layout.
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _SpecColor("Specular Color", Color) = (0, 0, 0, 0)
        _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _Surface("Surface", Float) = 0.0
        _MoonRim("Rim (brightness, exponent, up cutoff, horizon lean)", Vector) = (0, 3, 0.6, 0.6)
        _MoonAoTint("Foot occlusion tint", Color) = (0.3, 0.3, 0.4, 1)
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

            #include "LofiLitInput.hlsl"
            #include "LofiLitCore.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 occlusion : TEXCOORD1;
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
                half occlusion : TEXCOORD4;
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
                output.occlusion = (half)input.occlusion.x;
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

                LofiSurface surface;
                surface.positionWS = input.positionWS;
                surface.normalWS = input.normalWS;
                surface.positionCS = input.positionCS;
                surface.albedo = input.albedo;
                surface.emission = half3(0, 0, 0);
                surface.albedo = lerp(surface.albedo, _MoonAoTint.rgb, input.occlusion);
                surface.vertexLight = input.fogFactorAndVertexLight.yzw;
                surface.fogFactor = input.fogFactorAndVertexLight.x;
                return half4(LofiLit(surface), 1.0);
            }
            ENDHLSL
        }

        UsePass "MoonProject/Art/LofiLit/SHADOWCASTER"
        UsePass "MoonProject/Art/LofiLit/DEPTHONLY"
        UsePass "MoonProject/Art/LofiLit/DEPTHNORMALS"
    }

    Fallback Off
}
