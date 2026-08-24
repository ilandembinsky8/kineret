

Shader "Tazo/Y_Rotate_Cloud"
{
	Properties
	{



		[Header(All)]
		_MColor("Thick Color", Color) = (1,1,1,1)
		_MColor_thin("Thin Color", Color) = (1,1,1,1)
		_D("Density", Range(0, 10)) = 1
		_DD("Density Clamp", Range(0.5, 1)) = 1
		_HF("Top Fade", Range(0.01, 5)) = 1
		_E("Edge sharp", Range(0, 10)) = 0.98
		_UE("Upper Edge Contrast", Range(0.5, 1)) = 1
		_tile_all("Tile All", Range(1, 20.0)) = 1
		_tileOffsetX_all("OffsetX all", Range(-10, 10)) = 0
		_tileOffsetY_all("OffseeY all", Range(-10, 10)) = 0
		[Toggle] _Invert("Thick?", Float) = 1
		[Toggle] _InvertC("CirroCumulus ?", Float) = 0
		[Toggle] _InvertB("BottomFade ?", Float) = 0
		[Header(Main Stream)]
		[NoScaleOffset]_Project("Project(RGB)", 2D) = "white" {}
		_tile("Tile", Range(1, 20.0)) = 1
		_tileOffsetX("OffsetX", Range(-1, 1)) = 0
		_tileOffsetY("OffseeY", Range(-1, 1)) = 0

		[NoScaleOffset]_ProjectUV("Distortion(R)", 2D) = "black" {}
		_tileUV("UV Dis Tile", Range(1,100.0)) = 1
		_flow_offset("flow_offset", Range(-10, 10)) = 0
		_flow_strength("flow_strength", Range(-10, 10)) = 0.5


		[Header(Deflect Stream 1)]
		_tile_1("Tile", Range(1, 20.0)) = 1
		_tileOffsetX_1("OffsetX", Range(-1, 1)) = 0
		_tileOffsetY_1("OffseeY", Range(-1, 1)) = 0
		_tileUV_1("UV Dis Tile", Range(1,100.0)) = 1
		_flow_offset_1("flow_offset", Range(-10, 10)) = 0
		_flow_strength_1("flow_strength", Range(-10, 10)) = 0.5
		[Header(Deflect Stream 2)]
		_tile_2("Tile", Range(1, 20.0)) = 1
		_tileOffsetX_2("OffsetX", Range(-1, 1)) = 0
		_tileOffsetY_2("OffseeY", Range(-1, 1)) = 0
		_tileUV_2("UV Dis Tile", Range(1,100.0)) = 1
		_flow_offset_2("flow_offset", Range(-10, 10)) = 0
		_flow_strength_2("flow_strength", Range(-10, 10)) = 0.5
		[Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0

	}

		Subshader
		{
			Tags { "Queue" = "Transparent-499" "IgnoreProjector" = "True" "RenderType" = "Transparent" }




			//shell
					Pass
					{
				Cull Front
				ZWrite[_ZWrite]
				Blend SrcAlpha OneMinusSrcAlpha
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma shader_feature _INVERT_ON
				#pragma shader_feature _INVERTC_ON
				#pragma shader_feature _INVERTB_ON
				#pragma fragmentoption ARB_precision_hint_fastest

				#include "UnityCG.cginc"
				struct appdata
				{
					float4 vertex : POSITION;
					float4 texcoord : TEXCOORD0;
					float3 normal : NORMAL;
				};

				struct v2f
				{
					float4 pos	: SV_POSITION;


					float4 uv_object : TEXCOORD0;
					fixed4 color : COLOR;

				};
				float _tileOffsetX_all;
				float _tileOffsetY_all;
				float _tile_all;
				fixed _D;
				fixed _E;
				fixed _UE;
				float _tile;
				float _tileUV;
				float _flow_offset;
				float _flow_strength;
				float _tileOffsetX;
				float _tileOffsetY;
				float _tile_1;
				float _tileUV_1;
				float _flow_offset_1;
				float _flow_strength_1;
				float _tileOffsetX_1;
				float _tileOffsetY_1;
				float _tile_2;
				float _tileUV_2;
				float _flow_offset_2;
				float _flow_strength_2;
				float _tileOffsetX_2;
				float _tileOffsetY_2;
				fixed _DD;
				fixed _HF;
				v2f vert(appdata v)
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.uv_object = v.texcoord;
					o.uv_object.a = v.normal.y;
					return o;
				}

				uniform float4 _MColor;
				uniform float4 _MColor_thin;
				uniform sampler2D _Project;
				uniform sampler2D _ProjectUV;

				float4 frag(v2f i) : SV_Target
				{

					float2 offset = float2(_tileOffsetX + _tileOffsetX_all, _tileOffsetY + _tileOffsetY_all);
					float4 t = tex2D(_ProjectUV, (frac(offset * _Time.y * _flow_offset) + i.uv_object.rg * _tileUV * _tile_all));
					fixed4 p = tex2D(_Project, offset * _Time.x + i.uv_object.rg * _tile * _tile_all + t * _flow_strength);
	#ifdef _INVERTC_ON
					fixed4 p_vu = tex2D(_Project, offset.gr * _Time.x + i.uv_object.gr * _tile * _tile_all * 3 + t * _flow_strength);
					fixed4 p_mask = tex2D(_Project, offset.gr * _Time.x + i.uv_object.gr * _tile * _tile_all * 2 + t * _flow_strength);
					p = lerp(p_vu,p, p_mask.b);
	#endif
					float2 offset_1 = float2(_tileOffsetX_1 + _tileOffsetX_all, _tileOffsetY_1 + _tileOffsetY_all);
					float4 t_1 = tex2D(_ProjectUV, (offset_1 * _Time.y + frac(offset_1 * _Time.y * _flow_offset_1) + i.uv_object.rg * _tileUV_1 * _tile_all));
					fixed4 p_1 = tex2D(_Project, offset_1 * _Time.x + i.uv_object.rg * _tile_1 * _tile_all + t_1 * _flow_strength_1);

					float2 offset_2 = float2(_tileOffsetX_2 + _tileOffsetX_all, _tileOffsetY_2 + _tileOffsetY_all);
					float4 t_2 = tex2D(_ProjectUV, (offset_2 * _Time.y + frac(offset_2 * _Time.y * _flow_offset_2) + i.uv_object.rg * _tileUV_2 * _tile_all));
					fixed4 p_2 = tex2D(_Project, offset_2 * _Time.x + i.uv_object.rg * _tile_2 * _tile_all + t_2 * _flow_strength_2);



					fixed4 cc = _MColor;

	#ifdef _INVERT_ON
					fixed a = saturate(p.r * (p_1.r + p_2.r));
	#else
					fixed a = saturate(p.r * (p_1.r * p_2.r));
	#endif
					cc = lerp(_MColor_thin, _MColor, a);
					fixed aa= saturate(i.uv_object.a);
					fixed mask =1-aa ;
					mask = _HF * (mask - 0.5) + 0.5;
#ifdef _INVERTB_ON
					aa *= aa;
#endif
					cc.a =  smoothstep(0, lerp(1,_UE,aa),   clamp(   saturate(_D * pow(cc.a * (a),lerp(_E,_E*2, aa))*aa*mask),0, _DD));

					return cc;
				}
				ENDCG
			}


		}

			Fallback "VertexLit"
}
