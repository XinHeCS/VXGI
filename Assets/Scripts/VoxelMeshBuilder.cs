using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VoxelMeshBuilder : MonoBehaviour
{
    // public static Mesh CreateMesh(Voxelization vx, Vector3Int startIndex, Vector3Int endIndex, Vector3 scale, Vector3 min)
    // {
        // List<Vector3> verts = new List<Vector3>();
        // List<int> indices = new List<int>();
        //
        // for (int z = startIndex.z; z <= endIndex.z; z++)
        // {
        //     for (int y = startIndex.y; y <= endIndex.y; y++)
        //     {
        //         for (int x = startIndex.x; x <= endIndex.x; x++)
        //         {
        //             if (vx.GetVoxelValue(x, y, z) != 1) continue;
        //
        //             Vector3 pos = min + new Vector3(x * scale.x, y * scale.y, z * scale.z);
        //
        //             if (x == endIndex.x || vx.GetVoxelValue(x + 1, y, z) == 0)
        //                 AddRightQuad(verts, indices, scale, pos);
        //
        //             if (x == 0 || vx.GetVoxelValue(x - 1, y, z) == 0)
        //                 AddLeftQuad(verts, indices, scale, pos);
        //
        //             if (y == endIndex.y || vx.GetVoxelValue(x, y + 1, z) == 0)
        //                 AddTopQuad(verts, indices, scale, pos);
        //
        //             if (y == 0 || vx.GetVoxelValue(x, y - 1, z) == 0)
        //                AddBottomQuad(verts, indices, scale, pos);
        //
        //             if (z == endIndex.z || vx.GetVoxelValue(x, y, z + 1) == 0)
        //                 AddFrontQuad(verts, indices, scale, pos);
        //
        //             if (z == 0 || vx.GetVoxelValue(x, y, z - 1) == 0)
        //                 AddBackQuad(verts, indices, scale, pos);
        //         }
        //     }
        // }
        //
        // Mesh mesh = new Mesh();
        // if(verts.Count > 65000)
        // {
        //     Debug.Log("Mesh has too many verts. You will have to add code to split it up.");
        //     mesh.indexFormat = IndexFormat.UInt32;
        // }
        //
        // mesh.SetVertices(verts);
        // mesh.SetTriangles(indices, 0);
        //
        // mesh.RecalculateBounds();
        // mesh.RecalculateNormals();

        // return mesh;
    // }

    private static void AddRightQuad(List<Vector3> verts, List<int> indices, Vector3 scale, Vector3 pos)
    {
        int count = verts.Count;

        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 1 * scale.y, 0 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 0 * scale.z));

        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 1 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 1 * scale.y, 0 * scale.z));

        indices.Add(count + 2); indices.Add(count + 1); indices.Add(count + 0);
        indices.Add(count + 5); indices.Add(count + 4); indices.Add(count + 3);
    }

    private static void AddLeftQuad(List<Vector3> verts, List<int> indices, Vector3 scale, Vector3 pos)
    {
        int count = verts.Count;

        verts.Add(pos + new Vector3(0 * scale.x, 0 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 0 * scale.z));
        verts.Add(pos + new Vector3(0 * scale.x, 0 * scale.y, 0 * scale.z));

        verts.Add(pos + new Vector3(0 * scale.x, 0 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 0 * scale.z));

        indices.Add(count + 0); indices.Add(count + 1); indices.Add(count + 2);
        indices.Add(count + 3); indices.Add(count + 4); indices.Add(count + 5);
    }

    private static void AddTopQuad(List<Vector3> verts, List<int> indices, Vector3 scale, Vector3 pos)
    {
        int count = verts.Count;

        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 1 * scale.y, 0 * scale.z));
        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 0 * scale.z));

        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 1 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 1 * scale.y, 0 * scale.z));

        indices.Add(count + 0); indices.Add(count + 1); indices.Add(count + 2);
        indices.Add(count + 3); indices.Add(count + 4); indices.Add(count + 5);
    }

    private static void AddBottomQuad(List<Vector3> verts, List<int> indices, Vector3 scale, Vector3 pos)
    {
        int count = verts.Count;

        verts.Add(pos + new Vector3(0 * scale.x, 0 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 0 * scale.z));
        verts.Add(pos + new Vector3(0 * scale.x, 0 * scale.y, 0 * scale.z));

        verts.Add(pos + new Vector3(0 * scale.x, 0 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 0 * scale.z));

        indices.Add(count + 2); indices.Add(count + 1); indices.Add(count + 0);
        indices.Add(count + 5); indices.Add(count + 4); indices.Add(count + 3);
    }

    private static void AddFrontQuad(List<Vector3> verts, List<int> indices, Vector3 scale, Vector3 pos)
    {
        int count = verts.Count;

        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(0 * scale.x, 0 * scale.y, 1 * scale.z));

        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 1 * scale.y, 1 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 1 * scale.z));

        indices.Add(count + 2); indices.Add(count + 1); indices.Add(count + 0);
        indices.Add(count + 5); indices.Add(count + 4); indices.Add(count + 3);
    }

    private static void AddBackQuad(List<Vector3> verts, List<int> indices, Vector3 scale, Vector3 pos)
    {
        int count = verts.Count;

        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 0 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 0 * scale.z));
        verts.Add(pos + new Vector3(0 * scale.x, 0 * scale.y, 0 * scale.z));

        verts.Add(pos + new Vector3(0 * scale.x, 1 * scale.y, 0 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 1 * scale.y, 0 * scale.z));
        verts.Add(pos + new Vector3(1 * scale.x, 0 * scale.y, 0 * scale.z));

        indices.Add(count + 0); indices.Add(count + 1); indices.Add(count + 2);
        indices.Add(count + 3); indices.Add(count + 4); indices.Add(count + 5);
    }
}
