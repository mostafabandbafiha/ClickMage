// Made with Amplify Shader Editor v1.9.5.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "LazyEti/BIRP/SpotLight"
{
	Properties
	{
		[HDR][Header(___Light Settings___)][Space(10)]_LightColor("Light Color", Color) = (1,0.9443759,0.8349056,1)
		_GradientMin("Gradient Min", Range( 0 , 1)) = 0
		_GradientMax("Gradient Max", Range( 0 , 2)) = 1
		_EdgeSoftness("EdgeSoftness", Range( 0 , 1)) = 0
		_DepthBlending("Depth Blending", Range( 0 , 5)) = 0
		[Toggle]_TurnOff("TurnOff", Range( 0 , 1)) = 0
		_OffColor("Off Color", Color) = (0.490566,0.490566,0.490566,0)
		[Space(25)][Toggle]DistanceFade("___Distance Fade___", Float) = 0
		[Tooltip(Starts fading away at this distance from the camera)]_FarFade("Far Fade", Range( 0 , 400)) = 200
		_FarTransition("Far Transition", Range( 1 , 100)) = 50
		_CloseFade("Close Fade", Range( 0 , 50)) = 0
		_CloseTransition("Close Transition", Range( 0 , 50)) = 0
		[Space(25)][Toggle(___FLICKERING____ON)] ___Flickering___("___Flickering___", Float) = 0
		_FlickerIntensity("Flicker Intensity", Range( 0.1 , 1)) = 0.5
		_FlickerSpeed("Flicker Speed", Range( 0.01 , 5)) = 1
		_FlickerSoftness("Flicker Softness", Range( 0 , 1)) = 0.5
		[Space(15)][Toggle]DayAlpha("Day Alpha", Float) = 0

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
	LOD 100

		CGINCLUDE
		#pragma target 3.5
		ENDCG
		Blend SrcAlpha One, SrcAlpha One
		AlphaToMask Off
		Cull Off
		ColorMask RGBA
		ZWrite Off
		ZTest LEqual
		Offset 0 , 0
		
		
		
		Pass
		{
			Name "Unlit"

			CGPROGRAM

			

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
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma shader_feature_local ___FLICKERING____ON


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float3 ase_normal : NORMAL;
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
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			//This is a late directive
			
			uniform float4 _LightColor;
			uniform float _GradientMin;
			uniform float _GradientMax;
			UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
			uniform float4 _CameraDepthTexture_TexelSize;
			uniform float _DepthBlending;
			uniform float _EdgeSoftness;
			uniform float DistanceFade;
			uniform float _FarFade;
			uniform float _FarTransition;
			uniform float _CloseFade;
			uniform float _CloseTransition;
			uniform float _FlickerSpeed;
			uniform float _FlickerSoftness;
			uniform float _FlickerIntensity;
			uniform float4 _OffColor;
			uniform float _TurnOff;
			uniform float DayAlpha;
			float2 ClipValuesBirp32_g54(  )
			{
				float far = 1;
				 #ifdef UNITY_REVERSED_Z
				far = 0;
				#endif
				return float2(UNITY_NEAR_CLIP_VALUE,far);
			}
			
			float noise58_g55( float x )
			{
				float n = sin (2 * x) + sin(3.14159265 * x);
				return n;
			}
			
			half3 HSVToRGB( half3 c )
			{
				half4 K = half4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
				half3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
				return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
			}
			

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord2 = screenPos;
				float3 ase_worldNormal = UnityObjectToWorldNormal(v.ase_normal);
				o.ase_texcoord3.xyz = ase_worldNormal;
				float3 ase_worldPos = mul(unity_ObjectToWorld, float4( (v.vertex).xyz, 1 )).xyz;
				float3 worldSpaceLightDir = UnityWorldSpaceLightDir(ase_worldPos);
				float3 vertexToFrag7_g58 = worldSpaceLightDir;
				o.ase_texcoord4.xyz = vertexToFrag7_g58;
				
				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.zw = 0;
				o.ase_texcoord3.w = 0;
				o.ase_texcoord4.w = 0;
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
			
			fixed4 frag (v2f i , bool ase_vface : SV_IsFrontFace) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				float2 texCoord125 = i.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float smoothstepResult124 = smoothstep( _GradientMin , _GradientMax , texCoord125.y);
				float4 screenPos = i.ase_texcoord2;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float temp_output_22_0_g54 = _DepthBlending;
				float screenDepth17_g54 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
				float distanceDepth17_g54 = saturate( abs( ( screenDepth17_g54 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( temp_output_22_0_g54 ) ) );
				float clampDepth21_g54 = SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch20_g54 = ( 1.0 - clampDepth21_g54 );
				#else
				float staticSwitch20_g54 = clampDepth21_g54;
				#endif
				float lerpResult11_g54 = lerp( _ProjectionParams.y , _ProjectionParams.z , staticSwitch20_g54);
				float2 localClipValuesBirp32_g54 = ClipValuesBirp32_g54();
				float2 break10_g54 = localClipValuesBirp32_g54;
				float lerpResult5_g54 = lerp( _ProjectionParams.y , _ProjectionParams.z , (0.0 + (ase_screenPosNorm.z - break10_g54.x) * (1.0 - 0.0) / (break10_g54.y - break10_g54.x)));
				float lerpResult16_g54 = lerp( distanceDepth17_g54 , saturate( ( ( lerpResult11_g54 - lerpResult5_g54 ) / temp_output_22_0_g54 ) ) , unity_OrthoParams.w);
				float3 ase_worldNormal = i.ase_texcoord3.xyz;
				float3 ase_worldViewDir = UnityWorldSpaceViewDir(WorldPosition);
				ase_worldViewDir = normalize(ase_worldViewDir);
				float dotResult150 = dot( ase_worldNormal , ase_worldViewDir );
				float switchResult149 = (((ase_vface>0)?(dotResult150):(-dotResult150)));
				float ifLocalVar145 = 0;
				if( _EdgeSoftness <= 0.0 )
				ifLocalVar145 = 1.0;
				else
				ifLocalVar145 = saturate( ( 1.0 - ( _EdgeSoftness * pow( ( abs( ( 1.0 - switchResult149 ) ) + 0.1 ) , 5.0 ) ) ) );
				float4 transform14_g57 = mul(unity_ObjectToWorld,float4( 0,0,0,1 ));
				float3 _Vector0 = float3(1,0,1);
				float Dist41_g57 = distance( ( transform14_g57.xyz * _Vector0 ) , ( _Vector0 * _WorldSpaceCameraPos ) );
				float mulTime17_g55 = _Time.y * ( _FlickerSpeed * 4 );
				float x58_g55 = ( mulTime17_g55 + ( 0.0 * UNITY_PI ) );
				float localnoise58_g55 = noise58_g55( x58_g55 );
				float temp_output_44_0_g55 = ( ( 1.0 - _FlickerSoftness ) * 0.5 );
				#ifdef ___FLICKERING____ON
				float staticSwitch53_g55 = saturate( (( 1.0 - _FlickerIntensity ) + ((0.0 + (localnoise58_g55 - -2.0) * (1.0 - 0.0) / (2.0 - -2.0)) - ( 1.0 - temp_output_44_0_g55 )) * (1.0 - ( 1.0 - _FlickerIntensity )) / (temp_output_44_0_g55 - ( 1.0 - temp_output_44_0_g55 ))) );
				#else
				float staticSwitch53_g55 = 1.0;
				#endif
				half3 hsvTorgb47_g55 = HSVToRGB( half3(radians( staticSwitch53_g55 ),1.0,1.0) );
				float3 lerpResult51_g55 = lerp( hsvTorgb47_g55 , float3( 1,1,1 ) , staticSwitch53_g55);
				float4 appendResult159 = (float4(( _LightColor.rgb * _LightColor.a * ( smoothstepResult124 * saturate( lerpResult16_g54 ) * ifLocalVar145 ) * (( DistanceFade )?( ( saturate( ( 1.0 - ( ( Dist41_g57 - _FarFade ) / _FarTransition ) ) ) * saturate( ( ( Dist41_g57 - _CloseFade ) / _CloseTransition ) ) ) ):( 1.0 )) * ( staticSwitch53_g55 * lerpResult51_g55 ) ) , 1.0));
				float3 vertexToFrag7_g58 = i.ase_texcoord4.xyz;
				float dotResult3_g58 = dot( -vertexToFrag7_g58 , float3( 0,1,0 ) );
				float4 lerpResult130 = lerp( appendResult159 , _OffColor , ( _TurnOff * (( DayAlpha )?( saturate( ( dotResult3_g58 * 4.0 ) ) ):( 1.0 )) ));
				
				
				finalColor = lerpResult130;
				return finalColor;
			}
			ENDCG
		}
	}
	
	
	Fallback Off
}
/*ASEBEGIN
Version=19501
Node;AmplifyShaderEditor.CommentaryNode;122;-3120,-480;Inherit;False;1912.656;398.8617;;16;156;155;154;153;152;151;150;149;148;147;146;145;144;143;134;133;EdgeSoftness;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldNormalVector;152;-3056,-400;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;153;-3056,-256;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DotProductOpNode;150;-2848,-320;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;151;-2736,-256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwitchByFaceNode;149;-2592,-320;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;148;-2416,-320;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;154;-2288,-320;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;155;-2176,-320;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;156;-2192,-208;Inherit;False;Constant;_Pow;Pow;4;0;Create;True;0;0;0;False;0;False;5;0;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;133;-2048,-320;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;3.14;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;147;-2192,-416;Inherit;False;Property;_EdgeSoftness;EdgeSoftness;3;0;Create;True;0;0;0;False;0;False;0;0.693;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;121;-1952,-672;Inherit;False;743;165;;3;137;123;157;Depth Fade;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;134;-1888,-352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;120;-1776,-1040;Inherit;False;564;339;;4;136;135;125;124;AlphaGradient;1,1,1,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;144;-1712,-352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;137;-1914,-614;Inherit;False;Property;_DepthBlending;Depth Blending;4;0;Create;True;0;0;0;False;0;False;0;0.19;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;119;-1088,-224;Inherit;False;516.6265;253.6194;;2;132;131;Flickering;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;125;-1664,-976;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;135;-1728,-848;Inherit;False;Property;_GradientMin;Gradient Min;1;0;Create;True;0;0;0;False;0;False;0;0.123;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;136;-1728,-784;Inherit;False;Property;_GradientMax;Gradient Max;2;0;Create;True;0;0;0;False;0;False;1;2;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;143;-1568,-352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;146;-1568,-272;Inherit;False;Constant;_Float1;Float 1;10;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;157;-1623.815,-613.4585;Inherit;False;DepthFade Ortho Friendly;-1;;54;3129d81269455f3429c966e56facac9c;0;1;22;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;131;-1040,-160;Inherit;False;FlickerFunction;13;;55;f6225b1ef66c663478bc4f0259ec00df;0;4;9;FLOAT;0;False;8;FLOAT;0;False;21;FLOAT;0;False;29;FLOAT;0;False;2;FLOAT;0;FLOAT3;45
Node;AmplifyShaderEditor.CommentaryNode;117;-912,-768;Inherit;False;505.8481;313.4387;;2;129;127;Light Color;1,1,1,1;0;0
Node;AmplifyShaderEditor.SaturateNode;123;-1392,-624;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;124;-1424,-848;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;145;-1424,-416;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;118;-368,-672;Inherit;False;639;495;;6;142;140;139;138;130;159;Off Color;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;128;-1120,-576;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;129;-848,-704;Inherit;False;Property;_LightColor;Light Color;0;2;[HDR];[Header];Create;True;1;___Light Settings___;0;0;False;1;Space(10);False;1,0.9443759,0.8349056,1;1,0.7296548,0.1933962,0.4745098;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode;141;-960,-432;Inherit;False;AdvancedCameraFade;7;;57;e6e830f789d28b746963801d61c2a1ec;0;6;40;FLOAT;0;False;46;FLOAT;0;False;47;FLOAT;0;False;48;FLOAT;0;False;17;FLOAT3;0,0,0;False;20;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;132;-716,-160;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;140;-208,-272;Inherit;False;DayAlpha;18;;58;bc1f8ebe2e26696419e0099f8a3e27dc;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;142;-336,-352;Inherit;False;Property;_TurnOff;TurnOff;5;1;[Toggle];Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;127;-560,-624;Inherit;False;5;5;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;138;-144,-560;Inherit;False;Property;_OffColor;Off Color;6;0;Create;True;0;0;0;False;0;False;0.490566,0.490566,0.490566,0;0.6981132,0.2762136,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;139;-64,-352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;159;-337.1364,-622.8063;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;130;96,-624;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;116;320,-624;Float;False;True;-1;2;;100;5;LazyEti/BIRP/SpotLight;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;True;8;5;False;;1;False;;8;5;False;;1;False;;True;0;False;;0;False;;False;False;False;False;False;False;False;False;False;True;0;False;;True;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;2;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;3;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;0;1;True;False;;False;0
WireConnection;150;0;152;0
WireConnection;150;1;153;0
WireConnection;151;0;150;0
WireConnection;149;0;150;0
WireConnection;149;1;151;0
WireConnection;148;0;149;0
WireConnection;154;0;148;0
WireConnection;155;0;154;0
WireConnection;133;0;155;0
WireConnection;133;1;156;0
WireConnection;134;0;147;0
WireConnection;134;1;133;0
WireConnection;144;0;134;0
WireConnection;143;0;144;0
WireConnection;157;22;137;0
WireConnection;123;0;157;0
WireConnection;124;0;125;2
WireConnection;124;1;135;0
WireConnection;124;2;136;0
WireConnection;145;0;147;0
WireConnection;145;2;143;0
WireConnection;145;3;146;0
WireConnection;145;4;146;0
WireConnection;128;0;124;0
WireConnection;128;1;123;0
WireConnection;128;2;145;0
WireConnection;132;0;131;0
WireConnection;132;1;131;45
WireConnection;127;0;129;5
WireConnection;127;1;129;4
WireConnection;127;2;128;0
WireConnection;127;3;141;0
WireConnection;127;4;132;0
WireConnection;139;0;142;0
WireConnection;139;1;140;0
WireConnection;159;0;127;0
WireConnection;130;0;159;0
WireConnection;130;1;138;0
WireConnection;130;2;139;0
WireConnection;116;0;130;0
ASEEND*/
//CHKSM=A82586A6CFA2853A07591EA21BF7E38B0AF68E3D