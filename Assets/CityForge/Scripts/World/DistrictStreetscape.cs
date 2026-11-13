using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private const string StreetscapeHistoryKey = "CityForge.Streetscape.Recent";
        private LotAssetCatalogEntry streetscapeAsset;
        private PointWireTool streetscapeWire;
        private Transform streetscapeRoot;

        private static string[] StreetscapeHistory() =>
            PlayerPrefs.GetString(StreetscapeHistoryKey).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        private void ArmStreetscape(LotAssetCatalogEntry asset)
        {
            CloseTransportModal();
            CancelStreetscape();
            streetscapeAsset = asset;
            if (!DrawsFenceRun(asset))
            {
                ShowDistrictMessage($"Click to place {asset.label} · Esc to stop");
                return;
            }

            if (!streetscapeWire)
            {
                streetscapeWire = gameObject.AddComponent<PointWireTool>();
                streetscapeWire.Initialize(GetComponentInChildren<Camera>(), townToolbar.parent,
                    new StreetscapeSurface(this), new Color(.78f, .53f, 1f));
                streetscapeWire.OpenPath = true;
                streetscapeWire.HandleEscape = false;
                streetscapeWire.RepeatOnCompletion = true;
                streetscapeWire.RestrictUiToHud = true;
                streetscapeWire.CompletionRequested = points => PlaceFenceRun(points, false);
                streetscapeWire.LoopCompletionRequested = points => PlaceFenceRun(points, true);
            }
            streetscapeWire.Label = asset.label;
            streetscapeWire.DrawingHint = "Click to draw fence spans · Enter to finish · Click first marker to close · Esc to stop";
            streetscapeWire.Arm();
        }

        private static bool DrawsFenceRun(LotAssetCatalogEntry asset) =>
            asset.category == "Fences" && asset.fenceSpan >= .1f;

        private void CancelStreetscape()
        {
            if (streetscapeAsset == null) return;
            streetscapeAsset = null;
            if (streetscapeWire) streetscapeWire.Cancel();
            if (districtMessage) ShowDistrictMessage("");
        }

        private bool UpdateStreetscape()
        {
            if (streetscapeAsset == null) return false;
            if (!CanBuildTown || !TownConstructionVisible || IsSaving || Input.GetKeyDown(KeyCode.Escape))
                CancelStreetscape();
            else if (!DrawsFenceRun(streetscapeAsset) && Application.isFocused && Input.GetMouseButtonDown(0))
            {
                var screen = (Vector2)Input.mousePosition;
                if (!RoadContextOverUi(screen) && TryRoadGround(screen, out var point))
                {
                    var item = new LotObjectDefinition { assetId = streetscapeAsset.id, position = point };
                    ShowDistrictMessage(PlaceStreetscape(new[] { item }) ?? $"{streetscapeAsset.label} placed · Esc to stop");
                }
            }
            return true;
        }

        private string PlaceFenceRun(IReadOnlyList<Vector2> points, bool closed)
        {
            if (streetscapeAsset == null || !CanPlaceRoad) return "Fence placement is unavailable.";
            if (!LotFenceLayout.TryBuild(points, closed, streetscapeAsset, MaxPlacedActors - PlacedActors.Count,
                    null, out var pieces, out var problem)) return problem ?? "Draw at least one fence span.";
            return PlaceStreetscape(pieces);
        }

        private string PlaceStreetscape(IReadOnlyList<LotObjectDefinition> pieces)
        {
            if (pieces.Count > MaxPlacedActors - PlacedActors.Count) return "The region has reached its placement limit.";
            var water = DistrictLotWater();
            foreach (var piece in pieces)
            {
                var center = piece.position;
                var direction = new Vector2(Mathf.Cos(piece.yaw * Mathf.Deg2Rad), -Mathf.Sin(piece.yaw * Mathf.Deg2Rad));
                int steps = Mathf.Max(1, Mathf.CeilToInt(piece.fenceLength));
                for (int i = 0; i <= steps; i++)
                {
                    var point = center + direction * (piece.fenceLength * ((float)i / steps - .5f));
                    if (!InActiveDistrict(point) || TryLotWater(point, LotHeight(point), water, out _) ||
                        PickDistrictRoad(point) != null || roadBuildings.Any(building => building.Contains(point, .1f)))
                        return "Keep the placement on dry ground, clear of roads and buildings.";
                }
            }
            foreach (var piece in pieces)
            {
                var record = new DistrictPlacedActor
                {
                    assetId = piece.assetId, position = piece.position, streetscape = piece,
                    district = DistrictAtWorld(new Vector3(piece.position.x, 0, piece.position.y))
                };
                PresentStreetscape(record, streetscapeAsset);
                PlacedActors.Add(record);
            }
            PlayerPrefs.SetString(StreetscapeHistoryKey, string.Join("|", StreetscapeHistory()
                .Prepend(streetscapeAsset.id).Distinct().Take(32)));
            return null;
        }

        private void PresentStreetscape(DistrictPlacedActor record, LotAssetCatalogEntry asset)
        {
            if (!streetscapeRoot)
            {
                streetscapeRoot = new GameObject("District Streetscape").transform;
                streetscapeRoot.SetParent(transform, false);
            }
            var instance = LotRuntimeLoader.InstantiateObject(record.streetscape, asset, streetscapeRoot);
            instance.transform.position = new Vector3(record.position.x, LotHeight(record.position), record.position.y);
            LamppostLight.Attach(instance, asset, TimeOfDay);
        }

        private void RebuildStreetscape()
        {
            if (streetscapeRoot)
            {
                streetscapeRoot.gameObject.SetActive(false);
                Destroy(streetscapeRoot.gameObject);
                streetscapeRoot = null;
            }
            if (SelectedDistrict < 0) return;
            var catalog = LotAssetCatalog.Load();
            if (!catalog) return;
            foreach (var record in PlacedActors)
                if (record.streetscape != null && ActiveDistrictMembers.Contains(record.district) &&
                    catalog.Find(record.assetId) is { } asset && asset.prefab)
                    PresentStreetscape(record, asset);
        }

        private sealed class StreetscapeSurface : PointWireSurface
        {
            private readonly PrototypeRegionView view;
            public StreetscapeSurface(PrototypeRegionView view) => this.view = view;
            public override bool Available => view && view.CanEditRoad && view.streetscapeAsset != null;
            public override bool CanEdit => Available;
            public override Collider Collider => view.RoadTerrain.GetComponent<TerrainCollider>();
            public override bool Contains(Vector2 point) => view.InActiveDistrict(point);
            public override float Height(Vector2 point) => view.LotHeight(point);
        }
    }
}
