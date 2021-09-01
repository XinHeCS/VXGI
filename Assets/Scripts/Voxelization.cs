using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class Voxelization : MonoBehaviour
{
    public float _voxelStepX = 0.5f;
    
    public float _voxelStepY = 0.5f;
    
    public float _voxelStepZ = 0.5f;

    public float _voxelPlaneOffset = 0.2f;

    private Vector3Int _resolution;

    private int _length;

    private RenderTexture _albedoBuffer;
    
    private RenderTexture _normalBuffer;
    
    private RenderTexture _emissiveBuffer;

    private int[] _data;

    private Bounds _sceneBounds = new Bounds();
    
    private List<VoxelRenderer> _objs = new List<VoxelRenderer>();

    private Light _directionalLight;

    private Camera _camera;

    private int _voxelStepID = Shader.PropertyToID("_step");
    private int _resolutionID = Shader.PropertyToID("_resolution");
    private int _aledoBufferID = Shader.PropertyToID("_albedoBuffer");
    private int _normalBufferID = Shader.PropertyToID("_normalBuffer");
    private int _emissiveBufferID = Shader.PropertyToID("_emissiveBuffer");
    private int _sceneBoundsMinID = Shader.PropertyToID("_sceneMinAABB");
    private int _sceneBoundsMaxID = Shader.PropertyToID("_sceneMaxAABB");
    private int _voxelPlaneID = Shader.PropertyToID("_viewProject");
    private int _diretionLightID = Shader.PropertyToID("_sunDir");
    private int _cameraPosID = Shader.PropertyToID("_cameraPos");

    private void Start()
    {
        // _mainCamera = GetComponent<Camera>();
        _directionalLight = GameObject.Find("Directional Light").GetComponent<Light>();
        _camera = Camera.current;
    }

    private void Update()
    {
        if (UpdateVoxelParam())
        {
            UpdateVoxelCamera();
        }
        
        // ReadVoxelBuffer();
    }

    private void OnPostRender()
    {
        foreach (var obj in _objs)
        {
            obj.ShowVoxelMesh();
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
        _length = (int)(_resolution.x * _resolution.y * _resolution.z);
        
        UpdateVoxelBuffer(_length);

        foreach (var obj in _objs)
        {
            var material = obj.sharedMaterial;
            material.SetFloat(_voxelStepID, _voxelStepX);
            material.SetVector(_sceneBoundsMinID, _sceneBounds.min);
            material.SetVector(_sceneBoundsMaxID, _sceneBounds.max);
            material.SetVector(_resolutionID, new Vector3(_resolution.x, _resolution.y, _resolution.z));
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
            material.SetTexture(_aledoBufferID, _albedoBuffer);
            material.SetTexture(_normalBufferID, _normalBuffer);
            material.SetTexture(_emissiveBufferID, _emissiveBuffer);
        }
    }

    private void UpdateVoxelBuffer(int length)
    {
        if (_albedoBuffer == null)
        {
            _albedoBuffer =
                new RenderTexture(_resolution.x, _resolution.y, 0, RenderTextureFormat.ARGB32);
            _albedoBuffer.dimension = TextureDimension.Tex3D;
            _albedoBuffer.volumeDepth = _resolution.z;
            _albedoBuffer.enableRandomWrite = true;
            _albedoBuffer.Create();
        }

        if (_normalBuffer == null)
        {
            _normalBuffer =
                new RenderTexture(_resolution.x, _resolution.y, 0, RenderTextureFormat.ARGB32);
            _normalBuffer.dimension = TextureDimension.Tex3D;
            _normalBuffer.volumeDepth = _resolution.z;
            _normalBuffer.enableRandomWrite = true;
            _normalBuffer.Create();
        }
        
        if (_emissiveBuffer == null)
        {
            _emissiveBuffer =
                new RenderTexture(_resolution.x, _resolution.y, 0, RenderTextureFormat.ARGB32);
            _emissiveBuffer.dimension = TextureDimension.Tex3D;
            _emissiveBuffer.volumeDepth = _resolution.z;
            _emissiveBuffer.enableRandomWrite = true;
            _emissiveBuffer.Create();
        }
        
        Graphics.ClearRandomWriteTargets();
        Graphics.SetRandomWriteTarget(1, _albedoBuffer);
        Graphics.SetRandomWriteTarget(2, _normalBuffer);
        Graphics.SetRandomWriteTarget(3, _emissiveBuffer);
    }
}
