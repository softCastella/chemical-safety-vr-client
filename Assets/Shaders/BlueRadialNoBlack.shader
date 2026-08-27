Shader "Project/Blue Radial No Black"
{
    Properties
    {
        [HDR] _CenterColor("Center Color", Color) = (0.05, 0.55, 1.0, 1.0)
        [HDR] _EdgeColor("Edge Color", Color) = (0.02, 0.16, 0.55, 1.0)
        _Radius("Gradient Radius", Range(0.1, 1.5)) = 0.72
        _Softness("Gradient Softness", Range(0.1, 4.0)) = 1.35
        _Brightness("Brightness", Range(0.1, 3.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CenterColor;
                half4 _EdgeColor;
                float _Radius;
                float _Softness;
                float _Brightness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float distanceFromCenter = length((input.uv - 0.5) * 2.0);
                float gradient = saturate(distanceFromCenter / max(_Radius, 0.0001));
                gradient = pow(gradient, _Softness);

                half4 color = lerp(_CenterColor, _EdgeColor, gradient);
                color.rgb *= _Brightness;
                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }
    }
}
