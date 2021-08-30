using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class Voxelization : MonoBehaviour
{
    public float _voxelStepX = 0.5f;
    
    public float _voxelStepY = 0.5f;
    
    public float _voxelStepZ = 0.5f;

    public float _voxelPlaneOffset = 0.2f;

    public bool _showVxoelMesh = true;
    
    public bool _showDebugVoxelColor = true;

    public Material _voxelMaterial;

    private Mesh _vxoelMesh;

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
        _vxoelMesh = new Mesh();
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
            // RenderVoxelMesh();
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
            x = (int)Mathf.Clamp((bound.min.x - _sceneBounds.min.x) / _voxelStepX, 0.0f, _resolution.x - 1),
            y = (int)Mathf.Clamp((bound.min.y - _sceneBounds.min.y) / _voxelStepX, 0.0f, _resolution.y - 1),
            z = (int)Mathf.Clamp((bound.min.z - _sceneBounds.min.z) / _voxelStepX, 0.0f, _resolution.z - 1)
        };
        Vector3Int endIndex = new Vector3Int
        {
            x = (int) Mathf.Clamp((bound.max.x - _sceneBounds.min.x) / _voxelStepX, 0.0f, _resolution.x - 1),
            y = (int) Mathf.Clamp((bound.max.y - _sceneBounds.min.y) / _voxelStepX, 0.0f, _resolution.y - 1),
            z = (int) Mathf.Clamp((bound.max.z - _sceneBounds.min.z) / _voxelStepX, 0.0f, _resolution.z - 1)
        };

        return VoxelMeshBuilder.CreateMesh(
            this, startIndex, endIndex, new Vector3(_voxelStepX, _voxelStepY, _voxelStepZ), _sceneBounds.min
            );
    }

    public int GetVoxelValue(int x, int y, int z)
    {
        int index = (int)(y * _resolution.x * _resolution.z + z * _resolution.x + x);
        return _data[index];
    }

    private void BuildVoxelVolumeVertexBuffer()
    {
        Vector3[] vertices = new Vector3[_length];
        int[] indices = new int[_length];

        for (int i = 0; i < _length; i++)
        {
            indices[i] = i;
        }

        if (vertices.Length > 65000)
        {
            _vxoelMesh.indexFormat = IndexFormat.UInt32;
        }
        
        _vxoelMesh.SetVertices(vertices);
        _vxoelMesh.SetIndices(indices, MeshTopology.Points, 0);
    }

    private void RenderVoxelMesh()
    {
        // _voxelMaterial.SetBuffer(_voxelBufferID, _voxelBuffer);
        // _voxelMaterial.SetVector(_sceneBoundsMinID, _sceneBounds.min);
        // _voxelMaterial.SetVector(_resolutionID, _resolution);
        // _voxelMaterial.SetFloat(_voxelStepID, _voxelStepX);
        // EnableDebugColor(_showDebugVoxelColor);
        _data = new int[_length];
        _voxelBuffer.GetData(_data);

        foreach (VoxelRenderer voxelRenderer in _objs)
        {
            if (voxelRenderer.voxelMesh == null)
            {
                voxelRenderer.voxelMesh = GetVoxelMesh(voxelRenderer);
            }
            Graphics.DrawMesh(voxelRenderer.voxelMesh, Matrix4x4.identity, _voxelMaterial, LayerMask.NameToLayer("Default"));
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
        _resolution.x = (int) (range.x / _voxelStepX) + 1;
        _resolution.y = (int) (range.y / _voxelStepY) + 1;
        _resolution.z = (int) (range.z / _voxelStepZ) + 1;
        var length = (int)(_resolution.x * _resolution.y * _resolution.z);
        
        UpdateVoxelBuffer(length);
        if (length != _length)
        {
            _length = length;
            BuildVoxelVolumeVertexBuffer();
        }

        foreach (var obj in _objs)
        {
            var material = obj.sharedMaterial;
            material.SetFloat(_voxelStepID, _voxelStepX);
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
