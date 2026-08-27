Shader "Tyche/PPE/Hazmat Contamination"
{
    Properties
    {
        _BaseMap ("Base Color Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ContaminationMask ("Chemical Stain Mask", 2D) = "black" {}
        _ContaminationDarkColor ("Dark Brown Stain", Color) = (0.16, 0.075, 0.018, 1)
        _ContaminationOliveColor ("Olive Stain", Color) = (0.22, 0.24, 0.045, 1)
        _ContaminationContrast ("Stain Contrast", Range(0.5, 2)) = 1.15
        _ContaminationStrength ("Contamination Strength", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 baseUv : TEXCOORD0;
                float2 contaminationUv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ContaminationMask);
            SAMPLER(sampler_ContaminationMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _ContaminationMask_ST;
                half4 _BaseColor;
                half4 _ContaminationDarkColor;
                half4 _ContaminationOliveColor;
                half _ContaminationContrast;
                half _ContaminationStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.baseUv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.contaminationUv = TRANSFORM_TEX(input.uv, _ContaminationMask);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUv) * _BaseColor;
                half rawMask = SAMPLE_TEXTURE2D(
                    _ContaminationMask,
                    sampler_ContaminationMask,
                    input.contaminationUv).r;
                half stainAmount = saturate(pow(saturate(rawMask), _ContaminationContrast)) *
                    _ContaminationStrength;
                half darkMix = smoothstep(0.55h, 0.9h, rawMask);
                half3 stainColor = lerp(
                    _ContaminationOliveColor.rgb,
                    _ContaminationDarkColor.rgb,
                    darkMix);

                return half4(lerp(baseColor.rgb, stainColor, stainAmount), baseColor.a);
            }
            ENDHLSL
        }
    }
}
