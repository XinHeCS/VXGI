using System;
using System.Net.Sockets;
using TreeEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshFilter))]
public class VoxelRenderer : MonoBehaviour
{
    public Material _debugMaterial;

    public Material _voxelizationMaterial;
    
    private GameObject _voxelGo;

    private MeshFilter _meshFilter;
    
    private Voxelization _voxelization;

    // Start is called before the first frame update
    void Start()
    {
        _voxelization =  Camera.main.GetComponent<Voxelization>();
        if (!_voxelization)
        {
            Debug.LogError("Voxelization component not found!");
        }

        _meshFilter = GetComponent<MeshFilter>();

        _voxelization.AddVoxelObjects(this);

        RenderPipelineManager.beginCameraRendering += OnBeforeCameraRender;
    }

    public Bounds GetAABB()
    {
        return _meshFilter.mesh.bounds;
    }

    public Material Material => _voxelizationMaterial;
    public Material sharedMaterial => _voxelizationMaterial;
    public Mesh Mesh => _meshFilter.mesh;

    private void OnBeforeCameraRender(ScriptableRenderContext content, Camera renderCamera)
    {
        if (_debugMaterial != null)
        {
            _voxelization.SetUpVoxelMaterial(_debugMaterial, true);
            var tf = transform;
            Graphics.DrawMesh(_meshFilter.mesh, tf.position, tf.rotation, _debugMaterial, 0);
        }
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeforeCameraRender;
    }
}
