Shader "Unlit/VoxelDebug"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        LOD 100

        Pass
        {
            cull back
            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile_local _ VOXEL_MESH
            #pragma target 5.0
            #pragma vertex vert
            // #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "inter_avg.cginc"
            #include "pbr.cginc"

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
            
            RWTexture3D<uint> _albedoBuffer : register(u1);
            RWTexture3D<uint> _normalBuffer : register(u2);
            RWTexture3D<uint> _emissiveBuffer : register(u3);
            
            float3 _sceneMinAABB;
            float3 _resolution;
            float _step;
            
            fixed4 frag(g2f i) : SV_Target
            {
                int x = clamp(int((i.wordPos.x - _sceneMinAABB.x) / _step), 0, _resolution.x - 1);
                int y = clamp(int((i.wordPos.y - _sceneMinAABB.y) / _step), 0, _resolution.y - 1);
                int z = clamp(int((i.wordPos.z - _sceneMinAABB.z) / _step), 0, _resolution.z - 1);

                const uint3 index = uint3(x, y, z);
                // const fixed4 voxelValue = fixed4(1, 1, 1, 1);
                float4 voxelValue = convRGBA8ToVec4(_albedoBuffer.Load(uint4(index, 0)));
                
                return fixed4(voxelValue.rgb, 1) / 255.0;
            }
            
            ENDCG
        }
    }
}