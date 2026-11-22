using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class AudioCanopyIndex
    {
        private const float CellSize = 24f;
        private readonly Dictionary<Vector2Int, List<Vector2>> cells = new();
        private TreeInstance[] trees;
        private Vector2 origin;
        private Vector2 size;
        private int cursor, smallEnd;
        internal bool Ready => trees != null && cursor >= trees.Length;
        internal void Begin(TreeInstance[] source, Vector2 offset, Vector2 terrainSize, int baselineCount)
        { trees = source; origin = offset; size = terrainSize; smallEnd = baselineCount; cursor = 0; cells.Clear(); }
        internal void Advance(int budget)
        {
            if (trees == null) return;
            int end = Mathf.Min(trees.Length, cursor + budget);
            for (; cursor < end; cursor++)
            {
                var tree = trees[cursor]; int index = tree.prototypeIndex;
                if (index < 3 || (index >= 11 && index < smallEnd)) continue;
                var point = origin + Vector2.Scale(new Vector2(tree.position.x, tree.position.z), size);
                var key = Key(point);
                if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<Vector2>();
                list.Add(point);
            }
        }
        private static Vector2Int Key(Vector2 point) => Vector2Int.FloorToInt(point / CellSize);
        internal bool OverCanopy(Vector2 point)
        {
            if (!Ready) return false;
            var key = Key(point); int nearby = 0; float closest = float.PositiveInfinity;
            for (int z = -1; z <= 1; z++) for (int x = -1; x <= 1; x++)
            {
                if (!cells.TryGetValue(key + new Vector2Int(x, z), out var treesInCell)) continue;
                foreach (var tree in treesInCell)
                {
                    float distance = (tree - point).sqrMagnitude;
                    if (distance > 18f * 18f) continue;
                    nearby++; closest = Mathf.Min(closest, distance);
                }
            }
            return nearby >= 3 && closest <= 8f * 8f;
        }
    }
}
