using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForge.World
{
    [Serializable] public sealed class NativeCampRecord
    {
        public int district,seed,teepees;
        public Vector2 center,water;
        public float heading;
        public bool nearWater;
        public float Radius=>(14+(teepees-5)*1.6f)*.75f;
        public float Clearance=>Radius+7;
        public Vector2 TeepeePosition(int index){float angle=(heading+Mathf.Lerp(50,310,index/(float)(teepees-1)))*Mathf.Deg2Rad;return center+new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*Radius;}
    }
    /// <summary>Content version is independent of geography recipe12. Only newly generated records are placed.</summary>
    public static class NativeCampPlanner
    {
        public static float MapSide(int size,int grid)=>1024*Mathf.Pow(2,size)*(grid/2f);
        public static NativeCampRecord[] Create(PrototypeRegionData data,RegionCreationSettings recipe,int size)
        {
            if(recipe.nativeCampVersion!=1)return Array.Empty<NativeCampRecord>();
            int grid=recipe.GridSide,target=Mathf.RoundToInt(grid*grid*.2f);float side=MapSide(size,grid),cell=side/grid;
            float sea=TropicalOceanDepth.DeepRecipe(recipe)?TropicalOceanDepth.Sea:12;
            var random=new System.Random(recipe.seed^0x63a27);var order=Enumerable.Range(0,grid*grid).ToArray();
            for(int i=order.Length-1;i>0;i--){int j=random.Next(i+1);(order[i],order[j])=(order[j],order[i]);}
            var wet=new Dictionary<Vector2Int,List<Vector2>>();
            if(data.WaterBodies?.Length>0)for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(data.Heights[z,x]*PrototypeRegionData.Height<sea-.02f){
                var p=new Vector2((x/512f-.5f)*side,(z/512f-.5f)*side);var key=Vector2Int.FloorToInt(p/64);
                if(!wet.TryGetValue(key,out var list))wet[key]=list=new List<Vector2>();list.Add(p);
            }
            float Height(Vector2 p)=>data.SmoothHeight(p.x/side+.5f,p.y/side+.5f)*PrototypeRegionData.Height;
            bool Water(Vector2 p,out Vector2 point){float best=120*120;point=p;var key=Vector2Int.FloorToInt(p/64);
                for(int z=-2;z<=2;z++)for(int x=-2;x<=2;x++)if(wet.TryGetValue(key+new Vector2Int(x,z),out var list))foreach(var q in list){float d=(q-p).sqrMagnitude;if(d<best){best=d;point=q;}}
                return best<120*120;
            }
            bool Ground(NativeCampRecord camp){
                float low=float.PositiveInfinity,high=float.NegativeInfinity;
                for(int z=-4;z<=4;z++)for(int x=-4;x<=4;x++){
                    var p=camp.center+new Vector2(x,z)*(camp.Clearance/4);if((p-camp.center).magnitude>camp.Clearance)continue;
                    float h=Height(p);if(h<sea+.75f)return false;low=Mathf.Min(low,h);high=Mathf.Max(high,h);
                }
                // Use naturally gentle terrain rather than flattening the canonical landscape.
                if(high-low>9f)return false;
                var offset=new System.Random(data.Seed);float phase=offset.Next(100,10000);var origin=new Vector2(camp.district%grid*cell-side/2,camp.district/grid*cell-side/2);
                for(int i=0;i<=64;i++){float t=i/64f;var road=origin+new Vector2(cell*(.15f+.035f*Mathf.Sin(t*8+phase)),cell*(.04f+.90f*t));if(Vector2.Distance(camp.center,road)<camp.Clearance+7)return false;}
                return true;
            }
            var chosen=new List<NativeCampRecord>();
            // Prefer water-adjacent sites across the region before considering dry fallback sites.
            for(int pass=0;pass<2&&chosen.Count<target;pass++)foreach(int district in order){
                if(chosen.Count>=target)break;if(chosen.Any(c=>c.district==district))continue;
                var rng=new System.Random(recipe.seed^unchecked(district*170003+7619));
                int count=rng.Next(5,10);var camp=new NativeCampRecord{district=district,seed=rng.Next(),teepees=count};
                var origin=new Vector2(district%grid*cell-side/2,district/grid*cell-side/2);float margin=camp.Clearance+8;
                if(cell<margin*2)continue;
                bool found=false,bestNear=false;float bestDistance=float.PositiveInfinity;Vector2 bestCenter=default,bestWater=default;
                for(int attempt=0;attempt<420;attempt++){
                    camp.center=origin+new Vector2(margin+(float)rng.NextDouble()*(cell-margin*2),margin+(float)rng.NextDouble()*(cell-margin*2));
                    camp.nearWater=Water(camp.center,out camp.water);if(pass==0&&!camp.nearWater)continue;
                    if(!Ground(camp))continue;
                    float distance=camp.nearWater?Vector2.Distance(camp.center,camp.water):10000;
                    if(distance<bestDistance){found=true;bestDistance=distance;bestCenter=camp.center;bestWater=camp.water;bestNear=camp.nearWater;}
                    if(!camp.nearWater)break;
                }
                if(found){camp.center=bestCenter;camp.water=bestWater;camp.nearWater=bestNear;var direction=camp.water-camp.center;
                    camp.heading=camp.nearWater?Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg:(float)rng.NextDouble()*360;chosen.Add(camp);}
            }
            return chosen.OrderBy(c=>c.district).ToArray();
        }
        public static void Validate(NativeCampRecord[] camps,int grid,float side)
        {
            if(camps==null)return;
            if(camps.Length>Mathf.RoundToInt(grid*grid*.2f)||camps.Any(c=>c==null)||camps.Select(c=>c.district).Distinct().Count()!=camps.Length)throw new ArgumentException("Invalid settlement count or duplicate district");
            float cell=side/grid;
            foreach(var c in camps){
                if(c.district<0||c.district>=grid*grid||c.teepees<5||c.teepees>9||!float.IsFinite(c.center.x)||!float.IsFinite(c.center.y)||!float.IsFinite(c.heading)||!float.IsFinite(c.water.x)||!float.IsFinite(c.water.y))throw new ArgumentException("Invalid settlement metadata");
                var origin=new Vector2(c.district%grid*cell-side/2,c.district/grid*cell-side/2);var local=c.center-origin;
                if(local.x<c.Clearance||local.y<c.Clearance||local.x>cell-c.Clearance||local.y>cell-c.Clearance)throw new ArgumentException("Settlement crosses its original district boundary");
            }
        }
    }
    public sealed partial class PrototypeRegionView
    {
        public NativeCampRecord[] NativeCamps {get;private set;}=Array.Empty<NativeCampRecord>();
        public bool InCampClearing(Vector2 p,float margin=0){foreach(var camp in NativeCamps)if((p-camp.center).sqrMagnitude<Mathf.Pow(camp.Clearance+margin,2))return true;return false;}
    }
}
