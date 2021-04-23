Shader "Unlit/WorldSpaceNormal"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
			};

			v2f vert (float4 vertex : POSITION, float3 normal : NORMAL)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(vertex);
				o.normal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));
				return o;
			}
            
			fixed4 frag (v2f i) : SV_Target
			{
				return half4(i.normal * 0.5 + 0.5, 1.0f);
			}
			ENDCG
		}
	}
}
