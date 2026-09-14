Shader "VolumetricCloud3_URP14"
{
	Properties
	{
        [ToggleKeyword(_NEW_SHADING)] _NewShading ("New Shading", int) = 1
	    [NoScaleOffset]_PerlinNormalMap ("Perlin Normal Map", 2D) = "white" {}

		[Header(Colors)]
			[HDR]_BaseColor ("Base Color", Color) = (0.56, 0.56, 0.56, 1)
			[HDR]_Shading ("Shading Color", Color) = (0, 0, 0, 1)
            [ModulatedKeyword(_DISABLE_DIRECT, 0)] _DirectContribution ("Direct Contribution", float ) = 1
                [ToggleAndRequires(_NORMALMAP, _DirectContribution, 0)] _NormalmapState ("Normalmap", int) = 1
                    [Requires(_DirectContribution, 0, _NormalmapState, 1)] _NormalsIntensity ("Shading Intensity", float ) = 1
                    [Requires(_DirectContribution, 0, _NormalmapState, 1)] _Normalized ("Normalized", float ) = 0.16
                    [Requires(_DirectContribution, 0, _NormalmapState, 1)] _SilverLining ("Silver Lining", float ) = 0.8
            
            [ModulatedKeyword(_DISABLE_INDIRECT, 0)] _IndirectContribution("Sky Contribution", float) = 1
                [Requires(_IndirectContribution, 0)] _IndirectOcclusion("Sky Occlusion", float) = 1
                [Requires(_IndirectContribution, 0)] _IndirectOcclusionNormalized("Sky Occlusion Normalized", float) = 0
                [Requires(_IndirectContribution, 0)] _SkyLightAttenuation ("Occlusion Tint", Color) = (0.5, 0.5, 0.5, 1)
                [ToggleAndRequires(_COMPLEXLIGHTING_MODE, _IndirectContribution, 0)] _ComplexLightingMode ("Advanced Lighting", int) = 1
                    [Requires(_IndirectContribution, 0, _ComplexLightingMode, 1)] _IndirectSaturation("Indirect Saturation", float) = 0.8
			
			[Space]
			
			_DistanceBlend ("Distance Blend", float ) = 0.3
			[ToggleKeyword(_DISTBLEND_POST)] _DistBlendPost ("Post Blend", int) = 0
            
            [Space]
            
			[ToggleKeyword(_RENDERSHADOWS)] _SSShadows ("Screen Space Shadows", int) = 1
				[Requires(_SSShadows)] _ShadowColor ("Shadow Color", Color) = (1.0,1.0,1.0,1.0)
				[ToggleAndRequires(_SSSHADOW_MAPBLEND, _SSShadows)] _BlendWithShadowmap ("Blend Out Of Shadowmap", int) = 0
				[ToggleAndRequires(_VOLUMETRIC_SSS, _SSShadows)] _VolumetricSSShadows ("Volumetric", int) = 0
            [ToggleKeyword(_RENDERSHADOWSONLY)] _RenderShadowsOnly ("Render Shadows Only", int) = 0
            [ToggleKeyword(_HQPOINTLIGHT)] _HQPointLight ("High Quality Point Light", int) = 1


        [Header(Shape)]
			_Coverage ("Coverage", float ) = 0
			[ToggleKeyword(_COVERAGEMAP)] _CoverageMapOn ("Coverage Map", int) = 0
				[Requires(_CoverageMapOn)] _CoverageMap ("Coverage Map", 2D) = "black" {}
				[Requires(_CoverageMapOn)] _CoverageLow ("Coverage Low", float) = -1
				[Requires(_CoverageMapOn)] _CoverageHigh ("Coverage High", float) = 1
			    [Requires(_CoverageMapOn)] _CoverageChannel ("Channel", vector) = (1,0,0,0)
			_CloudDensity ("Density", float) = 2
			_CloudLocalScale ("Local Scale", float) = 1
			_CloudBaseFlatness("Cloud Center Height", float) = 0.1
			_CloudFlatness("Cloud Flatness", float) = 1
			_LodBase("Lod Base", float) = -10
			_LodOffset("Lod Offset", float) = 5

		[Header(Animation)]
			_TimeMult ("Speed", float ) = 0.1
			_TimeMultSecondLayer ("Speed Second Layer", float ) = 4
			_WindDirection ("Wind Direction", vector) = (0,0,1,0)

		[Header(Dimensions)]
			_CloudTransform("Cloud Transform", vector) =  (100, 20, 0, 0)
			_Tiling ("Tiling", float ) = 1500
			[ToggleKeyword(_SPHERE_MAPPED)] _SphereMapped ("Spherical", int) = 0
				[Requires(_SphereMapped)] _CloudSpherePosition("Sphere Position", vector) =  (0, 0, 0, 0)
				[ToggleAndRequires(_SPHERICAL_MAPPING, _SphereMapped)] _SphericalMapping ("Spherical Mapping", int) = 0
				[Requires(_SphereMapped)] _SphereHorizonStretch("Sphere Stretch Horizon", float) = 0.33
			[KeywordEnumFullDrawer(Y_AXIS, X_AXIS, Z_AXIS)] _Alignment("Plane Alignment", int) = 0
			_OrthographicPerspective ("Orthographic Perspective", float) = 0

		[Space(10)]

		[Header(Raymarcher)]
			_DrawDistance("Draw Distance", float) = 10000.0
			_MaxSteps("Steps Max", int) = 75
			_StepSize("Step Min Size", float) = 0.3
			_StepSkip("Step Skip", float) = 1
			_StepParallelMult("Step Parallel Mult", float) = 35

		[Space(10)]

		[Header(Debug)]
			_AlphaCut ("AlphaCut", float ) = 0.01
			_RenderQueue("Render Queue", Range(0, 5000)) = 2501
			[Toggle]_ShowStepCount("Show Step Count", int) = 0
	}

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vertForwardModified
            #pragma fragment fragForwardClouds
            #pragma shader_feature_local _NEW_SHADING
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _RENDERSHADOWS
            #pragma shader_feature_local _RENDERSHADOWSONLY
            #pragma shader_feature_local _HQPOINTLIGHT
            #pragma shader_feature_local _COVERAGEMAP
            #pragma shader_feature_local _SPHERE_MAPPED
            #pragma shader_feature_local _SPHERICAL_MAPPING
            #pragma shader_feature_local _DISTBLEND_POST
            #pragma shader_feature_local _COMPLEXLIGHTING_MODE
            #pragma shader_feature_local _SSSHADOW_MAPBLEND
            #pragma shader_feature_local _VOLUMETRIC_SSS
            #pragma shader_feature_local Y_AXIS X_AXIS Z_AXIS
            #pragma shader_feature_local _DISABLE_DIRECT
            #pragma shader_feature_local _DISABLE_INDIRECT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "VolumetricCloudsCG_URP14.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "VolumetricClouds3.VCloudShaderGUI"
}
