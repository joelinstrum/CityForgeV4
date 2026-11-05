using UnityEngine;
namespace CityForge.World
{
    public sealed partial class MeadowDistrictLandscape
    {
        /// <summary>Locally repairs old meadow caches polluted by the unconditional riverbank pass.</summary>
        public void RepairPlayerLakeMeadow(RegionWaterBody body)
        {
            if(!body.playerCreated||View.CreationSettings==null||View.CreationSettings.version<5||View.Coastline)return;
            const float margin=24;float side=View.MapSide;int n=terrainData.alphamapResolution;
            var min=(body.Bounds.min-Vector2.one*.5f)*side-Vector2.one*margin;var max=(body.Bounds.max-Vector2.one*.5f)*side+Vector2.one*margin;
            int x0=Mathf.Clamp(Mathf.FloorToInt((min.x-Origin.x)/Side*(n-1)),0,n-1),x1=Mathf.Clamp(Mathf.CeilToInt((max.x-Origin.x)/Side*(n-1)),0,n-1),z0=Mathf.Clamp(Mathf.FloorToInt((min.y-Origin.y)/Side*(n-1)),0,n-1),z1=Mathf.Clamp(Mathf.CeilToInt((max.y-Origin.y)/Side*(n-1)),0,n-1);
            var maps=terrainData.GetAlphamaps(x0,z0,x1-x0+1,z1-z0+1);int count=Baseline.layers.Length;
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){
                var p=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                if(!Contains(p)||BaseWaterDistance(p)<20||View.InPlayerChannelClearing(p)||View.InCampClearing(p,4)||RoadDistance(p)<Baseline.roadWidth+2||PlayerLakeGeometry.Distance(body,p,side)>margin)continue;
                bool other=false;foreach(var b in View.Data.WaterBodies)if(b!=body&&(b.kind==WaterBodyKind.Lake||b.kind==WaterBodyKind.Pond)&&PlayerLakeGeometry.Distance(b,p,side)<12){other=true;break;}if(other)continue;
                var ecology=View.Data.Biomes?.At(p/side+Vector2.one*.5f);float forest=ecology?.woodland??Woodland(p),dry=ecology?.dryness??0,patch=Noise(p,13,8),litter=Canopy(p)*(.5f+patch*.25f),soil=dry*.08f;
                int zz=z-z0,xx=x-x0;float extras=0;for(int l=4;l<count;l++)extras+=maps[zz,xx,l];float mass=Mathf.Clamp01(1-extras),grass=1-soil;
                maps[zz,xx,0]=mass*soil;maps[zz,xx,1]=mass*grass*(1-forest*.4f)*(1-litter);maps[zz,xx,2]=mass*grass*forest*.4f*(1-litter);maps[zz,xx,3]=mass*grass*litter;
                for(int l=count;l<maps.GetLength(2);l++)maps[zz,xx,l]=0;
                if(lakeBaseAlphamaps!=null)for(int l=0;l<Mathf.Min(count,lakeBaseAlphamaps.GetLength(2));l++)lakeBaseAlphamaps[z,x,l]=maps[zz,xx,l];
            }
            terrainData.SetAlphamaps(x0,z0,maps);
        }
    }
}
