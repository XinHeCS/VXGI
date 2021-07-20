using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshFilter))]
public class VoxelRenderer : MonoBehaviour
{
    public bool _showDebugVoxelColor = true;
    
    private Mesh _mesh;

    private GameObject _voxelGo;

    private MeshRenderer _meshRenderer;

    private MeshFilter _meshFilter;
    
    private Voxelization _voxelization;

    private bool GenerateVoxelMesh => _showDebugVoxelColor && _voxelGo == null;

    // Start is called before the first frame update
    void Start()
    {
        _voxelization =  Camera.main.GetComponent<Voxelization>();
        if (!_voxelization)
        {
            Debug.LogError("Voxelization component not found!");
        }

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _mesh = _meshFilter.sharedMesh;
        
        _voxelization.AddVoxelObjects(this);
    }

    public Bounds GetAABB()
    {
        return _meshRenderer.bounds;
    }

    public Material material
    {
        get
        {
            return _meshRenderer.material;
        }
    }

    public Material sharedMaterial
    {
        get
        {
            return _meshRenderer.sharedMaterial;
        }
    }

    public Mesh voxelMesh { get; set; }
}
