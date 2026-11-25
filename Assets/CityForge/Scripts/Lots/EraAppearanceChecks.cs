#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CityForge.UI;
using CityForge.World;
using UnityEngine;
namespace CityForge.Lots
{
    public static class EraAppearanceChecks
    {
        static readonly string Output = System.Environment.GetEnvironmentVariable("CITYFORGE_ERA_CHECK_OUTPUT") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/EraAppearanceChecks"));
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); Debug.Log("ERA PASS: " + message); }
        internal static LotDefinition Fixture()
        {
            var lot = new LotDefinition { name = "Era appearance check" };
            lot.paths.Add(new LotPathDefinition { materialId = "cobblestone", width = 2, points = new[] { new Vector2(-2,0), new Vector2(2,0) } });
            return lot;
        }
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            var lot = Fixture(); var path = lot.paths[0];
            var horse = new LotAssetCatalogEntry { id="v3-vehicle-hitched-horse", category=LotAssetCatalog.Transport, label="Hitched horse" };
            Require(EraAppearance.Allows(lot, horse, "founders"), "legacy Founders horse retained");
            Require(!EraAppearance.Allows(lot, horse, "modern"), "modern district hides hitched horse");
            Require(EraAppearance.Allows(lot, new LotAssetCatalogEntry { id=horse.id, category="Buildings" }, "modern"), "buildings never suppressed");
            Require(LotPathMaterial.Find("concrete").IsAvailable, "native concrete texture available");
            string geometry = JsonUtility.ToJson(path);
            var resolved = EraAppearance.Resolve(lot,"modern");
            Require(resolved.paths[0].materialId=="concrete" && JsonUtility.ToJson(path)==geometry && resolved.paths[0].points.SequenceEqual(path.points), "era paving changes detached material only");
            path.materialLocked=true; Require(EraAppearance.MaterialId(lot,path,"modern")=="cobblestone", "path lock wins over profile"); path.materialLocked=false;
            lot.eraAppearance=new EraAppearanceSettings { eraId="modern", keptProps=new[]{horse.id}, pavingConfigured=true, entranceMaterial="concrete" };
            Require(EraAppearance.Allows(lot,horse,"founders"), "explicit Keep overrides vehicle era");
            path.eraRole=EraPathRole.Entrance; Require(EraAppearance.MaterialId(lot,path,"founders")=="concrete", "purpose paving uses local profile");
            lot.eraAppearance.hiddenProps=new[]{horse.id}; lot.eraAppearance.keptProps=Array.Empty<string>();
            Require(!EraAppearance.Allows(lot,horse,"founders"), "explicit Hide applies");
            lot.eraAppearance.preserveAuthored=true; Require(EraAppearance.MaterialId(lot,path,"modern")=="cobblestone", "heritage preserves paving");
            var original=Fixture(); var working=LotDefinition.Parse(JsonUtility.ToJson(original)); working.eraAppearance=lot.eraAppearance;
            var customization=LotCustomizationService.CreateOverride(original,working,null);
            var restored=LotCustomizationService.Apply(original,JsonUtility.FromJson<LotCustomization>(JsonUtility.ToJson(customization)),null);
            Require(restored.eraAppearance.eraId=="modern" && restored.eraAppearance.hiddenProps.Contains(horse.id), "player lot appearance override roundtrips");
            Require(LotCustomizationService.Apply(original,new LotCustomization(),null).eraAppearance?.eraId != "modern", "old customization defaults preserve appearance");
            bool rejected=false; try { EraAppearance.Validate(new EraAppearanceSettings { hiddenProps=new[]{horse.id}, keptProps=new[]{horse.id} }); } catch(InvalidDataException) { rejected=true; }
            Require(rejected,"contradictory prop rules rejected");
            rejected=false; try { var invalid=Fixture();invalid.paths[0].eraRole=(EraPathRole)99;LotDefinition.Parse(JsonUtility.ToJson(invalid)); }catch(InvalidDataException){rejected=true;} Require(rejected,"invalid path purpose rejected");
            var root=new GameObject("Selective appearance test"); var view=root.AddComponent<EraLotAppearanceView>();
            var building=new GameObject("Building");building.transform.SetParent(root.transform);var actor=new GameObject("Horse");actor.transform.SetParent(root.transform);
            view.Register(building,new LotAssetCatalogEntry{category="Buildings"});view.Register(actor,horse);
            int buildingId=building.GetInstanceID();var job=new LotVehicleJobState();string cargo=JsonUtility.ToJson(job);
            view.Apply(original,"modern",true);Require(!actor.activeSelf&&building.activeSelf&&building.GetInstanceID()==buildingId&&JsonUtility.ToJson(job)==cargo,"in-place switch hides actor and retains building and job data");
            view.Apply(original,"founders",true);Require(actor.activeSelf,"switching back restores same actor");UnityEngine.Object.DestroyImmediate(root);
            var immatureRoot = new GameObject("Population delay test"); var immatureView = immatureRoot.AddComponent<EraLotAppearanceView>();
            var delayed = new GameObject("Delayed mounted character"); delayed.transform.SetParent(immatureRoot.transform); delayed.SetActive(false);
            immatureView.Register(delayed, new LotAssetCatalogEntry { id = "v3-vehicle-hitched-horse", category = "Characters" });
            immatureView.Apply(original, "founders", false); Require(!delayed.activeSelf, "profile cannot reveal an unpopulated character");
            immatureView.Apply(original, "founders", true); Require(delayed.activeSelf, "populated character can be restored"); UnityEngine.Object.DestroyImmediate(immatureRoot);
            var ordered = Fixture(); ordered.objects.Add(new LotObjectDefinition { assetId = "a" }); ordered.objects.Add(new LotObjectDefinition { assetId = "b" });
            string orderedBefore = JsonUtility.ToJson(ordered);
            var reordered = LotDefinition.Parse(orderedBefore); reordered.objects.Reverse(); reordered.eraAppearance = new EraAppearanceSettings { eraId = "modern" };
            Require(EraAppearance.OnlyAppearanceChanged(ordered, reordered), "cosmetic draw order does not trigger a building rebuild");
            Require(JsonUtility.ToJson(ordered) == orderedBefore, "appearance comparison leaves authored definition untouched");
            reordered.objects[0].position = Vector2.one;
            Require(!EraAppearance.OnlyAppearanceChanged(ordered, reordered), "actual cosmetic movement still requests edited-lot presentation");
            var source=SavedRegionStore.ListRegions().First(); var snapshot=SavedRegionStore.Load(source.path);
            var oldBytes=File.ReadAllBytes(source.path);
            snapshot.info.districts ??= new[]{new RegionDistrictInfo{index=0}};
            if(snapshot.info.districts.Length==0)snapshot.info.districts=new[]{new RegionDistrictInfo{index=0}};
            snapshot.info.districts[0].appearanceEraId="modern";
            var record=new DistrictLotRecord{id=Guid.NewGuid().ToString("N"),definition=original,customization=customization};
            snapshot.info.lots=(snapshot.info.lots??Array.Empty<DistrictLotRecord>()).Concat(new[]{record}).ToArray();
            string roundtrip=Path.Combine(Output,"Era-Roundtrip.cfregion");SavedRegionStore.Write(snapshot,roundtrip);var reload=SavedRegionStore.Load(roundtrip);
            Require(reload.info.districts[0].appearanceEraId=="modern" && reload.info.lots.Last().customization.appearance.eraId=="modern", "real region file preserves district and lot profiles");
            Require(File.ReadAllBytes(source.path).SequenceEqual(oldBytes),"original region file unchanged");
            File.WriteAllText(Path.Combine(Output,"Checks.txt"),"PASS policy, heritage/prop exceptions, purpose paving, locks, protected buildings, additive customization, invalid data, in-place visibility, real cfregion save/reload; original save unchanged.");
        }
    }
}
#endif
