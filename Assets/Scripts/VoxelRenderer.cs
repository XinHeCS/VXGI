using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshFilter))]
public class VoxelRenderer : MonoBehaviour
{
    public bool _showVoxels = true;

    public Material _voxelMaterial;
    
    private Mesh _mesh;

    private GameObject _voxelGo;

    private MeshRenderer _meshRenderer;

    private MeshFilter _meshFilter;
    
    private Voxelization _voxelization;

    private bool GenerateVoxelMesh => _showVoxels && _voxelGo == null;

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

    public void ShowVoxelMesh()
    {
        if (GenerateVoxelMesh)
        {
            BuildVoxelMesh();
        }
        else
        {
            if (_voxelGo)
            {
                // _voxelGo.SetActive(false);
                // _meshRenderer.enabled = true;
            }
        }
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

    private void BuildVoxelMesh()
    {
        _voxelGo = new GameObject("Voxel mesh");

        var meshFilter = _voxelGo.AddComponent<MeshFilter>();
        meshFilter.mesh = _voxelization.GetVoxelMesh(this);
        var meshRenderer = _voxelGo.AddComponent<MeshRenderer>();
        meshRenderer.material = _voxelMaterial;

        _meshRenderer.enabled = false;
        _voxelGo.transform.SetParent(transform);
    }
}
