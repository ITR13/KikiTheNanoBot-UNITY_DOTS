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

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 blockPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
            CBUFFER_END

            #ifdef DOTS_INSTANCING_ON
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
            #endif

            float4 UnityObjectToClipPos(float3 pos)
            {
                float4 temp = TransformObjectToHClip(pos);
                return temp;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = GetVertexPositionInputs(v.vertex.xyz).positionCS;
                
                #ifdef DOTS_INSTANCING_ON
                float4x4 unity_ObjectToWorld = k_identity4x4;// GetObjectToWorldMatrix();
                #endif

                o.blockPos = v.vertex.xyz * half3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22)
                ) - float3(0.5, 0.5, 0.5) - unity_ObjectToWorld._m03_m13_m23;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                const float3 binary = round(abs(frac(i.blockPos) - float3(0.5, 0.5, 0.5)) * 1.1111111111111);
                const float edge = any(binary * binary.yzx);
                const float center = 1 - edge;

                return _BaseColor * center + _EdgeColor * edge;
            }
            ENDHLSL
        }
    }
}