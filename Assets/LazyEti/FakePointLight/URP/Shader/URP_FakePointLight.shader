// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "LazyEti/URP/FakePointLight"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[NoScaleOffset][SingleLineTexture]_GradientTexture("Gradient Texture", 2D) = "white" {}
		[HDR]_LightTint("Light Tint", Color) = (1,1,1,1)
		[Space(5)]_LightSoftness("Light Softness", Range( 0 , 1)) = 1
		[IntRange]_LightPosterize("Light Posterize", Range( 0 , 128)) = 1
		[Space(5)]_ShadingBlend("Shading Blend", Range( 0 , 1)) = 0.5
		_ShadingSoftness("Shading Softness", Range( 0.01 , 1)) = 0.5
		[Toggle(___HALO____ON)] ___Halo___("___Halo___", Float) = 1
		[HDR]_HaloTint("Halo Tint", Color) = (1,1,1,1)
		_HaloSize("Halo Size", Range( 0 , 5)) = 0
		[IntRange]_HaloPosterize("Halo Posterize", Range( 0 , 128)) = 0
		_HaloDepthFade("Halo Depth Fade", Range( 0.1 , 2)) = 0.5
		[Space(25)][Toggle]DistanceFade("___Distance Fade___", Float) = 0
		[Tooltip(Starts fading away at this distance from the camera)]_FarFade("Far Fade", Range( 0 , 400)) = 200
		_FarTransition("Far Transition", Range( 1 , 100)) = 50
		_CloseFade("Close Fade", Range( 0 , 50)) = 0
		_CloseTransition("Close Transition", Range( 0 , 50)) = 0
		[Space(25)][Toggle(___FLICKERING____ON)] ___Flickering___("___Flickering___", Float) = 0
		_FlickerIntensity("Flicker Intensity", Range( 0 , 1)) = 0.5
		_FlickerHue("Flicker Hue", Color) = (1,1,1)
		_FlickerSpeed("Flicker Speed", Range( 0.01 , 5)) = 1
		_FlickerSoftness("Flicker Softness", Range( 0 , 1)) = 0.5
		_SizeFlickering("Size Flickering", Range( 0 , 0.5)) = 0.1
		[Space(25)][Toggle(___NOISE____ON)] ___Noise___("___Noise___", Float) = 0
		[NoScaleOffset][SingleLineTexture]_NoiseTexture("Noise Texture", 2D) = "white" {}
		[KeywordEnum(Red,RedxGreen,Alpha)] _TexturePacking("Texture Packing", Float) = 0
		_Noisiness("Noisiness", Range( 0 , 2)) = 1
		_NoiseScale("Noise Scale", Range( 0.1 , 5)) = 0.1
		_NoiseMovement("Noise Movement", Range( 0 , 1)) = 0
		[Space(20)][Toggle(_SPECULARHIGHLIGHT_ON)] _SpecularHighlight("Specular Highlight", Float) = 0
		_SpecIntensity("Spec Intensity", Range( 0 , 1)) = 0.5
		[Space(20)][Toggle(_DITHERINGPATTERN_ON)] _DitheringPattern("Dithering Pattern", Float) = 0
		_DitherIntensity("Dither Intensity", Range( 0.01 , 1)) = 0.5
		[Space(20)][KeywordEnum(OFF,Low,Medium,High,Insane)] _ScreenShadows("Screen Shadows (HEAVY)", Float) = 0
		[space(25)]_ShadowThreshold("Shadow Threshold", Range( 0.05 , 1)) = 0.5
		[Toggle(_PARTICLEMODE_ON)] _ParticleMode("Particle Mode", Float) = 0
		[Space(15)][Toggle(_ACCURATECOLORS_ON)] _AccurateColors("Accurate Colors", Float) = 0
		[Space(15)][Toggle(_DAYFADING_ON)] _DayFading("Day Fading", Float) = 0
		[Space(15)][KeywordEnum(Additive,Contrast,Negative)] _Blendmode("Blendmode", Float) = 0
		[Enum(Default,0,Off,1,On,2)][Space(5)]_DepthWrite("Depth Write", Float) = 0
		[HideInInspector][IntRange]_SrcBlend("SrcBlend", Range( 0 , 12)) = 1
		[HideInInspector][IntRange]_DstBlend("DstBlend", Range( 0 , 12)) = 1
		[HideInInspector]_RandomOffset("RandomOffset", Range( 0 , 1)) = 0


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector][ToggleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Overlay" "Queue"="Overlay" "UniversalMaterialType"="Unlit" }

		Cull Front
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_DepthWrite]
			ZTest Always
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM
            #pragma multi_compile_instancing
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ASE_VERSION 19801
            #define ASE_SRP_VERSION 100400
            #define REQUIRE_DEPTH_TEXTURE 1
            #define REQUIRE_OPAQUE_TEXTURE 1
            #define ASE_USING_SAMPLING_MACROS 1

            #pragma multi_compile _ DOTS_INSTANCING_ON

			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_UNLIT

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#define ASE_NEEDS_FRAG_WORLD_VIEW_DIR
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _BLENDMODE_ADDITIVE _BLENDMODE_CONTRAST _BLENDMODE_NEGATIVE
			#pragma shader_feature_local _DITHERINGPATTERN_ON
			#pragma shader_feature_local _ACCURATECOLORS_ON
			#pragma shader_feature_local _PARTICLEMODE_ON
			#pragma shader_feature_local ___FLICKERING____ON
			#pragma shader_feature_local ___NOISE____ON
			#pragma shader_feature_local _TEXTUREPACKING_RED _TEXTUREPACKING_REDXGREEN _TEXTUREPACKING_ALPHA
			#pragma shader_feature_local _SCREENSHADOWS_OFF _SCREENSHADOWS_LOW _SCREENSHADOWS_MEDIUM _SCREENSHADOWS_HIGH _SCREENSHADOWS_INSANE
			#pragma shader_feature_local _DAYFADING_ON
			#pragma shader_feature_local _SPECULARHIGHLIGHT_ON
			#pragma shader_feature_local ___HALO____ON
			#ifdef STEREO_INSTANCING_ON
			TEXTURE2D_ARRAY(_CameraNormalsTexture); SAMPLER(sampler_CameraNormalsTexture);
			#else
			TEXTURE2D(_CameraNormalsTexture); SAMPLER(sampler_CameraNormalsTexture);
			#endif


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				#ifdef ASE_FOG
					float fogFactor : TEXCOORD2;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD3;
				#endif
				#if defined(LIGHTMAP_ON)
					float4 lightmapUVOrVertexSH : TEXCOORD4;
				#endif
				
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_color : COLOR;
				float4 ase_texcoord7 : TEXCOORD7;
				float4 ase_texcoord8 : TEXCOORD8;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _HaloTint;
			float4 _LightTint;
			float3 _FlickerHue;
			float _DstBlend;
			float _HaloDepthFade;
			float _HaloPosterize;
			float _HaloSize;
			float _SpecIntensity;
			float _ShadingBlend;
			float _CloseTransition;
			float _CloseFade;
			float _FarTransition;
			float _FarFade;
			float DistanceFade;
			float _ShadowThreshold;
			float _ShadingSoftness;
			float _Noisiness;
			float _NoiseMovement;
			float _NoiseScale;
			float _SizeFlickering;
			float _FlickerIntensity;
			float _FlickerSoftness;
			float _RandomOffset;
			float _FlickerSpeed;
			float _LightSoftness;
			float _DepthWrite;
			float _SrcBlend;
			float _LightPosterize;
			float _DitherIntensity;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			TEXTURE2D(_GradientTexture);
			TEXTURE2D(_NoiseTexture);
			SAMPLER(sampler_NoiseTexture);
			SAMPLER(sampler_GradientTexture);


			float noise58_g1436( float x )
			{
				float n = sin (2 * x) + sin(3.14159265 * x);
				return n;
			}
			
			float4 NormalTexURP2275( float2 uvs )
			{
				#ifdef STEREO_INSTANCING_ON
				return SAMPLE_TEXTURE2D_ARRAY(_CameraNormalsTexture,sampler_CameraNormalsTexture,uvs,unity_StereoEyeIndex);
				#else
				return SAMPLE_TEXTURE2D(_CameraNormalsTexture,sampler_CameraNormalsTexture,uvs);
				#endif
			}
			
			float4 ASEScreenPositionNormalizedToPixel( float4 screenPosNorm )
			{
				float4 screenPosPixel = screenPosNorm * float4( _ScreenParams.xy, 1, 1 );
				#if UNITY_UV_STARTS_AT_TOP
					screenPosPixel.xy = float2( screenPosPixel.x, ( _ProjectionParams.x < 0 ) ? _ScreenParams.y - screenPosPixel.y : screenPosPixel.y );
				#else
					screenPosPixel.xy = float2( screenPosPixel.x, ( _ProjectionParams.x > 0 ) ? _ScreenParams.y - screenPosPixel.y : screenPosPixel.y );
				#endif
				return screenPosPixel;
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
				     1, 49, 13, 61,  4, 52, 16, 64,
				    33, 17, 45, 29, 36, 20, 48, 32,
				     9, 57,  5, 53, 12, 60,  8, 56,
				    41, 25, 37, 21, 44, 28, 40, 24,
				     3, 51, 15, 63,  2, 50, 14, 62,
				    35, 19, 47, 31, 34, 18, 46, 30,
				    11, 59,  7, 55, 10, 58,  6, 54,
				    43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[ r ] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			
			float ExperimentalScreenShadowsURP( float2 lightDirScreen, float threshold, float stepsSpace, float stepsNum, float radius, float mask, float3 wPos, float3 lightPos, float3 camPos, float2 screenPos, float3 offsetDir )
			{
				if(mask<=0) return 1;
				float depth = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(screenPos), _ZBufferParams);
											
				//offset light position by its max radius:
				float3 lightRadiusOffsetPos = lightPos+ (offsetDir * radius);
				//convert position to real depth distance:
				float MaxDist = -mul( UNITY_MATRIX_V, float4(lightRadiusOffsetPos, 1)).z;
				//Early Return if greater than max radius:
				if(depth > MaxDist) return 1;
				//initialization:
				float shadow =0;
				float op = 2/stepsNum;
				float spacing =((stepsSpace/stepsNum)) *(clamp(distance (lightPos,camPos),radius,1));
				//float spacing =((stepsSpace/stepsNum)) ;
				float t = spacing;
				float realLightDist = -mul( UNITY_MATRIX_V, float4(lightPos, 1)).z;
				[unroll]  for (int i = 1;i <= stepsNum ;i++)
				{                    
					float2 uvs = screenPos + lightDirScreen.xy * t; //offset uv
					t = clamp( spacing * i,-1,1); //ray march
					float d = SHADERGRAPH_SAMPLE_SCENE_DEPTH(uvs); //sample depth
					float l = LinearEyeDepth(d, _ZBufferParams);
					if(MaxDist < l) return 1;
					float3 world = ComputeWorldSpacePosition(uvs, d, UNITY_MATRIX_I_VP);
					if(distance(wPos,lightPos) > radius) return 1; //remove out of range artifacts
					if(shadow>=1) break; 
					if(world.y - wPos.y> threshold * MaxDist && abs(world.y-wPos.y) < radius)  shadow  += op;
				}
				 //return smoothstep(.9,1,shadow);
				shadow = step (0.01, shadow)*mask;
				return (1- shadow);
			}
			
			float2 WorldToScreen83_g2197( float3 pos )
			{
				float4 wts = ComputeScreenPos( TransformWorldToHClip(pos));
				float3 wts_NDC = wts.xyz / wts.w;
				return wts_NDC.xy;
			}
			
			inline float Dither4x4Bayer( int x, int y )
			{
				const float dither[ 16 ] = {
				     1,  9,  3, 11,
				    13,  5, 15,  7,
				     4, 12,  2, 10,
				    16,  8, 14,  6 };
				int r = y * 4 + x;
				return dither[ r ] / 16; // same # of instructions as pre-dividing due to compiler magic
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				#ifdef _PARTICLEMODE_ON
				float3 staticSwitch255 = input.texcoord.xyz;
				#else
				float3 staticSwitch255 = ase_objectPosition;
				#endif
				float3 POSITION653 = staticSwitch255;
				float3 _Vector0 = float3(1,0,1);
				float Dist41_g2190 = distance( ( POSITION653 * _Vector0 ) , ( _Vector0 * _WorldSpaceCameraPos ) );
				float vertexToFrag49_g2190 = ( saturate( ( 1.0 - ( ( Dist41_g2190 - _FarFade ) / _FarTransition ) ) ) * saturate( ( ( Dist41_g2190 - _CloseFade ) / _CloseTransition ) ) );
				output.ase_texcoord6.w = vertexToFrag49_g2190;
				float dotResult3_g2189 = dot( -_MainLightPosition.xyz , float3( 0,1,0 ) );
				float vertexToFrag9_g2189 = saturate( ( dotResult3_g2189 * 4.0 ) );
				output.ase_texcoord7.x = vertexToFrag9_g2189;
				#ifdef _PARTICLEMODE_ON
				float staticSwitch1913 = input.texcoord.w;
				#else
				float staticSwitch1913 = _RandomOffset;
				#endif
				float RANDOMNESS3727 = staticSwitch1913;
				float temp_output_29_0_g1436 = RANDOMNESS3727;
				float mulTime17_g1436 = _TimeParameters.x * ( ( _FlickerSpeed + ( temp_output_29_0_g1436 * 0.1 ) ) * 4 );
				float x58_g1436 = ( mulTime17_g1436 + ( temp_output_29_0_g1436 * PI ) );
				float localnoise58_g1436 = noise58_g1436( x58_g1436 );
				float temp_output_44_0_g1436 = ( ( 1.0 - _FlickerSoftness ) * 0.5 );
				#ifdef ___FLICKERING____ON
				float staticSwitch53_g1436 = saturate( (( 1.0 - _FlickerIntensity ) + ((0.0 + (localnoise58_g1436 - -2.0) * (1.0 - 0.0) / (2.0 - -2.0)) - ( 1.0 - temp_output_44_0_g1436 )) * (1.0 - ( 1.0 - _FlickerIntensity )) / (temp_output_44_0_g1436 - ( 1.0 - temp_output_44_0_g1436 ))) );
				#else
				float staticSwitch53_g1436 = 1.0;
				#endif
				float FlickerAlpha416 = staticSwitch53_g1436;
				float FlickerSize477 = (( 1.0 - _SizeFlickering ) + (FlickerAlpha416 - 0.0) * (1.0 - ( 1.0 - _SizeFlickering )) / (1.0 - 0.0));
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				#ifdef _PARTICLEMODE_ON
				float3 staticSwitch3833 = ( ase_objectScale * input.texcoord1.xyz );
				#else
				float3 staticSwitch3833 = ase_objectScale;
				#endif
				float3 break3887 = staticSwitch3833;
				float SCALE3835 = max( max( break3887.x , break3887.y ) , break3887.z );
				#ifdef _PARTICLEMODE_ON
				float staticSwitch4535 = ( SCALE3835 * 0.1 );
				#else
				float staticSwitch4535 = 1.0;
				#endif
				float vertexToFrag32_g2197 = ( ( _HaloSize * ( FlickerSize477 * staticSwitch4535 ) ) * 0.5 );
				output.ase_texcoord7.y = vertexToFrag32_g2197;
				float3 pos75_g2197 = POSITION653;
				float vertexToFrag15_g2200 = ( unity_OrthoParams.w == 0.0 ? ( distance( _WorldSpaceCameraPos , pos75_g2197 ) / -UNITY_MATRIX_P[ 1 ][ 1 ] ) : unity_OrthoParams.y );
				output.ase_texcoord7.z = vertexToFrag15_g2200;
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - ase_positionWS );
				float3 ase_viewDirSafeWS = SafeNormalize( ase_viewVectorWS );
				float dotResult31_g2197 = dot( ase_viewDirSafeWS , ( _WorldSpaceCameraPos - pos75_g2197 ) );
				float vertexToFrag36_g2197 = step( 0.0 , dotResult31_g2197 );
				output.ase_texcoord7.w = vertexToFrag36_g2197;
				float vertexToFrag62_g2197 = distance( _WorldSpaceCameraPos , pos75_g2197 );
				output.ase_texcoord8.x = vertexToFrag62_g2197;
				float3 temp_cast_0 = (1.0).xxx;
				float3 lerpResult51_g1436 = lerp( _FlickerHue , temp_cast_0 , ( staticSwitch53_g1436 * staticSwitch53_g1436 ));
				float3 vertexToFrag67_g1436 = lerpResult51_g1436;
				output.ase_texcoord8.yzw = vertexToFrag67_g1436;
				
				output.ase_texcoord5 = input.texcoord;
				output.ase_texcoord6.xyz = input.texcoord1.xyz;
				output.ase_color = input.ase_color;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(LIGHTMAP_ON)
					OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy);
				#endif


				#ifdef ASE_FOG
					output.fogFactor = ComputeFogFactor( vertexInput.positionCS.z );
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined(LOD_FADE_CROSSFADE)
					LODDitheringTransition( input.positionCS.xyz, unity_LODFade.x );
				#endif

				float3 WorldPosition = input.positionWS;
				float3 WorldViewDirection = GetWorldSpaceNormalizeViewDir( WorldPosition );
				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float temp_output_3711_0 = ( ( 1.0 - ( _LightSoftness * 1.1 ) ) * 0.5 );
				float4 ase_positionSSNorm = ScreenPos / ScreenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float depthLinearEye6_g2203 = LinearEyeDepth( SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_positionSSNorm.xy ), _ZBufferParams );
				float3 objToWorldDir13_g2204 = mul( GetObjectToWorldMatrix(), float4( (transpose( mul( GetWorldToObjectMatrix(), UNITY_MATRIX_I_V ) )[2]).xyz, 0.0 ) ).xyz;
				float dotResult4_g2203 = dot( WorldViewDirection , -objToWorldDir13_g2204 );
				float3 worldToView103_g2203 = mul( UNITY_MATRIX_V, float4( WorldPosition, 1 ) ).xyz;
				float depth01_94_g2203 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_positionSSNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch97_g2203 = ( 1.0 - depth01_94_g2203 );
				#else
				float staticSwitch97_g2203 = depth01_94_g2203;
				#endif
				float lerpResult98_g2203 = lerp( _ProjectionParams.y , _ProjectionParams.z , staticSwitch97_g2203);
				float3 appendResult100_g2203 = (float3(worldToView103_g2203.x , worldToView103_g2203.y , -lerpResult98_g2203));
				float3 viewToWorld101_g2203 = mul( UNITY_MATRIX_I_V, float4( appendResult100_g2203, 1.0 ) ).xyz;
				float3 ReconstructedPos539 = (( unity_OrthoParams.w < 1.0 ? ( ( depthLinearEye6_g2203 * ( WorldViewDirection / dotResult4_g2203 ) ) + _WorldSpaceCameraPos ) : viewToWorld101_g2203 )).xyz;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				#ifdef _PARTICLEMODE_ON
				float3 staticSwitch255 = input.ase_texcoord5.xyz;
				#else
				float3 staticSwitch255 = ase_objectPosition;
				#endif
				float3 POSITION653 = staticSwitch255;
				float3 LocalPos4107 = ( ReconstructedPos539 - POSITION653 );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				#ifdef _PARTICLEMODE_ON
				float3 staticSwitch3833 = ( ase_objectScale * input.ase_texcoord6.xyz );
				#else
				float3 staticSwitch3833 = ase_objectScale;
				#endif
				float3 break3887 = staticSwitch3833;
				float SCALE3835 = max( max( break3887.x , break3887.y ) , break3887.z );
				#ifdef _PARTICLEMODE_ON
				float staticSwitch1913 = input.ase_texcoord5.w;
				#else
				float staticSwitch1913 = _RandomOffset;
				#endif
				float RANDOMNESS3727 = staticSwitch1913;
				float temp_output_29_0_g1436 = RANDOMNESS3727;
				float mulTime17_g1436 = _TimeParameters.x * ( ( _FlickerSpeed + ( temp_output_29_0_g1436 * 0.1 ) ) * 4 );
				float x58_g1436 = ( mulTime17_g1436 + ( temp_output_29_0_g1436 * PI ) );
				float localnoise58_g1436 = noise58_g1436( x58_g1436 );
				float temp_output_44_0_g1436 = ( ( 1.0 - _FlickerSoftness ) * 0.5 );
				#ifdef ___FLICKERING____ON
				float staticSwitch53_g1436 = saturate( (( 1.0 - _FlickerIntensity ) + ((0.0 + (localnoise58_g1436 - -2.0) * (1.0 - 0.0) / (2.0 - -2.0)) - ( 1.0 - temp_output_44_0_g1436 )) * (1.0 - ( 1.0 - _FlickerIntensity )) / (temp_output_44_0_g1436 - ( 1.0 - temp_output_44_0_g1436 ))) );
				#else
				float staticSwitch53_g1436 = 1.0;
				#endif
				float FlickerAlpha416 = staticSwitch53_g1436;
				float FlickerSize477 = (( 1.0 - _SizeFlickering ) + (FlickerAlpha416 - 0.0) * (1.0 - ( 1.0 - _SizeFlickering )) / (1.0 - 0.0));
				float temp_output_3981_0 = saturate( ( 1.0 - ( length( LocalPos4107 ) / ( SCALE3835 * ( FlickerSize477 * 0.45 ) ) ) ) );
				float3 UVS73_g2191 = ( ( ReconstructedPos539 * 0.1 ) * _NoiseScale );
				float2 ScreenPos4516 = (ase_positionSSNorm).xy;
				float2 uvs2275 = ScreenPos4516;
				float4 localNormalTexURP2275 = NormalTexURP2275( uvs2275 );
				float3 worldNormals1927 = (localNormalTexURP2275).xyz;
				float3 temp_cast_0 = (3.0).xxx;
				float3 temp_output_141_0_g2191 = pow( abs( worldNormals1927 ) , temp_cast_0 );
				float3 temp_cast_1 = (1.0).xxx;
				float dotResult144_g2191 = dot( temp_output_141_0_g2191 , temp_cast_1 );
				float3 break147_g2191 = ( saturate( temp_output_141_0_g2191 ) / dotResult144_g2191 );
				float4 ase_positionSS_Pixel = ASEScreenPositionNormalizedToPixel( ase_positionSSNorm );
				float dither151_g2191 = Dither8x8Bayer( fmod( ase_positionSS_Pixel.x, 8 ), fmod( ase_positionSS_Pixel.y, 8 ) );
				float2 lerpResult19_g2191 = lerp( (UVS73_g2191).xz , ( (UVS73_g2191).yz * 0.9 ) , round( ( ( ( ( 1.0 - break147_g2191.x ) * break147_g2191.x ) * dither151_g2191 ) + break147_g2191.x ) ));
				float2 lerpResult20_g2191 = lerp( lerpResult19_g2191 , ( (UVS73_g2191).xy * 0.94 ) , round( ( break147_g2191.z + ( dither151_g2191 * ( break147_g2191.z * ( 1.0 - break147_g2191.z ) ) ) ) ));
				float temp_output_60_0_g2191 = RANDOMNESS3727;
				float mulTime41_g2191 = _TimeParameters.x * ( ( _NoiseMovement + ( temp_output_60_0_g2191 * 0.1 ) ) * 0.2 );
				float Time70_g2191 = ( mulTime41_g2191 + ( temp_output_60_0_g2191 * PI ) );
				float4 tex2DNode3_g2191 = SAMPLE_TEXTURE2D( _NoiseTexture, sampler_NoiseTexture, ( lerpResult20_g2191 + ( Time70_g2191 * float2( 1.02,0.87 ) ) ) );
				#if defined( _TEXTUREPACKING_RED )
				float staticSwitch211_g2191 = tex2DNode3_g2191.r;
				#elif defined( _TEXTUREPACKING_REDXGREEN )
				float staticSwitch211_g2191 = tex2DNode3_g2191.g;
				#elif defined( _TEXTUREPACKING_ALPHA )
				float staticSwitch211_g2191 = tex2DNode3_g2191.a;
				#else
				float staticSwitch211_g2191 = tex2DNode3_g2191.r;
				#endif
				float4 tex2DNode14_g2191 = SAMPLE_TEXTURE2D( _NoiseTexture, sampler_NoiseTexture, ( ( lerpResult20_g2191 * 0.7 ) + ( Time70_g2191 * float2( -0.72,-0.67 ) ) ) );
				#if defined( _TEXTUREPACKING_RED )
				float staticSwitch212_g2191 = tex2DNode14_g2191.r;
				#elif defined( _TEXTUREPACKING_REDXGREEN )
				float staticSwitch212_g2191 = tex2DNode14_g2191.r;
				#elif defined( _TEXTUREPACKING_ALPHA )
				float staticSwitch212_g2191 = tex2DNode14_g2191.a;
				#else
				float staticSwitch212_g2191 = tex2DNode14_g2191.r;
				#endif
				#ifdef ___NOISE____ON
				float staticSwitch186_g2191 = ( 1.0 + ( ( staticSwitch211_g2191 * staticSwitch212_g2191 * _Noisiness ) - ( _Noisiness * 0.2 ) ) );
				#else
				float staticSwitch186_g2191 = 1.0;
				#endif
				float noise3969 = staticSwitch186_g2191;
				float smoothstepResult745 = smoothstep( temp_output_3711_0 , ( 1.0 - temp_output_3711_0 ) , ( temp_output_3981_0 * ( temp_output_3981_0 + noise3969 ) ));
				float lPosterize4215 = _LightPosterize;
				float temp_output_8_0_g2195 = lPosterize4215;
				float BrightnessBoost3902 = saturate( pow( temp_output_3981_0 , 30.0 ) );
				float temp_output_9_0_g2195 = ( smoothstepResult745 + BrightnessBoost3902 );
				float temp_output_5_0_g2195 = ( 256.0 / temp_output_8_0_g2195 );
				float GradientMask555 = ( smoothstepResult745 * ( temp_output_8_0_g2195 <= 0.0 ? temp_output_9_0_g2195 : saturate( ( floor( ( temp_output_9_0_g2195 * temp_output_5_0_g2195 ) ) / temp_output_5_0_g2195 ) ) ) );
				float2 temp_cast_2 = (( 1.0 - GradientMask555 )).xx;
				float4 temp_output_2_0_g2196 = ( SAMPLE_TEXTURE2D( _GradientTexture, sampler_GradientTexture, temp_cast_2 ) * _LightTint * input.ase_color );
				float temp_output_8_0_g2194 = lPosterize4215;
				float3 normalizeResult3808 = normalize( -LocalPos4107 );
				float3 LightDir4264 = normalizeResult3808;
				float dotResult436 = dot( LightDir4264 , worldNormals1927 );
				float3 pos338_g2192 = POSITION653;
				float rad280_g2192 = ( SCALE3835 * 0.5 );
				float temp_output_298_0_g2192 = ( 1.0 - ( abs( (pos338_g2192).y ) / rad280_g2192 ) );
				float3 normalizeResult278_g2192 = normalize( ( pos338_g2192 - _WorldSpaceCameraPos ) );
				float3 objToWorldDir13_g2193 = mul( GetObjectToWorldMatrix(), float4( (transpose( mul( GetWorldToObjectMatrix(), UNITY_MATRIX_I_V ) )[2]).xyz, 0.0 ) ).xyz;
				float dotResult283_g2192 = dot( normalizeResult278_g2192 , -objToWorldDir13_g2193 );
				float temp_output_310_0_g2192 = ( temp_output_298_0_g2192 * dotResult283_g2192 );
				float4 worldToScreen279_g2192 = ComputeScreenPos( TransformWorldToHClip( ( pos338_g2192 * float3(1,0.1,1) ) ) );
				float3 worldToScreen279_g2192NDC = worldToScreen279_g2192.xyz/worldToScreen279_g2192.w;
				float2 temp_output_344_0_g2192 = (( float4( worldToScreen279_g2192NDC , 0.0 ) - ase_positionSSNorm )).xy;
				float2 temp_output_384_0_g2192 = ( temp_output_310_0_g2192 * temp_output_344_0_g2192 );
				float2 lightDirScreen483_g2192 = temp_output_384_0_g2192;
				float temp_output_476_0_g2192 = ( _ShadowThreshold * 0.01 );
				float threshold483_g2192 = temp_output_476_0_g2192;
				float stepsSpace483_g2192 = 1.0;
				#if defined( _SCREENSHADOWS_OFF )
				float staticSwitch296_g2192 = -1.0;
				#elif defined( _SCREENSHADOWS_LOW )
				float staticSwitch296_g2192 = 16.0;
				#elif defined( _SCREENSHADOWS_MEDIUM )
				float staticSwitch296_g2192 = 32.0;
				#elif defined( _SCREENSHADOWS_HIGH )
				float staticSwitch296_g2192 = 64.0;
				#elif defined( _SCREENSHADOWS_INSANE )
				float staticSwitch296_g2192 = 128.0;
				#else
				float staticSwitch296_g2192 = -1.0;
				#endif
				float StepQual300_g2192 = staticSwitch296_g2192;
				float stepsNum483_g2192 = StepQual300_g2192;
				float radius483_g2192 = rad280_g2192;
				float vertexToFrag49_g2190 = input.ase_texcoord6.w;
				float vertexToFrag9_g2189 = input.ase_texcoord7.x;
				#ifdef _DAYFADING_ON
				float staticSwitch11_g2189 = vertexToFrag9_g2189;
				#else
				float staticSwitch11_g2189 = 1.0;
				#endif
				float DistanceFade3571 = ( (( DistanceFade )?( vertexToFrag49_g2190 ):( 1.0 )) * staticSwitch11_g2189 );
				float temp_output_335_0_g2192 = DistanceFade3571;
				float mask483_g2192 = temp_output_335_0_g2192;
				float3 worldPos353_g2192 = ReconstructedPos539;
				float3 wPos483_g2192 = worldPos353_g2192;
				float3 lightPos483_g2192 = pos338_g2192;
				float3 camPos483_g2192 = _WorldSpaceCameraPos;
				float2 screenPos483_g2192 = ase_positionSSNorm.xy;
				float3 offsetDir485_g2192 = ( dotResult283_g2192 * normalizeResult278_g2192 );
				float3 offsetDir483_g2192 = offsetDir485_g2192;
				float localExperimentalScreenShadowsURP483_g2192 = ExperimentalScreenShadowsURP( lightDirScreen483_g2192 , threshold483_g2192 , stepsSpace483_g2192 , stepsNum483_g2192 , radius483_g2192 , mask483_g2192 , wPos483_g2192 , lightPos483_g2192 , camPos483_g2192 , screenPos483_g2192 , offsetDir483_g2192 );
				#if defined( _SCREENSHADOWS_OFF )
				float staticSwitch341_g2192 = 1.0;
				#elif defined( _SCREENSHADOWS_LOW )
				float staticSwitch341_g2192 = localExperimentalScreenShadowsURP483_g2192;
				#elif defined( _SCREENSHADOWS_MEDIUM )
				float staticSwitch341_g2192 = localExperimentalScreenShadowsURP483_g2192;
				#elif defined( _SCREENSHADOWS_HIGH )
				float staticSwitch341_g2192 = localExperimentalScreenShadowsURP483_g2192;
				#elif defined( _SCREENSHADOWS_INSANE )
				float staticSwitch341_g2192 = localExperimentalScreenShadowsURP483_g2192;
				#else
				float staticSwitch341_g2192 = 1.0;
				#endif
				float ScreenSpaceShadows1881 = staticSwitch341_g2192;
				float smoothstepResult4598 = smoothstep( 0.0 , _ShadingSoftness , saturate( ( dotResult436 * noise3969 * ScreenSpaceShadows1881 ) ));
				float temp_output_9_0_g2194 = smoothstepResult4598;
				float temp_output_5_0_g2194 = ( 256.0 / temp_output_8_0_g2194 );
				float ShadingMask552 = saturate( ( ( temp_output_8_0_g2194 <= 0.0 ? temp_output_9_0_g2194 : saturate( ( floor( ( temp_output_9_0_g2194 * temp_output_5_0_g2194 ) ) / temp_output_5_0_g2194 ) ) ) + _ShadingBlend ) );
				float surfaceMask487 = step( 0.01 , temp_output_3981_0 );
				float FinalLightMask4298 = ( GradientMask555 * ShadingMask552 * surfaceMask487 );
				float3 normalizeResult4267 = normalize( ( WorldViewDirection + LightDir4264 ) );
				float dotResult4231 = dot( normalizeResult4267 , ( worldNormals1927 * float3(1,0.99,1) ) );
				#ifdef _SPECULARHIGHLIGHT_ON
				float staticSwitch4285 = ( ( _SpecIntensity * 2 ) * pow( saturate( dotResult4231 ) , ( (0.5*0.5 + 0.5) * 200 ) ) * FinalLightMask4298 );
				#else
				float staticSwitch4285 = 0.0;
				#endif
				float Spec4287 = staticSwitch4285;
				float3 temp_output_12_0_g2201 = ( ( (temp_output_2_0_g2196).rgb * ( (temp_output_2_0_g2196).a * FinalLightMask4298 * 0.1 ) ) + Spec4287 );
				float4 fetchOpaqueVal5_g2201 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ScreenPos4516.xy ), 1.0 );
				float3 saferPower9_g2201 = abs( (fetchOpaqueVal5_g2201).rgb );
				float3 temp_cast_5 = (saturate( (0.8 + (GradientMask555 - 0.0) * (0.0 - 0.8) / (2.0 - 0.0)) )).xxx;
				#ifdef _ACCURATECOLORS_ON
				float3 staticSwitch11_g2201 = ( ( temp_output_12_0_g2201 * 4 ) * pow( saferPower9_g2201 , temp_cast_5 ) );
				#else
				float3 staticSwitch11_g2201 = temp_output_12_0_g2201;
				#endif
				float3 temp_cast_6 = (0.0).xxx;
				float vertexToFrag32_g2197 = input.ase_texcoord7.y;
				float3 pos75_g2197 = POSITION653;
				float3 pos83_g2197 = pos75_g2197;
				float2 localWorldToScreen83_g2197 = WorldToScreen83_g2197( pos83_g2197 );
				float2 appendResult15_g2197 = (float2(( _ScreenParams.x / _ScreenParams.y ) , 1.0));
				float vertexToFrag15_g2200 = input.ase_texcoord7.z;
				float smoothstepResult33_g2197 = smoothstep( 0.0 , vertexToFrag32_g2197 , length( ( ( ( (localWorldToScreen83_g2197).xy - ScreenPos4516 ) * appendResult15_g2197 ) * vertexToFrag15_g2200 ) ));
				float vertexToFrag36_g2197 = input.ase_texcoord7.w;
				float HaloMask38_g2197 = ( ( 1.0 - smoothstepResult33_g2197 ) * vertexToFrag36_g2197 );
				float temp_output_8_0_g2198 = _HaloPosterize;
				float temp_output_9_0_g2198 = HaloMask38_g2197;
				float temp_output_5_0_g2198 = ( 256.0 / temp_output_8_0_g2198 );
				float HaloPosterized43_g2197 = ( HaloMask38_g2197 * ( temp_output_8_0_g2198 <= 0.0 ? temp_output_9_0_g2198 : saturate( ( floor( ( temp_output_9_0_g2198 * temp_output_5_0_g2198 ) ) / temp_output_5_0_g2198 ) ) ) );
				float2 temp_cast_7 = (( 1.0 - HaloPosterized43_g2197 )).xx;
				float4 temp_output_2_0_g2199 = ( SAMPLE_TEXTURE2D( _GradientTexture, sampler_GradientTexture, temp_cast_7 ) * _HaloTint * input.ase_color );
				float vertexToFrag62_g2197 = input.ase_texcoord8.x;
				float HaloPenetrationMask68_g2197 = saturate( pow( saturate( ( distance( ReconstructedPos539 , _WorldSpaceCameraPos ) - vertexToFrag62_g2197 ) ) , _HaloDepthFade ) );
				#ifdef ___HALO____ON
				float3 staticSwitch54_g2197 = ( (temp_output_2_0_g2199).rgb * ( (temp_output_2_0_g2199).a * HaloMask38_g2197 * HaloPenetrationMask68_g2197 * HaloPosterized43_g2197 ) );
				#else
				float3 staticSwitch54_g2197 = temp_cast_6;
				#endif
				float3 halo4548 = staticSwitch54_g2197;
				float3 vertexToFrag67_g1436 = input.ase_texcoord8.yzw;
				float3 FlickerHue1892 = vertexToFrag67_g1436;
				float3 temp_output_17_0_g2202 = ( ( ( staticSwitch11_g2201 + halo4548 ) * FlickerHue1892 ) * ( DistanceFade3571 * FlickerAlpha416 ) );
				float dither10_g2202 = Dither4x4Bayer( fmod( ase_positionSS_Pixel.x, 4 ), fmod( ase_positionSS_Pixel.y, 4 ) );
				float3 break3_g2202 = temp_output_17_0_g2202;
				float smoothstepResult9_g2202 = smoothstep( 0.0 , _DitherIntensity , ( 0.333 * ( break3_g2202.x + break3_g2202.y + break3_g2202.z ) * ( _DitherIntensity + 1.0 ) ));
				dither10_g2202 = step( dither10_g2202, saturate( smoothstepResult9_g2202 * 1.00001 ) );
				#ifdef _DITHERINGPATTERN_ON
				float3 staticSwitch12_g2202 = ( temp_output_17_0_g2202 * dither10_g2202 );
				#else
				float3 staticSwitch12_g2202 = temp_output_17_0_g2202;
				#endif
				float3 temp_output_4553_0 = staticSwitch12_g2202;
				#if defined( _BLENDMODE_ADDITIVE )
				float3 staticSwitch4585 = temp_output_4553_0;
				#elif defined( _BLENDMODE_CONTRAST )
				float3 staticSwitch4585 = temp_output_4553_0;
				#elif defined( _BLENDMODE_NEGATIVE )
				float3 staticSwitch4585 = ( 1.0 - saturate( temp_output_4553_0 ) );
				#else
				float3 staticSwitch4585 = temp_output_4553_0;
				#endif
				float3 FinalColor4905 = staticSwitch4585;
				float4 appendResult3578 = (float4(FinalColor4905 , 1.0));
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = appendResult3578.xyz;
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef ASE_FOG
					Color = MixFog(Color, input.fogFactor);
				#endif

				return half4( Color, Alpha );
			}
			ENDHLSL
		}

	
	}
	
	CustomEditor "FPL.CustomMaterialEditor"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.CommentaryNode;3728;-3339.878,-1045.967;Inherit;False;734.679;386.8545;;5;1914;742;3832;3727;1913;Random;0.6886792,0,0.67818,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;476;-4574.993,-894.2358;Inherit;False;1143.62;715.2286;;15;3834;4591;260;711;709;486;3835;3888;3886;3887;653;3833;255;252;3814;Particle transform;0.5424528,1,0.9184569,1;0;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;742;-3294.294,-865.4466;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;1914;-3302.18,-986.6736;Inherit;False;Property;_RandomOffset;RandomOffset;52;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;260;-4548.069,-345.5797;Inherit;False;1;3;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ObjectScaleNode;3834;-4523.051,-532.8078;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.StaticSwitch;1913;-3048.042,-882.5033;Inherit;False;Property;_ParticleMesh;ParticleMesh;43;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;255;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;484;-2586.187,-1047.923;Inherit;False;960.162;381.7733;;7;1892;477;466;467;416;4498;463;Flicker;0.5613208,0.8882713,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;4517;-4769.077,-132.6272;Inherit;False;628.2538;236.657;screen pos;3;4516;4515;4514;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4591;-4327.826,-369.1419;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3727;-2813.076,-881.6541;Inherit;False;RANDOMNESS;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectPositionNode;3814;-4228.537,-841.8353;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;463;-2436.796,-808.6651;Inherit;False;Property;_SizeFlickering;Size Flickering;24;0;Create;True;0;0;0;False;0;False;0.1;0.5;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4498;-2565.921,-954.3461;Inherit;False;FlickerFunction;18;;1436;f6225b1ef66c663478bc4f0259ec00df;0;4;9;FLOAT;0;False;8;FLOAT;0;False;21;FLOAT;0;False;29;FLOAT;0;False;2;FLOAT;0;FLOAT3;45
Node;AmplifyShaderEditor.StaticSwitch;3833;-4207.758,-532.2737;Inherit;False;Property;_ParticleMesh;ParticleMesh;43;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;255;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;252;-4448.995,-741.957;Inherit;False;0;3;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenPosInputsNode;4514;-4749.077,-81.62715;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;480;-1606.75,-1080.458;Inherit;False;1475.525;429.7961;;12;4874;3954;262;55;3966;4107;3836;3708;3819;478;539;654;World SphericalMask;0.9034846,0.5330188,1,1;0;0
Node;AmplifyShaderEditor.StaticSwitch;255;-4025.325,-766.166;Inherit;False;Property;_ParticleMode;Particle Mode;43;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;416;-2267.689,-954.5582;Inherit;False;FlickerAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;467;-2163.736,-808.0251;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;3887;-3971.227,-532.9357;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.ComponentMaskNode;4515;-4550.602,-81.08909;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;4964;-1603.651,-1016.364;Inherit;False;Reconstruct World Pos from Depth VR;-1;;2203;474d2b03c8647914986393f8dfbd9fe4;0;0;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;3573;-4116.101,-14.06753;Inherit;False;870.2667;343.3553;;3;3571;4467;3821;Distance Fading;0.4079299,0.8396226,0.4819806,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;2273;-4888.541,138.3773;Inherit;False;742.5842;232.0259;;4;1927;4875;2275;4518;NormalsTexture;0.5424528,1,0.8822392,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;653;-3777.223,-766.7069;Inherit;False;POSITION;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TFHCRemapNode;466;-2013.38,-954.6299;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;3886;-3867.227,-532.9357;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4516;-4350.054,-80.06077;Inherit;False;ScreenPos;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;4874;-1276.664,-1016.913;Inherit;False;True;True;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;3821;-4086.4,142.9396;Inherit;False;653;POSITION;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;477;-1827.317,-955.2652;Inherit;False;FlickerSize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;3888;-3766.552,-508.2912;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4518;-4868.018,234.1036;Inherit;False;4516;ScreenPos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;654;-1046.13,-949.4542;Inherit;False;653;POSITION;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;539;-1080.325,-1017.349;Inherit;False;ReconstructedPos;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;4569;-3723.431,239.1101;Inherit;False;DayAlpha;46;;2189;bc1f8ebe2e26696419e0099f8a3e27dc;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4792;-3905.166,46.45875;Inherit;False;AdvancedCameraFade;12;;2190;e6e830f789d28b746963801d61c2a1ec;0;6;40;FLOAT;0;False;46;FLOAT;0;False;47;FLOAT;0;False;48;FLOAT;0;False;17;FLOAT3;0,0,0;False;20;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3835;-3657.271,-507.5939;Inherit;False;SCALE;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;2275;-4689.167,234.2081;Inherit;False;#ifdef STEREO_INSTANCING_ON$return SAMPLE_TEXTURE2D_ARRAY(_CameraNormalsTexture,sampler_CameraNormalsTexture,uvs,unity_StereoEyeIndex)@$#else$return SAMPLE_TEXTURE2D(_CameraNormalsTexture,sampler_CameraNormalsTexture,uvs)@$#endif;4;Create;1;True;uvs;FLOAT2;0,0;In;;Inherit;False;Normal Tex URP;True;False;0;;False;1;0;FLOAT2;0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;3819;-868.9444,-1017.758;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;478;-884.4586,-815.4437;Inherit;False;477;FlickerSize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;442;-2558.515,-598.0592;Inherit;False;2265.728;426.7485;Be sure to have a renderer feature that writes to _CameraNormalsTexture for this to work;18;4235;4234;4264;2274;1882;553;552;551;562;471;4213;4216;4598;549;4219;4595;436;3808;Normal Direction Masking;0.6086246,0.5235849,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4467;-3592.764,45.93977;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;4875;-4550.378,234.9712;Inherit;False;True;True;True;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4107;-723.6319,-1018.016;Inherit;False;LocalPos;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;3836;-748.7867,-894.8469;Inherit;False;3835;SCALE;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;3708;-715.3141,-815.6875;Inherit;False;0.45;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;1356;-3153.411,-53.7183;Inherit;False;907.1592;385.8282;;5;1881;3576;3838;3540;3830;Experimental Shadows;1,0.0518868,0.0518868,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;513;-3379.062,-569.1953;Inherit;False;781.0093;390.9941;;5;3969;1929;3729;542;4619;Noise;1,0.6084906,0.6084906,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3571;-3455.373,47.83217;Inherit;False;DistanceFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1927;-4348.426,233.8262;Inherit;False;worldNormals;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LengthOpNode;55;-515.3771,-1018.688;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3966;-535.5971,-891.0703;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4234;-2517.79,-477.1054;Inherit;False;4107;LocalPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;510;-92.06051,-1081.912;Inherit;False;1034.724;415.4808;;10;66;745;514;769;509;3711;3971;3712;3981;4624;Light Mask Hardness;1,0.8561655,0.3632075,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;3830;-3069.891,41.52334;Inherit;False;653;POSITION;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;3576;-3097.815,231.8469;Inherit;False;3571;DistanceFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3838;-3068.156,169.1145;Inherit;False;3835;SCALE;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3540;-3099.951,106.7249;Inherit;False;539;ReconstructedPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;262;-379.0427,-1020.662;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;542;-3330.709,-370.4586;Inherit;False;539;ReconstructedPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;3729;-3327.576,-296.6964;Inherit;False;3727;RANDOMNESS;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1929;-3331.611,-447.0953;Inherit;False;1927;worldNormals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NegateNode;4235;-2348.879,-477.6057;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;66;-106.8148,-767.4788;Inherit;False;Property;_LightSoftness;Light Softness;2;0;Create;True;0;0;0;False;1;Space(5);False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;3808;-2204.026,-478.0162;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;3954;-266.9739,-1020.73;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4619;-3099.628,-428.4288;Inherit;False;3DNoiseMap;25;;2191;2fca756491ec7bf4e9c71d18280c45cc;0;5;257;FLOAT3;0,0,0;False;21;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;60;FLOAT;0;False;229;FLOAT2;0,0;False;2;FLOAT;0;FLOAT;213
Node;AmplifyShaderEditor.FunctionNode;4907;-2853.486,75.6248;Inherit;False;ExperimentalScreenSpaceShadows;40;;2192;79f826106fc5f154c96059cc1326b755;0;4;337;FLOAT3;0,0,0;False;336;FLOAT3;0,0,0;False;370;FLOAT;0;False;335;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;3968;1113.218,-1089.262;Inherit;False;675.3597;364.2049;Additional Masks;6;3902;487;485;3958;3952;3984;;0.3531061,0.406577,0.6509434,1;0;0
Node;AmplifyShaderEditor.SaturateNode;3981;3.173216,-1021.999;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;4624;157.0977,-735.9185;Inherit;False;1.1;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1881;-2505.991,70.01049;Inherit;False;ScreenSpaceShadows;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3969;-2827.074,-429.44;Inherit;False;noise;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;2274;-2097.306,-403.2095;Inherit;False;1927;worldNormals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4264;-2063.805,-478.2308;Inherit;False;LightDir;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;500;966.688,-619.572;Inherit;False;1117.27;328.2859;;8;4215;492;4203;3992;3896;4214;555;4200;Light Posterize;0.5707547,1,0.9954711,1;0;0
Node;AmplifyShaderEditor.RelayNode;3984;1143.982,-1023.644;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;3712;282.4139,-766.8303;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3971;55.14507,-891.7;Inherit;False;3969;noise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;436;-1839.606,-477.819;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;553;-1831.932,-373.276;Inherit;False;3969;noise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1882;-1877.893,-306.75;Inherit;False;1881;ScreenSpaceShadows;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;3952;1267.87,-1020.482;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;30;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;3711;416.8805,-766.8687;Inherit;False;0.5;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;509;234.626,-914.4458;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;492;994.0338,-391.6727;Inherit;False;Property;_LightPosterize;Light Posterize;3;1;[IntRange];Create;True;0;0;0;False;0;False;1;0;0;128;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4595;-1630.537,-477.8681;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;3958;1401.523,-1020.424;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;769;577.5012,-769.8422;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;514;373.3369,-938.2104;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4215;1268.29,-392.126;Inherit;False;lPosterize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;4219;-1470.269,-477.6597;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;549;-1553.529,-346.7776;Inherit;False;Property;_ShadingSoftness;Shading Softness;5;0;Create;True;0;0;0;False;0;False;0.5;0.554;0.01;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3902;1566.586,-1018.981;Inherit;False;BrightnessBoost;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;745;732.6475,-938.0301;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;4598;-1266.524,-478.1245;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4216;-1262.152,-359.961;Inherit;False;4215;lPosterize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;4214;1155.234,-550.8469;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3896;1073.631,-469.6752;Inherit;False;3902;BrightnessBoost;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4213;-1042.616,-478.2397;Inherit;False;SimplePosterize;-1;;2194;163fbd1f7d6893e4ead4288913aedc26;0;2;9;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;471;-1028.935,-362.4963;Inherit;False;Property;_ShadingBlend;Shading Blend;4;0;Create;True;0;0;0;False;1;Space(5);False;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;4286;-2162.305,-90.49103;Inherit;False;1843.076;430.4736;;20;4287;4285;4302;4274;4500;4229;4301;4278;4230;4280;4282;4231;4244;4275;4267;4276;4268;4266;4265;4236;Specular;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;3992;1303.437,-489.5305;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;562;-753.7457,-477.736;Inherit;False;2;2;0;FLOAT;0.5;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;485;1272.435,-902.7695;Inherit;False;2;0;FLOAT;0.01;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4203;1444.219,-490.2576;Inherit;False;SimplePosterize;-1;;2195;163fbd1f7d6893e4ead4288913aedc26;0;2;9;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;551;-646.3391,-477.2158;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4236;-2112.304,101.583;Inherit;False;4264;LightDir;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;4265;-2087.153,-40.49116;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;4299;2112.15,-616.8647;Inherit;False;601.5642;321.6143;;4;4298;4297;4294;4296;Final Mask;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;487;1392.153,-902.3915;Inherit;False;surfaceMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4200;1725.967,-553.4279;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;4266;-1899.537,-13.03955;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4268;-1909.485,87.2591;Inherit;False;1927;worldNormals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;4276;-1965.365,164.2637;Inherit;False;Constant;_Vector0;Vector 0;22;0;Create;True;0;0;0;False;0;False;1,0.99,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;552;-509.6927,-477.2604;Inherit;False;ShadingMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;684;-292.3336,-140.3656;Inherit;False;1461.293;483.7008;;14;4288;4289;141;143;1976;4300;4006;200;201;140;481;4537;707;557;Light Radius Mix;1,0.4198113,0.7623972,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;555;1876.57,-552.6373;Inherit;False;GradientMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4296;2156.409,-418.4277;Inherit;False;487;surfaceMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4294;2139.463,-487.8464;Inherit;False;552;ShadingMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;4267;-1728.615,-11.04053;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4275;-1705.37,85.07172;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;4244;-1786.394,215.2414;Inherit;False;Constant;_SpecPower;Spec Power;43;0;Create;True;0;0;0;False;0;False;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4297;2358.666,-551.0913;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;4231;-1539.638,52.24847;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;4282;-1528.802,160.2945;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;557;-289.8167,77.72614;Inherit;False;555;GradientMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;4547;-256.7654,-604.4515;Inherit;False;1195.04;419.7061;;11;4548;4529;4531;4528;4527;4530;4535;4534;4533;4532;4862;Halo;0,0.9419041,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4298;2502.125,-549.413;Inherit;False;FinalLightMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;4280;-1325.337,162.5448;Inherit;False;200;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;4230;-1378.217,51.97283;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4278;-1372.085,-37.45283;Inherit;False;Property;_SpecIntensity;Spec Intensity;35;0;Create;True;0;0;0;False;0;False;0.5;0.79;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;707;-85.45042,78.04494;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;4537;-240.7852,-104.7736;Inherit;True;Property;_GradientTexture;Gradient Texture;0;2;[NoScaleOffset];[SingleLineTexture];Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.GetLocalVarNode;4532;-244.6469,-300.284;Inherit;False;3835;SCALE;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4301;-1159.205,153.3743;Inherit;False;4298;FinalLightMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;4229;-1161.391,51.01678;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;4500;-1099.719,-37.48457;Inherit;False;2;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;481;74.40509,-81.76846;Inherit;True;Property;_GradientTexture1;GradientTexture1;1;3;[Header];[NoScaleOffset];[SingleLineTexture];Create;True;1;___Light Settings___;0;0;False;1;Space(10);False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;140;84.75879,108.7269;Inherit;False;Property;_LightTint;Light Tint;1;1;[HDR];Create;True;1;___Light Settings___;0;0;False;0;False;1,1,1,1;3.550702,5.723955,7.603524,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.VertexColorNode;201;338.9914,100.2544;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;4533;-75.64525,-367.3888;Inherit;False;Constant;_Float1;Float 1;23;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;4534;-76.67821,-301.1487;Inherit;False;0.1;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4274;-951.7147,25.90228;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4302;-947.5248,-44.41169;Inherit;False;Constant;_s;s;23;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;200;383.8732,-82.44735;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;4535;69.02787,-324.9747;Inherit;False;Property;_ParticleMesh;ParticleMesh;43;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;255;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4530;120.6909,-395.9944;Inherit;False;477;FlickerSize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4006;533.7668,-81.23064;Inherit;False;Alpha Split;-1;;2196;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.GetLocalVarNode;4300;516.4923,17.88296;Inherit;False;4298;FinalLightMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1976;539.9111,86.14977;Inherit;False;Constant;_intensityScale;intensityScale;20;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;4285;-798.2579,0.2761188;Inherit;False;Property;_SpecularHighlight;Specular Highlight;34;0;Create;True;0;0;0;False;1;Space(20);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4531;313.5437,-351.4328;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4527;256.7258,-489.5515;Inherit;False;539;ReconstructedPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4528;281.4258,-558.4515;Inherit;False;653;POSITION;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4529;288.9737,-424.355;Inherit;False;4516;ScreenPos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;143;726.2546,-15.83389;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4287;-531.461,2.621488;Inherit;False;Spec;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;3841;1198.329,-130.634;Inherit;False;1557.381;407.0168;;18;4585;4572;4587;2109;2108;2107;4553;3579;1901;657;1384;600;1897;3572;4793;4549;2676;4520;Final Mix;0.2959238,0.4695747,0.6603774,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;141;878.584,-80.82604;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4289;871.376,27.88382;Inherit;False;4287;Spec;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4862;501.7485,-425.7458;Inherit;False;HaloFunction;6;;2197;739bbcda129bbae47870a33d01fe91b1;0;5;69;FLOAT3;0,0,0;False;70;FLOAT3;0,0,0;False;72;FLOAT2;0,0;False;74;FLOAT;0;False;80;SAMPLER2D;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;4288;1048.366,-80.44186;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4520;1223.068,-18.70964;Inherit;False;4516;ScreenPos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;2676;1221.361,49.75436;Inherit;False;555;GradientMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4548;735.5288,-426.4152;Inherit;False;halo;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1892;-2269.525,-885.0829;Inherit;False;FlickerHue;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4549;1446.843,42.02893;Inherit;False;4548;halo;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;4793;1403.63,-78.38047;Inherit;False;AccurateColors;44;;2201;570575a1eb6bdc7409ed58545512a33b;0;3;12;FLOAT3;0,0,0;False;13;FLOAT2;0,0;False;15;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;600;1649.485,-78.28804;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;1897;1647.595,18.47083;Inherit;False;1892;FlickerHue;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;1384;1499.312,156.9453;Inherit;False;416;FlickerAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3572;1672.795,131.7761;Inherit;False;3571;DistanceFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;657;1829.148,-78.65127;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1901;1884.459,133.5524;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3579;1977.678,-78.75454;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;4553;2131.007,-79.12321;Inherit;False;Dithering;36;;2202;b490ae982132eea449373f63bf44f108;0;1;17;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;4587;2275.056,21.47349;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;4572;2408.482,20.62648;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;4585;2535.309,-82.11815;Inherit;False;Property;_Blendmode;Blendmode;48;0;Create;True;0;0;0;False;1;Space(15);False;0;0;0;True;;KeywordEnum;3;Additive;Contrast;Negative;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;4906;1844.339,-1081.256;Inherit;False;940.1803;354.4319;;4;4615;3578;4904;2106;Output;1,0,0.8522549,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4905;3266.991,-90.47872;Inherit;False;FinalColor;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;4615;1894.339,-1031.256;Inherit;False;297;283;BehindTheScene;3;4617;4616;4561;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;4893;2786.192,3.429039;Inherit;False;648.5369;595.7183;Dev Debugging;10;4903;4902;4901;4900;4899;4898;4897;4896;4895;4894;;1,0.3537736,0.3537736,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;4904;2233.82,-990.507;Inherit;False;4905;FinalColor;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StickyNoteNode;486;-3885.901,-374.273;Inherit;False;371.5999;141.3;Particle Custom Vertex stream setup !!;;1,0.9012449,0.3254717,1;1. Center = TexCoord0.xyz  (Particle Position)$$2. StableRandom.x TexCoord0.w (random flicker)$$3. Size.xyz = TexCoord1.xyz (Particle Size);0;0
Node;AmplifyShaderEditor.StickyNoteNode;709;-4461.079,-779.02;Inherit;False;215;182;Center (Texcoord0.xyz);;1,1,1,1;;0;0
Node;AmplifyShaderEditor.StickyNoteNode;711;-4555.474,-386.069;Inherit;False;216;177;Size.xyz (Texcoord1.xyz);;1,1,1,1;;0;0
Node;AmplifyShaderEditor.StickyNoteNode;3832;-3307.867,-904.8484;Inherit;False;232.9993;214.9999;Random (Texcoord0.w);;1,1,1,1;;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;4894;2836.192,53.42905;Inherit;False;539;ReconstructedPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4895;2840.658,122.689;Inherit;False;1927;worldNormals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4896;2843.724,189.1473;Inherit;False;552;ShadingMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4897;2875.985,254.4704;Inherit;False;3969;noise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4898;2815.506,317.6464;Inherit;False;1892;FlickerHue;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4899;2817.854,395.1371;Inherit;False;416;FlickerAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4900;2935.477,452.0976;Inherit;False;4287;Spec;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4901;2876.309,516.8113;Inherit;False;1881;ScreenSpaceShadows;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4903;2990.854,339.1371;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;4617;1914.272,-839.8246;Inherit;False;Property;_DstBlend;DstBlend;51;2;[HideInInspector];[IntRange];Create;True;0;3;Default;0;Off;1;On;2;1;UnityEngine.Rendering.BlendMode;True;0;False;1;1;0;12;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4616;1913.272,-910.8247;Inherit;False;Property;_SrcBlend;SrcBlend;50;2;[HideInInspector];[IntRange];Create;True;0;3;Default;0;Off;1;On;2;1;UnityEngine.Rendering.BlendMode;True;0;False;1;1;0;12;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4561;1959.339,-981.2559;Inherit;False;Property;_DepthWrite;Depth Write;49;1;[Enum];Create;True;0;3;Default;0;Off;1;On;2;0;True;1;Space(5);False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;3578;2405.068,-989.8336;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StaticSwitch;4902;3132.728,40.18596;Inherit;False;Property;_DEBUGMODE;DEBUG MODE /!\;53;0;Create;True;0;0;0;False;1;Space(5);False;0;0;0;True;;KeywordEnum;8;OFF;ReconstructedPosition;DepthNormals;ShadowMask;Noise;Flicker;Specular;ScreenSpaceShadows;Create;True;True;Fragment;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2105;1519.678,2379.429;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;3;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2107;2560,208;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;3;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2108;2560,208;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;3;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2109;2560,208;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;3;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2106;2542.52,-988.8693;Float;False;True;-1;2;FPL.CustomMaterialEditor;0;13;LazyEti/URP/FakePointLight;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;1;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Overlay=RenderType;Queue=Overlay=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;True;True;1;1;True;_SrcBlend;1;True;_DstBlend;0;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;True;2;True;_DepthWrite;True;7;False;;True;True;0;False;;0;False;;True;1;LightMode=UniversalForwardOnly;False;False;7;Define;REQUIRE_DEPTH_TEXTURE 1;False;;Custom;False;0;0;;Include;;False;;Native;False;0;0;;Custom;#ifdef STEREO_INSTANCING_ON;False;;Custom;False;0;0;;Custom;TEXTURE2D_ARRAY(_CameraNormalsTexture)@ SAMPLER(sampler_CameraNormalsTexture)@;False;;Custom;False;0;0;;Custom;#else;False;;Custom;False;0;0;;Custom;TEXTURE2D(_CameraNormalsTexture)@ SAMPLER(sampler_CameraNormalsTexture)@;False;;Custom;False;0;0;;Custom;#endif;False;;Custom;False;0;0;;Hidden/InternalErrorShader;0;0;Standard;22;Surface;1;638684424641455607;  Blend;0;0;Two Sided;1;0;Alpha Clipping;0;638684424561819053;  Use Shadow Threshold;0;0;Cast Shadows;0;638684424595876409;Receive Shadows;0;638684424611325586;GPU Instancing;1;0;LOD CrossFade;0;0;Built-in Fog;0;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Vertex Position,InvertActionOnDeselection;1;0;0;5;False;True;False;False;False;False;;True;0
WireConnection;1913;1;1914;0
WireConnection;1913;0;742;4
WireConnection;4591;0;3834;0
WireConnection;4591;1;260;0
WireConnection;3727;0;1913;0
WireConnection;4498;29;3727;0
WireConnection;3833;1;3834;0
WireConnection;3833;0;4591;0
WireConnection;255;1;3814;0
WireConnection;255;0;252;0
WireConnection;416;0;4498;0
WireConnection;467;0;463;0
WireConnection;3887;0;3833;0
WireConnection;4515;0;4514;0
WireConnection;653;0;255;0
WireConnection;466;0;416;0
WireConnection;466;3;467;0
WireConnection;3886;0;3887;0
WireConnection;3886;1;3887;1
WireConnection;4516;0;4515;0
WireConnection;4874;0;4964;0
WireConnection;477;0;466;0
WireConnection;3888;0;3886;0
WireConnection;3888;1;3887;2
WireConnection;539;0;4874;0
WireConnection;4792;17;3821;0
WireConnection;3835;0;3888;0
WireConnection;2275;0;4518;0
WireConnection;3819;0;539;0
WireConnection;3819;1;654;0
WireConnection;4467;0;4792;0
WireConnection;4467;1;4569;0
WireConnection;4875;0;2275;0
WireConnection;4107;0;3819;0
WireConnection;3708;0;478;0
WireConnection;3571;0;4467;0
WireConnection;1927;0;4875;0
WireConnection;55;0;4107;0
WireConnection;3966;0;3836;0
WireConnection;3966;1;3708;0
WireConnection;262;0;55;0
WireConnection;262;1;3966;0
WireConnection;4235;0;4234;0
WireConnection;3808;0;4235;0
WireConnection;3954;0;262;0
WireConnection;4619;21;1929;0
WireConnection;4619;1;542;0
WireConnection;4619;60;3729;0
WireConnection;4907;337;3830;0
WireConnection;4907;336;3540;0
WireConnection;4907;370;3838;0
WireConnection;4907;335;3576;0
WireConnection;3981;0;3954;0
WireConnection;4624;0;66;0
WireConnection;1881;0;4907;0
WireConnection;3969;0;4619;0
WireConnection;4264;0;3808;0
WireConnection;3984;0;3981;0
WireConnection;3712;0;4624;0
WireConnection;436;0;4264;0
WireConnection;436;1;2274;0
WireConnection;3952;0;3984;0
WireConnection;3711;0;3712;0
WireConnection;509;0;3981;0
WireConnection;509;1;3971;0
WireConnection;4595;0;436;0
WireConnection;4595;1;553;0
WireConnection;4595;2;1882;0
WireConnection;3958;0;3952;0
WireConnection;769;0;3711;0
WireConnection;514;0;3981;0
WireConnection;514;1;509;0
WireConnection;4215;0;492;0
WireConnection;4219;0;4595;0
WireConnection;3902;0;3958;0
WireConnection;745;0;514;0
WireConnection;745;1;3711;0
WireConnection;745;2;769;0
WireConnection;4598;0;4219;0
WireConnection;4598;2;549;0
WireConnection;4214;0;745;0
WireConnection;4213;9;4598;0
WireConnection;4213;8;4216;0
WireConnection;3992;0;4214;0
WireConnection;3992;1;3896;0
WireConnection;562;0;4213;0
WireConnection;562;1;471;0
WireConnection;485;1;3984;0
WireConnection;4203;9;3992;0
WireConnection;4203;8;4215;0
WireConnection;551;0;562;0
WireConnection;487;0;485;0
WireConnection;4200;0;4214;0
WireConnection;4200;1;4203;0
WireConnection;4266;0;4265;0
WireConnection;4266;1;4236;0
WireConnection;552;0;551;0
WireConnection;555;0;4200;0
WireConnection;4267;0;4266;0
WireConnection;4275;0;4268;0
WireConnection;4275;1;4276;0
WireConnection;4297;0;555;0
WireConnection;4297;1;4294;0
WireConnection;4297;2;4296;0
WireConnection;4231;0;4267;0
WireConnection;4231;1;4275;0
WireConnection;4282;0;4244;0
WireConnection;4298;0;4297;0
WireConnection;4280;0;4282;0
WireConnection;4230;0;4231;0
WireConnection;707;0;557;0
WireConnection;4229;0;4230;0
WireConnection;4229;1;4280;0
WireConnection;4500;0;4278;0
WireConnection;481;0;4537;0
WireConnection;481;1;707;0
WireConnection;4534;0;4532;0
WireConnection;4274;0;4500;0
WireConnection;4274;1;4229;0
WireConnection;4274;2;4301;0
WireConnection;200;0;481;0
WireConnection;200;1;140;0
WireConnection;200;2;201;0
WireConnection;4535;1;4533;0
WireConnection;4535;0;4534;0
WireConnection;4006;2;200;0
WireConnection;4285;1;4302;0
WireConnection;4285;0;4274;0
WireConnection;4531;0;4530;0
WireConnection;4531;1;4535;0
WireConnection;143;0;4006;6
WireConnection;143;1;4300;0
WireConnection;143;2;1976;0
WireConnection;4287;0;4285;0
WireConnection;141;0;4006;0
WireConnection;141;1;143;0
WireConnection;4862;69;4528;0
WireConnection;4862;70;4527;0
WireConnection;4862;72;4529;0
WireConnection;4862;74;4531;0
WireConnection;4862;80;4537;0
WireConnection;4288;0;141;0
WireConnection;4288;1;4289;0
WireConnection;4548;0;4862;0
WireConnection;1892;0;4498;45
WireConnection;4793;12;4288;0
WireConnection;4793;13;4520;0
WireConnection;4793;15;2676;0
WireConnection;600;0;4793;0
WireConnection;600;1;4549;0
WireConnection;657;0;600;0
WireConnection;657;1;1897;0
WireConnection;1901;0;3572;0
WireConnection;1901;1;1384;0
WireConnection;3579;0;657;0
WireConnection;3579;1;1901;0
WireConnection;4553;17;3579;0
WireConnection;4587;0;4553;0
WireConnection;4572;0;4587;0
WireConnection;4585;1;4553;0
WireConnection;4585;0;4553;0
WireConnection;4585;2;4572;0
WireConnection;4905;0;4585;0
WireConnection;4903;0;4898;0
WireConnection;4903;1;4899;0
WireConnection;3578;0;4904;0
WireConnection;4902;1;4585;0
WireConnection;4902;0;4894;0
WireConnection;4902;2;4895;0
WireConnection;4902;3;4896;0
WireConnection;4902;4;4897;0
WireConnection;4902;5;4903;0
WireConnection;4902;6;4900;0
WireConnection;4902;7;4901;0
WireConnection;2106;2;3578;0
ASEEND*/
//CHKSM=FB77A01E6FE752CD2E467276877FBDA7485A2255