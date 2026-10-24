Shader "MoonProject/R1/MareDecal"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo (RGBA Feathered)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0.0, 2.0)) = 0.45
        [NoScaleOffset] _MaskMap("Mask Map (R=Metal, G=AO, B=Detail, A=Smooth)", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.08
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _DustColor("Dust Cavity Color", Color) = (0.42, 0.46, 0.56, 1.0)
        _DustAmount("Dust Amount", Range(0.0, 1.0)) = 0.15
        _WearColor("Edge Wear Color", Color) = (0.86, 0.90, 0.96, 1.0)
        _WearAmount("Edge Wear Amount", Range(0.0, 1.0)) = 0.10
        _AOStrength("Vertex AO Strength", Range(0.0, 1.0)) = 0.30
        _MicroVariation("Micro Detail Variation", Range(0.0, 0.5)) = 0.08
        _Opacity("Overall Opacity", Range(0.0, 1.0)) = 0.92
        _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength("Emission Strength", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Geometry+15"
        }

        Pass
        {
            Name "ForwardMareDecal"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -2, -2
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../World/LofiAtmosphere.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _Smoothness;
                half _Metallic;
                half4 _DustColor;
                half _DustAmount;
                half4 _WearColor;
                half _WearAmount;
                half _AOStrength;
                half _MicroVariation;
                half _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float2 uvRaw      : TEXCOORD4;
                half4  color      : COLOR;
                half   upMask     : TEXCOORD5;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.tangentWS = float4(normInputs.tangentWS, IN.tangentOS.w);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uvRaw = IN.uv;
                OUT.color = IN.color;
                // Only render top-facing surfaces of the thin decal sheet in world space; rim/bottom fade to 0
                OUT.upMask = saturate((normInputs.normalWS.y - 0.15) / 0.55);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 maskSample = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, IN.uv);

                // Smooth UV border feather so polygon boundaries are 100% invisible
                float2 uvClamped = saturate(IN.uvRaw);
                half borderX = pow( saturate(sin(3.14159265 * uvClamped.x)), 1.25 );
                half borderY = pow( saturate(sin(3.14159265 * uvClamped.y)), 1.10 );
                half uvFeather = borderX * borderY;

                half vcolAlpha = IN.color.a > 0.02h ? lerp(0.65h, 1.0h, IN.color.a) : 1.0h;
                half alpha = saturate(baseSample.a * _BaseColor.a * _Opacity * uvFeather * vcolAlpha * IN.upMask);
                if (alpha < 0.003h)
                {
                    discard;
                }

                half3 albedo = baseSample.rgb * _BaseColor.rgb;
                half microVar = (maskSample.b - 0.5h) * _MicroVariation;
                albedo = saturate(albedo * (1.0h + microVar));

                half wearMask = saturate(IN.color.g * _WearAmount);
                albedo = lerp(albedo, _WearColor.rgb, wearMask * 0.35h);

                half dustMask = saturate(IN.color.b * _DustAmount);
                albedo = lerp(albedo, _DustColor.rgb, dustMask * 0.45h);

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                float3 N_geom = normalize(IN.normalWS);
                float3 T = normalize(IN.tangentWS.xyz);
                float3 B = cross(N_geom, T) * IN.tangentWS.w;
                float3 N = normalize(normalTS.x * T + normalTS.y * B + normalTS.z * N_geom);
                float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                half ao = lerp(1.0h, IN.color.r * maskSample.g, _AOStrength);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half NdotL = saturate(dot(N, mainLight.direction));
                // Lunar silicate opposition/grazing lift so the swirl reads softly even under low grazing sun
                half grazingLift = saturate(NdotL * 0.72h + 0.28h);
                half shadowAtten = lerp(0.24h, 1.0h, mainLight.shadowAttenuation) * mainLight.distanceAttenuation;

                half3 directLight = mainLight.color * (grazingLift * shadowAtten);

                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light addLight = GetAdditionalLight(lightIndex, IN.positionWS);
                    half addNdotL = saturate(dot(N, addLight.direction));
                    directLight += addLight.color * (addNdotL * addLight.distanceAttenuation * addLight.shadowAttenuation);
                }
                #endif

                half3 ambient = SampleSH(N) * ao;
                half3 H = normalize(mainLight.direction + V);
                half spec = pow(saturate(dot(N, H)), 12.0h) * _Smoothness * 0.12h * shadowAtten;

                half3 finalRGB = albedo * (directLight * ao + ambient) + mainLight.color * spec;
                finalRGB = MoonAtmosphere(finalRGB, IN.positionWS, 0.0h);

                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
