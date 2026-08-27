Shader "3D UI Test/XR/Hologram Ray"
{
    Properties
    {
        [HDR] _Color ("Hologram Color", Color) = (0.05, 2.5, 3.5, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.9
        _Brightness ("Brightness", Range(0, 5)) = 1.6
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.18
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1.4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "HologramRay"
            Blend SrcAlpha One
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
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Opacity;
                half _Brightness;
                half _PulseStrength;
                half _PulseSpeed;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half pulse = 1.0h + sin((_Time.y * _PulseSpeed + input.uv.x * 0.35h) * 6.2831853h) * _PulseStrength;
                half alpha = input.color.a * _Color.a * _Opacity;
                return half4(input.color.rgb * _Color.rgb * _Brightness * pulse, alpha);
            }
            ENDHLSL
        }
    }
}
