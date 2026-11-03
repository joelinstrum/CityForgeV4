using System;
using System.Collections;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class MeadowDistrictLandscape
    {
        // Rebuild beach presentation after restoring canonical painted ground,
        // as lake beds/camp clearings do. Never add a transient slot to the save.
        float[,,] coastalBaseAlphamaps;
        float[,] coastalBeachDistances;
        public int CoastalBeachLayer {get;private set;}=-1;
        public int CoastalBeachPaintedSamples {get;private set;}
        public int CoastalBeachDirtSamples {get;private set;}

        void BuildCoastalBeachDistances()
        {
            int n=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Side/2)),256,2048)+1;
            float pixel=Side/(n-1);var heights=new float[n,n];var wet=new bool[n,n];
            var oceans=Array.FindAll(View.Data.WaterBodies,b=>b.kind==WaterBodyKind.Ocean);
            coastalBeachDistances=new float[n,n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                var p=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                float h=Height(p)-View.SeaLevel;heights[z,x]=h;
                if(h<0){var uv=p/View.MapSide+Vector2.one*.5f;foreach(var ocean in oceans)if(ocean.Contains(uv)){wet[z,x]=true;break;}}
                coastalBeachDistances[z,x]=Side;
            }
            for(int z=1;z<n-1;z++)for(int x=1;x<n-1;x++){
                float nearest=Side,h=heights[z,x];
                void Edge(int nx,int nz){if(wet[z,x]==wet[nz,nx])return;nearest=Mathf.Min(nearest,pixel*Mathf.Clamp01(Mathf.Abs(h)/Mathf.Max(.00001f,Mathf.Abs(h-heights[nz,nx]))));}
                Edge(x-1,z);Edge(x+1,z);Edge(x,z-1);Edge(x,z+1);
                coastalBeachDistances[z,x]=nearest;
            }
            for(int z=1;z<n;z++)for(int x=1;x<n;x++)coastalBeachDistances[z,x]=Mathf.Min(coastalBeachDistances[z,x],Mathf.Min(coastalBeachDistances[z,x-1]+pixel,Mathf.Min(coastalBeachDistances[z-1,x]+pixel,coastalBeachDistances[z-1,x-1]+pixel*1.41421356f)));
            for(int z=n-2;z>=0;z--)for(int x=n-2;x>=0;x--)coastalBeachDistances[z,x]=Mathf.Min(coastalBeachDistances[z,x],Mathf.Min(coastalBeachDistances[z,x+1]+pixel,Mathf.Min(coastalBeachDistances[z+1,x]+pixel,coastalBeachDistances[z+1,x+1]+pixel*1.41421356f)));
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(wet[z,x])coastalBeachDistances[z,x]*=-1;
        }

        float CoastalBeachDistance(Vector2 p)
        {
            int last=coastalBeachDistances.GetLength(0)-1;
            float gx=Mathf.Clamp((p.x-Origin.x)/Side*last,0,last-.001f),gz=Mathf.Clamp((p.y-Origin.y)/Side*last,0,last-.001f);int x=(int)gx,z=(int)gz;
            return Mathf.Lerp(Mathf.Lerp(coastalBeachDistances[z,x],coastalBeachDistances[z,x+1],gx-x),Mathf.Lerp(coastalBeachDistances[z+1,x],coastalBeachDistances[z+1,x+1],gx-x),gz-z);
        }

        public float CoastalBeachWeight(Vector2 p)
        {
            if(coastalBeachDistances==null)return 0;
            float d=CoastalBeachDistance(p);
            if(d>44||!Contains(p))return 0;
            // The ocean floor stays sand at every depth. Fading the submerged
            // apron back to canonical meadow makes grass show through the water.
            if(d<=0)return 1;
            if(View.InCampClearing(p)||RoadDistance(p)<=Baseline.roadWidth)return 0;
            float width=26+10*Noise(p,55,712)+4*Noise(p,11,714);
            float rough=(Noise(p,5,715)-.5f)*3;
            float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width*.28f,width,d+rough));
            float high=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,7,Height(p)-View.SeaLevel));
            float steep=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(20,35,MountainSlope(p)));
            return edge*high*steep;
        }

        float CoastalBeachDirtWeight(Vector2 p,float beach)
        {
            if(beach<=0)return 0;
            float d=CoastalBeachDistance(p);if(d<=0)return 0;
            float dry=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,12,d));
            return beach*dry*(.10f+.25f*Noise(p,12,716))*Mathf.Lerp(.5f,1,1-beach);
        }

        IEnumerator PaintCoastalBeachStages()
        {
            if(IsRegionOverview||View.Coastline||!HasOcean())yield break;
            var source=Resources.Load<TerrainLayer>("RiverBeds/RichSand");
            if(!source)throw new InvalidOperationException("Missing approved coastal sand terrain layer.");
            BuildCoastalBeachDistances();yield return null;
            var layer=Own(Instantiate(source));layer.name="Coastal beach sand";
            layer.diffuseTexture=Own(MatteStreamSoil(source.diffuseTexture));
            layer.specular=new Color(.025f,.025f,.025f);layer.smoothness=0;layer.normalScale=.45f;
            int n=terrainData.alphamapResolution,count=terrainData.terrainLayers.Length;
            coastalBaseAlphamaps=terrainData.GetAlphamaps(0,0,n,n);
            var maps=new float[n,n,count+1];var timer=System.Diagnostics.Stopwatch.StartNew();
            for(int z=0;z<n;z++){
                for(int x=0;x<n;x++){
                    var p=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                    float weight=CoastalBeachWeight(p);if(weight>.5f)CoastalBeachPaintedSamples++;
                    float dirt=CoastalBeachDirtWeight(p,weight);if(dirt>.01f)CoastalBeachDirtSamples++;
                    for(int i=0;i<count;i++)maps[z,x,i]=coastalBaseAlphamaps[z,x,i]*(1-weight);
                    // Layer zero is the approved matte courtyard/settlement dirt.
                    maps[z,x,0]+=dirt;maps[z,x,count]=weight-dirt;
                }
                if(timer.ElapsedMilliseconds>12){yield return null;timer.Restart();}
            }
            var layers=new TerrainLayer[count+1];Array.Copy(terrainData.terrainLayers,layers,count);layers[count]=layer;
            terrainData.terrainLayers=layers;terrainData.SetAlphamaps(0,0,maps);CoastalBeachLayer=count;
        }
    }
}
