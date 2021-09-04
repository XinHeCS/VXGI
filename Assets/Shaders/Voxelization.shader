Shader "Unlit/Voxelization"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _roughness ("Roughness", Range(0, 1)) = 0.5
        _metallic ("Metallic", Range(0, 1)) = 0.5
        _emissive ("Emissive", Color) = (0, 0, 0, 1)
        _intensity ("Intensity", Range(0, 255)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // Calculate voxel data
        Pass
        {
//            Tags {"LightMode" = "Voxelization"}
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
            #include "inter_avg.cginc"
            #include "pbr.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 wordPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = mul(unity_ObjectToWorld, v.vertex);
                o.normal = UnityObjectToWorldDir(v.normal.xyz);
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
                    o.worldNormal = IN[i].normal;
                    o.vertex = mul(_viewProject[index], IN[i].vertex);
                    o.uv = IN[i].uv;
                    OUT.Append(o);
                }
            }

            // uniform RWStructuredBuffer<int> _voxelBuffer : register(u1);
            float3 _sceneMinAABB;
            float3 _sceneMaxAABB;
            float3 _resolution;
            float _step;

            uniform RWTexture3D<uint> _albedoBuffer : register(u1);
            uniform RWTexture3D<uint> _normalBuffer : register(u2);
            uniform RWTexture3D<uint> _emissiveBuffer : register(u3);
            
            float _roughness;
            float _metallic;
            float4 _emissive;
            float _intensity;
            
            fixed4 frag (g2f i) : SV_Target
            {
                int x = clamp(int((i.wordPos.x - _sceneMinAABB.x) / _step), 0, _resolution.x - 1);
                int y = clamp(int((i.wordPos.y - _sceneMinAABB.y) / _step), 0, _resolution.y - 1);
                int z = clamp(int((i.wordPos.z - _sceneMinAABB.z) / _step), 0, _resolution.z - 1);
                
                uint3 index = uint3(x, y, z);
                // InterlockedOr(_albedoBuffer[index], 1);

                float4 albedo = tex2D(_MainTex, i.uv);
                albedo.a = 1.0;

                float4 normal = float4(i.worldNormal, 1.0);

                _emissive.rgb *= _intensity;
                _emissive.a = 1.0;

                uint ori;
                uint newVal = convVec4ToRGBA8(albedo * 255.0);
                // InterlockedRGBA8Avg(_albedoBuffer, index, albedo);
                InterlockedExchange(_albedoBuffer[index], newVal, ori);
                // InterlockedRGBA8Avg(_normalBuffer, index, normal);
                // InterlockedRGBA8Avg(_emissiveBuffer, index, _emissive);
                                            
                // sample the texture
                // fixed4 col = float4(0, 0, 0, 0);
                // clip(-1);
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
        
        // Render geometry
//        Pass {
//            cull back
//            CGPROGRAM
//            #pragma enable_d3d11_debug_symbols
//            #pragma multi_compile_local _ VOXEL_MESH
//            #pragma target 5.0
//            #pragma vertex vert
//            // #pragma geometry geom
//            #pragma fragment frag
//
//            #include "UnityCG.cginc"
//            #include "inter_avg.cginc"
//            #include "pbr.cginc"
//
//            struct appdata
//            {
//                float4 vertex : POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            struct v2g
//            {
//                float4 vertex : POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            struct g2f
//            {
//                float4 vertex : SV_POSITION;
//                float4 wordPos : TEXCOORD1;
//                float2 uv : TEXCOORD0;
//            };
//
//            sampler2D _MainTex;
//            float4 _MainTex_ST;
//            
//            g2f vert(appdata i)
//            {
//                g2f o;
//                o.vertex = UnityObjectToClipPos(i.vertex);
//                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
//                o.wordPos = mul(unity_ObjectToWorld, i.vertex);
//                return o;
//            }
//            
//            uniform RWTexture3D<uint> _albedoBuffer : register(u1);
//            uniform RWTexture3D<uint> _normalBuffer : register(u2);
//            uniform RWTexture3D<uint> _emissiveBuffer : register(u3);
//            SamplerState LinearSampler;
//            float3 _sceneMinAABB;
//            float3 _resolution;
//            float _step;
//            
//            fixed4 frag(g2f i) : SV_Target
//            {
//                int x = clamp(int((i.wordPos.x - _sceneMinAABB.x) / _step), 0, _resolution.x - 1);
//                int y = clamp(int((i.wordPos.y - _sceneMinAABB.y) / _step), 0, _resolution.y - 1);
//                int z = clamp(int((i.wordPos.z - _sceneMinAABB.z) / _step), 0, _resolution.z - 1);
//
//                const uint3 index = uint3(x, y, z);
//                // const fixed4 voxelValue = fixed4(1, 1, 1, 1);
//                float4 voxelValue = convRGBA8ToVec4(_albedoBuffer.Load(uint4(index, 0)));
//                
//                return fixed4(voxelValue.rgb, 1) / 255.0;
//            }
//            
//            ENDCG
//        }
    }
}

//                float3 F0 = lerp( float3(0.04, 0.04, 0.04), albedo.rgb, _metallic);
//                float3 Lo = float3(0.0, 0.0, 0.0);
//
//                float3 N = normalize(i.worldNormal);
//
//                float3 L = _sunDir;
//                float3 V = normalize(_WorldSpaceCameraPos - i.worldNormal);
//                float3 H = normalize( V + L );
//
//                float NdotL = max( dot( N, L ), 0.0 );
//                float NdotH = max( dot( N, H ), 0.0 );
//                float NdotV = max( dot( N, V ), 0.0 );
//
//                // cook-torrance brdf
//                float NDF = distributionGGX( NdotH, _roughness );
//                float G = geometrySmith( NdotV, NdotL, _roughness );
//                float3 F = fresnelSchlick( clamp( dot( H, V ), 0.0, 1.0 ), F0 );
//
//                float3 nom = NDF * G * F;
//                float denom = 4 * NdotV * NdotL;
//
//                float3 specular = nom / max( denom, 0.001 );
//
//                float3 kS = F;
//                float3 kD = float3(1, 1, 1) - kS;
//                kD *= 1.0 - _metallic;
//                
//                Lo += ( kD * albedo.rgb / PI + specular ) * _sunIntensity * NdotL;
