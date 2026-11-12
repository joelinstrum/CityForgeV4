using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static CityForge.UI.LotEditorWorkspace;

namespace CityForge.UI
{
    internal sealed class LotEditorPanel : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color32(17, 27, 37, 255);
        private static readonly Color ButtonColor = new Color32(27, 42, 55, 255);
        private static readonly Color Gold = new Color32(232, 186, 99, 255);
        private static readonly Color Accent = new Color32(105, 77, 38, 255);
        private static readonly Color Stroke = new Color32(65, 81, 93, 255);
        private LotLibrary library;
        private readonly LotEditorSession session = new LotEditorSession();
        private LotAssetCatalog catalog;
        private readonly LotAssetThumbnails thumbnails = new LotAssetThumbnails();
        private bool varyPlacementSize;
        private readonly PointWirePath repeatPath = new PointWirePath();
        private LotObjectDefinition repeatSource;
        private readonly PointWirePath fencePath = new PointWirePath();
        private bool drawingFence;
        private readonly PointWirePath groundPath = new PointWirePath();
        private bool drawingPath;
        private int pathWidth = 3;
        private Button textureTool, terrainTool;
        private bool terrainMode;
        private Vector2Int? terrainTile;
        private RectTransform terrainInspector;
        private Text terrainTileLabel;
        private Button rotateOverlayLeft, rotateOverlayRight;
        private Text overlayModeLabel;
        private LotGroundTexture overlayBrush;
        private int overlayBrushSpan = 1;
        private Vector2? overlayLast;
        private readonly HashSet<Vector2Int> overlayStroke = new HashSet<Vector2Int>();
        private bool curvePath;
        private Button curveDraftCorners, curveSelectedCorners;
        private LotEdgeBlendSlider edgeBlendSlider;
        private Toggle recessedPathToggle;
        private Text edgeBlendLabel;
        private RectTransform pathTools;
        private Button pathTool, finishPath, clearPath, choosePathMaterial;
        private readonly List<Button> pathWidthButtons = new List<Button>();
        private Button chooseFence;
        private readonly System.Random placementRandom = new System.Random();
        private LotEditorViewport viewport;
        private Font font;
        private Text waterfrontRowsLabel;
        private RectTransform inspector, assetList, selectionToolbar, pathSelectionToolbar;
        private Button rotateSelectionButton, shrinkSelectionButton, growSelectionButton;
        private GameObject assetPopup, assetPopupOpener;
        private ScrollRect assetScroll;
        private GameObject choices;
        private Text width, length, status, selectedLabel, snapLabel, gridLabel;
        private Text lotTitle, lotSummary, validation, tooltip;
        private Font headingFont;
        private LotToolDescription tooltipOwner;
        private Button selectTool, moveTool;
        private const string RecentAssetsKey = "CityForge.Lots.RecentAssets";
        private const string RecentPathMaterialsKey = "CityForge.Lots.RecentPathMaterials";
        private readonly List<string> recentAssets = new List<string>();
        private readonly List<string> recentPathMaterials = new List<string>();
        private string category = "";
        private readonly System.Collections.Generic.Dictionary<string, Button> categoryButtons =
            new System.Collections.Generic.Dictionary<string, Button>();
        private InputField nameField, costField, monthlyCostField;
        private Button lotCategory, lotSubcategory, lotEra;
        private Text lotCategoryLabel, lotSubcategoryLabel, lotEraLabel;
        private Button undo, redo, scriptButton;
        private readonly HashSet<string> selectedGroup = new();
        private LotObjectDefinition[] clipboard = Array.Empty<LotObjectDefinition>();
        private string primarySelection, armed, dragged;
        private string selected
        {
            get => primarySelection;
            set
            {
                primarySelection = value;
                selectedGroup.Clear();
            }
        }
        private float placementYaw, placementScale = 1f;
        private LotPathDefinition selectedPath;
        private Vector2 dragStart, dragOrigin, dragPosition;
        private bool moving, grid = true, snap = true;
        private LotDefinition playerLot;
        private Action<LotCustomization> savePlayer;
        private UnityAction goBack;
        private bool confirmDiscard;
        private bool PlayerMode => playerLot != null;

        public void Initialize(Font uiFont, Font serif, LotDefinition original, LotCustomization customization,
            Action<LotCustomization> saveCustomization, UnityAction back)
        {
            font = uiFont;
            library = LotLibrary.Local;
            headingFont = serif ? serif : uiFont;
            catalog = LotAssetCatalog.Load();
            recentAssets.AddRange(PlayerPrefs.GetString(RecentAssetsKey)
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Distinct().Take(32));
            recentPathMaterials.AddRange(PlayerPrefs.GetString(RecentPathMaterialsKey)
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Distinct().Take(4));
            playerLot = original;
            savePlayer = saveCustomization;
            goBack = back;
            if (PlayerMode) session.Open(LotCustomizationService.Apply(original, customization, catalog));
            var surface = new GameObject("3D viewport", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            surface.transform.SetParent(transform, false);
            Place(surface.rectTransform, Vector2.zero, Vector2.one, new Vector2(7, 55), new Vector2(-357, -64));
            AddOutline(surface, Stroke);
            viewport = surface.gameObject.AddComponent<LotEditorViewport>();
            viewport.Initialize(surface, Click, BeginMove, PreviewMove, EndMove);
            viewport.CommitEntryMarker = CommitEntryMarker;
            viewport.ConfigureFootprintEditing(CommitBuildingFootprint, SnapPoint, () =>
            {
                selected = null;
                selectedGroup.Clear();
                selectedPath = null;
                Refresh("Drag the footprint to move it. Drag a corner or edge handle to resize.");
            });

            lotTitle = Label(transform, "", 22, 72, 610, 27);
            lotTitle.font = headingFont;
            lotSummary = Label(transform, "", 24, 104, 620, 16);
            lotSummary.color = new Color32(194, 202, 206, 255);

            var categories = Tray("Categories", 22, 147, 681, 53);
            AddCategory(categories, "", "All assets", LotGlyph.Catalog, 0);
            AddCategory(categories, "Buildings", "Buildings", LotGlyph.House, 1);
            AddCategory(categories, "Trees", "Trees", LotGlyph.Tree, 2);
            AddCategory(categories, "Fences", "Fences", LotGlyph.Fence, 3);
            pathTool = ToolTile(categories, "Paths", LotGlyph.Path, 4, StartGroundPath, "Draw a ground path");
            textureTool = ToolTile(categories, "Textures", LotGlyph.Textures, 5, OpenGroundTexturePopup,
                "Choose base and overlay textures for the lot");
            textureTool.gameObject.SetActive(!PlayerMode);
            terrainTool = ToolTile(categories, "Terrain", LotGlyph.Grid, 6, SelectTerrain,
                "Select a terrain tile to remove its overlay");
            terrainTool.gameObject.SetActive(!PlayerMode);
            var overlayBadge = Tray("Overlay mode", 22, 255, 412, 33);
            overlayModeLabel = Label(overlayBadge, "", 8, 2, 395, 16);
            overlayModeLabel.color = Gold;
            AddCategory(categories, "Landscaping", "Landscaping", LotGlyph.Landscaping, 7);
            if (!PlayerMode) AddCategory(categories, "Characters", "Characters", LotGlyph.Character, 8);
            AddCategory(categories, LotAssetCatalog.StreetFurniture, LotAssetCatalog.StreetFurniture, LotGlyph.StreetFurniture, 9);
            pathTools = AddImage("Ground path controls", transform, PanelColor).rectTransform;
            Place(pathTools, Vector2.zero, Vector2.zero, new Vector2(22, 121), new Vector2(621, 167));
            AddOutline(pathTools.GetComponent<Image>(), Stroke);
            Label(pathTools, "Width", 8, 5, 49);
            foreach (int meters in LotPathSurface.Widths)
            {
                pathWidthButtons.Add(Control(pathTools, $"{meters} m", 60 + pathWidthButtons.Count * 51, 5, 48,
                    () => { pathWidth = meters; Refresh(rebuildPreview: false); }, description: $"Set path width to {meters} meters"));
            }
            finishPath = Control(pathTools, "Finish", 318, 5, 66, () => FinishGroundPath(false),
                description: "Keep the current path (Enter or Escape)");
            clearPath = Control(pathTools, "Clear", 388, 5, 66, () => { ClearGroundPath(); Refresh(rebuildPreview: false); },
                description: "Remove the ground path sketch");
            curveDraftCorners = Control(pathTools, "Curve corners", 458, 5, 134,
                () => { curvePath = !curvePath; Refresh(rebuildPreview: false); },
                description: "Toggle rounded corners for the path sketch");
            choosePathMaterial = Control(transform, "Select path material", 22, 207, 220, OpenPathMaterialPopup,
                LotGlyph.Path, "Choose a surface for the amber path");
            pathTool.gameObject.SetActive(!PlayerMode);

            chooseFence = Control(transform, "Choose fence for path", 22, 207, 209,
                () => OpenAssetPopup("Fences", "Choose fence for path"), LotGlyph.Fence,
                "Fill the purple path with a fence style");

            var bar = selectionToolbar = Tray("Tools", 648, 147, 385, 53);
            selectTool = Control(bar, "Select", 6, 8, 42, () => { armed = null; moving = false; Refresh(); },
                LotGlyph.Select, "Select an object on the lot", true);
            moveTool = Control(bar, "Move", 53, 8, 42, () => { armed = null; moving = CanEdit(Selection); Refresh(); },
                LotGlyph.Move, "Move the selected object", true);
            rotateSelectionButton = Control(bar, "Rotate 90°", 100, 8, 42, RotateSelection,
                LotGlyph.Rotate, "Rotate the selected object 90 degrees", true);
            shrinkSelectionButton = Control(bar, "Scale −", 147, 8, 42, () => ChangeObject(item => item.scale = Mathf.Max(.25f, item.scale - .25f)),
                LotGlyph.Shrink, "Decrease the selected object's scale", true);
            growSelectionButton = Control(bar, "Scale +", 194, 8, 42, () => ChangeObject(item => item.scale = Mathf.Min(3f, item.scale + .25f)),
                LotGlyph.Grow, "Increase the selected object's scale", true);
            Control(bar, "Duplicate", 241, 8, 42, Duplicate, LotGlyph.Copy, "Duplicate the selected object", true);
            Control(bar, "Delete", 288, 8, 42, Delete, LotGlyph.Trash, "Delete the selected object (Delete or Backspace)", true);

            scriptButton = Control(bar, "Script", 335, 8, 42, OpenCharacterScript, LotGlyph.Edit,
                "Script · Configure the selected character's behavior", true);

            pathSelectionToolbar = Tray("Path tools", 713, 147, 358, 132);
            Control(pathSelectionToolbar, "Change material", 6, 8, 158,
                () => OpenPathMaterialPopup(selectedPath), LotGlyph.Path, "Choose a new material for the selected path");
            curveSelectedCorners = Control(pathSelectionToolbar, "Curve corners", 170, 8, 134,
                () =>
                {
                    var path = selectedPath;
                    if (path == null || PlayerMode) return;
                    session.Edit(_ => path.curved = !path.curved);
                    Refresh();
                }, description: "Toggle rounded corners for the selected path");
            Control(pathSelectionToolbar, "Delete path", 310, 8, 42, Delete, LotGlyph.Trash,
                "Delete the entire path (Delete or Backspace)", true);
            edgeBlendLabel = Label(pathSelectionToolbar, "Edge Blending", 10, 49, 150, 15);
            var sliderTrack = AddImage("Edge Blending slider", pathSelectionToolbar, ButtonColor);
            At(sliderTrack.rectTransform, 164, 56, 181, 22);
            sliderTrack.raycastTarget = true;
            var slideArea = new GameObject("Handle area", typeof(RectTransform)).GetComponent<RectTransform>();
            slideArea.SetParent(sliderTrack.transform, false);
            Place(slideArea, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
            var thumb = AddImage("Handle", slideArea, Gold);
            thumb.rectTransform.sizeDelta = new Vector2(16, 0);
            thumb.rectTransform.anchorMax = Vector2.up;
            edgeBlendSlider = sliderTrack.gameObject.AddComponent<LotEdgeBlendSlider>();
            edgeBlendSlider.targetGraphic = thumb;
            edgeBlendSlider.handleRect = thumb.rectTransform;
            edgeBlendSlider.maxValue = LotPathDefinition.MaxEdgeBlend;
            edgeBlendSlider.wholeNumbers = true;
            edgeBlendSlider.Commit = CommitEdgeBlend;
            edgeBlendSlider.onValueChanged.AddListener(value =>
            {
                if (selectedPath == null) return;
                int blend = (int)value;
                viewport.PreviewPathEdgeBlend(selectedPath, blend);
                edgeBlendLabel.text = $"Edge Blending {blend}%";
                if (!edgeBlendSlider.PointerHeld) CommitEdgeBlend();
            });

            recessedPathToggle = Checkbox(pathSelectionToolbar, "Recess path", "Recess path · edge shadow",
                10, 90, 335, false);
            recessedPathToggle.onValueChanged.AddListener(value =>
            {
                var path = selectedPath;
                if (path == null || PlayerMode) return;
                if (!session.Edit(_ => path.recessed = value)) return;
                viewport.PreviewPathEdgeBlend(path, path.edgeBlend);
                Refresh(rebuildPreview: false);
            });

            var viewTools = AddImage("Viewport controls", transform, PanelColor).rectTransform;
            Place(viewTools, Vector2.zero, Vector2.zero, new Vector2(22, 69), new Vector2(650, 115));
            AddOutline(viewTools.GetComponent<Image>(), Stroke);
            gridLabel = Control(viewTools, "Grid: On", 5, 5, 104, () => { grid = !grid; Refresh(); },
                LotGlyph.Grid, "Show or hide the lot and drafting grids").GetComponentInChildren<Text>();
            snapLabel = Control(viewTools, "Snap: On", 115, 5, 105, () => { snap = !snap; Refresh(); },
                LotGlyph.Snap, "Toggle snapping to one-meter increments").GetComponentInChildren<Text>();
            Control(viewTools, "Top / Iso", 226, 5, 115, viewport.TopDown, LotGlyph.View, "Switch between top-down and isometric views");
            Button lighting = null;
            lighting = Control(viewTools, "Day", 347, 5, 89, () =>
            {
                bool night = viewport.ToggleNight();
                lighting.GetComponentInChildren<Text>().text = night ? "Night" : "Day";
                MarkActive(lighting, night);
            }, LotGlyph.Lighting, "Toggle day and night lighting in the lot preview");
            Button animation = null;
            animation = Control(viewTools, "Suspend animation", 442, 5, 179, () =>
            {
                bool paused = viewport.ToggleAnimations();
                animation.GetComponentInChildren<Text>().text = paused ? "Resume animation" : "Suspend animation";
                MarkActive(animation, paused);
            }, description: "Pause or resume animations in the lot preview");

            inspector = AddImage("Lot inspector", transform, PanelColor).rectTransform;
            Place(inspector, Vector2.right, Vector2.one, new Vector2(-350, 55), new Vector2(-7, -64));
            AddOutline(inspector.GetComponent<Image>(), Stroke);
            Section(inspector, PlayerMode ? "Player Mode" : "Developer Mode", 14);
            Label(inspector, "Lot name", 20, 58, 85, 16);
            nameField = Field(inspector, "Lot name", 110, 58, 217);
            nameField.gameObject.name = "Lot name input";
            AddGlyph(nameField.transform, LotGlyph.Edit, 190, 9, 18);
            nameField.textComponent.rectTransform.offsetMax = new Vector2(-35, 0);
            nameField.characterLimit = 80;
            nameField.onEndEdit.AddListener(value =>
            {
                if (!PlayerMode && !string.IsNullOrWhiteSpace(value)) session.Edit(lot => lot.name = value.Trim());
                Refresh();
            });
            costField = CostField("Cost", 15, (lot, value) => lot.cost = value);
            monthlyCostField = CostField("Monthly cost", 176, (lot, value) => lot.monthlyCost = value);
            lotCategory = LotChoice("Category", 174, () =>
                ShowLotChoices("Category", 210, LotClassification.Categories.Select(item => (item.id, item.name)),
                    (lot, value) =>
                    {
                        if (LotClassification.FindCategory(lot.category)?.id == value || lot.category == value) return;
                        lot.category = value;
                        lot.subcategory = "";
                    }), out lotCategoryLabel);
            lotSubcategory = LotChoice("Subcategory", 212, () =>
                ShowLotChoices("Subcategory", 248, LotClassification.ForCategory(session.Lot.category).Select(item => (item.id, item.name)),
                    (lot, value) => lot.subcategory = value), out lotSubcategoryLabel);
            lotEra = LotChoice("First era", 250, () =>
                ShowLotChoices("First era", 286, CityForgeTimeline.Eras.Select(era => (era.id, era.Name)),
                    (lot, value) =>
                    {
                        lot.firstEraId = value;
                        lot.firstEraYear = CityForgeTimeline.Find(value)?.FirstYear ?? 0;
                    }),
                out lotEraLabel);
            Section(inspector, "Lot Dimensions", 294);
            Label(inspector, "Width (tiles)", 20, 338, 150);
            Label(inspector, "Length (tiles)", 20, 383, 150);
            width = Control(inspector, "Width dropdown", 190, 338, 137, () => ShowSizes(true)).GetComponentInChildren<Text>();
            length = Control(inspector, "Length dropdown", 190, 383, 137, () => ShowSizes(false)).GetComponentInChildren<Text>();
            nameField.interactable = !PlayerMode;
            width.GetComponentInParent<Button>().interactable = !PlayerMode;
            length.GetComponentInParent<Button>().interactable = !PlayerMode;
            Section(inspector, "Selection", 436);
            selectedLabel = Label(inspector, "", 20, 477, 308);
            Section(inspector, "Asset Catalog", 527);
            Control(inspector, "Browse assets…", 15, 571, 312, () => OpenAssetPopup("", "All assets"),
                LotGlyph.Catalog, "Open the searchable asset picker");

            if (!PlayerMode)
                Control(inspector, "Requirements & Bonus…", 15, 613, 312, OpenCapitalSettings,
                    description: "Set site requirements and a capital bonus for this lot");

            if (!PlayerMode)
                waterfrontRowsLabel = Control(inspector, "Waterfront rows", 15, 655, 312, OpenWaterfrontRows,
                    description: "Set how many blue rows at the water-facing edge may extend over water").GetComponentInChildren<Text>();
            if (!PlayerMode)
                Control(inspector, "Growable layout…", 15, 697, 312, OpenZoningLayout,
                    description: "Set zone, density, wealth, building slot and entrances");
            terrainInspector = AddImage("Terrain tile menu", inspector, ButtonColor).rectTransform;
            At(terrainInspector, 15, PlayerMode ? 621 : 742, 312, 110);
            AddOutline(terrainInspector.GetComponent<Image>(), Stroke);
            Label(terrainInspector, "Overlay orientation", 10, 3, 290, 18).font = headingFont;
            terrainTileLabel = Label(terrainInspector, "", 10, 28, 290, 14);
            rotateOverlayLeft = Control(terrainInspector, "Rotate left 90°", 9, 62, 142,
                () => RotateOverlay(-1), description: "Rotate the selected tile's overlay counterclockwise");
            rotateOverlayRight = Control(terrainInspector, "Rotate right 90°", 158, 62, 145,
                () => RotateOverlay(1), description: "Rotate the selected tile's overlay clockwise");

            var footer = AddImage("Status bar", transform, PanelColor).rectTransform;
            Place(footer, Vector2.zero, Vector2.right, new Vector2(7, 5), new Vector2(-7, 50));
            undo = Control(footer, "Undo", 8, 3, 72, () => { session.Undo(); Refresh(); }, LotGlyph.Undo, "Undo the last lot edit");
            redo = Control(footer, "Redo", 88, 3, 72, () => { session.Redo(); Refresh(); }, LotGlyph.Redo, "Redo the last undone lot edit");
            if (PlayerMode)
            {
                Control(footer, "Cancel", 168, 3, 72, () => goBack());
                Control(footer, "Reset", 248, 3, 86, ResetCosmetics);
            }
            else
            {
                Control(footer, "New", 168, 3, 72, NewLot, description: "Start a new lot");
                Control(footer, "Saved Lots", 248, 3, 99, () => OpenSavedLots(LotType.Playable),
                    description: "Browse, load, or delete saved lots");
                Control(footer, "Templates", 355, 3, 99, () => OpenSavedLots(LotType.Template),
                    description: "Browse, load, or delete lot templates");
                Control(footer, "Rotate Lot", 462, 3, 42, RotateLot, LotGlyph.RotateLot, "Rotate Lot", iconOnly: true);
                Control(footer, "Growable Lots", 512, 3, 104, () => OpenSavedLots(LotType.Zoning),
                    description: "Browse growable layouts by zone and wealth");
                var saveTemplate = Control(footer, "Save as Template", 0, 4, 152, () => Save(LotType.Template),
                    description: "Save this layout as a reusable template");
                PinRight((RectTransform)saveTemplate.transform, 133, 152);
            }
            var save = Control(footer, PlayerMode ? "Apply" : "Save Lot", 0, 4, 118, () => Save(!PlayerMode && session.Lot.zoningLayout.Configured ? LotType.Zoning : LotType.Playable), LotGlyph.Save,
                PlayerMode ? "Apply landscaping to this lot" : "Save this lot to the library");
            PinRight((RectTransform)save.transform, 7, 118);
            MarkActive(save, true);
            var validate = Control(footer, "Validate", 0, 4, 103,
                () => Refresh(Validate() ?? "No validation issues."), LotGlyph.Check, "Check asset references and lot boundaries");
            int saveSpace = PlayerMode ? 0 : 160;
            PinRight((RectTransform)validate.transform, 133 + saveSpace, 103);
            validation = Label(footer, "", 0, 4, 170, 15);
            PinRight(validation.rectTransform, 247 + saveSpace, 170);
            int statusLeft = PlayerMode ? 353 : 625;
            status = Label(footer, "", statusLeft, 4, 710, 15);
            Place(status.rectTransform, Vector2.up, Vector2.one, new Vector2(statusLeft, -40), new Vector2(-430 - saveSpace, -4));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;

            var bubble = AddImage("Tool description", transform, new Color32(9, 17, 24, 255));
            AddOutline(bubble, Stroke);
            tooltip = AddText("Description", bubble.transform, "", font, 16, TextAnchor.MiddleCenter);
            Place(tooltip.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
            bubble.gameObject.SetActive(false);
            Refresh();
        }

        public void CancelInteraction()
        {
            if (assetPopup)
            {
                CloseAssetPopup();
                return;
            }
            if (choices)
            {
                CloseChoices();
                return;
            }
            if (pickingLotEntrance)
            {
                pickingLotEntrance = false;
                Refresh();
                return;
            }
            if (repeatSource != null)
            {
                ClearRepeat();
                Refresh(rebuildPreview: false);
                return;
            }
            if (terrainMode)
            {
                StopOverlay();
                Refresh();
                return;
            }
            if (drawingPath)
            {
                FinishGroundPath(false);
                return;
            }
            if (drawingFence)
            {
                FinishFencePath(false);
                return;
            }
            if (selectedPath == null && fencePath.Finished)
            {
                ClearFencePath();
                Refresh();
                return;
            }
            if (selected == null && selectedPath == null && armed == null && !moving) return;
            selected = null;
            selectedPath = null;
            dragged = null;
            armed = null;
            moving = false;
            Refresh();
        }

        public void RequestBack()
        {
            if (assetPopup)
            {
                CloseAssetPopup();
                return;
            }
            if (choices)
            {
                CloseChoices();
                return;
            }
            if (HasUnfinishedLot && !confirmDiscard)
            {
                Refresh("Unsaved changes. Use Back again to discard them.");
                confirmDiscard = true;
                return;
            }
            goBack();
        }

        private void Update()
        {
            var active = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (Input.GetKeyDown(KeyCode.Tab) && EventSystem.current)
            {
                var controls = GetComponentsInChildren<Selectable>().Where(control => control.IsInteractable() &&
                    (!choices || control.transform.IsChildOf(choices.transform)) &&
                    (!assetPopup || control.transform.IsChildOf(assetPopup.transform))).ToArray();
                if (controls.Length != 0)
                {
                    int index = Array.FindIndex(controls, control => control.gameObject == active);
                    bool backwards = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    int next = index < 0 ? (backwards ? controls.Length - 1 : 0) :
                        (index + (backwards ? -1 : 1) + controls.Length) % controls.Length;
                    EventSystem.current.SetSelectedGameObject(controls[next].gameObject);
                }
                return;
            }
            if (assetPopup || dragged != null || overlayStroke.Count != 0 || active && active.GetComponent<InputField>()) return;
            if (!choices)
            {
                if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals) ||
                    Input.GetKeyDown(KeyCode.KeypadPlus))
                    viewport.ZoomBy(1);
                else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                    viewport.ZoomBy(-1);
            }
            if (!choices && !terrainMode && !drawingPath && !drawingFence && repeatSource == null &&
                armed == null && CanEdit(Selection) && NudgeSelection()) return;
            if (!choices && Input.GetKeyDown(KeyCode.R))
            {
                if (repeatSource != null)
                {
                    ClearRepeat();
                    Refresh(rebuildPreview: false);
                    return;
                }
                if (CanRotatePlacement(ArmedPreviewAsset))
                {
                    placementYaw = (placementYaw + 90f) % 360f;
                    Refresh(rebuildPreview: false);
                }
                else if (armed == null && !moving && !terrainMode && !drawingFence && !drawingPath &&
                    !fencePath.Finished && !groundPath.Finished && selectedGroup.Count == 0 &&
                    Selection is { } source && CanEdit(source))
                {
                    if (repeatPath.TryAdd(source.position))
                    {
                        repeatSource = source;
                        viewport.ShowRepeatPath(repeatPath, SnapPoint);
                        Refresh(rebuildPreview: false);
                    }
                    return;
                }
            }
            if (drawingPath && !choices)
            {
                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    groundPath.Undo();
                    Refresh(rebuildPreview: false);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    FinishGroundPath(false);
                    return;
                }
            }
            if (drawingFence && Input.GetKeyDown(KeyCode.Backspace))
            {
                fencePath.Undo();
                return;
            }
            if (!drawingPath && !drawingFence && !choices &&
                (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)))
            {
                Delete();
                return;
            }
            if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                  Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))) return;
            if (!choices && !terrainMode && !drawingPath && !drawingFence && repeatSource == null)
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    var items = SelectedAssets().Where(CanEdit).ToArray();
                    if (items.Length != 0)
                    {
                        clipboard = items.Select(item => CopyAsset(item, item.position)).ToArray();
                        Refresh($"Copied {items.Length} {(items.Length == 1 ? "asset" : "assets")}.", rebuildPreview: false);
                    }
                    return;
                }
                if (Input.GetKeyDown(KeyCode.V))
                {
                    PasteAssets(clipboard);
                    return;
                }
            }
            if (Input.GetKeyDown(KeyCode.S)) Save(session.Lot.type);
            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) session.Redo();
                else session.Undo();
                Refresh();
            }
        }

        private void ResetCosmetics()
        {
            var original = LotDefinition.Parse(JsonUtility.ToJson(playerLot));
            session.Edit(lot => lot.objects = original.objects);
            selected = armed = null;
            Refresh();
        }

        private bool CanEdit(LotObjectDefinition item)
        {
            return item != null && (!PlayerMode || LotCustomizationService.IsCosmetic(catalog, item.assetId));
        }

        private Button LotChoice(string label, float y, Action open, out Text value)
        {
            Label(inspector, label, 20, y, 116, 15);
            var button = Control(inspector, label + " dropdown", 140, y, 187, open,
                description: "Choose the lot's " + label.ToLowerInvariant());
            value = button.GetComponentInChildren<Text>();
            return button;
        }

        private void ShowLotChoices(string label, float y, IEnumerable<(string id, string name)> values, Action<LotDefinition, string> edit)
        {
            if (PlayerMode) return;
            string name = label + " choices";
            bool wasOpen = choices && choices.name == name;
            CloseChoices();
            if (wasOpen) return;
            var options = values.Prepend((id: "", name: "Unspecified")).ToArray();
            var popup = AddImage(name, inspector, new Color32(27, 41, 53, 255));
            popup.raycastTarget = true;
            choices = popup.gameObject;
            float height = options.Length * 38 + 8;
            At(popup.rectTransform, 140, y, 187, Mathf.Min(height, 312));
            AddOutline(popup, Gold);
            var list = popup.rectTransform;
            if (height > 312)
            {
                popup.gameObject.AddComponent<RectMask2D>();
                var scroll = popup.gameObject.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.viewport = popup.rectTransform;
                list = AddImage("Choices", popup.transform, Color.clear).rectTransform;
                list.pivot = Vector2.up;
                Place(list, Vector2.up, Vector2.one, new Vector2(0, -height), Vector2.zero);
                scroll.content = list;
            }
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                Control(list, option.name, 4, 4 + i * 38, 179,
                    () =>
                    {
                        session.Edit(lot => edit(lot, option.id));
                        CloseChoices();
                        Refresh(rebuildPreview: false);
                    });
            }
        }

        private void ShowSizes(bool changeWidth)
        {
            bool wasOpen = choices && choices.name == (changeWidth ? "Width choices" : "Length choices");
            CloseChoices();
            if (wasOpen) return;
            var popup = AddImage(changeWidth ? "Width choices" : "Length choices", inspector,
                new Color32(27, 41, 53, 255));
            popup.raycastTarget = true;
            choices = popup.gameObject;
            At(popup.rectTransform, 15, changeWidth ? 378 : 423, 312, 174);
            AddOutline(popup, Gold);
            for (int i = 1; i <= LotDefinition.MaxTiles; i++)
            {
                int value = i;
                Control(popup.rectTransform, value.ToString(), 5 + (i - 1) % 8 * 38, 5 + (i - 1) / 8 * 41, 35,
                    () =>
                    {
                        session.Edit(lot =>
                        {
                            if (changeWidth) lot.width = value;
                            else lot.length = value;
                            lot.waterfrontRows = Mathf.Min(lot.waterfrontRows, lot.WaterfrontDepth - 1);
                        });
                        CloseChoices();
                        Refresh();
                    });
            }
        }

        private void RotateLot()
        {
            if (PlayerMode || assetPopup) return;
            if (drawingPath || drawingFence || groundPath.Finished || fencePath.Finished)
            {
                Refresh("Finish or clear the current sketch before rotating the lot.", rebuildPreview: false);
                return;
            }
            CloseChoices();
            StopOverlay();
            session.Edit(lot => lot.RotateClockwise());
            Refresh("Lot rotated 90° clockwise.");
        }

        private void SeedBuildingFootprint(Vector2 point, LotAssetCatalogEntry asset)
        {
            var lot = session.Lot;
            var layout = lot.zoningLayout;
            if (!asset.CanPreviewGrowable(layout.zone, layout.wealth, lot.firstEraId))
            {
                Refresh("Choose a building matching this growable lot's era, zone and wealth.", rebuildPreview: false);
                return;
            }
            var building = new LotObjectDefinition
            {
                assetId = asset.id, position = SnapPoint(point), yaw = placementYaw, scale = placementScale
            };
            var next = layout.SeedBuilding(building, asset);
            var problem = next.FitProblem(lot);
            if (problem == null && SlotOverlapsBuilding(lot, next))
                problem = "Keep the building footprint clear of fixed buildings.";
            if (problem != null)
            {
                Refresh(problem, rebuildPreview: false);
                return;
            }
            session.Edit(draft => draft.zoningLayout = next);
            selected = null;
            selectedGroup.Clear();
            selectedPath = null;
            armed = null;
            RememberAsset(asset.id);
            Refresh($"Footprint sized to {asset.label} · {next.buildingSize.x:0.##} × {next.buildingSize.y:0.##} m.");
        }

        private void CommitBuildingFootprint(Rect bounds)
        {
            var layout = session.Lot.zoningLayout;
            if (bounds == layout.BuildingSlot)
            {
                Refresh();
                return;
            }
            var offset = bounds.center - layout.buildingCenter;
            layout.buildingCenter = bounds.center;
            layout.buildingSize = bounds.size;
            if (layout.hasEntrance) layout.entrance += offset;
            var problem = layout.FitProblem(session.Lot);
            if (problem == null && SlotOverlapsBuilding(session.Lot, layout))
                problem = "Keep the building footprint clear of fixed buildings.";
            if (problem != null)
            {
                Refresh(problem);
                return;
            }
            session.Edit(lot => lot.zoningLayout = layout);
            Refresh("Building footprint updated.");
        }

        private void OpenZoningLayout()
        {
            if (assetPopup || PlayerMode) return;
            CloseChoices();
            var lot = session.Lot;
            var layout = lot.zoningLayout;
            int zone = layout.Configured ? (int)layout.zone - 1 : 0;
            int use = zone / 3, density = zone % 3, wealth = layout.Configured ? (int)layout.wealth - 1 : 0;
            var slot = layout.Configured ? layout.BuildingSlot : new Rect(lot.LandBounds.center - lot.LandBounds.size * .25f,
                lot.LandBounds.size * .5f);
            var building = Selection;
            var asset = building != null && catalog ? catalog.Find(building.assetId) : null;
            var frame = CreateModal("Growable layout");
            Place(frame, Vector2.one * .5f, Vector2.one * .5f,
                new Vector2(-280, -340), new Vector2(280, 340));
            Label(frame, "Variable building slot · meters from the lot center", 20, 220, 520, 17);
            var widthField = SlotField("Width", 20, 253, slot.width);
            var depthField = SlotField("Depth", 285, 253, slot.height);
            var xField = SlotField("Center X", 20, 298, slot.center.x);
            var zField = SlotField("Center Z", 285, 298, slot.center.y);
            bool replaceBuilding = false;
            if (asset?.category == "Buildings")
                Control(frame, "Use selected building as slot", 20, 348, 520, () =>
                {
                    var bounds = LotRuntimeLoader.Footprint(building, asset);
                    widthField.text = MeterText(bounds.width);
                    depthField.text = MeterText(bounds.height);
                    xField.text = MeterText(bounds.center.x);
                    zField.text = MeterText(bounds.center.y);
                    replaceBuilding = true;
                }, description: "Use this building's footprint and remove the model when applying");
            var previewBuildingId = layout.previewBuildingId;
            var previewButton = Control(frame, "Preview building", 282, 397, 258, () => { },
                description: "Cycle compatible preview buildings. Apply to see the model at 70% opacity.");
            var previewLabel = previewButton.GetComponentInChildren<Text>();
            previewButton.onClick.AddListener(CyclePreviewBuilding);
            UpdatePreviewChoice();
            Choices("Zone use", new[] { "Residential", "Commercial", "Industrial" }, 79, use, value => use = value);
            Choices("Density", new[] { "1", "2", "3" }, 123, density, value => density = value);
            Choices("Wealth", new[] { "$", "$$", "$$$" }, 167, wealth, value => wealth = value);
            var frontage = layout.frontage;
            Label(frame, "Frontage", 20, 397, 88, 16);
            var frontageButton = Control(frame, frontage.ToString(), 115, 397, 154, () => { });
            var frontageLabel = frontageButton.GetComponentInChildren<Text>();
            frontageButton.onClick.AddListener(() =>
            {
                frontage = (LotFrontage)(((int)frontage + 1) % 4);
                frontageLabel.text = frontage.ToString();
            });
            Label(frame, "Entrance coordinates", 20, 445, 260, 16);
            var entranceX = SlotField("House X", 20, 477, layout.hasEntrance ? layout.entrance.x : (float?)null);
            var entranceZ = SlotField("House Z", 285, 477, layout.hasEntrance ? layout.entrance.y : (float?)null);
            var drivewayX = SlotField("Driveway X", 20, 520, layout.hasDriveway ? layout.driveway.x : (float?)null);
            var drivewayZ = SlotField("Driveway Z", 285, 520, layout.hasDriveway ? layout.driveway.y : (float?)null);
            var error = Label(frame, "", 20, 569, 520, 16);
            At(error.rectTransform, 20, 569, 520, 49);
            error.color = Gold;
            Control(frame, "Place entry marker", 285, 435, 248,
                () => ApplyLayout(placeEntryMarker: true));
            Control(frame, "Choose which buildings can be placed here", 20, 622, 345, () =>
            {
                if (string.IsNullOrEmpty(lot.firstEraId))
                {
                    error.text = "Set the lot’s era in the Lot Editor before choosing buildings.";
                    return;
                }
                ApplyLayout(openBuildingChoices: true);
            });
            Control(frame, "Apply layout", 373, 622, 167, () => ApplyLayout(), LotGlyph.Check);

            void Choices(string title, string[] labels, int y, int initial, Action<int> select)
            {
                Label(frame, title, 20, y, 100, 16);
                var buttons = new Button[labels.Length];
                for (int i = 0; i < labels.Length; i++)
                {
                    int value = i;
                    buttons[i] = Control(frame, labels[i], 124 + i * 140, y, 132, () =>
                    {
                        select(value);
                        UpdatePreviewChoice();
                        for (int j = 0; j < buttons.Length; j++) MarkActive(buttons[j], j == value);
                    });
                    MarkActive(buttons[i], i == initial);
                }
            }

            TownZone SelectedZone() => (TownZone)(use * 3 + density + 1);
            LotWealth SelectedWealth() => (LotWealth)(wealth + 1);

            LotAssetCatalogEntry[] PreviewChoices() => catalog ? catalog.entries
                .Where(item => item != null && layout.AllowsGrowthBuilding(item.id) && item.CanPreviewGrowable(SelectedZone(), SelectedWealth(), lot.firstEraId))
                .OrderBy(item => item.label).ToArray() : Array.Empty<LotAssetCatalogEntry>();

            void UpdatePreviewChoice()
            {
                var choices = PreviewChoices();
                var choice = Array.Find(choices, item => item.id == previewBuildingId);
                previewBuildingId = choice?.id;
                previewButton.interactable = choices.Length > 0;
                previewLabel.text = choice != null ? choice.label :
                    choices.Length > 0 ? "Preview building: None" : "No compatible buildings";
            }

            void CyclePreviewBuilding()
            {
                var choices = PreviewChoices();
                int next = Array.FindIndex(choices, item => item.id == previewBuildingId) + 1;
                previewBuildingId = next < choices.Length ? choices[next].id : null;
                UpdatePreviewChoice();
            }

            InputField SlotField(string title, int x, int y, float? value)
            {
                Label(frame, title, x, y, 88, 16);
                var field = Field(frame, "m", x + 95, y, 154);
                field.gameObject.name = title + " input";
                field.contentType = InputField.ContentType.DecimalNumber;
                field.text = value.HasValue ? MeterText(value.Value) : "";
                return field;
            }

            void ApplyLayout(bool openBuildingChoices = false, bool placeEntryMarker = false)
            {
                if (!ReadMeters(widthField, out float w) || !ReadMeters(depthField, out float d) ||
                    !ReadMeters(xField, out float x) || !ReadMeters(zField, out float z))
                {
                    error.text = "Enter width, depth and center coordinates in meters.";
                    return;
                }
                if (!ReadAnchor(entranceX, entranceZ, out bool hasEntrance, out var entrance) ||
                    !ReadAnchor(drivewayX, drivewayZ, out bool hasDriveway, out var driveway))
                {
                    error.text = "Enter both coordinates for each entrance you want to use.";
                    return;
                }
                var next = new LotZoningLayout
                {
                    zone = SelectedZone(), wealth = SelectedWealth(), previewBuildingId = previewBuildingId,
                    restrictGrowthBuildings = layout.restrictGrowthBuildings,
                    growthBuildingIds = layout.growthBuildingIds,
                    previewYawOffset = previewBuildingId == layout.previewBuildingId ? layout.previewYawOffset : 0,
                    previewScale = previewBuildingId == layout.previewBuildingId ? layout.previewScale : 1f,
                    buildingCenter = new Vector2(x, z), buildingSize = new Vector2(w, d),
                    frontage = frontage, hasEntrance = hasEntrance, entrance = entrance,
                    hasDriveway = hasDriveway, driveway = driveway
                };
                var fitProblem = next.Valid ? next.FitProblem(lot) : "Enter a positive width and depth for the building slot.";
                if (fitProblem != null)
                {
                    error.text = fitProblem;
                    return;
                }
                if (SlotOverlapsBuilding(lot, next, replaceBuilding ? building : null))
                {
                    error.text = "Move fixed buildings out of the variable slot.";
                    return;
                }
                session.Edit(item =>
                {
                    item.zoningLayout = next;
                    if (replaceBuilding) item.objects.Remove(building);
                });
                if (replaceBuilding) selected = null;
                CloseAssetPopup();
                if (placeEntryMarker)
                {
                    armed = null;
                    pickingLotEntrance = true;
                    Refresh();
                    return;
                }
                if (openBuildingChoices)
                {
                    OpenGrowthBuildingChoices();
                    return;
                }
                var preview = catalog && !string.IsNullOrEmpty(previewBuildingId) ? catalog.Find(previewBuildingId) : null;
                string previewProblem = null;
                if (preview != null)
                {
                    var placement = next.hasEntrance
                        ? LotGrowableBuildings.PlaceInSlot(next, preview) : next.BuildingPreview();
                    previewProblem = placement == null ? $"Set an entry point in Building info for {preview.label}."
                        : next.BuildingFitProblem(lot, placement, preview, catalog);
                }
                Refresh(previewProblem ?? $"{next.Label} growable layout ready to save. Building fit allows 10% extra.");
            }

            static bool ReadAnchor(InputField x, InputField z, out bool present, out Vector2 point)
            {
                point = default;
                present = !string.IsNullOrWhiteSpace(x.text) || !string.IsNullOrWhiteSpace(z.text);
                if (!present) return true;
                if (!ReadMeters(x, out float east) || !ReadMeters(z, out float north)) return false;
                point = new Vector2(east, north);
                return float.IsFinite(east) && float.IsFinite(north);
            }

            static string MeterText(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
            static bool ReadMeters(InputField field, out float value) =>
                float.TryParse(field.text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private void OpenGrowthBuildingChoices()
        {
            var lot = session.Lot;
            var layout = lot.zoningLayout;
            var era = CityForgeTimeline.Eras.FirstOrDefault(period => period.id == lot.firstEraId);
            if (!layout.Configured || era == null) return;

            var frame = CreateModal("Choose which buildings can be placed here", 660, 570);
            string use = (int)layout.zone <= 3 ? "Residential" : (int)layout.zone <= 6 ? "Commercial" : "Industrial";
            Label(frame, $"{era.Name} / {use} / {layout.Label}", 20, 57, 620, 18);
            Label(frame, "Growth randomly picks from the checked buildings that fit.", 20, 91, 620, 16);
            var feedback = Label(frame, "", 20, 489, 620, 16);
            CreatePickerResults(frame, 134, 82);

            var eligible = catalog ? catalog.entries
                .Where(asset => asset != null && asset.CanPreviewGrowable(layout.zone, layout.wealth, era.id))
                .OrderBy(asset => asset.label, StringComparer.OrdinalIgnoreCase).ToArray()
                : Array.Empty<LotAssetCatalogEntry>();
            var selectedIds = new HashSet<string>(eligible.Where(asset => layout.AllowsGrowthBuilding(asset.id))
                .Select(asset => asset.id));
            int y = 6;
            foreach (var asset in eligible)
            {
                var row = Checkbox(assetList, asset.id, asset.label, 6, y, 608, selectedIds.Contains(asset.id));
                row.onValueChanged.AddListener(value =>
                {
                    if (value) selectedIds.Add(asset.id);
                    else selectedIds.Remove(asset.id);
                    feedback.text = "";
                });
                var thumbnail = new GameObject("Building thumbnail", typeof(RectTransform), typeof(RawImage))
                    .GetComponent<RawImage>();
                thumbnail.transform.SetParent(row.transform, false);
                At(thumbnail.rectTransform, 574, 2, 28, 28);
                thumbnail.raycastTarget = false;
                thumbnail.texture = thumbnails.Get(asset);
                y += 37;
            }
            assetList.sizeDelta = new Vector2(0, y);
            if (eligible.Length == 0)
                Label(assetList, "No buildings match this era, zoning and wealth.", 8, 6, 590, 16);

            Control(frame, "Back to layout", 20, 524, 175, () =>
            {
                CloseAssetPopup();
                OpenZoningLayout();
            });
            Control(frame, "Apply building choices", 418, 524, 222, () =>
            {
                if (selectedIds.Count == 0)
                {
                    feedback.text = "Choose at least one building.";
                    return;
                }
                layout.restrictGrowthBuildings = true;
                layout.growthBuildingIds = eligible.Where(asset => selectedIds.Contains(asset.id))
                    .Select(asset => asset.id).ToArray();
                if (!layout.AllowsGrowthBuilding(layout.previewBuildingId))
                    layout.previewBuildingId = "";
                session.Edit(draft => draft.zoningLayout = layout);
                CloseAssetPopup();
                OpenZoningLayout();
            }, LotGlyph.Check).interactable = eligible.Length > 0;
        }

        private bool SlotOverlapsBuilding(LotDefinition lot, LotZoningLayout layout, LotObjectDefinition except = null)
        {
            if (!catalog) return false;
            foreach (var item in lot.objects)
            {
                if (item == except) continue;
                var asset = catalog.Find(item.assetId);
                if (asset?.category == "Buildings" && layout.BuildingSlot.Overlaps(LotRuntimeLoader.Footprint(item, asset)))
                    return true;
            }
            return false;
        }

        private void OpenWaterfrontRows()
        {
            if (assetPopup) return;
            CloseChoices();
            var frame = CreateModal("Waterfront rows");
            At(Label(frame, "Choose how many 4 m rows extend over water. Keep at least one row on land.",
                20, 65, 520, 16).rectTransform, 20, 65, 520, 58);
            int depth = session.Lot.IsWaterfront ? session.Lot.WaterfrontDepth : session.Lot.length;
            for (int i = 0; i < depth; i++)
            {
                int rows = i;
                var option = Control(frame, rows == 0 ? "None" : rows.ToString(),
                    20 + i % 8 * 65, 135 + i / 8 * 44, 61, () =>
                    {
                        session.Edit(lot =>
                        {
                            if (rows > 0 && !lot.IsWaterfront) lot.waterfrontSide = LotWaterfrontSide.South;
                            lot.waterfrontRows = rows;
                        });
                        CloseAssetPopup();
                    });
                MarkActive(option, rows == session.Lot.waterfrontRows);
            }
            var traffic = Control(frame, "Attracts ship vessels", 20, 325, 290, () =>
            {
                session.Edit(lot => lot.harborVesselTraffic = lot.AttractsVessels
                    ? HarborVesselTraffic.Disabled : HarborVesselTraffic.Enabled);
                CloseAssetPopup();
                OpenWaterfrontRows();
            }, description: "Allow ambient vessel visits on a coast or major river");
            traffic.interactable = session.Lot.IsWaterfront;
            traffic.GetComponentInChildren<Text>().text = session.Lot.AttractsVessels
                ? "Attracts ship vessels: On" : "Attracts ship vessels: Off";
        }

        private void CloseChoices()
        {
            if (!choices) return;
            choices.SetActive(false);
            Destroy(choices);
            choices = null;
        }

        private void OpenAssetPopup(string filter, string title)
        {
            if (assetPopup) return;
            CloseChoices();
            StopOverlay();
            category = filter;
            Refresh(rebuildPreview: false);
            if (drawingPath) FinishGroundPath(false);
            if (filter == "Buildings")
            {
                OpenBuildingPicker();
                return;
            }
            var frame = CreatePicker(title, "Search by asset name or category", out var search);

            bool fencePicker = filter == "Fences";
            if (fencePicker)
                Control(frame.transform, fencePath.Finished ? "Redraw fence path" : "Draw fence path",
                    309, 103, 231, StartFencePath, LotGlyph.Fence, "Click markers to sketch a fence route");
            var toggle = Checkbox(frame, "Vary placement size", "Vary placement size (up to ±15%)",
                20, 103, fencePicker ? 278 : 520, varyPlacementSize);
            toggle.onValueChanged.AddListener(value => varyPlacementSize = value);
            var variation = toggle.gameObject;
            if (filter == "Characters") variation.gameObject.SetActive(false);
            if (fencePicker && fencePath.Finished)
            {
                variation.gameObject.SetActive(false);
                Label(frame.transform, "Choose a style to fill the guide", 20, 103, 280, 16);
            }

            if (fencePicker)
            {
                Control(frame.transform, "Select all sections", 20, 147, 256, SelectAllFenceSections);
                Control(frame.transform, "Surround lot", 284, 147, 256, SurroundLotWithFence);
            }

            CreatePickerResults(frame, fencePicker ? 191 : 147);
            BindPickerSearch(search, PopulateAssets);
        }

        private RectTransform CreatePicker(string title, string placeholder, out InputField search)
        {
            var frame = CreateModal(title);
            search = Field(frame, placeholder, 20, 58, 520);
            return frame;
        }

        private RectTransform CreateModal(string title, float width = 560, float height = 540)
        {
            ClearRepeat();
            assetPopupOpener = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;

            var shade = AddImage("Asset picker", transform, new Color32(4, 9, 15, 192));
            shade.raycastTarget = true;
            Place(shade.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            assetPopup = shade.gameObject;
            viewport.SetPlacementGhost(null, null);
            var frame = AddImage("Picker window", shade.transform, PanelColor);
            Place(frame.rectTransform, Vector2.one * .5f, Vector2.one * .5f,
                new Vector2(-width, -height) * .5f, new Vector2(width, height) * .5f);
            AddOutline(frame, Gold);
            Label(frame.transform, title, 20, 9, width - 120, 24).font = headingFont;
            Control(frame.transform, "Close", width - 92, 10, 72, CloseAssetPopup);
            return frame.rectTransform;
        }

        private void CreatePickerResults(Transform frame, float top, float bottom = 18f)
        {
            var scrollImage = AddImage("Asset results", frame.transform, new Color32(10, 18, 26, 255));
            scrollImage.raycastTarget = true;
            Place(scrollImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, bottom), new Vector2(-20, -top));
            var scrollView = scrollImage.rectTransform;
            scrollView.gameObject.AddComponent<RectMask2D>();
            assetScroll = scrollView.gameObject.AddComponent<ScrollRect>();
            assetScroll.horizontal = false;
            assetScroll.movementType = ScrollRect.MovementType.Clamped;
            assetScroll.scrollSensitivity = 32;
            assetScroll.viewport = scrollView;
            assetList = AddImage("Asset entries", scrollView, Color.clear).rectTransform;
            assetList.anchorMin = Vector2.up;
            assetList.anchorMax = Vector2.one;
            assetList.pivot = Vector2.up;
            assetList.anchoredPosition = Vector2.zero;
            assetList.sizeDelta = Vector2.zero;
            assetScroll.content = assetList;
        }

        private static void BindPickerSearch(InputField search, UnityAction<string> populate)
        {
            search.onValueChanged.AddListener(populate);
            populate("");
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(search.gameObject);
            search.ActivateInputField();
        }

        private void CloseAssetPopup()
        {
            if (!assetPopup) return;
            assetPopup.SetActive(false);
            Destroy(assetPopup);
            assetPopup = null;
            assetList = null;
            assetScroll = null;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(assetPopupOpener);
            assetPopupOpener = null;
            Refresh();
        }

        private void PopulateAssets(string query)
        {
            var allowed = catalog ? catalog.entries.Where(asset => asset != null && asset.prefab &&
                (!PlayerMode || LotCustomizationService.IsCosmetic(catalog, asset.id)) &&
                (category.Length == 0 ? asset.category != "Buildings" : asset.category == category) &&
                (!fencePath.Finished || category != "Fences" || asset.fenceSpan > 0)).ToArray() : Array.Empty<LotAssetCatalogEntry>();
            var recents = category == "Landscaping" ? System.Array.Empty<string>() : recentAssets.ToArray();
            PopulatePicker(query, allowed, recents, item => item.id, item => item.label,
                item => PickerGroup(item),
                item => item.label + " " + item.category + " " + PickerGroup(item) +
                    (item.IsCharacter ? $" {item.fromYear} {item.toYear} NPC" : ""), thumbnails.Get,
                item => CategoryGlyph(item.category), item => item.id == armed, item =>
                {
                    if (fencePath.Finished && item.fenceSpan > 0)
                    {
                        FillFencePath(item);
                        return;
                    }
                    ArmAsset(item);
                }, item => fencePath.Finished && item.fenceSpan > 0
                    ? "Fill the fence path with " + item.label : "Place " + item.label + " on the lot",
                !catalog ? "Local catalog unavailable" : "No matching assets");
        }

        private static string PickerGroup(LotAssetCatalogEntry asset) =>
            (asset.IsCharacter ? CityForgeTimeline.Find(asset.eraId)?.Name : null) ??
            (string.IsNullOrEmpty(asset.group) ? asset.category : asset.group);

        private void ArmAsset(LotAssetCatalogEntry item)
        {
            armed = item.id;
            placementYaw = 0;
            placementScale = item.DefaultPlacementScale;
            if (CanRotatePlacement(item) && varyPlacementSize)
            {
                var preview = new LotObjectDefinition { assetId = item.id, scale = placementScale };
                ApplyPlacementVariation(preview);
                placementScale = preview.scale;
            }
            if (item.IsCharacter || CanRotatePlacement(item)) selected = null;
            selectedPath = null;
            moving = false;
            CloseAssetPopup();
        }

        private bool pickingLotEntrance;
        private void OpenBuildingInfo(LotAssetCatalogEntry asset, Action returnToBuildings)
        {
            CloseAssetPopup();
            var frame = CreateModal("Building info · " + asset.label, 1040, 660);
            var saved = LotBuildingInfo.Load(asset.id);
            var facing = saved?.facing ?? LotFrontage.South;
            var type = saved?.entryType ?? LotEntryType.Pedestrian;
            var entrance = saved?.entrance ?? new Vector2(0f, -asset.footprint.y * .5f);
            float halfSpan = Mathf.Max(1f, Mathf.Max(asset.footprint.x, asset.footprint.y) * .65f);
            halfSpan = Mathf.Max(halfSpan, Mathf.Max(Mathf.Abs(entrance.x), Mathf.Abs(entrance.y)) * 1.15f);

            var plan = new GameObject("Building plan", typeof(RectTransform), typeof(RawImage), typeof(LotEntryPlanInput))
                .GetComponent<RawImage>();
            plan.transform.SetParent(frame, false);
            At(plan.rectTransform, 20, 68, 560, 560);
            plan.texture = thumbnails.GetPlan(asset, halfSpan);
            var marker = AddImage("Entry point", plan.transform, Gold);
            marker.raycastTarget = false;
            var widthLine = AddImage("Entry width", plan.transform, Gold);
            widthLine.raycastTarget = false;

            Label(frame, "Drag the marker to the doorway or driveway.", 610, 70, 408, 18);
            var orientation = Label(frame, "North is up · East is right", 610, 109, 350, 16);
            string[] directions = { "North is up · East is right", "North is right · East is down",
                "North is down · East is left", "North is left · East is up" };
            int planTurns = 0;
            Control(frame, "Rotate plan", 973, 109, 42, () =>
            {
                planTurns = (planTurns + 1) % 4;
                plan.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f * planTurns);
                orientation.text = directions[planTurns];
            }, LotGlyph.Rotate, "Rotate plan clockwise 90 degrees", iconOnly: true);
            Label(frame, "Entry type", 610, 167, 185, 17);
            var typeButton = Control(frame, type.ToString(), 805, 167, 210, () => { });
            Label(frame, "Entry width (m)", 610, 222, 185, 17);
            var width = Field(frame, "Width in meters", 805, 222, 210);
            width.contentType = InputField.ContentType.DecimalNumber;
            width.text = (saved != null && saved.width > 0f ? saved.width : DefaultWidth()).ToString(CultureInfo.InvariantCulture);
            Label(frame, "Entry faces", 610, 277, 185, 17);
            var side = Control(frame, facing.ToString(), 805, 277, 210, () => { });
            var coordinates = Label(frame, "", 610, 333, 408, 16);
            var feedback = Label(frame, "", 610, 390, 408, 16);
            At(feedback.rectTransform, 610, 390, 408, 66);

            typeButton.onClick.AddListener(() =>
            {
                type = type == LotEntryType.Pedestrian ? LotEntryType.Vehicle : LotEntryType.Pedestrian;
                typeButton.GetComponentInChildren<Text>().text = type.ToString();
                width.text = DefaultWidth().ToString(CultureInfo.InvariantCulture);
            });
            side.onClick.AddListener(() =>
            {
                facing = (LotFrontage)(((int)facing + 1) % 4);
                side.GetComponentInChildren<Text>().text = facing.ToString();
                UpdateMarker();
            });
            width.onValueChanged.AddListener(_ => UpdateMarker());
            plan.GetComponent<LotEntryPlanInput>().PlaceEntry = uv =>
            {
                entrance = (uv * 2f - Vector2.one) * halfSpan;
                feedback.text = "";
                UpdateMarker();
            };

            Control(frame, "Save building info", 805, 582, 210, () =>
            {
                if (!ReadWidth(out float meters))
                {
                    feedback.text = "Enter a positive width.";
                    return;
                }
                try
                {
                    new LotBuildingInfo { entrance = entrance, facing = facing, entryType = type, width = meters }.Save(asset.id);
                    CloseAssetPopup();
                    Refresh($"Saved entry point for {asset.label}.");
                    returnToBuildings();
                }
                catch (Exception error) when (error is System.IO.IOException || error is UnauthorizedAccessException)
                {
                    feedback.text = "Could not save building info.";
                    Debug.LogWarning(error.Message);
                }
            });
            UpdateMarker();

            float DefaultWidth() => type == LotEntryType.Vehicle ? 3f : 1f;
            bool ReadWidth(out float meters) => float.TryParse(width.text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out meters) && float.IsFinite(meters) && meters > 0f;
            void UpdateMarker()
            {
                var uv = entrance / (2f * halfSpan) + Vector2.one * .5f;
                Place(marker.rectTransform, uv, uv, -Vector2.one * 6, Vector2.one * 6);
                float halfWidth = ReadWidth(out float meters) ? Mathf.Min(meters / (4f * halfSpan), .5f) : 0f;
                bool horizontal = facing is LotFrontage.North or LotFrontage.South;
                var direction = horizontal ? Vector2.right : Vector2.up;
                var thickness = horizontal ? new Vector2(0, 1.5f) : new Vector2(1.5f, 0);
                Place(widthLine.rectTransform, uv - direction * halfWidth, uv + direction * halfWidth, -thickness, thickness);
                coordinates.text = $"Entry position: X {entrance.x:0.00} m · Z {entrance.y:0.00} m";
            }
        }

        private void OpenBuildingPicker(CityForgeTimeline.Era era = null, LotContentCatalog.Category use = null,
            string query = "", bool editingInfo = false)
        {
            var frame = CreateModal("Buildings");
            var breadcrumb = Label(frame, "", 112, 58, 428, 17);
            var search = Field(frame, "Search buildings", 20, 103, 520);
            var variation = Checkbox(frame, "Vary placement size", "Vary placement size (up to ±15%)",
                20, 145, 520, varyPlacementSize);
            variation.onValueChanged.AddListener(value => varyPlacementSize = value);
            var editInfo = Checkbox(frame, "Edit building info", "Edit building info instead of placing", 20, 185, 520, editingInfo);
            editInfo.gameObject.SetActive(!PlayerMode);
            CreatePickerResults(frame, PlayerMode ? 187 : 226);
            search.SetTextWithoutNotify(query);
            var uses = LotClassification.Categories;
            Button back = null;
            back = Control(frame, "Back", 20, 58, 80, Back, LotGlyph.Back);
            search.onValueChanged.AddListener(_ => Render());
            Render();

            void Back()
            {
                if (use != null) use = null;
                else era = null;
                search.SetTextWithoutNotify("");
                Render();
            }

            void Render()
            {
                back.gameObject.SetActive(era != null);
                search.gameObject.SetActive(use != null);
                variation.gameObject.SetActive(use != null);
                breadcrumb.text = era == null ? "Choose an era" : use == null ? era.Name + " · Choose a category" : era.Name + " / " + use.name;
                if (use != null)
                {
                    var choices = catalog ? catalog.entries.Where(item => item != null && item.prefab &&
                        item.category == "Buildings" && LotClassification.FindCategory(item.group) == use &&
                        (string.IsNullOrEmpty(item.eraId) ? item.fromYear <= era.LastYear && item.toYear >= era.FirstYear
                            : item.eraId == era.id)).ToArray()
                        : Array.Empty<LotAssetCatalogEntry>();
                    var uncategorizedRecents = recentAssets.Where(id =>
                        Array.Exists(choices, item => item.id == id && string.IsNullOrEmpty(item.subcategory))).ToArray();
                    string Subcategory(LotAssetCatalogEntry item) =>
                        item.growableZone != TownZone.None ? item.GrowableLabel :
                        LotClassification.SubcategoryName(use.id, item.subcategory) ?? "";
                    PopulatePicker(search.text, choices, uncategorizedRecents, item => item.id, item => item.label,
                        Subcategory, item => item.label + " " + Subcategory(item),
                        thumbnails.Get, _ => LotGlyph.House, _ => false, item =>
                        {
                            if (editInfo.isOn && !PlayerMode)
                            {
                                var previousQuery = search.text;
                                OpenBuildingInfo(item, () => OpenBuildingPicker(era, use, previousQuery, editingInfo: true));
                            }
                            else ArmAsset(item);
                        },
                        item => "Preview and place " + item.label, "No buildings in this category");
                    return;
                }
                ClearPickerRows();
                int y = 5;
                if (era == null)
                {
                    foreach (var period in CityForgeTimeline.Eras)
                    {
                        Control(assetList, $"{period.Name}   {period.FirstYear}–{period.LastYear}", 8, y, 504,
                            () => { era = period; Render(); });
                        y += 44;
                    }
                }
                else
                {
                    foreach (var option in uses)
                    {
                        Control(assetList, option.name, 8, y, 504, () => { use = option; Render(); });
                        y += 44;
                    }
                }
                assetList.sizeDelta = new Vector2(0, y + 5);
                assetList.anchoredPosition = Vector2.zero;
                assetScroll.StopMovement();
            }
        }

        private bool CanScript(LotObjectDefinition item) =>
            CanEdit(item) && catalog && catalog.Find(item.assetId)?.IsCharacter == true;

        private void OpenCapitalSettings()
        {
            if (PlayerMode || assetPopup) return;
            CloseChoices();
            var frame = CreateModal("Requirements & Bonus");
            var lot = session.Lot;
            Label(frame, "Site requirements · all selected items are required", 20, 49, 520, 17);
            var resourceNames = new[]
            {
                "Timber", "Stone", "Iron", "Coal", "Fish", "Fertile soil", "Pasture",
                "Fresh water", "Natural harbor", "Scenic beauty", "Clay"
            };
            var resources = new Toggle[resourceNames.Length];
            for (int i = 0; i < resources.Length; i++)
            {
                var name = resourceNames[i];
                resources[i] = Checkbox(frame, name, name, 20 + i % 2 * 264, 88 + i / 2 * 34, 256,
                    lot.requiredResources.Contains((LotRequiredResource)i));
            }
            var kiln = Checkbox(frame, "Kiln access", "Kiln access", 284, 258, 256, lot.requiresKiln);

            Label(frame, "Capital bonus", 20, 292, 520, 18);
            var menu = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            menu.name = "Capital bonus type";
            menu.transform.SetParent(frame, false);
            At((RectTransform)menu.transform, 20, 329, 520, 33);
            foreach (var label in menu.GetComponentsInChildren<Text>(true)) label.font = font;
            var effect = menu.GetComponent<Dropdown>();
            effect.ClearOptions();
            effect.AddOptions(new List<string>
            {
                "None", "Early Works · public construction discount", "Local Justice · crime reduction",
                "Public Confidence · civic prestige", "Sturdy Foundations · public repair discount",
                "Port Administration · port fee revenue", "Population Growth · faster population growth"
            });
            effect.SetValueWithoutNotify((int)lot.capitalBonus.effect);

            var percentLabel = Label(frame, "Bonus (%)", 20, 369, 76, 16);
            var percent = Field(frame, "Bonus percentage", 96, 369, 78);
            var usesLabel = Label(frame, "Buildings (0 = ongoing)", 195, 369, 233, 16);
            var uses = Field(frame, "Buildings", 434, 369, 106);
            percent.contentType = uses.contentType = InputField.ContentType.IntegerNumber;
            percent.characterLimit = 3;
            uses.characterLimit = 2;
            percent.SetTextWithoutNotify(lot.capitalBonus.percent.ToString());
            uses.SetTextWithoutNotify(lot.capitalBonus.eligibleBuildings.ToString());
            var port = Checkbox(frame, "Operating port", "Bonus requires an operating port", 20, 416, 520,
                lot.capitalBonus.requiresOperatingPort);
            var status = Label(frame, "Saved as lot settings for future gameplay.", 20, 479, 314, 14);
            Control(frame, "Cancel", 343, 479, 91, CloseAssetPopup);
            MarkActive(Control(frame, "Apply", 445, 479, 95, ApplySettings, LotGlyph.Check), true);

            effect.onValueChanged.AddListener(value =>
            {
                int[] defaults = { 0, 15, 25, 10, 20, 15, 10 };
                percent.SetTextWithoutNotify(defaults[value].ToString());
                uses.SetTextWithoutNotify(value == (int)LotCapitalEffect.PublicBuildingCostReduction ? "3" : "0");
                port.SetIsOnWithoutNotify(value == (int)LotCapitalEffect.PortFeeRevenueIncrease);
                RefreshBonusFields();
            });
            RefreshBonusFields();

            void RefreshBonusFields()
            {
                bool enabled = effect.value != (int)LotCapitalEffect.None;
                percent.gameObject.SetActive(enabled);
                percentLabel.gameObject.SetActive(enabled);
                port.gameObject.SetActive(enabled);
                bool limited = effect.value == (int)LotCapitalEffect.PublicBuildingCostReduction;
                uses.gameObject.SetActive(limited);
                usesLabel.gameObject.SetActive(limited);
            }

            void ApplySettings()
            {
                var bonus = new LotCapitalBonus { effect = (LotCapitalEffect)effect.value };
                if (bonus.effect != LotCapitalEffect.None)
                {
                    bool validPercent = int.TryParse(percent.text, out bonus.percent);
                    bool validUses = bonus.effect != LotCapitalEffect.PublicBuildingCostReduction ||
                        int.TryParse(uses.text, out bonus.eligibleBuildings);
                    bonus.requiresOperatingPort = port.isOn;
                    if (!validPercent || !validUses || !bonus.IsValid)
                    {
                        status.text = "Use 1–100% and 0–99 buildings.";
                        status.color = Gold;
                        return;
                    }
                }
                session.Edit(definition =>
                {
                    definition.requiredResources = resources.Select((toggle, index) => (toggle, index))
                        .Where(item => item.toggle.isOn).Select(item => (LotRequiredResource)item.index).ToList();
                    definition.requiresKiln = kiln.isOn;
                    definition.capitalBonus = bonus;
                });
                CloseAssetPopup();
                Refresh("Lot requirements and bonus applied.", rebuildPreview: false);
            }
        }

        private void OpenCharacterScript()
        {
            var character = Selection;
            if (assetPopup || !CanScript(character)) return;
            CloseChoices();
            var settings = character.characterScript;
            if (!settings.configured)
            {
                settings.configured = true;
                settings.idlePercent = LotCharacterScript.Default.idlePercent;
                settings.walkTalkPercent = LotCharacterScript.Default.walkTalkPercent;
            }
            var frame = CreateModal("Character Script");
            Label(frame, catalog.Find(character.assetId).label, 20, 49, 520, 17);
            Button randomizedTab = null, scriptedTab = null, apply = null;
            var randomized = AddImage("Randomized settings", frame, PanelColor).rectTransform;
            At(randomized, 20, 146, 520, 316);
            var scripted = AddImage("Scripted settings", frame, PanelColor).rectTransform;
            At(scripted, 20, 146, 520, 250);
            var dockWorker = Checkbox(scripted, "Dock Worker", "Dock Worker", 0, 0, 520,
                settings.scriptedBehavior == LotScriptedBehavior.DockWorker);
            var dockNote = Label(scripted, "Starts when a visiting ship is berthed at this harbor lot.",
                8, 47, 495, 16);
            At(dockNote.rectTransform, 8, 47, 495, 30);
            var dockOptions = AddImage("Dock worker options", scripted, PanelColor).rectTransform;
            At(dockOptions, 0, 80, 520, 182);
            var delay = NumberField(dockOptions, "Arrival delay (seconds)", settings.dockArrivalDelay, 8);
            var duration = NumberField(dockOptions, "Work time (seconds)", settings.DockWorkSeconds, 50);
            var cargo = Checkbox(dockOptions, "Move cargo", "Move cargo on deck", 14, 92, 492, !settings.dockIdleOnly);
            var stay = Checkbox(dockOptions, "Stay visible", "Stay visible on the lot between visits",
                14, 137, 492, settings.dockStayVisible);
            dockOptions.gameObject.SetActive(dockWorker.isOn);
            dockWorker.onValueChanged.AddListener(value => dockOptions.gameObject.SetActive(value));
            bool randomizedSelected = false;
            var error = Label(frame, "", 20, 483, 310, 15);
            error.color = Gold;
            randomizedTab = Control(frame, "Randomized", 20, 97, 256, () => SetTab(true));
            scriptedTab = Control(frame, "Scripted", 284, 97, 256, () => SetTab(false));

            var actions = new[] { "Idle", "Walk", "Run" };
            var actionButtons = new Button[actions.Length];
            var actionPanels = new RectTransform[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                int action = i;
                actionButtons[i] = Control(randomized, actions[i], i * 174, 0, 168, () => ShowAction(action));
                actionPanels[i] = AddImage(actions[i] + " options", randomized, ButtonColor).rectTransform;
                At(actionPanels[i], 0, 49, 520, 250);
                AddOutline(actionPanels[i].GetComponent<Image>(), Stroke);
            }

            var idle = NumberField(actionPanels[0], "Percent of time to be idle", settings.idlePercent, suffix: "%");
            var talk = NumberField(actionPanels[1], "Percent of time to talk", settings.walkTalkPercent, suffix: "%");
            idle.onValueChanged.AddListener(_ => error.text = "");
            talk.onValueChanged.AddListener(_ => error.text = "");
            var paths = Checkbox(actionPanels[1], "Stay on paths", "Stay on paths", 14, 67, 492, settings.stayOnPaths);
            var buildings = Checkbox(actionPanels[1], "Enter buildings", "Enter buildings with a designated door or entry",
                14, 112, 492, settings.enterBuildings);
            var interact = Checkbox(actionPanels[1], "Interact with NPCs", "Walk up to and interact with other NPCs",
                14, 157, 492, settings.interactWithNpcs);
            Label(actionPanels[2], "No additional settings for Run yet.", 14, 12, 492, 16);
            Control(frame, "Cancel", 343, 483, 91, CloseAssetPopup);
            apply = Control(frame, "Apply", 445, 483, 95, ApplyScript, LotGlyph.Check);
            MarkActive(apply, true);
            ShowAction(0);
            SetTab(settings.scriptedBehavior == LotScriptedBehavior.None);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(
                (randomizedSelected ? randomizedTab : scriptedTab).gameObject);

            void SetTab(bool showRandomized)
            {
                MarkActive(randomizedTab, showRandomized);
                MarkActive(scriptedTab, !showRandomized);
                randomized.gameObject.SetActive(showRandomized);
                scripted.gameObject.SetActive(!showRandomized);
                randomizedSelected = showRandomized;
                error.text = "";
            }

            void ShowAction(int action)
            {
                for (int i = 0; i < actionButtons.Length; i++)
                {
                    MarkActive(actionButtons[i], i == action);
                    actionPanels[i].gameObject.SetActive(i == action);
                }
            }

            void ApplyScript()
            {
                if (randomizedSelected && (!int.TryParse(idle.text, out settings.idlePercent) ||
                    !int.TryParse(talk.text, out settings.walkTalkPercent) || !settings.IsValid))
                {
                    error.text = "Enter percentages from 0 to 100.";
                    return;
                }
                if (!session.Lot.objects.Contains(character)) return;
                settings.scriptedBehavior = !randomizedSelected && dockWorker.isOn
                    ? LotScriptedBehavior.DockWorker : LotScriptedBehavior.None;
                if (settings.scriptedBehavior == LotScriptedBehavior.DockWorker)
                {
                    if (!int.TryParse(delay.text, out int arrivalDelay) || arrivalDelay < 0 || arrivalDelay > 60 ||
                        !int.TryParse(duration.text, out int workTime) || workTime < 1 || workTime > 120)
                    {
                        error.text = "Delay 0–60 s · Work 1–120 s";
                        return;
                    }
                    settings.dockArrivalDelay = arrivalDelay;
                    settings.dockWorkSeconds = workTime;
                    settings.dockIdleOnly = !cargo.isOn;
                    settings.dockStayVisible = stay.isOn;
                }
                settings.stayOnPaths = paths.isOn;
                settings.enterBuildings = buildings.isOn;
                settings.interactWithNpcs = interact.isOn;
                session.Edit(_ => character.characterScript = settings);
                CloseAssetPopup();
                viewport.RestartCharacterPreview(character.id);
                Refresh("Character behavior applied.", rebuildPreview: false);
            }
        }

        private InputField NumberField(Transform parent, string caption, int value, int top = 12, string suffix = "")
        {
            Label(parent, caption, 14, top, 400, 17);
            var field = Field(parent, suffix, 425, top, 81);
            field.gameObject.name = caption + " input";
            AddOutline(field.targetGraphic, Stroke);
            field.contentType = InputField.ContentType.IntegerNumber;
            field.characterLimit = 3;
            field.SetTextWithoutNotify(value.ToString());
            return field;
        }

        private void OpenGroundTexturePopup()
        {
            if (assetPopup || PlayerMode) return;
            category = "Textures";
            Refresh(rebuildPreview: false);
            CloseChoices();
            if (drawingPath) FinishGroundPath(false);
            if (drawingFence) FinishFencePath(false);
            var frame = CreatePicker("Lot Textures", "Search textures", out var search);
            CreatePickerResults(frame, 192);
            bool overlay = overlayBrush != null;
            Button baseTab = null, overlayTab = null, one = null, four = null;
            baseTab = Control(frame, "Base Textures", 20, 103, 256, () => { overlay = false; Populate(search.text); });
            overlayTab = Control(frame, "Overlay Textures", 284, 103, 256, () => { overlay = true; Populate(search.text); });
            Label(frame, "Repeat over", 20, 146, 102, 16);
            one = Control(frame, "8 m", 126, 145, 90, () => SetSpan(1));
            four = Control(frame, "16 m", 222, 145, 146, () => SetSpan(2));
            BindPickerSearch(search, Populate);

            void SetSpan(int span)
            {
                if (overlay) overlayBrushSpan = span;
                else session.Edit(lot => lot.baseTextureSpan = span);
                Refresh();
                Populate(search.text);
            }

            void Populate(string query)
            {
                var lot = session.Lot;
                int span = overlay ? overlayBrushSpan : lot.baseTextureSpan;
                MarkActive(baseTab, !overlay);
                MarkActive(overlayTab, overlay);
                MarkActive(one, span == 1);
                MarkActive(four, span == 2);
                PopulatePicker(query, overlay ? LotGroundTexture.Overlay : LotGroundTexture.Base,
                    Array.Empty<string>(), item => item.Id, item => item.Label, _ => "", item => item.Label,
                    item => item.Texture, _ => LotGlyph.Textures,
                    item => item.Id == (overlay ? overlayBrush?.Id : lot.baseTexture), item =>
                    {
                        if (overlay)
                        {
                            StopOverlay();
                            if (item.Id.Length == 0)
                            {
                                session.Edit(draft =>
                                {
                                    draft.overlayTexture = "";
                                    draft.overlays.Clear();
                                });
                            }
                            else
                            {
                                EnterTerrain();
                                overlayBrush = item;
                            }
                            CloseAssetPopup();
                            return;
                        }
                        session.Edit(draft =>
                        {
                            if (draft.baseTexture != item.Id && item.DefaultSpan != 1)
                                draft.baseTextureSpan = item.DefaultSpan;
                            draft.baseTexture = item.Id;
                        });
                        Refresh();
                        Populate(search.text);
                    }, item => !overlay && item.Id.Length == 0
                        ? "Use the district terrain"
                        : overlay && item.Id.Length != 0 ? "Drag to place " + item.Label : "Apply " + item.Label,
                    "No matching textures");
            }
        }

        private void SelectTerrain()
        {
            CloseChoices();
            if (drawingPath) FinishGroundPath(false);
            if (drawingFence) FinishFencePath(false);
            StopOverlay();
            EnterTerrain();
            Refresh();
        }

        private void EnterTerrain()
        {
            category = "Terrain";
            terrainMode = true;
            selected = armed = dragged = null;
            selectedPath = null;
            moving = false;
        }

        private void StopOverlay()
        {
            terrainMode = false;
            terrainTile = null;
            overlayBrush = null;
            ClearOverlayStroke();
        }

        private void ClearOverlayStroke()
        {
            overlayLast = null;
            overlayStroke.Clear();
            viewport.ClearOverlayPreview();
        }

        private Vector2 GridPosition(Vector2 point)
        {
            var gridPoint = (point + session.Lot.Size * .5f) / LotDefinition.TileMeters;
            return new Vector2(Mathf.Clamp(gridPoint.x, 0, session.Lot.width - .0001f),
                Mathf.Clamp(gridPoint.y, 0, session.Lot.length - .0001f));
        }

        private Rect TileBounds(Vector2Int tile) => new Rect(
            (Vector2)tile * LotDefinition.TileMeters - session.Lot.Size * .5f,
            Vector2.one * LotDefinition.TileMeters);

        private bool IsOnLot(Vector2 point) => LotFenceLayout.ClampToLot(point, session.Lot.Size * .5f) == point;

        private void PaintThrough(Vector2 start, Vector2 end)
        {
            var cell = Vector2Int.FloorToInt(start);
            var target = Vector2Int.FloorToInt(end);
            var delta = end - start;
            int stepX = Math.Sign(delta.x), stepY = Math.Sign(delta.y);
            while (true)
            {
                if (overlayStroke.Add(cell)) viewport.PreviewOverlayTile(TileBounds(cell));
                if (cell == target) break;

                float x = cell.x == target.x ? float.PositiveInfinity :
                    (cell.x + (stepX > 0 ? 1 : 0) - start.x) / delta.x;
                float y = cell.y == target.y ? float.PositiveInfinity :
                    (cell.y + (stepY > 0 ? 1 : 0) - start.y) / delta.y;
                if (x <= y) cell.x += stepX;
                if (y <= x) cell.y += stepY;
            }
        }

        private void EditOverlayTiles(List<Rect> tiles, LotGroundTexture brush, int rotate = 0)
        {
            var lot = session.Lot;
            if (brush == null && string.IsNullOrEmpty(lot.overlayTexture) &&
                !lot.overlays.Any(overlay => overlay.Bounds.Overlaps(tiles[0]))) return;
            var remaining = LotGroundOverlay.Cut(lot, tiles, rotate);
            if (brush != null)
            {
                foreach (var tile in tiles)
                    remaining.Add(new LotGroundOverlay
                    {
                        texture = brush.Id, span = overlayBrushSpan, min = tile.min, max = tile.max,
                        anchored = true,
                        anchor = -lot.Size * .5f + Vector2.one * (LotGroundTexture.RepeatMeters * overlayBrushSpan / 2f)
                    });
            }
            if (remaining.Count > LotGroundOverlay.MaxCount)
            {
                Refresh("This lot has reached its overlay limit.");
                return;
            }
            session.Edit(draft =>
            {
                draft.overlayTexture = "";
                draft.overlays = remaining;
            });
            Refresh();
        }

        private void RotateOverlay(int direction)
        {
            if (terrainMode && terrainTile.HasValue && overlayStroke.Count == 0)
                EditOverlayTiles(new List<Rect> { TileBounds(terrainTile.Value) }, null, direction);
        }

        private void OpenPathMaterialPopup() => OpenPathMaterialPopup(null);

        private void OpenPathMaterialPopup(LotPathDefinition path)
        {
            if (assetPopup || PlayerMode || (path == null ? !groundPath.Finished : !session.Lot.paths.Contains(path))) return;
            CloseChoices();
            var frame = CreatePicker(path == null ? "Select path material" : "Change path material",
                "Search by material or category", out var search);
            CreatePickerResults(frame, 103);
            BindPickerSearch(search, query => PopulatePicker(query, LotPathMaterial.All.Where(item => item.IsAvailable).ToArray(), recentPathMaterials,
                item => item.Id, item => item.Label, item => item.Group, item => item.Label + " " + item.Group,
                item => item.Texture, _ => LotGlyph.Path, item => item.Id == path?.materialId,
                item => ApplyPathMaterial(path, item), item => "Pave this path with " + item.Label,
                "No matching materials"));
        }

        private void ClearPickerRows()
        {
            foreach (Transform child in assetList)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private void PopulatePicker<T>(string query, T[] allowed, IReadOnlyList<string> recents,
            Func<T, string> id, Func<T, string> label, Func<T, string> group, Func<T, string> searchable,
            Func<T, Texture> thumbnail, Func<T, LotGlyph> icon, Func<T, bool> active,
            Action<T> select, Func<T, string> description, string emptyMessage) where T : class
        {
            ClearPickerRows();

            var terms = query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            bool Matches(T item)
            {
                string text = searchable(item);
                return terms.All(term => text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var recent = recents.Select(key => Array.Find(allowed, item => id(item) == key))
                .Where(item => item != null).Take(4).Where(Matches).ToArray();
            var matches = allowed.Where(Matches).ToArray();
            int y = 5;
            if (recent.Length != 0)
            {
                Heading("Recently used");
                foreach (var item in recent) Row(item);
            }

            foreach (var section in matches.Where(item => !recent.Contains(item))
                .GroupBy(group)
                .OrderBy(section =>
                {
                    int era = Array.FindIndex(CityForgeTimeline.Eras, item => item.Name == section.Key);
                    return era < 0 ? int.MaxValue : era;
                })
                .ThenBy(section => section.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(section.Key)) Heading(section.Key);
                foreach (var item in section.OrderBy(label, StringComparer.OrdinalIgnoreCase)) Row(item);
            }
            if (matches.Length == 0)
                Heading(emptyMessage);

            assetList.sizeDelta = new Vector2(0, y + 5);
            assetList.anchoredPosition = Vector2.zero;
            assetScroll.StopMovement();

            void Heading(string title)
            {
                var text = Label(assetList, title, 12, y, 492, 17);
                text.color = Gold;
                y += 35;
            }

            void Row(T item)
            {
                var entry = Control(assetList, label(item), 8, y, 504, () => select(item), description: description(item));
                At((RectTransform)entry.transform, 8, y, 504, 62);
                var texture = thumbnail(item);
                if (texture)
                {
                    var preview = new GameObject("Asset thumbnail", typeof(RectTransform), typeof(RawImage))
                        .GetComponent<RawImage>();
                    preview.transform.SetParent(entry.transform, false);
                    preview.texture = texture;
                    preview.raycastTarget = false;
                    At(preview.rectTransform, 6, 4, 54, 54);
                }
                else AddGlyph(entry.transform, icon(item), 21, 19, 24);
                var caption = entry.GetComponentInChildren<Text>();
                caption.alignment = TextAnchor.MiddleLeft;
                caption.rectTransform.offsetMin = new Vector2(73, 0);
                MarkActive(entry, active(item));
                y += 68;
            }
        }

        private void RememberAsset(string id)
        {
            recentAssets.Remove(id);
            recentAssets.Insert(0, id);
            if (recentAssets.Count > 32) recentAssets.RemoveAt(32);
            PlayerPrefs.SetString(RecentAssetsKey, string.Join("|", recentAssets));
        }

        private void CommitEdgeBlend()
        {
            var path = selectedPath;
            if (path == null || PlayerMode) return;
            int blend = (int)edgeBlendSlider.value;
            if (session.Edit(_ => path.edgeBlend = blend))
                Refresh(rebuildPreview: false);
        }

        private void ApplyPathMaterial(LotPathDefinition path, LotPathMaterial material)
        {
            if (path == null)
            {
                FillGroundPath(material);
                return;
            }
            if (PlayerMode || !session.Lot.paths.Contains(path)) return;
            session.Edit(_ => path.materialId = material.Id);
            RememberPathMaterial(material.Id);
            CloseAssetPopup();
        }

        private void FillGroundPath(LotPathMaterial material)
        {
            if (!groundPath.Finished || PlayerMode) return;
            if (session.Lot.paths.Count >= LotPathDefinition.MaxPaths)
            {
                CloseAssetPopup();
                Refresh("This lot has reached its path limit.");
                return;
            }
            var path = new LotPathDefinition
            {
                points = groundPath.Points.ToArray(),
                closed = groundPath.Closed,
                curved = curvePath,
                width = pathWidth,
                materialId = material.Id
            };
            if (!PathFits(path))
            {
                CloseAssetPopup();
                Refresh("Keep all path markers inside the lot before applying a material.");
                return;
            }
            session.Edit(lot => lot.paths.Add(path));
            RememberPathMaterial(material.Id);
            ClearGroundPath();
            CloseAssetPopup();
            Refresh($"Placed {material.Label} path.");
        }

        private void RememberPathMaterial(string id)
        {
            recentPathMaterials.Remove(id);
            recentPathMaterials.Insert(0, id);
            if (recentPathMaterials.Count > 4) recentPathMaterials.RemoveAt(4);
            PlayerPrefs.SetString(RecentPathMaterialsKey, string.Join("|", recentPathMaterials));
        }

        private bool PathFits(LotPathDefinition path)
        {
            var half = session.Lot.Size * .5f;
            return path.points.All(point => Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.y) <= half.y);
        }

        private void StartGroundPath()
        {
            CloseChoices();
            if (drawingPath || PlayerMode) return;
            if (session.Lot.paths.Count >= LotPathDefinition.MaxPaths)
            {
                Refresh("This lot has reached its path limit.");
                return;
            }
            StopOverlay();
            if (groundPath.Finished)
            {
                OpenPathMaterialPopup();
                return;
            }

            if (drawingFence) FinishFencePath(false);
            selected = armed = dragged = null;
            selectedPath = null;
            moving = false;
            category = "Paths";
            drawingPath = true;
            Refresh();
        }

        private void FinishGroundPath(bool close)
        {
            drawingPath = false;
            if (!(close ? groundPath.TryClose() : groundPath.TryFinishOpen())) ClearGroundPath();
            Refresh(rebuildPreview: false);
        }

        private void ClearGroundPath()
        {
            drawingPath = false;
            groundPath.Clear();
        }

        private Vector2? GroundPathPoint(Vector2 point) =>
            LotFenceLayout.IsNearLot(session.Lot, point)
                ? LotFenceLayout.ClampToLot(SnapPoint(point), session.Lot.Size * .5f)
                : (Vector2?)null;

        private void SelectAllFenceSections()
        {
            CloseAssetPopup();
            var fences = session.Lot.objects.Where(item =>
                CanEdit(item) && catalog && catalog.Find(item.assetId)?.category == "Fences").ToArray();
            if (fences.Length == 0)
            {
                Refresh("No editable fence sections on this lot.");
                return;
            }

            ClearFencePath();
            ClearRepeat();
            selectedPath = null;
            armed = dragged = null;
            moving = false;
            selected = fences[0].id;
            if (fences.Length > 1)
                foreach (var fence in fences) selectedGroup.Add(fence.id);
            Refresh($"Selected {fences.Length} fence sections.");
        }

        private void SurroundLotWithFence()
        {
            StartFencePath();
            var half = session.Lot.Size * .5f;
            fencePath.TryAdd(new Vector2(-half.x, -half.y));
            fencePath.TryAdd(new Vector2(half.x, -half.y));
            fencePath.TryAdd(new Vector2(half.x, half.y));
            fencePath.TryAdd(new Vector2(-half.x, half.y));
            FinishFencePath(true);
            OpenAssetPopup("Fences", "Fences");
        }

        private void StartFencePath()
        {
            CloseAssetPopup();
            StopOverlay();
            ClearFencePath();
            selected = armed = dragged = null;
            selectedPath = null;
            moving = false;
            drawingFence = true;
            viewport.ShowFencePath(fencePath);
            Refresh();
        }

        private void ClearFencePath()
        {
            drawingFence = false;
            fencePath.Clear();
            viewport.ShowFencePath(null);
        }

        private void FinishFencePath(bool close)
        {
            bool finished = close ? fencePath.TryClose() : fencePath.TryFinishOpen();
            drawingFence = false;
            if (!finished) ClearFencePath();
            Refresh();
        }

        private void FillFencePath(LotAssetCatalogEntry asset)
        {
            CloseAssetPopup();
            if (!LotFenceLayout.TryBuild(session.Lot, fencePath.Points, fencePath.Closed, asset,
                    out var pieces, out var error))
            {
                Refresh(error);
                return;
            }
            session.Edit(lot => lot.objects.AddRange(pieces));
            RememberAsset(asset.id);
            ClearFencePath();
            Refresh($"Placed {pieces.Count} fence pieces.");
        }

        private void ClearRepeat()
        {
            if (repeatSource == null) return;
            repeatSource = null;
            repeatPath.Clear();
            viewport.ShowRepeatPath(null, null);
        }

        private void RepeatAsset(Vector2 destination)
        {
            var source = repeatSource;
            var lot = session.Lot;
            var asset = catalog ? catalog.Find(source.assetId) : null;
            if (!lot.objects.Contains(source) || !CanEdit(source) || asset == null || !asset.prefab)
            {
                ClearRepeat();
                Refresh("Select an asset to repeat.", rebuildPreview: false);
                return;
            }

            var offset = destination - source.position;
            float distance = offset.magnitude;
            var heading = offset.normalized;
            var size = LotRuntimeLoader.Footprint(source, asset).size;
            float spacing = Mathf.Max(.25f, Mathf.Abs(heading.x) * size.x + Mathf.Abs(heading.y) * size.y + .05f);
            int count = Mathf.FloorToInt(distance / spacing);
            if (count == 0)
            {
                Refresh("Move the end point farther from the selected asset.", rebuildPreview: false);
                return;
            }
            if (count > 512 - lot.objects.Count)
            {
                Refresh("Shorten the row to stay within the lot's object limit.", rebuildPreview: false);
                return;
            }

            var copies = new List<LotObjectDefinition>(count);
            for (int i = 1; i <= count; i++)
            {
                var copy = CopyAsset(source, Vector2.Lerp(source.position, destination, (float)i / count));
                if (!LotRuntimeLoader.Fits(lot, copy, asset))
                {
                    Refresh("Keep the repeated assets inside the lot.", rebuildPreview: false);
                    return;
                }
                copies.Add(copy);
            }

            session.Edit(item => item.objects.AddRange(copies));
            ClearRepeat();
            Refresh($"Placed {count} {(count == 1 ? "copy" : "copies")}.");
        }

        private void CommitEntryMarker(Vector2 point)
        {
            var layout = session.Lot.zoningLayout;
            layout.hasEntrance = true;
            layout.entrance = point;
            if (layout.FitProblem(session.Lot) is { } problem)
            {
                Refresh(problem);
                return;
            }

            pickingLotEntrance = false;
            session.Edit(lot => lot.zoningLayout = layout);
            Refresh("Building entry marker set.");
        }

        private void Click(Vector2 point, string hit, LotPathDefinition pathHit)
        {
            CloseChoices();
            if (pickingLotEntrance)
            {
                CommitEntryMarker(point);
                return;
            }
            if (repeatSource != null)
            {
                RepeatAsset(SnapPoint(point));
                return;
            }
            if (terrainMode)
            {
                terrainTile = IsOnLot(point) ? Vector2Int.FloorToInt(GridPosition(point)) : (Vector2Int?)null;
                Refresh();
                return;
            }
            if (drawingPath)
            {
                var marker = GroundPathPoint(point);
                if (!marker.HasValue) Refresh("Click on or near the lot to add a path marker.");
                else if (viewport.NearGroundPathStart(point)) FinishGroundPath(true);
                else if (groundPath.TryAdd(marker.Value)) Refresh(rebuildPreview: false);
                return;
            }
            if (drawingFence)
            {
                if (viewport.NearFenceStart(point)) FinishFencePath(true);
                else
                {
                    if (LotFenceLayout.IsNearLot(session.Lot, point))
                        fencePath.TryAdd(LotFenceLayout.ClampToLot(SnapPoint(point), session.Lot.Size * .5f));
                    else Refresh("Click on or near the lot to add a fence marker.");
                }
                return;
            }
            if (ArmedPreviewAsset is LotAssetCatalogEntry previewAsset)
            {
                if (!PlayerMode && previewAsset.category == "Buildings" && session.Lot.zoningLayout.Configured &&
                    session.Lot.zoningLayout.BuildingSlot.Contains(point))
                {
                    SeedBuildingFootprint(point, previewAsset);
                    return;
                }
                var position = PreviewPlacementPosition(point);
                if (!position.HasValue)
                {
                    Refresh("Choose a spot where the asset fits inside the lot.", rebuildPreview: false);
                    return;
                }
                if (session.Lot.objects.Count >= 512)
                {
                    Refresh("This lot has reached its object limit.", rebuildPreview: false);
                    return;
                }
                var placed = new LotObjectDefinition { assetId = previewAsset.id, position = position.Value, yaw = placementYaw, scale = placementScale };
                session.Edit(lot => lot.objects.Add(placed));
                selected = placed.id;
                armed = null;
                RememberAsset(previewAsset.id);
                Refresh();
                return;
            }
            var picked = Pick(point, hit);
            selectedPath = null;
            if (moving && selectedGroup.Count != 0 && Selection is { } anchor)
            {
                var items = SelectedAssets().ToArray();
                var delta = GroupOffset(items, SnapPoint(point) - anchor.position);
                if (delta.HasValue) session.Edit(_ =>
                {
                    foreach (var item in items) item.position += delta.Value;
                });
            }
            else if (moving)
                ChangeObject(item => item.position = MovePosition(item, point));
            else if (picked != null)
            {
                armed = null;
                if (ShiftHeld && Selection is { } anchorItem && CanEdit(anchorItem) && CanEdit(picked))
                {
                    var min = Vector2.Min(anchorItem.position, picked.position);
                    var max = Vector2.Max(anchorItem.position, picked.position);
                    const float edgeTolerance = .001f;
                    var range = Rect.MinMaxRect(min.x - edgeTolerance, min.y - edgeTolerance,
                        max.x + edgeTolerance, max.y + edgeTolerance);
                    selectedGroup.Clear();
                    foreach (var item in session.Lot.objects)
                        if (CanEdit(item) && range.Contains(item.position)) selectedGroup.Add(item.id);
                    if (selectedGroup.Count < 2) selectedGroup.Clear();
                }
                else selected = picked.id;
            }
            else if (armed != null && session.Lot.objects.Count < 512)
            {
                var item = new LotObjectDefinition { assetId = armed, position = SnapPoint(point) };
                var asset = catalog.Find(armed);
                if (asset.fenceSpan > 0)
                {
                    item.fenceLength = asset.fenceSpan;
                    item.fenceEndPost = true;
                }
                if (asset.category == "Fences")
                {
                    if (!LotFenceLayout.IsNearLot(session.Lot, point))
                    {
                        Refresh("Click on or near the lot to place the fence.");
                        return;
                    }
                    item.position = LotFenceLayout.ClampPiece(session.Lot, item, asset, item.position);
                }
                if (varyPlacementSize) ApplyPlacementVariation(item);
                if (asset.category == "Fences" && !LotRuntimeLoader.Fits(session.Lot, item, asset))
                {
                    Refresh("This fence piece is too large for the lot.");
                    return;
                }
                session.Edit(lot => lot.objects.Add(item));
                selected = item.id;
                RememberAsset(item.assetId);
            }
            else
            {
                selected = null;
                if (!PlayerMode && armed == null) selectedPath = pathHit;
            }
            Refresh();
        }

        private LotObjectDefinition Pick(Vector2 point, string hit)
        {
            if (hit != null) return session.Lot.objects.Find(item => item.id == hit);
            return session.Lot.objects.LastOrDefault(item =>
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                return asset != null && LotRuntimeLoader.Footprint(item, asset).Contains(point);
            });
        }

        private LotAssetCatalogEntry ArmedPreviewAsset
        {
            get
            {
                var asset = catalog && armed != null ? catalog.Find(armed) : null;
                return asset?.IsCharacter == true || CanRotatePlacement(asset) ? asset : null;
            }
        }

        private static bool CanRotatePlacement(LotAssetCatalogEntry asset) =>
            asset?.category == "Buildings" || asset?.category == LotAssetCatalog.StreetFurniture;

        private Vector2? PreviewPlacementPosition(Vector2 point)
        {
            var asset = ArmedPreviewAsset;
            if (asset == null || !IsOnLot(point)) return null;
            var position = asset.IsCharacter ? SnapCharacter(point) : SnapPoint(point);
            var size = placementYaw % 180f == 0 ? asset.footprint : new Vector2(asset.footprint.y, asset.footprint.x);
            var half = (session.Lot.Size - size * placementScale) * .5f;
            return Mathf.Abs(position.x) <= half.x && Mathf.Abs(position.y) <= half.y ? position : (Vector2?)null;
        }

        private Vector2 SnapPoint(Vector2 point) =>
            snap ? new Vector2(Mathf.Round(point.x), Mathf.Round(point.y)) : point;

        private Vector2 SnapCharacter(Vector2 point)
        {
            var snapped = SnapPoint(point);
            bool onDeck = LotCharacterDeck.TryHeight(session.Lot, point, out float height);
            bool snappedOnDeck = LotCharacterDeck.TryHeight(session.Lot, snapped, out float snappedHeight);
            // A narrow porch can fall between grid points. Keep the clicked deck point in that case.
            return onDeck != snappedOnDeck || (onDeck && !Mathf.Approximately(height, snappedHeight)) ? point : snapped;
        }

        private Vector2 MovePosition(LotObjectDefinition item, Vector2 point)
        {
            var asset = catalog ? catalog.Find(item.assetId) : null;
            var snapped = asset?.IsCharacter == true ? SnapCharacter(point) : SnapPoint(point);
            return asset?.category == "Fences" || CanRotatePlacement(asset)
                ? LotFenceLayout.ClampPiece(session.Lot, item, asset, snapped)
                : snapped;
        }

        private void BeginMove(string hit, Vector2 point)
        {
            if (pickingLotEntrance) return;
            if (terrainMode)
            {
                if (overlayBrush == null || !IsOnLot(point)) return;
                var start = GridPosition(point);
                overlayLast = start;
                PaintThrough(start, start);
                return;
            }
            if (repeatSource != null || drawingFence || drawingPath || ArmedPreviewAsset != null || ShiftHeld) return;
            var item = Pick(point, hit);
            if (item == null) return;
            if (!selectedGroup.Contains(item.id)) selected = item.id;
            selectedPath = null;
            armed = null;
            moving = false;
            dragged = CanEdit(item) ? item.id : null;
            dragStart = point;
            dragPosition = item.position;
            dragOrigin = viewport.PreviewPosition(item.id) ?? item.position;
            Refresh();
        }

        private void PreviewMove(Vector2 point)
        {
            if (overlayStroke.Count != 0)
            {
                if (IsOnLot(point))
                {
                    var end = GridPosition(point);
                    PaintThrough(overlayLast ?? end, end);
                    overlayLast = end;
                }
                else overlayLast = null;
                return;
            }
            if (dragged == null) return;
            var item = session.Lot.objects.Find(candidate => candidate.id == dragged);
            if (item == null) return;
            if (selectedGroup.Count != 0)
            {
                var items = SelectedAssets().ToArray();
                var delta = GroupOffset(items, SnapPoint(item.position + point - dragStart) - item.position);
                if (!delta.HasValue) return;
                dragPosition = item.position + delta.Value;
                foreach (var member in items) viewport.PreviewMove(member.id, member.position + delta.Value);
                return;
            }
            dragPosition = MovePosition(item, dragOrigin + point - dragStart);
            viewport.PreviewMove(dragged, dragPosition);
        }

        private void EndMove()
        {
            if (overlayStroke.Count != 0)
            {
                var tiles = overlayStroke.Select(TileBounds).ToList();
                ClearOverlayStroke();
                EditOverlayTiles(tiles, overlayBrush);
                return;
            }
            if (dragged == null) return;
            var item = session.Lot.objects.Find(candidate => candidate.id == dragged);
            dragged = null;
            if (CanEdit(item) && item.position != dragPosition)
            {
                var delta = dragPosition - item.position;
                session.Edit(_ =>
                {
                    foreach (var member in SelectedAssets()) member.position += delta;
                });
            }
            Refresh();
        }

        private void ApplyPlacementVariation(LotObjectDefinition item)
        {
            var asset = catalog ? catalog.Find(item.assetId) : null;
            if (asset == null) return;
            var margin = session.Lot.Size * .5f - new Vector2(Mathf.Abs(item.position.x), Mathf.Abs(item.position.y));
            var footprint = LotRuntimeLoader.Footprint(item, asset).size * .5f;
            float max = Mathf.Min(1.15f, margin.x / footprint.x, margin.y / footprint.y);
            if (max < .85f) return;
            item.scale *= Mathf.Lerp(.85f, max, (float)placementRandom.NextDouble());
        }

        private void OnDestroy() => thumbnails.Dispose();

        private LotObjectDefinition Selection => session.Lot.objects.Find(item => item.id == selected);

        private void RotateSelection()
        {
            var item = Selection;
            if (!CanEdit(item)) return;
            var asset = catalog ? catalog.Find(item.assetId) : null;
            session.Edit(_ =>
            {
                float oldYaw = item.yaw;
                item.yaw = (oldYaw + 90) % 360;
                if (asset?.category != "Buildings") return;
                var footprint = LotRuntimeLoader.Footprint(item, asset).size;
                var size = session.Lot.Size;
                if (footprint.x > size.x + .001f || footprint.y > size.y + .001f)
                    item.yaw = oldYaw;
                else
                    item.position = LotFenceLayout.ClampPiece(session.Lot, item, asset, item.position);
            });
            Refresh();
        }

        private void ChangeObject(Action<LotObjectDefinition> edit)
        {
            var item = Selection;
            if (CanEdit(item)) session.Edit(_ => edit(item));
            Refresh();
        }

        private static bool ShiftHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        private IEnumerable<LotObjectDefinition> SelectedAssets() => session.Lot.objects.Where(item =>
            item.id == selected || selectedGroup.Contains(item.id));

        private static LotObjectDefinition CopyAsset(LotObjectDefinition item, Vector2 position) => new()
        {
            assetId = item.assetId, position = position, yaw = item.yaw, scale = item.scale,
            fenceLength = item.fenceLength, fenceEndPost = item.fenceEndPost, characterScript = item.characterScript
        };

        private bool NudgeSelection()
        {
            var pixels = Vector2.zero;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) pixels.x -= 1;
            if (Input.GetKeyDown(KeyCode.RightArrow)) pixels.x += 1;
            if (Input.GetKeyDown(KeyCode.DownArrow)) pixels.y -= 1;
            if (Input.GetKeyDown(KeyCode.UpArrow)) pixels.y += 1;
            if (pixels == Vector2.zero || !viewport.TryScreenNudge(pixels, out var requested)) return false;

            var items = SelectedAssets().ToArray();
            var offset = GroupOffset(items, requested);
            if (offset.HasValue && offset.Value.sqrMagnitude > 0f)
            {
                session.Edit(_ =>
                {
                    foreach (var item in items) item.position += offset.Value;
                });
                Refresh();
            }
            return true;
        }

        private Vector2? GroupOffset(IEnumerable<LotObjectDefinition> items, Vector2 requested)
        {
            var half = session.Lot.Size * .5f;
            var min = Vector2.negativeInfinity;
            var max = Vector2.positiveInfinity;
            foreach (var item in items)
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                if (asset == null || !asset.prefab || !CanEdit(item)) return null;
                var footprint = LotRuntimeLoader.Footprint(item, asset);
                min = Vector2.Max(min, -half - footprint.min);
                max = Vector2.Min(max, half - footprint.max);
            }
            if (min.x > max.x || min.y > max.y) return null;
            return Vector2.Min(Vector2.Max(requested, min), max);
        }

        private void Duplicate() => PasteAssets(SelectedAssets().Where(CanEdit).ToArray());

        private void PasteAssets(LotObjectDefinition[] items)
        {
            if (items.Length == 0) return;
            if (items.Length > 512 - session.Lot.objects.Count)
            {
                Refresh("There isn't room for all the copies within the lot's object limit.", rebuildPreview: false);
                return;
            }
            var offset = GroupOffset(items, Vector2.one);
            if (!offset.HasValue)
            {
                Refresh("The copied assets don't fit on this lot.", rebuildPreview: false);
                return;
            }

            var copies = items.Select(item => CopyAsset(item, item.position + offset.Value)).ToArray();
            session.Edit(lot => lot.objects.AddRange(copies));
            selected = copies[0].id;
            if (copies.Length > 1)
                foreach (var copy in copies) selectedGroup.Add(copy.id);
            selectedPath = null;
            armed = null;
            moving = false;
            Refresh($"Pasted {copies.Length} {(copies.Length == 1 ? "asset" : "assets")} · Drag to move");
        }

        private void Delete()
        {
            if (terrainMode)
            {
                if (terrainTile.HasValue && overlayStroke.Count == 0)
                    EditOverlayTiles(new List<Rect> { TileBounds(terrainTile.Value) }, null);
                return;
            }
            if (selectedPath != null && !PlayerMode)
            {
                session.Edit(lot => lot.paths.Remove(selectedPath));
                selectedPath = null;
                Refresh();
                return;
            }
            var items = SelectedAssets().Where(CanEdit).ToArray();
            if (items.Length == 0) return;
            session.Edit(lot =>
            {
                foreach (var item in items) lot.objects.Remove(item);
            });
            selected = null;
            moving = false;
            Refresh();
        }

        private InputField CostField(string label, float x, Action<LotDefinition, int> edit)
        {
            Label(inspector, label, x + 5, 95, 145, 15);
            var field = Field(inspector, label, x, 127, 151);
            field.gameObject.name = label + " input";
            field.interactable = !PlayerMode;
            field.contentType = InputField.ContentType.IntegerNumber;
            field.characterLimit = 10;
            AddOutline(field.targetGraphic, Stroke);
            field.onEndEdit.AddListener(text =>
            {
                if (PlayerMode) return;
                if (int.TryParse(text, out int amount) && amount >= 0)
                {
                    session.Edit(lot => edit(lot, amount));
                    Refresh(rebuildPreview: false);
                }
                else Refresh("Enter a non-negative whole number for " + label.ToLowerInvariant() + ".",
                    rebuildPreview: false);
            });
            return field;
        }

        private bool HasUnfinishedLot => session.Dirty || groundPath.Points.Count != 0 || fencePath.Points.Count != 0;

        private void NewLot()
        {
            if (assetPopup) return;
            CloseChoices();
            if (!HasUnfinishedLot)
            {
                OpenLot(new LotDefinition());
                return;
            }
            var frame = CreateModal("Start a new lot");
            Label(frame, "Discard the current unsaved changes and start a new lot?", 20, 64, 520, 16);
            Control(frame, "Discard and start new", 322, 115, 218, () =>
            {
                CloseAssetPopup();
                OpenLot(new LotDefinition());
            });
        }

        private void OpenLot(LotDefinition lot)
        {
            pickingLotEntrance = false;
            session.Open(lot);
            selected = armed = dragged = null;
            selectedPath = null;
            moving = false;
            ClearFencePath();
            ClearGroundPath();
            StopOverlay();
            CloseChoices();
            Refresh();
        }

        private void OpenSavedLots(LotType type)
        {
            if (assetPopup) return;
            CloseChoices();
            string itemName = type == LotType.Template ? "template" : type == LotType.Zoning ? "zoning lot" : "lot";
            LotLibrary.Entry[] entries;
            try { entries = library.List(type); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Refresh($"Could not open the saved {itemName} library.");
                return;
            }

            var frame = CreateModal(type == LotType.Template ? "Lot Templates" : type == LotType.Zoning ? "Growable Lots" : "Saved Lots");
            var search = Field(frame, $"Search saved {itemName}s", 20, 58, 520);
            var notice = Label(frame, "", 20, 437, 520, 15);
            notice.color = Gold;
            CreatePickerResults(frame, 105, 108);
            LotLibrary.Entry selectedEntry = null;
            var rows = new Dictionary<LotLibrary.Entry, Button>();
            bool confirmLoad = false, confirmDelete = false;
            Button load = null, delete = null;
            load = Control(frame, "Load", 178, 484, 170, LoadSelected, description: $"Open the selected {itemName}");
            delete = Control(frame, "Delete", 358, 484, 182, DeleteSelected,
                description: $"Delete the selected {itemName} from the library");
            BindPickerSearch(search, Render);

            void Render(string query)
            {
                ClearPickerRows();
                rows.Clear();
                if (selectedEntry != null && !Matches(selectedEntry.Lot, query))
                {
                    selectedEntry = null;
                    confirmLoad = confirmDelete = false;
                }
                int y = 5;
                foreach (var entry in entries)
                {
                    var lot = entry.Lot;
                    if (!Matches(lot, query)) continue;
                    var row = Control(assetList, lot.name, 8, y, 504, () =>
                    {
                        selectedEntry = entry;
                        confirmLoad = confirmDelete = false;
                        foreach (var pair in rows) MarkActive(pair.Value, pair.Key == selectedEntry);
                        UpdateActions();
                    });
                    At((RectTransform)row.transform, 8, y, 504, 58);
                    var title = row.GetComponentInChildren<Text>();
                    At(title.rectTransform, 10, 1, 484, 32);
                    title.alignment = TextAnchor.MiddleLeft;
                    var details = Label(row.transform,
                        type == LotType.Zoning ? $"{lot.zoningLayout.Label}  ·  {lot.width} × {lot.length} tiles  ·  " +
                            $"Slot {lot.zoningLayout.buildingSize.x:0.##} × {lot.zoningLayout.buildingSize.y:0.##} m" :
                            $"{lot.width} × {lot.length} tiles  ·  Cost {lot.cost:N0}  ·  Monthly {lot.monthlyCost:N0}  ·  {entry.SavedUtc.ToLocalTime():g}",
                        10, 26, 484, 13);
                    details.color = new Color32(174, 189, 200, 255);
                    At(details.rectTransform, 10, 28, 484, 26);
                    rows.Add(entry, row);
                    MarkActive(row, entry == selectedEntry);
                    y += 64;
                }
                if (y == 5) Label(assetList, entries.Length == 0 ? $"No saved {itemName}s yet." : $"No matching {itemName}s.", 12, 5, 484, 17);
                assetList.sizeDelta = new Vector2(0, Mathf.Max(45, y + 5));
                assetList.anchoredPosition = Vector2.zero;
                assetScroll.StopMovement();
                UpdateActions();
            }

            bool Matches(LotDefinition lot, string query) =>
                lot.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (type == LotType.Zoning && lot.zoningLayout.Label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

            void UpdateActions(string message = null)
            {
                load.interactable = delete.interactable = selectedEntry != null;
                load.GetComponentInChildren<Text>().text = confirmLoad ? "Discard and load" : "Load";
                delete.GetComponentInChildren<Text>().text = confirmDelete ? "Confirm delete" : "Delete";
                notice.text = message ?? (confirmDelete ? $"Delete the selected {itemName} permanently?"
                    : confirmLoad ? "Discard the current unsaved changes?" : "");
            }

            void LoadSelected()
            {
                if (selectedEntry == null) return;
                confirmDelete = false;
                if (HasUnfinishedLot && !confirmLoad)
                {
                    confirmLoad = true;
                    UpdateActions();
                    return;
                }
                try
                {
                    var lot = library.Load(selectedEntry.Lot.id);
                    if (lot.type != type) throw new InvalidDataException("Saved lot type changed.");
                    CloseAssetPopup();
                    OpenLot(lot);
                }
                catch (Exception error) when (error is IOException || error is InvalidDataException ||
                    error is UnauthorizedAccessException || error is ArgumentException)
                {
                    confirmLoad = false;
                    UpdateActions($"Could not load the selected {itemName}.");
                }
            }

            void DeleteSelected()
            {
                if (selectedEntry == null) return;
                confirmLoad = false;
                if (!confirmDelete)
                {
                    confirmDelete = true;
                    UpdateActions();
                    return;
                }
                try
                {
                    string id = selectedEntry.Lot.id;
                    library.Delete(id);
                    if (session.Lot.id == id) session.MarkUnsaved();
                    entries = entries.Where(entry => entry.Lot.id != id).ToArray();
                    selectedEntry = null;
                    confirmDelete = false;
                    Refresh(rebuildPreview: false);
                    Render(search.text);
                    UpdateActions($"Saved {itemName} deleted.");
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    confirmDelete = false;
                    UpdateActions($"Could not delete the selected {itemName}.");
                }
            }
        }

        private void Save(LotType type)
        {
            var problem = Validate();
            if (problem != null)
            {
                Refresh(problem);
                return;
            }
            try
            {
                if (PlayerMode)
                {
                    savePlayer(LotCustomizationService.CreateOverride(playerLot, session.Lot, catalog));
                    goBack();
                    return;
                }
                var saved = library.SaveAs(session.Lot, type);
                bool copied = saved != session.Lot;
                if (copied) session.Open(saved);
                else session.MarkSaved();
                Refresh(type == LotType.Template ? "Template saved." : type == LotType.Zoning ? "Growable lot saved." :
                    copied ? "Playable lot saved as a copy." : "Lot saved.",
                    rebuildPreview: false);
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException)
            {
                Refresh(type == LotType.Template ? "Could not save the template." : "Could not save the lot.");
            }
        }

        private string Validate()
        {
            var zoning = session.Lot.zoningLayout;
            if (zoning.Configured)
            {
                if (!zoning.Valid) return "Review the growable layout settings.";
                var fitProblem = zoning.FitProblem(session.Lot);
                if (fitProblem != null) return fitProblem;
            }
            if (zoning.Configured && SlotOverlapsBuilding(session.Lot, zoning))
                return "Move fixed buildings out of the variable slot.";
            if (!session.Lot.HasValidCapitalSettings()) return "Review the lot requirements and capital bonus.";
            foreach (var item in session.Lot.objects)
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                if (asset == null || !asset.prefab) return "Missing asset: " + item.assetId;
                if (!LotRuntimeLoader.Fits(session.Lot, item, asset))
                    return "Move objects inside the lot before saving.";
            }
            if (session.Lot.paths.Any(path => !PathFits(path)))
                return "Keep path markers inside the lot before saving.";
            var half = session.Lot.Size * .5f;
            if (session.Lot.overlays.Any(overlay =>
                LotFenceLayout.ClampToLot(overlay.min, half) != overlay.min ||
                LotFenceLayout.ClampToLot(overlay.max, half) != overlay.max))
                return "Keep overlay areas inside the lot before saving.";
            return null;
        }

        private static string ChoiceLabel(string value) =>
            (string.IsNullOrEmpty(value) ? "Unspecified" : value) + "  ▾";

        private void Refresh(string message = null, bool rebuildPreview = true)
        {
            confirmDiscard = false;
            if (rebuildPreview) ClearRepeat();
            var lot = session.Lot;
            if (selectedPath != null && !lot.paths.Contains(selectedPath)) selectedPath = null;
            if (waterfrontRowsLabel)
                waterfrontRowsLabel.text = lot.IsWaterfront ? $"Waterfront: {lot.waterfrontRows} {lot.waterfrontSide}  ▾" : "Waterfront: Land only  ▾";
            width.text = lot.width + "  ▾";
            length.text = lot.length + "  ▾";
            nameField.SetTextWithoutNotify(lot.name);
            costField.SetTextWithoutNotify(lot.cost.ToString());
            monthlyCostField.SetTextWithoutNotify(lot.monthlyCost.ToString());
            var parentCategory = LotClassification.FindCategory(lot.category);
            lotCategoryLabel.text = ChoiceLabel(parentCategory?.name ?? lot.category);
            lotSubcategoryLabel.text = ChoiceLabel(LotClassification.SubcategoryName(lot.category, lot.subcategory));
            lotEraLabel.text = ChoiceLabel(LotClassification.Era(lot)?.Name ??
                (lot.firstEraYear > 0 ? lot.firstEraYear.ToString() : lot.firstEraId));
            lotCategory.interactable = lotEra.interactable = !PlayerMode;
            lotSubcategory.interactable = !PlayerMode && parentCategory != null;
            snapLabel.text = snap ? "Snap: On" : "Snap: Off";
            gridLabel.text = grid ? "Grid: On" : "Grid: Off";
            lotTitle.text = lot.type == LotType.Template ? lot.name + "  ·  Template" :
                lot.zoningLayout.Configured ? lot.name + "  ·  " + lot.zoningLayout.Label : lot.name;
            lotSummary.text = $"{lot.width} × {lot.length} tiles  ·  {lot.objects.Count} objects" + (session.Dirty ? "  ·  Unsaved changes" : "");
            var item = Selection;
            bool hasSelection = item != null;
            if (!hasSelection) selectedGroup.Clear();
            else if (selectedGroup.Count != 0)
            {
                selectedGroup.RemoveWhere(id => !lot.objects.Exists(candidate => candidate.id == id));
                if (selectedGroup.Count < 2) selectedGroup.Clear();
            }
            bool singleEditable = selectedGroup.Count < 2 && CanEdit(item);
            rotateSelectionButton.interactable = shrinkSelectionButton.interactable = growSelectionButton.interactable = singleEditable;
            scriptButton.interactable = singleEditable && CanScript(item);
            if (!hasSelection) moving = false;
            selectionToolbar.gameObject.SetActive(hasSelection);
            pathSelectionToolbar.gameObject.SetActive(selectedPath != null);
            recessedPathToggle.SetIsOnWithoutNotify(selectedPath?.recessed == true);
            if (!edgeBlendSlider.PointerHeld)
            {
                int blend = selectedPath?.edgeBlend ?? 0;
                edgeBlendSlider.SetValueWithoutNotify(blend);
                edgeBlendLabel.text = $"Edge Blending {blend}%";
            }
            MarkActive(curveSelectedCorners, selectedPath?.curved == true);
            MarkActive(curveDraftCorners, curvePath);
            MarkActive(selectTool, !moving && armed == null);
            MarkActive(moveTool, moving);
            foreach (var pair in categoryButtons) MarkActive(pair.Value, pair.Key == category);
            if (terrainTile.HasValue && (terrainTile.Value.x >= lot.width || terrainTile.Value.y >= lot.length))
                terrainTile = null;
            terrainInspector.gameObject.SetActive(terrainTile.HasValue);
            if (terrainTile.HasValue)
            {
                var tile = TileBounds(terrainTile.Value);
                bool hasOverlay = !string.IsNullOrEmpty(lot.overlayTexture) ||
                    lot.overlays.Any(overlay => overlay.Bounds.Overlaps(tile));
                terrainTileLabel.text = $"Tile {terrainTile.Value.x + 1}, {terrainTile.Value.y + 1}" +
                    (hasOverlay ? "" : " · No overlay");
                rotateOverlayLeft.interactable = rotateOverlayRight.interactable = hasOverlay;
            }
            MarkActive(terrainTool, terrainMode);
            MarkActive(textureTool, overlayBrush != null);
            overlayModeLabel.transform.parent.gameObject.SetActive(terrainMode);
            overlayModeLabel.text = overlayBrush != null
                ? $"Terrain · {overlayBrush.Label} · Esc to exit"
                : "Terrain · Click a tile · Delete clears overlay";
            undo.interactable = session.CanUndo;
            redo.interactable = session.CanRedo;
            selectedLabel.text = selectedPath != null
                ? $"Selected: {LotPathMaterial.Find(selectedPath.materialId).Label} path · {selectedPath.width} m"
                : selectedGroup.Count > 1 ? $"Selected: {selectedGroup.Count} assets"
                : item == null ? "Selected: None" :
                $"Selected: {catalog?.Find(item.assetId)?.label ?? item.assetId} · {item.yaw:0}° · {item.scale:0.##}×";
            chooseFence.gameObject.SetActive(fencePath.Finished);
            choosePathMaterial.gameObject.SetActive(groundPath.Finished);
            At((RectTransform)choosePathMaterial.transform, fencePath.Finished ? 240 : 22, 207, 220, 36);
            pathTools.gameObject.SetActive(drawingPath || groundPath.Finished);
            MarkActive(pathTool, drawingPath || groundPath.Finished);
            for (int i = 0; i < LotPathSurface.Widths.Length; i++) MarkActive(pathWidthButtons[i], LotPathSurface.Widths[i] == pathWidth);
            finishPath.interactable = drawingPath && groundPath.Points.Count >= 2;
            clearPath.interactable = groundPath.Points.Count > 0;
            var hint = pickingLotEntrance ? "Click inside the blue building slot to place the entry marker · Esc to cancel"
                : repeatSource != null ? "Click the end of the row · Esc to cancel"
                : terrainMode ? "Click to select · Delete clears overlay · Drag to paint with a texture"
                : drawingPath ? "Click to add points · Enter or Esc to finish · Click start to close"
                : drawingFence ? "Click to add markers · Click the first marker to close · Esc to keep open"
                : groundPath.Finished ? "Select path material to pave the amber ribbon"
                : fencePath.Finished ? "Choose a fence to fill the purple path · Esc to discard the guide"
                : moving ? "Click to move" : CanRotatePlacement(ArmedPreviewAsset) ? "Click to place · R to rotate · Esc to cancel"
                : armed != null ? "Click to place"
                : selectedGroup.Count > 1 ? "Arrow keys to nudge · Drag to move the group"
                : CanEdit(item) ? "Arrow keys to nudge · Shift-click to select a range · R to repeat"
                : "Drag an asset to move · Right drag to orbit · Scroll to zoom";
            var issue = Validate();
            validation.text = issue == null ? "No validation issues" : "Check lot boundaries / assets";
            validation.color = issue == null ? new Color32(142, 211, 141, 255) : Gold;
            status.text = $"{lot.width} × {lot.length}  |  {lot.objects.Count} objects" +
                (session.Dirty ? "  |  Unsaved" : "") + "  |  " + (message ?? (pickingLotEntrance || repeatSource != null || drawingPath || groundPath.Finished || drawingFence || fencePath.Finished ? hint : issue ?? hint));
            viewport.EnableFootprintEditing(!PlayerMode && !assetPopup && !terrainMode &&
                repeatSource == null && !drawingPath && !drawingFence && ArmedPreviewAsset == null);
            viewport.SetCharacterFocus(category == "Characters");
            viewport.SetMoveState(dragged, moving);
            viewport.SetPlacementGhost(assetPopup ? null : ArmedPreviewAsset, PreviewPlacementPosition, placementYaw, placementScale);
            viewport.ShowGroundPath(drawingPath || groundPath.Finished ? groundPath : null, pathWidth, curvePath, GroundPathPoint);
            viewport.PickingEntryMarker = pickingLotEntrance;
            if (rebuildPreview) viewport.Refresh(lot, catalog, selected, selectedGroup, selectedPath, grid,
                terrainTile.HasValue ? TileBounds(terrainTile.Value) : (Rect?)null, showGrowablePreview: !PlayerMode);
        }

        private Button Control(Transform parent, string title, float x, float y, float size, Action action,
            LotGlyph glyph = LotGlyph.None, string description = null, bool iconOnly = false)
        {
            var image = AddImage(title, parent, ButtonColor);
            image.raycastTarget = true;
            At(image.rectTransform, x, y, size, 36);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            AddOutline(image, Stroke);
            if (glyph != LotGlyph.None)
                AddGlyph(image.transform, glyph, iconOnly ? (size - 24) / 2 : 8, iconOnly ? 6 : 9, iconOnly ? 24 : 18);
            var label = AddText("Label", image.transform, iconOnly ? "" : title, font, 16, TextAnchor.MiddleCenter);
            Place(label.rectTransform, Vector2.zero, Vector2.one,
                glyph != LotGlyph.None && !iconOnly ? new Vector2(29, 0) : Vector2.zero, new Vector2(-4, 0));
            if (description != null) Describe(button, description);
            return button;
        }

        private RectTransform Tray(string name, float x, float y, float w, float h)
        {
            var tray = AddImage(name, transform, PanelColor);
            At(tray.rectTransform, x, y, w, h);
            AddOutline(tray, Stroke);
            return tray.rectTransform;
        }

        private void Section(Transform parent, string caption, int top)
        {
            var section = AddImage(caption, parent, ButtonColor);
            At(section.rectTransform, 14, top, 315, 35);
            var text = Label(section.transform, "›   " + caption, 11, 0, 290, 20);
            text.font = headingFont;
        }

        private void AddCategory(Transform parent, string value, string label, LotGlyph glyph, int column)
        {
            if (PlayerMode && value == "Buildings") return;
            categoryButtons.Add(value, ToolTile(parent, label, glyph, column,
                () => OpenAssetPopup(value, label), "Browse " + label.ToLowerInvariant()));
        }

        private Button ToolTile(Transform parent, string label, LotGlyph glyph, int column, Action action,
            string description)
        {
            var button = Control(parent, label, 5 + column * 67, 4, 64, action, glyph, description, true);
            At((RectTransform)button.transform, 5 + column * 67, 4, 64, 45);
            At(button.GetComponentInChildren<LotEditorIcon>().rectTransform, 21, 2, 22, 22);
            var caption = button.GetComponentInChildren<Text>();
            caption.text = label;
            caption.fontSize = label == LotAssetCatalog.StreetFurniture ? 9 : 12;
            At(caption.rectTransform, 0, 25, 64, 18);
            return button;
        }

        private static LotGlyph CategoryGlyph(string value)
        {
            switch (value)
            {
                case "Buildings": return LotGlyph.House;
                case "Trees": return LotGlyph.Tree;
                case "Fences": return LotGlyph.Fence;
                case "Characters": return LotGlyph.Character;
                case "Landscaping": return LotGlyph.Landscaping;
                case LotAssetCatalog.StreetFurniture: return LotGlyph.StreetFurniture;
                default: return LotGlyph.Catalog;
            }
        }

        internal void Describe(Button button, string description) =>
            button.gameObject.AddComponent<LotToolDescription>().Configure(this, description);

        internal void ShowDescription(LotToolDescription owner, bool show)
        {
            if (!tooltip) return;
            if (!show)
            {
                if (tooltipOwner == owner) tooltip.transform.parent.gameObject.SetActive(false);
                return;
            }
            tooltipOwner = owner;
            tooltip.text = owner.Description;
            var rect = (RectTransform)transform;
            var control = (RectTransform)owner.transform;
            var point = (Vector2)rect.InverseTransformPoint(control.TransformPoint(control.rect.center));
            var bubble = (RectTransform)tooltip.transform.parent;
            var size = new Vector2(Mathf.Min(420f, tooltip.preferredWidth + 20), 30);
            var x = Mathf.Clamp(point.x - size.x / 2, rect.rect.xMin + 7, rect.rect.xMax - size.x - 7);
            var top = point.y - control.rect.height / 2 - 5;
            if (top - size.y < rect.rect.yMin) top = point.y + control.rect.height / 2 + size.y + 5;
            At(bubble, x - rect.rect.xMin, rect.rect.yMax - top, size.x, size.y);
            bubble.SetAsLastSibling();
            bubble.gameObject.SetActive(true);
        }

        private static void MarkActive(Button button, bool active)
        {
            button.targetGraphic.color = active ? Accent : ButtonColor;
            button.GetComponent<Outline>().effectColor = active ? Gold : Stroke;
        }

        internal static void AddOutline(Graphic graphic, Color color)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1, -1);
        }

        internal static void AddGlyph(Transform parent, LotGlyph glyph, float x, float y, float size)
        {
            var root = new GameObject(glyph + " icon", typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(parent, false);
            var icon = root.AddComponent<LotEditorIcon>();
            icon.Glyph = glyph;
            icon.color = new Color32(239, 230, 210, 255);
            icon.raycastTarget = false;
            At(icon.rectTransform, x, y, size, size);
            icon.SetVerticesDirty();
        }

        private static void PinRight(RectTransform rect, float right, float width) =>
            Place(rect, Vector2.one, Vector2.one, new Vector2(-right - width, -40), new Vector2(-right, -4));

        private Toggle Checkbox(Transform parent, string name, string label, float x, float y, float width, bool value)
        {
            var background = AddImage(name, parent, ButtonColor);
            At(background.rectTransform, x, y, width, 32);
            background.raycastTarget = true;
            var toggle = background.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            var box = AddImage("Checkbox", background.transform, PanelColor);
            At(box.rectTransform, 9, 6, 20, 20);
            AddOutline(box, Stroke);
            AddGlyph(box.transform, LotGlyph.Check, 0, 0, 20);
            toggle.graphic = box.GetComponentInChildren<LotEditorIcon>();
            var caption = Label(background.transform, label, 38, 0, width - 43, 16);
            At(caption.rectTransform, 38, 0, width - 43, 32);
            toggle.SetIsOnWithoutNotify(value);
            return toggle;
        }

        private InputField Field(Transform parent, string placeholder, float x, float y, float size)
        {
            var image = AddImage(placeholder, parent, ButtonColor);
            image.raycastTarget = true;
            At(image.rectTransform, x, y, size, 36);
            var input = image.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = AddText("Value", image.transform, "", font, 18, TextAnchor.MiddleLeft);
            var hint = AddText("Placeholder", image.transform, placeholder, font, 18, TextAnchor.MiddleLeft);
            hint.color = new Color32(173, 181, 188, 255);
            input.placeholder = hint;
            foreach (var text in new[] { input.textComponent, hint })
                Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
            return input;
        }

        private Text Label(Transform parent, string title, float x, float y, float size, int fontSize = 18)
        {
            var label = AddText(title, parent, title, font, fontSize, TextAnchor.MiddleLeft);
            At(label.rectTransform, x, y, size, 36);
            return label;
        }

        private static void At(RectTransform rect, float x, float y, float width, float height)
        {
            Place(rect, Vector2.up, Vector2.up, new Vector2(x, -y - height), new Vector2(x + width, -y));
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = low;
            rect.offsetMax = high;
        }
    }
    internal sealed class LotEntryPlanInput : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        internal Action<Vector2> PlaceEntry;

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var plan = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(plan, eventData.position,
                    eventData.pressEventCamera, out var point)) return;
            var rect = plan.rect;
            PlaceEntry?.Invoke(new Vector2(Mathf.InverseLerp(rect.xMin, rect.xMax, point.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, point.y)));
        }
    }

    internal sealed class LotEdgeBlendSlider : Slider
    {
        internal bool PointerHeld { get; private set; }
        internal Action Commit;

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
                PointerHeld = true;
            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (eventData.button != PointerEventData.InputButton.Left || !PointerHeld) return;
            PointerHeld = false;
            Commit?.Invoke();
        }

        protected override void OnDisable()
        {
            PointerHeld = false;
            base.OnDisable();
        }
    }
}
