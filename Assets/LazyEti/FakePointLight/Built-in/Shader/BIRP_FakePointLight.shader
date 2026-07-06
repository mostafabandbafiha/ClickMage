// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "LazyEti/BIRP/FakePointLight"
{
	Properties
	{
		[NoScaleOffset][SingleLineTexture]_GradientTexture("Gradient Texture", 2D) = "white" {}
		[HDR]_LightTint("Light Tint", Color) = (1,1,1,1)
		[Space(5)]_LightSoftness("Light Softness", Range( 0 , 1)) = 1
		[IntRange]_LightPosterize("Light Posterize", Range( 0 , 128)) = 1
		[Space(5)]_ShadingBlend("Shading Blend", Range( 0 , 1)) = 0.5
		_ShadingSoftness("Shading Softness", Range( 0.01 , 1)) = 0.01
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
		[Space(15)][Toggle(_DAYFADING_ON)] _DayFading("Day Fading", Float) = 0
		[Space(15)][KeywordEnum(Additive,Contrast,Negative)] _Blendmode("Blendmode", Float) = 0
		[Enum(Default,0,Off,1,On,2)][Space(5)]_DepthWrite("Depth Write", Float) = 0
		[HideInInspector][IntRange]_SrcBlend("SrcBlend", Range( 0 , 12)) = 1
		[HideInInspector][IntRange]_DstBlend("DstBlend", Range( 0 , 12)) = 1
		[HideInInspector]_RandomOffset("RandomOffset", Range( 0 , 1)) = 0

	}

	SubShader
	{
		

		Tags { "RenderType"="Overlay" "Queue"="Overlay" }
	LOD 100

		CGINCLUDE
		#pragma target 3.5
		ENDCG
		Blend [_SrcBlend] [_DstBlend], SrcAlpha One
		AlphaToMask Off
		Cull Front
		ColorMask RGBA
		ZWrite [_DepthWrite]
		ZTest Always
		Offset 1000 , 2000
		

		
		Pass
		{
			Name "Unlit"

			CGPROGRAM

			#define ASE_VERSION 19801
			#define ASE_USING_SAMPLING_MACROS 1


			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "UnityStandardBRDF.cginc"
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _BLENDMODE_ADDITIVE _BLENDMODE_CONTRAST _BLENDMODE_NEGATIVE
			#pragma shader_feature_local _DITHERINGPATTERN_ON
			#pragma shader_feature_local _PARTICLEMODE_ON
			#pragma shader_feature_local ___FLICKERING____ON
			#pragma shader_feature_local ___NOISE____ON
			#pragma shader_feature_local _TEXTUREPACKING_RED _TEXTUREPACKING_REDXGREEN _TEXTUREPACKING_ALPHA
			#pragma shader_feature_local _SCREENSHADOWS_OFF _SCREENSHADOWS_LOW _SCREENSHADOWS_MEDIUM _SCREENSHADOWS_HIGH _SCREENSHADOWS_INSANE
			#pragma shader_feature_local _DAYFADING_ON
			#pragma shader_feature_local _SPECULARHIGHLIGHT_ON
			#pragma shader_feature_local ___HALO____ON
			#ifdef STEREO_INSTANCING_ON
			UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(_CameraDepthNormalsTexture);
			#else
			UNITY_DECLARE_TEX2D_NOSAMPLER(_CameraDepthNormalsTexture);
			#endif
			SamplerState sampler_CameraDepthNormalsTexture;
			#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
			#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
			#define SAMPLE_TEXTURE2D_LOD(tex,samplerTex,coord,lod) tex.SampleLevel(samplerTex,coord, lod)
			#define SAMPLE_TEXTURE2D_BIAS(tex,samplerTex,coord,bias) tex.SampleBias(samplerTex,coord,bias)
			#define SAMPLE_TEXTURE2D_GRAD(tex,samplerTex,coord,ddx,ddy) tex.SampleGrad(samplerTex,coord,ddx,ddy)
			#else//ASE Sampling Macros
			#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
			#define SAMPLE_TEXTURE2D_LOD(tex,samplerTex,coord,lod) tex2Dlod(tex,float4(coord,0,lod))
			#define SAMPLE_TEXTURE2D_BIAS(tex,samplerTex,coord,bias) tex2Dbias(tex,float4(coord,0,bias))
			#define SAMPLE_TEXTURE2D_GRAD(tex,samplerTex,coord,ddx,ddy) tex2Dgrad(tex,coord,ddx,ddy)
			#endif//ASE Sampling Macros
			


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
				#endif
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			//This is a late directive
			
			uniform float _SrcBlend;
			uniform float _DstBlend;
			uniform float _DepthWrite;
			UNITY_DECLARE_TEX2D_NOSAMPLER(_GradientTexture);
			uniform float _LightSoftness;
			UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
			uniform float4 _CameraDepthTexture_TexelSize;
			uniform float _FlickerSpeed;
			uniform float _RandomOffset;
			uniform float _FlickerSoftness;
			uniform float _FlickerIntensity;
			uniform float _SizeFlickering;
			UNITY_DECLARE_TEX2D_NOSAMPLER(_NoiseTexture);
			uniform float _NoiseScale;
			uniform float _NoiseMovement;
			SamplerState sampler_NoiseTexture;
			uniform float _Noisiness;
			uniform float _LightPosterize;
			SamplerState sampler_GradientTexture;
			uniform float4 _LightTint;
			uniform float _ShadingSoftness;
			uniform float _ShadowThreshold;
			uniform float DistanceFade;
			uniform float _FarFade;
			uniform float _FarTransition;
			uniform float _CloseFade;
			uniform float _CloseTransition;
			uniform float _ShadingBlend;
			uniform float _SpecIntensity;
			uniform float _HaloSize;
			uniform float _HaloPosterize;
			uniform float4 _HaloTint;
			uniform float _HaloDepthFade;
			uniform float3 _FlickerHue;
			uniform float _DitherIntensity;
			float4x4 Inverse4x4(float4x4 input)
			{
				#define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
				float4x4 cofactors = float4x4(
				minor( _22_23_24, _32_33_34, _42_43_44 ),
				-minor( _21_23_24, _31_33_34, _41_43_44 ),
				minor( _21_22_24, _31_32_34, _41_42_44 ),
				-minor( _21_22_23, _31_32_33, _41_42_43 ),
			
				-minor( _12_13_14, _32_33_34, _42_43_44 ),
				minor( _11_13_14, _31_33_34, _41_43_44 ),
				-minor( _11_12_14, _31_32_34, _41_42_44 ),
				minor( _11_12_13, _31_32_33, _41_42_43 ),
			
				minor( _12_13_14, _22_23_24, _42_43_44 ),
				-minor( _11_13_14, _21_23_24, _41_43_44 ),
				minor( _11_12_14, _21_22_24, _41_42_44 ),
				-minor( _11_12_13, _21_22_23, _41_42_43 ),
			
				-minor( _12_13_14, _22_23_24, _32_33_34 ),
				minor( _11_13_14, _21_23_24, _31_33_34 ),
				-minor( _11_12_14, _21_22_24, _31_32_34 ),
				minor( _11_12_13, _21_22_23, _31_32_33 ));
				#undef minor
				return transpose( cofactors ) / determinant( input );
			}
			
			float noise58_g1436( float x )
			{
				float n = sin (2 * x) + sin(3.14159265 * x);
				return n;
			}
			
			float4 NormalTex4805( float2 uvs )
			{
				#ifdef STEREO_INSTANCING_ON
				return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture,uvs);
				#else
				return SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture,sampler_CameraDepthNormalsTexture,uvs);
				//return tex2D(_CameraDepthNormalsTexture,uvs);
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
			
			float ExperimentalScreenShadowsBirp( float2 lightDirScreen, float threshold, float stepsSpace, float stepsNum, float radius, float mask, float3 wPos, float3 lightPos, float3 camPos, float2 screenPos, float3 offsetDir )
			{
				if(mask<=0) return 1;
				float depth = SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, screenPos );
				depth = 1/(_ZBufferParams.z * depth + _ZBufferParams.w);
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
					float d = SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, uvs );
					float l = 1/(_ZBufferParams.z * d + _ZBufferParams.w);
					if(MaxDist < l) return 1;
				#ifdef UNITY_REVERSED_Z
				d = 1- d;
				#endif
				float4 ndcPos = float4(uvs,d , 1.0)*2.0 -1.0;
				ndcPos.w =1;
				float4 viewPos = mul(unity_CameraInvProjection, ndcPos);
				viewPos /= viewPos.w; // Perspective divide
				viewPos.z *= -1;
				float3 world = mul(unity_CameraToWorld, viewPos).xyz;
					//float3 world = camPos - ((dirCompute.xyz/dirCompute.w) * l);
					if(distance(wPos,lightPos) > radius) return 1; //remove out of range artifacts
					if(shadow>=1) break; 
					if(world.y - wPos.y> threshold * MaxDist && abs(world.y-wPos.y) < radius)  shadow  += op;
				}
				shadow = step (0.01, shadow)*mask;
				return (1- shadow);
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
			


			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float4 ase_positionCS = UnityObjectToClipPos( v.vertex );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord1 = screenPos;
				float3 ase_objectPosition = UNITY_MATRIX_M._m03_m13_m23;
				#ifdef _PARTICLEMODE_ON
				float3 staticSwitch255 = v.ase_texcoord.xyz;
				#else
				float3 staticSwitch255 = ase_objectPosition;
				#endif
				float3 POSITION653 = staticSwitch255;
				float3 _Vector0 = float3(1,0,1);
				float Dist41_g1919 = distance( ( POSITION653 * _Vector0 ) , ( _Vector0 * _WorldSpaceCameraPos ) );
				float vertexToFrag49_g1919 = ( saturate( ( 1.0 - ( ( Dist41_g1919 - _FarFade ) / _FarTransition ) ) ) * saturate( ( ( Dist41_g1919 - _CloseFade ) / _CloseTransition ) ) );
				o.ase_texcoord3.w = vertexToFrag49_g1919;
				float3 ase_positionWS = mul( unity_ObjectToWorld, float4( ( v.vertex ).xyz, 1 ) ).xyz;
				float3 worldSpaceLightDir = UnityWorldSpaceLightDir( ase_positionWS );
				float dotResult3_g1918 = dot( -worldSpaceLightDir , float3( 0,1,0 ) );
				float vertexToFrag9_g1918 = saturate( ( dotResult3_g1918 * 4.0 ) );
				o.ase_texcoord4.x = vertexToFrag9_g1918;
				#ifdef _PARTICLEMODE_ON
				float staticSwitch1913 = v.ase_texcoord.w;
				#else
				float staticSwitch1913 = _RandomOffset;
				#endif
				float RANDOMNESS3727 = staticSwitch1913;
				float temp_output_29_0_g1436 = RANDOMNESS3727;
				float mulTime17_g1436 = _Time.y * ( ( _FlickerSpeed + ( temp_output_29_0_g1436 * 0.1 ) ) * 4 );
				float x58_g1436 = ( mulTime17_g1436 + ( temp_output_29_0_g1436 * UNITY_PI ) );
				float localnoise58_g1436 = noise58_g1436( x58_g1436 );
				float temp_output_44_0_g1436 = ( ( 1.0 - _FlickerSoftness ) * 0.5 );
				#ifdef ___FLICKERING____ON
				float staticSwitch53_g1436 = saturate( (( 1.0 - _FlickerIntensity ) + ((0.0 + (localnoise58_g1436 - -2.0) * (1.0 - 0.0) / (2.0 - -2.0)) - ( 1.0 - temp_output_44_0_g1436 )) * (1.0 - ( 1.0 - _FlickerIntensity )) / (temp_output_44_0_g1436 - ( 1.0 - temp_output_44_0_g1436 ))) );
				#else
				float staticSwitch53_g1436 = 1.0;
				#endif
				float FlickerAlpha416 = staticSwitch53_g1436;
				float FlickerSize477 = (( 1.0 - _SizeFlickering ) + (FlickerAlpha416 - 0.0) * (1.0 - ( 1.0 - _SizeFlickering )) / (1.0 - 0.0));
				float3 ase_objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
				#ifdef _PARTICLEMODE_ON
				float3 staticSwitch3833 = ( ase_objectScale * v.ase_texcoord1.xyz );
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
				float vertexToFrag32_g1945 = ( ( _HaloSize * ( FlickerSize477 * staticSwitch4535 ) ) * 0.5 );
				o.ase_texcoord4.y = vertexToFrag32_g1945;
				float3 pos75_g1945 = POSITION653;
				float vertexToFrag15_g1948 = ( unity_OrthoParams.w == 0.0 ? ( distance( _WorldSpaceCameraPos , pos75_g1945 ) / -UNITY_MATRIX_P[ 1 ][ 1 ] ) : unity_OrthoParams.y );
				o.ase_texcoord4.z = vertexToFrag15_g1948;
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - ase_positionWS );
				float3 ase_viewDirSafeWS = Unity_SafeNormalize( ase_viewVectorWS );
				float dotResult31_g1945 = dot( ase_viewDirSafeWS , ( _WorldSpaceCameraPos - pos75_g1945 ) );
				float vertexToFrag36_g1945 = step( 0.0 , dotResult31_g1945 );
				o.ase_texcoord4.w = vertexToFrag36_g1945;
				float vertexToFrag62_g1945 = distance( _WorldSpaceCameraPos , pos75_g1945 );
				o.ase_texcoord5.x = vertexToFrag62_g1945;
				float3 temp_cast_0 = (1.0).xxx;
				float3 lerpResult51_g1436 = lerp( _FlickerHue , temp_cast_0 , ( staticSwitch53_g1436 * staticSwitch53_g1436 ));
				float3 vertexToFrag67_g1436 = lerpResult51_g1436;
				o.ase_texcoord5.yzw = vertexToFrag67_g1436;
				
				o.ase_texcoord2 = v.ase_texcoord;
				o.ase_texcoord3.xyz = v.ase_texcoord1.xyz;
				o.ase_color = v.color;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);

				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				#endif
				return o;
			}

			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				float temp_output_3711_0 = ( ( 1.0 - ( _LightSoftness * 1.1 ) ) * 0.5 );
				float4 screenPos = i.ase_texcoord1;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float depthLinearEye6_g1845 = LinearEyeDepth( SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_positionSSNorm.xy ) );
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - WorldPosition );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				float4x4 invertVal5_g1846 = Inverse4x4( unity_ObjectToWorld );
				float dotResult4_g1845 = dot( ase_viewDirWS , -mul( unity_ObjectToWorld, float4( (transpose( mul( invertVal5_g1846, UNITY_MATRIX_I_V ) )[2]).xyz , 0.0 ) ).xyz );
				float3 worldToView72_g1845 = mul( UNITY_MATRIX_V, float4( WorldPosition, 1 ) ).xyz;
				float depth01_65_g1845 = SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_positionSSNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch68_g1845 = ( 1.0 - depth01_65_g1845 );
				#else
				float staticSwitch68_g1845 = depth01_65_g1845;
				#endif
				float lerpResult69_g1845 = lerp( _ProjectionParams.y , _ProjectionParams.z , staticSwitch68_g1845);
				float3 appendResult73_g1845 = (float3(worldToView72_g1845.xy , -lerpResult69_g1845));
				float3 viewToWorld74_g1845 = mul( UNITY_MATRIX_I_V, float4( appendResult73_g1845, 1.0 ) ).xyz;
				float3 ReconstructedPos539 = ( unity_OrthoParams.w < 1.0 ? ( ( depthLinearEye6_g1845 * ( ase_viewDirWS / dotResult4_g1845 ) ) + _WorldSpaceCameraPos ) : viewToWorld74_g1845 );
				float3 ase_objectPosition = UNITY_MATRIX_M._m03_m13_m23;
				#ifdef _PARTICLEMODE_ON
				float3 staticSwitch255 = i.ase_texcoord2.xyz;
				#else
				float3 staticSwitch255 = ase_objectPosition;
				#endif
				float3 POSITION653 = staticSwitch255;
				float3 LocalPos4107 = ( ReconstructedPos539 - POSITION653 );
				float3 ase_objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
				#ifdef _PARTICLEMODE_ON
				float3 staticSwitch3833 = ( ase_objectScale * i.ase_texcoord3.xyz );
				#else
				float3 staticSwitch3833 = ase_objectScale;
				#endif
				float3 break3887 = staticSwitch3833;
				float SCALE3835 = max( max( break3887.x , break3887.y ) , break3887.z );
				#ifdef _PARTICLEMODE_ON
				float staticSwitch1913 = i.ase_texcoord2.w;
				#else
				float staticSwitch1913 = _RandomOffset;
				#endif
				float RANDOMNESS3727 = staticSwitch1913;
				float temp_output_29_0_g1436 = RANDOMNESS3727;
				float mulTime17_g1436 = _Time.y * ( ( _FlickerSpeed + ( temp_output_29_0_g1436 * 0.1 ) ) * 4 );
				float x58_g1436 = ( mulTime17_g1436 + ( temp_output_29_0_g1436 * UNITY_PI ) );
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
				float3 UVS73_g1920 = ( ( ReconstructedPos539 * 0.1 ) * _NoiseScale );
				float2 ScreenPos4516 = (ase_positionSSNorm).xy;
				float2 uvs4805 = ScreenPos4516;
				float4 localNormalTex4805 = NormalTex4805( uvs4805 );
				float3 decodeViewNormalStereo4806 = DecodeViewNormalStereo( localNormalTex4805 );
				float3 viewToWorldDir4807 = mul( UNITY_MATRIX_I_V, float4( decodeViewNormalStereo4806, 0.0 ) ).xyz;
				float3 worldNormals1927 = viewToWorldDir4807;
				float3 temp_cast_3 = (3.0).xxx;
				float3 temp_output_141_0_g1920 = pow( abs( worldNormals1927 ) , temp_cast_3 );
				float3 temp_cast_4 = (1.0).xxx;
				float dotResult144_g1920 = dot( temp_output_141_0_g1920 , temp_cast_4 );
				float3 break147_g1920 = ( saturate( temp_output_141_0_g1920 ) / dotResult144_g1920 );
				float4 ase_positionSS_Pixel = ASEScreenPositionNormalizedToPixel( ase_positionSSNorm );
				float dither151_g1920 = Dither8x8Bayer( fmod( ase_positionSS_Pixel.x, 8 ), fmod( ase_positionSS_Pixel.y, 8 ) );
				float2 lerpResult19_g1920 = lerp( (UVS73_g1920).xz , ( (UVS73_g1920).yz * 0.9 ) , round( ( ( ( ( 1.0 - break147_g1920.x ) * break147_g1920.x ) * dither151_g1920 ) + break147_g1920.x ) ));
				float2 lerpResult20_g1920 = lerp( lerpResult19_g1920 , ( (UVS73_g1920).xy * 0.94 ) , round( ( break147_g1920.z + ( dither151_g1920 * ( break147_g1920.z * ( 1.0 - break147_g1920.z ) ) ) ) ));
				float temp_output_60_0_g1920 = RANDOMNESS3727;
				float mulTime41_g1920 = _Time.y * ( ( _NoiseMovement + ( temp_output_60_0_g1920 * 0.1 ) ) * 0.2 );
				float Time70_g1920 = ( mulTime41_g1920 + ( temp_output_60_0_g1920 * UNITY_PI ) );
				float4 tex2DNode3_g1920 = SAMPLE_TEXTURE2D( _NoiseTexture, sampler_NoiseTexture, ( lerpResult20_g1920 + ( Time70_g1920 * float2( 1.02,0.87 ) ) ) );
				#if defined( _TEXTUREPACKING_RED )
				float staticSwitch211_g1920 = tex2DNode3_g1920.r;
				#elif defined( _TEXTUREPACKING_REDXGREEN )
				float staticSwitch211_g1920 = tex2DNode3_g1920.g;
				#elif defined( _TEXTUREPACKING_ALPHA )
				float staticSwitch211_g1920 = tex2DNode3_g1920.a;
				#else
				float staticSwitch211_g1920 = tex2DNode3_g1920.r;
				#endif
				float4 tex2DNode14_g1920 = SAMPLE_TEXTURE2D( _NoiseTexture, sampler_NoiseTexture, ( ( lerpResult20_g1920 * 0.7 ) + ( Time70_g1920 * float2( -0.72,-0.67 ) ) ) );
				#if defined( _TEXTUREPACKING_RED )
				float staticSwitch212_g1920 = tex2DNode14_g1920.r;
				#elif defined( _TEXTUREPACKING_REDXGREEN )
				float staticSwitch212_g1920 = tex2DNode14_g1920.r;
				#elif defined( _TEXTUREPACKING_ALPHA )
				float staticSwitch212_g1920 = tex2DNode14_g1920.a;
				#else
				float staticSwitch212_g1920 = tex2DNode14_g1920.r;
				#endif
				#ifdef ___NOISE____ON
				float staticSwitch186_g1920 = ( 1.0 + ( ( staticSwitch211_g1920 * staticSwitch212_g1920 * _Noisiness ) - ( _Noisiness * 0.2 ) ) );
				#else
				float staticSwitch186_g1920 = 1.0;
				#endif
				float noise3969 = staticSwitch186_g1920;
				float smoothstepResult745 = smoothstep( temp_output_3711_0 , ( 1.0 - temp_output_3711_0 ) , ( temp_output_3981_0 * ( temp_output_3981_0 + noise3969 ) ));
				float lPosterize4215 = _LightPosterize;
				float temp_output_8_0_g1943 = lPosterize4215;
				float BrightnessBoost3902 = saturate( pow( temp_output_3981_0 , 30.0 ) );
				float temp_output_9_0_g1943 = ( smoothstepResult745 + BrightnessBoost3902 );
				float temp_output_5_0_g1943 = ( 256.0 / temp_output_8_0_g1943 );
				float GradientMask555 = ( smoothstepResult745 * ( temp_output_8_0_g1943 <= 0.0 ? temp_output_9_0_g1943 : saturate( ( floor( ( temp_output_9_0_g1943 * temp_output_5_0_g1943 ) ) / temp_output_5_0_g1943 ) ) ) );
				float2 temp_cast_5 = (( 1.0 - GradientMask555 )).xx;
				float4 temp_output_2_0_g1944 = ( SAMPLE_TEXTURE2D( _GradientTexture, sampler_GradientTexture, temp_cast_5 ) * _LightTint * i.ase_color );
				float temp_output_8_0_g1942 = lPosterize4215;
				float3 normalizeResult3808 = normalize( -LocalPos4107 );
				float3 LightDir4264 = normalizeResult3808;
				float dotResult436 = dot( LightDir4264 , worldNormals1927 );
				float3 pos338_g1933 = POSITION653;
				float rad280_g1933 = ( SCALE3835 * 0.5 );
				float temp_output_298_0_g1933 = ( 1.0 - ( abs( (pos338_g1933).y ) / rad280_g1933 ) );
				float3 normalizeResult278_g1933 = normalize( ( pos338_g1933 - _WorldSpaceCameraPos ) );
				float4x4 invertVal5_g1934 = Inverse4x4( unity_ObjectToWorld );
				float dotResult283_g1933 = dot( normalizeResult278_g1933 , -mul( unity_ObjectToWorld, float4( (transpose( mul( invertVal5_g1934, UNITY_MATRIX_I_V ) )[2]).xyz , 0.0 ) ).xyz );
				float temp_output_310_0_g1933 = ( temp_output_298_0_g1933 * dotResult283_g1933 );
				float4 worldToScreen279_g1933 = ComputeScreenPos( mul( UNITY_MATRIX_VP, float4(( pos338_g1933 * float3(1,0.1,1) ), 1.0 ) ) );
				float3 worldToScreen279_g1933NDC = worldToScreen279_g1933.xyz/worldToScreen279_g1933.w;
				float2 temp_output_344_0_g1933 = (( float4( worldToScreen279_g1933NDC , 0.0 ) - ase_positionSSNorm )).xy;
				float2 temp_output_384_0_g1933 = ( temp_output_310_0_g1933 * temp_output_344_0_g1933 );
				float2 lightDirScreen311_g1933 = temp_output_384_0_g1933;
				float temp_output_476_0_g1933 = ( _ShadowThreshold * 0.01 );
				float threshold311_g1933 = temp_output_476_0_g1933;
				float stepsSpace311_g1933 = 1.0;
				#if defined( _SCREENSHADOWS_OFF )
				float staticSwitch296_g1933 = -1.0;
				#elif defined( _SCREENSHADOWS_LOW )
				float staticSwitch296_g1933 = 16.0;
				#elif defined( _SCREENSHADOWS_MEDIUM )
				float staticSwitch296_g1933 = 32.0;
				#elif defined( _SCREENSHADOWS_HIGH )
				float staticSwitch296_g1933 = 64.0;
				#elif defined( _SCREENSHADOWS_INSANE )
				float staticSwitch296_g1933 = 128.0;
				#else
				float staticSwitch296_g1933 = -1.0;
				#endif
				float StepQual300_g1933 = staticSwitch296_g1933;
				float stepsNum311_g1933 = StepQual300_g1933;
				float radius311_g1933 = rad280_g1933;
				float vertexToFrag49_g1919 = i.ase_texcoord3.w;
				float vertexToFrag9_g1918 = i.ase_texcoord4.x;
				#ifdef _DAYFADING_ON
				float staticSwitch11_g1918 = vertexToFrag9_g1918;
				#else
				float staticSwitch11_g1918 = 1.0;
				#endif
				float DistanceFade3571 = ( (( DistanceFade )?( vertexToFrag49_g1919 ):( 1.0 )) * staticSwitch11_g1918 );
				float temp_output_335_0_g1933 = DistanceFade3571;
				float mask311_g1933 = temp_output_335_0_g1933;
				float3 worldPos353_g1933 = ReconstructedPos539;
				float3 wPos311_g1933 = worldPos353_g1933;
				float3 lightPos311_g1933 = pos338_g1933;
				float3 camPos311_g1933 = _WorldSpaceCameraPos;
				float2 screenPos311_g1933 = ase_positionSSNorm.xy;
				float3 offsetDir485_g1933 = ( dotResult283_g1933 * normalizeResult278_g1933 );
				float3 offsetDir311_g1933 = offsetDir485_g1933;
				float localExperimentalScreenShadowsBirp311_g1933 = ExperimentalScreenShadowsBirp( lightDirScreen311_g1933 , threshold311_g1933 , stepsSpace311_g1933 , stepsNum311_g1933 , radius311_g1933 , mask311_g1933 , wPos311_g1933 , lightPos311_g1933 , camPos311_g1933 , screenPos311_g1933 , offsetDir311_g1933 );
				#if defined( _SCREENSHADOWS_OFF )
				float staticSwitch341_g1933 = 1.0;
				#elif defined( _SCREENSHADOWS_LOW )
				float staticSwitch341_g1933 = localExperimentalScreenShadowsBirp311_g1933;
				#elif defined( _SCREENSHADOWS_MEDIUM )
				float staticSwitch341_g1933 = localExperimentalScreenShadowsBirp311_g1933;
				#elif defined( _SCREENSHADOWS_HIGH )
				float staticSwitch341_g1933 = localExperimentalScreenShadowsBirp311_g1933;
				#elif defined( _SCREENSHADOWS_INSANE )
				float staticSwitch341_g1933 = localExperimentalScreenShadowsBirp311_g1933;
				#else
				float staticSwitch341_g1933 = 1.0;
				#endif
				float ScreenSpaceShadows1881 = staticSwitch341_g1933;
				float smoothstepResult4598 = smoothstep( 0.0 , _ShadingSoftness , saturate( ( dotResult436 * noise3969 * ScreenSpaceShadows1881 ) ));
				float temp_output_9_0_g1942 = smoothstepResult4598;
				float temp_output_5_0_g1942 = ( 256.0 / temp_output_8_0_g1942 );
				float ShadingMask552 = saturate( ( ( temp_output_8_0_g1942 <= 0.0 ? temp_output_9_0_g1942 : saturate( ( floor( ( temp_output_9_0_g1942 * temp_output_5_0_g1942 ) ) / temp_output_5_0_g1942 ) ) ) + _ShadingBlend ) );
				float surfaceMask487 = step( 0.01 , temp_output_3981_0 );
				float FinalLightMask4298 = ( GradientMask555 * ShadingMask552 * surfaceMask487 );
				float3 normalizeResult4267 = normalize( ( ase_viewDirWS + LightDir4264 ) );
				float dotResult4231 = dot( normalizeResult4267 , ( worldNormals1927 * float3(1,0.99,1) ) );
				#ifdef _SPECULARHIGHLIGHT_ON
				float staticSwitch4285 = ( ( _SpecIntensity * 2 ) * pow( saturate( dotResult4231 ) , ( (0.5*0.5 + 0.5) * 200 ) ) * FinalLightMask4298 );
				#else
				float staticSwitch4285 = 0.0;
				#endif
				float Spec4287 = staticSwitch4285;
				float3 temp_cast_10 = (0.0).xxx;
				float vertexToFrag32_g1945 = i.ase_texcoord4.y;
				float3 pos75_g1945 = POSITION653;
				float4 worldToScreen9_g1945 = ComputeScreenPos( mul( UNITY_MATRIX_VP, float4(pos75_g1945, 1.0 ) ) );
				float3 worldToScreen9_g1945NDC = worldToScreen9_g1945.xyz/worldToScreen9_g1945.w;
				float2 appendResult15_g1945 = (float2(( _ScreenParams.x / _ScreenParams.y ) , 1.0));
				float vertexToFrag15_g1948 = i.ase_texcoord4.z;
				float smoothstepResult33_g1945 = smoothstep( 0.0 , vertexToFrag32_g1945 , length( ( ( ( (worldToScreen9_g1945NDC).xy - ScreenPos4516 ) * appendResult15_g1945 ) * vertexToFrag15_g1948 ) ));
				float vertexToFrag36_g1945 = i.ase_texcoord4.w;
				float HaloMask38_g1945 = ( ( 1.0 - smoothstepResult33_g1945 ) * vertexToFrag36_g1945 );
				float temp_output_8_0_g1946 = _HaloPosterize;
				float temp_output_9_0_g1946 = HaloMask38_g1945;
				float temp_output_5_0_g1946 = ( 256.0 / temp_output_8_0_g1946 );
				float HaloPosterized43_g1945 = ( HaloMask38_g1945 * ( temp_output_8_0_g1946 <= 0.0 ? temp_output_9_0_g1946 : saturate( ( floor( ( temp_output_9_0_g1946 * temp_output_5_0_g1946 ) ) / temp_output_5_0_g1946 ) ) ) );
				float2 temp_cast_11 = (( 1.0 - HaloPosterized43_g1945 )).xx;
				float4 temp_output_2_0_g1947 = ( SAMPLE_TEXTURE2D( _GradientTexture, sampler_GradientTexture, temp_cast_11 ) * _HaloTint * i.ase_color );
				float vertexToFrag62_g1945 = i.ase_texcoord5.x;
				float HaloPenetrationMask68_g1945 = saturate( pow( saturate( ( distance( ReconstructedPos539 , _WorldSpaceCameraPos ) - vertexToFrag62_g1945 ) ) , _HaloDepthFade ) );
				#ifdef ___HALO____ON
				float3 staticSwitch54_g1945 = ( (temp_output_2_0_g1947).rgb * ( (temp_output_2_0_g1947).a * HaloMask38_g1945 * HaloPenetrationMask68_g1945 * HaloPosterized43_g1945 ) );
				#else
				float3 staticSwitch54_g1945 = temp_cast_10;
				#endif
				float3 halo4548 = staticSwitch54_g1945;
				float3 vertexToFrag67_g1436 = i.ase_texcoord5.yzw;
				float3 FlickerHue1892 = vertexToFrag67_g1436;
				float3 temp_output_17_0_g1949 = ( ( ( ( ( (temp_output_2_0_g1944).rgb * ( (temp_output_2_0_g1944).a * FinalLightMask4298 * 0.1 ) ) + Spec4287 ) + halo4548 ) * FlickerHue1892 ) * ( DistanceFade3571 * FlickerAlpha416 ) );
				float dither10_g1949 = Dither4x4Bayer( fmod( ase_positionSS_Pixel.x, 4 ), fmod( ase_positionSS_Pixel.y, 4 ) );
				float3 break3_g1949 = temp_output_17_0_g1949;
				float smoothstepResult9_g1949 = smoothstep( 0.0 , _DitherIntensity , ( 0.333 * ( break3_g1949.x + break3_g1949.y + break3_g1949.z ) * ( _DitherIntensity + 1.0 ) ));
				dither10_g1949 = step( dither10_g1949, saturate( smoothstepResult9_g1949 * 1.00001 ) );
				#ifdef _DITHERINGPATTERN_ON
				float3 staticSwitch12_g1949 = ( temp_output_17_0_g1949 * dither10_g1949 );
				#else
				float3 staticSwitch12_g1949 = temp_output_17_0_g1949;
				#endif
				float3 temp_output_4553_0 = staticSwitch12_g1949;
				#if defined( _BLENDMODE_ADDITIVE )
				float3 staticSwitch4585 = temp_output_4553_0;
				#elif defined( _BLENDMODE_CONTRAST )
				float3 staticSwitch4585 = temp_output_4553_0;
				#elif defined( _BLENDMODE_NEGATIVE )
				float3 staticSwitch4585 = ( 1.0 - saturate( temp_output_4553_0 ) );
				#else
				float3 staticSwitch4585 = temp_output_4553_0;
				#endif
				float3 FinalColor5024 = staticSwitch4585;
				float4 appendResult3578 = (float4(FinalColor5024 , 1.0));
				

				finalColor = appendResult3578;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "FPL.CustomMaterialEditor"
	
	Fallback Off
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.CommentaryNode;3728;-3339.878,-1045.967;Inherit;False;734.679;386.8545;;5;1914;742;3832;3727;1913;Random;0.6886792,0,0.67818,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;476;-4574.993,-894.2358;Inherit;False;1143.62;715.2286;;15;3834;4591;260;711;709;486;3835;3888;3886;3887;653;3833;255;252;3814;Particle transform;0.5424528,1,0.9184569,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;1914;-3302.18,-986.6736;Inherit;False;Property;_RandomOffset;RandomOffset;50;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;742;-3294.294,-865.4466;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;4517;-4672.879,-123.0072;Inherit;False;628.2538;236.657;screen pos;3;4516;4515;4514;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;260;-4548.069,-345.5797;Inherit;False;1;3;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ObjectScaleNode;3834;-4523.051,-532.8078;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.StaticSwitch;1913;-3048.042,-882.5033;Inherit;False;Property;_ParticleMesh;ParticleMesh;43;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;255;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;4514;-4649.879,-74.00718;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;484;-2586.187,-1047.923;Inherit;False;970.912;381.7733;;8;1892;477;466;467;416;463;4498;654;Flicker;0.5613208,0.8882713,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4591;-4327.826,-369.1419;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3727;-2813.076,-881.6541;Inherit;False;RANDOMNESS;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;4515;-4448.404,-72.46912;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ObjectPositionNode;3814;-4228.537,-841.8353;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TexCoordVertexDataNode;252;-4448.995,-741.957;Inherit;False;0;3;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;3833;-4188.361,-426.1765;Inherit;False;Property;_ParticleMesh;ParticleMesh;43;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;255;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;4498;-2565.921,-954.3461;Inherit;False;FlickerFunction;18;;1436;f6225b1ef66c663478bc4f0259ec00df;0;4;9;FLOAT;0;False;8;FLOAT;0;False;21;FLOAT;0;False;29;FLOAT;0;False;2;FLOAT;0;FLOAT3;45
Node;AmplifyShaderEditor.RangedFloatNode;463;-2436.796,-808.6651;Inherit;False;Property;_SizeFlickering;Size Flickering;24;0;Create;True;0;0;0;False;0;False;0.1;0.5;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;2273;-5100.651,152.9973;Inherit;False;1044.893;308.6611;;5;1927;4806;4805;4518;4807;NormalsTexture;0.5424528,1,0.8822392,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4516;-4253.856,-71.61797;Inherit;False;ScreenPos;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;255;-4025.325,-766.166;Inherit;False;Property;_ParticleMode;Particle Mode;43;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.BreakToComponentsNode;3887;-3947.627,-423.15;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RegisterLocalVarNode;416;-2267.689,-954.5582;Inherit;False;FlickerAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;467;-2163.736,-808.0251;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4518;-5055.706,240.8076;Inherit;False;4516;ScreenPos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;480;-1586.786,-1078.922;Inherit;False;1355.739;434.4033;;10;3954;4107;262;3966;55;3819;3708;539;478;3836;World SphericalMask;0.9034846,0.5330188,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;3573;-4019.903,-4.447549;Inherit;False;870.2667;343.3553;;5;3571;4467;3821;4792;4569;Distance Fading;0.4079299,0.8396226,0.4819806,1;0;0
Node;AmplifyShaderEditor.FunctionNode;4971;-1566.887,-1021.288;Inherit;False;Reconstruct World Pos from Depth VR;-1;;1845;474d2b03c8647914986393f8dfbd9fe4;0;0;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;653;-3777.223,-766.7069;Inherit;False;POSITION;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;3886;-3843.627,-423.15;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;466;-2013.38,-954.6299;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;4805;-4881.154,242.6219;Float;False;#ifdef STEREO_INSTANCING_ON$return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture,uvs)@$#else$return SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture,sampler_CameraDepthNormalsTexture,uvs)@$//return tex2D(_CameraDepthNormalsTexture,uvs)@$$#endif;4;Create;1;True;uvs;FLOAT2;0,0;In;;Inherit;False;NormalTex;True;False;0;;False;1;0;FLOAT2;0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;654;-1939.366,-791.3782;Inherit;False;653;POSITION;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;3888;-3742.952,-398.5054;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;477;-1827.317,-955.2652;Inherit;False;FlickerSize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3821;-3990.201,152.5596;Inherit;False;653;POSITION;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;539;-1228.561,-1021.273;Inherit;False;ReconstructedPos;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;3819;-1017.181,-1021.682;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3835;-3633.671,-397.8081;Inherit;False;SCALE;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;478;-931.2987,-775.1152;Inherit;False;477;FlickerSize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4569;-3627.232,248.7301;Inherit;False;DayAlpha;44;;1918;bc1f8ebe2e26696419e0099f8a3e27dc;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4792;-3808.967,56.07874;Inherit;False;AdvancedCameraFade;12;;1919;e6e830f789d28b746963801d61c2a1ec;0;6;40;FLOAT;0;False;46;FLOAT;0;False;47;FLOAT;0;False;48;FLOAT;0;False;17;FLOAT3;0,0,0;False;20;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DecodeViewNormalStereoHlpNode;4806;-4743.688,242.6837;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;442;-2529.175,-598.0592;Inherit;False;2247.203;410.7375;Be sure to have a renderer feature that writes to _CameraNormalsTexture for this to work;18;552;551;562;4213;4595;4219;4216;549;4598;471;1882;553;436;2274;4264;3808;4235;4234;Normal Direction Masking;0.6086246,0.5235849,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4107;-871.8685,-1021.94;Inherit;False;LocalPos;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ScaleNode;3708;-762.1542,-775.359;Inherit;False;0.45;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3836;-855.9872,-877.1537;Inherit;False;3835;SCALE;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4467;-3496.565,55.55975;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TransformDirectionNode;4807;-4468.164,241.4839;Inherit;False;View;World;False;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;513;-3363.908,-590.4105;Inherit;False;781.0093;390.9941;;4;3969;1929;3729;542;Noise;1,0.6084906,0.6084906,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;1356;-3111.287,-14.07189;Inherit;False;907.1592;385.8282;;5;1881;3576;3838;3540;3830;Experimental Shadows;1,0.0518868,0.0518868,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3966;-642.7975,-873.377;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LengthOpNode;55;-663.6138,-1022.612;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4234;-2486.791,-480.0054;Inherit;False;4107;LocalPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3571;-3359.174,57.45215;Inherit;False;DistanceFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1927;-4269.113,243.5302;Inherit;False;worldNormals;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;510;-125.0605,-1081.912;Inherit;False;1067.724;415.4808;;10;66;4624;769;3711;3712;745;514;509;3971;3981;Light Mask Hardness;1,0.8561655,0.3632075,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;542;-3313.745,-372.6483;Inherit;False;539;ReconstructedPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;3729;-3310.612,-298.886;Inherit;False;3727;RANDOMNESS;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1929;-3314.647,-449.2849;Inherit;False;1927;worldNormals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;262;-492.2797,-1022.586;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;4235;-2317.88,-480.5057;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;3830;-3027.767,81.16975;Inherit;False;653;POSITION;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;3576;-3055.692,271.4933;Inherit;False;3571;DistanceFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3838;-3026.032,208.7609;Inherit;False;3835;SCALE;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3540;-3057.828,146.3713;Inherit;False;539;ReconstructedPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;4619;-3084.65,-435.7374;Inherit;False;3DNoiseMap;25;;1920;2fca756491ec7bf4e9c71d18280c45cc;0;5;257;FLOAT3;0,0,0;False;21;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;60;FLOAT;0;False;229;FLOAT2;0,0;False;2;FLOAT;0;FLOAT;213
Node;AmplifyShaderEditor.OneMinusNode;3954;-380.211,-1022.654;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;66;-103.8148,-763.4788;Inherit;False;Property;_LightSoftness;Light Softness;2;0;Create;True;0;0;0;False;1;Space(5);False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;3808;-2170.026,-480.2162;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;5010;-2811.362,115.2712;Inherit;False;ExperimentalScreenSpaceShadows;40;;1933;79f826106fc5f154c96059cc1326b755;0;4;337;FLOAT3;0,0,0;False;336;FLOAT3;0,0,0;False;370;FLOAT;0;False;335;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;3968;1099.996,-1081.244;Inherit;False;675.3597;364.2049;Additional Masks;6;3902;487;485;3958;3952;3984;;0.3531061,0.406577,0.6509434,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3969;-2798.872,-440.3917;Inherit;False;noise;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;3981;3.173216,-1021.999;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;4624;167.0977,-764.9185;Inherit;False;1.1;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;2274;-2028.707,-404.0095;Inherit;False;1927;worldNormals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1881;-2464.868,115.6569;Inherit;False;ScreenSpaceShadows;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4264;-2029.805,-480.4308;Inherit;False;LightDir;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;500;962.7515,-619.572;Inherit;False;1109.27;328.2859;;8;555;4200;4203;3992;3896;4214;4215;492;Light Posterize;0.5707547,1,0.9954711,1;0;0
Node;AmplifyShaderEditor.RelayNode;3984;1130.76,-1015.626;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3971;55.14507,-891.7;Inherit;False;3969;noise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;3712;304.4139,-766.8303;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;436;-1826.206,-481.019;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;553;-1712.748,-422.1669;Inherit;False;3969;noise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1882;-1777.72,-349.5772;Inherit;False;1881;ScreenSpaceShadows;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;509;234.626,-914.4458;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;3952;1254.648,-1012.464;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;30;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;3711;438.8805,-766.8687;Inherit;False;0.5;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;492;990.0972,-391.6727;Inherit;False;Property;_LightPosterize;Light Posterize;3;1;[IntRange];Create;True;0;0;0;False;0;False;1;0;0;128;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4595;-1523.673,-482.6231;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;514;373.3369,-938.2104;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;3958;1388.301,-1012.406;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;769;599.5012,-769.8422;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4215;1264.353,-392.126;Inherit;False;lPosterize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;549;-1515.065,-362.8324;Inherit;False;Property;_ShadingSoftness;Shading Softness;5;0;Create;True;0;0;0;False;0;False;0.01;0.01;0.01;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;4219;-1384.505,-481.7147;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;3902;1553.364,-1010.963;Inherit;False;BrightnessBoost;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;745;732.6475,-938.0301;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;4598;-1235.66,-481.2795;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4216;-1224.634,-357.1521;Inherit;False;4215;lPosterize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;4214;1151.298,-550.8469;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3896;1069.695,-469.6752;Inherit;False;3902;BrightnessBoost;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;471;-1011.221,-359.3797;Inherit;False;Property;_ShadingBlend;Shading Blend;4;0;Create;True;0;0;0;False;1;Space(5);False;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4213;-1033.537,-481.7328;Inherit;False;SimplePosterize;-1;;1942;163fbd1f7d6893e4ead4288913aedc26;0;2;9;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;4286;-2104.033,-35.29148;Inherit;False;1843.076;430.4736;;20;4287;4285;4302;4274;4500;4229;4301;4278;4230;4280;4282;4231;4244;4275;4267;4276;4268;4266;4265;4236;Specular;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;3992;1299.5,-489.5305;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;562;-744.1313,-481.3555;Inherit;False;2;2;0;FLOAT;0.5;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;485;1259.213,-894.7515;Inherit;False;2;0;FLOAT;0.01;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4203;1440.282,-490.2576;Inherit;False;SimplePosterize;-1;;1943;163fbd1f7d6893e4ead4288913aedc26;0;2;9;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;4265;-2050.88,15.70839;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SaturateNode;551;-636.7248,-480.8353;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4236;-2054.031,156.7825;Inherit;False;4264;LightDir;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;4299;2103.049,-618.1647;Inherit;False;597.6643;321.6143;;4;4298;4297;4294;4296;Final Mask;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;487;1378.931,-894.3735;Inherit;False;surfaceMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4200;1722.03,-553.4279;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;4266;-1841.264,42.16;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4268;-1851.212,142.4586;Inherit;False;1927;worldNormals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;4276;-1907.092,219.4632;Inherit;False;Constant;_Vector0;Vector 0;22;0;Create;True;0;0;0;False;0;False;1,0.99,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;552;-500.0786,-480.8799;Inherit;False;ShadingMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;684;-214.2027,-140.3656;Inherit;False;1486.598;484.9661;;13;141;557;707;1976;143;200;140;201;481;4288;4300;4289;4537;Light Radius Mix;1,0.4198113,0.7623972,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;4296;2142.408,-422.7277;Inherit;False;487;surfaceMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;555;1865.633,-553.6373;Inherit;False;GradientMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;4267;-1670.342,44.15902;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4275;-1647.097,140.2712;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;4244;-1728.121,270.4409;Inherit;False;Constant;_SpecPower;Spec Power;43;0;Create;True;0;0;0;False;0;False;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4294;2109.462,-491.1464;Inherit;False;552;ShadingMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;4547;-256.7654,-604.4515;Inherit;False;1195.04;419.7061;;11;4548;5014;4529;4531;4528;4527;4530;4535;4534;4533;4532;Halo;0,0.9419041,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4297;2345.666,-552.3913;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;4231;-1481.365,107.448;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;4282;-1470.529,215.494;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;557;-211.6858,77.72614;Inherit;False;555;GradientMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4532;-244.6469,-300.284;Inherit;False;3835;SCALE;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;4537;-162.6543,-104.7736;Inherit;True;Property;_GradientTexture;Gradient Texture;0;2;[NoScaleOffset];[SingleLineTexture];Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ScaleNode;4280;-1267.064,217.7443;Inherit;False;200;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;4230;-1319.944,107.1723;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4278;-1313.812,17.74671;Inherit;False;Property;_SpecIntensity;Spec Intensity;35;0;Create;True;0;0;0;False;0;False;0.5;0.79;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;707;-7.319501,78.04494;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4298;2494.477,-548.0357;Inherit;False;FinalLightMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4533;-75.64525,-367.3888;Inherit;False;Constant;_Float1;Float 1;23;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;4534;-76.67821,-301.1487;Inherit;False;0.1;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4301;-1100.932,208.5738;Inherit;False;4298;FinalLightMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;4229;-1103.118,106.2163;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleNode;4500;-1041.446,17.71498;Inherit;False;2;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;481;152.536,-81.76846;Inherit;True;Property;_GradientTexture1;GradientTexture1;1;3;[Header];[NoScaleOffset];[SingleLineTexture];Create;True;1;___Light Settings___;0;0;False;1;Space(10);False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;140;162.8897,108.7269;Inherit;False;Property;_LightTint;Light Tint;1;1;[HDR];Create;True;1;___Light Settings___;0;0;False;0;False;1,1,1,1;3.550702,5.723955,7.603524,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.VertexColorNode;201;417.1223,100.2544;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;4535;69.02787,-324.9747;Inherit;False;Property;_ParticleMesh;ParticleMesh;43;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;255;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4530;120.6909,-395.9944;Inherit;False;477;FlickerSize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4274;-893.4418,81.10181;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4302;-889.2518,10.78786;Inherit;False;Constant;_s;s;23;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;200;462.0041,-82.44735;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;4527;236.7258,-485.5515;Inherit;False;539;ReconstructedPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4528;261.4258,-554.4515;Inherit;False;653;POSITION;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4531;313.5437,-351.4328;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;4529;268.9737,-420.355;Inherit;False;4516;ScreenPos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;4300;594.6234,17.88296;Inherit;False;4298;FinalLightMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1976;618.0422,86.14977;Inherit;False;Constant;_intensityScale;intensityScale;20;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;4285;-739.9849,55.47567;Inherit;False;Property;_SpecularHighlight;Specular Highlight;34;0;Create;True;0;0;0;False;1;Space(20);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;4810;611.8979,-81.23064;Inherit;False;Alpha Split;-1;;1944;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;143;804.3857,-15.83389;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4287;-473.1881,57.82103;Inherit;False;Spec;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;5014;507.7485,-440.7458;Inherit;False;HaloFunction;6;;1945;739bbcda129bbae47870a33d01fe91b1;0;5;69;FLOAT3;0,0,0;False;70;FLOAT3;0,0,0;False;72;FLOAT2;0,0;False;74;FLOAT;0;False;80;SAMPLER2D;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;3841;1307.8,-130.634;Inherit;False;1378.884;412.2429;;12;4585;4572;4587;4553;3579;657;1901;600;1897;3572;1384;4549;Final Mix;0.2959238,0.4695747,0.6603774,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;141;956.7151,-80.82604;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4289;955.4199,27.88382;Inherit;False;4287;Spec;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4548;741.5288,-441.4152;Inherit;False;halo;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;4288;1126.497,-80.44186;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1892;-2269.525,-885.0829;Inherit;False;FlickerHue;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;4549;1346.507,-0.2102308;Inherit;False;4548;halo;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;1384;1554.93,182.7225;Inherit;False;416;FlickerAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;3572;1525.319,105.8096;Inherit;False;3571;DistanceFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1897;1536.453,16.89042;Inherit;False;1892;FlickerHue;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;600;1538.343,-80.90653;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1901;1736.984,106.2923;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;657;1718.006,-81.26976;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3579;1866.537,-81.37303;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;4553;2019.866,-81.7417;Inherit;False;Dithering;36;;1949;b490ae982132eea449373f63bf44f108;0;1;17;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;4587;2200.373,14.03743;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;4572;2333.799,13.19042;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;4585;2452.253,-85.65208;Inherit;False;Property;_Blendmode;Blendmode;46;0;Create;True;0;0;0;False;1;Space(15);False;0;0;0;True;;KeywordEnum;3;Additive;Contrast;Negative;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;5032;1909.577,-1098.446;Inherit;False;1001.237;354.4316;;5;4800;3578;5025;5013;4615;Output;0.9657564,0,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;5024;3384.344,-90.04384;Inherit;False;FinalColor;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;5023;2709.598,-9.9309;Inherit;False;648.5369;595.7183;Dev Debugging;10;5030;5029;5018;5028;5027;5026;5017;5016;5031;5047;;1,0.3537736,0.3537736,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;4615;1959.577,-1048.446;Inherit;False;297;283;BehindTheScene;3;4617;4616;4561;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;5013;2379.045,-916.6129;Inherit;False;Constant;_Float3;Float 3;23;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;5025;2343.365,-992.0341;Inherit;False;5024;FinalColor;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;5027;2726.912,307.2863;Inherit;False;1892;FlickerHue;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;5029;2729.26,384.7771;Inherit;False;416;FlickerAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;5017;2767.13,175.787;Inherit;False;552;ShadingMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;5026;2799.391,241.1102;Inherit;False;3969;noise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5028;2902.26,328.7771;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;5031;2878.715,503.4514;Inherit;False;552;ShadingMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;5047;2754.175,34.5647;Inherit;False;539;ReconstructedPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;5030;2896.883,423.7375;Inherit;False;4287;Spec;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;5016;2755.064,104.3289;Inherit;False;1927;worldNormals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;5018;3056.134,26.82594;Inherit;False;Property;_DEBUGMODE;DEBUG MODE /!\;51;0;Create;True;0;0;0;False;1;Space(5);False;0;0;0;True;;KeywordEnum;8;OFF;ReconstructedPosition;DepthNormals;ShadowMask;Noise;Flicker;Specular;ScreenSpaceShadows;Create;True;True;Fragment;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StickyNoteNode;486;-4165.404,-623.873;Inherit;False;371.5999;141.3;Particle Custom Vertex stream setup !!;;1,0.9012449,0.3254717,1;1. Center = TexCoord0.xyz  (Particle Position)$$2. StableRandom.x TexCoord0.w (random flicker)$$3. Size.xyz = TexCoord1.xyz (Particle Size);0;0
Node;AmplifyShaderEditor.StickyNoteNode;709;-4461.079,-779.02;Inherit;False;215;182;Center (Texcoord0.xyz);;1,1,1,1;;0;0
Node;AmplifyShaderEditor.StickyNoteNode;711;-4555.474,-386.069;Inherit;False;216;177;Size.xyz (Texcoord1.xyz);;1,1,1,1;;0;0
Node;AmplifyShaderEditor.StickyNoteNode;3832;-3307.867,-904.8484;Inherit;False;232.9993;214.9999;Random (Texcoord0.w);;1,1,1,1;;0;0
Node;AmplifyShaderEditor.RangedFloatNode;4616;1978.51,-928.0143;Inherit;False;Property;_SrcBlend;SrcBlend;48;2;[HideInInspector];[IntRange];Create;True;0;3;Default;0;Off;1;On;2;1;UnityEngine.Rendering.BlendMode;True;0;False;1;1;0;12;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4617;1979.51,-857.0144;Inherit;False;Property;_DstBlend;DstBlend;49;2;[HideInInspector];[IntRange];Create;True;0;3;Default;0;Off;1;On;2;1;UnityEngine.Rendering.BlendMode;True;0;False;1;1;0;12;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4561;2024.577,-998.446;Inherit;False;Property;_DepthWrite;Depth Write;47;1;[Enum];Create;True;0;3;Default;0;Off;1;On;2;0;True;1;Space(5);False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;3578;2531.189,-994.2681;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4800;2689.814,-993.712;Float;False;True;-1;2;FPL.CustomMaterialEditor;100;5;LazyEti/BIRP/FakePointLight;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;True;8;5;True;_SrcBlend;1;True;_DstBlend;8;5;False;;1;False;;True;0;False;;0;False;;False;False;False;False;False;False;False;False;False;True;0;False;;True;True;1;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;True;2;True;_DepthWrite;True;7;False;;True;True;1000;False;;2000;False;;True;2;RenderType=Overlay=RenderType;Queue=Overlay=Queue=0;True;3;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;7;Include;;False;;Native;False;0;0;;Custom;#ifdef STEREO_INSTANCING_ON;False;;Custom;False;0;0;;Custom;UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(_CameraDepthNormalsTexture)@;False;;Custom;False;0;0;;Custom;#else;False;;Custom;False;0;0;;Custom;UNITY_DECLARE_TEX2D_NOSAMPLER(_CameraDepthNormalsTexture)@;False;;Custom;False;0;0;;Custom;#endif;False;;Custom;False;0;0;;Custom;SamplerState sampler_CameraDepthNormalsTexture@;False;;Custom;False;0;0;;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;638685074746738673;0;1;True;False;;True;0
WireConnection;1913;1;1914;0
WireConnection;1913;0;742;4
WireConnection;4591;0;3834;0
WireConnection;4591;1;260;0
WireConnection;3727;0;1913;0
WireConnection;4515;0;4514;0
WireConnection;3833;1;3834;0
WireConnection;3833;0;4591;0
WireConnection;4498;29;3727;0
WireConnection;4516;0;4515;0
WireConnection;255;1;3814;0
WireConnection;255;0;252;0
WireConnection;3887;0;3833;0
WireConnection;416;0;4498;0
WireConnection;467;0;463;0
WireConnection;653;0;255;0
WireConnection;3886;0;3887;0
WireConnection;3886;1;3887;1
WireConnection;466;0;416;0
WireConnection;466;3;467;0
WireConnection;4805;0;4518;0
WireConnection;3888;0;3886;0
WireConnection;3888;1;3887;2
WireConnection;477;0;466;0
WireConnection;539;0;4971;0
WireConnection;3819;0;539;0
WireConnection;3819;1;654;0
WireConnection;3835;0;3888;0
WireConnection;4792;17;3821;0
WireConnection;4806;0;4805;0
WireConnection;4107;0;3819;0
WireConnection;3708;0;478;0
WireConnection;4467;0;4792;0
WireConnection;4467;1;4569;0
WireConnection;4807;0;4806;0
WireConnection;3966;0;3836;0
WireConnection;3966;1;3708;0
WireConnection;55;0;4107;0
WireConnection;3571;0;4467;0
WireConnection;1927;0;4807;0
WireConnection;262;0;55;0
WireConnection;262;1;3966;0
WireConnection;4235;0;4234;0
WireConnection;4619;21;1929;0
WireConnection;4619;1;542;0
WireConnection;4619;60;3729;0
WireConnection;3954;0;262;0
WireConnection;3808;0;4235;0
WireConnection;5010;337;3830;0
WireConnection;5010;336;3540;0
WireConnection;5010;370;3838;0
WireConnection;5010;335;3576;0
WireConnection;3969;0;4619;0
WireConnection;3981;0;3954;0
WireConnection;4624;0;66;0
WireConnection;1881;0;5010;0
WireConnection;4264;0;3808;0
WireConnection;3984;0;3981;0
WireConnection;3712;0;4624;0
WireConnection;436;0;4264;0
WireConnection;436;1;2274;0
WireConnection;509;0;3981;0
WireConnection;509;1;3971;0
WireConnection;3952;0;3984;0
WireConnection;3711;0;3712;0
WireConnection;4595;0;436;0
WireConnection;4595;1;553;0
WireConnection;4595;2;1882;0
WireConnection;514;0;3981;0
WireConnection;514;1;509;0
WireConnection;3958;0;3952;0
WireConnection;769;0;3711;0
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
WireConnection;4280;0;4282;0
WireConnection;4230;0;4231;0
WireConnection;707;0;557;0
WireConnection;4298;0;4297;0
WireConnection;4534;0;4532;0
WireConnection;4229;0;4230;0
WireConnection;4229;1;4280;0
WireConnection;4500;0;4278;0
WireConnection;481;0;4537;0
WireConnection;481;1;707;0
WireConnection;4535;1;4533;0
WireConnection;4535;0;4534;0
WireConnection;4274;0;4500;0
WireConnection;4274;1;4229;0
WireConnection;4274;2;4301;0
WireConnection;200;0;481;0
WireConnection;200;1;140;0
WireConnection;200;2;201;0
WireConnection;4531;0;4530;0
WireConnection;4531;1;4535;0
WireConnection;4285;1;4302;0
WireConnection;4285;0;4274;0
WireConnection;4810;2;200;0
WireConnection;143;0;4810;6
WireConnection;143;1;4300;0
WireConnection;143;2;1976;0
WireConnection;4287;0;4285;0
WireConnection;5014;69;4528;0
WireConnection;5014;70;4527;0
WireConnection;5014;72;4529;0
WireConnection;5014;74;4531;0
WireConnection;5014;80;4537;0
WireConnection;141;0;4810;0
WireConnection;141;1;143;0
WireConnection;4548;0;5014;0
WireConnection;4288;0;141;0
WireConnection;4288;1;4289;0
WireConnection;1892;0;4498;45
WireConnection;600;0;4288;0
WireConnection;600;1;4549;0
WireConnection;1901;0;3572;0
WireConnection;1901;1;1384;0
WireConnection;657;0;600;0
WireConnection;657;1;1897;0
WireConnection;3579;0;657;0
WireConnection;3579;1;1901;0
WireConnection;4553;17;3579;0
WireConnection;4587;0;4553;0
WireConnection;4572;0;4587;0
WireConnection;4585;1;4553;0
WireConnection;4585;0;4553;0
WireConnection;4585;2;4572;0
WireConnection;5024;0;4585;0
WireConnection;5028;0;5027;0
WireConnection;5028;1;5029;0
WireConnection;5018;1;4585;0
WireConnection;5018;0;5047;0
WireConnection;5018;2;5016;0
WireConnection;5018;3;5017;0
WireConnection;5018;4;5026;0
WireConnection;5018;5;5028;0
WireConnection;5018;6;5030;0
WireConnection;5018;7;5031;0
WireConnection;3578;0;5025;0
WireConnection;3578;3;5013;0
WireConnection;4800;0;3578;0
ASEEND*/
//CHKSM=309632C5E808A714AD7EEFD74CD52DB9815F6782