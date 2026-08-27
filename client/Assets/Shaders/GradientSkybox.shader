Shader "Skybox/Directional Gradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.844, 0.943, 1, 1)
        _BottomColor ("Bottom Color", Color) = (0.03, 0.16, 0.36, 1)
        _GradientPower ("Gradient Power", Range(0.1, 5)) = 1
        _Exposure ("Exposure", Range(0, 8)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half4 _TopColor;
            half4 _BottomColor;
            half _GradientPower;
            half _Exposure;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Map the entire sphere from bottom (-1) to top (+1). The
                // smooth curve avoids a visible seam around the horizon.
                half height = saturate(normalize(input.direction).y * 0.5h + 0.5h);
                half gradient = pow(smoothstep(0.0h, 1.0h, height), _GradientPower);
                half3 color = lerp(_BottomColor.rgb, _TopColor.rgb, gradient);
                return half4(color * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}
