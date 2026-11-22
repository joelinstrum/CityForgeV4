using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private RectTransform savedLotPicker;
        private GameObject lotGhost;
        private DistrictLotRecord ghostPlacement;
        private readonly Dictionary<(Material, bool), Material> lotGhostMaterials = new();
        private LineRenderer districtHover;
        private int[] hoveredDistrictMembers;
        private CombinedDistrictGroup[] hoveredDistrictGroups;

        private void OpenSavedLotPicker()
        {
            LotDefinition[] lots;
            try { lots = LotLibrary.Local.List().Select(entry => entry.Lot).Where(lot => !IsFoundersLot(lot)).ToArray(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                SetLotFeedback("Could not read the saved lot library.");
                return;
            }
            if (lots.Length == 0) { SetLotFeedback("Save a lot in the Lot Editor first."); return; }
            lastRepaveSurface = null;
            CloseTownSection();
            SetRoadPaths(false);
            CancelDistrictActor();
            savedLotPicker = FounderRect(lotActions.transform.parent, "Saved lot picker", Vector2.zero, Vector2.zero);
            savedLotPicker.anchorMin = Vector2.zero;
            savedLotPicker.anchorMax = Vector2.one;
            savedLotPicker.offsetMin = savedLotPicker.offsetMax = Vector2.zero;
            savedLotPicker.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .58f);
            var panel = FounderRect(savedLotPicker, "Saved lots", Vector2.zero, new Vector2(620, 470));
            StyleTownPanel(panel, TownPanelColor);
            FounderText(panel, "Place Saved Lot", new Vector2(0, 205), new Vector2(450, 34), 24).color = TownGold;
            TownButton(panel, lotFont, "×", 568, -10, 36, CloseSavedLotPicker).Select();
            var viewport = FounderRect(panel, "Lot list", new Vector2(0, -20), new Vector2(580, 380));
            viewport.gameObject.AddComponent<Image>().color = new Color(.05f, .07f, .09f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport;
            var content = FounderRect(viewport, "Lot categories", Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            scroll.content = content;
            var catalog = LotAssetCatalog.Load();
            int top = 8;
            foreach (var category in lots.GroupBy(lot => LotClassification.FindCategory(lot.category)?.name ??
                (string.IsNullOrWhiteSpace(lot.category) ? "Uncategorized" : lot.category)).OrderBy(group => group.Key))
            {
                var label = FounderText(content, category.Key, Vector2.zero, new Vector2(552, 30), 19);
                Position(label.rectTransform, new Vector2(14, -top), new Vector2(552, 30));
                label.alignment = TextAnchor.MiddleLeft;
                label.color = TownGold;
                top += 34;
                foreach (var lot in category.OrderBy(item => item.name))
                {
                    var button = TownButton(content, lotFont, $"{lot.name}  ·  {lot.width} × {lot.length}", 14, -top, 552,
                        () => BeginLotPlacement(lot));
                    button.interactable = lot.objects.Count > 0 && LotAssetProblem(lot, catalog) == null;
                    top += 44;
                }
            }
            content.sizeDelta = new Vector2(0, top + 8);
            content.anchoredPosition = Vector2.zero;
        }

        private void CloseSavedLotPicker()
        {
            if (!savedLotPicker) return;
            savedLotPicker.gameObject.SetActive(false);
            Destroy(savedLotPicker.gameObject);
            savedLotPicker = null;
        }

        private void UpdateSavedLotGhost(DistrictLotRecord placement)
        {
            if (movingDistrictLot != null) return;
            if (!lotGhost)
            {
                lotGhost = new GameObject("Lot placement preview");
                lotGhost.transform.SetParent(transform, false);
                var catalog = LotAssetCatalog.Load();
                if (!catalog) return;
                foreach (var item in placement.definition.objects)
                {
                    var asset = catalog.Find(item.assetId);
                    if (asset == null || !asset.prefab) continue;
                    var instance = LotRuntimeLoader.InstantiateObject(item, asset, lotGhost.transform, VisualSeason);
                    foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                    foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            var source = materials[i];
                            if (!source || !source.HasProperty("_BaseColor")) continue;
                            var key = (source, asset.category == "Landscaping");
                            if (!lotGhostMaterials.TryGetValue(key, out var faded))
                            {
                                faded = new Material(source);
                                var color = faded.GetColor("_BaseColor");
                                color.a *= key.Item2 ? .82f : .52f;
                                faded.SetColor("_BaseColor", color);
                                faded.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                                LotGroundTexture.ConfigureTransparentLayer(faded, LotGroundOverlay.MaxCount + 3);
                                lotGhostMaterials.Add(key, faded);
                            }
                            materials[i] = faded;
                        }
                        renderer.sharedMaterials = materials;
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }
            lotGhost.SetActive(true);
            if (ghostPlacement == placement) return;
            ghostPlacement = placement;
            lotGhost.transform.SetPositionAndRotation(new Vector3(placement.center.x, 0, placement.center.y),
                Quaternion.Euler(0, placement.FacingDegrees, 0));
            var footprint = new DistrictLotFootprint(placement);
            float level = !placement.definition.levelLot || placement.definition.IsWaterfront ? 0 : DistrictLotFoundation.SampleLevel(placement.definition.Size,footprint.WorldPoint,NaturalLotHeight);
            var assets = LotAssetCatalog.Load();
            var objects = placement.definition.objects.Where(item => assets.Find(item.assetId)?.prefab).ToArray();
            for(int i=0;i<lotGhost.transform.childCount&&i<objects.Length;i++)
            {
                var item=lotGhost.transform.GetChild(i);var authored=objects[i];
                var position=item.position;
                position.y=!placement.definition.levelLot||placement.definition.IsWaterfront?NaturalLotHeight(new Vector2(position.x,position.z)):level;
                if(placement.definition.IsWaterfront&&LotRuntimeLoader.Footprint(authored,assets.Find(authored.assetId)).Overlaps(placement.definition.WaterfrontBounds))
                    position.y=Mathf.Max(position.y,placement.waterfrontDeckHeight);
                position.y+=authored.elevation+placement.definition.elevation+placement.elevation;
                item.position=position;
            }
        }

        private void DestroySavedLotGhost()
        {
            if (lotGhost)
            {
                lotGhost.SetActive(false);
                Destroy(lotGhost);
            }
            lotGhost = null;
            ghostPlacement = null;
            foreach (var material in lotGhostMaterials.Values) Destroy(material);
            lotGhostMaterials.Clear();
        }

        private void ClearLotStructures(DistrictLotRecord record)
        {
            var footprint = new DistrictLotFootprint(record);
            var removed = PlacedLots.Where(other => other != movingDistrictLot &&
                other.id != record.id && footprint.Overlaps(new DistrictLotFootprint(other))).ToHashSet();
            PlacedLots.RemoveAll(removed.Contains);
            if (removed.Count > 0) QueueDemolitionGrowth(removed);
            PlacedActors.RemoveAll(actor =>
            {
                if (actor.zoningStreetlamp || actor.streetscape == null || !footprint.Contains(actor.position)) return false;
                if (streetscapeViews.Remove(actor, out var view) && view)
                {
                    view.SetActive(false);
                    Destroy(view);
                }
                return true;
            });
        }

        private readonly HashSet<DistrictLotRecord> revealingLots = new();

        private System.Collections.IEnumerator LotPlacementDust(DistrictLotRecord record)
        {
            districtLotRoots.TryGetValue(record,out var hiddenLot);
            float ground=LotHeight(record.center),height=8;
            if(hiddenLot)foreach(var renderer in hiddenLot.GetComponentsInChildren<Renderer>(true))height=Mathf.Max(height,renderer.bounds.max.y-ground);
            var veil=ConstructionDustVeil.Create(transform,record,ground,height);
            try { yield return new WaitForSeconds(6f); }
            finally {
                revealingLots.Remove(record);
                if(districtLotRoots.TryGetValue(record,out var revealed)&&revealed)revealed.gameObject.SetActive(true);
                if(veil)veil.Reveal();
            }
        }

        private void ClearLotVegetation(DistrictLotRecord record) => ClearDevelopedSites(new[] { record });

        private void UpdateRegionDistrictHover()
        {
            int district = -1;
            if (SelectedDistrict < 0 && !DistrictLoading && Application.isFocused && relief &&
                relief.gameObject.activeSelf && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
                relief.TryPick(GetComponentInChildren<Camera>().ScreenPointToRay(Input.mousePosition), out district);
            SetRegionDistrictHover(district);
        }

        private void SetRegionDistrictHover(int district)
        {
            if (hoveredDistrictGroups == CombinedDistricts &&
                (district < 0 ? hoveredDistrictMembers == null :
                    hoveredDistrictMembers != null && Array.IndexOf(hoveredDistrictMembers, district) >= 0))
                return;

            hoveredDistrictGroups = CombinedDistricts;
            hoveredDistrictMembers = district < 0 ? null : DistrictMembers(district);
            if (district < 0)
            {
                if (districtHover) districtHover.enabled = false;
                return;
            }
            if (!districtHover)
            {
                districtHover = new GameObject("Hovered district border").AddComponent<LineRenderer>();
                districtHover.transform.SetParent(transform, false);
                districtHover.sharedMaterial = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
                districtHover.sharedMaterial.SetColor("_BaseColor", new Color(1f, .78f, .33f));
                districtHover.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                districtHover.widthMultiplier = DistrictSide * .008f;
                districtHover.loop = true;
            }
            var edges = new HashSet<(Vector2Int from, Vector2Int to)>();
            foreach (int member in hoveredDistrictMembers)
            {
                var lowerLeft = new Vector2Int(member % GridSide, member / GridSide);
                var lowerRight = lowerLeft + Vector2Int.right;
                var upperRight = lowerRight + Vector2Int.up;
                var upperLeft = lowerLeft + Vector2Int.up;
                AddEdge(lowerLeft, lowerRight);
                AddEdge(lowerRight, upperRight);
                AddEdge(upperRight, upperLeft);
                AddEdge(upperLeft, lowerLeft);
            }

            const int samplesPerEdge = 32;
            var nextCorner = edges.ToDictionary(edge => edge.from, edge => edge.to);
            districtHover.positionCount = edges.Count * samplesPerEdge;
            var corner = edges.First().from;
            for (int edge = 0; edge < edges.Count; edge++)
            {
                var next = nextCorner[corner];
                for (int step = 0; step < samplesPerEdge; step++)
                {
                    var point = Vector2.Lerp(corner, next, step / (float)samplesPerEdge) / GridSide;
                    var surface = relief.SurfacePoint(point);
                    districtHover.SetPosition(edge * samplesPerEdge + step, surface + Vector3.up * (DistrictSide * .004f));
                }
                corner = next;
            }

            void AddEdge(Vector2Int from, Vector2Int to)
            {
                // Opposing edges are shared by two members and lie inside the group.
                if (!edges.Remove((to, from))) edges.Add((from, to));
            }
            districtHover.enabled = true;
        }
    }
}
