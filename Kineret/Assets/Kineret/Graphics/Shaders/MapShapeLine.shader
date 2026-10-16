// Flat coloured line for the map shapes.
//
// URP's own Unlit shader has no _ZTest property, so it can only ever be hidden by the terrain.
// This one exposes the depth test, which lets a shape be drawn either as part of the world
// (ZTest LEqual, ridges in front block it) or as an overlay that is always readable
// (ZTest Always, drawn after everything else).
Shader "Kineret/MapShapeLine"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.224, 0.867, 0.125, 1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4   // LEqual
        [Enum(Off, 0, On, 1)] _ZWrite ("Depth write", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "IgnoreProjector" = "True" }

        Pass
        {
            Name "MapShapeLine"
            Tags { "LightMode" = "UniversalForward" }

            ZTest [_ZTest]
            ZWrite [_ZWrite]
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _ZTest;
                float _ZWrite;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_BaseColor.rgb, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
