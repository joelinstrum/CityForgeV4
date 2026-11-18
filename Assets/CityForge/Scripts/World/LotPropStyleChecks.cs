#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForge.Lots;
using CityForge.CameraSystem;
using CityForge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CityForge.Editor
{
    public static class LotPropStyleChecks
    {
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        private static LotDefinition Layout(LotWealth wealth) => new LotDefinition
        {
            type = LotType.Zoning, width = 6, length = 6, category = "residential", firstEraId = "founders",
            zoningLayout = new LotZoningLayout { zone = World.TownZone.Residential1, wealth = wealth, buildingSize = new Vector2(6,6) },
            objects = new List<LotObjectDefinition>
            {
                new LotObjectDefinition { assetId = wealth == LotWealth.High ? "beige-stone-wrought-iron-section" : wealth == LotWealth.Medium ? "flowered-picket-section" : "weathered-picket-section", position = new Vector2(-2,5), fenceLength = 2.4f },
                new LotObjectDefinition { assetId = wealth == LotWealth.High ? "beige-stone-wrought-iron-partial" : wealth == LotWealth.Medium ? "flowered-picket-partial" : "weathered-picket-partial", position = new Vector2(-.2f,5), fenceLength = 1.2f, fenceEndPost = true }
            },
            paths = new List<LotPathDefinition>
            {
                new LotPathDefinition { points = new[]{new Vector2(-10,-9),new Vector2(10,-9)}, width = 1, materialId = LotPathMaterial.Plankboard },
                new LotPathDefinition { points = new[]{new Vector2(0,-9),new Vector2(0,0)}, width = 1, materialId = LotPathMaterial.ResidentialFootpath, generatedResidentialEntry = true },
                new LotPathDefinition { points = new[]{new Vector2(-9,-8),new Vector2(-9,0)}, width = 3, materialId = "timber-deck", use = LotPathUse.Vehicle }
            }
        };
        public static void Run()
        {
            EditorApplication.ExecuteMenuItem("CityForge/Lots/Prepare local catalog");
            var catalog = LotAssetCatalog.Load(); Require(catalog, "No local catalog");
            var root = new GameObject("Disposable fence normalization checks");
            try
            {
                var lodProblems = new List<string>();
                foreach (var style in LotPropStyles.Fences)
                {
                    var asset = catalog.Find(style.assetId);
                    Require(asset != null && asset.prefab, "Missing classified fence " + style.assetId);
                    if (!style.Straight) continue;
                    Require(asset.fenceSpan > 0 && Mathf.Abs(asset.FencePlacementLength - style.Length) < .001f, "Fence span missing " + style.assetId);
                    var rigObject = new GameObject("Fence LOD camera"); rigObject.transform.SetParent(root.transform);
                    var rig = rigObject.AddComponent<CityForgeCameraController>();
                    var item = new LotObjectDefinition { assetId = asset.id, fenceLength = asset.FencePlacementLength, fenceEndPost = true };
                    var instance = LotRuntimeLoader.InstantiateObject(item, asset, root.transform, World.RegionSeason.Summer, rig);
                    rig.SetZoomLevel(2);
                    // Exclude separate posts and force the nearest tier for native LOD prefabs.
                    var model = instance.transform.Find("Fence span").GetChild(0);
                    var group = model.GetComponentInChildren<LODGroup>();
                    var renderers = group ? group.GetLODs()[0].renderers : model.GetComponentsInChildren<Renderer>();
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    Require(Mathf.Abs(bounds.size.x - style.Length) < .025f, $"Rendered {style.assetId} length {bounds.size.x} != {style.Length}");
                    Require(Mathf.Abs(bounds.center.x) < .025f && Mathf.Abs(bounds.center.z) < .025f, "Off-center fence " + style.assetId);
                    if (asset.distantPrefab)
                    {
                        var detail = instance.GetComponent<World.DistrictBuildingDetail>();
                        Require(detail, "Missing shared distant detail " + asset.id);
                        var distant = (Renderer[])typeof(World.DistrictBuildingDetail).GetField("distantRenderers", BindingFlags.Instance|BindingFlags.NonPublic).GetValue(detail);
                        rig.SetZoomLevel(3);
                        var farBounds = distant[0].bounds;
                        foreach (var renderer in distant) { farBounds.Encapsulate(renderer.bounds); Require(renderer.enabled,"Far tier hidden"); }
                        if (Mathf.Abs(farBounds.size.x-style.Length)>=.04f || Mathf.Abs(farBounds.center.x)>=.04f || Mathf.Abs(farBounds.center.z)>=.04f)
                            lodProblems.Add(asset.id + " near=" + bounds + " far=" + farBounds);
                        rig.SetZoomLevel(2); Require(renderers.All(renderer=>renderer.enabled), "Near tier did not return");
                    }
                    Object.DestroyImmediate(instance); Object.DestroyImmediate(rigObject);
                    Require(LotFenceLayout.TryBuild(new[]{Vector2.zero,Vector2.right*12},false,asset,512,null,out var pieces,out _),"Path fill failed");
                    Require(pieces.Count == (style.part == "Partial" ? 10 : 5), "Path uses source-specific section length");
                    Require(Mathf.Abs(pieces.Sum(piece => piece.fenceLength) - 12) < .001f, "Path end moved");
                }
                Require(lodProblems.Count == 0, "Distant alignment: " + string.Join("; ", lodProblems));
                foreach (var wealth in new[]{LotWealth.Low,LotWealth.Medium,LotWealth.High})
                {
                    var seen = new HashSet<string>(); var surfaces = new HashSet<string>();
                    for (int n=0;n<40;n++)
                    {
                        var lot = Layout(wealth); string original = JsonUtility.ToJson(lot);
                        var grown = LotDefinition.Parse(original); LotPropStyles.Apply(grown,"build-"+n,catalog);
                        Require(JsonUtility.ToJson(lot)==original,"Source layout mutated");
                        var family = LotPropStyles.Find(grown.objects[0].assetId);
                        Require(family.wealth==wealth && LotPropStyles.Find(grown.objects[1].assetId).family==family.family,"Wealth/family mismatch");
                        seen.Add(family.family); surfaces.Add(grown.paths[0].materialId);
                        for(int i=0;i<2;i++) Require(grown.objects[i].id==lot.objects[i].id && grown.objects[i].position==lot.objects[i].position &&
                            grown.objects[i].fenceLength==lot.objects[i].fenceLength && grown.objects[i].scale==lot.objects[i].scale,"Swap changed endpoints/identity");
                        Require(grown.objects[1].fenceEndPost && !grown.objects[0].fenceEndPost,"End-post roles changed");
                        Require(LotPropStyles.Find(grown.objects[1].assetId).part=="Partial","Partial replaced by section");
                        Require(grown.paths[1].materialId==LotPathMaterial.ResidentialFootpath && grown.paths[2].materialId=="timber-deck","Entry/vehicle path replaced");
                        var reopened = LotDefinition.Parse(JsonUtility.ToJson(grown));
                        Require(JsonUtility.ToJson(reopened)==JsonUtility.ToJson(grown),"Save/reopen rerolled finishes");
                        var again=LotDefinition.Parse(original);LotPropStyles.Apply(again,"build-"+n,catalog);
                        Require(JsonUtility.ToJson(again)==JsonUtility.ToJson(grown),"Same seed chose different finishes");
                    }
                    Require((wealth != LotWealth.Low || seen.Count>1) && surfaces.Count>1,"No eligible per-build variety for "+wealth);
                }
                CheckWholeSets(catalog);
                CheckPathBuilds(catalog);
                CheckTextureWealth();
                var unsupported=Layout(LotWealth.High);unsupported.firstEraId="victorian";
                string unsupportedJson=JsonUtility.ToJson(unsupported);LotPropStyles.Apply(unsupported,"unsupported",catalog);
                Require(JsonUtility.ToJson(unsupported)==unsupportedJson,"Unsupported era silently borrowed Founders styles");
                var ordinary=Layout(LotWealth.High);ordinary.type=LotType.Playable;
                string ordinaryJson=JsonUtility.ToJson(ordinary);LotPropStyles.Apply(ordinary,"ordinary",catalog);
                Require(JsonUtility.ToJson(ordinary)==ordinaryJson,"Non-growable default changed");
                var legacy=Layout(LotWealth.Low);legacy.objects[0].fenceLength=3.73f;legacy.objects[0].yaw=45;
                LotPropStyles.Apply(legacy,"legacy",catalog);Require(legacy.objects[0].fenceLength==3.73f&&legacy.objects[0].yaw==45,"Legacy span changed");
                CheckSafety(root);
                CheckUi();
                Debug.Log("LOT PROP STYLE CHECKS PASS: all classified sections/partials measured at 2.4/1.2m, centered models, uniform path fills, per-wealth coherent families, per-build variety, stable saves and IDs, legacy lengths, mandatory growable-only behavior, strict shape/wealth matching, whole-set fallback and path exclusions.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void CheckTextureWealth()
        {
            foreach(var id in new[]{"packed-earth","residential-footpath","rich-brown-garden-soil","tilled-garden-soil","garden-soil","gravel","compacted-gravel-pebble"})
                Require(LotPropStyles.FindPath(id).wealth==LotWealth.Low,"Dirt/natural path is not low wealth: "+id);
            foreach(var id in new[]{"weathered-plankboard","damaged-brick","timber-deck","brick","cobblestone","weathered-bluestone","concrete"})
                Require(LotPropStyles.FindPath(id).wealth==LotWealth.Medium,"Plain path is not medium wealth: "+id);
            foreach(var id in new[]{"weathered-red-brick-herringbone","english-garden-flagstones","english-garden-brick-medallion","gray-diamond-stonework","interlocking-brick-and-stone","weathered-brick-and-stone-mosaic"})
                Require(LotPropStyles.FindPath(id).wealth==LotWealth.High && LotPropStyles.FindGround(true,id).wealth==LotWealth.High,"Fancy texture wealth mismatch across pickers: "+id);
            foreach(bool overlay in new[]{false,true})foreach(var texture in overlay?LotGroundTexture.Overlay:LotGroundTexture.Base)
                Require(texture.Id.Length==0 || LotPropStyles.FindGround(overlay,texture.Id)!=null,"Unclassified ground texture: "+texture.Id);
            Require(new[]{LotWealth.Low,LotWealth.Medium,LotWealth.High}.All(w=>LotPropStyles.FindPath("compacted-gravel-pebble").AllowsWealth(w)),"Compacted pebble missing shared wealth");
            Require(LotGroundTexture.Find(false,"packed-earth").Wealth(false)==LotWealth.Low,"Ground dirt is not low wealth");
            Debug.Log("TEXTURE WEALTH CHECKS PASS: all path/base/overlay textures classified; fancy high, plain medium, dirt low; shared texture classifications agree across pickers.");
        }
        private static void CheckPathBuilds(LotAssetCatalog catalog)
        {
            Require(LotPathMaterial.All.All(material => LotPropStyles.FindPath(material.Id) != null),"Unclassified path material");
            foreach (var source in LotPropStyles.Paths)
            {
                var seen=new HashSet<string>();
                for(int n=0;n<24;n++)
                {
                    var authored=Layout(LotWealth.High);authored.firstEraId=source.era;
                    authored.paths.Clear();
                    for(int i=0;i<2;i++)authored.paths.Add(new LotPathDefinition
                    {
                        materialId=source.materialId,points=new[]{new Vector2(-4,i*2),new Vector2(4,i*2)},width=3,
                        use=source.family=="Road"?LotPathUse.Vehicle:LotPathUse.Pedestrian,
                        curved=true,edgeBlend=25,textureRotation=1,depth=LotPathDepth.HalfFoot,border=LotPathBorder.Curb
                    });
                    authored.paths.Add(new LotPathDefinition{materialId=LotPathMaterial.ResidentialFootpath,points=new[]{Vector2.zero,Vector2.up*3},width=1,generatedResidentialEntry=true});
                    var poolWealth=source.SelectionWealth(authored);
                    var mate=LotPropStyles.Paths.FirstOrDefault(style=>style.materialId!=source.materialId&&style.SelectionWealth(authored)==poolWealth&&style.family==source.family&&style.era==source.era);
                    if(mate!=null)authored.paths.Add(new LotPathDefinition{materialId=mate.materialId,points=new[]{new Vector2(-4,6),new Vector2(4,6)},width=1});
                    string original=JsonUtility.ToJson(authored);
                    var grown=LotDefinition.Parse(original);LotPropStyles.Apply(grown,"path-build-"+n,catalog);
                    Require(JsonUtility.ToJson(authored)==original,"Path choice mutated editor source");
                    Require(grown.paths[0].materialId==grown.paths[1].materialId,"Same texture replaced inconsistently");
                    if(mate!=null)Require(grown.paths[3].materialId==grown.paths[0].materialId,"Compatible path pool produced mixed finishes");
                    var chosen=LotPropStyles.FindPath(grown.paths[0].materialId);
                    Require(chosen.AllowsWealth(poolWealth)&&chosen.family==source.family&&chosen.era==source.era,"Path replacement changed wealth/surface role/era");
                    seen.Add(chosen.materialId);
                    for(int i=0;i<2;i++)
                    {
                        var geometry=JsonUtility.FromJson<LotPathDefinition>(JsonUtility.ToJson(grown.paths[i]));geometry.materialId=source.materialId;
                        Require(JsonUtility.ToJson(geometry)==JsonUtility.ToJson(authored.paths[i]),"Path substitution changed geometry or use");
                    }
                    Require(grown.paths[2].materialId==LotPathMaterial.ResidentialFootpath,"Generated entrance path changed");
                    string built=JsonUtility.ToJson(grown);Require(JsonUtility.ToJson(LotDefinition.Parse(built))==built,"Path changed when save reopened");
                    var repeat=LotDefinition.Parse(original);LotPropStyles.Apply(repeat,"path-build-"+n,catalog);
                    Require(JsonUtility.ToJson(repeat)==built,"Build seed did not reproduce path choice");
                    var manual=LotDefinition.Parse(original);manual.type=LotType.Playable;string before=JsonUtility.ToJson(manual);
                    LotPropStyles.Apply(manual,"path-build-"+n,catalog);Require(JsonUtility.ToJson(manual)==before,"Manual lot path varied");
                }
                int eligible=LotPropStyles.Paths.Count(p=>p.family==source.family&&p.AllowsWealth(source.AllowsWealth(LotWealth.High)?LotWealth.High:source.wealth)&&p.era==source.era&&LotPathMaterial.Find(p.materialId)?.IsAvailable==true);
                Require(eligible<2||seen.Count>1,"Path pool did not vary across builds: "+source.materialId);
            }
            Debug.Log("PATH BUILD VARIETY CHECKS PASS: every path material classified, same-wealth/surface/era replacements, consistent repeated textures, build-only choice, saved results, geometry preservation, entrance exclusions, manual-lot exemption and per-build variety.");
        }
        private static void CheckWholeSets(LotAssetCatalog catalog)
        {
            var families=new HashSet<string>();
            for(int n=0;n<60;n++)
            {
                var low=Layout(LotWealth.High);
                low.objects[0].assetId="weathered-picket-section";low.objects[1].assetId="weathered-picket-partial";
                // Older optional settings are ignored, even when explicitly disabled.
                string json=JsonUtility.ToJson(low);
                low=LotDefinition.Parse(json.Insert(1,"\"appearance\":{\"configured\":true,\"varyFences\":false,\"varySidewalks\":false},"));
                LotPropStyles.Apply(low,"mandatory-"+n,catalog);
                Require(low.objects.All(o=>LotPropStyles.Find(o.assetId).wealth==LotWealth.Low),"Low fence upgraded to lot wealth");
                Require(low.objects.All(o=>LotPropStyles.Find(o.assetId).family==LotPropStyles.Find(low.objects[0].assetId).family),"Mixed family result");
                families.Add(LotPropStyles.Find(low.objects[0].assetId).family);
            }
            Require(families.Count==2,"Disabled legacy setting prevented automatic complete-family selection");
            var limited=ScriptableObject.CreateInstance<LotAssetCatalog>();
            try
            {
                limited.entries=catalog.entries.Where(a=>LotPropStyles.Find(a.id)?.part!="Partial" || a.id=="weathered-picket-partial").ToArray();
                for(int n=0;n<20;n++)
                {
                    var lot=Layout(LotWealth.Low);LotPropStyles.Apply(lot,"missing-partial-"+n,limited);
                    Require(lot.objects[0].assetId=="weathered-picket-section"&&lot.objects[1].assetId=="weathered-picket-partial","Incomplete family partially replaced fence set");
                }
                var gate=Layout(LotWealth.High);gate.objects.Add(new LotObjectDefinition{assetId="v3-ornate-iron-gate",position=new Vector2(6,5)});
                string before=string.Join(",",gate.objects.Select(o=>o.assetId));LotPropStyles.Apply(gate,"fixed-gate",catalog);
                Require(before==string.Join(",",gate.objects.Select(o=>o.assetId)),"Gate without like replacement left mixed fences");
                var corner=Layout(LotWealth.High);corner.objects.Add(new LotObjectDefinition{assetId="v3-wrought-iron-corner",position=new Vector2(6,5)});
                before=string.Join(",",corner.objects.Select(o=>o.assetId));LotPropStyles.Apply(corner,"fixed-corner",catalog);
                Require(before==string.Join(",",corner.objects.Select(o=>o.assetId)),"Corner without complete family left mixed fences");
                var mixed=Layout(LotWealth.Low);mixed.objects[1].assetId="flowered-picket-partial";
                before=string.Join(",",mixed.objects.Select(o=>o.assetId));LotPropStyles.Apply(mixed,"mixed-wealth",catalog);
                Require(before==string.Join(",",mixed.objects.Select(o=>o.assetId)),"Mixed source wealth silently changed");
                foreach(var wealth in new[]{LotWealth.Low,LotWealth.Medium,LotWealth.High})
                {
                    var seen=new HashSet<string>();
                    for(int n=0;n<40;n++){var lot=Layout(wealth);lot.objects.RemoveAt(1);LotPropStyles.Apply(lot,"sections-"+n,catalog);seen.Add(lot.objects[0].assetId);}
                    Require(seen.Count>1,"Full-section-only lot failed to vary "+wealth);
                }
            }
            finally{Object.DestroyImmediate(limited);}
        }
        public static void Regressions()
        {
            Run();
            ExtractionCaptureChecks.Run();
        }
        private static void CheckSafety(GameObject root)
        {
            var catalog=ScriptableObject.CreateInstance<LotAssetCatalog>();
            try
            {
                catalog.entries=new[]
                {
                    new LotAssetCatalogEntry{id="weathered-picket-section",category="Fences",prefab=root,footprint=new Vector2(2.4f,.1f),fenceSpan=2.4f,fenceDepth=.1f},
                    new LotAssetCatalogEntry{id="wood-fence-section",category="Fences",prefab=root,footprint=new Vector2(2.4f,1.2f),fenceSpan=2.4f,fenceDepth=1.2f},
                    new LotAssetCatalogEntry{id="obstacle",category="Buildings",prefab=root,footprint=Vector2.one}
                };
                var nearBuilding=Layout(LotWealth.Low);nearBuilding.objects.RemoveAt(1);nearBuilding.objects[0].position=new Vector2(0,5);
                nearBuilding.objects.Add(new LotObjectDefinition{assetId="obstacle",position=new Vector2(0,5.7f)});
                LotPropStyles.Apply(nearBuilding,"safety",catalog);
                Require(nearBuilding.objects[0].assetId=="weathered-picket-section","Thicker replacement overlaps building");
                var parallel=Layout(LotWealth.Low);parallel.objects[0].position=new Vector2(0,5);
                parallel.objects[1].assetId="weathered-picket-section";parallel.objects[1].fenceLength=2.4f;parallel.objects[1].position=new Vector2(0,5.5f);
                LotPropStyles.Apply(parallel,"parallel",catalog);Require(parallel.objects.All(o=>o.assetId=="weathered-picket-section"),"Parallel fences gained overlap");
                var legacy=Layout(LotWealth.Low);legacy.objects.RemoveAt(1);legacy.objects[0].fenceLength=0;
                catalog.entries[0].fenceAlongZ=true;catalog.entries[0].footprint=new Vector2(.1f,2.4f);
                string seed = Enumerable.Range(0,100).Select(n=>"legacy-axis-"+n).First(id=>LotPropStyles.Hash(id+":fence")%2==0);
                LotPropStyles.Apply(legacy,seed,catalog);
                Require(legacy.objects[0].assetId=="wood-fence-section"&&legacy.objects[0].yaw==90&&legacy.objects[0].fenceLength==2.4f,"Legacy standalone changed axis/length");
            }
            finally{Object.DestroyImmediate(catalog);}
        }
        private static void CheckUi()
        {
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var back=LotEditorWorkspace.Create(font,()=>{});var canvas=back.GetComponentInParent<Canvas>();var panel=canvas.GetComponent<LotEditorPanel>();
            void Invoke(string name,params object[] args)=>typeof(LotEditorPanel).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(panel,args);
            try
            {
                Invoke("OpenLot",Layout(LotWealth.High));
                Require(typeof(LotEditorPanel).GetMethod("OpenLotAppearance",BindingFlags.Instance|BindingFlags.NonPublic)==null,"Optional build controls remain");
                Invoke("OpenAssetPopup","Fences","Choose fence");
                var dropdown=canvas.GetComponentsInChildren<Dropdown>().First(d=>d.name=="Fence wealth");dropdown.value=3;
                var panelField=typeof(LotEditorPanel).GetField("assetList",BindingFlags.Instance|BindingFlags.NonPublic);
                var list=(RectTransform)panelField.GetValue(panel);
                Require(list.GetComponentsInChildren<Button>().All(button=>!button.gameObject.activeSelf || !button.name.Contains("Rustic")),"Fence wealth filter included low wealth");
                Invoke("CloseAssetPopup");Invoke("OpenGroundTexturePopup");
                var textureFilter=canvas.GetComponentsInChildren<Dropdown>().First(d=>d.name=="Texture wealth");textureFilter.value=1;
                list=(RectTransform)panelField.GetValue(panel);
                Require(list.GetComponentsInChildren<Button>().Any(b=>b.name=="Packed Earth"),"Dirt filter omitted packed earth");
                Require(!list.GetComponentsInChildren<Button>().Any(b=>b.name=="Lush Grass"),"Dirt filter included plain grass");
                textureFilter.value=3;canvas.GetComponentsInChildren<Button>().First(b=>b.name=="Overlay Textures").onClick.Invoke();
                list=(RectTransform)panelField.GetValue(panel);
                Require(list.GetComponentsInChildren<Button>().Any(b=>b.name=="Weathered Red Brick Herringbone"),"Fancy filter omitted herringbone");
                Require(!list.GetComponentsInChildren<Button>().Any(b=>b.name=="Brick Walkway"),"Fancy filter included plain brick");
            }
            finally{Object.DestroyImmediate(canvas.gameObject);}
        }
        public static void RenderUi()
        {
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var title=AssetDatabase.LoadAssetAtPath<Font>("Assets/CityForge/Fonts/Cinzel-Regular.ttf");
            const string chars="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 —?.,/:()$·&±…";
            foreach(var face in new[]{font,title})if(face)foreach(int size in new[]{14,15,16,17,18,19,20,21,24,30})face.RequestCharactersInTexture(chars,size);
            var back=LotEditorWorkspace.Create(title?title:font,()=>{});var canvas=back.GetComponentInParent<Canvas>();var panel=canvas.GetComponent<LotEditorPanel>();
            var camera=new GameObject("UI capture camera").AddComponent<Camera>();var target=new RenderTexture(1672,941,24);var previous=RenderTexture.active;
            void Invoke(string name,params object[] args)=>typeof(LotEditorPanel).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(panel,args);
            try
            {
                Invoke("OpenLot",Layout(LotWealth.High));
                camera.orthographic=true;camera.orthographicSize=10;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.GetComponent<CanvasScaler>().enabled=false;canvas.scaleFactor=1;
                Directory.CreateDirectory("Library/LotPropStylePreview");
                foreach(string page in new[]{"Fence-Categories"})
                {
                    Invoke("OpenAssetPopup","Fences","Fence styles by wealth");canvas.GetComponentsInChildren<Dropdown>().First(d=>d.name=="Fence wealth").value=3;
                    for(int settle=0;settle<3;settle++){Canvas.ForceUpdateCanvases();camera.Render();}
                    RenderTexture.active=target;var pixels=new Texture2D(1672,941,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1672,941),0,0);pixels.Apply();File.WriteAllBytes("Library/LotPropStylePreview/"+page+".png",pixels.EncodeToPNG());Object.DestroyImmediate(pixels);
                }
                Debug.Log("LOT PROP UI PREVIEWS rendered wealth-filtered fence catalog.");
            }
            finally{RenderTexture.active=previous;Object.DestroyImmediate(canvas.gameObject);Object.DestroyImmediate(camera.gameObject);target.Release();Object.DestroyImmediate(target);}
        }
        public static void Render()
        {
            Run();
            var root=new GameObject("Disposable lot finishes preview");
            var target=new RenderTexture(1400,850,24);var previous=RenderTexture.active;
            try
            {
                var catalog=LotAssetCatalog.Load();
                var sun=new GameObject("Preview sun").AddComponent<Light>();sun.transform.SetParent(root.transform);sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(50,-30,0);
                var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.orthographic=true;camera.orthographicSize=10.8f;camera.transform.position=new Vector3(24,23,-30);camera.transform.LookAt(new Vector3(0,0,0));camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.17f,.19f);
                var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform);ground.transform.localScale=new Vector3(43,.1f,16);ground.transform.localPosition=new Vector3(0,-.08f,0);
                var grass=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.34f,.41f,.22f)};ground.GetComponent<Renderer>().sharedMaterial=grass;
                for(int w=1;w<=3;w++)
                {
                    var lot=Layout((LotWealth)w);lot.objects[0].position=new Vector2(-1.2f,3);lot.objects[1].position=new Vector2(.6f,3);
                    LotPropStyles.Apply(lot,"render-"+w,catalog);
                    var parent=new GameObject(new string('$',w)).transform;parent.SetParent(root.transform);parent.position=new Vector3((w-2)*13,0,0);
                    foreach(var item in lot.objects)LotRuntimeLoader.InstantiateObject(item,catalog.Find(item.assetId),parent);
                    var path=new LotPathDefinition{points=new[]{new Vector2(-5,0),new Vector2(5,0)},width=3,materialId=lot.paths[0].materialId};
                    var style=LotPathMaterial.Find(path.materialId);var mesh=LotPathSurface.BuildMesh(path,style.TileSize,.02f);var paving=new GameObject("Sidewalk",typeof(MeshFilter),typeof(MeshRenderer));paving.transform.SetParent(parent,false);paving.GetComponent<MeshFilter>().sharedMesh=mesh;paving.GetComponent<MeshRenderer>().sharedMaterial=style.CreateMaterial();
                    var label=new GameObject("Wealth label").AddComponent<TextMesh>();label.transform.SetParent(parent,false);label.transform.localPosition=new Vector3(0,.1f,-4);label.transform.rotation=camera.transform.rotation;label.text=new string('$',w)+"  "+LotPropStyles.Find(lot.objects[0].assetId).family;label.characterSize=.27f;label.fontSize=40;label.anchor=TextAnchor.MiddleCenter;
                }
                camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1400,850,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1400,850),0,0);pixels.Apply();Directory.CreateDirectory("Library/LotPropStylePreview");File.WriteAllBytes("Library/LotPropStylePreview/Wealth-Finishes.png",pixels.EncodeToPNG());Object.DestroyImmediate(pixels);Object.DestroyImmediate(grass);
                Debug.Log("LOT PROP FINISH PREVIEW rendered three wealth levels using private existing fence art and sidewalk materials.");
            }
            finally{RenderTexture.active=previous;Object.DestroyImmediate(root);target.Release();Object.DestroyImmediate(target);}
        }
    }
}
#endif
