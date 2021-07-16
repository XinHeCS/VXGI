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
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            // make fog work

            #include "UnityCG.cginc"

            struct appdata
            {
                uint index : SV_VertexID;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                uint index : TEXCOORD0;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            uniform RWStructuredBuffer<int> _voxelBuffer : register(u1);
            float3 _sceneMinAABB;
            float3 _resolution;
            float _step;
            v2g vert (appdata v)
            {
                v2g o;

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
                float3 startPos = _sceneMinAABB + float3(_step, _step, _step) * 0.5f;
                o.vertex = float4(boundPos * float3(_step, _step, _step) + startPos, 1);
                o.index = v.index;
                
                return o;
            }

            [maxvertexcount(36)]
            void geom(point v2g IN[1], inout TriangleStream<g2f> OUT)
            {
	            const float4 cubeVertices[8] = 
	            {
                    float4( 0.5f,  0.5f,  0.5f, 0.0f),
                    float4( 0.5f,  0.5f, -0.5f, 0.0f),
                    float4( 0.5f, -0.5f,  0.5f, 0.0f),
                    float4( 0.5f, -0.5f, -0.5f, 0.0f),
                    float4(-0.5f,  0.5f,  0.5f, 0.0f),
                    float4(-0.5f,  0.5f, -0.5f, 0.0f),
                    float4(-0.5f, -0.5f,  0.5f, 0.0f),
                    float4(-0.5f, -0.5f, -0.5f, 0.0f)
	            };
                
	            const int cubeIndices[24] = 
	            {
		            0, 2, 1, 3, // right
		            6, 4, 7, 5, // left
		            5, 4, 1, 0, // up
		            6, 7, 2, 3, // down
		            4, 6, 0, 2, // front
		            1, 3, 5, 7  // back
	            };

                if (_voxelBuffer[IN[0].index] < 1)
                {
                    return;
                }

                float4 stepVec = float4(_step, _step, _step, 0);
                float4 projectVertex[8];
                for (int i = 0; i < 8; ++i)
                {
                    float4 vertexPos = IN[0].vertex + stepVec * cubeVertices[i];
                    projectVertex[i] = mul(unity_MatrixVP, vertexPos);
                }

                for (int face = 0; face < 6; ++face)
                {
                    // Add first triangle
                    for (int i = 0; i < 3; ++i)
                    {
                        g2f o;
                        o.vertex = projectVertex[cubeIndices[4 * face + i]];
                        OUT.Append(o);
                    }
                    OUT.RestartStrip();

                    // Add second triangle
                    g2f v1, v2, v3;
                    v1.vertex = projectVertex[cubeIndices[4 * face + 2]];
                    v2.vertex = projectVertex[cubeIndices[4 * face + 1]];
                    v3.vertex = projectVertex[cubeIndices[4 * face + 3]];
                    OUT.Append(v1);
                    OUT.Append(v2);
                    OUT.Append(v3);
                    OUT.RestartStrip();
                }
            }

            fixed4 _Color;
            fixed4 frag (v2g i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;
                return col;
            }
            ENDCG
        }
    }
}
