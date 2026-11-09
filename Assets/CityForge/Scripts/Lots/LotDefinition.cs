using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityForge.Lots
{
    public enum LotType
    {
        Playable = 0,
        Template = 1,
        Zoning = 2
    }

    public enum HarborVesselTraffic { Default, Enabled, Disabled }

    public enum LotWaterfrontSide { North, East, South, West }

    [Serializable]
    public sealed class LotDefinition
    {
        public const int MaxTiles = 16;
        public const float TileMeters = 8f;
        public int schema = 1;
        public bool harborHouseScaleUpdated;
        public LotType type;
        public LotZoningLayout zoningLayout;
        public string id = Guid.NewGuid().ToString("N");
        public string name = "Colonial Homestead";
        public int cost;
        public int monthlyCost;
        public string category = "";
        public string subcategory = "";
        public string firstEraId = "";
        public int firstEraYear;
        public List<LotRequiredResource> requiredResources = new List<LotRequiredResource>();
        public bool requiresKiln;
        public LotCapitalBonus capitalBonus;
        public int width = 3;
        public int length = 4;
        public int waterfrontRows;
        public HarborVesselTraffic harborVesselTraffic;
        public LotWaterfrontSide waterfrontSide;
        public List<LotObjectDefinition> objects = new List<LotObjectDefinition>();

        public List<LotPathDefinition> paths = new List<LotPathDefinition>();

        public string baseTexture = "";
        public string overlayTexture = "";
        public int baseTextureSpan = 1;
        public int baseTextureRotation;
        public int overlayTextureSpan = 1;
        public int overlayTextureRotation;
        public List<LotGroundOverlay> overlays = new List<LotGroundOverlay>();

        public Vector2 Size => new Vector2(width, length) * TileMeters;

        public bool IsWaterfront => waterfrontRows > 0;
        internal bool AttractsVessels => IsWaterfront && (harborVesselTraffic switch
        {
            HarborVesselTraffic.Enabled => true,
            HarborVesselTraffic.Disabled => false,
            _ => (LotClassification.FindCategory(category)?.id == "transportation" &&
                (subcategory == "harbor" || subcategory == "Harbor")) ||
                objects.Exists(item => item.assetId == "harbor-council-house")
        });
        internal int WaterfrontDepth => (int)waterfrontSide % 2 == 0 ? length : width;
        internal Vector2 WaterfrontSize => SizeForRotation((int)waterfrontSide);
        internal float WaterfrontStart => WaterfrontSize.y * .5f - waterfrontRows * TileMeters;
        internal Rect LandBounds => LotGroundOverlay.QuarterTurn(
            new Rect(-WaterfrontSize * .5f, new Vector2(WaterfrontSize.x, WaterfrontDepth * TileMeters - waterfrontRows * TileMeters)),
            (int)waterfrontSide);
        internal Rect WaterfrontBounds => LotGroundOverlay.QuarterTurn(
            new Rect(-WaterfrontSize.x * .5f, WaterfrontStart, WaterfrontSize.x, waterfrontRows * TileMeters),
            (int)waterfrontSide);

        internal Vector2 SizeForRotation(int rotation) => rotation % 2 == 0 ? Size : new Vector2(Size.y, Size.x);
        internal bool OnWaterfront(Vector2 point) => IsWaterfront &&
            LotGroundOverlay.QuarterTurn(point, -(int)waterfrontSide).y >= WaterfrontStart;

        internal Rect ClipToLand(Rect area)
        {
            if (!IsWaterfront) return area;
            var land = LandBounds;
            return Rect.MinMaxRect(Mathf.Max(land.xMin, area.xMin), Mathf.Max(land.yMin, area.yMin),
                Mathf.Min(land.xMax, area.xMax), Mathf.Min(land.yMax, area.yMax));
        }

        internal void RotateClockwise()
        {
            (width, length) = (length, width);
            if (zoningLayout.Configured)
            {
                zoningLayout.buildingCenter = LotGroundOverlay.QuarterTurn(zoningLayout.buildingCenter, 1);
                zoningLayout.buildingSize = new Vector2(zoningLayout.buildingSize.y, zoningLayout.buildingSize.x);
            }
            if (IsWaterfront) waterfrontSide = (LotWaterfrontSide)(((int)waterfrontSide + 1) % 4);
            baseTextureRotation = (baseTextureRotation + 1) % 4;
            overlayTextureRotation = (overlayTextureRotation + 1) % 4;
            foreach (var item in objects)
            {
                item.position = LotGroundOverlay.QuarterTurn(item.position, 1);
                item.yaw = Mathf.Repeat(item.yaw + 90f, 360f);
            }
            foreach (var path in paths)
            {
                for (int i = 0; i < path.points.Length; i++)
                    path.points[i] = LotGroundOverlay.QuarterTurn(path.points[i], 1);
                path.textureRotation = (path.textureRotation + 1) % 4;
            }
            foreach (var overlay in overlays)
            {
                var bounds = LotGroundOverlay.QuarterTurn(overlay.Bounds, 1);
                overlay.min = bounds.min;
                overlay.max = bounds.max;
                if (overlay.anchored) overlay.anchor = LotGroundOverlay.QuarterTurn(overlay.anchor, 1);
                overlay.rotation = (overlay.rotation + 1) % 4;
            }
        }

        public static LotDefinition Parse(string json)
        {
            var lot = JsonUtility.FromJson<LotDefinition>(json);
            if (lot == null || lot.schema != 1 || !Guid.TryParseExact(lot.id, "N", out _) ||
                string.IsNullOrWhiteSpace(lot.name) || !ValidSize(lot.width, lot.length) ||
                !Enum.IsDefined(typeof(LotWaterfrontSide), lot.waterfrontSide) ||
                !Enum.IsDefined(typeof(HarborVesselTraffic), lot.harborVesselTraffic) ||
                lot.waterfrontRows < 0 || lot.waterfrontRows >= lot.WaterfrontDepth ||
                !Enum.IsDefined(typeof(LotType), lot.type) || !lot.zoningLayout.Valid ||
                (lot.type == LotType.Zoning && !lot.zoningLayout.Configured) || lot.cost < 0 || lot.monthlyCost < 0 || lot.objects == null || lot.objects.Count > 512)
                throw new InvalidDataException("Invalid or unsupported lot definition.");

            lot.category ??= "";
            lot.subcategory ??= "";
            lot.firstEraId ??= "";
            if (!LotClassification.IsValid(lot.category, lot.subcategory, lot.firstEraId, lot.firstEraYear))
                throw new InvalidDataException("Invalid lot classification.");

            lot.requiredResources ??= new List<LotRequiredResource>();
            if (!lot.HasValidCapitalSettings())
                throw new InvalidDataException("Invalid lot requirements or capital bonus.");

            var ids = new HashSet<string>();
            foreach (var item in lot.objects)
            {
                if (item == null || !Guid.TryParseExact(item.id, "N", out _) || !ids.Add(item.id) ||
                    string.IsNullOrWhiteSpace(item.assetId) || !float.IsFinite(item.yaw) ||
                    !float.IsFinite(item.scale) || item.scale < .25f || item.scale > 3f ||
                    !float.IsFinite(item.fenceLength) || item.fenceLength < 0f || item.fenceLength > 192f ||
                    !float.IsFinite(item.position.x) || !float.IsFinite(item.position.y) ||
                    !item.characterScript.IsValid)
                    throw new InvalidDataException("Invalid lot object.");
            }
            lot.paths ??= new List<LotPathDefinition>();
            if (lot.paths.Count > LotPathDefinition.MaxPaths)
                throw new InvalidDataException("Too many lot paths.");
            foreach (var path in lot.paths)
            {
                if (path?.points == null || path.points.Length < (path.closed ? 3 : 2) || path.points.Length > 256 ||
                    path.textureRotation < 0 || path.textureRotation > 3 || path.edgeBlend < 0 || path.edgeBlend > LotPathDefinition.MaxEdgeBlend ||
                    Array.IndexOf(LotPathSurface.Widths, path.width) < 0 || LotPathMaterial.Find(path.materialId) == null)
                    throw new InvalidDataException("Invalid lot path.");
                var markers = new HashSet<Vector2>();
                foreach (var point in path.points)
                    if (!float.IsFinite(point.x) || !float.IsFinite(point.y) ||
                        Mathf.Abs(point.x) > MaxTiles * TileMeters / 2 || Mathf.Abs(point.y) > MaxTiles * TileMeters / 2 ||
                        !markers.Add(point))
                        throw new InvalidDataException("Invalid lot path marker.");
            }
            lot.baseTexture ??= "";
            lot.overlayTexture ??= "";
            // Older saves can omit the repeat size.
            if (lot.baseTextureSpan == 0) lot.baseTextureSpan = 1;
            if (lot.overlayTextureSpan == 0) lot.overlayTextureSpan = 1;
            if (LotGroundTexture.Find(false, lot.baseTexture) == null || LotGroundTexture.Find(true, lot.overlayTexture) == null ||
                lot.baseTextureRotation < 0 || lot.baseTextureRotation > 3 ||
                lot.overlayTextureRotation < 0 || lot.overlayTextureRotation > 3 ||
                (lot.baseTextureSpan != 1 && lot.baseTextureSpan != 2) ||
                (lot.overlayTextureSpan != 1 && lot.overlayTextureSpan != 2))
                throw new InvalidDataException("Invalid lot ground textures.");
            lot.overlays ??= new List<LotGroundOverlay>();
            if (lot.overlays.Count > LotGroundOverlay.MaxCount)
                throw new InvalidDataException("Too many lot overlays.");
            foreach (var overlay in lot.overlays)
                if (overlay == null || !overlay.IsValid()) throw new InvalidDataException("Invalid lot overlay.");
            return lot;
        }

        internal bool HasValidCapitalSettings()
        {
            var distinct = new HashSet<LotRequiredResource>();
            foreach (var resource in requiredResources)
                if (!Enum.IsDefined(typeof(LotRequiredResource), resource) || !distinct.Add(resource))
                    return false;
            return capitalBonus.IsValid;
        }

        public static bool ValidSize(int width, int length)
        {
            return width >= 1 && width <= MaxTiles && length >= 1 && length <= MaxTiles;
        }
    }

    public enum LotRequiredResource
    {
        Timber, Stone, Iron, Coal, Fish, FertileSoil, Pasture, FreshWater, NaturalHarbor, ScenicBeauty, Clay
    }

    public enum LotCapitalEffect
    {
        None, PublicBuildingCostReduction, CrimeReduction, CivicPrestigeIncrease,
        PublicBuildingRepairCostReduction, PortFeeRevenueIncrease, PopulationGrowthIncrease
    }

    [Serializable]
    public struct LotCapitalBonus
    {
        public LotCapitalEffect effect;
        public int percent;
        // Zero means ongoing; a positive value limits the public building discount.
        public int eligibleBuildings;
        public bool requiresOperatingPort;

        internal bool IsValid
        {
            get
            {
                if (!Enum.IsDefined(typeof(LotCapitalEffect), effect)) return false;
                if (effect == LotCapitalEffect.None)
                    return percent == 0 && eligibleBuildings == 0 && !requiresOperatingPort;
                return percent >= 1 && percent <= 100 && eligibleBuildings >= 0 && eligibleBuildings <= 99 &&
                    (eligibleBuildings == 0 || effect == LotCapitalEffect.PublicBuildingCostReduction);
            }
        }
    }

    public enum LotScriptedBehavior { None, DockWorker }

    [Serializable]
    public struct LotCharacterScript
    {
        public bool configured;
        public LotScriptedBehavior scriptedBehavior;
        public int dockArrivalDelay;
        public int dockWorkSeconds;
        public bool dockStayVisible;
        public bool dockIdleOnly;
        internal int DockWorkSeconds => dockWorkSeconds == 0 ? 22 : dockWorkSeconds;
        public int idlePercent;
        public int walkTalkPercent;
        public bool stayOnPaths;
        public bool enterBuildings;
        public bool interactWithNpcs;

        internal static LotCharacterScript Default => new LotCharacterScript
        {
            configured = true, idlePercent = 75, walkTalkPercent = 25
        };

        internal bool IsValid => Enum.IsDefined(typeof(LotScriptedBehavior), scriptedBehavior) &&
            dockArrivalDelay >= 0 && dockArrivalDelay <= 60 && dockWorkSeconds >= 0 && dockWorkSeconds <= 120 &&
            (!configured || (idlePercent >= 0 && idlePercent <= 100 && walkTalkPercent >= 0 && walkTalkPercent <= 100));
    }

    [Serializable]
    public sealed class LotObjectDefinition
    {
        public string id = Guid.NewGuid().ToString("N");
        public string assetId;
        public Vector2 position;
        public float yaw;
        public float scale = 1f;
        public float fenceLength;
        public bool fenceEndPost;
        public LotCharacterScript characterScript;
    }

    internal sealed class LotEditorSession
    {
        private readonly Stack<string> undo = new Stack<string>();
        private readonly Stack<string> redo = new Stack<string>();
        private string saved;

        public LotDefinition Lot { get; private set; } = new LotDefinition();
        public bool Dirty => JsonUtility.ToJson(Lot) != saved;
        public bool CanUndo => undo.Count != 0;
        public bool CanRedo => redo.Count != 0;

        public LotEditorSession() { MarkSaved(); }

        public bool Edit(Action<LotDefinition> edit)
        {
            var before = JsonUtility.ToJson(Lot);
            edit(Lot);
            if (before == JsonUtility.ToJson(Lot)) return false;
            undo.Push(before);
            redo.Clear();
            return true;
        }

        public void Undo() => Restore(undo, redo);
        public void Redo() => Restore(redo, undo);

        private void Restore(Stack<string> from, Stack<string> to)
        {
            if (from.Count == 0) return;
            to.Push(JsonUtility.ToJson(Lot));
            Lot = LotDefinition.Parse(from.Pop());
        }

        public void Open(LotDefinition lot)
        {
            Lot = lot;
            undo.Clear();
            redo.Clear();
            MarkSaved();
        }

        public void MarkUnsaved() => saved = null;

        public void MarkSaved() => saved = JsonUtility.ToJson(Lot);
    }
}
