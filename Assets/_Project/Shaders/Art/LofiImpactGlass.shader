// Micrometeorite flash & cooling impact glass (0.6 Living Moon §2):
// Mode 0 (Surface): Small fresh crater regolith + fused vitreous impact glass pool & shards. Vertex color channels follow
// R1 convention (R=AO, G=EdgeWear, B=Cavity, A=GlassMeltMask). Drives thermal emission via _Heat (1 -> 0 over ~10 min)
// from white-hot core (#FFF5DC) through molten amber (#FF7624) and deep ember (#9E280C) down to a permanent non-zero
// specular/emissive glint floor (_GlintFloor) so the cooled impact glass remains collectable forever.
// Mode 1 (Flash / Ejecta): Brief distant white starlight flash & ballistic vacuum ejecta shards.
Shader "MoonProject/Art/LofiImpactGlass"
{
    Properties
    {
        [MainColor] _BaseColor("Vitreous Glass / Regolith Albedo (sRGB)", Color) = (0.14, 0.18, 0.25, 1.0)
        _CraterRimColor("Fresh Crater Ejecta Regolith (sRGB)", Color) = (0.38, 0.40, 0.45, 1.0)
        [HDR] _HotCoreColor("Incandescent Core Color", Color) = (1.00, 0.94, 0.80, 1.0)
        [HDR] _WarmAmberColor("Molten Amber Color", Color) = (1.00, 0.46, 0.14, 1.0)
        [HDR] _DeepEmberColor("Cooling Ember Color", Color) = (0.62, 0.16, 0.05, 1.0)
        _PermanentGlintColor("Permanent Cooled Glint Color", Color) = (0.82, 0.92, 1.00, 1.0)
        _Heat("Normalized Thermal Heat (0..1)", Range(0.0, 1.0)) = 1.0
        _HeatIntensity("Peak Thermal Emission Intensity", Range(0.0, 3.5)) = 1.85
        _GlintFloor("Permanent Glint Floor Intensity", Range(0.02, 0.85)) = 0.28
        _FlashIntensity("Flash Burst Intensity", Range(0.0, 4.0)) = 0.0
        _FlashMode("Pass Mode (0=CraterAndGlass, 1=FlashStar)", Range(0.0, 1.0)) = 0.0
        _SpecPower("Glass Facet Specular Power", Range(4.0, 64.0)) = 24.0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2.0
        _ZWrite("ZWrite", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull [_Cull]

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
                half4 _CraterRimColor;
                half4 _HotCoreColor;
                half4 _WarmAmberColor;
                half4 _DeepEmberColor;
                half4 _PermanentGlintColor;
                half _Heat;
                half _HeatIntensity;
                half _GlintFloor;
                half _FlashIntensity;
                half _FlashMode;
                half _SpecPower;
                half _SrcBlend;
                half _DstBlend;
                half _Cull;
                half _ZWrite;
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

                // Mode 1: Faceted impact flash burst / ballistic ejecta shard
                if (_FlashMode > 0.5h)
                {
                    float2 centered = input.uv * 2.0 - 1.0;
                    // 4-point star + faceted core (zero circular dot pattern)
                    float starA = abs(centered.x) * 2.4 + abs(centered.y) * 0.45;
                    float starB = abs(centered.x) * 0.45 + abs(centered.y) * 2.4;
                    float diag = (abs(centered.x) + abs(centered.y)) * 1.15;
                    float starMask = max(saturate(1.0 - starA), max(saturate(1.0 - starB), saturate(1.0 - diag) * 0.85));
                    starMask = pow(starMask, 1.5);

                    half flashStrength = max(_FlashIntensity, 0.0h) * (half)starMask * input.vertexColor.a;
                    half3 flashRgb = lerp(_WarmAmberColor.rgb, _HotCoreColor.rgb, saturate((half)starMask * 1.2h))
                        * input.vertexColor.rgb * flashStrength;
                    half flashAlpha = saturate((half)starMask * input.vertexColor.a * saturate(_FlashIntensity));
                    return half4(max(flashRgb, 0.0h), flashAlpha);
                }

                // Mode 0: Crater regolith & cooling impact glass melt
                float normLenSq = dot(input.normalWS, input.normalWS);
                float3 normalWS = normLenSq > 1e-6 ? input.normalWS * rsqrt(normLenSq) : float3(0.0, 1.0, 0.0);
                // Ensure upward-hemisphere lighting normal for faceted low-poly crater rim
                float3 shadingNormal = normalize(float3(normalWS.x, abs(normalWS.y) + 0.35, normalWS.z));

                float3 viewRaw = GetWorldSpaceViewDir(input.positionWS);
                float viewLenSq = dot(viewRaw, viewRaw);
                float3 viewDir = viewLenSq > 1e-6 ? viewRaw * rsqrt(viewLenSq) : float3(0.0, 0.0, 1.0);

                half ao = saturate(input.vertexColor.r);
                half edgeWear = saturate(input.vertexColor.g);
                half cavity = saturate(input.vertexColor.b);
                half glassMask = saturate(input.vertexColor.a);

                // Base albedo: blend between fresh overturned regolith rim and dark vitreous impact glass
                half3 regolithAlbedo = _CraterRimColor.rgb * (0.82h + 0.32h * edgeWear);
                half3 glassAlbedo = _BaseColor.rgb * (0.75h + 0.60h * edgeWear);
                half3 albedo = lerp(regolithAlbedo, glassAlbedo, glassMask) * lerp(0.62h, 1.0h, ao * cavity);

                // Trilight ambient fallback + lunar earthshine floor
                half upWeight = saturate((half)shadingNormal.y);
                half3 trilightAmbient = lerp(unity_AmbientEquator.rgb, unity_AmbientSky.rgb, upWeight);
                half3 ambient = max(max(SampleSH(shadingNormal), trilightAmbient), half3(0.18h, 0.21h, 0.28h));

                // Main directional light (Day C sun or Night A earthlight) with wrapped regolith scatter
                Light mainLight = GetMainLight();
                half wrapNdotL = saturate((half)dot(shadingNormal, mainLight.direction) * 0.65h + 0.35h);
                half3 directLight = mainLight.color * wrapNdotL;

                #if defined(_ADDITIONAL_LIGHTS)
                uint addCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < addCount; ++li)
                {
                    Light addLight = GetAdditionalLight(li, input.positionWS);
                    half addNdotL = saturate((half)dot(shadingNormal, addLight.direction) * 0.65h + 0.35h);
                    directLight += addLight.color * (addNdotL * addLight.distanceAttenuation * 0.65h);
                }
                #endif

                half3 diffuse = albedo * (ambient + directLight);

                // Vitreous specular reflection + permanent cooled glint floor on impact glass facets
                float3 halfDir = normalize(mainLight.direction + viewDir);
                half ndoth = saturate((half)dot(shadingNormal, halfDir));
                half ndotv = saturate((half)dot(shadingNormal, viewDir));
                half fresnel = pow(1.0h - ndotv, 2.2h);
                half spec = pow(ndoth, max(_SpecPower, 4.0h)) * glassMask * (0.45h + 0.65h * edgeWear);
                half3 specular = mainLight.color * (spec * 0.85h);

                // Permanent glint floor: even after 10 minutes (_Heat = 0), the glass facets catch ambient/starlight
                // with a crisp crystalline glint so the crater remains discoverable and collectable forever.
                half facetGlintMask = glassMask * saturate(0.35h + 0.85h * edgeWear * edgeWear + 0.45h * fresnel);
                half3 permanentGlint = _PermanentGlintColor.rgb * (max(_GlintFloor, 0.0h) * facetGlintMask * 1.45h);

                // Thermal cool-down curve: _Heat in [0..1]
                // Radial concentration: center of melt pool (higher glassMask & cavity) stays hotter slightly longer
                half localHeat = saturate(_Heat * (0.65h + 0.50h * glassMask));
                half3 thermalColor;
                if (localHeat > 0.55h)
                {
                    half tHot = saturate((localHeat - 0.55h) / 0.45h);
                    thermalColor = lerp(_WarmAmberColor.rgb, _HotCoreColor.rgb, tHot);
                }
                else
                {
                    half tWarm = saturate(localHeat / 0.55h);
                    thermalColor = lerp(_DeepEmberColor.rgb, _WarmAmberColor.rgb, tWarm);
                }

                half thermalStrength = localHeat * max(_HeatIntensity, 0.0h) * glassMask;
                half3 thermalEmission = thermalColor * thermalStrength;

                // Combine thermal glow with the permanent glint floor (smooth max so floor is always respected)
                half3 totalEmission = max(thermalEmission, permanentGlint);
                half3 finalColor = diffuse + specular + totalEmission;

                if (any(isnan(finalColor)) || any(isinf(finalColor)))
                {
                    finalColor = albedo;
                }

                return half4(max(finalColor, 0.0h), 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
