using UnityEngine;
using UnityEngine.Rendering;
using CityForge.CameraSystem;
namespace CityForge.World
{
    /// <summary>Distance coverage and tree-only shadow policy; never edits shared materials or meshes.</summary>
    public sealed class ForestDistancePresentation : MonoBehaviour
    {
        static readonly int Coverage = Shader.PropertyToID("_DistanceCoverage");
        CityForgeCameraController rig;
        Renderer[] trees;
        ShadowCastingMode[] originalShadows;
        MaterialPropertyBlock[] originalBlocks;
        bool useCoverage = true, suppressFarShadows = true;
        public void Initialize(CityForgeCameraController cameraRig, Renderer[] treeRenderers)
        {
            rig = cameraRig; trees = treeRenderers;
            originalShadows = new ShadowCastingMode[trees.Length];
            originalBlocks = new MaterialPropertyBlock[trees.Length];
            for (int i = 0; i < trees.Length; i++)
            {
                originalShadows[i] = trees[i].shadowCastingMode;
                originalBlocks[i] = new MaterialPropertyBlock();
                trees[i].GetPropertyBlock(originalBlocks[i]);
            }
            rig.ZoomChanged += Apply;
            Apply(rig.ZoomLevel);
        }
        public void SetDiagnosticPolicy(bool coverage, bool shadowsOff)
        {
            useCoverage = coverage; suppressFarShadows = shadowsOff;
            if (rig != null) Apply(rig.ZoomLevel);
        }
        void Apply(int level)
        {
            bool far = level >= 4;
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < trees.Length; i++)
            {
                trees[i].shadowCastingMode = far && suppressFarShadows ? ShadowCastingMode.Off : originalShadows[i];
                if (far && useCoverage)
                {
                    trees[i].GetPropertyBlock(block);
                    block.SetFloat(Coverage, 1);
                    block.SetFloat("_FarCanopyPresentation", 1);
                    trees[i].SetPropertyBlock(block);
                }
                else
                {
                    trees[i].GetPropertyBlock(block);
                    block.SetFloat(Coverage, originalBlocks[i].GetFloat(Coverage));
                    block.SetFloat("_FarCanopyPresentation", 0);
                    trees[i].SetPropertyBlock(block);
                }
            }
        }
        void OnDestroy()
        {
            if (rig != null) rig.ZoomChanged -= Apply;
            if (trees == null || originalShadows == null || originalBlocks == null) return;
            for (int i = 0; i < trees.Length; i++) if (trees[i] != null)
            {
                trees[i].shadowCastingMode = originalShadows[i];
                trees[i].SetPropertyBlock(originalBlocks[i]);
            }
        }
    }
}
