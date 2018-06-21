// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/RenderToVolume"
{
	Properties
	{
		_MainTex("Diffuse (RGBA)", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			Tags{ "RenderType" = "Opaque" }
			Cull Off ZWrite Off ZTest Always Fog{ Mode Off }

			CGPROGRAM
	#pragma target 5.0
	#pragma vertex vert
	#pragma fragment frag
	#pragma exclude_renderers flash gles opengl

	#include "UnityCG.cginc"


		sampler2D _MainTex;

		uniform RWTexture3D<float4> volumeTex : register(u1);
		float volumeResolution;
		float4 volumeParams;

		struct ApplicationToVertex
		{
			float4 vertex : POSITION;
			float4 texcoord : TEXCOORD0;
		};

		struct VertexToFragment
		{
			float4 pos : SV_POSITION;
			float4 wPos : TEXCOORD0;
			float2 uv : TEXCOORD1;
		};

		void vert(ApplicationToVertex input, out VertexToFragment output)
		{
			output.pos = UnityObjectToClipPos(input.vertex);
			output.wPos = mul(unity_ObjectToWorld, input.vertex);
			output.uv = input.texcoord.xy;
		}

		void frag(VertexToFragment input)
		{
			float4 color = tex2D(_MainTex, input.uv);

			float3 volumePos = ((input.wPos.xyz - volumeParams.xyz) / volumeParams.w) * volumeResolution;

			int3 volumeCoords;
			volumeCoords.x = int(volumePos.x);
			volumeCoords.y = int(volumePos.y);
			int z = 0;// int(volumePos.z);
			for (int i = 0; i<volumeResolution; i++)
			{
				volumeCoords.z = z+i;
				color = float4(input.uv.x, input.uv.y, volumeCoords.z*0.008, 1);
				volumeTex[volumeCoords] = color;
			}
		}
		ENDCG
		}
	}
	Fallback Off
}
