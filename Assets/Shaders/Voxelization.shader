Shader "Unlit/Voxelization"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // Calculate voxel data
        Pass
        {
            Tags {"LightMode" = "Voxelization"}
            cull off
            zwrite off
            colorMask 0
//            conservative True
            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 wordPos : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = mul(unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            int select_view_projection(in v2g IN[3])
            {
                float3 p1 = IN[1].vertex.xyz - IN[0].vertex.xyz;
                float3 p2 = IN[2].vertex.xyz - IN[0].vertex.xyz;
                float3 faceNormal = cross(p1, p2);
                float nDx = abs(faceNormal.x);
                float nDy = abs(faceNormal.y);
                float nDz = abs(faceNormal.z);

                if (nDz > nDx && nDz > nDy)
                {
                    return 0;
                }
                if (nDx > nDy && nDx > nDz)
                {
                    return 1;
                }
                if (nDy > nDz && nDy > nDx)
                {
                    return 2;
                }
                return 0;
            }

            matrix _viewProject[3];
            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> OUT)
            {
                int index = select_view_projection(IN);
                // index = 1;
                for (int i = 0; i < 3; ++i)
                {
                    g2f o;
                    o.wordPos = IN[i].vertex;
                    o.vertex = mul(_viewProject[index], IN[i].vertex);
                    o.uv = IN[i].uv;
                    OUT.Append(o);
                }
            }

            uniform RWStructuredBuffer<int> _voxelBuffer : register(u1);
            float3 _sceneMinAABB;
            float3 _sceneMaxAABB;
            float3 _resolution;
            float _step;
            fixed4 frag (g2f i) : SV_Target
            {
                int x = clamp(int((i.wordPos.x - _sceneMinAABB.x) / _step), 0, _resolution.x - 1);
                int y = clamp(int((i.wordPos.y - _sceneMinAABB.y) / _step), 0, _resolution.y - 1);
                int z = clamp(int((i.wordPos.z - _sceneMinAABB.z) / _step), 0, _resolution.z - 1);
                
                int index = int(y * _resolution.x * _resolution.z + z * _resolution.x + x);
                InterlockedOr(_voxelBuffer[index], 1);
                
                // sample the texture
                // fixed4 col = float4(0, 0, 0, 0);
                // clip(-1);
                fixed4 col = float4(1, 1, 1, 1);
                col = fixed4(
                    float(x) / float(_resolution.x),
                    float(y) / float(_resolution.y),
                    float(z) / float(_resolution.z),
                    1
                    );
                return col;
            }
            ENDCG
        }
        
        // Render geometry
        Pass {
            cull back
            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile_local _ VOXEL_MESH
            #pragma target 5.0
            #pragma vertex vert
            // #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 wordPos : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            g2f vert(appdata i)
            {
                g2f o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.wordPos = mul(unity_ObjectToWorld, i.vertex);
                return o;
            }

            fixed4 _Color;
            uniform RWStructuredBuffer<int> _voxelBuffer : register(u1);
            float3 _sceneMinAABB;
            float3 _resolution;
            float _step;
            
            fixed4 frag(g2f i) : SV_Target
            {
#ifdef VOXEL_MESH
                int x = clamp(int((i.wordPos.x - _sceneMinAABB.x) / _step), 0, _resolution.x - 1);
                int y = clamp(int((i.wordPos.y - _sceneMinAABB.y) / _step), 0, _resolution.y - 1);
                int z = clamp(int((i.wordPos.z - _sceneMinAABB.z) / _step), 0, _resolution.z - 1);

                const int index = int(y * _resolution.x * _resolution.z + z * _resolution.x + x);
                const int voxelValue = (_voxelBuffer[index]);

                fixed4 col = fixed4(1, 1, 1, 1);
                if (voxelValue >= 1)
                {
                    col = fixed4(
                        float(x) / float(_resolution.x),
                        float(y) / float(_resolution.y),
                        float(z) / float(_resolution.z),                        
                        1
                        );
                }
                return col;
#else
                clip(-1);
#endif
                return fixed4(0 ,0 ,0 ,0);
            }
            
            ENDCG
        }
    }
}
