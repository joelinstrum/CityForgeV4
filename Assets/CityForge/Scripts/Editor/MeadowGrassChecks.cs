using CityForge.CameraSystem;
using CityForge.World;
using UnityEditor;
using UnityEngine;

namespace CityForge.EditorTools
{
    public static class MeadowGrassChecks
    {
        [MenuItem("CityForge/Preview Meadow Detail")]
        public static void Preview()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter Play mode first.");
            var view = Object.FindFirstObjectByType<PrototypeRegionView>();
            if (view == null)
            {
                CityForge.Editor.PrototypeRegionChecks.Preview();
                view = Object.FindFirstObjectByType<PrototypeRegionView>();
            }
            view.GetComponent<RegionPreviewDiagnostics>().SetDiagnostic(false, 1);
            view.EnterDistrict(3);
            view.GetComponentInChildren<CityForgeCameraController>().SetZoomLevel(1);
        }

        [MenuItem("CityForge/Preview Meadow Coverage")]
        public static void PreviewCoverage()
        {
            Preview();
            var view = Object.FindFirstObjectByType<PrototypeRegionView>();
            view.GetComponentInChildren<CityForgeCameraController>().SetZoomLevel(4);
            var kit = Resources.Load<RegionTerrainKit>("Region/TerrainKit");
            if (kit.meadowDetail == null || kit.meadowDetailNormal == null
                || ShaderUtil.ShaderHasError(kit.terrainMaterial.shader))
                throw new System.InvalidOperationException("Rich meadow detail resources failed validation.");
            Debug.Log("CityForge: rich turf detail resources and URP shader verified.");
        }

        [MenuItem("CityForge/Validate Meadow Grass")]
        public static void Validate()
        {
            var view = Object.FindFirstObjectByType<PrototypeRegionView>();
            if (!EditorApplication.isPlaying || view == null) throw new System.InvalidOperationException("Open the region in Play mode first.");
            if (ShaderUtil.ShaderHasError(Shader.Find("CityForge/Meadow Grass"))) throw new System.InvalidOperationException("Meadow shader failed.");
            var grass = view.GetComponent<MeadowGrassRenderer>();
            var camera = view.GetComponentInChildren<CityForgeCameraController>();
            var diagnostics = view.GetComponent<RegionPreviewDiagnostics>();
            var district = view.SelectedDistrict; var zoom = camera.ZoomLevel;
            var wasGray = diagnostics.IsEnabled; var exaggeration = diagnostics.Exaggeration;
            var cameraPose = camera.transform.localToWorldMatrix;
            var height = view.Data.Heights[400, 100];
            var stage = 0; var until = EditorApplication.timeSinceStartup + 2;
            diagnostics.SetDiagnostic(false, 1); camera.SetZoomLevel(1);
            void Tick()
            {
                if (EditorApplication.timeSinceStartup < until) return;
                try
                {
                    if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Play mode ended during grass check.");
                    if (stage == 0)
                    {
                        if (!grass.IsVisible || grass.VisibleTufts == 0) throw new System.InvalidOperationException("Close grass is missing.");
                        Debug.Log($"CityForge meadow: {grass.VisibleTufts} candidate tufts, {grass.VisibleBatches} visible tiles at zoom 1 (soil coverage clips further in shader).");
                        camera.SetZoomLevel(3);
                    }
                    else if (stage == 1)
                    {
                        if (grass.IsVisible || grass.VisibleBatches != 0) throw new System.InvalidOperationException("Distant grass was not hidden.");
                        camera.SetZoomLevel(1); diagnostics.SetDiagnostic(true, 2);
                    }
                    else
                    {
                        if (grass.IsVisible || grass.VisibleBatches != 0) throw new System.InvalidOperationException("Diagnostic grass was not hidden.");
                        if (view.SelectedDistrict != district || view.Data.Heights[400, 100] != height) throw new System.InvalidOperationException("Grass modified the region.");
                        diagnostics.SetDiagnostic(wasGray, exaggeration); camera.SetZoomLevel(zoom);
                        if (camera.transform.localToWorldMatrix != cameraPose) throw new System.InvalidOperationException("Camera was not restored.");
                        Debug.Log("CityForge: meadow shader, close detail, distant hiding, grayscale hiding and region/camera preservation verified.");
                        EditorApplication.update -= Tick; return;
                    }
                    stage++; until = EditorApplication.timeSinceStartup + 1;
                }
                catch (System.Exception error)
                {
                    diagnostics.SetDiagnostic(wasGray, exaggeration); camera.SetZoomLevel(zoom);
                    EditorApplication.update -= Tick; Debug.LogException(error);
                }
            }
            EditorApplication.update += Tick;
        }
    }
}
