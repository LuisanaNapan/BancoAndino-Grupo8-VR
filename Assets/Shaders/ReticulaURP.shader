// Adaptation for URP of Google's Apache-2.0 CardboardReticlePointer shader.
// Original: Copyright 2023 Google LLC.
// License: https://www.apache.org/licenses/LICENSE-2.0
Shader "Semana10/ReticulaURP"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _InnerDiameter ("Inner diameter", Float) = 1.5
        _OuterDiameter ("Outer diameter", Float) = 2.0
        _DistanceInMeters ("Distance", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha, OneMinusDstAlpha One
            Cull Back
            ZWrite Off
            ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _InnerDiameter;
                float _OuterDiameter;
                float _DistanceInMeters;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float scale = lerp(_OuterDiameter, _InnerDiameter, input.positionOS.z);
                float3 positionOS = float3(input.positionOS.xy * scale, _DistanceInMeters);
                output.positionCS = TransformObjectToHClip(positionOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return _Color;
            }
            ENDHLSL
        }
    }
}
