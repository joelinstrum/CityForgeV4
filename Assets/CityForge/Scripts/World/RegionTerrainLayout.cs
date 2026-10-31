using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    public enum RegionTerrainShape { Land, Pangea, Coast, MountainBorder, OneIsland, SmallIslands, Archipelago, Peninsula }
    /// <summary>Version-six land/ocean plan, shared by generation, shore descriptors and wizard preview.</summary>
    public sealed class RegionTerrainLayout
    {
        readonly RegionTerrainShape shape;readonly int seed,peninsulaOrientation;readonly float phase;
        readonly Vector2[] centers;readonly float[] radii;
        public bool HasOcean=>shape!=RegionTerrainShape.Land&&shape!=RegionTerrainShape.MountainBorder;
        public RegionTerrainLayout(RegionCreationSettings recipe)
        {
            shape=recipe.terrainShape;seed=recipe.seed;peninsulaOrientation=recipe.peninsulaNorthToSouth?3:seed;phase=(seed%997)*.0063f;
            int count=shape==RegionTerrainShape.SmallIslands?9:shape==RegionTerrainShape.Archipelago?4:1;centers=new Vector2[count];radii=new float[count];var rng=new System.Random(seed^0x713ab);
            for(int i=0;i<count;i++){
                if(shape==RegionTerrainShape.Archipelago){centers[i]=new Vector2(.27f+i%2*.46f,.27f+i/2*.46f)+new Vector2((float)rng.NextDouble()-.5f,(float)rng.NextDouble()-.5f)*.035f;radii[i]=.145f+(float)rng.NextDouble()*.025f;continue;}
                centers[i]=count==1?Vector2.one*.5f:new Vector2(.2f+i%3*.3f,.2f+i/3*.3f)+new Vector2((float)rng.NextDouble()-.5f,(float)rng.NextDouble()-.5f)*.07f;radii[i]=count==1?.32f:.065f+(float)rng.NextDouble()*.028f;}
        }
        public static Vector2 Orient(Vector2 p,int seed)=>((uint)seed%4) switch {1=>new Vector2(p.y,1-p.x),2=>Vector2.one-p,3=>new Vector2(1-p.y,p.x),_=>p};
        static Vector2 Unorient(Vector2 p,int seed)=>((uint)seed%4) switch {1=>new Vector2(1-p.y,p.x),2=>Vector2.one-p,3=>new Vector2(p.y,1-p.x),_=>p};
        float Coast(float v)=>.26f+.035f*Mathf.Sin(v*7+phase)+.018f*Mathf.Sin(v*19-phase);
        float PeninsulaCenter(float u)=>.5f+.035f*Mathf.Sin(u*7+phase);
        float PeninsulaWidth(float u)=>.24f-.065f*u/.62f;
        float Radius(float angle,int island)
        {
            if(shape==RegionTerrainShape.Pangea){float c=Mathf.Abs(Mathf.Cos(angle))/.46f,s=Mathf.Abs(Mathf.Sin(angle))/.44f;return Mathf.Pow(c*c*c*c+s*s*s*s,-.25f)*(1+.025f*Mathf.Sin(angle*3+phase)+.018f*Mathf.Sin(angle*7-phase));}
            return radii[island]*(1+.10f*Mathf.Sin(angle*3+phase+island)+.065f*Mathf.Sin(angle*5-phase+island));
        }
        // Positive on land, negative in ocean, in whole-region normalized units.
        public float LandDistance(Vector2 p)
        {
            if(!HasOcean)return 1;
            if(shape==RegionTerrainShape.Coast){p=Orient(p,seed);return p.x-Coast(p.y);}
            if(shape==RegionTerrainShape.Peninsula){p=Orient(p,peninsulaOrientation);float u=Mathf.Min(p.x,.62f);var center=new Vector2(u,PeninsulaCenter(u));return PeninsulaWidth(u)-Vector2.Distance(p,center);}
            float best=float.NegativeInfinity;
            for(int i=0;i<centers.Length;i++){var d=p-centers[i];float angle=Mathf.Atan2(d.y,d.x);best=Mathf.Max(best,Radius(angle,i)-d.magnitude);}return best;
        }
        public RegionWaterBody CreateOcean()
        {
            if(!HasOcean)return null;
            var body=new RegionWaterBody{id="regional-ocean",kind=WaterBodyKind.Ocean,appearanceProfile="coastal-ocean",hasSurfaceLevel=true,surfaceLevelNormalized=12f/PrototypeRegionData.Height};
            if(shape==RegionTerrainShape.Peninsula){
                var points=new List<Vector2>();
                for(int i=0;i<=96;i++){float u=.62f*i/96;points.Add(new Vector2(u,PeninsulaCenter(u)-PeninsulaWidth(u)));}
                for(int i=1;i<=96;i++){float a=-Mathf.PI*.5f+Mathf.PI*i/96;points.Add(new Vector2(.62f,PeninsulaCenter(.62f))+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*PeninsulaWidth(.62f));}
                for(int i=95;i>=0;i--){float u=.62f*i/96;points.Add(new Vector2(u,PeninsulaCenter(u)+PeninsulaWidth(u)));}
                points.Add(Vector2.up);points.Add(Vector2.one);points.Add(Vector2.right);points.Add(Vector2.zero);
                for(int i=0;i<points.Count;i++)points[i]=Unorient(points[i],peninsulaOrientation);body.outline=points.ToArray();
            }
            else if(shape==RegionTerrainShape.Coast){var points=new List<Vector2>{Unorient(Vector2.zero,seed),Unorient(Vector2.up,seed)};for(int i=256;i>=0;i--){float v=i/256f;points.Add(Unorient(new Vector2(Coast(v),v),seed));}body.outline=points.ToArray();}
            else{body.outline=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};body.holes=new RegionWaterContour[centers.Length];for(int j=0;j<centers.Length;j++){var points=new Vector2[192];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length;points[i]=centers[j]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Radius(a,j);}body.holes[j]=new RegionWaterContour{points=points};}}
            return body;
        }
    }
}
