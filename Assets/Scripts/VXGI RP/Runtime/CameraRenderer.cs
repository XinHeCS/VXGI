using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
    const string _commandBufferName = "Render Camera";

    static ShaderTagId VoxelizationShaderTagId = new ShaderTagId("Voxelization");
    static ShaderTagId UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    
    ScriptableRenderContext _context;

    Camera _camera;

    CommandBuffer _commandBuffer = new CommandBuffer
    {
        name = _commandBufferName
    };

    CullingResults _cullingResults;

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        _context = context;
        _camera = camera;

        // Start rendering process
#if UNITY_EDITOR
        PrepareBuffer();
#endif
        if (!Cull())
        {
            return;
        }
        SetUp();
        DrawVisibleGeometry();
#if UNITY_EDITOR
        DrawGizmos();
#endif
        Submit();
    }

    private bool Cull()
    {
        if (_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            _cullingResults = _context.Cull(ref p);
            return true;
        }
        return false;
    }

    private void SetUp()
    {
        _context.SetupCameraProperties(_camera);
        _commandBuffer.ClearRenderTarget(true, true, Color.clear);
        _commandBuffer.BeginSample(_commandBufferName);
        ExecuteCommandBuffer();
    }

    private void DrawVisibleGeometry()
    {
        var sortSetting = new SortingSettings(_camera);
        var drawSetting = new DrawingSettings(
            UnlitShaderTagId, sortSetting
            );
        var filterSetting = new FilteringSettings(RenderQueueRange.all);
        _context.DrawRenderers(_cullingResults, ref drawSetting, ref filterSetting);
        
        _context.DrawSkybox(_camera);
    }

#if UNITY_EDITOR
    private void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }

    private void PrepareBuffer()
    {
        _commandBuffer.name = _camera.name;
    }
    
#endif

    private void Submit()
    {
        _commandBuffer.EndSample(_commandBufferName);
        ExecuteCommandBuffer();
        _context.Submit();
    }

    private void ExecuteCommandBuffer()
    {
        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }
}
