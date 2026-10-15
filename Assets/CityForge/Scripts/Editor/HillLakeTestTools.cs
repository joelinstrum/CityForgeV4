using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.World;
namespace CityForge.Editor
{
    public static class HillLakeTestTools
    {
        [MenuItem("CityForge/Meadow Migration/Create And Open Hill Lake Test")]
        static void Open()
        {
            if(!EditorApplication.isPlaying)throw new System.Exception("Start MainMenu in Play mode first.");
            var view=Object.FindFirstObjectByType<PrototypeRegionView>();if(view){view.ReturnToMenu();EditorCoroutineRunner.Begin(OpenAfterReturn());}else OpenSaved();
        }
        [MenuItem("CityForge/Meadow Migration/Measure Lake Reflection Camera")]
        static void MeasureReflectionCost(){var view=Object.FindFirstObjectByType<PrototypeRegionView>();if(!view||!view.ApprovedDistrict.Water)throw new System.Exception("Open Crest lake first.");view.gameObject.AddComponent<LakeReflectionCostBenchmark>();}
        [MenuItem("CityForge/Meadow Migration/Measure Lake Reflection Filtering")]
        static void MeasureFiltering(){var view=Object.FindFirstObjectByType<PrototypeRegionView>();if(!view||!view.ApprovedDistrict.Water)throw new System.Exception("Open Crest lake first.");view.gameObject.AddComponent<LakeReflectionFilteringBenchmark>();}
        [MenuItem("CityForge/Meadow Migration/Compare Lake Water Performance")]
        static void Compare(){LakePrototypeTools.Run();}
        [MenuItem("CityForge/Meadow Migration/Use Accepted Lake And Pond Renderer")]
        static void TryStill(){SetRenderer(true);}
        [MenuItem("CityForge/Meadow Migration/Restore Crest Lake")]
        static void RestoreCrest(){SetRenderer(false);}
        static void SetRenderer(bool still){
            var path=Path.Combine(SavedRegionStore.DirectoryPath,HillLakeTestRegion.Id+".cfregion");
            LakeWaterRenderer.EnabledForEvaluation=still;var saved=SavedRegionStore.Load(path);saved.info.stillWater=false;saved.info.lakeWaterVersion=still?1:-1;SavedRegionStore.Save(saved);Open();
        }
        static System.Collections.IEnumerator OpenAfterReturn(){yield return null;yield return null;OpenSaved();}
        static void OpenSaved()
        {
            var path=Path.Combine(SavedRegionStore.DirectoryPath,HillLakeTestRegion.Id+".cfregion");
            if(!File.Exists(path))SavedRegionStore.Save(HillLakeTestRegion.Generate());
            var saved=SavedRegionStore.Load(path);
            if(saved.info.camera.size==160){saved.info.camera.size=80;saved.info.camera.zoom=2;SavedRegionStore.Save(saved);}
            Object.FindFirstObjectByType<UI.MainMenuController>().OpenSavedRegion(saved.info);
            var view=Object.FindFirstObjectByType<PrototypeRegionView>();
            var water=view.ApprovedDistrict.Water;
            if(!LakeWaterRenderer.UsesCalmWater(view)&&(!water||water.FlowLod.Enabled||water.FoamLod.Enabled||water.AnimatedWavesLod.Enabled))throw new System.Exception("Lake water profile failed.");
            if(!LakeWaterRenderer.UsesCalmWater(view)){var material=water.Surface.Material;
                if(material.GetFloat("_Crest_NormalMapScrollSpeed")!=0||material.GetFloat("_Crest_PlanarReflectionsApplySmoothness")!=1||material.GetFloat("_Crest_Smoothness")!=.75f||material.GetFloat("_Crest_PlanarReflectionsIntensity")!=.75f)throw new System.Exception("Calm Crest lake material failed.");
                Debug.Log("PASS calm Crest lake: stationary normals, roughness-filtered reflections, reflection intensity 0.75.");
            }
            SavedRegionStore.Save(SavedRegionStore.Capture(view,HillLakeTestRegion.Id,"Meadow — Hill and Lake"));
            Debug.Log("PASS hill/lake loaded and saved; lake/pond renderer="+(LakeWaterRenderer.UsesCalmWater(view)?"accepted calm water":"Crest fallback"));
        }
    }
}
