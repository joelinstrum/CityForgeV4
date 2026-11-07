using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    public sealed class LotDefinition
    {
        public const int MaxTiles = 16;
        public const float TileMeters = 8f;
        public int schema = 1;
        public string id = Guid.NewGuid().ToString("N");
        public string name = "Colonial Homestead";
        public int cost;
        public int monthlyCost;
        public string category = "";
        public string subcategory = "";
        public string firstEraId = "";
        public int firstEraYear;
        public int width = 3;
        public int length = 4;
        public List<LotObjectDefinition> objects = new List<LotObjectDefinition>();

        public List<LotPathDefinition> paths = new List<LotPathDefinition>();

        public string baseTexture = "";
        public string overlayTexture = "";
        public int baseTextureSpan = 1;
        public int overlayTextureSpan = 1;
        public List<LotGroundOverlay> overlays = new List<LotGroundOverlay>();

        public Vector2 Size => new Vector2(width, length) * TileMeters;

        public static LotDefinition Parse(string json)
        {
            var lot = JsonUtility.FromJson<LotDefinition>(json);
            if (lot == null || lot.schema != 1 || !Guid.TryParseExact(lot.id, "N", out _) ||
                string.IsNullOrWhiteSpace(lot.name) || !ValidSize(lot.width, lot.length) ||
                lot.cost < 0 || lot.monthlyCost < 0 || lot.objects == null || lot.objects.Count > 512)
                throw new InvalidDataException("Invalid or unsupported lot definition.");

            lot.category ??= "";
            lot.subcategory ??= "";
            lot.firstEraId ??= "";
            if (!LotClassification.IsValid(lot.category, lot.subcategory, lot.firstEraId, lot.firstEraYear))
                throw new InvalidDataException("Invalid lot classification.");

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

        public static bool ValidSize(int width, int length)
        {
            return width >= 1 && width <= MaxTiles && length >= 1 && length <= MaxTiles;
        }
    }

    [Serializable]
    public struct LotCharacterScript
    {
        public bool configured;
        public int idlePercent;
        public int walkTalkPercent;
        public bool stayOnPaths;
        public bool enterBuildings;
        public bool interactWithNpcs;

        internal static LotCharacterScript Default => new LotCharacterScript
        {
            configured = true, idlePercent = 75, walkTalkPercent = 25
        };

        internal bool IsValid => !configured ||
            (idlePercent >= 0 && idlePercent <= 100 && walkTalkPercent >= 0 && walkTalkPercent <= 100);
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
