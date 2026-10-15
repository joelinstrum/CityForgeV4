using UnityEngine;
using UnityEngine.Rendering;
using WaveHarmonic.Crest;
namespace CityForge.World
{
    /// <summary>Opt-in downstream ripple translation from the seeded river map; Crest retains its normal behavior elsewhere.</summary>
    public sealed class RiverFlowAnimation : MonoBehaviour
    {
        public WaterRenderer water;
        public Texture flowMap;
        public Vector4 flowRegion;
        public float visualSpeedMultiplier = 5f;
        static readonly int Advection = Shader.PropertyToID("_CityForgeRiverAdvection");
        void OnEnable() => RenderPipelineManager.beginCameraRendering += BeforeCamera;
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            Shader.SetGlobalVector(Advection, Vector4.zero);
        }
        void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!water || !water.isActiveAndEnabled || !water.FlowLod.Enabled)
            {
                Shader.SetGlobalVector(Advection, Vector4.zero);
                return;
            }
            Shader.SetGlobalTexture("_CityForgeRiverFlowMap", flowMap);
            Shader.SetGlobalVector("_CityForgeRiverFlowRegion", flowRegion);
            Shader.SetGlobalVector(Advection, new Vector4(water.TimeProvider.Time, visualSpeedMultiplier, 0, 0));
        }
    }
}
