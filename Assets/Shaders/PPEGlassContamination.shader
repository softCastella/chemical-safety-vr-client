Shader "Tyche/PPE/Glass Contamination"
{
    Properties
    {
        _GlassColor ("Glass Color", Color) = (0.32, 0.62, 0.74, 0.28)
        _FilmColor ("Dirty Film", Color) = (0.62, 0.46, 0.18, 1)
        _FilmStrength ("Dirty Film Strength", Range(0, 1)) = 0.22
        _StainMask ("Stain Mask", 2D) = "black" {}
        _StainColor ("Stain Color", Color) = (0.78, 0.55, 0.18, 1)
        _StainDarkColor ("Stain Rim Color", Color) = (0.36, 0.2, 0.06, 1)
        _StainStrength ("Stain Strength", Range(0, 1)) = 1
        _StainScale ("Object Stain Scale", Range(0.5, 40)) = 7
        _StainContrast ("Stain Contrast", Range(0.4, 2.5)) = 1.15
        _StainOpacity ("Stain Opacity", Range(0, 1)) = 0.82
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
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual

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
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 normalOS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_StainMask);
            SAMPLER(sampler_StainMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _StainMask_ST;
                half4 _GlassColor;
                half4 _FilmColor;
                half4 _StainColor;
                half4 _StainDarkColor;
                half _FilmStrength;
                half _StainStrength;
                half _StainScale;
                half _StainContrast;
                half _StainOpacity;
            CBUFFER_END

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_StainMask, sampler_StainMask, uv).r;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _StainMask);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 absNormal = abs(input.normalOS) + 1e-4;
                float3 weights = absNormal / (absNormal.x + absNormal.y + absNormal.z);
                float scale = max(_StainScale, 0.01);
                float2 uvXY = input.positionOS.xy * scale + 0.5;
                float2 uvXZ = input.positionOS.xz * scale + 0.5;
                float2 uvZY = input.positionOS.zy * scale + 0.5;

                half objectMask =
                    SampleMask(uvZY) * weights.x +
                    SampleMask(uvXZ) * weights.y +
                    SampleMask(uvXY) * weights.z;
                half uvMask = SampleMask(input.uv);
                half combinedMask = max(objectMask, uvMask * 0.85h);

                half stainAmount = saturate(pow(saturate(combinedMask), _StainContrast)) * _StainStrength;
                half rimMix = smoothstep(0.45h, 0.88h, combinedMask);
                half3 stainRgb = lerp(_StainColor.rgb, _StainDarkColor.rgb, rimMix);

                half filmAmount = _FilmStrength * (0.55h + 0.45h * Hash21(input.positionOS.xy * 18.0));
                half3 glassRgb = lerp(_GlassColor.rgb, _FilmColor.rgb, filmAmount);
                half glassAlpha = lerp(_GlassColor.a, saturate(_GlassColor.a + 0.12h), filmAmount);

                half3 color = lerp(glassRgb, stainRgb, stainAmount);
                half alpha = lerp(glassAlpha, saturate(glassAlpha + stainAmount * _StainOpacity), stainAmount);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
