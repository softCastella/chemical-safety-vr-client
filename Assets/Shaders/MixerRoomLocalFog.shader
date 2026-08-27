Shader "3D UI Test/XR/Mixer Room Local Fog"
{
    Properties
    {
        [HDR] _FogColor ("Fog Color", Color) = (0.78, 0.9, 1.0, 0.55)
        _Opacity ("Opacity", Range(0, 1)) = 0.42
        _NoiseScale ("Noise Scale", Range(0.2, 12)) = 3.6
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.52
        _SoftEdge ("Soft Edge", Range(0.01, 0.5)) = 0.18
        _VerticalFade ("Vertical Fade", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "LocalFog"
            Tags { "LightMode"="UniversalForward" }
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                half _Opacity;
                half _NoiseScale;
                half _NoiseStrength;
                half _SoftEdge;
                half _VerticalFade;
            CBUFFER_END

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                half a = Hash21(i);
                half b = Hash21(i + float2(1.0, 0.0));
                half c = Hash21(i + float2(0.0, 1.0));
                half d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half edgeX = smoothstep(0.0h, _SoftEdge, input.uv.x) *
                    smoothstep(0.0h, _SoftEdge, 1.0h - input.uv.x);
                half edgeY = smoothstep(0.0h, _SoftEdge, input.uv.y) *
                    smoothstep(0.0h, _SoftEdge, 1.0h - input.uv.y);
                half vertical = lerp(1.0h, smoothstep(0.05h, 0.86h, input.uv.y), _VerticalFade);
                half noiseA = ValueNoise(input.uv * _NoiseScale + float2(_Time.y * 0.035, 0.0));
                half noiseB = ValueNoise(input.uv * (_NoiseScale * 1.9h) + float2(0.0, _Time.y * 0.025));
                half fogNoise = lerp(1.0h, saturate(noiseA * 0.72h + noiseB * 0.45h), _NoiseStrength);
                half alpha = _FogColor.a * _Opacity * edgeX * edgeY * vertical * fogNoise;

                return half4(_FogColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
