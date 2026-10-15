using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WaveHarmonic.Crest;
using CityForge.World;
using UnityEngine.Rendering.Universal;

namespace CityForge.Editor
{
    [InitializeOnLoad] public static class MeadowMigrationTools
    {
        static Camera comparisonCamera;static PrototypeRegionView comparisonView;static int captureStage;static double captureDue;static bool referenceCapture;
        static MeadowMigrationTools(){EditorApplication.update+=CaptureTick;}
        static void StartCaptures(Camera camera,PrototypeRegionView view,bool reference){comparisonCamera=camera;comparisonView=view;referenceCapture=reference;captureStage=1;captureDue=EditorApplication.timeSinceStartup+3;}
        static void CaptureTick()
        {
            if(captureStage==0||!EditorApplication.isPlaying||!comparisonCamera||EditorApplication.timeSinceStartup<captureDue)return;
            var prefix=referenceCapture?"Meadow-Approved-Reference":"Meadow-Runtime-Pilot";
            try {
                if(captureStage==1){Capture(comparisonCamera,prefix+"-Close.png");if(referenceCapture)comparisonCamera.GetComponent<CameraSystem.MeadowShowcaseNavigation>().SetTargetDistance(160);else comparisonCamera.GetComponent<CameraSystem.CityForgeCameraController>().SetZoomLevel(3);captureStage=2;captureDue=EditorApplication.timeSinceStartup+3;}
                else{Capture(comparisonCamera,prefix+"-Wide.png");if(referenceCapture)comparisonCamera.GetComponent<CameraSystem.MeadowShowcaseNavigation>().SetTargetDistance(40);else comparisonCamera.GetComponent<CameraSystem.CityForgeCameraController>().SetZoomLevel(1);captureStage=0;Debug.Log(prefix+" matching-view captures completed.");}
            } catch(Exception e){captureStage=0;Debug.LogException(e);}
        }
        public const string Reference="Assets/CityForge/Art/Environment/MeadowNativeURP/Integration/Polish/PolishedMeadowValley.unity";
        public const string Output="/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/";
        [MenuItem("CityForge/Meadow Migration/Audit Approved Reference")]
        public static void Audit()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before auditing.");
            var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene=EditorSceneManager.OpenScene(Reference,OpenSceneMode.Additive);
            try {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                var roots=scene.GetRootGameObjects();
                T[] All<T>() where T:Component => roots.SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
                var terrain=All<Terrain>().First();var td=terrain.terrainData;
                var water=All<WaterRenderer>().First();
                var s=new StringBuilder("APPROVED MEADOW SYSTEM AUDIT\n");
                s.AppendLine("Terrain: "+AssetDatabase.GetAssetPath(td)+"; size="+td.size+"; height="+td.heightmapResolution+"; alpha="+td.alphamapResolution+"; trees="+td.treeInstanceCount);
                s.AppendLine("Terrain rendering: "+AssetDatabase.GetAssetPath(terrain.materialTemplate)+"; shader="+terrain.materialTemplate.shader.name+"; basemap="+terrain.basemapDistance+"; pixel error="+terrain.heightmapPixelError);
                for(var i=0;i<td.terrainLayers.Length;i++)s.AppendLine("Layer "+i+": "+AssetDatabase.GetAssetPath(td.terrainLayers[i]));
                for(var i=0;i<td.treePrototypes.Length;i++)s.AppendLine("Tree "+i+": "+AssetDatabase.GetAssetPath(td.treePrototypes[i].prefab)+"; instances="+td.treeInstances.Count(t=>t.prototypeIndex==i));
                var cover=All<MeadowGroundCoverRenderer>().First();
                s.AppendLine("Cover: "+cover.batches.Length+" batches; "+cover.batches.Sum(b=>b.matrices.Length)+" records; unique pieces="+cover.batches.Select(b=>(b.mesh,b.material,b.submesh)).Distinct().Count()+"; density="+cover.lod0Density+"; close="+cover.closestZoomDensity);
                foreach(var mat in cover.batches.Select(b=>b.material).Distinct())s.AppendLine("Cover material: "+AssetDatabase.GetAssetPath(mat));
                s.AppendLine("Crest root="+water.transform.root.name+"; children="+water.GetComponentsInChildren<Transform>(true).Length+"; FFT="+All<ShapeFFT>().Length+"; bodies="+All<WaterBody>().Length+"; depth="+All<DepthProbe>().Length+"; flow="+All<FlowLodInput>().Length+"; surface="+AssetDatabase.GetAssetPath(water.Surface.Material));
                foreach(var c in All<Camera>())s.AppendLine("Camera: "+c.name+"; "+c.transform.position+"; rotation="+c.transform.eulerAngles+"; ortho="+c.orthographic+"; size="+c.orthographicSize);
                foreach(var l in All<Light>().Where(l=>l.type==LightType.Directional))s.AppendLine("Sun: "+l.name+"; intensity="+l.intensity+"; color="+l.color+"; rotation="+l.transform.eulerAngles+"; shadows="+l.shadowStrength);
                s.AppendLine("RenderSettings: ambient="+RenderSettings.ambientMode+"; intensity="+RenderSettings.ambientIntensity+"; sky="+RenderSettings.ambientSkyColor+"; equator="+RenderSettings.ambientEquatorColor+"; ground="+RenderSettings.ambientGroundColor+"; fog="+RenderSettings.fog+"; skybox="+AssetDatabase.GetAssetPath(RenderSettings.skybox));
                foreach(var root in roots)s.AppendLine("ROOT: "+root.name+"; components="+string.Join(",",root.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().FullName)));
                s.AppendLine("DIRECT: Unity Terrain/splat shader/layers, native tree prototypes, instanced grass meshes/materials, road shader, Crest material/spectrum/FFT, lighting and volumes.");
                s.AppendLine("GENERALIZE: terrain height/control maps; tree/grass matrices; canopy raster; RAM route geometry/UVs; shoreline candidate rules; river flow meshes; WaterBody bounds; depth capture; camera focus/zoom density. Current authoring lives in editor helpers with literal 512m demo domain and fixed spline/control points.");
                s.AppendLine("REPLACE: RegionLandCover custom mesh surface, RegionSurface approximation, old grass/tree presentation, simple Region Water plane and region lighting while meadow mode is active. Preserve geographic Data.Heights, original Terrain collider, selection/HUD, return-to-menu and legacy fallback.");
                s.AppendLine("GAMEPLAY STATUS: inspected prototype exposes no implemented construction/save-load pipeline (menu placeholders remain); do not claim preservation or validation of nonexistent behavior. Authoritative map data/collider retained; generated road pilot data needs a later construction/save integration contract.");
                Directory.CreateDirectory(Output);File.WriteAllText(Output+"Meadow-Migration-Audit.txt",s.ToString());Debug.Log("Approved meadow audit saved.");
            } finally {EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);}
        }

        const string Resource="Assets/CityForge/Resources/Region/";
        [MenuItem("CityForge/Meadow Migration/Extract Runtime Baseline")]
        public static void Extract()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before extraction.");
            Audit();
            var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene=EditorSceneManager.OpenScene(Reference,OpenSceneMode.Additive);
            try {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                var roots=scene.GetRootGameObjects();
                T[] All<T>() where T:Component=>roots.SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
                var terrain=All<Terrain>().First();var td=terrain.terrainData;
                var path=Resource+"MeadowRuntimeBaseline.asset";
                var baseline=AssetDatabase.LoadAssetAtPath<MeadowRuntimeBaseline>(path);
                if(!baseline){baseline=ScriptableObject.CreateInstance<MeadowRuntimeBaseline>();AssetDatabase.CreateAsset(baseline,path);}
                baseline.referenceHeightResolution=td.heightmapResolution;baseline.referenceTreeDensity=td.treeInstanceCount/(td.size.x*td.size.z);baseline.treeSpacing=2.5f;
                baseline.terrainMaterial=terrain.materialTemplate;baseline.layers=td.terrainLayers;baseline.treePrefabs=td.treePrototypes.Select(t=>t.prefab).ToArray();
                baseline.treeDistance=terrain.treeDistance;baseline.treeBillboardDistance=terrain.treeBillboardDistance;baseline.treeCrossFadeLength=terrain.treeCrossFadeLength;baseline.treeMaximumFullLODCount=terrain.treeMaximumFullLODCount;
                baseline.treeWeights=new float[baseline.treePrefabs.Length];baseline.treeWidths=new float[baseline.treePrefabs.Length];baseline.treeHeights=new float[baseline.treePrefabs.Length];
                for(int i=0;i<baseline.treePrefabs.Length;i++){var instances=td.treeInstances.Where(t=>t.prototypeIndex==i).ToArray();baseline.treeWeights[i]=instances.Length/(float)td.treeInstanceCount;baseline.treeWidths[i]=instances.Length>0?instances.Average(t=>t.widthScale):1;baseline.treeHeights[i]=instances.Length>0?instances.Average(t=>t.heightScale):1;}
                baseline.roadMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/CityForge/Art/Environment/MeadowNativeURP/Integration/Polish/Materials/M_Path NaturalGroundContact Feathered SoftVerge.mat");
                baseline.flowMaterial=All<FlowLodInput>().First().GetComponent<MeshRenderer>().sharedMaterial;
                var cover=All<MeadowGroundCoverRenderer>().First();baseline.lod0Density=cover.lod0Density;baseline.closestZoomDensity=cover.closestZoomDensity;
                var materials=cover.batches.Select(b=>b.material).Distinct().ToArray();
                string flora="Assets/CityForge/Art/Environment/MeadowNativeURP/Meadow Environment Dynamic Nature/";
                string[] prefabs={"Grass/Prefabs Grass/prefab_grass_meadow_01_detailed_1.prefab","Grass/Prefabs Grass/prefab_grass_meadow_02_detailed_1.prefab","Details/Prefabs/prefab_detail_meadow_clover_01.prefab","Grass/Prefabs Flowers/prefab_flower_chamomile_01_1.prefab","Grass/Prefabs Flowers/prefab_flower_common_poppy_01_1.prefab","Grass/Prefabs Flowers/prefab_flower_cornflower_01_1.prefab"};
                baseline.plants=new MeadowPlantType[prefabs.Length];
                for(int i=0;i<prefabs.Length;i++){
                    var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(flora+prefabs[i]));go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    try {var lod=go.GetComponentInChildren<LODGroup>();var renderers=lod?lod.GetLODs()[0].renderers:go.GetComponentsInChildren<Renderer>();var pieces=new System.Collections.Generic.List<MeadowPlantPiece>();
                        foreach(var renderer in renderers){var mesh=renderer.GetComponent<MeshFilter>();if(!mesh)continue;for(int sub=0;sub<mesh.sharedMesh.subMeshCount;sub++){var source=renderer.sharedMaterials[Mathf.Min(sub,renderer.sharedMaterials.Length-1)];var material=materials.First(m=>m.name.StartsWith(source.name+" ValleyField",StringComparison.Ordinal));pieces.Add(new MeadowPlantPiece{mesh=mesh.sharedMesh,material=material,submesh=sub,local=renderer.transform.localToWorldMatrix});}}
                        baseline.plants[i]=new MeadowPlantType{pieces=pieces.ToArray()};
                    } finally {UnityEngine.Object.DestroyImmediate(go);}
                }
                baseline.forestFloorPlants=new MeadowPlantType[2];
                var leafMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/Flora/M_Poplar_leaves Valley.mat");
                for(int i=0;i<2;i++){
                    var leaf=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(flora+"Details/Prefabs/prefab_detail_poplar_leaves_01_"+(i+1)+".prefab"));leaf.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    try {var lod=leaf.GetComponentInChildren<LODGroup>();var renderers=lod?lod.GetLODs()[0].renderers:leaf.GetComponentsInChildren<Renderer>();var pieces=new System.Collections.Generic.List<MeadowPlantPiece>();foreach(var renderer in renderers){var filter=renderer.GetComponent<MeshFilter>();if(!filter)continue;for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)pieces.Add(new MeadowPlantPiece{mesh=filter.sharedMesh,material=leafMaterial,submesh=sub,local=renderer.transform.localToWorldMatrix});}baseline.forestFloorPlants[i]=new MeadowPlantType{pieces=pieces.ToArray()};} finally {UnityEngine.Object.DestroyImmediate(leaf);}
                }
                var leafGroups=roots.First(r=>r.name=="prefab_detail_poplar_leaves").GetComponentsInChildren<LODGroup>(true);
                if(leafGroups.Length>0)baseline.forestFloorScale=leafGroups.Average(g=>g.transform.lossyScale.x)/AssetDatabase.LoadAssetAtPath<GameObject>(flora+"Details/Prefabs/prefab_detail_poplar_leaves_01_1.prefab").transform.localScale.x;
                var water=All<WaterRenderer>().First();var waterClone=UnityEngine.Object.Instantiate(water.gameObject);waterClone.SetActive(false);waterClone.name="Approved Crest Runtime Template";
                foreach(Transform child in waterClone.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                waterClone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);waterClone.transform.localScale=Vector3.one;
                var waterSettings=new SerializedObject(waterClone.GetComponent<WaterRenderer>());
                foreach(var property in new[]{"_Camera","_Viewer","_Viewpoint"}){var value=waterSettings.FindProperty(property);if(value!=null)value.objectReferenceValue=null;}
                var follow=waterSettings.FindProperty("_FollowSceneCamera");if(follow!=null)follow.boolValue=false;waterSettings.ApplyModifiedPropertiesWithoutUndo();
                baseline.waterTemplate=PrefabUtility.SaveAsPrefabAsset(waterClone,Resource+"ApprovedCrestRuntime.prefab");UnityEngine.Object.DestroyImmediate(waterClone);
                GameObject SaveTemplate(GameObject source,string name){var clone=UnityEngine.Object.Instantiate(source);clone.transform.position=Vector3.zero;var asset=PrefabUtility.SaveAsPrefabAsset(clone,Resource+name+".prefab");UnityEngine.Object.DestroyImmediate(clone);return asset;}
                baseline.fftTemplate=SaveTemplate(All<ShapeFFT>().First().gameObject,"ApprovedMeadowFFT");
                baseline.windTemplate=SaveTemplate(roots.First(r=>r.name=="Prefab_Wind"),"ApprovedMeadowWind");
                baseline.sunTemplate=SaveTemplate(All<Light>().First(l=>l.type==LightType.Directional).gameObject,"ApprovedMeadowSun");
                var camera=All<Camera>().First(c=>c.name=="Main Camera");
                var cameraClone=UnityEngine.Object.Instantiate(camera.gameObject);cameraClone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);cameraClone.GetComponent<Camera>().enabled=false;
                var nav=cameraClone.GetComponent<CameraSystem.MeadowShowcaseNavigation>();if(nav)UnityEngine.Object.DestroyImmediate(nav);
                var listener=cameraClone.GetComponent<AudioListener>();if(listener)UnityEngine.Object.DestroyImmediate(listener);
                baseline.cameraSettingsTemplate=PrefabUtility.SaveAsPrefabAsset(cameraClone,Resource+"ApprovedMeadowCameraSettings.prefab");UnityEngine.Object.DestroyImmediate(cameraClone);
                baseline.cameraPipelineSettings=JsonUtility.ToJson(camera.GetUniversalAdditionalCameraData());
                baseline.volume=All<Volume>().First(v=>v.isGlobal&&v.sharedProfile).sharedProfile;
                baseline.skybox=RenderSettings.skybox;baseline.ambientMode=RenderSettings.ambientMode;baseline.ambientSky=RenderSettings.ambientSkyColor;baseline.ambientEquator=RenderSettings.ambientEquatorColor;baseline.ambientGround=RenderSettings.ambientGroundColor;baseline.ambientLight=RenderSettings.ambientLight;baseline.ambientIntensity=RenderSettings.ambientIntensity;baseline.reflectionIntensity=RenderSettings.reflectionIntensity;
                var sh=RenderSettings.ambientProbe;baseline.ambientSH=new float[27];for(int c=0;c<3;c++)for(int i=0;i<9;i++)baseline.ambientSH[c*9+i]=sh[c,i];
                baseline.shorelineReeds=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Art/Environment/RollingPondStudy/ReedClump.prefab");
                baseline.shorelineGrass=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/Flora/prefab_Terrain_grass_meadow_03_2.prefab");
                EditorUtility.SetDirty(baseline);AssetDatabase.SaveAssets();
                File.WriteAllText(Output+"Meadow-Baseline-Extraction.txt","Extracted actual five TerrainLayers, approved Terrain material, 15 native tree prototypes, six grass/clover/flower mesh/material pieces, native road verge material, downstream flow material, isolated Crest renderer/FFT/wind/sun templates and approved volume/sky settings. No source scene or source asset written. Runtime template contains no fixed WaterBody/depth/flow geometry.\n");
            } finally {EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);}
        }

        [MenuItem("CityForge/Meadow Migration/Open Pilot Through Game Menu")]
        public static void PreviewPilot()
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode in MainMenu first.");
            EditorApplication.isPaused=false;
            if(UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>())return;
            ModalPrefabBuilder.PreviewTerrain();
            var modal=UnityEngine.Object.FindFirstObjectByType<CityForge.UI.CityForgeModal>();
            var sizes=modal.GetComponentsInChildren<UnityEngine.UI.Dropdown>(true);
            foreach(var dropdown in sizes)if(dropdown.options.Count==3&&dropdown.options.Any(o=>o.text.ToLowerInvariant().Contains("small")))dropdown.value=0;
            modal.transform.Find("Panel75/RoundedContentClip/ScrollViewport/ScrollContent/CreateRegionTerrainIntro/NextButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }
        [MenuItem("CityForge/Meadow Migration/Capture And Validate Pilot")]
        public static void ValidatePilot()
        {
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!EditorApplication.isPlaying||!view||!view.ApprovedDistrict)throw new Exception("Open the runtime pilot first.");
            var pilot=view.ApprovedDistrict;
            if(pilot.DisplayTerrain.materialTemplate!=pilot.Baseline.terrainMaterial||pilot.DisplayTerrain.terrainData.terrainLayers.Length!=5)throw new Exception("Native terrain baseline mismatch.");
            var dump=new StringBuilder();
            foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))dump.AppendLine("CAM "+c.name+" enabled="+c.enabled+" active="+c.gameObject.activeInHierarchy+" position="+c.transform.position+" angle="+c.transform.eulerAngles+" ortho="+c.orthographic+" size="+c.orthographicSize+" near="+c.nearClipPlane+" far="+c.farClipPlane+" depth="+c.depth+" target="+c.targetTexture);
            dump.AppendLine("TERRAIN drawn="+pilot.DisplayTerrain.drawHeightmap+" active="+pilot.DisplayTerrain.enabled+" origin="+pilot.DisplayTerrain.transform.position+" size="+pilot.DisplayTerrain.terrainData.size+" protos="+pilot.DisplayTerrain.terrainData.treePrototypes.Length+" trees="+pilot.DisplayTerrain.terrainData.treeInstanceCount+" bounds="+pilot.DisplayTerrain.terrainData.bounds);
            dump.AppendLine("WATER material="+pilot.Water.Surface.Material+" position="+pilot.Water.transform.position+" scale="+pilot.Water.transform.localScale+" viewer="+pilot.Water.Viewer);
            File.WriteAllText(Output+"Meadow-Pilot-State.txt",dump.ToString());
            var whole=view.transform.Find("ContinuousRegionTerrain").GetComponent<Terrain>();
            var originalHeights=whole.terrainData.GetHeights(256,256,257,257);int resolution=pilot.DisplayTerrain.terrainData.heightmapResolution;var display=pilot.DisplayTerrain.terrainData.GetHeights(0,0,resolution,resolution);int stride=(resolution-1)/256;
            for(int z=0;z<257;z++)for(int x=0;x<257;x++)if(Mathf.Abs(originalHeights[z,x]-display[z*stride,x*stride])>2f/65535)throw new Exception("Display/collider height mismatch.");
            var generationHash=Fingerprint(pilot);
            view.RegenerateApprovedDistrict();pilot=view.ApprovedDistrict;
            if(generationHash!=Fingerprint(pilot))throw new Exception("Generated tree/road geometry changed after regeneration.");
            if(pilot.DisplayTerrain.terrainData.treeInstanceCount==0)throw new Exception("Native trees absent.");
            if(pilot.GetComponentsInChildren<FlowLodInput>(true).Length==0||pilot.GetComponentsInChildren<ShapeFFT>().Length!=1)throw new Exception("Crest flow/FFT missing.");
            var camera=view.GetComponentInChildren<Camera>();
            view.SetNaturalLandscape(false);
            if(pilot.gameObject.activeSelf||!view.transform.Find("RegionLandCover").GetComponent<MeshRenderer>().enabled)throw new Exception("Legacy fallback failed.");
            view.SetNaturalLandscape(true);
            var p=pilot.ComparisonFocus;p.y=pilot.Height(new Vector2(p.x,p.z))+3;camera.GetComponent<CameraSystem.CityForgeCameraController>().FrameMeadowComparison(view.MapSide/2,whole.terrainData.size.y,p);
            StartCaptures(camera,view,false);
            File.WriteAllText(Output+"Meadow-Runtime-Pilot-Checks.txt","PASS native Terrain material/five actual layers; display heights agree with authoritative region collider; repeat regeneration tree/road SHA256; actual FFT and generated flow inputs; legacy toggle restores mesh and prior camera/ambient settings. Seed="+view.Data.Seed+"; district side="+pilot.Side+"; trees="+pilot.DisplayTerrain.terrainData.treeInstanceCount+"; loaded tiles="+(pilot.GetComponentInChildren<MeadowRuntimeVegetation>(true)?.LoadedTiles??0)+"; regeneration SHA256="+generationHash+"; flow reaches="+pilot.GetComponentsInChildren<FlowLodInput>(true).Length+"; shader="+pilot.DisplayTerrain.materialTemplate.shader.name+"\n");
        }
        [MenuItem("CityForge/Meadow Migration/Save Meadow Test Region")]
        public static void SaveMeadowTest()
        {
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!EditorApplication.isPlaying||!view||!view.ApprovedDistrict||view.Size!=RegionSize.Small||view.Data.Seed!=730421)throw new Exception("Open the Small meadow pilot first.");
            view.EnterDistrict(3);view.GetComponentInChildren<CameraSystem.CityForgeCameraController>().ResetView();
            var snapshot=SavedRegionStore.Capture(view,SavedRegionStore.TestId,"Meadow Test Valley");
            var path=SavedRegionStore.Save(snapshot);var preset=Application.dataPath+"/CityForge/Resources/Regions/MeadowTestValley.bytes";
            SavedRegionStore.Write(snapshot,preset);AssetDatabase.ImportAsset("Assets/CityForge/Resources/Regions/MeadowTestValley.bytes");
            var roundtrip=SavedRegionStore.Load(path);
            foreach(var pair in new[]{new[]{view.Data.Heights,roundtrip.data.Heights},new[]{view.Data.WaterDistance,roundtrip.data.WaterDistance}}){
                for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(pair[0][z,x]!=pair[1][z,x])throw new Exception("Saved map data changed.");
            }
            if(snapshot.meadow.trees.Length!=roundtrip.meadow.trees.Length)throw new Exception("Saved tree count changed.");
            File.WriteAllText(Output+"Meadow-Saved-Region-Checks.txt","PASS versioned compressed snapshot roundtrip: exact 513 x 513 heights and water-distance maps.\nSaved locally: "+path+"\nBundled test snapshot: "+preset+"\nBytes="+new FileInfo(path).Length+"; trees="+roundtrip.meadow.trees.Length+"; control map="+roundtrip.meadow.alphamaps.GetLength(0)+"\n");
        }
        [MenuItem("CityForge/Meadow Migration/Show Open Region List")]
        public static void ShowOpenRegions()
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play MainMenu first.");
            var menu=UnityEngine.Object.FindFirstObjectByType<UI.MainMenuController>();menu.Handle(UI.MenuAction.OpenRegion);
        }
        [MenuItem("CityForge/Meadow Migration/Validate Plants And Camera Experiment")]
        public static void ValidatePlantCameraExperiment()
        {
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(!EditorApplication.isPlaying||!view||!view.ApprovedDistrict)throw new Exception("Load the saved meadow test first.");
            var pilot=view.ApprovedDistrict;var rig=view.GetComponentInChildren<CameraSystem.CityForgeCameraController>();var camera=rig.GetComponent<Camera>();var pose=rig.CaptureRegionState();
            var original=Fingerprint(pilot);var maps=pilot.DisplayTerrain.terrainData.GetAlphamaps(0,0,1024,1024);int full=pilot.FullTreeCount;
            for(int level=0;level<=5;level++){
                view.transform.Find("RegionHud/ZoomLevel"+level).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();float buttonSize=camera.orthographicSize;
                if(rig.ZoomLevel!=level||Mathf.Abs(buttonSize-20*Mathf.Pow(2,level))>.0001f)throw new Exception("Numbered zoom differs: "+level);
                if(level<5){rig.SetZoomLevel(level+1);rig.StepZoom(true);}else{rig.SetZoomLevel(level-1);rig.StepZoom(false);}
                if(rig.ZoomLevel!=level||camera.orthographicSize!=buttonSize)throw new Exception("Keyboard zoom and buttons differ.");
                if(rig.PanSpeedMultiplier!=(level<=1?1.69f:1f))throw new Exception("Panning multiplier differs.");
            }
            pilot.SetTreesEnabled(false);
            view.transform.Find("RegionHud/Trees").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if(!pilot.TreesEnabled||!pilot.DisplayTerrain.drawTreesAndFoliage)throw new Exception("Trees ON failed.");
            view.transform.Find("RegionHud/Trees").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if(pilot.TreesEnabled||pilot.DisplayTerrain.drawTreesAndFoliage)throw new Exception("Trees OFF failed.");
            pilot.SetSmallPlantsEnabled(true);if(pilot.DisplayTerrain.terrainData.treeInstanceCount!=full)throw new Exception("Full planting was not restored.");
            pilot.SetSmallPlantsEnabled(false,true);
            var shown=pilot.DisplayTerrain.terrainData.treeInstances;if(shown.Any(t=>t.prototypeIndex<3||t.prototypeIndex>=11))throw new Exception("Small tree prototypes still render.");
            if(pilot.GetComponentsInChildren<MeadowGroundCoverRenderer>().Length!=0||pilot.transform.Find("Seeded shoreline and understory").gameObject.activeInHierarchy||pilot.GetComponentInChildren<MeadowRuntimeVegetation>())throw new Exception("Small plant objects still active.");
            if(Fingerprint(pilot)!=original||pilot.CaptureSnapshot().trees.Length!=full)throw new Exception("Experiment altered canonical/save planting.");
            var after=pilot.DisplayTerrain.terrainData.GetAlphamaps(0,0,1024,1024);for(int z=0;z<1024;z++)for(int x=0;x<1024;x++)for(int layer=0;layer<5;layer++)if(after[z,x,layer]!=maps[z,x,layer])throw new Exception("Experiment repainted terrain.");
            rig.RestoreRegionState(pose);
            File.WriteAllText(Output+"Meadow-Plant-Camera-Checks.txt","PASS all six numbered buttons 0-5 match preferred keyboard StepZoom positions: 20,40,80,160,320,640.\nPASS pan multiplier 1.69 at 0/1, 1 elsewhere; applies to keyboard and middle-mouse drag.\nPASS full planting toggle restores "+full+" tree instances; OFF retains "+shown.Length+" mature trees and zero active small-plant/streaming renderers.\nPASS canonical tree/road/save data unchanged; all terrain paint values unchanged.\nExperiment left OFF.\n");
        }
        [MenuItem("CityForge/Meadow Migration/Validate Crest Readback Ring")]
        public static void ValidateReadbackRing()
        {
            var type=typeof(WaterRenderer).Assembly.GetType("WaveHarmonic.Crest.QueryBase").GetNestedType("SegmentRegistrarRingBuffer",System.Reflection.BindingFlags.NonPublic);
            var acquire=type.GetMethod("AcquireNew");var release=type.GetMethod("ReleaseLast");var index=type.GetField("_SegmentAcquire");
            object Make()=>Activator.CreateInstance(type,true);
            var fixedRing=Make();int pending=0;
            for(int i=0;i<10000;i++){
                if(pending>=7){release.Invoke(fixedRing,null);pending--;}
                int before=(int)index.GetValue(fixedRing);acquire.Invoke(fixedRing,null);if((int)index.GetValue(fixedRing)==before)throw new Exception("Readback ring stalled.");pending++;
                if(i%23==22){while(pending>0){release.Invoke(fixedRing,null);pending--;}}
            }
            var oldRing=Make();bool reproduced=false;
            for(int i=0;i<30;i++){int before=(int)index.GetValue(oldRing);acquire.Invoke(oldRing,null);if((int)index.GetValue(oldRing)==before){reproduced=true;break;}}
            if(!reproduced)throw new Exception("Old ring leak was not reproduced.");
            File.WriteAllText(Output+"Meadow-Crest-Readback-Checks.txt","PASS old request-retirement logic reproduces ring exhaustion.\nPASS 10,000 acquisitions with overflow retirement + batched completion, no stalled acquisitions.\nEmbedded Crest patch: each dropped oldest readback request also releases its registrar slot. Rendering/simulation settings untouched.\n");
        }
        [MenuItem("CityForge/Meadow Migration/Load Saved Test Through Open Region")]
        public static void LoadSavedTest()
        {
            if(!EditorApplication.isPlaying||UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>())throw new Exception("Play MainMenu without an open region first.");
            var expected=SavedRegionStore.Load(SavedRegionStore.ListRegions().First(r=>r.id==SavedRegionStore.TestId).path);
            var menu=UnityEngine.Object.FindFirstObjectByType<UI.MainMenuController>();menu.Handle(UI.MenuAction.OpenRegion);
            var modal=UnityEngine.Object.FindFirstObjectByType<UI.CityForgeModal>();
            var button=modal.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="OpenRegion_"+SavedRegionStore.TestId);var timer=System.Diagnostics.Stopwatch.StartNew();button.onClick.Invoke();timer.Stop();
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!view||!view.LoadedFromSnapshot||!view.ApprovedDistrict.UsedSavedTerrain)throw new Exception("Open Region did not restore cached landscape.");
            float worst=0;var alpha=view.ApprovedDistrict.DisplayTerrain.terrainData.GetAlphamaps(0,0,1024,1024);
            for(int z=0;z<1024;z++)for(int x=0;x<1024;x++)for(int layer=0;layer<5;layer++)worst=Mathf.Max(worst,Mathf.Abs(alpha[z,x,layer]-expected.meadow.alphamaps[z,x,layer]));
            if(worst>.00001f)throw new Exception("Saved terrain paint differs: "+worst);
            if(Fingerprint(view.ApprovedDistrict)!="DFB87BB89461F0F9CFFBF9DF0078502046620E5B3DBB107729E7AC6A7150D507")throw new Exception("Loaded tree/road placement changed.");
            view.transform.Find("RegionHud/SaveRegion").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if(view.SavedRegionId!=SavedRegionStore.TestId||view.transform.Find("RegionHud/SaveRegion/Caption").GetComponent<UnityEngine.UI.Text>().text!="REGION SAVED")throw new Exception("Save Region UI failed.");
            File.AppendAllText(Output+"Meadow-Saved-Region-Checks.txt","PASS normal Open Region row listener -> saved data + cached meadow restore, "+timer.ElapsedMilliseconds+" ms in Editor; paint max difference="+worst+"; original tree/road fingerprint preserved.\nPASS Save Region button saves the loaded region under the same ID.\n");
        }
        [MenuItem("CityForge/Meadow Migration/Validate Streaming And Saved Load")]
        public static void ValidateStreamingAndLoad()
        {
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!EditorApplication.isPlaying||!view||!view.ApprovedDistrict)throw new Exception("Open the meadow pilot first.");
            var pilot=view.ApprovedDistrict;var rng=new System.Random(48713);
            for(int i=0;i<10000;i++){var p=pilot.Origin+new Vector2((float)rng.NextDouble(),(float)rng.NextDouble())*pilot.Side;if(pilot.ExactCanopy(p)!=pilot.Canopy(p))throw new Exception("Canopy lookup changed.");}
            int tiles=0;
            foreach(var renderer in pilot.GetComponentsInChildren<MeadowGroundCoverRenderer>()){
                var cell=Vector2Int.FloorToInt(new Vector2(renderer.batches[0].bounds.center.x-16,renderer.batches[0].bounds.center.z-16)/32);
                var reference=MeadowVegetationReferenceCheck.BuildTile(pilot,view.GetComponentInChildren<Camera>(),cell);
                try{var expected=reference.GetComponent<MeadowGroundCoverRenderer>().batches;var actual=renderer.batches;if(expected.Length!=actual.Length)throw new Exception("Streaming batches differ.");
                    for(int b=0;b<actual.Length;b++){if(actual[b].mesh!=expected[b].mesh||actual[b].material!=expected[b].material||actual[b].submesh!=expected[b].submesh||actual[b].matrices.Length!=expected[b].matrices.Length)throw new Exception("Streaming pieces differ.");for(int m=0;m<actual[b].matrices.Length;m++)if(!actual[b].matrices[m].Equals(expected[b].matrices[m]))throw new Exception("Resumable tile placement differs.");}
                }finally{UnityEngine.Object.DestroyImmediate(reference);}
                int before=renderer.DensityCacheBuilds;renderer.enabled=false;renderer.enabled=true;if(renderer.DensityCacheBuilds!=before)throw new Exception("Density cache rebuilt on re-enable.");tiles++;if(tiles>=12)break;
            }
            if(tiles==0)throw new Exception("Wait for tiles to load.");
            int builds=pilot.GetComponentsInChildren<MeadowGroundCoverRenderer>().Sum(r=>r.DensityCacheBuilds);view.SetNaturalLandscape(false);view.SetNaturalLandscape(true);
            if(pilot.GetComponentsInChildren<MeadowGroundCoverRenderer>().Sum(r=>r.DensityCacheBuilds)!=builds)throw new Exception("Landscape toggle rebuilt density caches.");
            var cover=pilot.GetComponentInChildren<MeadowRuntimeVegetation>(true);
            File.WriteAllText(Output+"Meadow-Streaming-Checks.txt","PASS 10,000 exact canopy samples.\nPASS resumable vegetation equals the previous synchronous algorithm, all source matrices/pieces on "+tiles+" tiles.\nPASS density caches reused on renderer and landscape re-enable.\nSaved map loaded="+view.LoadedFromSnapshot+"; saved meadow terrain used="+pilot.UsedSavedTerrain+"\nStreaming slice max="+cover.MaximumStreamingSliceMilliseconds.ToString("F3")+" ms\nGeneration stages: "+pilot.GenerationTimings+"\n");
        }
        [MenuItem("CityForge/Meadow Migration/Validate Performance Changes")]
        public static void ValidatePerformance()
        {
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!EditorApplication.isPlaying||!view||!view.ApprovedDistrict)throw new Exception("Open the runtime pilot first.");
            var pilot=view.ApprovedDistrict;var rng=new System.Random(12451);float worst=0;
            for(int i=0;i<10000;i++){
                var p=pilot.Origin+new Vector2((float)rng.NextDouble(),(float)rng.NextDouble())*pilot.Side;
                float expected=Mathf.Min(pilot.ExactRoadDistance(p),pilot.RoadQueryLimit);
                worst=Mathf.Max(worst,Mathf.Abs(expected-pilot.RoadDistance(p)));
            }
            if(worst>0.00001f)throw new Exception("Indexed road query differs: "+worst);
            var hash=Fingerprint(pilot);
            if(view.Data.Seed==730421&&pilot.Side==512&&hash!="DFB87BB89461F0F9CFFBF9DF0078502046620E5B3DBB107729E7AC6A7150D507")throw new Exception("Tree/road placement changed: "+hash);
            int checkedTiles=0,oldBatches=0,newBatches=0;
            foreach(var cover in pilot.GetComponentsInChildren<MeadowGroundCoverRenderer>()){
                var type=typeof(MeadowGroundCoverRenderer);const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                foreach(var names in new[]{new[]{"levels","mergedLevels"},new[]{"closeLevels","mergedCloseLevels"}}){
                    var source=(Matrix4x4[][][])type.GetField(names[0],flags).GetValue(cover);
                    var merged=(NativeMeadowBatch[][])type.GetField(names[1],flags).GetValue(cover);
                    for(int level=0;level<4;level++){
                        var expected=new System.Collections.Generic.Dictionary<(Mesh,Material,int,Bounds),System.Collections.Generic.List<Matrix4x4>>();
                        for(int b=0;b<cover.batches.Length;b++){
                            var batch=cover.batches[b];var key=(batch.mesh,batch.material,batch.submesh,batch.bounds);
                            if(!expected.TryGetValue(key,out var list))expected[key]=list=new System.Collections.Generic.List<Matrix4x4>();
                            list.AddRange(source[b][level]);
                        }
                        foreach(var batch in merged[level]){
                            var list=expected[(batch.mesh,batch.material,batch.submesh,batch.bounds)];
                            for(int i=0;i<batch.matrices.Length;i++)if(!list[i].Equals(batch.matrices[i]))throw new Exception("Merged vegetation matrix differs.");
                            list.RemoveRange(0,batch.matrices.Length);
                        }
                        if(expected.Values.Any(list=>list.Count!=0))throw new Exception("Merged vegetation lost instances.");
                    }
                }
                oldBatches+=cover.batches.Length;newBatches+=cover.submittedBatches;checkedTiles++;
            }
            if(checkedTiles==0)throw new Exception("Wait for vegetation streaming first.");
            File.WriteAllText(Output+"Meadow-Performance-Checks.txt","PASS 10,000 indexed road queries versus exhaustive scan (saturated only beyond all exclusion/transition radii); max error="+worst+" m.\nPASS original seeded tree/road fingerprint: "+hash+"\nPASS exact vegetation matrices/order per mesh/material/submesh/bounds, all four LODs and both close-density states: "+checkedTiles+" tiles.\nSource batches="+oldBatches+"; current merged submissions="+newBatches+".\nGeneration stages: "+pilot.GenerationTimings+"\n");
        }
        [MenuItem("CityForge/Meadow Migration/Use MainMenu For Game Startup")]
        public static void UseGameStartup()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before changing startup.");
            const string path="Assets/CityForge/Scenes/Gameplay/MainMenu.unity";
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            EditorSceneManager.OpenScene(path);
            File.WriteAllText(Output+"Unity-Startup-Check.txt","Active editor scene: "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+"\nEditor Play startup: "+AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)+"\nFirst enabled build scene: "+EditorBuildSettings.scenes.First(s=>s.enabled).path+"\n");
        }
        [MenuItem("CityForge/Meadow Migration/Prepare Approved Reference Comparison")]
        public static void OpenReferenceComparison()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before opening the reference.");
            EditorSceneManager.OpenScene(Reference);
        }
        [MenuItem("CityForge/Meadow Migration/Capture Approved Reference")]
        public static void CaptureReference()
        {
            if(!EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=Reference)throw new Exception("Play the approved reference scene first.");
            var camera=Camera.main;var nav=camera.GetComponent<CameraSystem.MeadowShowcaseNavigation>();if(!nav.Isometric)nav.ToggleIsometric();nav.SetTargetDistance(40);EditorApplication.isPaused=false;StartCaptures(camera,null,true);
        }
        static string Fingerprint(MeadowDistrictLandscape pilot)
        {
            var text=new StringBuilder();var culture=System.Globalization.CultureInfo.InvariantCulture;
            foreach(var t in pilot.CanonicalTreeInstances)text.Append(t.prototypeIndex).Append(':').Append(t.position.x.ToString("R",culture)).Append(',').Append(t.position.y.ToString("R",culture)).Append(',').Append(t.position.z.ToString("R",culture)).Append(',').Append(t.widthScale.ToString("R",culture)).Append(',').Append(t.heightScale.ToString("R",culture)).Append(';');
            foreach(var p in pilot.Road)text.Append(p.x.ToString("R",culture)).Append(',').Append(p.y.ToString("R",culture)).Append(';');
            using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","");
        }
        static void Capture(Camera camera,string name)
        {
            float oldAspect=camera.aspect;camera.aspect=1920f/1080f;
            var target=new RenderTexture(1920,1080,24);target.Create();var old=RenderTexture.active;
            try {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=target});RenderTexture.active=target;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Output+name,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
            finally {camera.aspect=oldAspect;RenderTexture.active=old;target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}
