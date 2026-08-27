Shader "3D UI Test/XR/Hand Form Unlit"
{
    Properties
    {
        _BaseMap ("Base Color Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.945, 0.843, 0.774, 1)
        _FormShading ("Form Shading", Range(0, 0.8)) = 0.32
        _EdgeDarkening ("Finger Edge Darkening", Range(0, 0.8)) = 0.38
        _EdgePower ("Edge Width", Range(0.5, 8)) = 3
        // Opaque luminance fade for short hand-model swaps. Avoids Transparent queue / extra RT cost.
        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "FormUnlit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _FormShading;
                half _EdgeDarkening;
                half _EdgePower;
                half _Fade;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = normalize(input.normalWS);
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half facing = saturate(dot(normalWS, viewDirectionWS));

                // View-relative form shading keeps the hand visible without scene lights while
                // restoring cylindrical definition around every finger.
                half form = lerp(1.0h - _FormShading, 1.0h, facing);
                half edge = pow(1.0h - facing, _EdgePower) * _EdgeDarkening;

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                color.rgb *= form * (1.0h - edge) * saturate(_Fade);
                return color;
            }
            ENDHLSL
        }
    }
}
