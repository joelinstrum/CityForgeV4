#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
using CityForge.UI;
namespace CityForge.Editor
{
    /// <summary>Real recipe, persistence, wizard and ordinary district checks. Writes only to the supplied private output folder.</summary>
    public static class RegionTerrainShapeChecks
    {
        static void Require(bool test,string message){if(!test)throw new Exception(message);}
        static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(owner);
        public static void Run(string output,bool changedOnly=false)=>EditorCoroutineRunner.Begin(Guard(Check(output,changedOnly),output));
        static IEnumerator Guard(IEnumerator check,string output)
        {
            Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Status.txt"),"RUNNING");
            while(true){bool next;try{next=check.MoveNext();}catch(Exception error){File.WriteAllText(Path.Combine(output,"Status.txt"),"FAIL "+error);yield break;}if(!next)break;yield return null;}
            File.WriteAllText(Path.Combine(output,"Status.txt"),"PASS terrain recipes, wizard, persistence and ordinary district checks");
        }
        static int Components(RegionTerrainLayout layout,out float fraction)
        {
            const int n=129;var cells=new bool[n*n];int land=0,components=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){cells[z*n+x]=layout.LandDistance(new Vector2(x/(float)(n-1),z/(float)(n-1)))>=0;if(cells[z*n+x])land++;}
            fraction=land/(float)(n*n);var queue=new Queue<int>();
            for(int i=0;i<cells.Length;i++)if(cells[i]){components++;queue.Enqueue(i);cells[i]=false;while(queue.Count>0){int p=queue.Dequeue(),x=p%n,z=p/n;foreach(var d in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down}){int xx=x+d.x,zz=z+d.y;if(xx<0||zz<0||xx>=n||zz>=n)continue;int j=zz*n+xx;if(cells[j]){cells[j]=false;queue.Enqueue(j);}}}}
            return components;
        }
        static IEnumerator Check(string output,bool changedOnly)
        {
            var previous=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(previous)previous.ReturnToMenu();yield return null;yield return null;
            var report=new System.Text.StringBuilder();var recipe=new RegionCreationSettings{version=5,seed=730421,topography=3,rivers=0,streams=0,climate=RegionClimate.Tropical,animals=false,peninsulaNorthToSouth=true};
            float[,] geography;using(var source=new MapMagicRegionGeography(recipe,RegionSize.Small))geography=source.Generate();
            var legacy=RegionMapGenerator.Generate("Legacy v5 check",RegionSize.Small,recipe,geography);Require(new RegionCreationSettings().version==12,"New default version12");
            foreach(RegionTerrainShape shape in Enum.GetValues(typeof(RegionTerrainShape))){
                recipe.version=6;recipe.terrainShape=shape;var layout=new RegionTerrainLayout(recipe);float[,] field=geography;
                if(shape==RegionTerrainShape.MountainBorder){using var source=new MapMagicRegionGeography(recipe,RegionSize.Small);field=source.Generate();}
                var map=RegionMapGenerator.Generate("Terrain "+shape,RegionSize.Small,recipe,field);var repeat=RegionMapGenerator.Generate("Repeat "+shape,RegionSize.Small,recipe,field);
                int components=Components(layout,out float fraction);Require(components==(shape==RegionTerrainShape.SmallIslands?9:shape==RegionTerrainShape.Archipelago?4:1),"Land component count "+shape);
                var ocean=map.data.WaterBodies.FirstOrDefault(b=>b.kind==WaterBodyKind.Ocean);Require((ocean!=null)==layout.HasOcean,"Semantic ocean "+shape);
                for(int z=0;z<513;z+=8)for(int x=0;x<513;x+=8){Require(map.data.Heights[z,x]==repeat.data.Heights[z,x]&&map.data.WaterDistance[z,x]==repeat.data.WaterDistance[z,x],"Determinism "+shape);var p=new Vector2(x/512f,z/512f);float d=layout.LandDistance(p);
                    if(layout.HasOcean&&Mathf.Abs(d)>.003f&&x>0&&z>0&&x<512&&z<512){Require(ocean.Contains(p)==(d<0),"Ocean excludes actual land "+shape);Require((map.data.Heights[z,x]*1200<12)==(d<0),"Coast elevation/descriptor agreement "+shape);}
                    if(shape==RegionTerrainShape.Land)Require(map.data.Heights[z,x]==legacy.data.Heights[z,x],"Land retains v5 height recipe");
                    if(shape==RegionTerrainShape.MountainBorder&&RegionTerrainLayout.Orient(p,recipe.seed).x<.65f)Require(map.data.Heights[z,x]*1200<70,"Mountains confined to selected edge");
                }
                if(shape==RegionTerrainShape.OneIsland||shape==RegionTerrainShape.Pangea){Require(!ocean.Contains(Vector2.one*.5f),"Island center is land");Require(!ocean.Intersects(new Rect(.49f,.49f,.02f,.02f)),"Interior district is not ocean");Require(ocean.Intersects(new Rect(.01f,.01f,.03f,.03f)),"Open ocean district recognized");}
                if(shape==RegionTerrainShape.SmallIslands)Require(ocean.holes.Length==9,"Archipelago holes");
                if(shape==RegionTerrainShape.Peninsula){int touched=0;foreach(var edge in new[]{0,1,2,3}){bool landEdge=false;for(int j=1;j<128;j++){float t=j/128f;var p=edge==0?new Vector2(0,t):edge==1?new Vector2(1,t):edge==2?new Vector2(t,0):new Vector2(t,1);if(layout.LandDistance(p)>0)landEdge=true;}if(landEdge)touched++;}Require(touched==1,"Peninsula joins mainland on exactly one edge");Require(layout.LandDistance(new Vector2(.5f,1))>0&&layout.LandDistance(new Vector2(.5f,0))<0,"Peninsula extends north to south");}
                string temp=Path.Combine(Application.temporaryCachePath,"terrain-check-"+Guid.NewGuid().ToString("N")+".cfregion");try{SavedRegionStore.Write(map,temp);var loaded=SavedRegionStore.Load(temp);Require(loaded.info.creation.version==6&&loaded.info.creation.terrainShape==shape&&loaded.info.creation.peninsulaNorthToSouth,"Recipe persistence "+shape);Require(loaded.data.Heights[128,256]==map.data.Heights[128,256],"Stored heights authoritative");if(ocean!=null){var savedOcean=loaded.data.WaterBodies.First(b=>b.kind==WaterBodyKind.Ocean);Require((savedOcean.holes?.Length??0)==(ocean.holes?.Length??0)&&savedOcean.Contains(new Vector2(.01f,.01f))==ocean.Contains(new Vector2(.01f,.01f)),"Ocean holes persistence");}}finally{if(File.Exists(temp))File.Delete(temp);}
                report.AppendLine("PASS "+shape+": components="+components+" landFraction="+fraction.ToString("F3")+" ocean="+(ocean!=null)+"; deterministic; geography/descriptor agreement; save round trip.");File.WriteAllText(Path.Combine(output,"Checks.txt"),report.ToString());yield return null;
            }
            // Actual existing prefab, runtime-added third page and button progression.
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();menu.Handle(MenuAction.CreateRegion);var modal=UnityEngine.Object.FindObjectsByType<CityForgeModal>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).First(m=>m.RegionName!=null&&Field<GameObject>(m,"createRegionBasicSettings"));modal.SetRegionName("Terrain Trial");modal.SetRegionSize(RegionSize.Small);
            Field<Button>(modal,"nextButton").onClick.Invoke();Require(Field<int>(modal,"selectedNavigationIndex")==1,"Basic next enters Environment");Field<Button>(modal,"terrainNextButton").onClick.Invoke();Require(Field<int>(modal,"selectedNavigationIndex")==2&&Field<GameObject>(modal,"terrainShapePanel").activeSelf,"Environment next enters third Terrain page");
            var dropdown=Field<Dropdown>(modal,"terrainShapeChoice");Require(dropdown.options.Select(o=>o.text).SequenceEqual(new[]{"Peninsula","Land","Coast","Mountain Border","Pangea","Archipelago"}),"All six choices");
            var shapes=new[]{RegionTerrainShape.Peninsula,RegionTerrainShape.Land,RegionTerrainShape.Coast,RegionTerrainShape.MountainBorder,RegionTerrainShape.OneIsland,RegionTerrainShape.SmallIslands};for(int i=0;i<shapes.Length;i++){dropdown.value=i;Require(modal.ReadCreationSettings().terrainShape==shapes[i],"Wizard recipe maps "+shapes[i]);}
            var environment=Field<GameObject>(modal,"createRegionTerrainIntro").transform;
            environment.Find("ClimateDropdown").GetComponent<Dropdown>().value=3;
            environment.Find("TopographyDropdown").GetComponent<Dropdown>().value=3;
            environment.Find("Land TypeDropdown").GetComponent<Dropdown>().value=2;
            Field<Dropdown>(modal,"generationAnimals").value=0;
            var seedInput=Field<InputField>(modal,"generationSeed");seedInput.text="invalid";Require(!Field<Button[]>(modal,"navigationButtons")[2].interactable&&!Field<Button>(modal,"createMapButton").interactable,"Invalid seed gates final creation");seedInput.text="730421";
            report.AppendLine("PASS three-page navigation, all six choice mappings, preserved Environment values and invalid-seed guard.");
            File.WriteAllText(Path.Combine(output,"Checks.txt"),report.ToString());
            foreach(var shape in shapes){
                if(changedOnly&&shape!=RegionTerrainShape.Peninsula)continue;
                menu.Handle(MenuAction.CreateRegion);modal.SetRegionName("Terrain Trial "+shape);modal.SetRegionSize(RegionSize.Small);modal.SelectCreateRegionSection(2);dropdown.value=Array.IndexOf(shapes,shape);
                if(shape==RegionTerrainShape.Peninsula){yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Terrain-Step3.png"));yield return null;yield return null;}
                var expected=modal.ReadCreationSettings();Field<Button>(modal,"createMapButton").onClick.Invoke();Require(menu.IsOpeningRegion&&UnityEngine.Object.FindFirstObjectByType<RegionLoadingOverlay>(),"Actual creation preloader "+shape);
                double deadline=EditorApplication.timeSinceStartup+180;while(menu.IsOpeningRegion&&EditorApplication.timeSinceStartup<deadline)yield return null;
                var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(!menu.IsOpeningRegion&&view&&view.SelectedDistrict==-1&&view.DistrictCount==64&&view.CreationSettings.terrainShape==shape,"Actual worker-backed wizard creation "+shape);
                Require(view.Climate==RegionClimate.Tropical&&view.CreationSettings.vegetation==RegionVegetation.Woodland,"Environment retained "+shape);
                yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,shape+"-Region.png"));yield return null;yield return null;
                if(shape==RegionTerrainShape.Coast||shape==RegionTerrainShape.OneIsland||shape==RegionTerrainShape.Peninsula){
                    var ocean=view.Data.WaterBodies.First(b=>b.kind==WaterBodyKind.Ocean);int district=view.Districts.Where(d=>d.waterFraction>.1f&&d.waterFraction<.8f).OrderBy(d=>Mathf.Abs(d.waterFraction-.35f)).First().index;
                    view.RequestDistrict(district);deadline=EditorApplication.timeSinceStartup+180;while(view.DistrictLoading&&EditorApplication.timeSinceStartup<deadline)yield return null;
                    Require(!view.DistrictLoading&&view.ApprovedDistrict,"Coastal district loaded "+shape);var land=view.ApprovedDistrict;Require(land.Water&&land.Palms,"Ocean and Tropical palms "+shape);land.SetSmallPlantsEnabled(false);
                    Require(land.Road.All(p=>land.WaterDistance(p)>8&&land.Height(p)>view.SeaLevel+.5f),"Generated roads stay on land "+shape);
                    var palms=land.Palms;Require(palms.NativeCoastalCandidates>0,"Real shore recognized by palm band "+shape);
                    var forest=land.GetComponent<ApprovedDistrictForest>();int near=palms.AuthoredCoastalCandidates;Require(palms.AuthoredPalmCount==near/2+(forest.PresentedTreeCount-near)*30/100,"Approved coastal palm quota "+shape);
                    var area=RegionBiomeMap.DistrictBounds(district,8);var center=area.center;var shore=ocean.Contours.SelectMany(c=>c).Where(area.Contains).OrderBy(p=>(p-center).sqrMagnitude).First();var plan=new RegionTerrainLayout(view.CreationSettings);var gradient=new Vector2(plan.LandDistance(shore+Vector2.right*.001f)-plan.LandDistance(shore-Vector2.right*.001f),plan.LandDistance(shore+Vector2.up*.001f)-plan.LandDistance(shore-Vector2.up*.001f)).normalized;var point=(shore+gradient*.01f-Vector2.one*.5f)*view.MapSide;
                    var rig=view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();var camera=rig.CaptureRegionState();camera.focus=new Vector3(point.x,land.Height(point)+5,point.y);camera.yaw=315;camera.viewTilt=1;camera.hasViewTilt=true;rig.RestoreRegionState(camera);rig.SetZoomLevel(2);
                    yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,shape+"-District.png"));yield return null;yield return null;
                    report.AppendLine("PASS actual "+shape+" district="+district+"; approved canopy="+forest.TreeCount+"; native palms="+palms.NativePalmCount+" authored palms="+palms.AuthoredPalmCount+"; shore candidates="+palms.NativeCoastalCandidates+"; ocean Crest and palm shoreline composition.");
                }
                report.AppendLine("PASS normal staged wizard -> "+shape+" regional relief, 64 districts, Tropical environment retained.");File.WriteAllText(Path.Combine(output,"Checks.txt"),report.ToString());
                view.ReturnToMenu();yield return null;yield return null;
            }
            menu.Handle(MenuAction.CreateRegion);modal.SetRegionName("New Region");modal.SetRegionSize(RegionSize.Medium);dropdown.value=1;modal.SelectCreateRegionSection(2);yield return null;yield return null;
            report.AppendLine("PASS creator left open at Terrain step3; test regions were unsaved. Existing user saves untouched.");File.WriteAllText(Path.Combine(output,"Checks.txt"),report.ToString());
        }
    }
}
#endif
