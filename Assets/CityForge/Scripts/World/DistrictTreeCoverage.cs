using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Seeded woodland footprints, ranked per original district, with fixed in-forest spacing.</summary>
    public static class DistrictTreeCoverage
    {
        public const float Spacing=1024f/108;
        public readonly struct Plant
        {
            public readonly Vector2 point;public readonly uint hash;public readonly float score;
            public Plant(Vector2 point,uint hash,float score){this.point=point;this.hash=hash;this.score=score;}
        }
        public static bool Applies(RegionCreationSettings recipe)=>recipe!=null&&recipe.treeCoverageVersion==1;
        public static uint Hash(int x,int z,int seed)
        {
            unchecked{uint h=(uint)(x*73856093^z*19349663^seed);h^=h>>16;h*=0x7feb352d;h^=h>>15;return h;}
        }
        public static float Score(Vector2 uv,RegionCreationSettings recipe)
        {
            uint seed=Hash(37,19,recipe.seed);float ox=37+(seed&1023)*.137f,oz=19+((seed>>10)&1023)*.137f;
            var p=uv*recipe.GridSide;
            return Mathf.PerlinNoise(p.x*3.8912f+ox,p.y*3.8912f+oz)*.82f+Mathf.PerlinNoise(p.x*11.264f+14+ox,p.y*11.264f+51+oz)*.18f;
        }
        // Pure planner shared by gameplay and checks. Exclusions happen before the
        // percentile so coverage describes plantable land, including coastal cells.
        public static List<Plant> Plan(RegionCreationSettings recipe,int district,float mapSide,Func<Vector2,bool> eligible,out int available)
        {
            var bounds=RegionBiomeMap.DistrictBounds(district,recipe.GridSide);float side=mapSide/recipe.GridSide;
            var origin=(bounds.min-Vector2.one*.5f)*mapSide;int n=Mathf.Max(1,Mathf.RoundToInt(side/Spacing));float step=side/n;
            var points=new List<Plant>(n*n);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                uint h=Hash(x,z,recipe.seed^district*19349663);
                float jx=((h&65535)/65535f-.5f)*.55f,jz=(((h>>16)&65535)/65535f-.5f)*.55f;
                var p=origin+new Vector2(x+.5f+jx,z+.5f+jz)*step;if(!eligible(p))continue;
                points.Add(new Plant(p,h,Score(p/mapSide+Vector2.one*.5f,recipe)));
            }
            points.Sort((a,b)=>{int order=b.score.CompareTo(a.score);return order!=0?order:a.hash.CompareTo(b.hash);});
            available=points.Count;int target=Mathf.RoundToInt(available*recipe.TreeCoverage(district)/100f);
            if(target<points.Count)points.RemoveRange(target,points.Count-target);return points;
        }
        public static List<Plant> Plan(MeadowDistrictLandscape land)
        {
            var result=new List<Plant>();var recipe=land.View.CreationSettings;
            foreach(int district in land.View.ActiveDistrictMembers){
                result.AddRange(Plan(recipe,district,land.View.MapSide,p=>land.Contains(p)&&!land.View.InCampClearing(p,8)
                    &&land.CoverageWaterDistance(p)>=9&&land.RoadDistance(p)>=5&&land.CoverageHeight(p)>=land.View.SeaLevel+.2f
                    &&land.CoverageSlope(p)<=25,out _));
            }
            return result;
        }
    }
}
