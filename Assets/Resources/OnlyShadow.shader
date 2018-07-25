Shader "OnlyShadow"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Name "Caster"
			Tags{ "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders

			#include "UnityCG.cginc"

			struct v2f {
				V2F_SHADOW_CASTER;
				float2  uv : TEXCOORD1;
				float3 posWorld : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform float4 _MainTex_ST;
			uniform sampler2D _MainTex;
			uniform fixed _Cutoff;
			uniform fixed4 _Color;

			v2f vert(appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
