Shader "Tyche/PPE/Hazmat Arm Tear Neutral Ramp"
{
    Properties
    {
        _BaseMap ("Base Color", 2D) = "white" {}
        _BaseColor ("Base Tint", Color) = (1, 1, 1, 1)
        [Normal] _NormalMap ("Tear Normal", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.65
        _ShadowTint ("Ramp Shadow Tint", Color) = (0.55, 0.43, 0.20, 1)
        _LitTint ("Ramp Lit Tint", Color) = (1, 1, 1, 1)
        _RampStart ("Ramp Start", Range(0, 1)) = 0.18
        _RampEnd ("Ramp End", Range(0, 1)) = 0.78
        _ShadowFloor ("Minimum Form Light", Range(0, 1)) = 0.20
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "NeutralRampForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 tangentWS : TEXCOORD2;
                half tangentSign : TEXCOORD3;
                float2 uv : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NormalMap_ST;
                half4 _BaseColor;
                half _NormalStrength;
                half4 _ShadowTint;
                half4 _LitTint;
                half _RampStart;
                half _RampEnd;
                half _ShadowFloor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.tangentSign = input.tangentOS.w * GetOddNegativeScale();
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 baseUv = TRANSFORM_TEX(input.uv, _BaseMap);
                float2 normalUv = TRANSFORM_TEX(input.uv, _NormalMap);
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUv), _NormalStrength);
                half3 normalWS = normalize(input.normalWS);
                half3 tangentWS = normalize(input.tangentWS);
                half3 bitangentWS = cross(normalWS, tangentWS) * input.tangentSign;
                normalWS = normalize(mul(normalTS, half3x3(tangentWS, bitangentWS, normalWS)));

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half shapedLight = lerp(_ShadowFloor, 1.0h, ndl * mainLight.shadowAttenuation);
                half ramp = smoothstep(_RampStart, max(_RampStart + 0.001h, _RampEnd), shapedLight);
                half3 rampTint = lerp(_ShadowTint.rgb, _LitTint.rgb, ramp);
                half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUv).rgb * _BaseColor.rgb;

                // This intentionally excludes SampleSH, reflection probes, and light color.
                // The suit keeps neutral authored color even inside the blue PPE room.
                return half4(baseColor * rampTint, 1.0h);
            }
            ENDHLSL
        }
    }
}
