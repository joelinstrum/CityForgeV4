using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal string ResetDistrict(int district)
        {
            if (!CanOpenDistrictContext || district < 0 || district >= DistrictCount)
                return "Choose a district on the region map.";

            var members = new HashSet<int>(DistrictMembers(district));
            var water = Data.WaterBodies ?? Array.Empty<RegionWaterBody>();
            var remainingWater = water.Where(body => !IsLocalPlayerWater(body)).ToArray();
            if (remainingWater.Length != water.Length)
            {
                var map = new GameObject("Regional relief map");
                map.transform.SetParent(transform, false);
                var replacement = map.AddComponent<RegionReliefMap>();
                try
                {
                    Data.WaterBodies = remainingWater;
                    replacement.Build(this);
                }
                catch (Exception error)
                {
                    Data.WaterBodies = water;
                    Destroy(map);
                    Debug.LogException(error);
                    return "Could not restore the regional terrain.";
                }
                if (relief) Destroy(relief.gameObject);
                relief = replacement;
                GetComponent<DistrictCombiner>()?.RefreshBorders(relief);
                Waterless = remainingWater.Length == 0;
            }

            foreach (var town in Towns.Where(t => members.Contains(t.DistrictIndex(GridSide))).ToArray())
                DeleteTown(town);
            PlacedLots.RemoveAll(lot => members.Contains(lot.district));
            PlacedActors.RemoveAll(actor => members.Contains(actor.district));
            districtRoads.RemoveAll(road => members.Contains(road.district));
            undoRoadEdits.RemoveAll(edit => members.Contains(edit.Road.district));
            redoRoadEdits.RemoveAll(edit => members.Contains(edit.Road.district));
            foreach (var cell in townZones.Keys.Where(cell =>
            {
                var center = ZoneCorner(cell) + Vector2.one * (DistrictGridCellMeters * .5f);
                return members.Contains(DistrictAtWorld(new Vector3(center.x, 0f, center.y)));
            }).ToArray())
            {
                townZones.Remove(cell);
                residentialFacing.Remove(cell);
            }

            ForestAdditions = ForestAdditions.Where(tree => !members.Contains(tree.district)).ToArray();
            additionIndex = null;
            ForestRemovals = ForestRemovals.Select(stroke => new ForestRemovalStroke
            {
                x = stroke.x, z = stroke.z, radius = stroke.radius,
                districts = stroke.districts.Where(cell => !members.Contains(cell)).ToArray()
            }).Where(stroke => stroke.districts.Length > 0).ToArray();
            DeepHarborDistricts = DeepHarborDistricts.Where(cell => !members.Contains(cell)).ToArray();
            CombinedDistricts = CombinedDistricts.Where(group => !group.cells.Any(members.Contains)).ToArray();
            foreach (int cell in members) sessionMeadows.Remove(cell);
            if (members.Contains(LoadedMeadowDistrict))
            {
                LoadedMeadow = null;
                LoadedMeadowDistrict = -1;
            }
            GetComponent<DistrictCombiner>()?.RefreshBorders(relief);
            WoodlandRevision++;
            return null;

            bool IsLocalPlayerWater(RegionWaterBody body)
            {
                if (!body.playerCreated && !body.playerChannel) return false;
                var bounds = body.Bounds;
                if (body.playerChannel)
                {
                    float margin = body.halfWidthNormalized + body.bankWidthNormalized;
                    bounds.min -= Vector2.one * margin;
                    bounds.max += Vector2.one * margin;
                }
                bool touched = false;
                for (int cell = 0; cell < DistrictCount; cell++)
                {
                    var area = new Rect(DistrictOrigin(cell) / MapSide + Vector2.one * .5f, Vector2.one / GridSide);
                    if (!area.Overlaps(bounds)) continue;
                    if (!members.Contains(cell)) return false;
                    touched = true;
                }
                return touched;
            }
        }
    }
}
