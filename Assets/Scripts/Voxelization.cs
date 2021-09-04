using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class Voxelization : MonoBehaviour
{
    public float _voxelStep = 0.5f;

    public float _voxelPlaneOffset = 0.2f;

    private bool _voxelizeThisFrame = true;

    private Vector3Int _resolution;

    private RenderTexture _albedoBuffer;
    
    private RenderTexture _normalBuffer;
    
    private RenderTexture _emissiveBuffer;

    private Bounds _sceneBounds = new Bounds();

    private Matrix4x4[] _cameraMat = new Matrix4x4[3];

    private VXGIRenderPipeline _curRenderPipeline; 
    
    private List<VoxelRenderer> _objs = new List<VoxelRenderer>();

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

    private void Update()
    {
        // if (_curRenderPipeline == null)
        // {
        //     _curRenderPipeline = RenderPipelineManager.currentPipeline as VXGIRenderPipeline;
        // }
        // if (UpdateVoxelParam())
        // {
        //     // _curRenderPipeline.RenderVoxelThisFrame = false;
        //     // Graphics.
        //     VoxelizeScene();
        // }
        
        if (_voxelizeThisFrame)
        {
            if (UpdateVoxelParam())
            {
                // _voxelizeThisFrame = false;
                // Graphics.
                VoxelizeScene();
            }
        }

        // ReadVoxelBuffer();
    }

    private void OnDestroy()
    {
        _albedoBuffer.Release();
        _normalBuffer.Release();
        _emissiveBuffer.Release();
        _curRenderPipeline = null;
    }

    public RenderTexture AlbedoTexture => _albedoBuffer;
    public RenderTexture NormalTexture => _normalBuffer;
    public Vector3Int GridResolution => _resolution;
    public Bounds SceneBound => _sceneBounds;

    public void AddVoxelObjects(VoxelRenderer voxelRenderer)
    {
        if (_objs.Contains(voxelRenderer))
        {
            return;
        }
        _objs.Add(voxelRenderer);
        // BuildVoxelBuffer(voxelRenderer);
    }

    private void VoxelizeScene()
    {
        foreach (var voxelRenderer in _objs)
        {
            var tf = voxelRenderer.transform;
            SetUpVoxelMaterial(voxelRenderer.sharedMaterial);
            Graphics.DrawMesh(voxelRenderer.Mesh, tf.position, tf.rotation, voxelRenderer.sharedMaterial, 0);
        }
    }

    public void SetUpVoxelMaterial(Material mat, bool isDebug = false)
    {
        Graphics.ClearRandomWriteTargets();
        if (true)
        {
            Graphics.SetRandomWriteTarget(1, _albedoBuffer);
            Graphics.SetRandomWriteTarget(2, _normalBuffer);
            Graphics.SetRandomWriteTarget(3, _emissiveBuffer);
        }
        
        mat.SetTexture(_aledoBufferID, _albedoBuffer);
        mat.SetTexture(_normalBufferID, _normalBuffer);
        mat.SetTexture(_emissiveBufferID, _emissiveBuffer);
        
        mat.SetFloat(_voxelStepID, _voxelStep);
        mat.SetMatrixArray(_voxelPlaneID, _cameraMat);
        mat.SetVector(_sceneBoundsMinID, _sceneBounds.min);
        mat.SetVector(_sceneBoundsMaxID, _sceneBounds.max);
        mat.SetVector(_resolutionID, new Vector3(_resolution.x, _resolution.y, _resolution.z));
    }

    private bool UpdateVoxelParam()
    {
        if (_objs.Count <= 0)
        {
            return false;
        }
        
        UpdateBasicSceneInfo();
        UpdateVoxelBuffer();
        UpdateVoxelCamera();

        return true;
    }

    private void UpdateBasicSceneInfo()
    {
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
    }

    private void UpdateVoxelCamera()
    {
        Vector3 min = _sceneBounds.min;
        Vector3 max = _sceneBounds.max;
        Vector3 range = max - min;
        _cameraMat = new Matrix4x4[3];

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
        _cameraMat[0] = orthoMat * viewMat;
        
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
        _cameraMat[1] = orthoMat * viewMat;
        
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
        _cameraMat[2] = orthoMat * viewMat;
    }

    private void UpdateVoxelBuffer()
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
    }
}
