using System;using System.IO;using System.Linq;using System.Reflection;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using CityForge.CameraSystem;
namespace CityForge.Editor {
[InitializeOnLoad] public static class HillsValleyStudy {
const string Root="Assets/CityForge/Art/Environment/HillsValleyStudy/";
const string Trigger="/Users/joelinstrum/Documents/Codex/2026-10-08/tre/work/hills-valley";
static Camera camera;static double captureAt;
static HillsValleyStudy(){EditorApplication.update+=Tick;}
static void Tick(){if(EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(File.Exists(Trigger)){File.Delete(Trigger);Run();}if(captureAt>0&&EditorApplication.timeSinceStartup>captureAt){captureAt=0;Capture("HillsValley_Zoom1");var rig=camera.GetComponent<CityForgeCameraController>();rig.SetZoomLevel(2);rig.ApplyPose();Capture("HillsValley_Overview");rig.SetZoomLevel(1);rig.ApplyPose();}}
static float Hill(float x,float z,float cx,float cz,float rx,float rz,float amplitude){float u=(x-cx)/rx,v=(z-cz)/rz;return amplitude*Mathf.Exp(-(u*u+v*v)*1.8f);}
public static float Height(float x,float z){
float valleyX=120+12*Mathf.Sin(z*.022f);float valley=Mathf.SmoothStep(0,1,Mathf.InverseLerp(31,72,Mathf.Abs(x-valleyX)));
float shapes=Hill(x,z,42,100,67,86,19)+Hill(x,z,197,137,65,83,25)+Hill(x,z,104,222,110,58,34)+Hill(x,z,227,218,72,70,42);
float ridges=(Mathf.PerlinNoise(x*.026f+10,z*.026f+37)-.5f)*4;
float back=Mathf.SmoothStep(0,1,Mathf.InverseLerp(155,207,z));
float lowland=3+.007f*(z-80)+.22f*Mathf.PerlinNoise(x*.024f,z*.024f);
return lowland+shapes*Mathf.Max(valley,back)+ridges*Mathf.Max(valley,back)*.6f;
}
[MenuItem("CityForge/Ground Study/Hills and Valley Zoom 1")]
public static void Run(){var scene=EditorSceneManager.OpenScene("Assets/CityForge/Art/Environment/GroundCompositionStudy/GroundCompositionZoom1.unity",OpenSceneMode.Single);var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);var oldData=terrain.terrainData;
var ground=scene.GetRootGameObjects().First(g=>g.name.StartsWith("Ground with meadow"));var sourceMesh=ground.GetComponent<MeshFilter>().sharedMesh;var mesh=UnityEngine.Object.Instantiate(sourceMesh);mesh.name="Wooded hills and open valley";var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++){var p=vertices[i];p.y=Height(p.x,p.z);vertices[i]=p;}mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=Save(mesh,Root+"HillsValleyGround.asset");ground.GetComponent<MeshFilter>().sharedMesh=mesh;
var grass=scene.GetRootGameObjects().First(g=>g.name.StartsWith("Short meadow grass"));var grassMesh=UnityEngine.Object.Instantiate(grass.GetComponent<MeshFilter>().sharedMesh);var gv=grassMesh.vertices;for(int i=0;i<gv.Length;i++){var p=gv[i];p.y+=Height(p.x,p.z)-terrain.SampleHeight(p);gv[i]=p;}grassMesh.vertices=gv;grassMesh.RecalculateBounds();grassMesh=Save(grassMesh,Root+"HillsValleyGrass.asset");grass.GetComponent<MeshFilter>().sharedMesh=grassMesh;
var trees=scene.GetRootGameObjects().First(g=>g.name.StartsWith("Forest pack woodland"));foreach(Transform tree in trees.transform){var p=tree.position;p.y=Height(p.x,p.z);tree.position=p;}
var td=UnityEngine.Object.Instantiate(oldData);td.name="Hills and valley editable heights";td.size=new Vector3(256,80,256);int n=td.heightmapResolution;var heights=new float[n,n];for(int z=0;z<n;z++)for(int x=0;x<n;x++)heights[z,x]=Height(x*256f/(n-1),z*256f/(n-1))/80;td.SetHeights(0,0,heights);td=Save(td,Root+"HillsValleyTerrain.asset");terrain.terrainData=td;terrain.enabled=false;var collider=terrain.GetComponent<TerrainCollider>();if(collider)collider.terrainData=td;
camera=UnityEngine.Object.FindFirstObjectByType<Camera>();var rig=camera.GetComponent<CityForgeCameraController>();rig.FrameArea(2048,120,new Vector3(128,0,128));typeof(CityForgeCameraController).GetField("focus",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rig,new Vector3(128,18,128));rig.SetZoomLevel(1);rig.ApplyPose();camera.farClipPlane=5000;
AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,Root+"HillsValleyZoom1.unity");captureAt=EditorApplication.timeSinceStartup+4;float maxSlope=0;for(int z=70;z<=145;z++)for(int x=102;x<=139;x++){float dx=(Height(x+1,z)-Height(x-1,z))*.5f,dz=(Height(x,z+1)-Height(x,z-1))*.5f;maxSlope=Mathf.Max(maxSlope,Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg);}Debug.Log("HILLS VALLEY READY: inner building plain max slope="+maxSlope.ToString("F2")+" degrees");}
static T Save<T>(T item,string path) where T:UnityEngine.Object {var old=AssetDatabase.LoadMainAssetAtPath(path);if(old){EditorUtility.CopySerialized(item,old);UnityEngine.Object.DestroyImmediate(item);return (T)old;}AssetDatabase.CreateAsset(item,path);return item;}
static void Capture(string name){var rt=new RenderTexture(3840,2160,24);rt.Create();var previous=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(3840,2160,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,3840,2160),0,0);image.Apply();File.WriteAllBytes("/Users/joelinstrum/Documents/Codex/2026-10-08/tre/outputs/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);Debug.Log("HILLS VALLEY CAPTURE "+name);}finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}}
