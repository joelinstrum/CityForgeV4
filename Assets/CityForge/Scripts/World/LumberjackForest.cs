using System;
using CityForge.Lots;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class MeadowDistrictLandscape
    {
        private readonly Dictionary<Vector2, DistrictLumberjack> lumberjackClaims = new();

        internal bool TryClaimTimber(DistrictLumberjack worker, Vector2 home, Predicate<Vector2> reachable, out Vector2 target)
        {
            target = default;
            if (!TreesEnabled || IsRegionOverview || View.DistrictLoading || View.IsSaving || !DisplayTerrain || View.ForestRemovals.Length >= 8192)
                return false;
            EnsureTimberRouteIndex();
            if (!timberSearches.TryGetValue(worker, out var search) || search.home != home || search.center != worker.CaptureCenter)
                timberSearches[worker] = search = new TimberSearch { home = home, center = worker.CaptureCenter };
            var state = new NavigationState(View);
            // Keep the uncut alternatives even when harvesting replaces the canonical array.
            // Only gather again when the remembered set has no available candidates and the forest changed.
            if (!search.gathered || (search.exhausted && !ReferenceEquals(search.source, timberRouteSource)))
            {
                search.gathered = true; TimberCandidateSearches++;
                search.candidates.Clear(); search.source = timberRouteSource; search.exhausted = false;
                float radius = LotExtraction.CaptureRadius, height = Height(search.center);
                var low = Vector2Int.FloorToInt((search.center - Vector2.one * radius) / 8f);
                var high = Vector2Int.FloorToInt((search.center + Vector2.one * radius) / 8f);
                for (int z = low.y; z <= high.y; z++) for (int x = low.x; x <= high.x; x++)
                    if (timberRouteCells.TryGetValue(new Vector2Int(x,z), out var trees)) foreach (var point in trees)
                    {
                        float rise = Height(point) - height;
                        if ((point-home).sqrMagnitude >= radius*radius || !Contains(point) ||
                            (point-search.center).sqrMagnitude + rise*rise > radius*radius) continue;
                        search.candidates.Add(new TimberCandidate { point = point });
                    }
                search.candidates.Sort((a,b) => (a.point-home).sqrMagnitude.CompareTo((b.point-home).sqrMagnitude));
            }
            search.exhausted = true;
            foreach (var candidate in search.candidates)
            {
                var point = candidate.point;
                if (!timberPoints.Contains(point) ||
                    (lumberjackClaims.TryGetValue(point, out var owner) && owner)) continue;
                if (MixedForest && View.VisualSeason == RegionSeason.Summer && !nativeCapturePoints.Contains(point) &&
                    !MixedForest.HasTimberAt(point)) continue;
                if (!candidate.checkedRoute || !candidate.state.Equals(state))
                { candidate.accessible = reachable(point); candidate.state = state; candidate.checkedRoute = true; }
                if (!candidate.accessible) continue;
                target = point; lumberjackClaims[target] = worker; search.exhausted = false; return true;
            }
            return false;
        }

        private sealed class TimberCandidate
        { internal Vector2 point; internal NavigationState state; internal bool checkedRoute, accessible; }
        private sealed class TimberSearch
        {
            internal Vector2 home, center;
            internal object source;
            internal bool exhausted, gathered;
            internal readonly List<TimberCandidate> candidates = new();
        }
        internal int TimberCandidateSearches { get; private set; }
        private readonly Dictionary<DistrictLumberjack, TimberSearch> timberSearches = new();
        internal void ForgetTimberSearch(DistrictLumberjack worker) => timberSearches.Remove(worker);

        private TreeInstance[] timberRouteSource;
        private readonly Dictionary<Vector2Int, List<Vector2>> timberRouteCells = new();
        private readonly HashSet<Vector2> nativeCapturePoints = new();
        private readonly HashSet<Vector2> timberPoints = new();
        private void EnsureTimberRouteIndex()
        {
            var source = CanonicalTreeInstances;
            if (!ReferenceEquals(timberRouteSource, source))
            {
                timberRouteSource = source;
                timberRouteCells.Clear();nativeCapturePoints.Clear();timberPoints.Clear();
                foreach (var tree in source)
                {
                    if (tree.prototypeIndex < 3 || !ForestTree(tree.prototypeIndex)) continue;
                    var point = Origin + new Vector2(tree.position.x, tree.position.z) * Side;
                    var cell = Vector2Int.FloorToInt(point / 8f);
                    if (!timberRouteCells.TryGetValue(cell, out var trees)) timberRouteCells[cell] = trees = new List<Vector2>();
                    trees.Add(point);timberPoints.Add(point);
                    if(!MixedDistrictForest.ReplacedPrototype(tree.prototypeIndex))nativeCapturePoints.Add(point);
                }
            }
        }

        internal int CountCaptureTrees(Vector3 origin, float radius, Predicate<Vector2> exclude, int limit)
        {
            EnsureTimberRouteIndex();
            var center = new Vector2(origin.x,origin.z);int count=0;float radius2=radius*radius;
            var low=Vector2Int.FloorToInt((center-Vector2.one*radius)/8f);
            var high=Vector2Int.FloorToInt((center+Vector2.one*radius)/8f);
            for(int z=low.y;z<=high.y;z++)for(int x=low.x;x<=high.x;x++)
                if(timberRouteCells.TryGetValue(new Vector2Int(x,z),out var trees))foreach(var point in trees)
                {
                    float distance=(point-center).sqrMagnitude;
                    if(distance>radius2||!Contains(point)||exclude(point))continue;
                    float rise=Height(point)-origin.y;if(distance+rise*rise>radius2)continue;
                    if(MixedForest&&View.VisualSeason==RegionSeason.Summer&&!nativeCapturePoints.Contains(point)&&!MixedForest.HasTimberAt(point))continue;
                    if(++count>=limit)return count;
                }
            return count;
        }

        internal bool TimberRouteClear(Vector2 from, Vector2 to)
        {
            EnsureTimberRouteIndex();
            var segment = to - from;
            float length2 = segment.sqrMagnitude;
            var low = Vector2Int.FloorToInt((Vector2.Min(from, to) - Vector2.one * .65f) / 8f);
            var high = Vector2Int.FloorToInt((Vector2.Max(from, to) + Vector2.one * .65f) / 8f);
            for (int z = low.y; z <= high.y; z++)
            for (int x = low.x; x <= high.x; x++)
                if (timberRouteCells.TryGetValue(new Vector2Int(x,z), out var trees))
                    foreach (var point in trees)
                    {
                        float t = length2 > 0 ? Mathf.Clamp01(Vector2.Dot(point - from, segment) / length2) : 0;
                        if ((point - (from + t * segment)).sqrMagnitude < .65f * .65f) return false;
                    }
            return true;
        }

        internal void ReleaseTimber(DistrictLumberjack worker, Vector2 target)
        {
            if (lumberjackClaims.TryGetValue(target, out var owner) && owner == worker) lumberjackClaims.Remove(target);
        }

        internal bool FellClaimedTimber(DistrictLumberjack worker, Vector2 target, Vector2 from)
        {
            if (!lumberjackClaims.TryGetValue(target, out var owner) || owner != worker || View.DistrictLoading || View.IsSaving) return false;
            foreach (var tree in CanonicalTreeInstances)
            {
                var p = Origin + new Vector2(tree.position.x, tree.position.z) * Side;
                if ((p - target).sqrMagnitude > .0001f) continue;
                if (tree.prototypeIndex < 3 || !ForestTree(tree.prototypeIndex)) return false;
                var prefab = terrainData.treePrototypes[tree.prototypeIndex].prefab;
                var position = new Vector3(p.x, DisplayTerrain.transform.position.y + tree.position.y * terrainData.size.y, p.y);
                var rotation = Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0);
                var scale = prefab ? Vector3.Scale(prefab.transform.localScale, new Vector3(tree.widthScale, tree.heightScale, tree.widthScale)) : Vector3.one;
                if (MixedForest && View.VisualSeason == RegionSeason.Summer && MixedDistrictForest.ReplacedPrototype(tree.prototypeIndex))
                {
                    if (!MixedForest.TryTimberVisual(p, out prefab, out position, out rotation, out scale)) return false;
                }
                if (!prefab || RemoveForest(p, .05f) == 0) return false;
                var falling = Instantiate(prefab, position, rotation, transform);
                falling.name = "Harvested " + prefab.name;
                falling.transform.localScale = scale;
                foreach (var collider in falling.GetComponentsInChildren<Collider>()) collider.enabled = false;
                var away = p - from;
                if (away.sqrMagnitude < .01f) away = Vector2.right;
                StartCoroutine(AnimateFirFall(falling.transform, Vector3.Cross(Vector3.up, new Vector3(away.x, 0, away.y).normalized)));
                ReleaseTimber(worker, target);
                return true;
            }
            return false;
        }
    }
}
