// Three-stop gradient skybox for URP.
//
// A VR scene needs *some* background - the default grey makes it impossible to judge distance or
// motion. A gradient costs one full-screen pass with no texture, which matters on standalone headsets
// where a cubemap skybox eats memory bandwidth every frame.
Shader "XRDemo/Skybox Gradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.29, 0.42, 0.62, 1)
        _MiddleColor ("Middle Color", Color) = (0.68, 0.74, 0.80, 1)
        _BottomColor ("Bottom Color", Color) = (0.16, 0.17, 0.20, 1)
        _Direction ("Direction", Vector) = (0, 1, 0)
        _DitherStrength ("Dither Strength", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue" = "Background"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Skybox"
        }

        Pass
        {
            Name "Gradient"

            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex SkyboxVertex
            #pragma fragment SkyboxFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _MiddleColor;
                half4 _BottomColor;
                float4 _Direction;
                half _DitherStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 directionOS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // A gradient spread over the whole sky moves through a colour so slowly that 8-bit output
            // quantises it into visible bands - and in a headset those bands sit still while your head
            // moves, which makes them very obvious. A sub-quantum of noise breaks them up.
            half Dither(float2 positionSS)
            {
                half noise = frac(52.9829189 * frac(dot(positionSS, float2(0.06711056, 0.00583715))));
                return (noise - 0.5) * _DitherStrength / 255.0;
            }

            Varyings SkyboxVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionOS = input.uv;
                return output;
            }

            half4 SkyboxFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 direction = normalize(input.directionOS);

                // -1 at the far pole, 0 at the horizon, +1 at the near pole.
                float height = dot(direction, normalize(_Direction.xyz)) + Dither(input.positionCS.xy);

                // The three weights always sum to 1, so no stop can wash the others out.
                half bottom = saturate(-height);
                half top = saturate(height);
                half middle = 1.0 - abs(height);

                half3 color = _BottomColor.rgb * bottom
                    + _MiddleColor.rgb * middle
                    + _TopColor.rgb * top;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
