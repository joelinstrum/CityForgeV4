using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CityForge.World;
using UnityEngine;
namespace CityForge.Lots
{
    internal static class BuildingSigns
    {
        internal const string None="none";
        internal static BuildingSignAsset[] All()=>Resources.LoadAll<BuildingSignAsset>("Signs").Where(s=>!string.IsNullOrEmpty(s.id)).GroupBy(s=>s.id).Where(g=>g.Count()==1).Select(g=>g.First()).OrderBy(s=>s.label).ToArray();
        internal static string Choice(BuildingAppearanceSettings state,string mountId)=>state.signs?.FirstOrDefault(s=>s.mountId==mountId).signId??"";
        internal static BuildingAppearanceSettings Choose(BuildingAppearanceSettings state,string mountId,string id)
        {
            var list=(state.signs??Array.Empty<BuildingSignSelection>()).Where(s=>s.mountId!=mountId).ToList();
            if(!string.IsNullOrEmpty(id))list.Add(new BuildingSignSelection{mountId=mountId,signId=id});
            state.signs=list.OrderBy(s=>s.mountId,StringComparer.Ordinal).ToArray();return state;
        }
        internal static void ValidateChange(LotAssetCatalogEntry asset,BuildingAppearanceSettings before,BuildingAppearanceSettings after)
        {
            if(BuildingAppearance.SameSigns(before,after))return;
            foreach(var choice in after.signs??Array.Empty<BuildingSignSelection>())
            {
                if(Choice(before,choice.mountId)==choice.signId)continue;
                if(!BuildingSignCompatibility.TryProfile(asset,out var profile,out var reason))throw new InvalidDataException(reason);
                var mount=profile.mounts.FirstOrDefault(m=>m.id==choice.mountId);
                if(mount==null)throw new InvalidDataException("This sign attachment point is no longer available.");
                if(choice.signId==None)continue;
                var sign=All().FirstOrDefault(s=>s.id==choice.signId);var eligibility=BuildingSignCompatibility.Evaluate(mount,sign);
                if(!eligibility.Eligible)throw new InvalidDataException(eligibility.Reason);
            }
        }
    }
    internal sealed class BuildingSignInstances : MonoBehaviour
    {
        sealed class Slot
        {
            internal BuildingSignMountProfile Profile;internal BuildingSignMount Mount;internal string Choice;
            internal (Renderer R,bool Forced)[] Original=Array.Empty<(Renderer,bool)>();
            internal AuthoredSignLighting Light;internal bool LightEnabled;internal GameObject Replacement;
        }
        readonly List<Slot> slots=new();
        internal static void Attach(GameObject root,LotAssetCatalogEntry asset,BuildingAppearanceSettings state)
        {
            if(!BuildingSignCompatibility.TryProfile(asset,out _,out _))return;
            var control=root.AddComponent<BuildingSignInstances>();
            foreach(var profile in root.GetComponentsInChildren<BuildingSignMountProfile>(true))foreach(var mount in profile.mounts)
            {
                var slot=new Slot{Profile=profile,Mount=mount,Choice=""};
                var original=string.IsNullOrEmpty(mount.originalSignRootPath)?null:profile.transform.Find(mount.originalSignRootPath);
                if(original)
                {
                    var retained=mount.retainedHardwarePaths.Select(p=>original.Find(p)).Where(t=>t).ToArray();
                    slot.Original=original.GetComponentsInChildren<Renderer>(true).Where(r=>!retained.Any(t=>r.transform==t||r.transform.IsChildOf(t))).Select(r=>(r,r.forceRenderingOff)).ToArray();
                    slot.Light=original.GetComponent<AuthoredSignLighting>();slot.LightEnabled=slot.Light&&slot.Light.enabled;
                }
                control.slots.Add(slot);
            }
            control.Set(state);
        }
        internal void Set(BuildingAppearanceSettings state)
        {
            foreach(var slot in slots)
            {
                string choice=BuildingSigns.Choice(state,slot.Mount.id);
                if(choice==slot.Choice)continue;
                var sign=BuildingSigns.All().FirstOrDefault(s=>s.id==choice);
                if(choice!=""&&choice!=BuildingSigns.None&&!BuildingSignCompatibility.Evaluate(slot.Mount,sign).Eligible)choice="";
                if(slot.Replacement){slot.Replacement.SetActive(false);Destroy(slot.Replacement);slot.Replacement=null;}
                bool original=choice=="";
                foreach(var r in slot.Original)if(r.R)r.R.forceRenderingOff=original?r.Forced:true;
                if(slot.Light)slot.Light.enabled=original&&slot.LightEnabled;
                if(sign&&choice!=""&&choice!=BuildingSigns.None)
                {
                    slot.Replacement=Instantiate(slot.Profile.mediumTier?sign.mediumPrefab:sign.nearPrefab,slot.Profile.transform,false);
                    slot.Replacement.name="Selected sign · "+slot.Mount.id;
                    var t=slot.Replacement.transform;t.localPosition=slot.Mount.position;t.localRotation=Quaternion.Euler(slot.Mount.eulerAngles);
                    foreach(var child in t.GetComponentsInChildren<Transform>(true))child.gameObject.layer=slot.Profile.gameObject.layer;
                    foreach(var lighting in t.GetComponentsInChildren<AuthoredSignLighting>(true))lighting.BindClock(transform);
                }
                slot.Choice=choice;
            }
            GetComponent<DistrictBuildingDetail>()?.RefreshSignRenderers();
        }
    }
}
