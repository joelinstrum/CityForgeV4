using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable] public struct BuildingSignSelection { public string mountId, signId; }
    [Serializable] public struct BuildingAppearanceSettings
    {
        // Missing/empty retains the approved original. IDs are stable; no Unity objects in saves.
        public string trimColor;
        public string awningColor;
        public string wallColor;
        public bool wallSaturationSet;
        public float wallSaturation;
        public BuildingSignSelection[] signs;
    }
    internal static class BuildingAppearance
    {
        internal readonly struct PaintColor
        {
            internal readonly string Id, Label, Hex;
            internal PaintColor(string id, string label, string hex) { Id=id;Label=label;Hex=hex; }
            internal Color Color { get { ColorUtility.TryParseHtmlString(Hex,out var value);return value; } }
        }
        internal static readonly PaintColor[] Colors = {
            new("cream","Cream","#E9DCC0"), new("sand","Sand","#B39A78"),
            new("red","Brick red","#943B32"), new("green","Sage","#718363"),
            new("blue","Slate blue","#54749A"), new("charcoal","Charcoal","#343E46")
        };
        internal static float WallSaturation(BuildingAppearanceSettings state) => state.wallSaturationSet ? state.wallSaturation : .5f;
        internal static bool Original(BuildingAppearanceSettings state) => string.IsNullOrEmpty(state.trimColor);
        internal static BuildingAppearanceSettings Copy(BuildingAppearanceSettings s){if(s.signs!=null)s.signs=(BuildingSignSelection[])s.signs.Clone();return s;}
        internal static bool SameSigns(BuildingAppearanceSettings a,BuildingAppearanceSettings b)=>
            (a.signs??Array.Empty<BuildingSignSelection>()).OrderBy(s=>s.mountId,StringComparer.Ordinal).Select(s=>s.mountId+":"+s.signId).SequenceEqual(
            (b.signs??Array.Empty<BuildingSignSelection>()).OrderBy(s=>s.mountId,StringComparer.Ordinal).Select(s=>s.mountId+":"+s.signId));
        internal static bool Same(BuildingAppearanceSettings a, BuildingAppearanceSettings b) => (a.trimColor??"") == (b.trimColor??"") && (a.awningColor??"") == (b.awningColor??"") && (a.wallColor??"") == (b.wallColor??"") && (string.IsNullOrEmpty(a.wallColor) || WallSaturation(a)==WallSaturation(b)) && SameSigns(a,b);
        // Pure validation is also called from background save/load threads.
        internal static void Validate(BuildingAppearanceSettings state)
        {
            if (!Original(state) && !Colors.Any(c=>c.Id==state.trimColor)) throw new InvalidDataException("Invalid building trim color.");
            if (!string.IsNullOrEmpty(state.awningColor) && !Colors.Any(c=>c.Id==state.awningColor)) throw new InvalidDataException("Invalid building awning tint.");
            if (!string.IsNullOrEmpty(state.wallColor) && !Colors.Any(c=>c.Id==state.wallColor)) throw new InvalidDataException("Invalid building wall tint.");
            if(state.wallSaturationSet && (!float.IsFinite(state.wallSaturation)||state.wallSaturation<0||state.wallSaturation>1)) throw new InvalidDataException("Invalid wall tint saturation.");
            bool Id(string s)=>!string.IsNullOrEmpty(s)&&s.Length<=96&&s.All(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_');
            if(state.signs!=null&&(state.signs.Length>16||state.signs.Any(s=>!Id(s.mountId)||!Id(s.signId))||state.signs.Select(s=>s.mountId).Distinct().Count()!=state.signs.Length))
                throw new InvalidDataException("Invalid building sign selection.");
        }
        internal static PaintColor Find(string id) => Colors.First(c=>c.Id==id);
        // Explicit opt-in: body atlases, glazing and sign materials must never be guessed as paint regions.
        internal const string Marquee = "cf-building-marquee-tower-001";
        internal static bool TrimMaterial(Material material) => material && (material.name=="Near-Trim" || material.name=="Medium-Trim") &&
            material.HasProperty("_BaseColor") && material.HasProperty("_BaseMap");
        internal static bool SupportsTrim(LotAssetCatalogEntry asset) => SupportsRegion(asset,"trim") || asset?.id==Marquee && asset.prefab &&
            HasTrim(asset.prefab) && (!asset.distantPrefab || HasTrim(asset.distantPrefab));
        internal static bool SupportsAwnings(LotAssetCatalogEntry asset) => SupportsRegion(asset,"awnings");
        internal static bool SupportsWalls(LotAssetCatalogEntry asset) => SupportsRegion(asset,"walls");
        static bool SupportsRegion(LotAssetCatalogEntry asset,string region) => asset?.category=="Buildings" &&
            asset.prefab && asset.distantPrefab && HasRegion(asset.prefab,region) && HasRegion(asset.distantPrefab,region);
        static bool HasRegion(GameObject prefab,string region)
        {
            var profiles=prefab.GetComponentsInChildren<BuildingTintProfile>(true);
            if(profiles.Length!=1 || profiles[0].bindings==null)return false;
            var bindings=profiles[0].bindings.Where(b=>b!=null&&b.region==region).ToArray();
            return bindings.Length>0 && bindings.All(b=>b.material&&b.neutralAlbedo&&b.material.HasProperty("_BaseMap")&&b.material.HasProperty("_BaseColor")&&
                prefab.GetComponentsInChildren<Renderer>(true).Any(r=>!r.GetComponentInParent<CityForge.World.AuthoredSignLighting>()&&r.sharedMaterials.Contains(b.material)));
        }
        static bool HasTrim(GameObject prefab) => prefab.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(TrimMaterial));
        internal static bool Apply(LotDefinition lot, IReadOnlyDictionary<string,BuildingAppearanceSettings> changes, LotAssetCatalog catalog)
        {
            // Validate every target first so a failed selection cannot partially edit a lot.
            foreach(var pair in changes)
            {
                Validate(pair.Value);
                var item=lot.objects.Find(o=>o.id==pair.Key);
                if(item==null)throw new InvalidDataException("The selected building no longer exists.");
                var asset=catalog.Find(item.assetId);
                if((item.buildingAppearance.trimColor??"")!=(pair.Value.trimColor??"")&&!SupportsTrim(asset))throw new InvalidDataException("This building does not have paintable trim.");
                if((item.buildingAppearance.awningColor??"")!=(pair.Value.awningColor??"")&&!SupportsAwnings(asset))throw new InvalidDataException("This building does not have tintable awnings.");
                if(((item.buildingAppearance.wallColor??"")!=(pair.Value.wallColor??"") || !string.IsNullOrEmpty(pair.Value.wallColor)&&WallSaturation(item.buildingAppearance)!=WallSaturation(pair.Value))&&!SupportsWalls(asset))throw new InvalidDataException("This building does not have tintable walls.");
                BuildingSigns.ValidateChange(asset,item.buildingAppearance,pair.Value);
            }
            bool changed=false;
            foreach(var pair in changes)
            {
                var item=lot.objects.Find(o=>o.id==pair.Key);
                changed |= !Same(item.buildingAppearance,pair.Value);item.buildingAppearance=Copy(pair.Value);
            }
            return changed;
        }
    }
}
