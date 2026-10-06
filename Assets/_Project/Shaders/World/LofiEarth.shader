// Lofi Lunar Earth: a flat-shaded low-poly icosphere hanging in the sky. The mesh is a unit sphere in Earth's own
// frame; the vertex shader spins it, places it around the camera along _MoonEarthDirection and pins it just in
// front of the far plane, so it is always behind the terrain, never fogged and never clipped by distance.
// Lit side = palette colour (vertex colour, linear) with an emissive boost for soft bloom; the night side keeps a
// faint glow; a faceted fresnel rim gives the atmosphere. Parameters are _Moon* globals set from WorldSettings by
// MoonProject.World.SkyShaderGlobals.
Shader "MoonProject/World/LofiEarth"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-200"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Any distance inside the camera's clip range works: depth is overridden below.
            #define EARTH_DISTANCE 100.0
            #define FAR_PLANE_INSET 1e-5

            float4 _MoonEarthDirection;  // xyz unit direction to Earth's centre, w angular radius (radians)
            float4 _MoonEarthAxisX;      // xyz Earth frame X in world space, w tan(angular radius)
            float4 _MoonEarthAxisY;      // xyz Earth frame Y (spin axis)
            float4 _MoonEarthAxisZ;      // xyz Earth frame Z
            float4 _MoonEarthSun;        // xyz world direction toward the sun lighting Earth, w night brightness
            float4 _MoonEarthGlow;       // x lit-side emissive boost, y spin speed (radians per second)
            float4 _MoonEarthAtmosphere; // rgb rim colour (linear), a fresnel exponent

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
                float3 color : TEXCOORD2;
            };

            float3 Spin(float3 v, float s, float c)
            {
                return float3(c * v.x + s * v.z, v.y, -s * v.x + c * v.z);
            }

            float3 ToWorld(float3 v)
            {
                return v.x * _MoonEarthAxisX.xyz + v.y * _MoonEarthAxisY.xyz + v.z * _MoonEarthAxisZ.xyz;
            }

            Varyings Vert(Attributes input)
            {
                float s, c;
                sincos(_Time.y * _MoonEarthGlow.y, s, c);
                float3 offset = (_MoonEarthDirection.xyz + ToWorld(Spin(input.positionOS, s, c)) * _MoonEarthAxisX.w)
                    * EARTH_DISTANCE;

                Varyings output;
                output.positionCS = TransformWorldToHClip(_WorldSpaceCameraPos + offset);
                #if UNITY_REVERSED_Z
                output.positionCS.z = output.positionCS.w * FAR_PLANE_INSET;
                #else
                output.positionCS.z = output.positionCS.w * (1.0 - FAR_PLANE_INSET);
                #endif
                output.normalWS = ToWorld(Spin(input.normalOS, s, c));
                output.viewWS = -normalize(offset);
                output.color = input.color.rgb;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float lit = smoothstep(-0.12, 0.4, dot(normal, _MoonEarthSun.xyz));
                float3 color = input.color * lerp(_MoonEarthSun.w, 1.0, lit) * _MoonEarthGlow.x;
                float rim = pow(1.0 - saturate(dot(normal, input.viewWS)), _MoonEarthAtmosphere.a);
                color += _MoonEarthAtmosphere.rgb * rim * (0.3 + 0.7 * lit);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
