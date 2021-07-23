Shader "Unlit/DrawVoxel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ VOXEL_MESH
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            // make fog work

            #include "UnityCG.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
                float3 normal : NORMAL;
                uint index : SV_InstanceID;
            };

            struct v2f
            {
                float4 clipPos : SV_POSITION;
                float3 normal : NORMAL;
                float4 voxelPos : TEXCOORD0;
                uint index : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            uniform RWStructuredBuffer<int> _voxelBuffer : register(u1);
            float3 _sceneMinAABB;
            float3 _resolution;
            float _step;
            v2f vert (appdata v)
            {
                v2f o;

                uint3 resolutionInt = uint3(
                    uint(_resolution.x),
                    uint(_resolution.y),
                    uint(_resolution.z)
                    );
                uint3 boundPos = uint3(
                    uint(v.index % (resolutionInt.x * resolutionInt.z) % resolutionInt.x),
                    uint(v.index / (resolutionInt.z * resolutionInt.x)),
                    uint(v.index % (resolutionInt.x * resolutionInt.z) / resolutionInt.x)
                    );
                float3 stepVec = float3(_step, _step, _step);
                float3 startPos = _sceneMinAABB + stepVec * 0.5f;
                float4 worldPos = float4(boundPos * stepVec + startPos, 1);
                float4 offset = float4(v.vertex * _step * 0.5f, 1);
                o.clipPos = UnityWorldToClipPos(worldPos + offset);
                o.normal = v.normal;
                o.voxelPos = worldPos;
                o.index = v.index;
                
                return o;
            }

            fixed4 _Color;
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;
#ifdef VOXEL_MESH
                col = fixed4(
                    float(i.voxelPos.x) / float(_resolution.x),
                    float(i.voxelPos.y) / float(_resolution.y),
                    float(i.voxelPos.z) / float(_resolution.z),
                    1
                );
#else
                float3 lightDir = normalize(UnityWorldSpaceLightDir(i.voxelPos));
                float3 viewDir = normalize(UnityWorldSpaceViewDir(i.voxelPos));
                float3 halfway = normalize(lightDir + viewDir);

                float3 ambient = float3(0.1, 0.1, 0.1) * _Color;
                
                float diff = max(dot(lightDir, i.normal), 0.0);
                float3 diffuse = diff * _Color;
                
                float spec = pow(max(dot(i.normal, halfway), 0.0), 15);
                float3 specular = float3(unity_LightColor0.x, unity_LightColor0.y, unity_LightColor0.z) * spec;
                col = half4(ambient + diffuse + specular, 1); 
#endif  
                return col;
            }
            ENDCG
        }
    }
}
