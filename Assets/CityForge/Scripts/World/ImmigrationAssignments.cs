using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private bool immigrantAssignmentsDirty=true;
        private readonly Dictionary<string,ImmigrantDestination> immigrantAssignedSites=new();
        internal int ImmigrantAssignmentScans {get;private set;}
        public long UnhousedPopulation {get;private set;}
        public int AvailableResidentialParcels {get;private set;}
        public int ReservedResidentialParcels {get;private set;}
        private static IEnumerable<Vector2Int> ImmigrationCells(RectInt bounds)
        {foreach(var cell in bounds.allPositionsWithin)yield return cell;}

        private void MarkImmigrationSitesChanged()
        {
            immigrantAssignmentsDirty=true;
            nextEventTick=0;
        }

        private void UpdateImmigrantHousingCounts()
        {
            UnhousedPopulation=RegionEvents.Sum(s=>s.immigrants.Where(p=>!p.settled || p.awaitingHousing).Sum(p=>p.Population));
            var reserved=RegionEvents.SelectMany(s=>s.immigrants).Where(p=>!p.settled && !string.IsNullOrEmpty(p.destinationLotId))
                .Select(p=>p.destinationLotId).ToHashSet();
            var occupied=RegionEvents.SelectMany(s=>s.moveIns).Select(m=>m.lotId)
                .Concat(RegionEvents.SelectMany(s=>s.immigrants).Where(p=>p.settled&&!p.awaitingHousing).Select(p=>p.destinationLotId)).ToHashSet();
            ReservedResidentialParcels=immigrantAssignedSites.Count(pair=>pair.Value.Residential&&reserved.Contains(pair.Key));
            AvailableResidentialParcels=immigrantAssignedSites.Count(pair=>pair.Value.Residential&&!reserved.Contains(pair.Key)&&!occupied.Contains(pair.Key));
            RefreshImmigrationControl();
        }

        private void RefreshImmigrantAssignments()
        {
            if(!immigrantAssignmentsDirty || SelectedDistrict<0 || !ApprovedDistrict)return;
            immigrantAssignmentsDirty=false;
            SyncRegionEvents();
            CaptureImmigrantPositions();
            // Before the first arrival there is no housing ledger to maintain.
            // Thereafter actual edits also update vacancy counts, even if everyone
            // is housed. An unchanged town never enters this path.
            if(!RegionEvents.Any(s=>s.occurrences>0 || s.immigrants.Count>0 || s.moveIns.Count>0))return;
            ImmigrantAssignmentScans++;
            var definition=RegionEventCatalog.Definitions.First(d=>d.trigger=="galleon-docked");
            var sites=ImmigrantDestinations(definition).GroupBy(t=>t.Key).ToDictionary(g=>g.Key,g=>g.First());
            immigrantAssignedSites.Clear();foreach(var pair in sites)immigrantAssignedSites.Add(pair.Key,pair.Value);
            var occupied=RegionEvents.SelectMany(s=>s.moveIns).Select(m=>m.lotId)
                .Concat(RegionEvents.SelectMany(s=>s.immigrants).Where(p=>p.settled&&!p.awaitingHousing).Select(p=>p.destinationLotId)).ToHashSet();
            var people=RegionEvents.SelectMany(s=>s.immigrants).Where(p=>ActiveDistrictMembers.Contains(p.district)&&(!p.settled||p.awaitingHousing&&!pedestrians.Any(a=>a.Immigrant==p))).ToArray();
            var reserved=new HashSet<string>();
            // Keep existing residential reservations, including routes still being
            // planned. Walking does not discover or choose another destination.
            foreach(var p in people)
            {
                if(!string.IsNullOrEmpty(p.destinationLotId) && sites.TryGetValue(p.destinationLotId,out var site) &&
                    site.Residential && !occupied.Contains(site.Key) && reserved.Add(site.Key))
                {ApplyImmigrantAssignment(p,site);continue;}
                if(!p.awaitingHousing || !p.settled)ClearImmigrantAssignment(p);
            }
            var free=sites.Values.Where(t=>t.Residential&&!occupied.Contains(t.Key)&&!reserved.Contains(t.Key)).ToList();
            var commercial=sites.Values.Where(t=>!t.Residential).ToArray();
            foreach(var p in people.Where(p=>string.IsNullOrEmpty(p.destinationLotId)||
                !sites.TryGetValue(p.destinationLotId,out var at)||!at.Residential))
            {
                if(free.Count>0)
                {
                    var home=free.OrderBy(t=>(t.Point-p.position).sqrMagnitude).ThenBy(t=>ImmigrantChoiceHash(p.id+t.Key)).First();
                    free.Remove(home);reserved.Add(home.Key);ApplyImmigrantAssignment(p,home);
                }
                else if(commercial.Length>0)
                {
                    var refuge=commercial.OrderBy(t=>(t.Point-p.position).sqrMagnitude).ThenBy(t=>ImmigrantChoiceHash(p.id+t.Key)).First();
                    // An immigrant already inside this temporary refuge stays there.
                    if(p.destinationLotId!=refuge.Key || !p.settled)ApplyImmigrantAssignment(p,refuge);
                    p.awaitingHousing=true;
                }
                else
                {
                    ClearImmigrantAssignment(p);p.awaitingHousing=true;p.settled=false;
                    if(p.hasArrivalPoint && (p.position-p.arrivalPoint).sqrMagnitude>64)
                    {
                        p.position=p.arrivalPoint;
                        var actor=pedestrians.FirstOrDefault(a=>a.Immigrant==p);
                        if(actor!=null&&actor.Body)actor.Body.position=PedestrianGround(p.arrivalPoint);
                    }
                }
            }
            UpdateImmigrantHousingCounts();
        }

        private void ReleaseImmigrantHomes(IEnumerable<DistrictLotRecord> removed)
        {
            var lost=removed.ToDictionary(l=>l.id,l=>l);
            foreach(var state in RegionEvents)
            foreach(var moved in (state.moveIns??new()).Where(m=>lost.ContainsKey(m.lotId)).ToArray())
            {
                var rule=RegionEventCatalog.Definitions.FirstOrDefault(d=>d.id==state.eventId);
                if(rule==null)continue;
                var formerHome=lost[moved.lotId];
                var port=PlacedLots.FirstOrDefault(l=>l.id==state.sourceLotId);
                var point=formerHome.center;
                if(port!=null && TryImmigrantShore(port,out var shore))point=shore;
                var waiting=state.immigrants.Count>=96?state.immigrants.FirstOrDefault(p=>!p.settled||p.awaitingHousing):null;
                if(waiting!=null)waiting.representedPopulation=waiting.Population+moved.population;
                else state.immigrants.Add(new EventImmigrant{id=Guid.NewGuid().ToString("N"),assetId=rule.actorAssetIds[0],
                    district=formerHome.district,representedPopulation=moved.population,position=point,arrivalPoint=point,
                    hasArrivalPoint=true,awaitingHousing=true});
                state.settledPopulation=Math.Max(0,state.settledPopulation-moved.population);
                state.moveIns.Remove(moved);
            }
            MarkImmigrationSitesChanged();
        }

        private void ClearImmigrantAssignment(EventImmigrant p)
        {
            p.hasDestination=p.targetIsZone=p.arrivedAtZone=false;p.destinationLotId=null;
            var actor=pedestrians.FirstOrDefault(a=>a.Immigrant==p);
            if(actor!=null){actor.Route=Array.Empty<Vector2>();SetPedestrianAnimation(actor,false);}
            immigrantPlanRetry.Remove(p.id);immigrantRouteChecks.Remove(p.id);immigrantRoadSearches.Remove(p.id);
        }

        private void ApplyImmigrantAssignment(EventImmigrant p,ImmigrantDestination target)
        {
            bool same=p.destinationLotId==target.Key && (p.destination-target.Point).sqrMagnitude<.01f;
            if(!same)
            {
                ClearImmigrantAssignment(p);p.settled=false;
                p.destinationLotId=target.Key;p.destination=target.Point;
                p.targetIsZone=target.Zone;p.destinationZoneCell=target.Cell;
            }
            p.awaitingHousing=!target.Residential || !p.settled;
        }
    }
}
