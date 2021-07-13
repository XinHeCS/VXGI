using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Render/VXGI/Render Pipeline Asset")]
public class VXGIRenderPipelineAsset : RenderPipelineAsset
{
    protected override RenderPipeline CreatePipeline()
    {
        return new VXGIRenderPipeline();
    }
}
