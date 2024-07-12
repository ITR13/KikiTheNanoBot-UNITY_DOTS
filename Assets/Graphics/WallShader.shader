Shader "Unlit/WallShader"
{
    Properties
    {
        _BaseColor("BaseColor", Color) = (1, 1, 1, 1)
        _EdgeColor("EdgeColor", Color) = (0.25, 0.25, 0.25, 0)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            Cull Back
            Blend One Zero
            ZTest LEqual
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 blockPos : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _EdgeColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.blockPos = v.vertex.xyz * half3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22)
                ) - float3(0.5, 0.5, 0.5) - unity_ObjectToWorld._m03_m13_m23;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const float3 binary = round(abs(frac(i.blockPos) - float3(0.5, 0.5, 0.5)) * 1.1111111111111);
                const float edge = any(binary * binary.yzx);
                const float center = 1 - edge;

                return _BaseColor * center + _EdgeColor * edge;
            }
            ENDCG
        }
    }
}