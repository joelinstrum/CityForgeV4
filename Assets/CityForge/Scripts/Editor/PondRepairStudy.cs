using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using WaveHarmonic.Crest;
namespace CityForge.Editor {
[InitializeOnLoad]public static class PondRepairStudy {
const string Work="/Users/joelinstrum/Documents/Codex/2026-10-09/pl/work/pond-repair/",Out="/Users/joelinstrum/Documents/Codex/2026-10-09/pl/outputs/";
static double due;static int step;static string mode;static Vector3 originalSun;
static bool Take(string n){var p=Work+n;if(!File.Exists(p))return false;File.Delete(p);return true;}
static PondRepairStudy(){EditorApplication.update+=Tick;}
static void Tick(){if(EditorApplication.isCompiling)return;try{
 if(Take("resume"))EditorApplication.isPaused=false;
 if(!EditorApplication.isPlaying&&Take("build"))Build();
 if(Take("audit"))Audit();
 if(EditorApplication.isPlaying&&Take("light-trials")){mode="light";step=0;originalSun=GameObject.Find("Summer sun").transform.eulerAngles;SetLight();due=EditorApplication.timeSinceStartup+3;}
 if(EditorApplication.isPlaying&&Take("motion")){mode="motion";step=0;due=EditorApplication.timeSinceStartup+2;}
 if(due>0&&EditorApplication.timeSinceStartup>due){if(mode=="light"){Capture("PondRepair_Light"+step);step++;if(step<4){SetLight();due=EditorApplication.timeSinceStartup+3;}else{GameObject.Find("Summer sun").transform.eulerAngles=originalSun;due=0;}}else{Audit();Capture("PondRepair_Motion"+step);step++;due=step<2?EditorApplication.timeSinceStartup+2:0;}}
 }catch(Exception e){due=0;File.WriteAllText(Out+"PondRepair_Error.txt",e.ToString());Debug.LogException(e);}}
static void SetLight(){var angles=new[]{new Vector3(46,233,0),new Vector3(48,238,0),new Vector3(49,215,0),new Vector3(47,210,0)};GameObject.Find("Summer sun").transform.eulerAngles=angles[step];}
static Bounds Bounds(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
static void Audit(){var lines=new List<string>();lines.Add($"Playing={EditorApplication.isPlaying}, paused={EditorApplication.isPaused}, timeScale={Time.timeScale}, UnityTime={Time.time:F3}, frame={Time.frameCount}");var w=UnityEngine.Object.FindFirstObjectByType<WaterRenderer>();if(w)lines.Add($"Crest time={w.TimeProvider.Time:F3}, delta={w.TimeProvider.Delta:F4}, enabled={w.isActiveAndEnabled}, provider={w.TimeProvider.GetType().Name}");foreach(var fft in UnityEngine.Object.FindObjectsByType<ShapeFFT>(FindObjectsInactive.Include,FindObjectsSortMode.None))lines.Add("FFT "+fft.name+" enabled="+fft.isActiveAndEnabled+" "+EditorJsonUtility.ToJson(fft));foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t.parent!=null&&(t.parent.name.Contains("woodland")||t.parent.name.Contains("shoreline reflection"))&&t.GetComponentsInChildren<Renderer>().Length>0&&!t.name.StartsWith("Stone")){var b=Bounds(t.gameObject);lines.Add(t.name+" | "+t.position+" size="+b.size+" source="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));}File.WriteAllLines(Out+"PondRepair_Audit.txt",lines);File.AppendAllText(Out+"PondRepair_TimeSamples.txt",lines[0]+"\n"+lines[1]+"\n");}
static GameObject MakeFir(){
 const string root="Assets/CityForge/Art/Trees/BalsamFirStudy/";
 var ti=(TextureImporter)AssetImporter.GetAtPath(root+"BalsamAtlas.png");ti.alphaIsTransparency=true;ti.mipmapEnabled=true;ti.mipMapsPreserveCoverage=true;ti.alphaTestReferenceValue=.2f;ti.maxTextureSize=2048;ti.SaveAndReimport();
 var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"Balsam.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,root+"Balsam.mat");}
 mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(root+"BalsamAtlas.png"));mat.SetColor("_BaseColor",new Color(.85f,.95f,.85f,1));mat.SetFloat("_Smoothness",.1f);mat.SetFloat("_Cull",0);mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.2f);mat.EnableKeyword("_ALPHATEST_ON");mat.SetOverrideTag("RenderType","TransparentCutout");mat.renderQueue=2450;
 var obj=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(root+"BalsamFirA.fbx"));obj.name="Balsam fir A";foreach(var r in obj.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;}
 var prefab=PrefabUtility.SaveAsPrefabAsset(obj,root+"BalsamFirA.prefab");UnityEngine.Object.DestroyImmediate(obj);return prefab;
}
static Vector3 DryPosition(Terrain terrain,Vector3 pos){var dir=new Vector3(pos.x-133,0,pos.z-130).normalized;for(int i=0;i<20;i++){pos.y=terrain.SampleHeight(pos)+terrain.transform.position.y;if(pos.y>=7.35f)return pos;pos+=dir*.8f;}return pos;}
static void Place(GameObject go,Vector3 pos,float height){go.transform.localScale*=height/Bounds(go).size.y;go.transform.position=pos;var b=Bounds(go);go.transform.position+=Vector3.up*(pos.y-b.min.y);}
[MenuItem("CityForge/Ground Study/Balsam Fir and Live Water Lighting")]
public static void Build(){
 var fir=MakeFir();var scene=EditorSceneManager.OpenScene("Assets/CityForge/Art/Environment/NaturalTerrainLab/NaturalTerrain.unity");var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);int firs=0,full=0,removed=0;
 foreach(var rn in new[]{"MapMagic generated woodland, saplings and stones","Temporary shoreline reflection trees"}){
 var root=GameObject.Find(rn);foreach(var t in root.transform.Cast<Transform>().ToArray()){
 var go=t.gameObject;if(t.name.StartsWith("Stone")||go.GetComponentsInChildren<Renderer>().Length==0)continue;
 var b=Bounds(go);var pos=t.position;bool shore=rn.StartsWith("Temporary"),oldFir=t.name.Contains("Fraser");
 bool miniature=t.name=="Tree1"||t.name=="Tree2"||b.size.y<12;
 if(!shore&&!oldFir&&!miniature)continue;
 bool useFir=oldFir&&(shore||firs%2==0);
 var source=useFir?fir:AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Art/Environment/ObjectsWoodlandStudy/Tree"+(full%2==0?8:9)+".prefab");
 var replacement=(GameObject)PrefabUtility.InstantiatePrefab(source);replacement.transform.SetParent(root.transform);replacement.transform.rotation=t.rotation;replacement.name=useFir?"Mature Balsam fir "+firs:"Full canopy tree "+full;
 if(shore)pos=DryPosition(terrain,pos);
 Place(replacement,pos,shore?16:Mathf.Clamp(b.size.y,15,20));
 if(oldFir)firs++;if(!useFir)full++;if(miniature&&!oldFir)removed++;
 UnityEngine.Object.DestroyImmediate(go);
 }}
 var reeds=GameObject.Find("MapMagic pond shoreline reeds and stones");foreach(var t in reeds.transform.Cast<Transform>().ToArray())if(!t.name.StartsWith("Stone")){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;}
 GameObject.Find("Summer sun").transform.eulerAngles=new Vector3(46,233,0);
 AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,"Assets/CityForge/Art/Environment/NaturalTerrainLab/PondFirLighting.unity");
 File.WriteAllText(Out+"PondFirLighting_Changes.txt",$"Derived scene PondFirLighting.unity. Original NaturalTerrain and PondTreesSun preserved.\nProcessed {firs} old Fraser fir placements; retained a mix of new Balsam firs and full-canopy broadleaf trees.\nReplaced {full} trees with full-canopy variants Tree8/Tree9; removed/replaced {removed} miniature or sparse shoreline plants/trees throughout the scene.\nShoreline trees normalized to 16 m and moved to dry ground above y7.35; other replacements 15–20 m.\nSun: (46,233,0), previous (70,250,0); light intensity/color/shadow strength unchanged.\nCrest renderer, wave spectrum/input, water material, grass, terrain/bank shader and geometry unchanged.\n");Debug.Log("POND FIR LIGHTING READY");
}

static void Capture(string name){var c=GameObject.Find("Meadow presentation camera").GetComponent<Camera>();c.orthographicSize=40;c.transform.position=new Vector3(133,8,130)-c.transform.forward*110;c.ResetProjectionMatrix();var rt=new RenderTexture(1920,1080,24);rt.Create();var prev=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var t=new Texture2D(1920,1080,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1920,1080),0,0);t.Apply();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}finally{RenderTexture.active=prev;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}}
