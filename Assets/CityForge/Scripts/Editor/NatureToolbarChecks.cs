#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CityForge.UI;
using CityForge.World;
namespace CityForge.Editor
{
    public static class NatureToolbarChecks
    {
        [MenuItem("CityForge/Nature/Check icon toolbar and popups")]
        public static void Begin()
        {
            var v=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(!Application.isPlaying||!v||!v.ApprovedDistrict)throw new Exception("Open a district first.");v.StartCoroutine(Check(v));
        }
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static IEnumerator Frames(){yield return null;yield return null;yield return null;}
        static IEnumerator Check(PrototypeRegionView view)
        {
            var toolbar=view.GetComponent<NatureToolbar>();var root=view.transform.Find("RegionHud/NatureToolbar");var popup=toolbar.Popup;var land=view.ApprovedDistrict;
            var weather=land.GetComponent<DistrictWeather>();var oldSeason=view.Season;var oldWeather=weather.Current;
            int count=land.CanonicalTreeInstances.Length;string signature=land.MixedForest?land.MixedForest.PlacementSignature:null;
            Button Sun(string name)=>root.Find("Popup/SunOptions/SunMenu/"+name).GetComponent<Button>();
            try{
                Require(toolbar&&root&&root.gameObject.activeInHierarchy,"Toolbar missing");Require(!root.GetComponent<Image>()&&!root.GetComponent<RawImage>(),"Background toolbar bar added");
                foreach(string category in new[]{"Terrain","Water","Trees","Animals","Sun"}){
                    var button=root.Find(category).GetComponent<Button>();Require(button.GetComponentsInChildren<Text>(true).Length==0,"Main toolbar has labels");button.onClick.Invoke();
                    Require(toolbar.Selected.ToString()==category&&popup.gameObject.activeInHierarchy,"Category popup missing");
                    int options=category=="Water"||category=="Sun"?2:3;
                    for(int i=0;i<options;i++){
                        popup.transform.Find("Option"+i).GetComponent<Button>().onClick.Invoke();yield return Frames();Require(popup.Option==i&&popup.transform.Find("Explanation").GetComponent<Text>().text.Length>10,"Option/explanation missing");
                    }
                }
                toolbar.Select(NatureTool.Terrain);popup.SelectOption(0);yield return Frames();
                var slider=popup.transform.Find("Settings/RadiusSlider").GetComponent<Slider>();float prior=slider.value;slider.value=42;Require(popup.Value("Radius",0)==42,"Brush size not updated");
                popup.SelectOption(1);yield return Frames();popup.SelectOption(0);yield return Frames();Require(popup.transform.Find("Settings/RadiusSlider").GetComponent<Slider>().value==42,"Option settings did not persist");popup.transform.Find("Settings/RadiusSlider").GetComponent<Slider>().value=prior;
                toolbar.Select(NatureTool.Sun);popup.SelectOption(0);Sun("SeasonSummer").onClick.Invoke();yield return Frames();
                popup.SelectOption(1);Require(Sun("WeatherRain").gameObject.activeInHierarchy&&Sun("WeatherRain").interactable&&!Sun("WeatherSnow").interactable,"Summer weather availability");
                Sun("WeatherRain").onClick.Invoke();Require(weather.Current==DistrictWeather.WeatherKind.Rain,"Rain failed");
                if(view.Climate==RegionClimate.Temperate){
                    popup.SelectOption(0);Sun("SeasonFall").onClick.Invoke();yield return Frames();Require(view.Season==RegionSeason.Fall,"Fall failed");
                    Sun("SeasonWinter").onClick.Invoke();yield return Frames();popup.SelectOption(1);
                    Require(!Sun("WeatherRain").interactable&&Sun("WeatherSnow").interactable,"Winter availability");Sun("WeatherSnow").onClick.Invoke();Require(weather.Current==DistrictWeather.WeatherKind.Snow,"Snow failed");
                }else Require(!Sun("SeasonFall").interactable&&!Sun("SeasonWinter").interactable,"Tropical seasons enabled");
                Sun("WeatherClear").onClick.Invoke();Require(weather.Current==DistrictWeather.WeatherKind.Clear,"Clear failed");
                popup.transform.Find("Close").GetComponent<Button>().onClick.Invoke();Require(toolbar.Selected==NatureTool.None&&!popup.gameObject.activeSelf,"Close failed");
                foreach(string legacy in new[]{"SeasonSummer","SeasonFall","SeasonWinter","TemporaryWeather"})Require(!view.transform.Find("RegionHud/"+legacy).gameObject.activeSelf,"Legacy panel visible");
                Require(land.CanonicalTreeInstances.Length==count&&(!land.MixedForest||land.MixedForest.PlacementSignature==signature),"Popup changed planting");
                File.WriteAllText("Library/NatureToolbarChecks.txt","PASS five category popups, icon-only main toolbar, option descriptions, settings persistence, close, Sun season/weather callbacks, climate availability, hidden legacy panels and unchanged planting.\nNo saved regions written.\n");
            }finally{view.SetSeason(oldSeason);weather.SetWeather(oldWeather);toolbar.Dismiss();}
        }
    }
}
#endif
