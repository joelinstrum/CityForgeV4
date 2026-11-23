#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CityForge.Lots;
namespace CityForge.World {
public static class DistrictSaveStayChecks {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static GameObject Child(Transform parent,string name,params Type[] types){var go=new GameObject(name,types);go.transform.SetParent(parent,false);return go;}
 public static IEnumerator Check(){
 var root=new GameObject("Disposable in-place district save");var view=root.AddComponent<PrototypeRegionView>();view.enabled=false;var events=new GameObject("Disposable EventSystem",typeof(EventSystem));string temp=Path.Combine(Application.temporaryCachePath,"district-stay-"+Guid.NewGuid().ToString("N")+".cfregion");
 try{
 EventSystem.current=events.GetComponent<EventSystem>();
 var heights=new float[513,513];var water=new float[513,513];for(int z=0;z<513;z++)for(int x=0;x<513;x++){heights[z,x]=.5f;water[z,x]=1;}
 Set(view,"<Data>k__BackingField",new PrototypeRegionData(730421,heights,water){WaterBodies=Array.Empty<RegionWaterBody>()});Set(view,"<SelectedDistrict>k__BackingField",3);Set(view,"<Waterless>k__BackingField",true);Set(view,"<RegionName>k__BackingField","Disposable district save");
 var camera=Child(root.transform,"Camera",typeof(Camera),typeof(CameraSystem.CityForgeCameraController)).GetComponent<CameraSystem.CityForgeCameraController>();
 camera.enabled=false;
 var hud=Child(root.transform,"RegionHud",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
 Button MakeButton(string name,string iconName,float right,string art){var go=Child(hud.transform,name,typeof(RectTransform),typeof(Image),typeof(Button));var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;var caption=Child(go.transform,"Caption",typeof(RectTransform),typeof(Text)).GetComponent<Text>();caption.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");caption.fontSize=19;caption.color=Color.white;var icon=Child(go.transform,iconName,typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();icon.raycastTarget=false;typeof(PrototypeRegionView).GetMethod("LayoutHudAction",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{icon,true,right,name,art});return go.GetComponent<Button>();}
 var save=MakeButton("SaveRegion","Save disk",-151,"save");var back=MakeButton("ReturnToMenu","Return arrow",-89,"back");var progress=Child(save.transform,"SaveProgress",typeof(RectTransform));Child(progress.transform,"Fill",typeof(RectTransform));progress.SetActive(false);
 var path=new DistrictParkPathRecord{district=3,path=new LotPathDefinition{points=new[]{new Vector2(10,10),new Vector2(20,10)},width=2,materialId=LotPathMaterial.ResidentialFootpath,use=LotPathUse.Pedestrian}};
 typeof(PrototypeRegionView).GetMethod("RestoreParkPaths",F).Invoke(view,new object[]{new[]{path}});
 yield return null;yield return null;Canvas.ForceUpdateCanvases();
 foreach(var graphic in hud.GetComponentsInChildren<Graphic>()){graphic.Rebuild(CanvasUpdate.PreRender);graphic.canvasRenderer.cull=false;}
 Debug.Log($"SAVE HUD fixture: screen {Screen.width}x{Screen.height}; canvas {hud.GetComponent<Canvas>().pixelRect}; graphics {hud.GetComponentsInChildren<Graphic>().Length}");
 foreach(var target in new[]{save.transform,save.transform.Find("Caption"),back.transform}){
 var rect=(RectTransform)target;var world=rect.TransformPoint(rect.rect.center);var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,world)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Need(hits.Count>0,"HUD target not raycastable: "+target.name+" at "+pointer.position+" rect "+rect.rect);var expected=target==back.transform?back:save;Need(hits[0].gameObject.GetComponentInParent<Button>()==expected,"HUD save/back hit targets overlap: "+target.name);
 }
 Need(save.navigation.mode==Navigation.Mode.None&&back.navigation.mode==Navigation.Mode.None,"HUD actions retain automatic navigation");
 EventSystem.current.SetSelectedGameObject(back.gameObject);var before=JsonUtility.ToJson(camera.CaptureRegionState());SavedRegionSnapshot captured=null;var finish=new TaskCompletionSource<string>();var routine=view.SaveWithFeedback(snapshot=>{captured=snapshot;return finish.Task;});
 Need(routine.MoveNext()&&view.IsSaving,"Save did not start");Need(EventSystem.current.currentSelectedGameObject==null,"Save retained back-button keyboard focus");view.ShowOverview();Need(view.SelectedDistrict==3,"Navigation escaped district during save");routine.MoveNext();routine.MoveNext();Need(captured!=null,"Save did not capture district");
 SavedRegionStore.Write(captured,temp);finish.SetResult(temp);while(routine.MoveNext()){}
 Need(!view.IsSaving&&view.SelectedDistrict==3,"Save completion exited district");Need(JsonUtility.ToJson(camera.CaptureRegionState())==before,"Save moved district camera");Need(save.interactable&&back.interactable&&!progress.activeSelf,"Save did not restore HUD controls");view.ShowOverview();Need(view.SelectedDistrict==3,"Save release reached Back navigation");var loaded=SavedRegionStore.Load(temp);Need(loaded.info.district==3&&loaded.info.parkPaths.Length==1,"Native save lost district/path data");
 Debug.Log("DISTRICT SAVE STAY CHECKS PASS: disk and SAVE DISTRICT label raycast only to Save; Back target separate; actual capture/write/read preserves district, paths and camera; selected HUD action cleared; no navigation during save or release; controls restored. Only temporary save written.");
 }finally{if(File.Exists(temp))File.Delete(temp);if(File.Exists(temp+".bak"))File.Delete(temp+".bak");UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(events);}
 }
}}
#endif
