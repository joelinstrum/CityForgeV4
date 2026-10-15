using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;using System.Text;using CityForge.CameraSystem;using UnityEngine;
namespace CityForge.World {
public sealed class MountainSurfaceAcceptance:MonoBehaviour {
public static bool Running;string reportPath;readonly StringBuilder report=new();static PrototypeRegionView View=>FindFirstObjectByType<PrototypeRegionView>();
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Launch(){if(!Application.isEditor&&Array.Exists(System.Environment.GetCommandLineArgs(),a=>a=="--mountain-surface-compare"))new GameObject("Mountain surface comparison").AddComponent<MountainSurfaceAcceptance>();}
void Start(){Running=true;Application.runInBackground=true;reportPath=MountainAcceptance.Output+"Mountain-Surface-"+(Application.isEditor?"Editor":"Player")+"-Checks.txt";StartCoroutine(Protect(Check()));}
IEnumerator Protect(IEnumerator routine){var stack=new Stack<IEnumerator>();stack.Push(routine);while(stack.Count>0){bool next=false;object value=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Line("FAILED: "+error);MountainSurfaceTreatment.Enabled=true;Running=false;if(!Application.isEditor)Application.Quit(1);yield break;}if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator child){stack.Push(child);continue;}yield return value;}Running=false;if(!Application.isEditor)Application.Quit(0);else Destroy(gameObject);}
void Line(string text){report.AppendLine(text);File.WriteAllText(reportPath,report.ToString());Debug.Log(text);}
static void Require(bool value,string text){if(!value)throw new Exception(text);}
static IEnumerator Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
static ulong Hash(float[,] values){ulong h=14695981039346656037UL;foreach(float v in values){h^=BitConverter.ToUInt32(BitConverter.GetBytes(v),0);h*=1099511628211UL;}return h;}
static IEnumerator Open(bool enabled){if(View)View.ReturnToMenu();yield return null;yield return null;yield return Resources.UnloadUnusedAssets();MountainSurfaceTreatment.Enabled=enabled;var saved=SavedRegionStore.Load(MountainAcceptance.Output+"Mountain-Before-Surface-Pass.cfregion");PrototypeRegionView.Open(saved.info.name,FindFirstObjectByType<Canvas>(),snapshot:saved);View.ApprovedDistrict.SetTreesEnabled(true);View.ApprovedDistrict.SetSmallPlantsEnabled(true);View.ApprovedDistrict.SetFramePacing60(false);yield return Wait(1);}
IEnumerator Capture(string label){yield return new WaitForEndOfFrame();var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();File.WriteAllBytes(MountainAcceptance.Output+label+".png",image.EncodeToPNG());Destroy(image);}
IEnumerator Check(){
Line($"Mountain surface comparison: {(Application.isEditor?"Editor":"macOS player")}; {Application.unityVersion}; {SystemInfo.graphicsDeviceName}; {Screen.width}x{Screen.height}.");ulong heightHash=0,rocks=0;Vector2[] road=null;int trees=0;
foreach(bool enabled in new[]{false,true}){yield return Open(enabled);var land=View.ApprovedDistrict;
if(!enabled){heightHash=Hash(View.Data.Heights);rocks=land.GetComponent<MountainRockRenderer>().Fingerprint;road=land.Road.ToArray();trees=land.FullTreeCount;}else{Require(Hash(View.Data.Heights)==heightHash&&land.GetComponent<MountainRockRenderer>().Fingerprint==rocks&&land.Road.SequenceEqual(road)&&land.FullTreeCount==trees,"Surface change altered shapes, rocks, road or trees");Require(land.DisplayTerrain.materialTemplate.shader.name=="CityForge/Mountain Terrain"&&land.DisplayTerrain.materialTemplate.shader.isSupported,"Mountain shader missing/unsupported");}
var rig=View.GetComponentInChildren<CityForgeCameraController>();rig.enabled=false;
foreach(bool close in new[]{true,false}){var pose=new RegionCameraState{focus=close?new Vector3(750,190,770):new Vector3(500,75,510),pitch=35.26439f,yaw=315,distance=700,size=close?180:640,zoom=close?3:5};rig.RestoreRegionState(pose);yield return Wait(2);var samples=new List<float>();float start=Time.realtimeSinceStartup;
while(Time.realtimeSinceStartup-start<4){float t=Time.realtimeSinceStartup-start;pose.yaw=315+t*10;rig.RestoreRegionState(pose);yield return null;samples.Add(Time.unscaledDeltaTime*1000);}
samples.Sort();Line($"{(enabled?"Varied projection":"Original projection")}; {(close?"close cliff":"wide district")}: mean={samples.Average():F2}ms; p95={samples[(int)(samples.Count*.95)]:F2}ms; max={samples.Last():F2}ms.");pose.yaw=315;rig.RestoreRegionState(pose);yield return Wait(.2f);yield return Capture("Mountain-Surface-"+(Application.isEditor?"Editor":"Player")+"-"+(enabled?"After":"Before")+"-"+(close?"Close":"Wide"));}
}
Line($"PASS terrain {heightHash:X16}, rocks {rocks:X16}, {trees} trees and road unchanged across surface treatments.");
var snapshot=SavedRegionStore.Capture(View);string path=MountainAcceptance.Output+"Mountain-Surface-"+(Application.isEditor?"Editor":"Player")+"-Roundtrip.cfregion";SavedRegionStore.Write(snapshot,path);var reload=SavedRegionStore.Load(path);Require(Hash(reload.data.Heights)==heightHash&&reload.meadow.version==105,"Surface save lost authoritative heights/cache");
var finalRig=View.GetComponentInChildren<CityForgeCameraController>();finalRig.RestoreRegionState(new RegionCameraState{focus=new Vector3(500,75,510),pitch=35.26439f,yaw=315,distance=700,size=640,zoom=5});finalRig.enabled=true;View.ApprovedDistrict.SetFramePacing60(true);if(Application.isEditor){snapshot=SavedRegionStore.Capture(View);SavedRegionStore.Save(snapshot);SavedRegionStore.Write(snapshot,MountainAcceptance.Output+"Hills-And-Mountain-Test.cfregion");}
#if UNITY_EDITOR
Require(!UnityEditor.ShaderUtil.ShaderHasError(Shader.Find("Hidden/CityForge/Mountain Add Pass")),"Mountain add pass shader failed compilation");
#endif
Line("PASS save roundtrip and supported shaders. COMPLETE.");
}
}}
