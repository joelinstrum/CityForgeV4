#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class RegionBiomeChecks
    {
        public const string Output="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/Region-Biomes-Checks.txt";
        static RegionBiomeChecks(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Library/RegionBiomes.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            string action=File.ReadAllText(request).Trim();
            if(action=="integration"&&!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete(request);EditorApplication.isPaused=false;
            if(action=="integration")CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Integration()));else Run();
        }
        static IEnumerator Guard(IEnumerator routine)
        {
            while(true){object current;try{if(!routine.MoveNext())yield break;current=routine.Current;}catch(Exception e){File.AppendAllText(Output,"FAIL: "+e+"\n");Debug.LogException(e);yield break;}yield return current;}
        }
        [MenuItem("CityForge/Checks/Regional Biomes")] public static void Run()
        {
            try {
                File.WriteAllText(Output,"");
                var dry=RegionMapGenerator.Generate("Dry",RegionSize.Small,new RegionCreationSettings{seed=8472,topography=1,rivers=1,moisture=RegionMoisture.Dry,vegetation=RegionVegetation.OpenMeadow});
                var wet=RegionMapGenerator.Generate("Wet",RegionSize.Small,new RegionCreationSettings{seed=8472,topography=1,rivers=1,moisture=RegionMoisture.Wet,vegetation=RegionVegetation.Woodland});
                float dryTrees=0,wetTrees=0;
                for(int z=0;z<=40;z++)for(int x=0;x<=40;x++){
                    var uv=new Vector2(x/40f,z/40f);var a=dry.data.Biomes.At(uv);var b=wet.data.Biomes.At(uv);
                    Check(b.moisture>=a.moisture&&b.woodland>=a.woodland,"Moisture and vegetation choices influence ecology monotonically");dryTrees+=a.woodland;wetTrees+=b.woodland;
                    Check(dry.data.Heights[z*12,x*12]==wet.data.Heights[z*12,x*12],"Biome choices preserve terrain");
                }
                Check(wetTrees>dryTrees+100,"Substantial woodland difference");
                int grid=wet.info.creation.GridSide;
                for(int z=0;z<grid;z++)for(int x=0;x<grid;x++)for(int i=0;i<=16;i++){
                    float t=i/16f;int index=z*grid+x;var map=wet.data.Biomes;
                    if(x+1<grid)Same(map.AtDistrict(index,new Vector2(1,t)),map.AtDistrict(index+1,new Vector2(0,t)));
                    if(z+1<grid)Same(map.AtDistrict(index,new Vector2(t,1)),map.AtDistrict(index+grid,new Vector2(t,0)));
                }
                Check(wet.info.districts.Length==grid*grid,"Complete district grid");float area=0;foreach(var d in wet.info.districts)area+=d.bounds.width*d.bounds.height;Check(Mathf.Abs(area-1)<.0001f,"Districts cover region");
                var repeat=RegionMapGenerator.Generate("Repeat",RegionSize.Small,wet.info.creation);
                for(int i=0;i<grid*grid;i++)Check(wet.info.districts[i].woodland==repeat.info.districts[i].woodland,"Repeatable district summaries");
                string path=Path.Combine(Application.temporaryCachePath,"cityforge-biome-check.cfregion");
                try{SavedRegionStore.Write(wet,path);var loaded=SavedRegionStore.Load(path);Check(loaded.info.creation.moisture==RegionMoisture.Wet&&loaded.info.creation.vegetation==RegionVegetation.Woodland&&loaded.info.districts.Length==grid*grid,"Recipe and districts persist");var restored=new RegionBiomeMap(loaded.info.creation,loaded.data);Same(restored.At(new Vector2(.42f,.69f)),wet.data.Biomes.At(new Vector2(.42f,.69f)));}
                finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");}
                Check(RegionMapGenerator.Generate("Legacy",RegionSize.Small,new RegionCreationSettings{version=1}).data.Biomes==null,"Version-one maps retain legacy ecology");
                File.AppendAllText(Output,$"PASS data: user options change ecology; wet woodland potential {wetTrees/1681:F3} vs dry meadow {dryTrees/1681:F3}; complete district grid; every shared edge matches; seed repeatability; save/load recipe + district summaries + field match; version-one compatibility.\n");
            }catch(Exception e){File.AppendAllText(Output,"FAIL: "+e+"\n");Debug.LogException(e);}
        }
        static IEnumerator Integration()
        {
            Run();var check=RegionalReliefChecks.Check();while(check.MoveNext())yield return check.Current;
            File.AppendAllText(Output,"PASS live: expanded relief selection, isolated native districts, seasons and save/cache round trips (see Regional-Relief-Checks.txt).\n");
        }
        static void Same(RegionBiomeSample a,RegionBiomeSample b){Check(a.moisture==b.moisture&&a.woodland==b.woodland&&a.biome==b.biome,"Continuous deterministic biome fields");}
        static void Close(float a,float b){Check(Mathf.Abs(a-b)<.001f,"Rendered district terrain seam");}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    }
}
#endif
