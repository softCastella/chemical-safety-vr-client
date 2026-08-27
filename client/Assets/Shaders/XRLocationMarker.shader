Shader "3D UI Test/XR/Location Marker"
{
    Properties
    {
        [HDR] _Color ("Ring Color", Color) = (0.05, 1.5, 2.5, 1)
        [Enum(Ring, 0, Orb, 1)] _ShapeMode ("Shape", Float) = 0
        _Opacity ("Opacity", Range(0, 1)) = 1
        _RingRadius ("Ring / Orb Radius", Range(0.02, 0.5)) = 0.34
        _RingWidth ("Ring Width", Range(0.005, 0.45)) = 0.075
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.1)) = 0.018
        _GlowWidth ("Glow Width", Range(0.001, 0.5)) = 0.16
        _GlowStrength ("Glow Strength", Range(0, 5)) = 1.2
        _OrbCoreStrength ("Orb Core Strength", Range(0, 8)) = 3
        _ScalePulseStrength ("Scale Pulse Strength", Range(0, 1)) = 1
        _OpacityPulseStrength ("Opacity Pulse Strength", Range(0, 1)) = 1
        [Toggle] _UniformRingAlpha ("Uniform Ring Alpha", Float) = 0
        [Toggle] _ShimmerAffectsAlpha ("Shimmer Affects Alpha", Float) = 1
        _ShimmerStrength ("Shimmer Strength", Range(0, 1)) = 0
        _ShimmerSpeed ("Shimmer Speed", Range(0, 10)) = 2
        [Toggle] _ForwardIndicator ("Forward Indicator", Float) = 0
        _ForwardIndicatorStrength ("Forward Indicator Strength", Range(0, 5)) = 2
        _ForwardIndicatorLength ("Forward Indicator Length", Range(0.1, 0.8)) = 0.48
        _ForwardIndicatorWidth ("Forward Indicator Width", Range(0.02, 0.5)) = 0.26
        _ForwardIndicatorLineWidth ("Forward Indicator Line Width", Range(0.002, 0.08)) = 0.028
        [PerRendererData] _PulseOpacity ("Pulse Opacity", Range(0, 1)) = 1
        [PerRendererData] _PulseScale ("Pulse Scale", Range(0.1, 3)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "LocationMarker"
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
                half4 _Color;
                half _ShapeMode;
                half _Opacity;
                half _RingRadius;
                half _RingWidth;
                half _EdgeSoftness;
                half _GlowWidth;
                half _GlowStrength;
                half _OrbCoreStrength;
                half _ScalePulseStrength;
                half _OpacityPulseStrength;
                half _UniformRingAlpha;
                half _ShimmerAffectsAlpha;
                half _ShimmerStrength;
                half _ShimmerSpeed;
                half _ForwardIndicator;
                half _ForwardIndicatorStrength;
                half _ForwardIndicatorLength;
                half _ForwardIndicatorWidth;
                half _ForwardIndicatorLineWidth;
                half _PulseOpacity;
                half _PulseScale;
            CBUFFER_END

            half SegmentMask(half2 sampleUv, half2 segmentStart, half2 segmentEnd, half lineWidth, half softness)
            {
                half2 segment = segmentEnd - segmentStart;
                half segmentLengthSquared = max(dot(segment, segment), 0.0001h);
                half t = saturate(dot(sampleUv - segmentStart, segment) / segmentLengthSquared);
                half distanceToSegment = length(sampleUv - (segmentStart + segment * t));
                return 1.0h - smoothstep(lineWidth, lineWidth + softness, distanceToSegment);
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
                half effectivePulseScale = lerp(1.0h, _PulseScale, _ScalePulseStrength);
                half2 centeredUv = (input.uv - half2(0.5h, 0.5h)) / max(effectivePulseScale, 0.001h);
                half distanceFromCenter = length(centeredUv);
                half ringDistance = abs(distanceFromCenter - _RingRadius);
                half ring = 1.0h - smoothstep(_RingWidth, _RingWidth + _EdgeSoftness, ringDistance);
                half ringGlow = 1.0h - smoothstep(_RingWidth, _RingWidth + _GlowWidth, ringDistance);

                half orbMask = 1.0h - smoothstep(_RingRadius, _RingRadius + _EdgeSoftness, distanceFromCenter);
                half normalizedRadius = saturate(distanceFromCenter / max(_RingRadius, 0.001h));
                half sphereDepth = sqrt(saturate(1.0h - normalizedRadius * normalizedRadius));
                half orbCore = pow(sphereDepth, 6.0h) * _OrbCoreStrength;
                half orbGlow = 1.0h - smoothstep(_RingRadius, _RingRadius + _GlowWidth, distanceFromCenter);

                half arrowLength = max(_ForwardIndicatorLength, 0.001h);
                half arrowHalfWidth = _ForwardIndicatorWidth * 0.5h;
                half lineWidth = _ForwardIndicatorLineWidth;
                half arrowTipY = -arrowLength * 0.5h;
                half arrowTailY = arrowLength * 0.35h;
                half arrowHeadY = -arrowLength * 0.14h;
                half2 arrowTip = half2(0.0h, arrowTipY);
                half arrow = max(
                    SegmentMask(centeredUv, half2(0.0h, arrowTailY), arrowTip, lineWidth, _EdgeSoftness),
                    max(
                        SegmentMask(centeredUv, half2(-arrowHalfWidth, arrowHeadY), arrowTip, lineWidth, _EdgeSoftness),
                        SegmentMask(centeredUv, half2(arrowHalfWidth, arrowHeadY), arrowTip, lineWidth, _EdgeSoftness)));
                arrow *= step(0.5h, _ForwardIndicator);

                half isOrb = step(0.5h, _ShapeMode);
                half shape = max(lerp(ring, orbMask * (0.45h + sphereDepth * 0.55h) + orbCore, isOrb), arrow);
                half glow = max(lerp(ringGlow, orbGlow, isOrb), arrow * _ForwardIndicatorStrength);
                half shimmerWave = 0.5h + 0.5h * sin(_Time.y * _ShimmerSpeed * 6.2831853h);
                half shimmer = lerp(1.0h, shimmerWave, _ShimmerStrength);
                half uniformRing = 1.0h - smoothstep(_RingWidth, _RingWidth + _EdgeSoftness, ringDistance);
                half regularAlpha = saturate(shape + glow * _GlowStrength);
                half alphaShape = max(lerp(regularAlpha, uniformRing, _UniformRingAlpha * (1.0h - isOrb)), arrow);
                half effectivePulseOpacity = lerp(1.0h, _PulseOpacity, _OpacityPulseStrength);
                half alphaShimmer = lerp(1.0h, shimmer, _ShimmerAffectsAlpha);
                half alpha = alphaShape * _Opacity * effectivePulseOpacity * alphaShimmer;
                clip(alpha - 0.001h);
                half colorShape = max(lerp(shape + glow * _GlowStrength, uniformRing, _UniformRingAlpha * (1.0h - isOrb)), arrow * _ForwardIndicatorStrength);
                return half4(_Color.rgb * colorShape * shimmer, alpha);
            }
            ENDHLSL
        }
    }
}
