#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
using CityForge.UI;
namespace CityForge.Editor
{
    public static class DistrictClimateHudChecks
    {
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(owner);
        public static void Run(string savedRegion,string output)=>EditorCoroutineRunner.Begin(Guard(Check(savedRegion,output),output));
        static IEnumerator Guard(IEnumerator check,string output){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Status.txt"),"RUNNING");while(true){bool next;try{next=check.MoveNext();}catch(Exception error){File.WriteAllText(Path.Combine(output,"Status.txt"),"FAIL "+error);(check as IDisposable)?.Dispose();yield break;}if(!next)break;yield return null;}File.WriteAllText(Path.Combine(output,"Status.txt"),"PASS district navigation, Tropical evergreen seasons, Temperate seasons and save round trips");}
        static Button Button(PrototypeRegionView view,string name)=>view.transform.Find("RegionHud/"+name).GetComponent<Button>();
        static string Caption(PrototypeRegionView view,string name)=>Button(view,name).GetComponentInChildren<Text>().text;
        static IEnumerator Check(string savedRegion,string output)
        {
            string temp=Path.Combine(Application.temporaryCachePath,"district-climate-"+Guid.NewGuid().ToString("N")+".cfregion");
            try{
                var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
                var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();var report=new System.Text.StringBuilder();
                foreach(var climate in new[]{RegionClimate.Tropical,RegionClimate.Temperate}){
                    var snapshot=SavedRegionStore.Load(savedRegion);Require(snapshot.info.creation!=null,"Recipe fixture required");snapshot.info.creation.climate=climate;snapshot.info.climate=climate;snapshot.info.season=climate==RegionClimate.Tropical?2:0;
                    int district=snapshot.info.meadowCacheDistrict;SavedRegionStore.Write(snapshot,temp);menu.OpenSavedRegion(SavedRegionStore.Load(temp).info);
                    double deadline=UnityEditor.EditorApplication.timeSinceStartup+180;while(menu.IsOpeningRegion&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;
                    var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&!menu.IsOpeningRegion,"Normal fixture load");view.RequestDistrict(district);while(view.DistrictLoading&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;
                    Require(!view.DistrictLoading&&view.ApprovedDistrict,"District preloader");var land=view.ApprovedDistrict;land.SetSmallPlantsEnabled(false);
                    Require(Caption(view,"SaveRegion")=="SAVE DISTRICT"&&Caption(view,"ReturnToMenu")=="RETURN TO REGION","District action captions");Require(!view.transform.Find("RegionHud/RegionOverview").gameObject.activeSelf,"Duplicate overview action hidden");
                    var canonical=land.CanonicalTreeInstances.ToArray();var prototypes=land.DisplayTerrain.terrainData.treePrototypes.Select(p=>p.prefab).ToArray();var presented=land.DisplayTerrain.terrainData.treeInstances.ToArray();
                    if(climate==RegionClimate.Tropical){
                        Require(view.Season==RegionSeason.Winter&&view.VisualSeason==RegionSeason.Summer&&Field<RegionSeason>(land,"treeSeason")==RegionSeason.Summer,"Winter save loads evergreen Tropical presentation");
                        Require(!Button(view,"SeasonFall").interactable&&!Button(view,"SeasonWinter").interactable&&Button(view,"SeasonSummer").interactable,"Tropical seasonal controls");
                        int palms=land.Palms.NativePalmCount+land.Palms.AuthoredPalmCount;
                        foreach(var season in new[]{RegionSeason.Summer,RegionSeason.Fall,RegionSeason.Winter}){view.SetSeason(season);yield return null;
                            Require(view.Season==season&&view.VisualSeason==RegionSeason.Summer,"Calendar versus visual season");Require(prototypes.SequenceEqual(land.DisplayTerrain.terrainData.treePrototypes.Select(p=>p.prefab))&&presented.SequenceEqual(land.DisplayTerrain.terrainData.treeInstances),"Tropical native presentation stable");
                            Require(Field<RegionSeason>(land,"treeSeason")==RegionSeason.Summer&&canonical.SequenceEqual(land.CanonicalTreeInstances),"Tropical canonical/season stable");Require(palms==land.Palms.NativePalmCount+land.Palms.AuthoredPalmCount,"Tropical palms stable");
                            foreach(var light in land.GetComponentsInChildren<Light>())if(light.type==LightType.Directional)Require(Mathf.Approximately(light.shadowStrength,.62f)&&light.shadows==LightShadows.Soft,"Tropical shadow response stable");
                        }
                        land.SetSeason(RegionSeason.Winter);Require(Field<RegionSeason>(land,"treeSeason")==RegionSeason.Summer,"Direct landscape seasonal call respects climate");
                        var capture=SavedRegionStore.Capture(view);Require(capture.info.season==2&&capture.meadow.trees.SequenceEqual(canonical),"Calendar/canonical save capture");SavedRegionStore.Write(capture,temp);Require(SavedRegionStore.Load(temp).info.season==2,"Calendar persists without changing visual climate");
                        yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Tropical-Winter.png"));yield return null;yield return null;
                        report.AppendLine("PASS Tropical: Fall/Winter disabled; winter save, calendar transitions and direct terrain calls retain summer prototypes/native placements/canopy policy/palms/shadows; calendar and canonical save round trip.");
                    }else{
                        Require(Button(view,"SeasonFall").interactable&&Button(view,"SeasonWinter").interactable,"Temperate controls enabled");
                        Button(view,"SeasonFall").onClick.Invoke();Require(view.Season==RegionSeason.Fall&&view.VisualSeason==RegionSeason.Fall&&Field<RegionSeason>(land,"treeSeason")==RegionSeason.Fall,"Temperate Fall works");
                        Button(view,"SeasonWinter").onClick.Invoke();Require(view.VisualSeason==RegionSeason.Winter&&Field<RegionSeason>(land,"treeSeason")==RegionSeason.Winter&&!prototypes.SequenceEqual(land.DisplayTerrain.terrainData.treePrototypes.Select(p=>p.prefab)),"Temperate Winter works");Require(canonical.SequenceEqual(land.CanonicalTreeInstances),"Temperate canonical unchanged");
                        report.AppendLine("PASS Temperate: Fall/Winter controls enabled and change seasonal prototypes; canonical instances unchanged.");
                    }
                    int id=view.GetInstanceID();Button(view,"ReturnToMenu").onClick.Invoke();yield return null;
                    Require(view&&view.GetInstanceID()==id&&view.SelectedDistrict==-1,"Top district return opens same region overview");Require(Caption(view,"SaveRegion")=="SAVE REGION"&&Caption(view,"ReturnToMenu")=="RETURN TO MENU","Overview captions restored");
                    Require(SavedRegionStore.Capture(view).meadow.trees.SequenceEqual(canonical),"Returning retains district cache");
                    yield return null;yield return null;if(climate==RegionClimate.Tropical)ScreenCapture.CaptureScreenshot(Path.Combine(output,"Region-Overview.png"));yield return null;
                    Button(view,"ReturnToMenu").onClick.Invoke();yield return null;yield return null;Require(!UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>(),"Overview Return to Menu retains original menu behavior");
                    report.AppendLine("PASS "+climate+" navigation: district Save District/Return to Region; one-click overview; cached district preserved; overview Save Region/Return to Menu.");File.WriteAllText(Path.Combine(output,"Checks.txt"),report.ToString());
                }
                menu.OpenSavedRegion(SavedRegionStore.Load(savedRegion).info);while(menu.IsOpeningRegion)yield return null;var restored=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();restored.RequestDistrict(restored.LoadedMeadowDistrict);while(restored.DistrictLoading)yield return null;restored.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>().SetZoomLevel(1);
                report.AppendLine("PASS original saved region restored; only temporary fixture files written.");File.WriteAllText(Path.Combine(output,"Checks.txt"),report.ToString());
            }finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
#endif
