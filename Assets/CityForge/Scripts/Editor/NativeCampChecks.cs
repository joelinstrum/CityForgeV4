#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CityForge.World;
using CityForge.UI;
using CityForge.CameraSystem;
namespace CityForge.Editor
{
    public static class NativeCampChecks
    {
        [Serializable] sealed class Records {public NativeCampRecord[] camps;}
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static string Json(NativeCampRecord[] records)=>JsonUtility.ToJson(new Records{camps=records});
        public static void Unit(string output){
            Directory.CreateDirectory(output);var heights=new float[513,513];var water=new float[513,513];
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){heights[z,x]=(Mathf.Abs(x-256)<3?5f:15f)/PrototypeRegionData.Height;water[z,x]=Mathf.Abs(x-256)*8;}
            var data=new PrototypeRegionData(76142,heights,water){WaterBodies=new[]{new RegionWaterBody{kind=WaterBodyKind.River,centerline=new[]{new Vector2(.5f,0),new Vector2(.5f,1)},halfWidthNormalized=.005f}}};
            var recipe=new RegionCreationSettings{seed=data.Seed,nativeCampVersion=1,districtGrid=8};var camps=NativeCampPlanner.Create(data,recipe,0);
            Require(camps.Length>=2&&camps.Length<=3,"Two or three camps per region");Require(camps.All(c=>c.teepees>=5&&c.teepees<=9)&&camps.Select(c=>c.district).Distinct().Count()==camps.Length,"Counts and unique districts");
            Require(camps.All(c=>c.nearWater),"Water-first site selection");Require(Json(camps)==Json(NativeCampPlanner.Create(data,recipe,0)),"Deterministic complete camp metadata");
            NativeCampPlanner.Validate(camps,8,4096);
            var counts=new System.Collections.Generic.HashSet<int>();
            foreach(int grid in new[]{2,4,8,16})foreach(int seed in new[]{1,2,3,4,5,76142}){
                var variant=new RegionCreationSettings{seed=seed,districtGrid=grid,nativeCampVersion=1};
                var generated=NativeCampPlanner.Create(data,variant,0);
                Require(generated.Length>=2&&generated.Length<=3,"Camp cap across seeds and grid sizes");counts.Add(generated.Length);
            }
            Require(counts.SetEquals(new[]{2,3}),"Seeded targets include both two and three camps");
            foreach(var camp in camps){for(int i=0;i<camp.teepees;i++){
                var p=camp.TeepeePosition(i);Require(Mathf.Abs(Vector2.Distance(p,camp.center)-camp.Radius)<.01f,"Horseshoe radius");
                for(int j=i+1;j<camp.teepees;j++)Require(Vector2.Distance(p,camp.TeepeePosition(j))>6.5f,"Teepee separation");
            }}
            recipe.nativeCampVersion=0;Require(NativeCampPlanner.Create(data,recipe,0).Length==0,"Legacy recipe remains camp-free");
            Require(JsonUtility.FromJson<RegionCreationSettings>("{\"version\":12,\"districtGrid\":8}").nativeCampVersion==0,"Absent content flag stays off");recipe.nativeCampVersion=1;
            var info=new SavedRegionInfo{id="camp-unit",name="Camp unit",size=0,seed=data.Seed,district=-1,creation=recipe,nativeCamps=camps,waterBodies=data.WaterBodies,terrainHeightRange=PrototypeRegionData.Height};
            string path=Path.GetFullPath("Library/NativeCampRoundtrip-"+Guid.NewGuid().ToString("N")+".cfregion");
            try{SavedRegionStore.Write(new SavedRegionSnapshot{data=data,info=info},path);Require(Json(SavedRegionStore.Load(path).info.nativeCamps)==Json(camps),"Camp save/load roundtrip");}finally{if(File.Exists(path))File.Delete(path);}
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)heights[z,x]=5f/PrototypeRegionData.Height;
            Require(NativeCampPlanner.Create(data,recipe,0).Length==0,"All-water region never forces camps");
            var geography=new float[513,513];var controlRecipe=new RegionCreationSettings{seed=9173,districtGrid=2,rivers=1};
            var control=RegionMapGenerator.Generate("Control",RegionSize.Small,controlRecipe,geography);controlRecipe.nativeCampVersion=1;
            var withCamps=RegionMapGenerator.Generate("Camps",RegionSize.Small,controlRecipe,geography);
            Require(control.data.Heights.Cast<float>().SequenceEqual(withCamps.data.Heights.Cast<float>())&&control.data.WaterDistance.Cast<float>().SequenceEqual(withCamps.data.WaterDistance.Cast<float>()),"Camp content does not change canonical geography");
            File.WriteAllText(Path.Combine(output,"Unit-Checks.txt"),"PASS seeded 2–3 camps per region; 5–9 separated horseshoe teepees; water-first placement; complete deterministic records; content flag defaults off for legacy JSON; metadata validation/save roundtrip; all-water map produces zero camps; camp flag leaves geography/water fields identical.\n");
        }
        public static void Run(string output)=>EditorCoroutineRunner.Begin(Guard(output));
        static IEnumerator Guard(string output){
            Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Status.txt"),"RUNNING");var routine=Check(output);
            while(true){bool more;try{more=routine.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"Status.txt"),"FAIL "+e);(routine as IDisposable)?.Dispose();yield break;}if(!more)break;yield return routine.Current;}
            File.WriteAllText(Path.Combine(output,"Status.txt"),"PASS");
        }
        static IEnumerator Check(string output){
            Unit(output);Application.runInBackground=true;
            var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old){old.ReturnToMenu();yield return null;yield return null;}
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();Require(menu,"Main menu");
            menu.CreatePrototypeRegion("Native Settlements — Trial",RegionSize.Small,new RegionCreationSettings{nativeCampVersion=1,seed=921117,topography=0,rivers=1,streams=1,vegetation=RegionVegetation.OpenMeadow});
            double deadline=EditorApplication.timeSinceStartup+300;while(menu.IsOpeningRegion&&EditorApplication.timeSinceStartup<deadline)yield return null;
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&!menu.IsOpeningRegion&&view.NativeCamps.Length>0&&view.NativeCamps.Any(c=>c.nearWater),"Normal new-region generation with water-adjacent camp records");
            var camp=view.NativeCamps.Where(c=>c.nearWater).OrderByDescending(c=>c.teepees).FirstOrDefault()??view.NativeCamps[0];
            string records=Json(view.NativeCamps),id="native-camp-check-"+Guid.NewGuid().ToString("N");var snapshot=SavedRegionStore.Capture(view,id,"Native Settlements — Trial");string path=Path.Combine(SavedRegionStore.DirectoryPath,id+".cfregion");
            Require(!File.Exists(path),"Unique regression fixture");SavedRegionStore.Save(snapshot);
            view.ReturnToMenu();yield return null;yield return null;menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();menu.OpenSavedRegion(SavedRegionStore.Load(path).info);
            deadline=EditorApplication.timeSinceStartup+240;while(menu.IsOpeningRegion&&EditorApplication.timeSinceStartup<deadline)yield return null;
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&Json(view.NativeCamps)==records,"Normal saved overview retains every camp");
            Require(view.GetComponent<NativeCampMarkers>()&&view.transform.Find("RegionHud/Settlement markers").childCount==view.NativeCamps.Length,"Overview markers correspond to actual saved camps");
            float overviewUntil=Time.time+1;while(Time.time<overviewUntil)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Camp-Overview.png"));yield return null;yield return null;
            view.RequestDistrict(camp.district);deadline=EditorApplication.timeSinceStartup+300;while(view.DistrictLoading&&EditorApplication.timeSinceStartup<deadline)yield return null;
            Require(!view.DistrictLoading,"Camp district finishes loading");var land=view.ApprovedDistrict;var district=land.GetComponent<NativeCampDistrict>();
            Require(district&&district.Camps.Count==1&&district.Teepees.Count==camp.teepees&&district.TotemCount==1,"Five to nine teepees and central totem");
            var totem=land.GetComponentsInChildren<Transform>().First(t=>t.name=="Central totem");
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Resources/NativeCamps/Totem.fbx");
            Require(totem.childCount==1&&Quaternion.Angle(totem.GetChild(0).localRotation,imported.transform.localRotation)<.01f,"Totem preserves FBX up-axis independently of camp yaw");
            var population=district.Populations.Single();Require(population.People.Count==camp.teepees+3,"Chief and balanced resident population");
            var assets=Resources.Load<NativeCampAssets>("NativeCamps/Assets");Require(population.People.Count(p=>p.Definition==assets.chief)==1&&population.People.Count(p=>p.Definition==assets.warrior)==(population.People.Count-1)/2,"Distinct character definitions");
            Require(land.CanonicalTreeInstances.All(t=>!view.InCampClearing(land.Origin+new Vector2(t.position.x,t.position.z)*land.Side)),"Camp clearing excludes native trees");
            var rig=view.GetComponentInChildren<CityForgeCameraController>();rig.SetHeading(camp.heading+135);rig.ConfigureTerrainStudy(population.Center,36,1);
            var initial=population.People.Select(p=>p.Record.position).ToArray();float until=Time.time+30;int invalid=0;float largest=0;
            while(Time.time<until){foreach(var person in population.People){float distance=Vector2.Distance(new Vector2(person.Record.position.x,person.Record.position.z),camp.center);largest=Mathf.Max(largest,distance);if(distance>camp.Clearance+.1f||!population.Navigation.Suitable(person.Record.position))invalid++;}yield return null;}
            Require(invalid==0&&population.People.Where((p,i)=>Vector3.Distance(initial[i],p.Record.position)>.2f).Count()>2,"Residents move inside the camp leash on safe paths");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Native-Camp.png"));yield return null;yield return null;rig.SetZoomLevel(0);until=Time.time+1;while(Time.time<until)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Native-Camp-Close.png"));yield return null;yield return null;
            File.WriteAllText(Path.Combine(output,"Checks.txt"),$"PASS normal new-region worker generation, {view.NativeCamps.Length}/64 saved camp districts ({view.NativeCamps.Count(c=>c.nearWater)} near water), normal save/open, district {camp.district}: {camp.teepees} teepees, one totem, {population.People.Count} residents, one chief/balanced warriors and female residents. Upright totem wrapper verified; native tree clearing; 30-second safe movement within {largest:F1}m (limit {camp.Clearance:F1}m). New fixture only; existing saves untouched. Camp records persist; individual ambient activities reset on district load.\n");
            File.Delete(path); // Only this run's unique fixture; never a user save.
        }
    }
}
#endif
