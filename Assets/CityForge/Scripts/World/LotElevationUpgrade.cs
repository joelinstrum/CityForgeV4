using System.Linq;
using CityForge.Lots;
using UnityEngine;
namespace CityForge.World
{
    // Old placed snapshots predate authored elevation. Import only matching heights,
    // once; keep geometry and every explicit district adjustment authoritative.
    internal static class LotElevationUpgrade
    {
        internal static void Apply(DistrictLotRecord record, LotDefinition authored)
        {
            if(record.authoredElevationVersion!=0||authored==null||record.definition.id!=authored.id)return;
            var placed=record.definition;
            record.definition=placed=LotDefinition.Parse(JsonUtility.ToJson(placed));
            if(placed.elevation==0)placed.elevation=authored.elevation;
            foreach(var item in placed.objects){
                if(item.elevation!=0)continue;
                var source=authored.objects.FirstOrDefault(value=>value.id==item.id&&value.assetId==item.assetId);
                if(source!=null)item.elevation=source.elevation;
            }
            record.authoredElevationVersion=1;
        }
    }
}
