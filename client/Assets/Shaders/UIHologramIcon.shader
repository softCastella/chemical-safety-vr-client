Shader "3D UI Test/UI/Hologram Icon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.30, 0.90, 1.00, 1.00)
        _BaseOpacity ("Base Opacity", Range(0, 1)) = 0.72
        [HDR] _GlowColor ("Glow Color", Color) = (0.10, 0.80, 1.00, 1.00)
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.65
        _ScanlineDensity ("Scanline Density", Range(1, 200)) = 70
        _ScanlineSpeed ("Scanline Speed", Range(-5, 5)) = 0.35
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.14
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1.2
        _PulseStrength ("Pulse Strength", Range(0, 0.5)) = 0.06

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "HologramUI"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 positionOS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _TextureSampleAdd;
            half4 _Color;
            half4 _GlowColor;
            float4 _ClipRect;
            half _BaseOpacity;
            half _GlowStrength;
            half _ScanlineDensity;
            half _ScanlineSpeed;
            half _ScanlineStrength;
            half _PulseSpeed;
            half _PulseStrength;

            half GetUIRectClipping(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside.x * inside.y;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionOS = input.positionOS;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) + _TextureSampleAdd;
                half scan = 0.5h + 0.5h * sin((input.uv.y * _ScanlineDensity + _Time.y * _ScanlineSpeed) * 6.2831853h);
                half scanBoost = lerp(1.0h - _ScanlineStrength, 1.0h + _ScanlineStrength, scan);
                half pulse = 1.0h + sin(_Time.y * _PulseSpeed) * _PulseStrength;

                half3 baseColor = sprite.rgb * input.color.rgb;
                half luminance = dot(sprite.rgb, half3(0.299h, 0.587h, 0.114h));
                half3 glow = _GlowColor.rgb * luminance * _GlowStrength;
                half alpha = sprite.a * input.color.a * _BaseOpacity;

                #ifdef UNITY_UI_CLIP_RECT
                    alpha *= GetUIRectClipping(input.positionOS.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(alpha - 0.001h);
                #endif

                return half4((baseColor + glow) * scanBoost * pulse, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
