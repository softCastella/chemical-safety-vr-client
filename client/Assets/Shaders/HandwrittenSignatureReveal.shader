Shader "Project/Handwritten Signature Reveal"
{
    Properties
    {
        _MainTex ("Signature", 2D) = "white" {}
        _InkColor ("Ink Color", Color) = (0.03, 0.07, 0.09, 1)
        _Reveal ("Reveal", Range(0, 1)) = 0
        _RevealSoftness ("Reveal Softness", Range(0.001, 0.1)) = 0.025
        _InkThreshold ("Ink Threshold", Range(0, 1)) = 0.72
        _InkSoftness ("Ink Softness", Range(0.001, 0.3)) = 0.18
        _UseTextureAlpha ("Use Texture Alpha", Range(0, 1)) = 0
        _InkExpansion ("Ink Expansion", Range(0, 0.03)) = 0
        _AlphaBoost ("Alpha Boost", Range(1, 4)) = 1
        _CropRect ("Crop Rect", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "SignatureReveal"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _InkColor;
                float4 _CropRect;
                float _RevealSoftness;
                float _InkThreshold;
                float _InkSoftness;
                float _UseTextureAlpha;
                float _InkExpansion;
                float _AlphaBoost;
            CBUFFER_END

            // Per-renderer MaterialPropertyBlock overrides require an instanced prop
            // when GPU instancing is enabled; otherwise _Reveal stays at the material default (0).
            UNITY_INSTANCING_BUFFER_START(HandwriteRevealProps)
                UNITY_DEFINE_INSTANCED_PROP(float, _Reveal)
            UNITY_INSTANCING_BUFFER_END(HandwriteRevealProps)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float reveal = UNITY_ACCESS_INSTANCED_PROP(HandwriteRevealProps, _Reveal);

                float2 croppedUv = lerp(_CropRect.xy, _CropRect.zw, input.uv);
                float2 expansionX = float2(_InkExpansion, 0.0);
                float2 expansionY = float2(0.0, _InkExpansion);
                half4 signatureCenter = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, croppedUv);
                half4 signatureLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, croppedUv - expansionX);
                half4 signatureRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, croppedUv + expansionX);
                half4 signatureDown = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, croppedUv - expansionY);
                half4 signatureUp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, croppedUv + expansionY);

                half centerInk = 1.0h - smoothstep(
                    _InkThreshold,
                    _InkThreshold + _InkSoftness,
                    dot(signatureCenter.rgb, half3(0.2126h, 0.7152h, 0.0722h)));
                half leftInk = 1.0h - smoothstep(
                    _InkThreshold,
                    _InkThreshold + _InkSoftness,
                    dot(signatureLeft.rgb, half3(0.2126h, 0.7152h, 0.0722h)));
                half rightInk = 1.0h - smoothstep(
                    _InkThreshold,
                    _InkThreshold + _InkSoftness,
                    dot(signatureRight.rgb, half3(0.2126h, 0.7152h, 0.0722h)));
                half downInk = 1.0h - smoothstep(
                    _InkThreshold,
                    _InkThreshold + _InkSoftness,
                    dot(signatureDown.rgb, half3(0.2126h, 0.7152h, 0.0722h)));
                half upInk = 1.0h - smoothstep(
                    _InkThreshold,
                    _InkThreshold + _InkSoftness,
                    dot(signatureUp.rgb, half3(0.2126h, 0.7152h, 0.0722h)));

                half sourceAlpha = centerInk * signatureCenter.a;
                sourceAlpha = max(sourceAlpha, leftInk * signatureLeft.a);
                sourceAlpha = max(sourceAlpha, rightInk * signatureRight.a);
                sourceAlpha = max(sourceAlpha, downInk * signatureDown.a);
                sourceAlpha = max(sourceAlpha, upInk * signatureUp.a);
                sourceAlpha = lerp(centerInk, sourceAlpha, saturate(_UseTextureAlpha));
                sourceAlpha = saturate(sourceAlpha * _AlphaBoost);

                float revealPath = input.uv.x
                    + sin(input.uv.y * 17.0) * 0.018
                    + sin(input.uv.y * 41.0) * 0.008;
                half revealAlpha = smoothstep(
                    revealPath - _RevealSoftness,
                    revealPath + _RevealSoftness,
                    reveal);

                half4 output = _InkColor;
                output.a *= sourceAlpha * revealAlpha;
                return output;
            }
            ENDHLSL
        }
    }
}
