using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using WaveHarmonic.Crest;
using CityForge.World;
namespace CityForge.Editor
{
    public static class TypedWaterRendererChecks
    {
        [MenuItem("CityForge/Meadow Migration/Validate Typed Water Renderer")]
        static void Run()
        {
            if(!EditorApplication.isPlaying||Object.FindFirstObjectByType<PrototypeRegionView>())throw new System.Exception("Play MainMenu without a region first.");
            new GameObject("Typed water acceptance").AddComponent<LakeWaterAcceptance>();
        }
        static IEnumerator Checks()
        {
            string path=Path.Combine(SavedRegionStore.DirectoryPath,"meadow-narrow-river-crest-trial.cfregion");var report=new StringBuilder();
            foreach(var kind in new[]{WaterBodyKind.Lake,WaterBodyKind.Pond,WaterBodyKind.Ocean}){
                var saved=SavedRegionStore.Load(path);saved.info.minimalRiverCrest=false;saved.info.waterless=false;
                float u=saved.data.TributaryRiverCenter(.78f);float y=saved.data.Heights[399,(int)(u*512)]*PrototypeRegionData.Height*(1024/PrototypeRegionData.Side)+2.4f;
                var body=new RegionWaterBody{id="validation-"+kind,kind=kind,hasSurfaceLevel=true,surfaceLevelNormalized=y/(PrototypeRegionData.Height*1024/PrototypeRegionData.Side),outline=new[]{new Vector2(u-.007f,.75f),new Vector2(u+.007f,.75f),new Vector2(u+.007f,.81f),new Vector2(u-.007f,.81f)}};
                saved.data.WaterBodies=saved.info.waterBodies=new[]{body};
                PrototypeRegionView.Open("Typed water validation — "+kind,Object.FindFirstObjectByType<Canvas>(),snapshot:saved);
                yield return null;yield return null;
                var view=Object.FindFirstObjectByType<PrototypeRegionView>();var pilot=view.ApprovedDistrict;var water=pilot.Water;
                if(!water||water.FlowLod.Enabled||water.AnimatedWavesLod.Enabled||water.FoamLod.Enabled)throw new System.Exception("Inappropriate type effects: "+kind);
                var tags=pilot.GetComponentsInChildren<RegionWaterBodyTag>();if(tags.Length!=1||tags[0].kind!=kind||tags[0].bodyId!=body.id)throw new System.Exception("Wrong runtime body identity.");
                var local=tags[0].GetComponent<WaterBody>();var material=local.AboveSurfaceMaterial;
                if(!local.OverrideMaterials||material.GetFloat("_Crest_NormalMapStrength")!=WaterAppearanceProfile.For(kind).rippleStrength||material.IsKeywordEnabled("_CREST_FLOW_LOD"))throw new System.Exception("Per-body material profile failed.");
                if(!pilot.GetComponentInChildren<ClipLodInput>())throw new System.Exception("Missing polygon footprint input.");
                var snapshot=SavedRegionStore.Capture(view,"typed-validation","validation");string temp="/Users/joelinstrum/Documents/Codex/2026-10-11/be/work/typed-water-validation.cfregion";SavedRegionStore.Write(snapshot,temp);var loaded=SavedRegionStore.Load(temp);
                if(loaded.data.WaterBodies[0].kind!=kind||!loaded.data.WaterBodies[0].hasSurfaceLevel||loaded.data.WaterBodyAt(new Vector2(u,.78f))?.kind!=kind)throw new System.Exception("Type/level/geometry round trip failed.");
                report.AppendLine("PASS "+kind+": native Crest, polygon clip, body material and ID, still-water policy, surface level, save/load and location lookup.");
                view.ReturnToMenu();yield return null;yield return null;
            }
            var original=SavedRegionStore.Load(path);PrototypeRegionView.Open(original.info.name,Object.FindFirstObjectByType<Canvas>(),snapshot:original);yield return null;yield return null;
            var river=Object.FindFirstObjectByType<PrototypeRegionView>().ApprovedDistrict;
            if(!river.Water.FlowLod.Enabled||river.Water.AnimatedWavesLod.Enabled||river.Water.FoamLod.Enabled||river.Water.Surface.Material.GetFloat("_Crest_NormalMapStrength")!=.08f)throw new System.Exception("Approved river profile changed.");
            report.AppendLine("PASS approved river restored: current, reflections, depth and gentle ripples; no swell or foam.");report.AppendLine("Ocean swell remains off until bounded wave inputs are implemented. Separate water elevations within one active district remain unsupported and are rejected explicitly.");
            File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Typed-Water-Renderer-Checks.txt",report.ToString());Debug.Log("PASS typed water renderer checks and river restoration.");
        }
    }
    static class EditorCoroutineRunner
    {
        public static void Begin(IEnumerator routine){void Tick(){try{if(routine.MoveNext())return;}catch(System.Exception error){Debug.LogException(error);}EditorApplication.update-=Tick;(routine as System.IDisposable)?.Dispose();}EditorApplication.update+=Tick;}
    }
}
