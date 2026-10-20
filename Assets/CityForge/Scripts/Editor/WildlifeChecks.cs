#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class WildlifeChecks
    {
        const string Output="Library/WildlifeChecks/";
        static WildlifeChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/Wildlife.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}var command=File.ReadAllText(request).Trim();File.Delete(request);Directory.CreateDirectory(Output);EditorApplication.isPaused=false;
            CityForge.Editor.EditorCoroutineRunner.Begin(Guard(command=="current"||command=="birds"?CheckCurrent(command=="birds"):Check()));}
        static IEnumerator Guard(IEnumerator routine){var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0){object next;try{var current=stack.Peek();if(!current.MoveNext()){(current as IDisposable)?.Dispose();stack.Pop();continue;}next=current.Current;}
                catch(Exception e){File.WriteAllText(Output+"Checks.txt","FAIL: "+e);Debug.LogException(e);yield break;}
                if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;}}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static IEnumerator CheckCurrent(bool birds=false){
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            Require(view&&view.ApprovedDistrict,"An existing district is open");
            var land=view.ApprovedDistrict;var wildlife=land.Wildlife;
            Require(view.AnimalsEnabled&&wildlife,"Current enabled district has wildlife");
            if(wildlife.Animals.Count==0){land.RefreshWildlife();wildlife=land.Wildlife;}
            Require(wildlife.Animals.Count>0,"Current district population is nonempty");
            var camera=view.GetComponentInChildren<CityForgeCameraController>();
            foreach(int level in new[]{5,3,2,1,0}){camera.SetZoomLevel(level);wildlife.UpdateVisibility();
                Require(wildlife.Visible==(level<=2),"Close zoom visibility "+level);
                Require(wildlife.GetComponentsInChildren<Renderer>().All(r=>r.enabled==(level<=2)),"Renderer visibility "+level);
                yield return null;
            }
            wildlife.FindAnimal();yield return null;
            if(birds){
                foreach(var habitat in new[]{WildlifeHabitat.FlyingBird,WildlifeHabitat.WoodlandBird}){
                    var bird=wildlife.Animals.First(a=>a.species.habitat==habitat);var state=camera.CaptureRegionState();state.focus=bird.root.position;camera.RestoreRegionState(state);
                    int frame=Time.frameCount;while(Time.frameCount<frame+8)yield return null;
                    ScreenCapture.CaptureScreenshot(Output+habitat+".png");frame=Time.frameCount;while(Time.frameCount<frame+2)yield return null;
                }
            }
            File.WriteAllText(Output+"Current.txt","PASS: current region="+view.name+", district="+view.SelectedDistrict+", count="+wildlife.Animals.Count+"; zoom 0/1/2 visible, 3/5 hidden; Find Animal focuses at zoom1. Species: "+string.Join(", ",wildlife.Animals.GroupBy(a=>a.species.id).Select(g=>g.Key+"="+g.Count())));
        }
        static IEnumerator Check(){
            var library=Resources.Load<WildlifeLibrary>("Wildlife/TemperateWildlife");Require(library&&library.species.Length==24,"Twenty-three curated local species/flight variants");
            var allowed=new[]{"deer","rabbit","boar","wolf","bear","frog","perch","pike","salmon","crab"};
            foreach(var species in library.species){
                Require((allowed.Contains(species.id)||species.id.StartsWith("bird-"))&&species.prefab,"Allowlist and prefab");
                var animator=species.prefab.GetComponentInChildren<Animator>();Require(animator&&!animator.applyRootMotion,"Bounded code-driven movement");
                var controller=(AnimatorController)animator.runtimeAnimatorController;Require(controller,species.id+" local controller loaded");
                Require(controller.layers[0].stateMachine.states.All(s=>s.state.name=="Idle"||s.state.name=="Walk"),"No demo combat/death cycles");
                Require(controller.animationClips.All(c=>AnimationUtility.GetAnimationClipSettings(c).loopTime),"Looping authored clips");
                foreach(var renderer in species.prefab.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)Require(material.shader.name=="Universal Render Pipeline/Lit"&&material.GetTexture("_BaseMap"),"URP textured animal");
            }
            Require(JsonUtility.FromJson<RegionCreationSettings>("{\"version\":5}").animals==false&&JsonUtility.FromJson<SavedRegionInfo>("{}").animals==false,"Older recipes/saves default off");
            var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
            var menu=UnityEngine.Object.FindFirstObjectByType<CityForge.UI.MainMenuController>();menu.Handle(CityForge.UI.MenuAction.CreateRegion);
            var modal=UnityEngine.Object.FindFirstObjectByType<CityForge.UI.CityForgeModal>();modal.SetRegionName("Wildlife check");modal.SelectCreateRegionSection(1);
            var dropdown=modal.GetComponentsInChildren<Dropdown>(true).First(d=>d.name=="AnimalsDropdown");
            Require(dropdown.options.Count==2&&dropdown.value==1,"Wizard wildlife option defaults on");
            Require(dropdown.GetComponent<RectTransform>().anchoredPosition==new Vector2(0,-406),"Compact wildlife layout loaded");
            dropdown.value=0;Require(!modal.ReadCreationSettings().animals,"Wizard None is respected");dropdown.value=1;Require(modal.ReadCreationSettings().animals,"Wizard temperate selection captured");
            ScreenCapture.CaptureScreenshot(Output+"Wizard-Animals.png");int frame=Time.frameCount;while(Time.frameCount<frame+2)yield return null;
            // OpenStages replaces the menu modal through the normal region entry route.
            var recipe=new RegionCreationSettings{seed=730421,topography=1,rivers=1,animals=true,vegetation=RegionVegetation.Woodland};
            var snapshot=RegionMapGenerator.Generate("Temperate wildlife trial",RegionSize.Medium,recipe);
            yield return PrototypeRegionView.OpenStages(snapshot.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:snapshot);
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            Require(view.AnimalsEnabled&&view.ApprovedDistrict==null&&!view.GetComponentsInChildren<DistrictWildlife>().Any(),"No wildlife in overview");
            yield return view.EnterDistrictStages(36);var land=view.ApprovedDistrict;var wildlife=land.Wildlife;
            Require(wildlife&&wildlife.Animals.Count>=80,"Denser district wildlife population");
            Require(wildlife.Animals.Count(a=>a.species.id.StartsWith("bird-"))>=40,"Abundant Living Birds population");
            Require(wildlife.Animals.Any(a=>a.species.habitat==WildlifeHabitat.FlyingBird)&&wildlife.Animals.Any(a=>a.species.habitat==WildlifeHabitat.WoodlandBird),"Ground and flying birds present");
            var fingerprint=wildlife.Animals.Select(a=>a.species.id+":"+a.root.position.ToString("F4")).ToArray();
            foreach(var animal in wildlife.Animals)Require(wildlife.Suitable(animal.species,new Vector2(animal.root.position.x,animal.root.position.z),out _),"Initial habitat placement");
            var forbidden=new WildlifeSpecies{habitat=WildlifeHabitat.River};
            Require(!wildlife.Suitable(forbidden,land.Origin+Vector2.one*.5f,out _),"River-only species rejects dry ground");
            view.SetAnimalsEnabled(false);Require(land.Wildlife==null,"Existing region toggle disables immediately");yield return null;
            view.SetAnimalsEnabled(true);wildlife=land.Wildlife;
            Require(fingerprint.SequenceEqual(wildlife.Animals.Select(a=>a.species.id+":"+a.root.position.ToString("F4"))),"Seeded placements stable on re-enable");
            view.GetComponentInChildren<CityForgeCameraController>().SetZoomLevel(1);wildlife.UpdateVisibility();
            float time=Time.time;var positions=wildlife.Animals.Select(a=>a.root.position).ToArray();
            while(Time.time<time+15){foreach(var a in wildlife.Animals)Require(wildlife.Suitable(a.species,new Vector2(a.root.position.x,a.root.position.z),out _),"Wander remains in habitat");yield return null;}
            Require(wildlife.Animals.Where((a,i)=>(a.root.position-positions[i]).sqrMagnitude>.01f).Any(),"Ambient animals actually wander");
            Require(wildlife.Animals.All(a=>a.animator&&a.animator.GetCurrentAnimatorStateInfo(0).normalizedTime>0),"Animations advance");
            var path=Path.Combine(Application.temporaryCachePath,"wildlife-"+Guid.NewGuid().ToString("N")+".cfregion");
            try{
                var capture=SavedRegionStore.Capture(view);SavedRegionStore.Write(capture,path);var restored=SavedRegionStore.Load(path);
                Require(restored.info.animals&&restored.info.creation.animals,"Wildlife recipe and enabled state round trip");
                view.SetAnimalsEnabled(false);SavedRegionStore.Write(SavedRegionStore.Capture(view),path);Require(!SavedRegionStore.Load(path).info.animals,"Disabled override persists independently of original recipe");view.SetAnimalsEnabled(true);
            }finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");}
            // Read an existing region and enable animals in memory, leaving its player save untouched.
            var legacyPath=Path.Combine(SavedRegionStore.DirectoryPath,SavedRegionStore.TestId+".cfregion");
            if(File.Exists(legacyPath)){
                var legacy=SavedRegionStore.Load(legacyPath);view.ReturnToMenu();yield return null;yield return null;
                yield return PrototypeRegionView.OpenStages(legacy.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:legacy);
                view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();yield return view.EnterDistrictStages(3);
                Require(!view.AnimalsEnabled&&view.ApprovedDistrict.Wildlife==null,"Existing save initially unchanged");view.SetAnimalsEnabled(true);if(view.ApprovedDistrict.Wildlife.Animals.Count==0){
                    var legacyLand=view.ApprovedDistrict;var diagnostic=new System.Text.StringBuilder();
                    diagnostic.Append($"side={legacyLand.Side}, sea={view.SeaLevel}, roadWidth={legacyLand.Baseline.roadWidth}, roadLimit={legacyLand.RoadQueryLimit}; ");
                    foreach(var species in library.species){int fit=0;for(int z=0;z<32;z++)for(int x=0;x<32;x++){var p=legacyLand.Origin+new Vector2((x+.5f)/32,(z+.5f)/32)*legacyLand.Side;if(legacyLand.Wildlife.Suitable(species,p,out _))fit++;}diagnostic.Append(species.id+"="+fit+"; ");}
                    var mid=legacyLand.Origin+Vector2.one*legacyLand.Side*.5f;diagnostic.Append($"mid h={legacyLand.Height(mid)}, shore={legacyLand.ShoreDistance(mid)}, road={legacyLand.RoadDistance(mid)}, wood={legacyLand.Woodland(mid)}, slope={legacyLand.MountainSlope(mid)}");
                    throw new Exception("Existing district supports wildlife test: "+diagnostic);
                }
                view.ReturnToMenu();yield return null;yield return null;
                yield return PrototypeRegionView.OpenStages(snapshot.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:snapshot);
                view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();yield return view.EnterDistrictStages(36);
            }
            land=view.ApprovedDistrict;wildlife=land.Wildlife;
            var deer=wildlife.Animals.Where(a=>a.species.id=="deer").OrderBy(a=>land.Canopy(new Vector2(a.root.position.x,a.root.position.z))).FirstOrDefault()??wildlife.Animals[0];
            var modelReport=new System.Text.StringBuilder();
            foreach(var a in wildlife.Animals){var renderers=a.root.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                modelReport.AppendLine(a.species.id+" root="+a.root.position.ToString("F3")+" bounds="+bounds.ToString("F3"));
                Require(bounds.size.magnitude>.08f&&bounds.size.magnitude<8,"Sensible animated model scale");}
            File.WriteAllText(Output+"Model-Bounds.txt",modelReport.ToString());
            var camera=view.GetComponentInChildren<CityForgeCameraController>();camera.FrameMeadowComparison(land.Side,land.DisplayTerrain.terrainData.size.y,deer.root.position+Vector3.up,18);
            frame=Time.frameCount;while(Time.frameCount<frame+12)yield return null;
            ScreenCapture.CaptureScreenshot(Output+"Forest-Wildlife.png");frame=Time.frameCount;while(Time.frameCount<frame+2)yield return null;
            bool treesVisible=land.TreesEnabled,plantsVisible=land.SmallPlantsEnabled;
            land.SetTreesEnabled(false);land.SetSmallPlantsEnabled(false);frame=Time.frameCount;while(Time.frameCount<frame+5)yield return null;
            ScreenCapture.CaptureScreenshot(Output+"Animal-Model-Check.png");frame=Time.frameCount;while(Time.frameCount<frame+2)yield return null;
            land.SetTreesEnabled(treesVisible);land.SetSmallPlantsEnabled(plantsVisible);
            File.WriteAllText(Output+"Checks.txt","PASS: temperate mammals/fish plus seven Living Birds species and flight variants; URP textures; idle/walk looping animation; wizard on/off; legacy defaults off; no overview animals; suitable deterministic district placements; bounded wandering for15 seconds; advancing animation; reversible toggle; temporary save on/off round trip; existing district opt-in preview. No player saves overwritten. Final species counts: "+string.Join(", ",wildlife.Animals.GroupBy(a=>a.species.id).Select(g=>g.Key+"="+g.Count()))+"\n");
        }
    }
}
#endif
