// Water surface for the Kineret, drawn as one flat quad sitting just above the lake.
//
// The shoreline is not in the mesh. The quad covers the lake's whole bounding box and the
// terrain, which writes depth, hides every part of it that sits below ground - so the edge of the
// water is the terrain's own silhouette and is exact at any distance. A baked shore map supplies
// distance-from-shore, used for the shallow tint and for fading the last few metres out so the
// waterline is soft rather than a hard cut.
Shader "Kineret/LakeWater"
{
    Properties
    {
        _DeepColor("Deep Colour", Color) = (0.015, 0.105, 0.145, 1)
        _ShallowColor("Shallow Colour", Color) = (0.07, 0.30, 0.33, 1)

        _ShoreMap("Shore Map (R = distance, G = mask)", 2D) = "black" {}
        // xMin, zMin, xMax, zMax of the shore map in world space.
        _WorldRect("World Rect", Vector) = (0, 0, 1, 1)
        _MaxShoreDistance("Shore Map Range (m)", Float) = 600

        _ShallowDistance("Shallow Band (m)", Float) = 250
        _EdgeFade("Shore Fade (m)", Float) = 40
        _Opacity("Opacity", Range(0, 1)) = 1

        _Smoothness("Smoothness", Range(0, 1)) = 0.92
        _SpecularStrength("Sun Glint", Float) = 1
        _SkyReflection("Sky Reflection", Range(0, 1)) = 0.35

        _WaveStrength("Wave Strength", Range(0, 1)) = 0.25
        _WaveScale("Wave Scale (m)", Float) = 120
        _WaveSpeed("Wave Speed", Float) = 0.6
        _WaveFadeDistance("Wave Fade Distance (m)", Float) = 4000
        _WaveContrast("Wave Contrast", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "LakeWater"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_ShoreMap);
            SAMPLER(sampler_ShoreMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _ShoreMap_ST;
                float4 _WorldRect;
                float  _MaxShoreDistance;
                float  _ShallowDistance;
                float  _EdgeFade;
                float  _Opacity;
                float  _Smoothness;
                float  _SpecularStrength;
                float  _SkyReflection;
                float  _WaveStrength;
                float  _WaveScale;
                float  _WaveSpeed;
                float  _WaveFadeDistance;
                float  _WaveContrast;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                return output;
            }

            // Three octaves of five travelling waves each. Only the surface normal moves - the quad
            // stays flat - which is all that reads at the altitude this is seen from.
            //
            // The directions are spread over a half turn and turned again on every octave, and each
            // wave gets its own frequency and speed. Reusing one pair of directions across the
            // octaves, as this did at first, makes every octave reinforce the same two axes and the
            // surface falls into regular stripes.
            float3 WaveNormal(float2 p, float time)
            {
                const int kOctaves = 3;
                const int kWaves = 5;

                float2 slope = 0;
                float total = 0;

                float amplitude = 1;
                float frequency = TWO_PI / max(1.0, _WaveScale);

                [unroll]
                for (int i = 0; i < kOctaves; i++)
                {
                    [unroll]
                    for (int k = 0; k < kWaves; k++)
                    {
                        // 36 degrees apart, with the whole fan rotated by an irrational-ish step
                        // each octave so no two components ever line up.
                        float angle = k * (PI / kWaves) + i * 0.4712389;
                        float2 dir = float2(cos(angle), sin(angle));

                        // Detune each wave so the set never repeats as one pattern.
                        float detune = 1.0 + 0.17 * k - 0.07 * i;
                        float speed = _WaveSpeed * (0.65 + 0.14 * k + 0.23 * i);
                        float travel = (k & 1) ? -1.0 : 1.0;

                        float contribution = amplitude * frequency * detune;
                        slope += dir * cos(dot(p, dir) * frequency * detune + travel * time * speed) * contribution;
                        total += contribution;
                    }

                    amplitude *= 0.5;
                    frequency *= 1.87;   // not a whole number, so the octaves do not stack up
                }

                // Normalised by the total it could reach, so Wave Strength means the same steepness
                // whatever the scale or the number of waves.
                slope = slope / max(total, 1e-5) * _WaveStrength * 2.7;

                return normalize(float3(-slope.x, 1.0, -slope.y));
            }

            half3 SampleSky(float3 reflectVector, float perceptualRoughness)
            {
                float mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
                half4 encoded = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip);
                return DecodeHDREnvironment(encoded, unity_SpecCube0_HDR);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 positionWS = input.positionWS;

                // The shore map PNG is stored north-up, the way the map looks, and LoadImage flips
                // it, so texel v = 0 is the image's last row - the south edge. Verified by reading
                // the loaded texture back, not assumed: v therefore counts up from zMin.
                float2 uv = float2(
                    (positionWS.x - _WorldRect.x) / max(1e-4, _WorldRect.z - _WorldRect.x),
                    (positionWS.z - _WorldRect.y) / max(1e-4, _WorldRect.w - _WorldRect.y));

                half2 shore = SAMPLE_TEXTURE2D(_ShoreMap, sampler_ShoreMap, uv).rg;
                float shoreDistance = shore.r * _MaxShoreDistance;

                // Outside the lake there is nothing to draw. The terrain usually covers these
                // fragments anyway; this takes care of the few that it does not.
                clip(shore.g - 0.25);

                float3 viewVector = GetCameraPositionWS() - positionWS;
                float viewDistance = length(viewVector);
                float3 viewDirectionWS = viewVector / max(1e-4, viewDistance);

                float3 waveNormal = WaveNormal(positionWS.xz, _Time.y);
                float3 flat = float3(0, 1, 0);

                // The highlight is pow(N.H, ~300): ripples smaller than a pixel make it break up
                // into sparkling dots, so the normal feeding it is flattened out over a short
                // distance. Shading is linear in N and does not alias like that, so it keeps its
                // own, far longer reach - otherwise the lake goes glassy a kilometre or two out.
                float glintFade = saturate(1.0 - viewDistance / max(1.0, _WaveFadeDistance));
                glintFade *= glintFade;
                float shadeFade = saturate(1.0 - viewDistance / max(1.0, _WaveFadeDistance * 6.0));

                float3 normalWS = normalize(lerp(flat, waveNormal, glintFade));
                float smoothness = _Smoothness;

                float depthBlend = saturate(shoreDistance / max(1.0, _ShallowDistance));
                half3 colour = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthBlend);

                Light mainLight = GetMainLight();

                // Waves shade the water itself and not only the highlight. Measured against a flat
                // surface, so the term vanishes when Wave Strength is zero and the other presets
                // are left exactly as they were.
                float shade = dot(waveNormal, mainLight.direction) - dot(flat, mainLight.direction);
                colour *= 1.0 + shade * _WaveContrast * shadeFade;

                // Sun glint.
                float3 halfVector = normalize(mainLight.direction + viewDirectionWS);
                float exponent = exp2(smoothness * 11.0) + 2.0;
                float specular = pow(saturate(dot(normalWS, halfVector)), exponent);
                colour += mainLight.color * specular * _SpecularStrength;

                // Sky, strongest at grazing angles.
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirectionWS)), 5.0);
                float3 reflectVector = reflect(-viewDirectionWS, normalWS);
                colour += SampleSky(reflectVector, 1.0 - smoothness) * fresnel * _SkyReflection;

                float alpha = saturate(shoreDistance / max(0.01, _EdgeFade)) * _Opacity;

                // In a fragment, positionCS is the screen position and its z is 0..1 depth, not the
                // clip z ComputeFogFactor wants. Across a quad kilometres wide a per-vertex fog
                // factor would be just as wrong, so view depth is taken per fragment instead.
                float viewDepth = dot(positionWS - GetCameraPositionWS(), -UNITY_MATRIX_V[2].xyz);
                colour = MixFog(colour, ComputeFogFactorZ0ToFar(viewDepth));

                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
