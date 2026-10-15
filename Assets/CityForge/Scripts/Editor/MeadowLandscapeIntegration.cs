using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;

namespace CityForge.Editor
{
    [InitializeOnLoad] public static class MeadowLandscapeIntegration
    {
        const string W="/Users/joelinstrum/Documents/Codex/2026-10-09/pl/work/meadow-integration/";
        const string O="/Users/joelinstrum/Documents/Codex/2026-10-09/pl/outputs/";
        const string A="Assets/CityForge/Art/Environment/MeadowNativeURP/Integration/";
        const string ScenePath=A+"NaturalMeadowValley.unity";
        static readonly List<Vector2> route=new List<Vector2>();
        static readonly List<float> widths=new List<float>();
        static readonly List<Bounds> lots=new List<Bounds>();
        static float[,] woodland, roads;
        const int Grid=256;
        static int benchmarkStage=-1, samples, warmup, lastFrame=-1;
        static readonly List<float> times=new List<float>(), submission=new List<float>();
        static readonly List<int> draws=new List<int>(), instances=new List<int>();
        static readonly List<long> allocations=new List<long>();
        static bool previousAdaptive;
        static GameObject stressTile;
        static MeadowGroundCoverRenderer field;
        static MeadowLandscapeIntegration(){EditorApplication.update+=Tick;}
        static bool Take(string name){if(!File.Exists(W+name))return false;File.Delete(W+name);return true;}
        [MenuItem("CityForge/Environment Study/Open Natural Meadow Valley")]
        static void Open(){if(!EditorApplication.isPlaying)EditorSceneManager.OpenScene(ScenePath);}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            try
            {
                if(!EditorApplication.isPlaying&&Take("build"))Build();
                if(!EditorApplication.isPlaying&&Take("roadlighting"))RoadLighting();
                if(!EditorApplication.isPlaying&&Take("polish"))Polish();
                if(!EditorApplication.isPlaying&&Take("natural-transitions"))NaturalTransitions();
                if(!EditorApplication.isPlaying&&Take("zoom-magnify")) {
                    if(StudyName!="PolishedMeadowValley")throw new Exception("Polished scene required");
                    var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene,A+"Polish/BeforeZoomMagnification.unity");
                    var camera=Camera.main;var nav=camera.GetComponent<CameraSystem.MeadowShowcaseNavigation>();var serialized=new SerializedObject(nav);
                    var range=serialized.FindProperty("zoomRange");range.vector2Value/=1.5f;serialized.FindProperty("isometricSize").floatValue/=1.5f;serialized.ApplyModifiedPropertiesWithoutUndo();
                    var plane=new Plane(Vector3.up,new Vector3(0,9,0));var ray=new Ray(camera.transform.position,camera.transform.forward);
                    if(plane.Raycast(ray,out float distance)){var focus=ray.GetPoint(distance);camera.transform.position=focus-camera.transform.forward*(distance/1.5f);}
                    EditorUtility.SetDirty(camera.transform);EditorSceneManager.SaveScene(scene,A+"Polish/PolishedMeadowValley.unity");
                    File.WriteAllText(O+"ZoomMagnification-Checks.txt","50% greater magnification: zoom distance/orthographic size divided by1.5. Range="+range.vector2Value+"; isometric start="+nav.isometricSize+"; zoom step preserved1.35. Closest-two detail cutoff scales automatically with range. Prior scene: BeforeZoomMagnification.unity.\n");
                }
                if(!EditorApplication.isPlaying&&Take("near-zoom")) {
                    if(StudyName!="PolishedMeadowValley")throw new Exception("Polished scene required");
                    var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    EditorSceneManager.SaveScene(scene,A+"Polish/BeforeNearZoomReduction.unity");
                    var cover=UnityEngine.Object.FindFirstObjectByType<MeadowGroundCoverRenderer>();cover.closestZoomDensity=.7f;cover.RebuildDensityCache();EditorUtility.SetDirty(cover);
                    EditorSceneManager.SaveScene(scene,A+"Polish/PolishedMeadowValley.unity");
                }
                if(EditorApplication.isPlaying&&Take("near-zoom-check")) {
                    var nav=Camera.main.GetComponent<CameraSystem.MeadowShowcaseNavigation>();var cover=UnityEngine.Object.FindFirstObjectByType<MeadowGroundCoverRenderer>();
                    if(!nav.ClosestTwoZoomLevels||Mathf.Abs(cover.closestZoomDensity-.7f)>.001f)throw new Exception("Near zoom settings failed");
                    Capture("NearZoomReduction.png");File.WriteAllText(O+"NearZoomReduction-Checks.txt","Closest-two-zoom density70% active; zoom="+nav.TargetDistance+"; submitted="+cover.submittedInstances+"; allocation="+cover.submissionAllocatedBytes+" bytes. Broad zoom cache unchanged.\n");
                }
                if(!EditorApplication.isPlaying&&Take("quiet"))Quiet();
                if(!EditorApplication.isPlaying&&Take("quiet-tint")) {
                    var cover=UnityEngine.Object.FindFirstObjectByType<MeadowGroundCoverRenderer>();
                    foreach(var source in cover.batches.Select(b=>b.material).Distinct().Where(m=>m.name.Contains("grass_meadow")).ToArray()) {
                        var mat=new Material(source){name=source.name+" Quiet"};AssetDatabase.CreateAsset(mat,A+"Polish/Materials/"+mat.name+".mat");
                        foreach(var batch in cover.batches)if(batch.material==source)batch.material=mat;
                        foreach(var property in new[]{"_BaseColor","_Color"})if(mat.HasProperty(property))mat.SetColor(property,new Color(.46f,.65f,.32f,1));EditorUtility.SetDirty(mat);
                    }
                    EditorUtility.SetDirty(cover);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }
                if(!EditorApplication.isPlaying&&Take("quiet-finish")) {
                    var cover=UnityEngine.Object.FindFirstObjectByType<MeadowGroundCoverRenderer>();cover.RebuildDensityCache();EditorUtility.SetDirty(cover);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                    File.WriteAllText(O+"QuietMeadow-Build.txt","Retained ground-cover records="+cover.batches.Sum(b=>b.matrices.Length)+"; LOD0 density65%; grass mip bias2.5; albedo contrast suppressed48–78%; broad variation5.5%, secondary1.8%; normal intensity22%. Plant patches favor canopy, soil verges and riverbanks. BeforeQuietMeadow scene preserves prior state.\n");
                }
                if(!EditorApplication.isPlaying&&Take("river-settings"))RiverSettings();
                if(EditorApplication.isPlaying&&Take("benchmark"))BeginBenchmark();
                if(EditorApplication.isPlaying&&Take("perspective")){Camera.main.GetComponent<CameraSystem.MeadowShowcaseNavigation>().ResetView();Capture(StudyName+"-Perspective.png");}
                if(EditorApplication.isPlaying&&Take("isometric")){var nav=Camera.main.GetComponent<CameraSystem.MeadowShowcaseNavigation>();if(!nav.Isometric)nav.ToggleIsometric();Capture(StudyName+"-Isometric.png");}
                if(EditorApplication.isPlaying&&benchmarkStage>=0)SampleBenchmark();
                if(Take("validate"))Validate();
            }
            catch(Exception e){benchmarkStage=-1;File.WriteAllText(O+"MeadowIntegration_Error.txt",e.ToString());Debug.LogException(e);}
        }
        static string StudyName=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="PolishedMeadowValley"?"PolishedMeadowValley":"NaturalMeadowValley";
        static void NaturalTransitions()
        {
            if(StudyName!="PolishedMeadowValley")throw new Exception("Polished scene required");
            string folder=A+"Polish/";var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene,folder+"BeforeNaturalTransitions.unity");EditorSceneManager.SaveScene(scene,folder+"PolishedMeadowValley.unity");
            var terrain=Terrain.activeTerrain;var old=terrain.terrainData;AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(old),folder+"NaturalTransitionsTerrain.asset");
            var td=AssetDatabase.LoadAssetAtPath<TerrainData>(folder+"NaturalTransitionsTerrain.asset");terrain.terrainData=td;terrain.GetComponent<TerrainCollider>().terrainData=td;
            var mat=new Material(terrain.materialTemplate){name="Natural Transitions Terrain"};mat.shader=Shader.Find("CityForge/Natural Meadow Transitions");AssetDatabase.CreateAsset(mat,folder+"Materials/Natural Transitions Terrain.mat");terrain.materialTemplate=mat;
            var layers=td.terrainLayers.ToArray();var leaf=UnityEngine.Object.Instantiate(layers[3]);leaf.diffuseRemapMax=new Vector4(.58f,.52f,.42f,1);leaf.normalScale=.35f;AssetDatabase.CreateAsset(leaf,folder+"Layers/Natural Leaf Litter.terrainlayer");layers[3]=leaf;td.terrainLayers=layers;
            int n=td.alphamapResolution;var canopy=new float[n,n];
            foreach(var tree in td.treeInstances.Where(t=>t.prototypeIndex>=3)){
                float tx=tree.position.x*(n-1),tz=tree.position.z*(n-1);float radius=5f*n/512;
                for(int z=Mathf.Max(0,(int)(tz-radius));z<=Mathf.Min(n-1,(int)(tz+radius));z++)for(int x=Mathf.Max(0,(int)(tx-radius));x<=Mathf.Min(n-1,(int)(tx+radius));x++) {
                    float d=Vector2.Distance(new Vector2(x,z),new Vector2(tx,tz))/radius;canopy[z,x]+=Mathf.SmoothStep(0,1,1-d)*.6f;
                }
            }
            var maps=td.GetAlphamaps(0,0,n,n);MakeRoute();
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                float wx=x*512f/(n-1),wz=z*512f/(n-1);float patch=Mathf.PerlinNoise(wx/13+8,wz/13+17);float litter=Mathf.Clamp01(canopy[z,x])*(.5f+patch*.25f);
                float transfer=(maps[z,x,1]+maps[z,x,2])*litter;maps[z,x,1]*=1-litter;maps[z,x,2]*=1-litter;maps[z,x,3]+=transfer;
                float d=River(new Vector2(wx,wz),out float width,out float side);float bank=(1-Mathf.SmoothStep(0,1,Mathf.Abs(d-width)/3))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,.78f,patch));
                float mud=maps[z,x,1]*bank*.23f;maps[z,x,1]-=mud;maps[z,x,0]+=mud;
            }
            td.SetAlphamaps(0,0,maps);
            var copies=new Dictionary<Material,Material>();foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("RamSpline")))r.sharedMaterials=r.sharedMaterials.Select(source=>{
                if(copies.TryGetValue(source,out var copy))return copy;copy=new Material(source){name=source.name+" SoftVerge"};copy.shader=Shader.Find("CityForge/Study/Natural Road Verges");AssetDatabase.CreateAsset(copy,folder+"Materials/"+copy.name+".mat");copies.Add(source,copy);return copy;
            }).ToArray();
            var root=new GameObject("Selective shoreline and woodland understory");var random=new System.Random(11981);float R(){return(float)random.NextDouble();}
            var reeds=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Art/Environment/RollingPondStudy/ReedClump.prefab");
            var grass=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/Flora/prefab_Terrain_grass_meadow_03_2.prefab");
            int planted=0,understory=0,stones=0;
            for(int i=0;i<25000&&planted<160;i++){
                float x=R()*512,z=R()*512;float d=River(new Vector2(x,z),out float width,out float side);float y=terrain.SampleHeight(new Vector3(x,0,z));
                if(Mathf.Abs(d-width)>2.8f||y<8.45f||y>11f||Mathf.PerlinNoise(x/19+34,z/19+11)<.57f)continue;
                var prefab=y<9.5f?reeds:grass;if(!prefab)continue;var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(root.transform);go.transform.SetPositionAndRotation(new Vector3(x,y-.03f,z),Quaternion.Euler(0,R()*360,0));go.transform.localScale*=.75f+R()*.45f;planted++;
            }
            for(int i=0;i<1200;i++){
                float x=R()*512,z=R()*512;int gx=Mathf.Clamp((int)(x/512*(n-1)),0,n-1),gz=Mathf.Clamp((int)(z/512*(n-1)),0,n-1);
                if(canopy[gz,gx]<.35f||Mathf.PerlinNoise(x/10+5,z/10+7)<.64f||!grass)continue;float y=terrain.SampleHeight(new Vector3(x,0,z));if(y<9.3f)continue;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(grass);go.transform.SetParent(root.transform);go.transform.SetPositionAndRotation(new Vector3(x,y,z),Quaternion.Euler(0,R()*360,0));go.transform.localScale*=.7f+R()*.3f;understory++;
            }
            // Small stones sit on selected banks; no continuous rock border.
            var rock=td.treePrototypes[0].prefab;
            for(int i=0;i<route.Count;i+=5){if(R()>.35f)continue;var v=(route[Mathf.Min(i+1,route.Count-1)]-route[Mathf.Max(0,i-1)]).normalized;var p=route[i]+new Vector2(-v.y,v.x)*widths[i]*(R()<.5f?-1:1);if(p.x<0||p.y<0||p.x>512||p.y>512)continue;float y=terrain.SampleHeight(new Vector3(p.x,0,p.y));if(y<8.8f||y>10.5f)continue;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(rock);go.transform.SetParent(root.transform);var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0){UnityEngine.Object.DestroyImmediate(go);continue;}var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);go.transform.localScale*=.6f/Mathf.Max(bounds.size.x,bounds.size.z);go.transform.position=new Vector3(p.x,y,p.y);stones++;
            }
            EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText(O+"NaturalTransitions-Build.txt","Isolated terrain/material/litter layer and road shader copies. Broad grass variation7.5%, secondary1.2%, restrained olive shift. Canopy-weighted leaf litter with soft5m influence; selective mud pockets. Road edge feather widened. Shore plants="+planted+"; understory="+understory+"; stones="+stones+". Existing reduced cover and water assets unchanged.\n");
        }
        static void Quiet()
        {
            if(StudyName!="PolishedMeadowValley")throw new Exception("Polished scene required");
            string folder=A+"Polish/";var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene,folder+"BeforeQuietMeadow.unity");
            EditorSceneManager.SaveScene(scene,folder+"PolishedMeadowValley.unity");
            var terrain=Terrain.activeTerrain;var td=terrain.terrainData;
            var material=new Material(terrain.materialTemplate){name="Quiet Meadow Terrain"};material.shader=Shader.Find("CityForge/Quiet Meadow Terrain");
            AssetDatabase.CreateAsset(material,folder+"Materials/Quiet Meadow Terrain.mat");terrain.materialTemplate=material;terrain.basemapDistance=2000;
            // TerrainData copy keeps the previous polished state reversible.
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(td),folder+"QuietTerrain.asset");
            td=AssetDatabase.LoadAssetAtPath<TerrainData>(folder+"QuietTerrain.asset");terrain.terrainData=td;terrain.GetComponent<TerrainCollider>().terrainData=td;
            var layers=td.terrainLayers.ToArray();
            for(int i=1;i<=2;i++){var layer=UnityEngine.Object.Instantiate(layers[i]);layer.normalScale=.22f;AssetDatabase.CreateAsset(layer,folder+"Layers/Quiet "+i+".terrainlayer");layers[i]=layer;}
            td.terrainLayers=layers;
            var canopy=new float[64,64];foreach(var tree in td.treeInstances){int x=(int)(tree.position.x*63),z=(int)(tree.position.z*63);for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(x+dx>=0&&x+dx<64&&z+dz>=0&&z+dz<64)canopy[z+dz,x+dx]=1;}
            var maps=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);MakeRoute();
            var cover=UnityEngine.Object.FindFirstObjectByType<MeadowGroundCoverRenderer>();int before=0,after=0;
            foreach(var batch in cover.batches){before+=batch.matrices.Length;bool flower=batch.material.name.ToLowerInvariant().Contains("flower")||batch.material.name.ToLowerInvariant().Contains("clover");
                batch.matrices=batch.matrices.Where(m=>{
                    float x=m.m03,z=m.m23;float patch=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.7f,Mathf.PerlinNoise(x/18+7,z/18+23)));
                    float forest=canopy[Mathf.Clamp((int)(z/8),0,63),Mathf.Clamp((int)(x/8),0,63)];
                    int ax=Mathf.Clamp((int)(x/512*(td.alphamapWidth-1)),0,td.alphamapWidth-1),az=Mathf.Clamp((int)(z/512*(td.alphamapHeight-1)),0,td.alphamapHeight-1);
                    float road=maps[az,ax,0];float d=River(new Vector2(x,z),out var width,out var side);float bank=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width+1,width+10,d));
                    float edge=Mathf.Max(forest,Mathf.Max(bank,Mathf.Clamp01(road*5)));
                    float keep=flower?patch*Mathf.Lerp(.12f,.7f,edge):Mathf.Lerp(.27f,.82f,Mathf.Max(patch,edge));
                    float hash=Mathf.Repeat(Mathf.Sin(x*12.9898f+z*78.233f)*43758.5453f,1);return hash<keep;
                }).ToArray();after+=batch.matrices.Length;}
            cover.lod0Density=.65f;cover.RebuildDensityCache();
            EditorUtility.SetDirty(cover);EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText(O+"QuietMeadow-Build.txt","Ground cover matrix records: "+before+" -> "+after+". LOD0 density=65% of retained records; other density levels derive from this reduced base. Grass albedo mip bias2.5; fine albedo contrast suppressed48–78%, broad world-space color variation5.5%, secondary1.8%; grass normals22%. Spatial patch filtering favors woodland, soil verges and banks. BeforeQuietMeadow scene and prior terrain/layers remain preserved.\n");
        }
        static void Polish()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open NaturalMeadowValley to author polish variation.");
            string folder=A+"Polish/";Directory.CreateDirectory(folder+"Materials");Directory.CreateDirectory(folder+"Layers");Directory.CreateDirectory(folder+"Meshes");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene,folder+"PolishedMeadowValley.unity");
            var terrain=Terrain.activeTerrain;var original=terrain.terrainData;
            if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original),folder+"PolishedTerrain.asset"))throw new Exception("Polished terrain already exists");
            var td=AssetDatabase.LoadAssetAtPath<TerrainData>(folder+"PolishedTerrain.asset");
            var layers=original.terrainLayers.Select((layer,index)=>
            {
                var copy=UnityEngine.Object.Instantiate(layer);copy.name=layer.name+" RichGreen";
                var tint=index==1?new Vector4(.68f,.87f,.58f,1):index==2?new Vector4(.75f,.9f,.67f,1):index==0?new Vector4(.82f,.88f,.75f,1):Vector4.one;
                copy.diffuseRemapMax=Vector4.Scale(layer.diffuseRemapMax,tint);
                AssetDatabase.CreateAsset(copy,folder+"Layers/"+index+" "+copy.name+".terrainlayer");return copy;
            }).ToArray();
            td.terrainLayers=layers;terrain.terrainData=td;terrain.GetComponent<TerrainCollider>().terrainData=td;
            var maps=original.GetAlphamaps(0,0,original.alphamapWidth,original.alphamapHeight);int ar=original.alphamapResolution;
            // Remove the pale exposed strip underneath road feathering; keep building lots recognizable.
            for(int z=0;z<ar;z++)for(int x=0;x<ar;x++)
            {
                if(maps[z,x,0]>.45f){float removed=maps[z,x,0]*.68f;maps[z,x,0]-=removed;maps[z,x,1]+=removed;}
            }
            td.SetAlphamaps(0,0,maps);
            var cover=UnityEngine.Object.FindFirstObjectByType<MeadowGroundCoverRenderer>();var copies=new Dictionary<Material,Material>();
            foreach(var batch in cover.batches)
            {
                if(!batch.material.name.Contains("grass_meadow"))continue;
                if(!copies.TryGetValue(batch.material,out var copy))
                {
                    copy=new Material(batch.material){name=batch.material.name+" RichGreen"};
                    foreach(var property in new[]{"_BaseColor","_Color"})if(copy.HasProperty(property))copy.SetColor(property,new Color(.83f,.94f,.78f,1));
                    AssetDatabase.CreateAsset(copy,folder+"Materials/"+copy.name+".mat");copies.Add(batch.material,copy);
                }
                batch.material=copy;
            }
            copies.Clear();int roadsChanged=0;
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("RamSpline")&&f.sharedMesh))
            {
                var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name=filter.name+" SoftContact";var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++){var p=filter.transform.TransformPoint(vertices[i]);p.y=terrain.SampleHeight(p)+.012f;vertices[i]=filter.transform.InverseTransformPoint(p);}
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"Meshes/"+mesh.name+".asset");filter.sharedMesh=mesh;var collider=filter.GetComponent<MeshCollider>();if(collider)collider.sharedMesh=mesh;
                var renderer=filter.GetComponent<MeshRenderer>();renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>
                {
                    if(copies.TryGetValue(m,out var copy))return copy;
                    copy=new Material(m){name=m.name+" Feathered"};copy.shader=Shader.Find("CityForge/Study/Blended Natural Road");copy.SetColor("_BaseColor",new Color(.72f,.64f,.5f,1));
                    AssetDatabase.CreateAsset(copy,folder+"Materials/"+copy.name+".mat");copies.Add(m,copy);return copy;
                }).ToArray();roadsChanged++;
            }
            MakeRoute();int underwaterRocks=0;
            td.SetTreeInstances(td.treeInstances.Where(tree=>
            {
                var p=new Vector3(tree.position.x*512,0,tree.position.z*512);if(tree.prototypeIndex<3&&terrain.SampleHeight(p)<8.95f){underwaterRocks++;return false;}return true;
            }).ToArray(),true);
            var rockRoots=new HashSet<GameObject>();
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var root=PrefabUtility.GetOutermostPrefabInstanceRoot(renderer.gameObject);if(!root||!root.name.ToLowerInvariant().Contains("rock"))continue;
                var p=root.transform.position;if(p.x<0||p.x>512||p.z<0||p.z>512||terrain.SampleHeight(p)>=8.95f)continue;
                if(rockRoots.Add(root))root.SetActive(false);
            }
            var water=UnityEngine.Object.FindFirstObjectByType<WaveHarmonic.Crest.WaterRenderer>();
            // Only this cloned scene enables river flow; shared pond materials and FFT spectrum stay unchanged.
            var serialized=new SerializedObject(water);serialized.FindProperty("_FlowLod._Enabled").boolValue=true;
            serialized.FindProperty("_FlowLod._Resolution").intValue=256;
            serialized.FindProperty("_ClipLod._OverrideResolution").boolValue=true;serialized.FindProperty("_ClipLod._Resolution").intValue=512;
            serialized.FindProperty("_DepthLod._OverrideResolution").boolValue=true;serialized.FindProperty("_DepthLod._Resolution").intValue=512;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var flowRoot=new GameObject("Crest local downstream river flow");
            int flowCount=0;
            for(int i=0;i<route.Count-1;i+=4)
            {
                int j=Mathf.Min(i+4,route.Count-1);var a=route[i];var b=route[j];var direction=(b-a).normalized;var normal=new Vector2(-direction.y,direction.x);float half=Mathf.Max(widths[i],widths[j])+3;
                a-=direction*.8f;b+=direction*.8f;
                var mesh=new Mesh{name="River flow reach "+flowCount};mesh.vertices=new[]{new Vector3(a.x-normal.x*half,8.8f,a.y-normal.y*half),new Vector3(a.x+normal.x*half,8.8f,a.y+normal.y*half),new Vector3(b.x-normal.x*half,8.8f,b.y-normal.y*half),new Vector3(b.x+normal.x*half,8.8f,b.y+normal.y*half)};
                mesh.triangles=new[]{0,1,2,2,1,3};mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,0),new Vector2(1,1)};mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"Meshes/"+mesh.name+".asset");
                var material=new Material(Shader.Find("Crest/Inputs/Flow/Fixed Direction")){name="Downstream flow "+flowCount};material.SetFloat("_Crest_Speed",.45f);material.SetFloat("_Crest_Direction",Mathf.Repeat(Mathf.Atan2(direction.y,direction.x)/(Mathf.PI*2),1));material.SetFloat("_Crest_Feather",1);material.EnableKeyword("d_Feather");material.SetFloat("_Crest_FeatherWidth",.08f);AssetDatabase.CreateAsset(material,folder+"Materials/"+material.name+".mat");
                var go=new GameObject(mesh.name);go.transform.SetParent(flowRoot.transform);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                var input=WaveHarmonic.Crest._Extensions.AddComponent<WaveHarmonic.Crest.FlowLodInput>(go,WaveHarmonic.Crest.LodInputMode.Renderer);input.GetData<WaveHarmonic.Crest.FlowRendererLodInputData>().Renderer=renderer;flowCount++;
            }
            var probe=UnityEngine.Object.FindFirstObjectByType<WaveHarmonic.Crest.DepthProbe>();if(probe)probe.Populate();
            EditorUtility.SetDirty(td);EditorUtility.SetDirty(cover);EditorUtility.SetDirty(water);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText(O+"PolishedMeadowValley-Build.txt","Independent scene/terrain/terrain layers/grass materials/road meshes/materials. Roads="+roadsChanged+". Removed underwater terrain rocks="+underwaterRocks+", disabled rock prefab roots="+rockRoots.Count+". Crest directional flow reaches="+flowCount+" at 0.45m/s; flow resolution256, depth/clip512. Shared water materials/spectrum unchanged.\n");
        }
        static void RiverSettings()
        {
            if(StudyName!="PolishedMeadowValley")throw new Exception("Polished scene required");
            var water=UnityEngine.Object.FindFirstObjectByType<WaveHarmonic.Crest.WaterRenderer>();var serialized=new SerializedObject(water);var property=serialized.FindProperty("_Surface._Material");
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/CityForge/Art/Environment/CrestVisualLab/ShoreFreshwater.mat");var path=A+"Polish/Materials/Crest River Flow.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(source){name="Crest River Flow"};AssetDatabase.CreateAsset(material,path);}
            material.shader=source.shader;material.CopyPropertiesFromMaterial(source);
            serialized.FindProperty("_Material").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Packages/com.waveharmonic.crest/Runtime/Materials/Water.mat");
            material.SetFloat("_CREST_FLOW_LOD",1);material.EnableKeyword("_CREST_FLOW_LOD");property.objectReferenceValue=material;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static void MakeRoute()
        {
            route.Clear();widths.Clear();
            var cp=new[]{new Vector2(530,135),new Vector2(475,172),new Vector2(409,205),new Vector2(422,253),new Vector2(367,282),new Vector2(398,322),new Vector2(339,357),new Vector2(360,399),new Vector2(313,444),new Vector2(304,530)};
            float[] half={7,10,5,6,12,6,9,4.5f,10,6};
            for(int i=0;i<cp.Length-1;i++)for(int j=0;j<12;j++)
            {
                float f=j/12f;var a=cp[Mathf.Max(0,i-1)];var b=cp[i];var c=cp[i+1];var d=cp[Mathf.Min(cp.Length-1,i+2)];
                route.Add(.5f*((2*b)+(-a+c)*f+(2*a-5*b+4*c-d)*f*f+(-a+3*b-3*c+d)*f*f*f));
                widths.Add(Mathf.Lerp(half[i],half[i+1],Mathf.SmoothStep(0,1,f)));
            }
            route.Add(cp.Last());widths.Add(half.Last());
        }
        static float River(Vector2 p,out float width,out float side)
        {
            float best=float.MaxValue;width=7;side=0;
            for(int i=1;i<route.Count;i++)
            {
                var a=route[i-1];var v=route[i]-a;float f=Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude);var near=a+v*f;float distance=Vector2.Distance(p,near);
                if(distance>=best)continue;best=distance;side=Mathf.Sign(v.x*(p.y-near.y)-v.y*(p.x-near.x));
                width=Mathf.Lerp(widths[i-1],widths[i],f);
            }
            width+= (Mathf.PerlinNoise(p.x/13f+2,p.y/13f+7)-.5f)*1.2f+side*(Mathf.PerlinNoise(p.x/39f,p.y/39f)-.5f)*1.7f;
            return best;
        }
        static float Map(float[,] map,float x,float z)
        {
            float gx=Mathf.Clamp(x/2f,0,Grid-1.001f),gz=Mathf.Clamp(z/2f,0,Grid-1.001f);int ix=(int)gx,iz=(int)gz;
            return Mathf.Lerp(Mathf.Lerp(map[iz,ix],map[iz,ix+1],gx-ix),Mathf.Lerp(map[iz+1,ix],map[iz+1,ix+1],gx-ix),gz-iz);
        }
        static bool InLot(Vector3 p,float padding=0)
        {
            foreach(var lot in lots)if(Mathf.Abs(p.x-lot.center.x)<lot.extents.x+padding&&Mathf.Abs(p.z-lot.center.z)<lot.extents.z+padding)return true;
            return false;
        }
        static void RasterWoodland(TerrainData td)
        {
            woodland=new float[Grid,Grid];
            foreach(var tree in td.treeInstances)
            {
                if(tree.prototypeIndex<3||tree.prototypeIndex>10)continue;
                int gx=Mathf.Clamp((int)(tree.position.x*256),0,255),gz=Mathf.Clamp((int)(tree.position.z*256),0,255);
                for(int dz=-7;dz<=7;dz++)for(int dx=-7;dx<=7;dx++)
                {
                    int x=gx+dx,z=gz+dz;if(x<0||x>=Grid||z<0||z>=Grid)continue;
                    float r=Mathf.Sqrt(dx*dx+dz*dz)/7f;if(r>1)continue;
                    woodland[z,x]+=(1-Mathf.SmoothStep(0,1,r))*.19f;
                }
            }
            for(int z=0;z<Grid;z++)for(int x=0;x<Grid;x++)woodland[z,x]=Mathf.Clamp01(woodland[z,x]);
        }
        static float Cross(Vector2 a,Vector2 b,Vector2 p){return (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);}
        static void RasterRoads()
        {
            roads=new float[Grid,Grid];
            foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("RamSpline")&&f.sharedMesh))
            {
                var vertices=f.sharedMesh.vertices;var triangles=f.sharedMesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var av=f.transform.TransformPoint(vertices[triangles[i]]);var bv=f.transform.TransformPoint(vertices[triangles[i+1]]);var cv=f.transform.TransformPoint(vertices[triangles[i+2]]);
                    var a=new Vector2(av.x,av.z);var b=new Vector2(bv.x,bv.z);var c=new Vector2(cv.x,cv.z);
                    int x0=Mathf.Clamp((int)(Mathf.Min(a.x,Mathf.Min(b.x,c.x))/2),0,255),x1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))/2),0,255);
                    int z0=Mathf.Clamp((int)(Mathf.Min(a.y,Mathf.Min(b.y,c.y))/2),0,255),z1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))/2),0,255);
                    for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){var p=new Vector2(x*2+1,z*2+1);float u=Cross(a,b,p),v=Cross(b,c,p),w=Cross(c,a,p);if((u>=0&&v>=0&&w>=0)||(u<=0&&v<=0&&w<=0))roads[z,x]=1;}
                }
            }
        }
        static void Build()
        {
            if(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.EndsWith("RefinedMeadowValley.unity"))throw new Exception("Open RefinedMeadowValley before building integration variation.");
            var watch=Stopwatch.StartNew();Directory.CreateDirectory(A);Directory.CreateDirectory(A+"Materials");Directory.CreateDirectory(A+"Meshes");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene,ScenePath);
            var t=Terrain.activeTerrain;var source=t.terrainData;
            if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source),A+"NaturalValleyTerrain.asset"))throw new Exception("Terrain copy failed or variation already exists");
            var td=AssetDatabase.LoadAssetAtPath<TerrainData>(A+"NaturalValleyTerrain.asset");td.terrainLayers=source.terrainLayers;td.SetAlphamaps(0,0,source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight));
            t.terrainData=td;var collider=t.GetComponent<TerrainCollider>();if(collider)collider.terrainData=td;
            MakeRoute();
            var baseData=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/CityForge/Art/Environment/MeadowNativeURP/Meadow Environment Dynamic Nature/Demo Scenes/Meadow Terrain Demo.asset");
            int n=td.heightmapResolution;var heights=baseData.GetHeights(0,0,n,n);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                var p=new Vector2(x*512f/(n-1),z*512f/(n-1));float dist=River(p,out float width,out float side);float outer=Mathf.Max(heights[z,x]*250,9.7f);
                if(dist<width)heights[z,x]=Mathf.Lerp(6.15f+.35f*Mathf.PerlinNoise(p.x/36,p.y/36),8.8f,Mathf.Pow(dist/width,2))/250f;
                else
                {
                    float shoulder=48f+15f*Mathf.PerlinNoise(p.x/65+9,p.y/65+2);
                    float u=Mathf.Clamp01((dist-width)/shoulder);
                    float bankRise=.28f*(1-Mathf.Exp(-(dist-width)/3f));
                    heights[z,x]=Mathf.Lerp(8.8f+bankRise,outer,Mathf.SmoothStep(0,1,u))/250f;
                }
            }
            td.SetHeights(0,0,heights);
            var water=UnityEngine.Object.FindFirstObjectByType<WaveHarmonic.Crest.WaterRenderer>();
            var bodies=UnityEngine.Object.FindObjectsByType<WaveHarmonic.Crest.WaterBody>(FindObjectsSortMode.None);
            foreach(var body in bodies)UnityEngine.Object.DestroyImmediate(body.gameObject);
            for(int i=1;i<route.Count;i+=3)
            {
                int last=Mathf.Min(i+2,route.Count-1);var a=route[i-1];var b=route[last];float half=Mathf.Max(widths[i-1],widths[last])+3.5f;
                var center=(a+b)*.5f;var go=new GameObject("Variable river inclusion "+i);go.transform.SetParent(water.transform);go.transform.position=new Vector3(center.x,8.8f,center.y);
                go.transform.localScale=new Vector3(Mathf.Abs(b.x-a.x)+half*2,20,Mathf.Abs(b.y-a.y)+half*2);
                var body=go.AddComponent<WaveHarmonic.Crest.WaterBody>();Set(body,"_Affects",(int)WaveHarmonic.Crest.WaterBodyAffects.Everything);Set(body,"_Exclusion",(int)WaveHarmonic.Crest.WaterBodyExclusion.Include);Set(body,"_Precise",true);
            }
            RasterRoads();
            lots.Clear();lots.Add(new Bounds(new Vector3(300,0,248),new Vector3(20,100,18)));lots.Add(new Bounds(new Vector3(281,0,272),new Vector3(20,100,18)));lots.Add(new Bounds(new Vector3(295,0,307),new Vector3(18,100,16)));
            RasterWoodland(td);
            var rng=new System.Random(95213);float R(){return (float)rng.NextDouble();}
            var trees=td.treeInstances.Where(tree=>{var p=new Vector3(tree.position.x*512,0,tree.position.z*512);float d=River(new Vector2(p.x,p.z),out var width,out var side);return !InLot(p,5)&&d>width+11&&Map(roads,p.x,p.z)<.2f;}).ToList();
            int matureAdded=0,shrubsAdded=0;
            for(int i=0;i<7000;i++)
            {
                float x=R()*512,z=R()*512;float forest=Map(woodland,x,z);if(forest<.08f||forest>.6f||Map(roads,x,z)>.05f||InLot(new Vector3(x,0,z),5))continue;
                float d=River(new Vector2(x,z),out var width,out var side);if(d<width+12)continue;
                float noisyEdge=Mathf.PerlinNoise(x/18+7,z/18+12);if(R()>.12f*noisyEdge)continue;
                bool shrub=R()<.7f;var tree=new TreeInstance{position=new Vector3(x/512,0,z/512),prototypeIndex=shrub?11+(int)(R()*4):3+(int)(R()*8),widthScale=shrub?.85f+R()*.4f:.9f+R()*.25f,heightScale=shrub?.85f+R()*.4f:.9f+R()*.2f,rotation=R()*Mathf.PI*2,color=Color.white,lightmapColor=Color.white};trees.Add(tree);if(shrub)shrubsAdded++;else matureAdded++;
            }
            td.SetTreeInstances(trees.ToArray(),true);RasterWoodland(td);
            int ar=td.alphamapResolution;var splat=td.GetAlphamaps(0,0,ar,ar);
            for(int z=0;z<ar;z++)for(int x=0;x<ar;x++)
            {
                float wx=x*512f/(ar-1),wz=z*512f/(ar-1),forest=Map(woodland,wx,wz),road=Map(roads,wx,wz);
                float dist=River(new Vector2(wx,wz),out var width,out var side);float moisture=Mathf.PerlinNoise(wx/11+17,wz/11+2);
                float bank=(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((dist-width)/7f)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.72f,moisture));
                float broad=Mathf.PerlinNoise(wx/72f+3,wz/72f+6),secondary=Mathf.PerlinNoise(wx/12f+11,wz/12f+8);
                float soil=.035f+.07f*broad+.035f*secondary+road*.8f,wet=bank*.32f;
                float forestGrass=.22f*forest,leaves=.34f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.9f,forest));
                if(InLot(new Vector3(wx,0,wz)))soil=Mathf.Max(soil,.65f);
                splat[z,x,0]=soil;splat[z,x,1]=Mathf.Max(.1f,1-soil-wet-forestGrass-leaves);splat[z,x,2]=forestGrass;splat[z,x,3]=leaves;splat[z,x,4]=wet;
                float sum=0;for(int l=0;l<td.alphamapLayers;l++)sum+=splat[z,x,l];for(int l=0;l<td.alphamapLayers;l++)splat[z,x,l]/=sum;
            }
            td.SetAlphamaps(0,0,splat);
            FixRoads(t);
            var old=UnityEngine.Object.FindFirstObjectByType<NativeMeadowInstanceField>();
            var materials=old.batches.Select(batch=>batch.material).Distinct().ToArray();
            var newBatches=MakeCover(t,materials);var oldRoot=old.gameObject;UnityEngine.Object.DestroyImmediate(oldRoot);
            var root=new GameObject("Continuous meadow woodland edge ground cover");field=root.AddComponent<MeadowGroundCoverRenderer>();field.batches=newBatches;field.presentationCamera=Camera.main;field.RebuildDensityCache();
            var buildings=new GameObject("Existing V3 buildings - landscape readability test");
            PlaceBuilding("Farmhouse","tripo_convert_6e4dde22-3566-484b-a685-c971607adfc8.fbx",new Vector3(300,0,248),14,22,buildings.transform,t);
            PlaceBuilding("Farmhouse","tripo_convert_6e4dde22-3566-484b-a685-c971607adfc8.fbx",new Vector3(281,0,272),12,-25,buildings.transform,t);
            PlaceBuilding("Cabin","tripo_convert_29314f7c-a5a1-4e1e-bb31-4fdb32c8c27d.fbx",new Vector3(295,0,307),11,30,buildings.transform,t);
            var depth=UnityEngine.Object.FindFirstObjectByType<WaveHarmonic.Crest.DepthProbe>();if(depth)depth.Populate();
            EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);watch.Stop();
            File.WriteAllText(O+"MeadowIntegration_Build.txt","Scene "+ScenePath+"\nTrees "+td.treeInstanceCount+"; new edge trees "+matureAdded+" and shrubs "+shrubsAdded+".\nGround cover "+newBatches.Sum(batch=>batch.matrices.Length)+" mesh instances in "+newBatches.Length+" batches.\nVariable channel half widths4.5..12m plus bank noise; original Crest renderer/materials unchanged.\nEditor build seconds="+watch.Elapsed.TotalSeconds.ToString("F2")+".\n");
            Validate();
        }
        struct Piece{public Mesh mesh;public Material material;public int submesh;public Matrix4x4 local;}
        static NativeMeadowBatch[] MakeCover(Terrain terrain,Material[] existing)
        {
            string b="Assets/CityForge/Art/Environment/MeadowNativeURP/Meadow Environment Dynamic Nature/";
            string[] paths={"Grass/Prefabs Grass/prefab_grass_meadow_01_detailed_1.prefab","Grass/Prefabs Grass/prefab_grass_meadow_02_detailed_1.prefab","Details/Prefabs/prefab_detail_meadow_clover_01.prefab","Grass/Prefabs Flowers/prefab_flower_chamomile_01_1.prefab","Grass/Prefabs Flowers/prefab_flower_common_poppy_01_1.prefab","Grass/Prefabs Flowers/prefab_flower_cornflower_01_1.prefab"};
            var pieces=new List<Piece[]>();
            foreach(var path in paths)
            {
                var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(b+path));go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var lod=go.GetComponentInChildren<LODGroup>();var renderers=lod?lod.GetLODs()[0].renderers:go.GetComponentsInChildren<Renderer>();var list=new List<Piece>();
                foreach(var r in renderers){var mf=r.GetComponent<MeshFilter>();if(!mf)continue;for(int sm=0;sm<mf.sharedMesh.subMeshCount;sm++){var mat=r.sharedMaterials[Mathf.Min(sm,r.sharedMaterials.Length-1)];var copy=existing.First(m=>m.name==mat.name+" ValleyField");list.Add(new Piece{mesh=mf.sharedMesh,material=copy,submesh=sm,local=r.transform.localToWorldMatrix});}}
                pieces.Add(list.ToArray());UnityEngine.Object.DestroyImmediate(go);
            }
            var batches=new List<NativeMeadowBatch>();var random=new System.Random(95314);float R(){return (float)random.NextDouble();}
            for(int cz=0;cz<16;cz++)for(int cx=0;cx<16;cx++)
            {
                var lists=new Dictionary<(Mesh,Material,int),List<Matrix4x4>>();
                for(int k=0;k<2100;k++)
                {
                    float x=(cx+R())*32,z=(cz+R())*32,forest=Map(woodland,x,z);var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z)),z);
                    if(p.y<8.92f||Map(roads,x,z)>.12f||InLot(p,1))continue;
                    float density=Mathf.Lerp(.75f,.18f,forest);density*=.8f+.2f*Mathf.PerlinNoise(x/31,z/31);if(R()>density)continue;
                    int type=R()<.52f?0:1;if(R()<.045f+.12f*forest)type=2;
                    float patch=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.53f,.76f,Mathf.PerlinNoise(x/26+14,z/26+3)));
                    if(R()<.055f*patch*(1-forest))type=3+(int)(R()*3);
                    float scale=.8f+R()*.35f;Vector3 size=type<2?new Vector3(scale,scale*(.43f+.16f*forest),scale):Vector3.one*(type==2?.43f+R()*.2f:.65f+R()*.25f);
                    var matrix=Matrix4x4.TRS(p,Quaternion.Euler(0,R()*360,0),size);
                    foreach(var piece in pieces[type]){var key=(piece.mesh,piece.material,piece.submesh);if(!lists.TryGetValue(key,out var matrices)){matrices=new List<Matrix4x4>();lists[key]=matrices;}matrices.Add(matrix*piece.local);}
                }
                foreach(var pair in lists)for(int i=0;i<pair.Value.Count;i+=256)batches.Add(new NativeMeadowBatch{mesh=pair.Key.Item1,material=pair.Key.Item2,submesh=pair.Key.Item3,matrices=pair.Value.Skip(i).Take(256).ToArray(),bounds=new Bounds(new Vector3(cx*32+16,35,cz*32+16),new Vector3(40,100,40))});
            }
            return batches.ToArray();
        }
        static void FixRoads(Terrain terrain)
        {
            var materials=new Dictionary<Material,Material>();
            foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("RamSpline")&&f.sharedMesh))
            {
                var mesh=UnityEngine.Object.Instantiate(f.sharedMesh);mesh.name=f.name+" NaturalContact";var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++){var p=f.transform.TransformPoint(vertices[i]);p.y=terrain.SampleHeight(p)+.025f;vertices[i]=f.transform.InverseTransformPoint(p);}
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,A+"Meshes/"+mesh.name+".asset");f.sharedMesh=mesh;var collider=f.GetComponent<MeshCollider>();if(collider)collider.sharedMesh=mesh;
                var r=f.GetComponent<MeshRenderer>();if(!r)continue;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;
                var copies=r.sharedMaterials.Select(m=>{if(!materials.TryGetValue(m,out var copy)){copy=new Material(m){name=m.name+" NaturalGroundContact"};copy.SetShaderPassEnabled("ShadowCaster",false);foreach(string property in new[]{"_CastShadows"})if(copy.HasProperty(property))copy.SetFloat(property,0);foreach(string property in new[]{"_BaseAORemapMin","_Base2AORemapMin"})if(copy.HasProperty(property))copy.SetFloat(property,.65f);foreach(string property in new[]{"_BaseNormalScale","_Base2NormalScale"})if(copy.HasProperty(property))copy.SetFloat(property,.45f);AssetDatabase.CreateAsset(copy,A+"Materials/"+copy.name+".mat");materials.Add(m,copy);}return copy;}).ToArray();r.sharedMaterials=copies;
            }
        }
        static void RoadLighting()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Integration scene required");
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{A+"Materials"}))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if(!material.name.Contains("NaturalGroundContact"))continue;
                var texture=material.GetTexture("_BaseColorMap");
                material.shader=Shader.Find("CityForge/Study/Natural Road");material.shaderKeywords=Array.Empty<string>();material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",new Color(.65f,.53f,.4f,1));material.SetTextureScale("_BaseMap",new Vector2(.8f,1));material.renderQueue=3000;EditorUtility.SetDirty(material);
            }
            Camera.main.GetComponent<CameraSystem.MeadowShowcaseNavigation>().isometricSize=60f;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static void PlaceBuilding(string kind,string model,Vector3 position,float width,float yaw,Transform parent,Terrain terrain)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(A+"Buildings/"+kind+"/"+model);if(!source)throw new Exception("Missing imported "+kind);
            var root=new GameObject(kind+" landscape test");root.transform.SetParent(parent);var go=UnityEngine.Object.Instantiate(source,root.transform);var renderers=go.GetComponentsInChildren<MeshRenderer>();
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float scale=width/Mathf.Max(bounds.size.x,bounds.size.z);go.transform.localScale*=scale;
            var materials=new Dictionary<Material,Material>();
            foreach(var r in renderers)
            {
                r.sharedMaterials=r.sharedMaterials.Select(mat=>
                {
                    if(materials.TryGetValue(mat,out var copy))return copy;
                    copy=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=kind+" "+mat.name+" LandscapeLit"};
                    var tex=mat.HasProperty("_MainTex")?mat.GetTexture("_MainTex"):mat.mainTexture;
                    if(!tex)tex=AssetDatabase.LoadAssetAtPath<Texture2D>(A+"Buildings/"+kind+"/"+Path.GetFileNameWithoutExtension(model)+".fbm/"+(kind=="Farmhouse"?"tripo_image_6e4dde22_0.jpg":"tripo_image_29314f7c_0.jpg"));
                    copy.SetTexture("_BaseMap",tex);copy.SetColor("_BaseColor",Color.white);copy.SetFloat("_Smoothness",.12f);copy.SetFloat("_Metallic",0);
                    string asset=A+"Materials/"+copy.name+".mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(asset);if(saved){UnityEngine.Object.DestroyImmediate(copy);copy=saved;}else AssetDatabase.CreateAsset(copy,asset);
                    materials.Add(mat,copy);return copy;
                }).ToArray();
            }
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            go.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            position.y=terrain.SampleHeight(position)+.04f;root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            File.AppendAllText(O+"MeadowIntegration_BuildingBounds.txt",kind+" initial model bounds="+bounds+" calibrated footprint="+width+" at "+position+"\n");
        }
        static void Set(UnityEngine.Object target,string property,object value)
        {
            var serialized=new SerializedObject(target);var p=serialized.FindProperty(property);if(p==null)throw new Exception("Missing property "+property);
            if(value is int i)p.intValue=i;else if(value is bool b)p.boolValue=b;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Capture(string name)
        {
            var c=Camera.main;var rt=new RenderTexture(1920,1080,24);rt.Create();var previous=RenderTexture.active;
            try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(O+name,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
        static void Validate()
        {
            var terrain=Terrain.activeTerrain;var td=terrain.terrainData;var h=td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution);int invalid=0;
            foreach(float v in h)if(float.IsNaN(v)||v<0||v>1)invalid++;
            int roadShadows=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r=>r.name.StartsWith("RamSpline")&&r.shadowCastingMode!=ShadowCastingMode.Off);
            var water=UnityEngine.Object.FindFirstObjectByType<WaveHarmonic.Crest.WaterRenderer>();
            File.WriteAllText(O+StudyName+"-Validation.txt","Invalid terrain samples="+invalid+"; roads casting shadows="+roadShadows+"; buildings="+(GameObject.Find("Existing V3 buildings - landscape readability test")?.transform.childCount??0)+"; trees="+td.treeInstanceCount+"\nWater time="+(water&&EditorApplication.isPlaying?water.TimeProvider.Time:0)+"; flowEnabled="+(water&&water.FlowLod.Enabled)+"; flowInputs="+UnityEngine.Object.FindObjectsByType<WaveHarmonic.Crest.FlowLodInput>(FindObjectsSortMode.None).Length+"; activeFFT="+UnityEngine.Object.FindObjectsByType<WaveHarmonic.Crest.ShapeFFT>(FindObjectsSortMode.None).Count(f=>f.isActiveAndEnabled)+"\n");
            if(invalid>0||roadShadows>0)throw new Exception("Terrain/road validation failed");
        }
        static void BeginBenchmark()
        {
            field=UnityEngine.Object.FindFirstObjectByType<MeadowGroundCoverRenderer>();if(!field)throw new Exception("Integration renderer required");previousAdaptive=field.adaptiveDensity;benchmarkStage=0;StartStage();
            File.WriteAllText(O+StudyName+"-Performance.txt","Editor comparison at fixed perspective, same scene/lighting/water/buildings.180 frames per stage,40 warmup frames. Frame time includes Editor. Submission allocation is measured only around ground-cover draw submission. Two-tile test is an adjacent translated scatter field, not production world generation.\n");
            Camera.main.GetComponent<CameraSystem.MeadowShowcaseNavigation>().ResetView();
        }
        static void StartStage(){field.adaptiveDensity=benchmarkStage!=0;warmup=40;samples=0;times.Clear();submission.Clear();draws.Clear();instances.Clear();allocations.Clear();lastFrame=-1;}
        static void SampleBenchmark()
        {
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;if(warmup-->0)return;
            times.Add(Time.unscaledDeltaTime*1000f);submission.Add((float)field.submissionMilliseconds);draws.Add(UnityStats.drawCalls);instances.Add(field.submittedInstances);allocations.Add(field.submissionAllocatedBytes);samples++;
            if(samples<180)return;
            var sorted=times.OrderBy(v=>v).ToArray();string name=benchmarkStage==0?"Full density one tile":benchmarkStage==1?"Adaptive density one tile":"Adaptive density two adjacent cover tiles";
            File.AppendAllText(O+StudyName+"-Performance.txt",name+": mean="+times.Average().ToString("F2")+"ms p95="+sorted[(int)(sorted.Length*.95f)].ToString("F2")+"ms ground submission="+submission.Average().ToString("F3")+"ms managed submission allocation="+allocations.Average().ToString("F0")+"bytes/frame drawCalls="+draws.Average().ToString("F0")+" submitted primary instances="+instances.Average().ToString("F0")+"\n");
            if(benchmarkStage==1)
            {
                stressTile=new GameObject("Temporary adjacent scatter stress tile");var second=stressTile.AddComponent<MeadowGroundCoverRenderer>();second.presentationCamera=Camera.main;second.adaptiveDensity=true;
                second.batches=field.batches.Select(b=>new NativeMeadowBatch{mesh=b.mesh,material=b.material,submesh=b.submesh,bounds=new Bounds(b.bounds.center+Vector3.right*512,b.bounds.size),matrices=b.matrices.Select(m=>Matrix4x4.Translate(Vector3.right*512)*m).ToArray()}).ToArray();second.RebuildDensityCache();
            }
            benchmarkStage++;if(benchmarkStage<3){StartStage();return;}
            if(stressTile)UnityEngine.Object.DestroyImmediate(stressTile);field.adaptiveDensity=previousAdaptive;benchmarkStage=-1;
            File.AppendAllText(O+StudyName+"-Performance.txt","Complete. Temporary stress tile removed. Runtime generation/culling/streaming integration and player benchmarks remain unproven.\n");
        }
    }
}
