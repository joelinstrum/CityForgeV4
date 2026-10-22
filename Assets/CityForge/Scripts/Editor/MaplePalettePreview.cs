#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class MaplePalettePreview
    {
        static MaplePalettePreview(){EditorApplication.update+=Poll;}
        static void Poll(){
            const string request="Library/MaplePalette.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete(request);EditorApplication.isPaused=false;
            CityForge.Editor.EditorCoroutineRunner.Begin(Preview());
        }
        static IEnumerator Preview(){
            const string output="Library/MaplePalette/";Directory.CreateDirectory(output);
            var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
            var recipe=new RegionCreationSettings{seed=730421,topography=4,rivers=1};
            var snapshot=RegionMapGenerator.Generate("Summer maple palette preview",RegionSize.Medium,recipe);
            RegionBuildSteps.Complete(PrototypeRegionView.OpenStages(snapshot.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:snapshot));
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(36);
            var land=view.ApprovedDistrict;var trees=land.CanonicalTreeInstances;
            var first=trees.Where(t=>t.prototypeIndex==19).ToArray();var second=trees.Where(t=>t.prototypeIndex==20).ToArray();
            if(first.Length==0||second.Length==0)throw new Exception("Preview requires both Silver Maples in the canonical forest.");
            var a=first[0];var b=second.OrderBy(t=>(t.position-a.position).sqrMagnitude).First();
            float best=float.PositiveInfinity;
            foreach(var candidate in first){var other=second.OrderBy(t=>(t.position-candidate.position).sqrMagnitude).First();
                float distance=(candidate.position-other.position).sqrMagnitude;
                var center=(candidate.position+other.position)*.5f;
                if(center.x<.12f||center.x>.88f||center.z<.12f||center.z>.88f)continue;
                if(distance<best){best=distance;a=candidate;b=other;}}
            var p=land.Origin+new Vector2((a.position.x+b.position.x)*.5f,(a.position.z+b.position.z)*.5f)*land.Side;
            var camera=view.GetComponentInChildren<CityForgeCameraController>();
            camera.FrameMeadowComparison(land.Side,land.DisplayTerrain.terrainData.size.y,new Vector3(p.x,land.Height(p)+8,p.y),55);
            int frame=Time.frameCount;while(Time.frameCount<frame+12)yield return null;
            ScreenCapture.CaptureScreenshot(output+"Summer-Maples-Forest-Green.png");yield return null;
            File.WriteAllText(output+"Preview.txt","Both canonical Silver Maple A/B near camera; Summer season; existing enabled-species preference retained; disposable unsaved region. No player saves written.\n");
        }
    }
}
#endif
