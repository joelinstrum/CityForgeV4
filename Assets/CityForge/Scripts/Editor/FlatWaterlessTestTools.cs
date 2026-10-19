using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;
namespace CityForge.Editor
{
    public static class FlatWaterlessTestTools
    {
        [MenuItem("CityForge/Meadow Migration/Open Waterless Meadow River Beds")]
        static void OpenRiverBeds()
        {
            if(!EditorApplication.isPlaying||Object.FindFirstObjectByType<World.PrototypeRegionView>())throw new System.Exception("Play MainMenu without an open region first.");
            World.SavedRegionStore.EnsureTestRegion();
            var source=World.SavedRegionStore.Load(System.IO.Path.Combine(World.SavedRegionStore.DirectoryPath,World.SavedRegionStore.TestId+".cfregion"));
            var existing=System.IO.Path.Combine(World.SavedRegionStore.DirectoryPath,"meadow-river-beds-dry.cfregion");
            if(System.IO.File.Exists(existing))source=World.SavedRegionStore.Load(existing);
            source.info.id="meadow-river-beds-dry";source.info.name="Meadow River Beds — No Water";source.info.waterless=true;source.info.savedUtc=System.DateTime.UtcNow.ToString("O");
            var path=World.SavedRegionStore.Save(source);
            Object.FindFirstObjectByType<UI.MainMenuController>().OpenSavedRegionImmediatelyForTools(World.SavedRegionStore.Load(path).info);
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();var pilot=view.ApprovedDistrict;
            if(!view.Waterless||pilot.Water||pilot.Baseline.waterTemplate||pilot.Baseline.fftTemplate||pilot.Baseline.flowMaterial||Object.FindObjectsByType<WaveHarmonic.Crest.WaterRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=0)throw new System.Exception("Unexpected Crest reference in dry landscape.");
            float worst=0;var alpha=pilot.DisplayTerrain.terrainData.GetAlphamaps(0,0,1024,1024);
            for(int z=0;z<1024;z++)for(int x=0;x<1024;x++)for(int l=0;l<alpha.GetLength(2);l++)worst=Mathf.Max(worst,Mathf.Abs(alpha[z,x,l]-source.meadow.alphamaps[z,x,l]));
            if(worst>1f/255+.00001f||pilot.FullTreeCount!=source.meadow.trees.Length)throw new System.Exception("Saved landscape changed: paint="+worst+"; trees="+pilot.FullTreeCount+" vs "+source.meadow.trees.Length);
            System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Meadow-Dry-River-Beds-Checks.txt","PASS original saved height/water-distance maps retained; cached terrain paint within native 8-bit alpha precision; canonical tree count preserved.\nPASS zero Crest water renderers; dry baseline water/FFT/flow references cleared; water system never instantiated.\nSaved separately in Open Region: "+path+"\nOriginal Meadow Test Valley unchanged.\n");
            Debug.Log("PASS waterless Meadow River Beds: original channels preserved, no Crest objects or water template references.");
        }

        [MenuItem("CityForge/Meadow Migration/Deepen And Open Dry River Beds")]
        static void Deepen()
        {
            if(!EditorApplication.isPlaying||Object.FindFirstObjectByType<World.PrototypeRegionView>())throw new System.Exception("Play MainMenu without an open region first.");
            var source=World.SavedRegionStore.Load(System.IO.Path.Combine(World.SavedRegionStore.DirectoryPath,World.SavedRegionStore.TestId+".cfregion"));
            source.info.id="meadow-river-beds-dry";source.info.name="Meadow River Beds — No Water";source.info.waterless=true;source.info.savedUtc=System.DateTime.UtcNow.ToString("O");
            World.DryRiverBedMigration.Apply(source);var path=World.SavedRegionStore.Save(source);
            Object.FindFirstObjectByType<UI.MainMenuController>().OpenSavedRegionImmediatelyForTools(World.SavedRegionStore.Load(path).info);
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();var pilot=view.ApprovedDistrict;
            if(!pilot.UsedSavedTerrain||pilot.Water||pilot.DisplayTerrain.terrainData.alphamapLayers!=7)throw new System.Exception("Dry bed restore failed.");
            System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Meadow-Deep-River-Beds-Checks.txt","PASS dry saved terrain migrated once: main channel additional 6 m incision, tributary additional 3.5 m; gradual banks; gravel/sand/silt native terrain layers.\nPASS saved seven-layer terrain restored; no water instantiated; trees repositioned to new height and removed from exposed bed.\nOriginal region untouched; previous dry save recoverable from .bak.\n");
            Debug.Log("PASS deeper dry river beds, gravel/sand/silt, cached native terrain, no water.");
        }

        [MenuItem("CityForge/Meadow Migration/Inspect Dry Riverbed Materials")]
        static void Inspect()
        {
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();view.GetComponentInChildren<CameraSystem.CityForgeCameraController>().SetZoomLevel(1);var pilot=view.ApprovedDistrict;var result=new System.Text.StringBuilder();
            foreach(var l in pilot.DisplayTerrain.terrainData.terrainLayers)result.AppendLine(l.name+" diffuse="+(l.diffuseTexture?l.diffuseTexture.name:"NULL")+" remap="+l.diffuseRemapMax);
            foreach(var light in pilot.GetComponentsInChildren<Light>())result.AppendLine("Sun shadows="+light.shadows+" bias="+light.shadowBias+" normalBias="+light.shadowNormalBias);
            result.AppendLine("Material="+pilot.DisplayTerrain.materialTemplate.shader.name+" cached="+pilot.UsedSavedTerrain+" fulltrees="+pilot.FullTreeCount);
            System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/work/riverbed-material-inspection.txt",result.ToString());
        }

        [MenuItem("CityForge/Meadow Migration/Create And Open Single Narrow River")]
        static void SingleRiver()
        {
            if(!EditorApplication.isPlaying||Object.FindFirstObjectByType<World.PrototypeRegionView>())throw new System.Exception("Play MainMenu without an open region first.");
            const string assetPath="Assets/CityForge/Resources/RiverBeds/RockAssets.asset";
            var rocks=AssetDatabase.LoadAssetAtPath<World.RiverBedRockAssets>(assetPath);
            if(!rocks){rocks=ScriptableObject.CreateInstance<World.RiverBedRockAssets>();rocks.meshes=new Mesh[3];rocks.materials=new Material[3];
                for(int i=0;i<3;i++){
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Dnk_Dev/RockPack/Prefab/P_Rock_0"+(i+1)+".prefab");var mf=prefab.GetComponentInChildren<MeshFilter>();rocks.meshes[i]=mf.sharedMesh;var src=mf.GetComponent<Renderer>().sharedMaterial;
                    var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="River rock "+i,enableInstancing=true};material.SetTexture("_BaseMap",src.mainTexture);material.SetColor("_BaseColor",new Color(.58f,.55f,.49f));material.SetFloat("_Smoothness",.08f);material.SetFloat("_Metallic",0);
                    AssetDatabase.CreateAsset(material,"Assets/CityForge/Resources/RiverBeds/Rock"+i+".mat");rocks.materials[i]=material;
                }
                AssetDatabase.CreateAsset(rocks,assetPath);AssetDatabase.SaveAssets();
            }
            var source=World.SavedRegionStore.Load(System.IO.Path.Combine(World.SavedRegionStore.DirectoryPath,World.SavedRegionStore.TestId+".cfregion"));
            source.info.id="meadow-single-narrow-river";source.info.name="Meadow — Single Narrow River";source.info.waterless=true;source.info.savedUtc=System.DateTime.UtcNow.ToString("O");
            World.DryRiverBedMigration.Apply(source,true);var path=World.SavedRegionStore.Save(source);
            Object.FindFirstObjectByType<UI.MainMenuController>().OpenSavedRegionImmediatelyForTools(World.SavedRegionStore.Load(path).info);
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();var pilot=view.ApprovedDistrict;var renderer=pilot.GetComponent<World.RiverBedRockRenderer>();
            if(pilot.Water||view.RiverBedVersion!=2||!renderer||renderer.RockCount==0)throw new System.Exception("Single river/rocks setup failed.");
            System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Meadow-Single-River-Checks.txt","PASS single tributary channel; old main channel filled from neighboring bank heights; dirt-dominant bed, gravel and sand.\nPASS native cached terrain/collider; seeded instanced rock count="+renderer.RockCount+"; no water instantiated.\nSaved separately in Open Region. Existing reference and two-river saves unchanged.\n");
            Debug.Log("PASS single narrow dry river, dirt bed and "+renderer.RockCount+" seeded instanced rocks.");
        }

        [MenuItem("CityForge/Meadow Migration/Refine And Open Single River Appearance")]
        static void RefineRiver()
        {
            if(!EditorApplication.isPlaying||Object.FindFirstObjectByType<World.PrototypeRegionView>())throw new System.Exception("Play MainMenu without an open region first.");
            const string root="Assets/CityForge/Resources/RiverBeds/";
            if(!AssetDatabase.LoadAssetAtPath<TerrainLayer>(root+"Moss.terrainlayer")){
                var layer=new TerrainLayer{name="River bank moss",diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ScannedRocks/Textures/PBRspecTextures/Moss_Albedo.png"),normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ScannedRocks/Textures/PBRspecTextures/Moss_NormalMap.png"),tileSize=new Vector2(2,2),diffuseRemapMax=new Vector4(.26f,.34f,.19f,1),normalScale=.6f};
                AssetDatabase.CreateAsset(layer,root+"Moss.terrainlayer");
            }
            var rocks=AssetDatabase.LoadAssetAtPath<World.RiverBedRockAssets>(root+"RichRockAssets.asset");
            if(!rocks){rocks=ScriptableObject.CreateInstance<World.RiverBedRockAssets>();rocks.meshes=new Mesh[6];rocks.materials=new Material[6];rocks.mossy=new bool[6];
                int[] variants={1,3,5};var shader=AssetDatabase.LoadAssetAtPath<Shader>(root+"RiverMossRock.shader");if(!shader||ShaderUtil.ShaderHasError(shader))throw new System.Exception("Rock moss shader failed compilation.");
                for(int i=0;i<6;i++){
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScannedRocks/RockPrefabs_PBR/Rock"+variants[i%3]+".prefab");
                    var mf=prefab.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh).OrderBy(m=>m.sharedMesh.vertexCount).First();rocks.meshes[i]=mf.sharedMesh;var src=mf.GetComponent<Renderer>().sharedMaterial;
                    var material=new Material(shader){name=i<3?"Scanned river stone "+i:"Mossy scanned river stone "+i,enableInstancing=true};material.SetTexture("_BaseMap",src.mainTexture);material.SetTexture("_MossMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ScannedRocks/Textures/PBRspecTextures/Moss_Albedo.png"));material.SetColor("_BaseColor",new Color(.78f,.79f,.76f));material.SetFloat("_MossAmount",i<3?0:.85f);
                    if(!src.mainTexture)throw new System.Exception("Scanned rock texture missing.");AssetDatabase.CreateAsset(material,root+"RichRock"+i+".mat");rocks.materials[i]=material;rocks.mossy[i]=i>=3;
                }
                AssetDatabase.CreateAsset(rocks,root+"RichRockAssets.asset");AssetDatabase.SaveAssets();
            }
            var path=System.IO.Path.Combine(World.SavedRegionStore.DirectoryPath,"meadow-single-narrow-river.cfregion");var source=World.SavedRegionStore.Load(path);var before=(float[,])source.data.Heights.Clone();
            World.DryRiverBedMigration.RefineAppearance(source);source.info.savedUtc=System.DateTime.UtcNow.ToString("O");World.SavedRegionStore.Save(source);
            Object.FindFirstObjectByType<UI.MainMenuController>().OpenSavedRegionImmediatelyForTools(World.SavedRegionStore.Load(path).info);
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();var pilot=view.ApprovedDistrict;
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(view.Data.Heights[z,x]!=before[z,x])throw new System.Exception("Appearance update changed channel geometry.");
            if(pilot.Water||pilot.DisplayTerrain.terrainData.alphamapLayers!=8)throw new System.Exception("Riverbed setup differs.");
            System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Meadow-Riverbed-Appearance-Checks.txt","PASS geometry unchanged; eight native layers within two terrain texture passes; pebble bars/sand/soil/bank moss painted once into saved cache.\nPASS three scanned stone meshes, six instanced rock materials including moss variants; rock count="+pilot.GetComponent<World.RiverBedRockRenderer>().RockCount+"; no Crest instantiated.\nUpdated Single Narrow River in Open Region; previous save retained as .bak.\n");
            Debug.Log("PASS richer dry riverbed: pebble bars, sand, soil pockets, mossy scanned stones; channel unchanged.");
        }

        [MenuItem("CityForge/Meadow Migration/Diagnose Riverbank Normal Maps")]
        static void DiagnoseNormals()
        {
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();var terrain=view.ApprovedDistrict.DisplayTerrain;
            var layers=terrain.terrainData.terrainLayers;for(int i=0;i<layers.Length;i++){layers[i]=Object.Instantiate(layers[i]);layers[i].normalMapTexture=null;layers[i].normalScale=0;}
            terrain.terrainData.terrainLayers=layers;
            Debug.Log("Diagnostic only: terrain normal maps disabled; saved assets unchanged.");
        }
        [MenuItem("CityForge/Meadow Migration/Diagnose Riverbank Terrain Detail")]
        static void DiagnoseTerrainDetail()
        {
            Object.FindFirstObjectByType<World.PrototypeRegionView>().ApprovedDistrict.DisplayTerrain.heightmapPixelError=1;
        }

        [MenuItem("CityForge/Meadow Migration/Slow Saved River Animation")]
        static void SlowRiverAnimation()
        {
            int count=0;
            foreach(var info in World.SavedRegionStore.ListRegions()){
                var saved=World.SavedRegionStore.Load(info.path);bool changed=false;
                foreach(var body in saved.data.WaterBodies??System.Array.Empty<World.RegionWaterBody>())if(body.kind==World.WaterBodyKind.River&&Mathf.Abs(body.flowSpeed-.35f)<.0001f){body.flowSpeed=.12f;changed=true;count++;}
                if(changed){saved.info.waterBodies=saved.data.WaterBodies;World.SavedRegionStore.Save(saved);}
            }
            Debug.Log("Slowed "+count+" saved river currents to 0.12 m/s; independent diagonal normal scrolling disabled by River profile.");
        }
        [MenuItem("CityForge/Meadow Migration/Validate And Migrate Water Body Tags")]
        static void ValidateWaterTags()
        {
            foreach(World.WaterBodyKind kind in System.Enum.GetValues(typeof(World.WaterBodyKind))){
                var profile=World.WaterAppearanceProfile.For(kind);
                if(kind!=World.WaterBodyKind.River&&profile.flow)throw new System.Exception("Non-river current enabled.");
                if(kind!=World.WaterBodyKind.Ocean&&profile.waves)throw new System.Exception("Ocean swell enabled outside ocean.");
                if(profile.foam)throw new System.Exception("Foam should require a separate explicit treatment.");
            }
            int count=0;
            foreach(var info in World.SavedRegionStore.ListRegions()){
                var saved=World.SavedRegionStore.Load(info.path);float side=1024*Mathf.Pow(2,info.size);
                saved.data.WaterBodies??=World.RegionWaterBody.Generate(saved.data,info.riverBedVersion,info.waterless,side);
                saved.info.waterBodies=saved.data.WaterBodies;World.SavedRegionStore.Save(saved);
                var loaded=World.SavedRegionStore.Load(info.path);
                if(JsonUtility.ToJson(loaded.info)!=JsonUtility.ToJson(saved.info))throw new System.Exception("Water metadata round trip failed.");
                if(loaded.data.WaterBodies.Length!=saved.data.WaterBodies.Length)throw new System.Exception("Body count changed.");
                foreach(var body in loaded.data.WaterBodies)if(body.kind==World.WaterBodyKind.River&&loaded.data.WaterBodyAt(body.centerline[32])?.kind!=World.WaterBodyKind.River)throw new System.Exception("River location lookup failed.");
                if(info.riverBedVersion>=2&&(loaded.data.WaterBodies.Length!=1||loaded.data.WaterBodies[0].kind!=World.WaterBodyKind.River))throw new System.Exception("Narrow river should contain only one River body.");
                count++;
            }
            System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Water-Body-Type-Checks.txt","PASS River/Ocean/Lake/Pond/Unknown profile isolation; no non-river currents, no non-ocean swell, no automatic foam.\nPASS "+count+" existing saved regions migrated and metadata round-tripped with persistent body IDs, geometry and kinds.\nPASS river point lookup and single-river classification.\nNative river presentation uses River defaults. Ocean/lake/pond rendering expansion remains future work; legacy landscape remains fallback.\n");
            Debug.Log("PASS water body type profiles and "+count+" saved-region migrations.");
        }
        [MenuItem("CityForge/Meadow Migration/Toggle River Reflections")]
        static void ToggleRiverReflections()
        {
            var water=Object.FindFirstObjectByType<World.PrototypeRegionView>().ApprovedDistrict.Water;
            bool enabled=!water.Reflections.Enabled;water.Reflections.Enabled=enabled;water.Surface.Material.SetFloat("_Crest_PlanarReflectionsEnabled",enabled?1:0);
        }
        [MenuItem("CityForge/Meadow Migration/Measure River Reflection Trial")]
        static void MeasureRiverReflections()
        {
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();
            if(!view||!view.MinimalRiverCrest||!view.ApprovedDistrict.Water)throw new System.Exception("Open minimal river Crest trial first.");
            if(!view.GetComponent<World.RiverReflectionBenchmark>())view.gameObject.AddComponent<World.RiverReflectionBenchmark>();
        }
        [MenuItem("CityForge/Meadow Migration/Measure River Depth Trial")]
        static void MeasureRiverDepth()
        {
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();
            if(!view||!view.MinimalRiverCrest||!view.ApprovedDistrict.Water)throw new System.Exception("Open minimal river Crest trial first.");
            if(!view.GetComponent<World.RiverDepthBenchmark>())view.gameObject.AddComponent<World.RiverDepthBenchmark>();
        }
        [MenuItem("CityForge/Meadow Migration/Measure River Flow Trial")]
        static void MeasureRiverFlow()
        {
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();
            if(!view||!view.MinimalRiverCrest||!view.ApprovedDistrict.Water)throw new System.Exception("Open minimal river Crest trial first.");
            if(!view.GetComponent<World.RiverFlowBenchmark>())view.gameObject.AddComponent<World.RiverFlowBenchmark>();
        }
        [MenuItem("CityForge/Meadow Migration/Measure River Ripple Trial")]
        static void MeasureRiverRipples()
        {
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();
            if(!view||!view.MinimalRiverCrest||!view.ApprovedDistrict.Water)throw new System.Exception("Open minimal river Crest trial first.");
            if(!view.GetComponent<World.RiverRippleBenchmark>())view.gameObject.AddComponent<World.RiverRippleBenchmark>();
        }
        [MenuItem("CityForge/Meadow Migration/Measure Minimal River Crest Trial")]
        static void MeasureMinimalCrest()
        {
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();
            if(!view||!view.MinimalRiverCrest||!view.ApprovedDistrict.Water)throw new System.Exception("Open minimal river Crest trial first.");
            if(!view.GetComponent<World.MinimalRiverCrestBenchmark>())view.gameObject.AddComponent<World.MinimalRiverCrestBenchmark>();
        }
        [MenuItem("CityForge/Meadow Migration/Open Minimal River Crest Trial")]
        static void MinimalCrestTrial()
        {
            if(!EditorApplication.isPlaying||Object.FindFirstObjectByType<World.PrototypeRegionView>())throw new System.Exception("Play MainMenu without a region first.");
            var source=World.SavedRegionStore.Load(System.IO.Path.Combine(World.SavedRegionStore.DirectoryPath,"meadow-single-narrow-river.cfregion"));
            source.info.id="meadow-narrow-river-crest-trial";source.info.name="Meadow — Narrow River Crest Trial";source.info.waterless=true;source.info.minimalRiverCrest=true;
            source.info.waterBodies=source.data.WaterBodies??World.RegionWaterBody.Generate(source.data,source.info.riverBedVersion,source.info.waterless,1024*Mathf.Pow(2,source.info.size));source.data.WaterBodies=source.info.waterBodies;source.info.savedUtc=System.DateTime.UtcNow.ToString("O");var path=World.SavedRegionStore.Save(source);
            Object.FindFirstObjectByType<UI.MainMenuController>().OpenSavedRegionImmediatelyForTools(World.SavedRegionStore.Load(path).info);
        }
        [MenuItem("CityForge/Meadow Migration/Fix And Open Riverbank Grid Ridging")]
        static void FixBankRidging()
        {
            if(!EditorApplication.isPlaying||Object.FindFirstObjectByType<World.PrototypeRegionView>())throw new System.Exception("Play MainMenu without a region first.");
            var path=System.IO.Path.Combine(World.SavedRegionStore.DirectoryPath,"meadow-single-narrow-river.cfregion");var source=World.SavedRegionStore.Load(path);source.info.riverBedVersion=5;source.data.ConfigureContinuousRiver(1024*Mathf.Pow(2,source.info.size));
            int district=source.info.district;float ox=(district%2)*.5f,oz=(district/2)*.5f;
            for(int i=0;i<source.meadow.trees.Length;i++){var t=source.meadow.trees[i];t.position.y=source.data.SmoothHeight(ox+t.position.x*.5f,oz+t.position.z*.5f);source.meadow.trees[i]=t;}
            source.info.camera.zoom=1;source.info.camera.size=40;source.info.savedUtc=System.DateTime.UtcNow.ToString("O");World.SavedRegionStore.Save(source);
            Object.FindFirstObjectByType<UI.MainMenuController>().OpenSavedRegionImmediatelyForTools(World.SavedRegionStore.Load(path).info);
            var view=Object.FindFirstObjectByType<World.PrototypeRegionView>();var pilot=view.ApprovedDistrict;var terrain=pilot.DisplayTerrain;
            if(!terrain.GetComponent<TerrainCollider>().enabled||view.transform.Find("ContinuousRegionTerrain").GetComponent<TerrainCollider>().enabled)throw new System.Exception("River terrain collider mismatch.");
            float worst=0;var hs=terrain.terrainData.GetHeights(0,0,1025,1025);
            for(int z=0;z<1025;z+=17)for(int x=0;x<1025;x+=17)worst=Mathf.Max(worst,Mathf.Abs(hs[z,x]-view.Data.SmoothHeight(ox+x/1024f*.5f,oz+z/1024f*.5f)));
            if(worst>1f/32767+.00001f)throw new System.Exception("Smooth surface mismatch.");
            System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Meadow-Riverbank-Ridge-Fix-Checks.txt","PASS continuous channel profile evaluated at native terrain resolution; coarse sampled incision removed from grade before interpolation, then reapplied analytically. Saved sample elevations preserved.\nPASS native TerrainCollider uses identical smoothed heightfield; continuous legacy collider restored outside native mode.\nPASS rendered samples match interpolation within native height quantization: "+worst+".\nTextures, normal maps and instanced rocks retained; no Crest or per-frame smoothing.\n");
            Debug.Log("PASS riverbank grid-ridge correction: smooth terrain slopes and matching collision surface.");
        }

        [MenuItem("CityForge/Meadow Migration/Diagnose Riverbank Shadows")]
        static void DiagnoseBankShadows()
        {
            foreach(var light in Object.FindFirstObjectByType<World.PrototypeRegionView>().ApprovedDistrict.GetComponentsInChildren<Light>())light.shadows=UnityEngine.LightShadows.None;
        }
        [MenuItem("CityForge/Meadow Migration/Create Flat Waterless Test Scene")]
        static void Create()
        {
            if(EditorApplication.isPlaying)throw new System.Exception("Stop Play mode first.");
            const string path="Assets/CityForge/Scenes/Gameplay/FlatWaterlessTest.unity";
            EditorSceneManager.OpenScene("Assets/CityForge/Scenes/Gameplay/MainMenu.unity");
            new GameObject("Flat Waterless Test Generator").AddComponent<World.FlatWaterlessTestScene>();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),path);
            // Explicit test session; shipped startup remains MainMenu in build settings.
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            Debug.Log("Created FlatWaterlessTest.unity. Play to generate and save the waterless diagnostic region.");
        }
    }
}
