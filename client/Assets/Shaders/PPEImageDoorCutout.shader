Shader "PPE/Image Door Cutout"
{
    Properties
    {
        _BaseMap("Door Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (0.72, 0.72, 0.72, 1)
        _Cutoff("Background Cutoff", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(color.a - _Cutoff);

                // Cut out the chamfered rectangular window while retaining its frame.
                float2 distanceFromCenter = abs(input.uv - float2(0.502, 0.498));
                float2 edgeDistance = float2(0.243, 0.39) - distanceFromCenter;
                bool withinBounds = edgeDistance.x > 0 && edgeDistance.y > 0;
                // Account for the portrait image aspect so the corner cuts are
                // physically 45 degrees rather than square-UV diagonals.
                bool withinChamfers = edgeDistance.x * 0.497 + edgeDistance.y > 0.021;
                if (withinBounds && withinChamfers)
                    discard;

                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
