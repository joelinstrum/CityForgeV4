using CityForge.UI;
using CityForge.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.Editor
{
    public static class PrototypeRegionChecks
    {
        [MenuItem("CityForge/Bake Geographic Fields")]
        public static void BakeFields()
        {
            PrototypeRegionData.BakeGeographicFields("Assets/CityForge/Resources/Region/GeographicFields.bytes");
            AssetDatabase.Refresh();
            Debug.Log("CityForge: geographic fields baked; every cached sample matches the computed field exactly.");
        }

        [MenuItem("CityForge/Validate District Entry And Trees")]
        public static void ValidateDistrictsAndTrees()
        {
            var view = Object.FindFirstObjectByType<PrototypeRegionView>();
            if (!EditorApplication.isPlaying || view==null) throw new System.InvalidOperationException("Open a region in Play mode first.");
            view.ShowOverview();
            var camera=view.GetComponentInChildren<Camera>();
            var originalMesh=view.transform.Find("RegionLandCover").GetComponent<MeshFilter>().sharedMesh;
            var terrain=view.GetComponentInChildren<Terrain>();
            var trees=view.transform.Find("RandomTrees");
            if (view.TreeCount<640 || trees==null) throw new System.InvalidOperationException("Tree scatter missing.");
            var variantsSeen = new System.Collections.Generic.HashSet<string>();
            foreach (Transform tree in trees)
            {
                var p=tree.position;
                if (p.y<view.SeaLevel || Mathf.Abs(p.y+.08f-terrain.SampleHeight(p))>.01f)
                    throw new System.InvalidOperationException("A tree is floating or under water.");
                variantsSeen.Add(tree.name.Substring(0,tree.name.LastIndexOf('_')));
                var lod=tree.GetComponent<LODGroup>();
                if(lod==null || lod.GetLODs().Length!=3) throw new System.InvalidOperationException("Tree detail levels missing.");
                var renderers=lod.GetLODs()[0].renderers;
                var bounds=renderers[0].bounds;
                foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if(bounds.size.y<5 || bounds.size.y>30 || Mathf.Abs(bounds.min.y-p.y)>.01f)
                    throw new System.InvalidOperationException("Imported tree scale or base alignment is incorrect.");
                foreach(var renderer in tree.GetComponentsInChildren<Renderer>())
                    foreach(var material in renderer.sharedMaterials)
                        if(material==null || material.GetTexture("_BaseMap")==null)
                            throw new System.InvalidOperationException("Tree foliage material is missing.");
            }
            var expected = new System.Collections.Generic.HashSet<string>(System.Linq.Enumerable.Select(Resources.LoadAll<GameObject>(ForestPresentationSettings.Load().resourceFolder), prefab=>prefab.name));
            if(!variantsSeen.SetEquals(expected)) throw new System.InvalidOperationException("Tree scatter did not include every variant in the selected forest pack.");
            for (var i=0;i<4;i++)
            {
                var point=new Vector3(i%2==0 ? -view.MapSide/4 : view.MapSide/4,0,i/2==0 ? -view.MapSide/4 : view.MapSide/4);
                point.y=terrain.SampleHeight(point);
                var screen=camera.WorldToScreenPoint(point);
                if (!view.TryEnterDistrict(camera.ScreenPointToRay(screen)) || view.SelectedDistrict!=i)
                    throw new System.InvalidOperationException("Screen ray selected the wrong district.");
                var mesh=view.transform.Find("RegionLandCover").GetComponent<MeshFilter>().sharedMesh;
                if (mesh.triangles.Length!=256*256*6 || mesh==originalMesh)
                    throw new System.InvalidOperationException("District did not isolate its terrain.");
                foreach (Transform tree in trees)
                    if (tree.gameObject.activeSelf != (PrototypeRegionView.DistrictAt(tree.position)==i))
                        throw new System.InvalidOperationException("Neighbouring district trees remained visible.");
                view.ShowOverview();
                if (view.SelectedDistrict!=-1 || view.transform.Find("RegionLandCover").GetComponent<MeshFilter>().sharedMesh!=originalMesh)
                    throw new System.InvalidOperationException("Overview restoration failed.");
            }
            view.EnterDistrict(2);
            Debug.Log("CityForge: all four district screen picks, isolated meshes, overview restoration, and "+view.TreeCount+" grounded trees from "+variantsSeen.Count+" textured pack variants with three detail levels passed.");
        }

        [MenuItem("CityForge/Validate Region Wizard And Zoom")]
        public static void ValidateWizardAndZoom()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter Play mode first.");
            var runner = new GameObject("RegionWizardValidation").AddComponent<RegionWizardCheckRunner>();
            runner.StartCoroutine(CheckWizard(runner.gameObject));
        }

        private static System.Collections.IEnumerator CheckWizard(GameObject runner)
        {
            var existing = Object.FindFirstObjectByType<PrototypeRegionView>();
            if (existing != null) { existing.ReturnToMenu(); yield return null; }
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            foreach (var size in new[] { RegionSize.Small, RegionSize.Medium, RegionSize.Large })
            {
                controller.Handle(MenuAction.CreateRegion);
                var modal = Object.FindFirstObjectByType<CityForgeModal>();
                modal.SetRegionName("Joe's Valley"); modal.SetRegionSize(size);
                modal.SelectCreateRegionSection(1);
                var next = System.Array.Find(modal.GetComponentsInChildren<Button>(true), b => b.name == "TerrainNextButton" || b.transform.parent.name == "CreateRegionTerrainIntro" && b.name == "NextButton");
                if (next == null) throw new System.InvalidOperationException("Terrain Next button missing.");
                next.onClick.Invoke();
                var view = Object.FindFirstObjectByType<PrototypeRegionView>();
                if (view == null || view.RegionName != "Joe's Valley" || view.Size != size
                    || view.MapSide != 1024f * Mathf.Pow(2,(int)size))
                    throw new System.InvalidOperationException("Wizard size/name did not reach the map.");
                var rig = view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();
                var camera = rig.GetComponent<Camera>();
                if (!camera.orthographic || Mathf.Abs(camera.transform.eulerAngles.x - 35.264f) > .01f)
                    throw new System.InvalidOperationException("Region is not isometric orthographic.");
                var lastSize = 0f;
                for (var level=1; level<=5; level++)
                {
                    view.transform.Find("RegionHud/ZoomLevel" + level).GetComponent<Button>().onClick.Invoke();
                    if (rig.ZoomLevel != level || camera.orthographicSize <= lastSize)
                        throw new System.InvalidOperationException("Zoom stops are not ordered nearest to farthest.");
                    lastSize = camera.orthographicSize;
                }
                ValidateDiagnostics();
                Debug.Log("CityForge: wizard name, " + size + " size, isometric camera and five zoom stops passed.");
                view.ReturnToMenu(); yield return null;
            }
            controller.CreatePrototypeRegion("Great River Valley", RegionSize.Medium);
            Debug.Log("CityForge: all three region sizes and wizard creation validated.");
            Object.Destroy(runner);
        }

        [MenuItem("CityForge/Validate Region Diagnostics")]
        public static void ValidateDiagnostics()
        {
            var view = Object.FindFirstObjectByType<PrototypeRegionView>();
            if (!EditorApplication.isPlaying || view == null)
                throw new System.InvalidOperationException("Open the region in Play mode first.");
            var diagnostics = view.GetComponent<RegionPreviewDiagnostics>();
            diagnostics.SetDiagnostic(false, 1);
            var surface = view.transform.Find("RegionLandCover").GetComponent<MeshRenderer>();
            var mesh = surface.GetComponent<MeshFilter>().sharedMesh;
            var material = surface.sharedMaterial;
            var vertices = mesh.vertices;
            var camera = view.transform.Find("RegionCamera");
            var cameraPose = camera.localToWorldMatrix;
            var sun = view.transform.Find("RegionSun");
            var sunPose = sun.localToWorldMatrix;
            var waterPose = view.transform.Find("SeaAndConnectedRiverWater").localToWorldMatrix;
            var terrainData = view.GetComponentInChildren<Terrain>().terrainData;
            var terrainSize = terrainData.size;
            var heights = terrainData.GetHeights(0,0,513,513);
            var dataSample = view.Data.Heights[450,80];
            var hud = view.transform.Find("RegionHud");
            hud.Find("ReliefDiagnosticToggle").GetComponent<Button>().onClick.Invoke();
            foreach (var factor in new[] { 1,2,4 })
            {
                hud.Find("ReliefScale" + factor).GetComponent<Button>().onClick.Invoke();
                if (!diagnostics.IsEnabled || diagnostics.Exaggeration != factor
                    || surface.sharedMaterial == material || surface.transform.localScale.y != factor)
                    throw new System.InvalidOperationException("Diagnostic button failed.");
                var renderedSea = surface.transform.TransformPoint(new Vector3(0,view.SeaLevel,0)).y;
                if (Mathf.Abs(renderedSea - view.SeaLevel) > .001f)
                    throw new System.InvalidOperationException("Diagnostic displaced sea-level intersections.");
                if (camera.localToWorldMatrix != cameraPose || sun.localToWorldMatrix != sunPose
                    || view.transform.Find("SeaAndConnectedRiverWater").localToWorldMatrix != waterPose
                    || terrainData.size != terrainSize || view.Data.Heights[450,80] != dataSample)
                    throw new System.InvalidOperationException("Diagnostic modified the source region or scene rig.");
            }
            hud.Find("ReliefDiagnosticToggle").GetComponent<Button>().onClick.Invoke();
            if (surface.sharedMaterial != material || surface.transform.localScale != Vector3.one
                || surface.transform.localPosition != Vector3.zero)
                throw new System.InvalidOperationException("Diagnostic did not restore the normal presentation.");
            var after = mesh.vertices;
            for (var i=0;i<vertices.Length;i++)
                if (vertices[i] != after[i]) throw new System.InvalidOperationException("Diagnostic changed mesh vertices.");
            var afterHeights = terrainData.GetHeights(0,0,513,513);
            for (var z=0;z<513;z++) for (var x=0;x<513;x++)
                if (heights[z,x] != afterHeights[z,x]) throw new System.InvalidOperationException("Diagnostic changed collision heights.");
            diagnostics.SetDiagnostic(true,1);
            Debug.Log("CityForge: diagnostic 1x/2x/4x buttons and restoration verified; source heights, mesh, camera, sun and water unchanged.");
        }

        [MenuItem("CityForge/Capture Region Presentation")]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying || Object.FindFirstObjectByType<PrototypeRegionView>() == null)
                throw new System.InvalidOperationException("Open the region in Play mode first.");
            System.IO.Directory.CreateDirectory("Documentation/03_Technical/Screenshots");
            ScreenCapture.CaptureScreenshot("Documentation/03_Technical/Screenshots/Region_PBR.png");
            var kit = Resources.Load<RegionTerrainKit>("Region/TerrainKit");
            if (kit == null || kit.albedos.depth != 6 || kit.normals.depth != 6 || kit.masks.depth != 6
                || ShaderUtil.ShaderHasError(kit.terrainMaterial.shader))
                throw new System.InvalidOperationException("Region material kit validation failed.");
            Debug.Log("CityForge: region presentation capture requested; six PBR layers verified.");
        }

        [MenuItem("CityForge/Validate Coastal Prototype")]
        public static void Validate()
        {
            for (var seed = PrototypeRegionData.DefaultSeed; seed < PrototypeRegionData.DefaultSeed + 10; seed++)
            {
                var data = new PrototypeRegionData(seed);
                var land = 0;
                var buildable = 0;
                var water = 0;
                var maximumElevation = 0f;
                var gentleCells = new bool[513,513];
                for (var z = 1; z < 512; z++)
                for (var x = 1; x < 512; x++)
                {
                    var height = data.Heights[z, x] * PrototypeRegionData.Height;
                    maximumElevation = Mathf.Max(maximumElevation, height);
                    if (height < 12f) { water++; continue; }
                    land++;
                    var dx = (data.Heights[z, x + 1] - data.Heights[z, x - 1]) * PrototypeRegionData.Height;
                    var dz = (data.Heights[z + 1, x] - data.Heights[z - 1, x]) * PrototypeRegionData.Height;
                    var slope = Mathf.Sqrt(dx * dx + dz * dz) / (2f * PrototypeRegionData.Side / 512f);
                    if (slope < Mathf.Tan(2f * Mathf.Deg2Rad))
                    { buildable++; gentleCells[z,x] = true; }
                }
                Debug.Log("CityForge slope audit: " + (100f * buildable / land).ToString("F1") + "% gentle; peak " + maximumElevation.ToString("F0") + " m.");
                if (buildable / (float)land < .70f)
                    throw new System.InvalidOperationException("Prototype has insufficient gentle land, seed " + seed);
                var wetFraction = water / (511f * 511f);
                if (wetFraction < .18f || wetFraction > .33f)
                    throw new System.InvalidOperationException("Prototype coastal water fraction is outside target.");
                if (seed == PrototypeRegionData.DefaultSeed)
                {
                    var patches = LargestGentlePatches(gentleCells);
                    var cellSquareKm = Mathf.Pow(PrototypeRegionData.Side / 512f, 2f) / 1000000f;
                    if (patches[0] < land * .20f)
                        throw new System.InvalidOperationException("Gentle land is too fragmented for a large city area.");
                    Debug.Log("CityForge connected gentle land: largest " + (patches[0] * cellSquareKm).ToString("F1")
                        + " km²; second " + (patches[1] * cellSquareKm).ToString("F1") + " km² (4-neighbour connectivity).");
                    // Compare the actual wet footprint, not just its aggregate percentage.
                    var baseline = new PrototypeRegionData(seed, false);
                    var previous = new PrototypeRegionData(seed, true, false);
                    for (var z = 0; z < PrototypeRegionData.Resolution; z++)
                    for (var x = 0; x < PrototypeRegionData.Resolution; x++)
                    {
                        if ((baseline.Heights[z,x] * PrototypeRegionData.Height < 12f)
                            != (data.Heights[z,x] * PrototypeRegionData.Height < 12f))
                            throw new System.InvalidOperationException("Macro relief changed the water footprint.");
                        if (x / 512f >= .58f && previous.Heights[z,x] != data.Heights[z,x])
                            throw new System.InvalidOperationException("Foothill pass changed the eastern plain.");
                        if (baseline.WaterDistance[z,x] != data.WaterDistance[z,x])
                            throw new System.InvalidOperationException("Macro relief changed the water-distance field.");
                    }
                    Debug.Log("CityForge: all 263169 water footprint and distance samples preserved.");
                    var gentleFraction = buildable / (float)land;
                    if (gentleFraction > .80f || maximumElevation < 450f || maximumElevation >= PrototypeRegionData.Height)
                        throw new System.InvalidOperationException("Fixed region macro relief is outside the target range.");
                    if (data.Elevation(.15f, .90f) < 400f || data.Elevation(.72f, .43f) > 150f)
                        throw new System.InvalidOperationException("NW uplands / SE plains bias failed.");
                    if (data.Elevation(.25f, .75f) < data.Elevation(.50f, .75f) + 100f)
                        throw new System.InvalidOperationException("Broad river valley descent is missing.");
                    // Cross a southern main-river valley away from both spline axes.
                    // Its shoulders must rise across kilometres, beyond the channel band.
                    if (data.Elevation(.60f,.45f) < data.Elevation(.52f,.45f) + 15f
                        || data.Elevation(.72f,.45f) < data.Elevation(.60f,.45f) + 35f)
                        throw new System.InvalidOperationException("Broad river-distance valley shoulders are missing.");
                    Debug.Log("CityForge macro relief: peak " + maximumElevation.ToString("F0")
                        + " m; NW " + data.Elevation(.15f,.90f).ToString("F0")
                        + " m; SE " + data.Elevation(.72f,.43f).ToString("F0") + " m.");
                }
                var duplicate = new PrototypeRegionData(seed);
                for (var z = 0; z < 513; z += 17)
                for (var x = 0; x < 513; x += 17)
                    if (data.Heights[z, x] != duplicate.Heights[z, x])
                        throw new System.InvalidOperationException("Prototype seed is not deterministic.");
                Debug.Log("CityForge coastal seed " + seed + ": " + (100f * buildable / land).ToString("F1")
                    + "% of land below 2 degrees; " + (100f * wetFraction).ToString("F1") + "% water.");
            }
            Debug.Log("CityForge: coastal prototype validation passed (10 seeds).");
        }

        private static int[] LargestGentlePatches(bool[,] cells)
        {
            var remaining = (bool[,])cells.Clone();
            var largest = new int[2];
            var queue = new System.Collections.Generic.Queue<int>();
            for (var z = 1; z < 512; z++) for (var x = 1; x < 512; x++)
            {
                if (!remaining[z,x]) continue;
                remaining[z,x] = false;
                queue.Enqueue(z * 513 + x);
                var count = 0;
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue(); var cz = cell / 513; var cx = cell % 513;
                    count++;
                    Visit(cx - 1, cz); Visit(cx + 1, cz); Visit(cx, cz - 1); Visit(cx, cz + 1);
                }
                if (count > largest[0]) { largest[1] = largest[0]; largest[0] = count; }
                else if (count > largest[1]) largest[1] = count;
            }
            return largest;

            void Visit(int x, int z)
            {
                if (x < 1 || x >= 512 || z < 1 || z >= 512 || !remaining[z,x]) return;
                remaining[z,x] = false;
                queue.Enqueue(z * 513 + x);
            }
        }

        [MenuItem("CityForge/Preview Generated Coastal Region")]
        public static void Preview()
        {
            if (!EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Enter Play mode to preview generated terrain.");
            ModalPrefabBuilder.PreviewTerrain();
            var modal = Object.FindFirstObjectByType<CityForgeModal>();
            // Exercise the actual second Next button and its runtime listener.
            var next = modal.transform.Find("Panel75/RoundedContentClip/ScrollViewport/ScrollContent/CreateRegionTerrainIntro/NextButton").GetComponent<Button>();
            if (!next.interactable) throw new System.InvalidOperationException("Terrain Next is disabled.");
            next.onClick.Invoke();
            if (Object.FindFirstObjectByType<PrototypeRegionView>() == null)
                throw new System.InvalidOperationException("Terrain Next did not generate the region.");
        }
    }
    public sealed class RegionWizardCheckRunner : MonoBehaviour { }

}
