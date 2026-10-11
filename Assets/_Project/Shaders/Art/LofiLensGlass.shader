// 07's lens glass (docs/design/cutscenes.md O10, O11): a thin clear ring in front of the bezel that shows only what
// it reflects. Where the view, mirrored in the glass, points at Earth (World's _MoonEarthDirection), a soft highlight
// in Earth's cool colour, a few times Earth's own size because a curved ring this small blurs it; elsewhere a faint
// sheen on the grazing edge. So as the camera leaves the lens and turns back to the eye, the ring catches Earth, and
// on the lens shot Earth sits small in 07's eye. Additive and depth-tested, it never hides the amber lens.
Shader "MoonProject/Art/LofiLensGlass"
{
    Properties
    {
        [HDR] _Reflection("Earth's Reflection (linear)", Color) = (0.42, 0.66, 0.82, 1)
        _Spread("Reflection Size (x Earth's radius)", Range(1, 6)) = 2.5
        [HDR] _Sheen("Edge Sheen (linear)", Color) = (0.05, 0.07, 0.1, 1)
        _SheenPower("Edge Sheen Falloff", Range(1, 8)) = 4
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
            Name "LensGlass"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Reflection;
                half _Spread;
                half4 _Sheen;
                half _SheenPower;
            CBUFFER_END

            // World's sky (SkyShaderGlobals): xyz the unit direction to Earth, w its angular radius (radians).
            float4 _MoonEarthDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normal = normalize(input.normalWS);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 mirrored = reflect(-view, normal);
                float size = _MoonEarthDirection.w * _Spread;
                half earth = smoothstep(cos(size), cos(size * 0.35), dot(mirrored, _MoonEarthDirection.xyz));
                half edge = PositivePow(1.0 - saturate(dot(normal, view)), _SheenPower);
                return half4(_Reflection.rgb * earth + _Sheen.rgb * edge, 0.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
