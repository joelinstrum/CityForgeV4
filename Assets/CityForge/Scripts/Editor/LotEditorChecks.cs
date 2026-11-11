using System;
using System.IO;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

internal static class LotEditorChecks
{
    [MenuItem("CityForge/Lots/Check editor and runtime in Play Mode")]
    public static void Run()
    {
        Require(EditorApplication.isPlaying, "Start the MainMenu scene in Play Mode.");
        var menu = GameObject.Find("LotEditorButton").GetComponent<Button>();
        menu.onClick.Invoke();
        var workspace = GameObject.Find("Lot Editor Workspace");
        Require(workspace, "Main menu opens workspace.");
        var inspector = workspace.transform.Find("Lot inspector");
        var dimensions = new[] { inspector.Find("Width dropdown").GetComponent<Button>(),
            inspector.Find("Length dropdown").GetComponent<Button>() };
        Require(dimensions[0].GetComponentInChildren<Text>().text.StartsWith("6 ") &&
            dimensions[1].GetComponentInChildren<Text>().text.StartsWith("8 "), "Default is 6 × 8 on the 4 m grid.");
        Choose(dimensions[0], inspector, "Width choices", "32");
        Choose(dimensions[1], inspector, "Length choices", "1");
        Require(dimensions[0].GetComponentInChildren<Text>().text.StartsWith("32 ") &&
            dimensions[1].GetComponentInChildren<Text>().text.StartsWith("1 "), "Both size limits apply.");
        var footer = workspace.transform.Find("Status bar");
        footer.Find("Undo").GetComponent<Button>().onClick.Invoke();
        Require(dimensions[1].GetComponentInChildren<Text>().text.StartsWith("8 "), "Undo restores length.");
        footer.Find("Redo").GetComponent<Button>().onClick.Invoke();
        Require(dimensions[1].GetComponentInChildren<Text>().text.StartsWith("1 "), "Redo reapplies length.");

        footer.Find("Undo").GetComponent<Button>().onClick.Invoke();
        footer.Find("Undo").GetComponent<Button>().onClick.Invoke();

        const string recentsKey = "CityForge.Lots.RecentAssets";
        bool hadRecents = PlayerPrefs.HasKey(recentsKey);
        string oldRecents = PlayerPrefs.GetString(recentsKey);
        try
        {
            CheckViewportControls(workspace, footer);
            CheckFencePaths(workspace, footer);
        }
        finally
        {
            if (hadRecents) PlayerPrefs.SetString(recentsKey, oldRecents);
            else PlayerPrefs.DeleteKey(recentsKey);
        }

        var catalog = LotAssetCatalog.Load();
        Require(catalog && catalog.Find("farmhouse")?.prefab && catalog.Find("poplar")?.prefab &&
            catalog.Find("wood-fence")?.prefab, "Local catalog resolves three assets.");
        var legacy = new LotDefinition { schema = 1, width = 2, length = 3, waterfrontRows = 1 };
        var upgraded = LotDefinition.Parse(JsonUtility.ToJson(legacy));
        Require(upgraded.width == 4 && upgraded.length == 6 && upgraded.waterfrontRows == 2 &&
            upgraded.Size == new Vector2(16, 24) && upgraded.waterfrontRows * LotDefinition.TileMeters == 8,
            "Legacy lots preserve their physical and waterfront dimensions.");
        var upgradedAgain = LotDefinition.Parse(JsonUtility.ToJson(upgraded));
        Require(upgradedAgain.Size == upgraded.Size && upgradedAgain.waterfrontRows == upgraded.waterfrontRows,
            "An upgraded lot does not migrate a second time.");

        var authored = new LotDefinition();
        authored.objects.Add(new LotObjectDefinition { assetId = "farmhouse", yaw = 90 });
        authored.objects.Add(new LotObjectDefinition { assetId = "poplar", position = new Vector2(-9, 10) });
        authored.objects.Add(new LotObjectDefinition { assetId = "poplar", position = new Vector2(9, 10) });
        authored.objects.Add(new LotObjectDefinition { assetId = "wood-fence", position = new Vector2(0, -11) });

        var path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".cflot");
        var root = new GameObject("Lot test instances");
        try
        {
            File.WriteAllText(path, JsonUtility.ToJson(authored));
            var reopened = LotDefinition.Parse(File.ReadAllText(path));
            Require(reopened.id == authored.id && reopened.objects.Select(item => item.id)
                .SequenceEqual(authored.objects.Select(item => item.id)), "Round trip preserves stable IDs.");
            foreach (var item in reopened.objects)
                LotRuntimeLoader.InstantiateObject(item, catalog.Find(item.assetId), root.transform);
            Require(root.transform.childCount == 4 && Mathf.Abs(root.transform.GetChild(0).localEulerAngles.y - 90) < .01f,
                "Runtime resolves all assets and preserves rotation.");

            reopened.objects.RemoveAt(1);
            var cosmetic = LotCustomizationService.CreateOverride(authored, reopened, catalog);
            var applied = LotCustomizationService.Apply(authored, cosmetic, catalog);
            Require(applied.objects.Count == 3 && applied.objects[0].id == authored.objects[0].id &&
                authored.objects.Count == 4, "Cosmetic override leaves base data intact.");
            reopened.width = 16;
            Reject(() => LotCustomizationService.CreateOverride(authored, reopened, catalog));
            reopened.width = authored.width;
            reopened.objects[0].yaw = 0;
            Reject(() => LotCustomizationService.CreateOverride(authored, reopened, catalog));
            cosmetic.objects.Add(new LotObjectDefinition { assetId = "farmhouse" });
            Reject(() => LotCustomizationService.Apply(authored, cosmetic, catalog));
            authored.length = LotDefinition.MaxTiles + 1;
            Reject(() => LotDefinition.Parse(JsonUtility.ToJson(authored)));
        }
        finally
        {
            UnityEngine.Object.Destroy(root);
            File.Delete(path);
        }
        Debug.Log("LOT EDITOR CHECKS PASS: dimension dropdowns, grid and selection tools, direct dragging, open and closed fences, asset groups, thumbnails, size variation, live search and recents, undo and redo, saved IDs, runtime prefabs and rotation, cosmetic override and protection.");
    }

    private static void CheckViewportControls(GameObject workspace, Transform footer)
    {
        var toolbar = workspace.transform.Find("Tools").gameObject;
        Require(!toolbar.activeSelf, "Selection tools start hidden.");
        var grid = workspace.transform.Find("Viewport controls/Grid: On").GetComponent<Button>();
        Require(GameObject.Find("Width line"), "Grid starts visible.");
        grid.onClick.Invoke();
        Require(!GameObject.Find("Width line") && grid.GetComponentInChildren<Text>().text == "Grid: Off",
            "Grid Off removes lot grid lines.");
        grid.onClick.Invoke();
        Require(GameObject.Find("Width line"), "Grid On restores lot grid lines.");

        var viewport = (RectTransform)workspace.transform.Find("3D viewport");
        Canvas.ForceUpdateCanvases();
        workspace.transform.Find("Categories/Trees").GetComponent<Button>().onClick.Invoke();
        var picker = GameObject.Find("Asset picker").transform.Find("Picker window");
        var results = picker.Find("Asset results/Asset entries");
        Require(results.Find("Evergreen") && results.Find("Deciduous") && results.Find("Tropical"),
            "Tree picker shows all three groups.");
        Require(results.GetComponentsInChildren<Button>().All(button => button.GetComponentInChildren<RawImage>()?.texture),
            "Each tree has a rendered thumbnail.");
        var search = picker.GetComponentInChildren<InputField>();
        search.text = "coconut tropical";
        Require(results.GetComponentsInChildren<Button>().Length == 3 &&
            results.GetComponentsInChildren<Button>().All(button => button.name.StartsWith("Coconut palm")),
            "Live search matches names and groups together.");
        picker.Find("Close").GetComponent<Button>().onClick.Invoke();
        picker = OpenResidentialBuildings();
        var variation = picker.GetComponentInChildren<Toggle>();
        Require(!variation.isOn, "Size variation is opt-in.");
        variation.isOn = true;
        picker.Find("Asset results/Asset entries/Farmhouse").GetComponent<Button>().onClick.Invoke();
        ClickViewport(viewport, viewport.rect.center);
        Require(toolbar.activeSelf, "Selecting the placed object reveals its tools.");
        var camera = GameObject.Find("Lot preview camera").GetComponent<Camera>();
        var content = GameObject.Find("Lot preview contents").transform;
        var farmhouse = content.Find("Farmhouse");
        var start = farmhouse.GetComponentInChildren<Renderer>().bounds.center;
        var original = farmhouse.localPosition;
        var delta = new Vector3(2, 0, 1);
        DragViewport(viewport, LocalPoint(viewport, camera, start), LocalPoint(viewport, camera, start + delta));
        var moved = GameObject.Find("Lot preview contents").transform.Find("Farmhouse").localPosition;
        Require(moved == original + delta, "Dragging the visible model moves it without selecting a tool.");
        footer.Find("Undo").GetComponent<Button>().onClick.Invoke();
        Require(GameObject.Find("Lot preview contents").transform.Find("Farmhouse").localPosition == original,
            "One Undo restores the position before dragging.");
        OpenResidentialBuildings().Find("Asset results/Asset entries/Farmhouse")
            .GetComponent<Button>().onClick.Invoke();
        var first = GameObject.Find("Lot preview contents").transform.Find("Farmhouse");
        ClickViewport(viewport, LocalPoint(viewport, camera, first.position + new Vector3(4, 0, -8)));
        var scales = GameObject.Find("Lot preview contents").transform.Cast<Transform>()
            .Where(child => child.name == "Farmhouse").Select(child => child.localScale.x).ToArray();
        Require(scales.Length == 2 && scales.All(scale => scale >= .85f && scale <= 1.15f) &&
            scales[0] != scales[1], "Repeated placements get distinct sizes within the selected range.");
        toolbar.transform.Find("Select").GetComponent<Button>().onClick.Invoke();
        ClickViewport(viewport, viewport.rect.min + viewport.rect.size * .08f);
        Require(!toolbar.activeSelf, "Clicking empty space hides the selection tools.");
        footer.Find("Undo").GetComponent<Button>().onClick.Invoke();
        footer.Find("Undo").GetComponent<Button>().onClick.Invoke();
        picker = OpenResidentialBuildings();
        results = picker.Find("Asset results/Asset entries");
        Require(results.GetComponentsInChildren<Text>()[0].text == "Recently used" &&
            results.GetComponentsInChildren<Button>()[0].name == "Farmhouse", "Most recent placement appears first.");
        picker.Find("Close").GetComponent<Button>().onClick.Invoke();

        Transform OpenResidentialBuildings()
        {
            workspace.transform.Find("Categories/Buildings").GetComponent<Button>().onClick.Invoke();
            var frame = GameObject.Find("Asset picker").transform.Find("Picker window");
            var entries = frame.Find("Asset results/Asset entries");
            Require(!entries.Find("Farmhouse"), "Buildings are hidden until an era and category are chosen.");
            entries.GetComponentsInChildren<Button>().First(button => button.name.StartsWith("Founder's Era")).onClick.Invoke();
            Require(!entries.Find("Farmhouse"), "Choosing an era shows only its categories.");
            entries.Find("Residential").GetComponent<Button>().onClick.Invoke();
            return frame;
        }
    }

    private static void CheckFencePaths(GameObject workspace, Transform footer)
    {
        var viewport = (RectTransform)workspace.transform.Find("3D viewport");
        var camera = GameObject.Find("Lot preview camera").GetComponent<Camera>();
        var origin = GameObject.Find("Lot preview contents").transform.position;
        var guide = viewport.Find("Fence path guide").gameObject;
        var choose = workspace.transform.Find("Choose fence for path").GetComponent<Button>();
        var corners = new[] { new Vector2(-8, -6), new Vector2(8, -6), new Vector2(8, 6), new Vector2(-8, 6) };

        workspace.transform.Find("Categories/Fences").GetComponent<Button>().onClick.Invoke();
        var singlePicker = GameObject.Find("Asset picker").transform.Find("Picker window");
        singlePicker.GetComponentInChildren<Toggle>().isOn = false;
        singlePicker.Find("Asset results/Asset entries/Wood fence").GetComponent<Button>().onClick.Invoke();
        Point(new Vector2(12.6f, 0));
        Require(Spans().Length == 1 && Spans()[0].childCount == 3, "Single fence has its rail and two posts.");
        var style = LotAssetCatalog.Load().Find("wood-fence");
        Require(Mathf.Abs(Spans()[0].localPosition.x + (style.fenceSpan + style.fencePostWidth) / 2 - 12) < .01f,
            "A click just outside the lot seats the panel flush inside the edge.");
        footer.Find("Undo").GetComponent<Button>().onClick.Invoke();

        Start();
        Point(new Vector2(-8, -16.6f));
        Point(new Vector2(8, -16.6f));
        workspace.SendMessage("CancelInteraction");
        Require(guide.activeSelf && choose.gameObject.activeSelf, "Escape retains the open fence guide.");
        Fill();
        var open = Spans();
        Require(open.Length > 1 && open.All(span => span.localPosition.z >= -16 && span.localPosition.z < -15),
            "Open guide produces one straight run.");
        Require(open.Sum(span => span.childCount - 1) == open.Length + 1, "Open run shares posts and caps both ends.");
        Require(Mathf.Abs(open.Sum(span => span.Find("Fence span").localScale.x *
            LotAssetCatalog.Load().Find("wood-fence").fenceSpan) - 16) < .01f, "Panels fill the entire open run.");
        footer.Find("Undo").GetComponent<Button>().onClick.Invoke();
        Require(Spans().Length == 0, "Undo removes the whole run.");

        Start();
        foreach (var corner in corners) Point(corner);
        Point(corners[0]);
        Require(choose.gameObject.activeSelf, "Clicking the first marker closes the outline.");
        Fill();
        var closed = Spans();
        Require(closed.All(span => span.childCount == 2), "Closed fence uses one post per join.");
        Require(closed.Length > open.Length &&
            closed.Any(span => Mathf.Abs(span.localPosition.x + 8) < .01f), "The closed fence includes the closing edge.");
        footer.Find("Undo").GetComponent<Button>().onClick.Invoke();

        void Start()
        {
            workspace.transform.Find("Categories/Fences").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Asset picker").transform.Find("Picker window/Draw fence path")
                .GetComponent<Button>().onClick.Invoke();
            Canvas.ForceUpdateCanvases();
        }

        void Point(Vector2 point) =>
            ClickViewport(viewport, LocalPoint(viewport, camera, origin + new Vector3(point.x, 0, point.y)));

        void Fill()
        {
            choose.onClick.Invoke();
            GameObject.Find("Asset picker").transform.Find("Picker window/Asset results/Asset entries/Wood fence")
                .GetComponent<Button>().onClick.Invoke();
            Require(!guide.activeSelf, "Filling clears the guide.");
        }

        Transform[] Spans() => GameObject.Find("Lot preview contents").transform.Cast<Transform>()
            .Where(child => child.Find("Fence span")).ToArray();
    }

    private static Vector2 LocalPoint(RectTransform viewport, Camera camera, Vector3 world)
    {
        var point = camera.WorldToViewportPoint(world);
        return viewport.rect.min + Vector2.Scale(viewport.rect.size, new Vector2(point.x, point.y));
    }

    private static void DragViewport(RectTransform viewport, Vector2 start, Vector2 end)
    {
        var press = RectTransformUtility.WorldToScreenPoint(null, viewport.TransformPoint(start));
        var pointer = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = press,
            pressPosition = press,
            eligibleForClick = true
        };
        ExecuteEvents.Execute(viewport.gameObject, pointer, ExecuteEvents.beginDragHandler);
        Require(!pointer.eligibleForClick, "Dragging consumes the click.");
        pointer.position = RectTransformUtility.WorldToScreenPoint(null, viewport.TransformPoint(end));
        ExecuteEvents.Execute(viewport.gameObject, pointer, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(viewport.gameObject, pointer, ExecuteEvents.endDragHandler);
    }

    private static void ClickViewport(RectTransform viewport, Vector2 local)
    {
        var pointer = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, viewport.TransformPoint(local))
        };
        ExecuteEvents.Execute(viewport.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static void Choose(Button dropdown, Transform inspector, string popup, string value)
    {
        dropdown.onClick.Invoke();
        inspector.Find(popup + "/" + value).GetComponent<Button>().onClick.Invoke();
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Invalid lot edit was accepted.");
    }

    private static void Require(bool pass, string description)
    {
        if (!pass) throw new InvalidOperationException(description);
    }
}
