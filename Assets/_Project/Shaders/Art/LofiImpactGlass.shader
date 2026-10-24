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
                float3 positionOS : TEXCOORD3;
                half4 vertexColor : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 37.45);
                return frac(p.x * p.y);
            }

            float DecalNoise(float2 p)
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
                output.positionOS = input.positionOS.xyz;
                output.vertexColor = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Mode 1: Brief compact radial bloom flash / ballistic ejecta shard (no vertical light pillar)
                if (_FlashMode > 0.5h)
                {
                    float2 centered = input.uv * 2.0 - 1.0;
                    float r = length(centered);
                    float angle = atan2(centered.y, centered.x);
                    // Multi-facet starburst envelope without any tall vertical axis spike
                    float rays = 0.78 + 0.22 * cos(angle * 5.0 + 0.65) * sin(angle * 3.0 - 0.45);
                    float radial = saturate(1.0 - (r / max(rays, 0.52)));
                    float starMask = radial * radial * (3.0 - 2.0 * radial);

                    half flashStrength = max(_FlashIntensity, 0.0h) * (half)starMask * input.vertexColor.a;
                    half3 flashRgb = lerp(_WarmAmberColor.rgb, _HotCoreColor.rgb, saturate((half)starMask * 1.25h))
                        * input.vertexColor.rgb * flashStrength;
                    half flashAlpha = saturate((half)starMask * input.vertexColor.a * saturate(_FlashIntensity));
                    return half4(max(flashRgb, 0.0h), flashAlpha);
                }

                // Mode 0: Soft-rimmed terrain-decal crater & irregular cooling impact glass shards
                // uv.x = glassMask (0 = crater regolith decal, 1 = vitreous glass melt/shards)
                // uv.y = base terrain-decal feather alpha (1 inside crater & shards, fading to 0 on outer skirt)
                float normLenSq = dot(input.normalWS, input.normalWS);
                float3 normalWS = normLenSq > 1e-6 ? input.normalWS * rsqrt(normLenSq) : float3(0.0, 1.0, 0.0);
                float3 shadingNormal = normalize(float3(normalWS.x, normalWS.y + 0.20, normalWS.z));

                float3 viewRaw = GetWorldSpaceViewDir(input.positionWS);
                float viewLenSq = dot(viewRaw, viewRaw);
                float3 viewDir = viewLenSq > 1e-6 ? viewRaw * rsqrt(viewLenSq) : float3(0.0, 0.0, 1.0);

                half ao = saturate(input.vertexColor.r);
                half edgeWear = saturate(input.vertexColor.g);
                half cavity = saturate(input.vertexColor.b);
                half shardHeatVar = saturate(input.vertexColor.a);
                half glassMask = saturate((half)input.uv.x);
                float baseDecalAlpha = saturate(input.uv.y);

                // Irregular radial/angular ejecta noise on the outer terrain-decal skirt so it blends seamlessly into ground
                float2 localXZ = input.positionOS.xz;
                float localAngle = atan2(localXZ.y, localXZ.x);
                float localRad = length(localXZ);
                float ejectaNoise = DecalNoise(localXZ * 3.8 + float2(1.7, -2.3)) * 0.65
                    + DecalNoise(float2(localAngle * 2.4, localRad * 5.2)) * 0.35;
                float raysMod = 0.72 + 0.28 * cos(localAngle * 3.0 + 0.9) * sin(localAngle * 5.0 - 1.2);
                float featheredSkirt = smoothstep(0.06, 0.86, baseDecalAlpha * lerp(0.68, 1.18, ejectaNoise * raysMod));
                half outAlpha = (half)lerp(featheredSkirt, 1.0, glassMask);

                // Base albedo: match dark Mare basalt regolith on outer skirt, darkened inside the crater pit (ao*cavity),
                // and dark vitreous impactite on the central melt splatters & shards.
                half3 regolithAlbedo = _CraterRimColor.rgb * (0.76h + 0.34h * edgeWear + 0.12h * (half)ejectaNoise);
                half3 glassAlbedo = _BaseColor.rgb * (0.70h + 0.65h * edgeWear);
                half shadowPit = lerp(0.52h, 1.0h, ao * cavity);
                half3 albedo = lerp(regolithAlbedo, glassAlbedo, glassMask) * shadowPit;

                // Scene-faithful trilight ambient + directional light (no artificial floor that would make a grey disc)
                half upWeight = saturate((half)shadingNormal.y * 0.5h + 0.5h);
                half3 trilightAmbient = lerp(unity_AmbientGround.rgb, unity_AmbientSky.rgb, upWeight);
                half3 ambient = max(SampleSH(shadingNormal), trilightAmbient);

                Light mainLight = GetMainLight();
                half wrapNdotL = saturate((half)dot(shadingNormal, mainLight.direction) * 0.78h + 0.22h);
                half3 directLight = mainLight.color * wrapNdotL;

                #if defined(_ADDITIONAL_LIGHTS)
                uint addCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < addCount; ++li)
                {
                    Light addLight = GetAdditionalLight(li, input.positionWS);
                    half addNdotL = saturate((half)dot(shadingNormal, addLight.direction) * 0.75h + 0.25h);
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
                half facetGlintMask = glassMask * saturate(0.32h + 0.88h * edgeWear * edgeWear + 0.45h * fresnel);
                half3 permanentGlint = _PermanentGlintColor.rgb * (max(_GlintFloor, 0.0h) * facetGlintMask * 1.45h);

                // Thermal cool-down curve: _Heat in [0..1]
                // Per-shard heat variation (shardHeatVar) makes smaller outer droplets cool faster than thick central glass
                half localHeat = saturate(_Heat * (0.48h + 0.62h * shardHeatVar) * (0.60h + 0.50h * glassMask));
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

                // Subtle warm rim bounce inside the crater bowl while the impact glass is still hot
                half bowlWarmth = saturate(_Heat * (1.0h - glassMask) * (1.0h - ao) * 1.35h);
                half3 bowlBounce = _WarmAmberColor.rgb * (bowlWarmth * 0.26h);

                // Combine thermal glow with the permanent glint floor (smooth max so floor is always respected)
                half3 totalEmission = max(thermalEmission, permanentGlint) + bowlBounce;
                half3 finalColor = diffuse + specular + totalEmission;

                if (any(isnan(finalColor)) || any(isinf(finalColor)))
                {
                    finalColor = albedo;
                }

                return half4(max(finalColor, 0.0h), outAlpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
