Shader "Project/Chemical Mixer Interior"
{
    Properties
    {
        _BaseColor ("Steel Color", Color) = (0.42, 0.48, 0.5, 1)
        _DarkColor ("Recess Color", Color) = (0.08, 0.11, 0.12, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0.92
        _Smoothness ("Smoothness", Range(0, 1)) = 0.62
        _BrushScale ("Brush Scale", Float) = 180
        _BrushStrength ("Brush Strength", Range(0, 0.3)) = 0.08
        _SeamStrength ("Panel Seam Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DarkColor;
                half _Metallic;
                half _Smoothness;
                float _BrushScale;
                half _BrushStrength;
                half _SeamStrength;
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
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half faceSign = IS_FRONT_VFACE(face, 1.0h, -1.0h);
                half3 normalWS = normalize(input.normalWS * faceSign);
                half3 viewDir = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                float brushA = sin((input.uv.y + input.positionWS.y * 0.07) * _BrushScale);
                float brushB = sin((input.uv.y * 0.37 + input.positionWS.x * 0.03) * _BrushScale * 2.17);
                half brush = (brushA * 0.65 + brushB * 0.35) * _BrushStrength;

                half verticalSeam = 1.0h - smoothstep(0.0h, 0.018h, abs(frac(input.uv.x * 12.0h) - 0.5h));
                half ringSeam = 1.0h - smoothstep(0.0h, 0.022h, abs(frac(input.uv.y * 7.0h) - 0.5h));
                half seam = saturate(max(verticalSeam, ringSeam) * _SeamStrength);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half3 halfDir = SafeNormalize(mainLight.direction + viewDir);
                half specularPower = lerp(24.0h, 180.0h, _Smoothness);
                half specular = pow(saturate(dot(normalWS, halfDir)), specularPower) * lerp(0.25h, 1.6h, _Metallic);
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDir)), 3.0h);

                half3 steel = lerp(_DarkColor.rgb, _BaseColor.rgb, 0.78h + brush);
                steel *= 1.0h - seam * 0.55h;
                half3 ambient = SampleSH(normalWS) * steel;
                half3 direct = steel * mainLight.color * (0.18h + ndl * mainLight.shadowAttenuation);
                half3 highlights = mainLight.color * specular * mainLight.shadowAttenuation;
                half3 rim = lerp(steel, half3(0.72h, 0.84h, 0.88h), _Metallic) * fresnel * 0.45h;
                return half4(ambient + direct + highlights + rim, 1.0h);
            }
            ENDHLSL
        }
    }
}
