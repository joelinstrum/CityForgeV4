using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CityForge.Lots;
using CityForge.UI;
using CityForge.World;

namespace CityForge.EditorTools
{
    internal static class RebuiltOfficeCatalog
    {
        internal const string Root = "Assets/CityForge/Resources/Lots/RebuiltBuildings/";
        // Legacy registrations survive even before a prepared-package manifest is supplied.
        internal static readonly (string folder, string id, string label, float width, float depth, float height)[] Buildings =
        {
            ("ContinentalInsurance", "cf-building-continental-insurance-001", "Continental Insurance", 28, 20, 33.8f),
            ("ArtDecoOffice001", "cf-building-artdeco-office-001", "Art Deco Office 001", 32, 20, 22.38f),
            ("MarqueeTower", "cf-building-marquee-tower-001", "Marquee Tower", 20, 20, 28.37f)
        };
        [Serializable] internal sealed class PreparedBuilding
        {
            public string folder, id, label, eraId, group, subcategory, zone, wealth;
            public string[] additionalEraIds;
            public float width, depth, height;
        }
        internal static IEnumerable<PreparedBuilding> Definitions()
        {
            var definitions = Buildings.ToDictionary(b=>b.id,b=>new PreparedBuilding {
                folder=b.folder,id=b.id,label="Art Deco · "+b.label,eraId="modern",group="commercial",subcategory="office",
                zone="Commercial2",wealth="Medium",additionalEraIds=new[]{"postwar","contemporary"},width=b.width,depth=b.depth,height=b.height
            });
            if (Directory.Exists(Root))
                foreach (string path in Directory.GetFiles(Root,"UnityCatalog.json",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal))
                {
                    try
                    {
                        var d=JsonUtility.FromJson<PreparedBuilding>(File.ReadAllText(path));
                        if(d==null||string.IsNullOrWhiteSpace(d.id)||string.IsNullOrWhiteSpace(d.label)||
                           string.IsNullOrWhiteSpace(d.folder)||d.folder!=new DirectoryInfo(Path.GetDirectoryName(path)).Name||
                           d.folder.Any(c=>!char.IsLetterOrDigit(c)&&c!='_'&&c!='-')||
                           CityForgeTimeline.Find(d.eraId)==null||
                           !Enum.TryParse<TownZone>(d.zone,out _)||!Enum.TryParse<LotWealth>(d.wealth,out _)||
                           !float.IsFinite(d.width)||!float.IsFinite(d.depth)||!float.IsFinite(d.height)||d.width<=0||d.depth<=0||d.height<=0)
                            throw new InvalidDataException("Invalid prepared building catalog metadata: "+path);
                        if(definitions.TryGetValue(d.id,out var existing)&&existing.folder!=d.folder)
                            throw new InvalidDataException("Duplicate building ID in different folders: "+d.id);
                        definitions[d.id]=d;
                    }
                    catch(Exception error) when(error is IOException||error is ArgumentException)
                    { Debug.LogError("Prepared building registration failed: "+error.Message); }
                }
            return definitions.Values;
        }
        internal static IEnumerable<LotAssetCatalogEntry> CatalogEntries()
        {
            foreach (var b in Definitions())
            {
                var near=AssetDatabase.LoadAssetAtPath<GameObject>(Root+b.folder+"/Near.prefab");
                var medium=AssetDatabase.LoadAssetAtPath<GameObject>(Root+b.folder+"/Medium.prefab");
                if(!near||!medium)continue;
                yield return new LotAssetCatalogEntry {
                    id=b.id,label=b.label,description="Reconstructed building; authored metres and recessed illuminated rooms.",
                    category="Buildings",group=b.group,subcategory=b.subcategory,eraId=b.eraId,
                    additionalEraIds=b.additionalEraIds??Array.Empty<string>(),fromYear=CityForgeTimeline.Find(b.eraId).FirstYear,
                    prefab=near,distantPrefab=medium,materialOverride=null,preserveEmissionColors=true,
                    fitModel=false,defaultBuildingScale=1,footprint=new Vector2(b.width,b.depth),
                    buildingDimensionsMeters=new Vector3(b.width,b.height,b.depth),
                    growableZone=Enum.Parse<TownZone>(b.zone),growableWealth=Enum.Parse<LotWealth>(b.wealth)
                };
            }
        }
    }
}
