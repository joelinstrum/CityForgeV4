using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        public bool DeveloperMode { get; private set; }
        internal readonly List<DistrictLotRecord> PendingPlayerLots = new();

        internal void SetDeveloperMode(bool developer)
        {
            if (!developer) CloseDeveloperLotTools();
            DeveloperMode = developer;
            QueueLotGrowth(); nextEventTick = 0;
            RefreshImmigrationControl();
            RefreshDeveloperTools();
        }

        private IEnumerable<EventImmigrant> WaitingResidents => RegionEvents.SelectMany(s => s.immigrants)
            .Where(p => !p.settled && p.targetIsZone && p.arrivedAtZone);

        private bool PlayerPopulationPresent => Towns.Where(t=>ActiveDistrictMembers.Contains(t.DistrictIndex(GridSide)))
            .Any(t=>t.development!=null && t.development.population>0);

        private bool PlayerGrowthAllowed(RectInt cells, TownZone zone, LotWealth wealth)
        {
            if (DeveloperMode) return true;
            if (!PlayerPopulationPresent) return false;
            if (IsResidentialZone(zone)) return wealth == LotWealth.Low &&
                WaitingResidents.Any(p => p.destinationLotId!=null && p.destinationLotId.StartsWith("zone:") &&
                    cells.Contains(p.destinationZoneCell));
            if (IsCommercialZone(zone))
            {
                var localLots=PlacedLots.Where(l=>InActiveDistrict(l.center)).Select(l=>l.id).ToHashSet();
                long housed=RegionEvents.Sum(s=>s.moveIns.Where(m=>localLots.Contains(m.lotId)).Sum(m=>m.population)+
                    s.immigrants.Where(p=>p.settled&&!p.awaitingHousing&&localLots.Contains(p.destinationLotId)).Sum(p=>p.Population));
                long shops = PlacedLots.Count(l => InActiveDistrict(l.center) && LotClassification.FindCategory(l.definition.category)?.id == "commercial");
                return housed / 120 > shops;
            }
            return false;
        }

        private void BindResidentsToBuilding(DistrictLotRecord lot, RectInt? cells = null)
        {
            foreach (var person in RegionEvents.SelectMany(s=>s.immigrants).Where(p=>!p.settled && p.targetIsZone &&
                (p.destinationLotId == lot.id || cells.HasValue && cells.Value.Contains(p.destinationZoneCell))).ToArray())
            {
                person.targetIsZone = person.arrivedAtZone = person.hasDestination = false;
                person.destinationLotId = lot.id;
                var actor = pedestrians.FirstOrDefault(a=>a.Immigrant==person);
                if (actor != null)
                {
                    actor.Route = Array.Empty<Vector2>(); SetPedestrianAnimation(actor,false);
                    // Recovery for old interior-zone destinations: do not let a newly
                    // constructed footprint trap a waiting resident inside its walls.
                    var at=new Vector2(actor.Body.position.x,actor.Body.position.z);
                    if(!actor.EagleCarried && !FreightClear(at,at,.15f) && TryImmigrationStreetPoint(at,out var street))
                    {actor.Body.position=PedestrianGround(street);person.position=street;}
                }
                immigrantPlanRetry.Remove(person.id);immigrantZoneWaitStarted.Remove(person.id);
            }
            nextEventTick = 0;
            UpdateImmigrantHousingCounts();
        }

        private int immigrantStreetRevision=-1;
        private readonly Dictionary<Vector2Int,List<(Vector2 A,Vector2 B,float Radius)>> immigrantStreetCells=new();
        private bool OnImmigrationStreet(Vector2 point)
        {
            const float bucket=32;
            if(immigrantStreetRevision!=FreightRoadRevision)
            {
                immigrantStreetCells.Clear();
                foreach(var road in districtRoadMeshes.Keys)
                {
                    var points=RoadCenterline(road).ToArray();float radius=road.width*.5f+.15f;
                    for(int i=0;i<points.Length-(road.closed?0:1);i++)
                    {
                        var a=points[i];var b=points[(i+1)%points.Length];
                        var min=Vector2Int.FloorToInt((Vector2.Min(a,b)-Vector2.one*radius)/bucket);
                        var max=Vector2Int.FloorToInt((Vector2.Max(a,b)+Vector2.one*radius)/bucket);
                        for(int z=min.y;z<=max.y;z++)for(int x=min.x;x<=max.x;x++)
                        {
                            var key=new Vector2Int(x,z);
                            if(!immigrantStreetCells.TryGetValue(key,out var list))immigrantStreetCells[key]=list=new();
                            list.Add((a,b,radius));
                        }
                    }
                }
                immigrantStreetRevision=FreightRoadRevision;
            }
            if(!immigrantStreetCells.TryGetValue(Vector2Int.FloorToInt(point/bucket),out var spans))return false;
            foreach(var span in spans)
            {
                var edge=span.B-span.A;
                var near=span.A+edge*(edge.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-span.A,edge)/edge.sqrMagnitude):0);
                if((near-point).sqrMagnitude<=span.Radius*span.Radius)return true;
            }
            return false;
        }

        private bool TryImmigrationStreetPoint(Vector2 point,out Vector2 result)
        {
            result=default;float best=48*48;bool found=false;
            foreach(var road in districtRoadMeshes.Keys)
            {
                var points=RoadCenterline(road).ToArray();
                for(int i=0;i<points.Length-(road.closed?0:1);i++)
                {
                    var a=points[i];var edge=points[(i+1)%points.Length]-a;
                    var near=a+edge*(edge.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,edge)/edge.sqrMagnitude):0);
                    float distance=(near-point).sqrMagnitude;
                    if(distance>=best||!FreightClear(near,near,.15f))continue;
                    best=distance;found=true;result=near;
                }
            }
            return found;
        }

        private void GrowPendingPlayerLots()
        {
            if (!CanBuildTown || IsSaving) return;
            (LotDefinition Layout,LotObjectDefinition[] Buildings)[] options=null;
            foreach (var lot in PendingPlayerLots.Where(l=>InActiveDistrict(l.center)).ToArray())
            {
                var zone=lot.definition.zoningLayout.zone;
                bool arrived=WaitingResidents.Any(p=>p.destinationLotId==lot.id);
                bool demand=IsResidentialZone(zone) ? PlayerPopulationPresent && lot.definition.zoningLayout.wealth==LotWealth.Low && arrived :
                    PlayerGrowthAllowed(default,zone,lot.definition.zoningLayout.wealth);
                if (!DeveloperMode && !demand) continue;
                if(PlacedLots.Any(l=>new DistrictLotFootprint(l).Overlaps(new DistrictLotFootprint(lot))))continue;
                if(LotSiteProblem(lot,DistrictLotWater(),growing:true)!=null)continue;
                options ??= FoundersGrowthLayouts(out _);
                var option=options.FirstOrDefault(o=>o.Layout.id==lot.definition.id);
                if (option.Layout==null || option.Buildings.Length==0) continue;
                var building=option.Buildings[UnityEngine.Random.Range(0,option.Buildings.Length)];
                lot.definition=LotGrowableBuildings.Materialize(option.Layout,building,lot.id);
                lot.growthBuildingId=lot.definition.objects.Last().id;
                if(IsResidentialZone(zone))AddResidentialEntryPath(lot,building);
                LotPropStyles.Apply(lot.definition,lot.id,LotAssetCatalog.Load());
                PrepareLotGrading(lot); HoldLotPopulation(lot);
                PendingPlayerLots.Remove(lot); PlacedLots.Add(lot);
                BindResidentsToBuilding(lot);
                ClearDevelopedSites(new[]{lot}); PresentDistrictLots(new[]{lot});
                AddLotStreetlamps(lot); StartCoroutine(ConstructZonedLot(lot));
                RefreshZoneOverlay();
            }
        }

        private void AppendPendingPlayerZones(Mesh mesh)
        {
            if (PendingPlayerLots.Count==0) return;
            var vertices=mesh.vertices.ToList();
            var triangles=Enumerable.Range(0,mesh.subMeshCount).Select(i=>mesh.GetTriangles(i).ToList()).ToArray();
            foreach(var lot in PendingPlayerLots.Where(l=>InActiveDistrict(l.center)))
            {
                var footprint=new DistrictLotFootprint(lot);var half=lot.definition.Size*.5f;
                int first=vertices.Count;
                foreach(var local in new[]{new Vector2(-half.x,-half.y),new Vector2(-half.x,half.y),new Vector2(half.x,half.y),new Vector2(half.x,-half.y)})
                {var p=footprint.WorldPoint(local);vertices.Add(new Vector3(p.x,LotHeight(p)+.15f,p.y));}
                triangles[(int)lot.definition.zoningLayout.zone].AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
            mesh.SetVertices(vertices);
            for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i,false);
            mesh.RecalculateBounds();
        }
    }
}
