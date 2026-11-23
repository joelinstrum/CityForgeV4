#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
namespace CityForge.World {
public static class WarmNightLightingChecks {
 static void Need(bool b,string message){if(!b)throw new Exception(message);}
 public static void Run(){
 var root=new GameObject("Disposable warm night lighting");
 try {
 var view=root.AddComponent<PrototypeRegionView>();
 var land=root.AddComponent<MeadowDistrictLandscape>();
 typeof(MeadowDistrictLandscape).GetField("<View>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(land,view);
 var light=root.AddComponent<Light>();light.type=LightType.Directional;
 // At 23:00 the previous interpolation produced .85 and (1,.84,.66).
 var suns=(System.Collections.IList)typeof(MeadowDistrictLandscape).GetField("districtSuns",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(land);
 suns.Add((light,Quaternion.identity,1f,Color.white,false,0f,0f));
 var expected=new Color(1f,.84f,.66f);
 foreach(float hour in new[]{22f,23f,23.99f,0f,.01f,2f,4.99f}){
 view.SetClockHour(hour);
 Need(view.TimeOfDay==RegionTimeOfDay.Night,"Wrong night phase at "+hour);
 Need(PrototypeRegionView.SunriseProgress(hour)==0,"False sunrise at "+hour);
 PrototypeRegionView.ApplySun(light,view.TimeOfDay,Quaternion.identity,1,Color.white);
 Need(Mathf.Abs(light.intensity-.85f)<.00001f && light.color==expected,"Warm night changed at "+hour);
 land.InterpolateDistrictSun(RegionTimeOfDay.Night,RegionTimeOfDay.Morning,(hour<6?hour+24-22:hour-22)/8f);
 Need(Mathf.Abs(light.intensity-.85f)<.00001f && light.color==expected,"Live interpolation lost warmth at "+hour);
 var ambient=PrototypeRegionView.AmbientPreset(view.TimeOfDay);
 Need(ambient.level==1f && ambient.tint==Color.white,"23h ambient fill not preserved at "+hour);
 Need(PrototypeRegionView.SkyNightAmount(hour)>.99f,"Night sky lost at "+hour);
 }
 Need(PrototypeRegionView.SunriseProgress(5)==0 && PrototypeRegionView.SunriseProgress(6)>0 && PrototypeRegionView.SunriseProgress(8)==1,"Dawn progression broken");
 PrototypeRegionView.ApplySun(light,RegionTimeOfDay.Noon,Quaternion.identity,1,Color.white);
 Need(Mathf.Abs(light.intensity-.76f)<.00001f,"Daytime lighting changed");
 Debug.Log("WARM NIGHT LIGHTING CHECKS PASS: 22h/23h/midnight/2h/5h preserve approved 23h warm directional and ambient values; dark sky and dawn/daytime retained.");
 }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}}
#endif
