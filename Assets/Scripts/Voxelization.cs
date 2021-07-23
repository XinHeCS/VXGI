using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class Voxelization : MonoBehaviour
{
    public float _voxelStep = 0.5f;

    public float _voxelPlaneOffset = 0.2f;

    public bool _showVxoelMesh = true;
    
    public bool _showDebugVoxelColor = true;

    public Material _voxelMaterial;

    private Mesh _voxelMesh;

    private Vector3 _resolution;

    private int _length;
    
    private Camera _mainCamera;

    private ComputeBuffer _voxelBuffer;

    private int[] _data;

    private Bounds _sceneBounds = new Bounds();
    
    private List<VoxelRenderer> _objs = new List<VoxelRenderer>();

    private int _voxelStepID = Shader.PropertyToID("_step");
    private int _resolutionID = Shader.PropertyToID("_resolution");
    private int _voxelBufferID = Shader.PropertyToID("_voxelBuffer");
    private int _sceneBoundsMinID = Shader.PropertyToID("_sceneMinAABB");
    private int _sceneBoundsMaxID = Shader.PropertyToID("_sceneMaxAABB");
    private int _voxelPlaneID = Shader.PropertyToID("_viewProject");

    private void Start()
    {
        RenderPipelineManager.endCameraRendering += OnEndRendering;
        _mainCamera = GetComponent<Camera>();
        BuildVoxelVolumeVertexBuffer();
    }

    private void Update()
    {
        if (UpdateVoxelParam())
        {
            UpdateVoxelCamera();
        }
    }

    private void OnEndRendering(ScriptableRenderContext context, Camera camera)
    {
        if (_showVxoelMesh && (_voxelMaterial != null))
        {
            RenderVoxelMesh();
        }
    }

    public void AddVoxelObjects(VoxelRenderer voxelRenderer)
    {
        if (_objs.Contains(voxelRenderer))
        {
            return;
        }
        _objs.Add(voxelRenderer);
        // BuildVoxelBuffer(voxelRenderer);
    }

    public Mesh GetVoxelMesh(VoxelRenderer renderer)
    {
        if (!_objs.Contains(renderer))
        {
            Debug.LogError("Invalid voxel renderer");
            return new Mesh();
        }
        
        Bounds bound = renderer.GetAABB();
        Vector3 range = bound.max - bound.min;
        Vector3Int startIndex = new Vector3Int
        {
            x = (int)Mathf.Clamp((bound.min.x - _sceneBounds.min.x) / _voxelStep, 0.0f, _resolution.x - 1),
            y = (int)Mathf.Clamp((bound.min.y - _sceneBounds.min.y) / _voxelStep, 0.0f, _resolution.y - 1),
            z = (int)Mathf.Clamp((bound.min.z - _sceneBounds.min.z) / _voxelStep, 0.0f, _resolution.z - 1)
        };
        Vector3Int endIndex = new Vector3Int
        {
            x = (int) Mathf.Clamp((bound.max.x - _sceneBounds.min.x) / _voxelStep, 0.0f, _resolution.x - 1),
            y = (int) Mathf.Clamp((bound.max.y - _sceneBounds.min.y) / _voxelStep, 0.0f, _resolution.y - 1),
            z = (int) Mathf.Clamp((bound.max.z - _sceneBounds.min.z) / _voxelStep, 0.0f, _resolution.z - 1)
        };

        return VoxelMeshBuilder.CreateMesh(
            this, startIndex, endIndex, new Vector3(_voxelStep, _voxelStep, _voxelStep), _sceneBounds.min
            );
    }

    public int GetVoxelValue(int x, int y, int z)
    {
        int index = (int)(y * _resolution.x * _resolution.z + z * _resolution.x + x);
        return _data[index];
    }
    
    /**
     *  Box mesh generator
     * 
     *        E__________________ H
     *       /|                 /|
     *      / |                / |
     *     /  |               /  |
     *   A/__________________/D  |
     *    |                  |   |
     *    |   |              |   |
     *    |   |              |   |
     *    |  F|______________|___|G
     *    |  /               |  /
     *    | /                | /
     *   B|/_________________|C
     *
     */
    private enum VertexIndex
    { A = 0, B = 1, C = 2, D = 3, E = 4, F = 5, G = 6, H = 7 };

    private void BuildVoxelVolumeVertexBuffer()
    {
        _voxelMesh = new Mesh();

        Vector3[] outPoints = {
            new Vector3( -1.0f, +1.0f, +1.0f ), // A
            new Vector3( -1.0f, -1.0f, +1.0f ), // B
            new Vector3( +1.0f, -1.0f, +1.0f ), // C
            new Vector3( +1.0f, +1.0f, +1.0f ), // D
            new Vector3( -1.0f, +1.0f, -1.0f ), // E
            new Vector3( -1.0f, -1.0f, -1.0f ), // F
            new Vector3( +1.0f, -1.0f, -1.0f ), // G
            new Vector3( +1.0f, +1.0f, -1.0f )  // H
        };

        int[] outIndices = {
            (int)VertexIndex.A, (int)VertexIndex.B, (int)VertexIndex.D, // ABD
            (int)VertexIndex.D, (int)VertexIndex.B, (int)VertexIndex.C, // DBC
            (int)VertexIndex.E, (int)VertexIndex.H, (int)VertexIndex.F, // EHF
            (int)VertexIndex.H, (int)VertexIndex.G, (int)VertexIndex.F, // HGF

            (int)VertexIndex.D, (int)VertexIndex.C, (int)VertexIndex.G, // DCG
            (int)VertexIndex.D, (int)VertexIndex.G, (int)VertexIndex.H, // DGH
            (int)VertexIndex.A, (int)VertexIndex.F, (int)VertexIndex.B, // AFB
            (int)VertexIndex.A, (int)VertexIndex.E, (int)VertexIndex.F, // AEF

            (int)VertexIndex.A, (int)VertexIndex.D, (int)VertexIndex.H, // ADH
            (int)VertexIndex.A, (int)VertexIndex.H, (int)VertexIndex.E, // AHE
            (int)VertexIndex.B, (int)VertexIndex.F, (int)VertexIndex.G, // BFG
            (int)VertexIndex.B, (int)VertexIndex.G, (int)VertexIndex.C, // BGC
        };
        
        _voxelMesh.SetVertices(outPoints);
        _voxelMesh.SetIndices(outIndices, MeshTopology.Triangles, 0);
        _voxelMesh.RecalculateNormals();
    }

    private void RenderVoxelMesh()
    {
        _voxelMaterial.SetBuffer(_voxelBufferID, _voxelBuffer);
        _voxelMaterial.SetVector(_sceneBoundsMinID, _sceneBounds.min);
        _voxelMaterial.SetVector(_resolutionID, _resolution);
        _voxelMaterial.SetFloat(_voxelStepID, _voxelStep);
        EnableDebugColor(_showDebugVoxelColor);

        if (_voxelMesh != null)
        {
            Graphics.DrawMeshInstanced(_voxelMesh, 0, _voxelMaterial, new Matrix4x4[_length]);
        }
    }

    private void BuildVoxelBuffer(VoxelRenderer voxelRenderer)
    {
        var material = voxelRenderer.material;
        material.SetBuffer(_voxelBufferID, _voxelBuffer);
    }

    private bool UpdateVoxelParam()
    {
        if (_objs.Count <= 0)
        {
            return false;
        }

        _sceneBounds = _objs[0].GetAABB();
        foreach (var obj in _objs)
        {
            _sceneBounds.Encapsulate(obj.GetAABB());
        }

        Vector3 min = _sceneBounds.min;
        Vector3 max = _sceneBounds.max;
        Vector3 range = max - min;
        _resolution = Vector3Int.zero;
        _resolution.x = (int) (range.x / _voxelStep) + 1;
        _resolution.y = (int) (range.y / _voxelStep) + 1;
        _resolution.z = (int) (range.z / _voxelStep) + 1;
        var length = (int)(_resolution.x * _resolution.y * _resolution.z);
        
        UpdateVoxelBuffer(length);
        if (length != _length)
        {
            _length = length;
        }

        foreach (var obj in _objs)
        {
            var material = obj.sharedMaterial;
            material.SetFloat(_voxelStepID, _voxelStep);
            material.SetVector(_sceneBoundsMinID, _sceneBounds.min);
            material.SetVector(_sceneBoundsMaxID, _sceneBounds.max);
            material.SetVector(_resolutionID, _resolution);
        }

        return true;
    }

    private void UpdateVoxelCamera()
    {
        Vector3 min = _sceneBounds.min;
        Vector3 max = _sceneBounds.max;
        Vector3 range = max - min;
        Matrix4x4[] cameraMat = new Matrix4x4[3];

        // X-Y plane
        var lookFrom = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, max.z + _voxelPlaneOffset);
        var lookTo = lookFrom + Vector3.back;
        var viewMat = Matrix4x4.LookAt(lookFrom, lookTo, Vector3.up).inverse;
        // print(viewMat);
        var orthoMat = Matrix4x4.Ortho(
            -range.x * 0.51f,
            range.x * 0.51f,
            -range.y * 0.51f,
            range.y * 0.51f,
            0.1f,
            range.z * 1.2f + _voxelPlaneOffset
        );
        // Flip z   
        var thirdCol = orthoMat.GetColumn(2);
        thirdCol *= -1;
        orthoMat.SetColumn(2, thirdCol);
        // print(orthoMat);
        orthoMat = GL.GetGPUProjectionMatrix(orthoMat, false);
        cameraMat[0] = orthoMat * viewMat;
        
        // Y-Z plane
        lookFrom = new Vector3(max.x + _voxelPlaneOffset, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f);
        lookTo = lookFrom + Vector3.left;
        viewMat = Matrix4x4.LookAt(lookFrom, lookTo, Vector3.up).inverse;
        orthoMat = Matrix4x4.Ortho(
            -range.z * 0.51f,
            range.z * 0.51f,
            -range.y * 0.51f,
            range.y * 0.51f,
            0.1f,
            range.x * 1.2f + _voxelPlaneOffset
        );
        // Flip z
        thirdCol = orthoMat.GetColumn(2);
        thirdCol *= -1;
        orthoMat.SetColumn(2, thirdCol);
        orthoMat = GL.GetGPUProjectionMatrix(orthoMat, false);
        cameraMat[1] = orthoMat * viewMat;
        
        // Z-X plane
        lookFrom = new Vector3((min.x + max.x) * 0.5f, max.y + _voxelPlaneOffset, (min.z + max.z) * 0.5f);
        lookTo = lookFrom + Vector3.down;
        viewMat = Matrix4x4.LookAt(lookFrom, lookTo, Vector3.forward).inverse;
        orthoMat = Matrix4x4.Ortho(
            -range.x * 0.51f,
            range.x * 0.51f,
            -range.z * 0.51f,
            range.z * 0.51f,
            0.1f,
            range.x * 1.2f + _voxelPlaneOffset
        );
        // Flip z
        thirdCol = orthoMat.GetColumn(2);
        thirdCol *= -1;
        orthoMat.SetColumn(2, thirdCol);
        orthoMat = GL.GetGPUProjectionMatrix(orthoMat, false);
        cameraMat[2] = orthoMat * viewMat;
        
        foreach (var obj in _objs)
        {
            var material = obj.sharedMaterial;
            material.SetMatrixArray(_voxelPlaneID, cameraMat);
            material.SetBuffer(_voxelBufferID, _voxelBuffer);
        }
    }

    private void UpdateVoxelBuffer(int length)
    {
        _voxelBuffer?.Release();
        _voxelBuffer = new ComputeBuffer(length, sizeof(int), ComputeBufferType.Structured);
        Graphics.ClearRandomWriteTargets();
        Graphics.SetRandomWriteTarget(1, _voxelBuffer,false);
        // var testList = new List<int>(length);  
        // _voxelBuffer.SetData(new List<int>(length));
    }
    
    private void EnableDebugColor(bool enable)
    {
        if (enable)
        {
            _voxelMaterial.EnableKeyword("VOXEL_MESH");
        }
        else
        {
            _voxelMaterial.DisableKeyword("VOXEL_MESH");
        }
    }

    private void ReadVoxelBuffer()
    {
        if (_voxelBuffer != null)
        {
            int[] data = new int[_length];
            _voxelBuffer.GetData(data);

            int voxelCount = 0;
            for (int i = 0; i < _length; i++)
            {
                if (data[i] >= 1)
                {
                    ++voxelCount;
                }
            }
            print($"Build Voxel: {voxelCount} / {_length}");
        }
    }

    private void OnDestroy()
    {
        _voxelBuffer?.Dispose();
    }
}
