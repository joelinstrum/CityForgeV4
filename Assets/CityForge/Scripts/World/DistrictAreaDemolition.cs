using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    internal static class AreaDemolition
    {
        internal static bool Contains(Rect bounds, Vector2 point) => point.x >= bounds.xMin && point.x <= bounds.xMax &&
            point.y >= bounds.yMin && point.y <= bounds.yMax;

        internal static List<DistrictRoadRecord> Outside(DistrictRoadRecord road, IReadOnlyList<Vector2> line, Rect bounds, int markerLimit = 64)
        {
            var box = new ParcelGridCell(bounds.center, bounds.size);
            var result = new List<DistrictRoadRecord>(); var run = new List<Vector2>(); bool cut = false;
            int segments = road.closed ? line.Count : line.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                var a = line[i]; var b = line[(i + 1) % line.Count];
                if (!box.InteriorInterval(a, b, out var interval)) { Add(a, b); continue; }
                cut = true;
                if (interval.x > .00001f) Add(a, Vector2.Lerp(a, b, interval.x));
                Finish();
                if (interval.y < .99999f) Add(Vector2.Lerp(a, b, interval.y), b);
            }
            if (!cut) return new List<DistrictRoadRecord> { road };
            Finish();
            if (road.closed && result.Count > 1 && (result[^1].points[^1] - result[0].points[0]).sqrMagnitude < .0001f)
            {
                result[0].points = result[^1].points.Concat(result[0].points.Skip(1)).ToArray(); result.RemoveAt(result.Count - 1);
            }
            var bounded = new List<DistrictRoadRecord>();
            foreach (var piece in result)
                for (int start = 0; start < piece.points.Length - 1; start += markerLimit - 1)
                    bounded.Add(new DistrictRoadRecord { district = piece.district, surfaceId = piece.surfaceId, streetTreeStyle=piece.streetTreeStyle, width = piece.width,
                        gridAligned = piece.gridAligned, points = piece.points.Skip(start).Take(markerLimit).ToArray() });
            return bounded;
            void Add(Vector2 a, Vector2 b)
            {
                if ((a - b).sqrMagnitude < .000001f) return;
                if (run.Count == 0) run.Add(a);
                else if ((run[^1] - a).sqrMagnitude > .0001f) { Finish(); run.Add(a); }
                run.Add(b);
            }
            void Finish()
            {
                if (run.Count >= 2)
                {
                    var simplified = new List<Vector2> { run[0] };
                    for (int i = 1; i < run.Count - 1; i++)
                    {
                        var a = simplified[^1]; var b = run[i]; var c = run[i + 1];
                        if ((b - a).sqrMagnitude < 1f || Vector2.Dot((b - a).normalized, (c - b).normalized) > .99999f) continue;
                        simplified.Add(b);
                    }
                    var end = run[^1];
                    while (simplified.Count > 1 && (simplified[^1] - end).sqrMagnitude < 1f) simplified.RemoveAt(simplified.Count - 1);
                    if ((simplified[^1] - end).sqrMagnitude >= 1f)
                    {
                        simplified.Add(end);
                        bool clear = true;
                        for (int i = 1; i < simplified.Count; i++)
                            if (box.InteriorInterval(simplified[i - 1], simplified[i], out _)) { clear = false; break; }
                        result.Add(new DistrictRoadRecord { district = road.district, surfaceId = road.surfaceId, streetTreeStyle=road.streetTreeStyle,
                            width = road.width, gridAligned = road.gridAligned, points = clear ? simplified.ToArray() : run.ToArray() });
                    }
                }
                run.Clear();
            }
        }
    }
    public sealed partial class PrototypeRegionView
    {
        private void RefreshDetonateAreaOutline()
        {
            if (detonateArea is not Rect area) return;
            if (!detonateAreaOutline)
            {
                detonateAreaOutline = new GameObject("Destruction rectangle").AddComponent<LineRenderer>();
                detonateAreaOutline.transform.SetParent(transform, false);
                detonateAreaOutline.loop = true; detonateAreaOutline.useWorldSpace = true;
                detonateAreaOutline.widthMultiplier = .24f;
                detonateAreaOutline.shadowCastingMode = ShadowCastingMode.Off; detonateAreaOutline.receiveShadows = false;
                detonateAreaOutline.sharedMaterial = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
                detonateAreaOutline.sharedMaterial.SetColor("_BaseColor", new Color(1f, .12f, .08f));
            }
            var corners = new[] { area.min, new Vector2(area.xMin, area.yMax), area.max, new Vector2(area.xMax, area.yMin) };
            var vertices = new List<Vector3>();
            for (int edge = 0; edge < 4; edge++)
            {
                var a = corners[edge]; var b = corners[(edge + 1) % 4];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 4));
                for (int step = 0; step < steps; step++)
                {
                    var p = Vector2.Lerp(a, b, (float)step / steps);
                    vertices.Add(new Vector3(p.x, LotHeight(p) + .4f, p.y));
                }
            }
            detonateAreaOutline.positionCount = vertices.Count; detonateAreaOutline.SetPositions(vertices.ToArray());
            detonateAreaOutline.enabled = true;
        }

        private void CommitDetonateArea(Rect area)
        {
            if (ForestRemovals.Length >= 8192)
            { ShowDistrictMessage("Forest removal edit limit reached. The selection was not changed."); ClearDetonateStroke(); return; }
            bool Inside(Vector2 point) => AreaDemolition.Contains(area, point) && InActiveDistrict(point);
            bool WholeLot(DistrictLotRecord lot)
            {
                var footprint = new DistrictLotFootprint(lot); var half = lot.definition.Size * .5f;
                return ActiveDistrictMembers.Contains(lot.district) && AreaDemolition.Contains(area, footprint.WorldPoint(-half)) && AreaDemolition.Contains(area, footprint.WorldPoint(half)) &&
                    AreaDemolition.Contains(area, footprint.WorldPoint(new Vector2(-half.x, half.y))) && AreaDemolition.Contains(area, footprint.WorldPoint(new Vector2(half.x, -half.y)));
            }
            // Prepare all road changes before mutating anything. A crossing road keeps its outside pieces.
            var replacements = new List<DistrictRoadRecord>(); var changedRoads = new HashSet<DistrictRoadRecord>();
            foreach (var road in districtRoads)
            {
                if (!ActiveDistrictMembers.Contains(road.district)) { replacements.Add(road); continue; }
                var pieces = AreaDemolition.Outside(road, RoadCenterline(road), area, MaxRoadPoints);
                replacements.AddRange(pieces);
                if (pieces.Count != 1 || pieces[0] != road) changedRoads.Add(road);
            }
            if (replacements.Any(road => changedRoads.Count > 0 && RoadGuideProblem(road.points, road.closed, MapSide * .5f) != null))
            { ShowDistrictMessage("Move the selection edge slightly: it would leave an invalid road fragment."); ClearDetonateStroke(); return; }
            if (replacements.Count > MaxDistrictRoads)
            { ShowDistrictMessage("This selection would leave too many road fragments. Use a larger selection."); ClearDetonateStroke(); return; }
            var keptPaths=new List<DistrictParkPathRecord>();
            foreach(var record in parkPaths)
            {
                if(!ActiveDistrictMembers.Contains(record.district)){keptPaths.Add(record);continue;}
                var path=record.path;var line=new List<Vector2>();if(path.curved)LotPathSurface.RoundCorners(path.points,null,path.closed,path.width,line);else line.AddRange(path.points);
                var proxy=new DistrictRoadRecord{district=record.district,points=line.ToArray(),closed=path.closed,width=path.width};
                foreach(var piece in AreaDemolition.Outside(proxy,line,area,MaxRoadPoints))
                {
                    var copy=JsonUtility.FromJson<LotPathDefinition>(JsonUtility.ToJson(path));copy.points=piece.points;copy.closed=piece.closed;copy.curved=false;keptPaths.Add(new DistrictParkPathRecord{district=record.district,path=copy});
                }
            }
            var removedBridges=ActiveBridges.Where(b=>BridgeOutline(b,0).All(Inside)).ToArray();
            if(removedBridges.Length>0){RestoreBridgeEarthwork();foreach(var bridge in removedBridges)bridges.Remove(bridge);}
            parkPaths.Clear();parkPaths.AddRange(keptPaths);
            var removedLots=PlacedLots.Where(WholeLot).ToArray();
            int lots = PlacedLots.RemoveAll(WholeLot);
            if(lots>0)QueueDemolitionGrowth(removedLots);
            int actors = PlacedActors.RemoveAll(actor => Inside(actor.position));
            foreach (var cell in townZones.Keys.Where(cell => Inside(ZoneCorner(cell) + Vector2.one * (DistrictGridCellMeters * .5f))).ToArray())
            {
                townZones.Remove(cell); zoneWealth.Remove(cell); residentialFacing.Remove(cell); zoneLotSizes.Remove(cell); zoneLotAnchors.Remove(cell);
                localGrowthCells.Remove(cell);
            }
            districtRoads.Clear(); districtRoads.AddRange(replacements);
            undoRoadEdits.RemoveAll(edit => changedRoads.Contains(edit.Road)); redoRoadEdits.RemoveAll(edit => changedRoads.Contains(edit.Road));
            var removals = new List<ForestRemovalStroke>(ForestRemovals) { new ForestRemovalStroke {
                x = area.center.x, z = area.center.y, radius = 1, rectangleSize = area.size, districts = (int[])ActiveDistrictMembers.Clone() } };
            ForestRemovals = removals.ToArray(); WoodlandRevision++;
            ForestAdditions = ForestAdditions.Where(tree => !Inside(new Vector2(tree.x, tree.z))).ToArray(); additionIndex = null;
            if (ApprovedDistrict)
            {
                ApprovedDistrict.ClearTreesUnderLot(Inside);
                ApprovedDistrict.Geography?.Invalidate(TerrainGeography.Pad(area, 20));
            }
            foreach (var tile in districtGroundPaint.tiles)
            {
                if (!area.Overlaps(new Rect(tile.Minimum, Vector2.one * GroundPaintData.TileMeters))) continue;
                var bytes = tile.Read(); bool dirty = false;
                for (int z = 0; z < GroundPaintData.Resolution; z++)
                for (int x = 0; x < GroundPaintData.Resolution; x++)
                {
                    var p = tile.Minimum + new Vector2(x + .5f, z + .5f) * GroundPaintData.PixelMeters;
                    if (!Inside(p)) continue;
                    int offset = (z * GroundPaintData.Resolution + x) * GroundPaintData.Layers;
                    Array.Clear(bytes, offset, GroundPaintData.Layers); dirty = true;
                }
                if (dirty) { tile.editing = bytes; tile.Flush(); }
            }
            districtGroundPaint.PruneEmpty();
            SelectDistrictLot(null); ClearDetonateStroke();
            if(changedRoads.Count>0 || removedBridges.Length>0){RebuildDistrictRoads();BuildDetonateRoadRuns();}
            else RebuildParkPaths();
            RemoveDistrictLotViews(removedLots);
            if(lots==0){RefreshZoneOverlay();MarkImmigrationSitesChanged();}
            if(actors>0)RebuildPlacedPedestrians();
            RebuildGroundPaint();
            ShowDistrictMessage($"Cleared the selected area: {lots} lots, {changedRoads.Count} road sections, {actors} placed objects, forest, zoning and ground paint. Save the region to keep it.");
        }
    }
}
