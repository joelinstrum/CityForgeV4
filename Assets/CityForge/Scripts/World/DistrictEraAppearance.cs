using System;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal string DistrictAppearanceEra(int district)
        {
            var id = Districts?.FirstOrDefault(d => d.index == district)?.appearanceEraId;
            return string.IsNullOrEmpty(id) || CityForgeTimeline.Find(id) == null ? EraAppearance.EraForYear(TownYear(district)) : id;
        }
        private LotDefinition EffectiveAppearanceLot(DistrictLotRecord record)
        {
            var catalog = LotAssetCatalog.Load();
            try { return record.customization != null ? LotCustomizationService.Apply(record.definition, record.customization, catalog) : record.definition; }
            catch (System.IO.InvalidDataException error) { Debug.LogWarning(error.Message); return record.definition; }
        }
        private void RefreshEraAppearance()
        {
            foreach (var pair in districtLotRoots)
            {
                if (!pair.Value) continue;
                var lot = EffectiveAppearanceLot(pair.Key);
                pair.Value.GetComponent<EraLotAppearanceView>()?.Apply(lot, DistrictAppearanceEra(pair.Key.district), LotPopulationReady(pair.Key));
            }
            foreach (var pair in streetscapeViews)
                if (pair.Value) pair.Value.SetActive(EraAppearance.Allows(new LotDefinition(), LotAssetCatalog.Load()?.Find(pair.Key.streetscape?.assetId), DistrictAppearanceEra(pair.Key.district)));
            // Explicit profile changes invalidate traffic only, never lot geometry or freight jobs.
            for (int i = pedestrians.Count - 1; i >= 0; i--)
                if (pedestrians[i].Ambient)
                {
                    if (pedestrians[i].Body) { pedestrians[i].Body.gameObject.SetActive(false); Destroy(pedestrians[i].Body.gameObject); }
                    pedestrians.RemoveAt(i);
                }
            foreach (var actor in pedestrians.Where(actor => !actor.Ambient))
            {
                actor.AppearanceAllowed = EraAppearance.Allows(new LotDefinition(), LotAssetCatalog.Load()?.Find(actor.AssetId), DistrictAppearanceEra(actor.AppearanceDistrict));
                SetTrafficVisibility(actor);
            }
            populatedTrafficLots.Clear(); ambientTrafficCount = 0;
            RebuildAmbientTraffic(LotAssetCatalog.Load());
            QueueDistrictSidewalkRefresh();
        }
        private void BuildGovernmentEraContent()
        {
            GovernmentText(governmentPage,"Choose the district's appearance independently of its simulation year.",new Vector2(0,233),new Vector2(1120,60),21);
            if (SelectedDistrict < 0)
            { GovernmentText(governmentPage,"Open a district to manage its appearance.",Vector2.zero,new Vector2(1100,60),24); return; }
            string current = Districts.FirstOrDefault(d=>d.index==SelectedDistrict)?.appearanceEraId ?? "";
            var ids = new[]{""}.Concat(CityForgeTimeline.Eras.Select(e=>e.id)).ToArray();
            string label = string.IsNullOrEmpty(current) ? "Automatic · district year" : CityForgeTimeline.Find(current)?.Name;
            GovernmentButton(governmentPage,"Appearance era: "+label+"  ▸",new Vector2(0,135),new Vector2(1050,64),()=>
            {
                string next=ids[(Array.IndexOf(ids,current)+1)%ids.Length];
                foreach(var district in Districts.Where(d=>ActiveDistrictMembers.Contains(d.index)))district.appearanceEraId=next;
                RefreshEraAppearance(); ShowGovernmentPage("Era Content");
            },true);
            GovernmentText(governmentPage,"Art Deco and later use concrete lot paving and compatible vehicles. The Lot Editor can override the era, lock individual path materials, and keep or hide specific props.",new Vector2(0,20),new Vector2(1100,110),23);
            GovernmentText(governmentPage,"Buildings, roads, terrain and saved freight cargo stay in place. Save the region to keep this profile. Appearance does not advance the calendar or unlock buildings.",new Vector2(0,-110),new Vector2(1100,100),21);
            GovernmentText(governmentPage,"Content gap: motor freight replacements and era sound banks still need assets. Older freight actors are hidden and their jobs remain saved.",new Vector2(0,-225),new Vector2(1100,90),20);
        }
    }
}
