// Soft, unlit light for gameplay effects: sonar rings, site beacons, the tractor beam, the tether, scrap flashes,
// relic halos and dust. One shader, configured per material:
//   uv.x runs ALONG an effect (beam length, ring circumference), uv.y runs ACROSS it (beam width, ring band).
//   _AcrossIn / _AcrossOut fade uv.y in from 0 and out toward 1 (unequal widths give a wave a soft leading edge
//   and a longer tail), _LengthFade fades both ends of uv.x, bands scroll along uv.x,
//   _FresnelMix brightens silhouettes (halos, flashes), _CoreMix softens them away (light columns, beam cones),
//   _RadialMask turns a quad into a soft round sprite (dust, sparks, the home halo); _RadialPower sharpens its core
//   ((1 - r^2) ^ power: 2 is a soft ball, higher a bright core with a long faint tail and no visible rim).
// Output is premultiplied: SrcBlend One with DstBlend One is additive light, DstBlend OneMinusSrcAlpha is soft matter.
// _Intensity is the per-renderer brightness gameplay eases through a MaterialPropertyBlock; vertex colour multiplies
// colour and alpha (LineRenderer and particle colours).
Shader "MoonProject/Gameplay/SoftGlow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.37, 0.95, 1, 1)
        _Intensity ("Intensity", Float) = 1
        _AcrossIn ("Across Fade From uv.y 0", Range(0, 1)) = 0.5
        _AcrossOut ("Across Fade Toward uv.y 1", Range(0, 1)) = 0.5
        _LengthFade ("Along Fade (uv.x ends)", Range(0, 0.5)) = 0
        _BandCount ("Band Count (along)", Float) = 0
        _BandSpeed ("Band Speed", Float) = 0
        _BandStrength ("Band Strength", Range(0, 1)) = 0
        _FresnelMix ("Fresnel Mix", Range(0, 1)) = 0
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2
        _CoreMix ("Soft Core Mix", Range(0, 1)) = 0
        _CorePower ("Soft Core Power", Range(0.5, 8)) = 1.5
        _RadialMask ("Radial Mask", Range(0, 1)) = 0
        _RadialPower ("Radial Mask Power", Range(1, 8)) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
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
            Name "SoftGlow"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _AcrossIn;
                half _AcrossOut;
                half _LengthFade;
                half _BandCount;
                half _BandSpeed;
                half _BandStrength;
                half _FresnelMix;
                half _FresnelPower;
                half _CoreMix;
                half _CorePower;
                half _RadialMask;
                half _RadialPower;
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
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            // 0 at both edges of a 0..1 coordinate, 1 inside: fades in over fadeIn from 0 and out over fadeOut toward
            // 1, each with a smooth step; a width of 0 disables that side's fade.
            half EdgeMask(half coordinate, half fadeIn, half fadeOut)
            {
                half rise = saturate(coordinate / max(fadeIn, 1e-4));
                half fall = saturate((1.0 - coordinate) / max(fadeOut, 1e-4));
                rise = rise * rise * (3.0 - 2.0 * rise);
                fall = fall * fall * (3.0 - 2.0 * fall);
                return rise * fall;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half across = EdgeMask(input.uv.y, _AcrossIn, _AcrossOut);
                half along = EdgeMask(input.uv.x, _LengthFade, _LengthFade);

                half wave = 0.5 + 0.5 * sin((input.uv.x * _BandCount - _Time.y * _BandSpeed) * TWO_PI);
                half bands = 1.0 - _BandStrength * wave;

                half facing = saturate(abs(dot(normalize(input.normalWS), normalize(input.viewDirWS))));
                half rim = lerp(1.0, pow(1.0 - facing, _FresnelPower), _FresnelMix);
                half core = lerp(1.0, pow(facing, _CorePower), _CoreMix);

                float2 centred = input.uv * 2.0 - 1.0;
                half radial = saturate(1.0 - dot(centred, centred));
                half round = lerp(1.0, pow(radial, _RadialPower), _RadialMask);

                half alpha = saturate(across * along * bands * rim * core * round * input.color.a * _Color.a);
                half3 rgb = _Color.rgb * input.color.rgb * _Intensity * alpha;
                rgb = MixFogColor(rgb, half3(0.0, 0.0, 0.0), input.fogFactor);
                return half4(rgb, alpha * saturate(_Intensity));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
