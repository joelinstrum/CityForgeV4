using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WaveHarmonic.Crest;
namespace CityForge.World
{
    public sealed partial class MeadowDistrictLandscape
    {
        Texture2D terraformOceanClip;DepthProbe terraformOceanProbe;
        sealed class CoastCell {internal int X,Z;internal float BaseClip,Distance;internal bool Seed,Wet,Connected,Previous;}
        readonly Dictionary<int,CoastCell> terraformCoast=new();
        readonly List<Rect> terraformCoastPending=new();
        bool terraformCoastScheduled;
        static readonly int[] CoastDX={-1,1,0,0},CoastDZ={0,0,-1,1};
        internal RegionWaterBody TerraformOceanBody=>coastalOceans.FirstOrDefault()??Array.Find(View.Data.WaterBodies,b=>b.kind==WaterBodyKind.Ocean);
        GeneratedStreamAppearance[] terraformStreams;RegionWaterBody[] terraformStreamBodies;
        internal void ClearTerraformObjects(ZoneGroundPlane plane,Rect bounds)
        {
            bool Inside(Vector2 p)=>bounds.Contains(p)&&plane.Contains(p,PrototypeRegionView.GroundPlaneBlend(plane))&&View.InActiveDistrict(p);
            if(wildlife)wildlife.ClearUnderDevelopment(Inside);
            if(terraformStreams==null||terraformStreamBodies!=View.Data.WaterBodies){terraformStreamBodies=View.Data.WaterBodies;terraformStreams=GetComponentsInChildren<GeneratedStreamAppearance>();}
            foreach(var stream in terraformStreams)if(stream)stream.ClearEarthwork(bounds,Inside);
        }
        internal void QueueTerraformCoast(Rect bounds)
        {
            if(!terraformOceanClip)return;terraformCoastPending.Add(bounds);
            if(!terraformCoastScheduled){terraformCoastScheduled=true;StartCoroutine(RefreshTerraformCoastWhenReady());}
        }
        IEnumerator RefreshTerraformCoastWhenReady(){yield return null;while(View&&View.TerraformPainting)yield return null;if(View)FlushTerraformCoast();terraformCoastScheduled=false;}
        internal bool TerraformOceanAt(Vector2 p)
        {
            if(!terraformOceanClip||!Contains(p))return false;int n=terraformOceanClip.width;var c=Vector2Int.FloorToInt((p-Origin)/Side*n);
            return terraformCoast.TryGetValue(c.y*n+c.x,out var sample)&&sample.Connected;
        }
        internal IEnumerable<Vector2> TerraformOceanPoints(){if(terraformOceanClip){float pixel=Side/terraformOceanClip.width;foreach(var c in terraformCoast.Values)if(c.Connected)yield return Origin+new Vector2(c.X+.5f,c.Z+.5f)*pixel;}}
        bool TerraformShore(Vector2 p,out float distance)
        {
            distance=0;if(!terraformOceanClip||!Contains(p))return false;int n=terraformOceanClip.width;var c=Vector2Int.FloorToInt((p-Origin)/Side*n);
            if(!terraformCoast.TryGetValue(c.y*n+c.x,out var sample)||sample.Distance>=Side)return false;
            distance=sample.Distance*(sample.Connected?-1:1);return true;
        }
        (RegionWaterBody Body,Rect Bounds)[] terraformOceans;
        bool OriginalOcean(Vector2 p)
        {
            var uv=p/View.MapSide+Vector2.one*.5f;
            terraformOceans??=coastalOceans.Select(b=>(b,b.Bounds)).ToArray();
            foreach(var entry in terraformOceans)if(entry.Bounds.Contains(uv)&&entry.Body.Contains(uv))return true;
            return false;
        }
        internal void FlushTerraformCoast()
        {
            if(!terraformOceanClip||terraformCoastPending.Count==0)return;
            int n=terraformOceanClip.width;float pixel=Side/n;var touched=new HashSet<int>();
            var area=terraformCoastPending[0];
            foreach(var bounds in terraformCoastPending){area=Union(area,bounds);var lo=Vector2Int.Max(Vector2Int.zero,Vector2Int.FloorToInt((bounds.min-Origin)/pixel)-Vector2Int.one*2);var hi=Vector2Int.Min(Vector2Int.one*(n-1),Vector2Int.CeilToInt((bounds.max-Origin)/pixel)+Vector2Int.one*2);
                for(int z=lo.y;z<=hi.y;z++)for(int x=lo.x;x<=hi.x;x++)touched.Add(z*n+x);
            }
            terraformCoastPending.Clear();
            foreach(int index in touched){int x=index%n,z=index/n;var p=Origin+new Vector2(x+.5f,z+.5f)*pixel;
                if(!terraformCoast.TryGetValue(index,out var cell)){float clip=terraformOceanClip.GetPixel(x,z).r;cell=new CoastCell{X=x,Z=z,BaseClip=clip,Seed=clip<.5f&&OriginalOcean(p)};terraformCoast[index]=cell;}
                cell.Wet=View.InActiveDistrict(p)&&Height(p)<View.SeaLevel-.015f;
            }
            var queue=new Queue<CoastCell>();
            bool NativeSea(int x,int z){var p=Origin+new Vector2(x+.5f,z+.5f)*pixel;if(!OriginalOcean(p))return false;if(x<0||z<0||x>=n||z>=n)return true;return terraformOceanClip.GetPixel(x,z).r<.5f;}
            foreach(var cell in terraformCoast.Values){cell.Previous=cell.Connected;cell.Connected=false;cell.Distance=Side;}
            foreach(var cell in terraformCoast.Values){if(!cell.Wet)continue;bool seed=cell.Seed;
                for(int d=0;d<4&&!seed;d++){int x=cell.X+CoastDX[d],z=cell.Z+CoastDZ[d];if(x<0||z<0||x>=n||z>=n||!terraformCoast.ContainsKey(z*n+x))seed=NativeSea(x,z);}
                if(seed){cell.Connected=true;queue.Enqueue(cell);}
            }
            while(queue.Count>0){var cell=queue.Dequeue();for(int d=0;d<4;d++){int x=cell.X+CoastDX[d],z=cell.Z+CoastDZ[d];if(x<0||z<0||x>=n||z>=n)continue;if(terraformCoast.TryGetValue(z*n+x,out var next)&&next.Wet&&!next.Connected){next.Connected=true;queue.Enqueue(next);}}}
            bool WetAt(int x,int z){if(x>=0&&z>=0&&x<n&&z<n&&terraformCoast.TryGetValue(z*n+x,out var cell))return cell.Connected;return NativeSea(x,z);}
            var tiles=new HashSet<Vector2Int>();
            foreach(var cell in terraformCoast.Values){for(int d=0;d<4;d++)if(WetAt(cell.X+CoastDX[d],cell.Z+CoastDZ[d])!=cell.Connected){cell.Distance=pixel*.5f;break;}
                if(cell.Connected!=cell.Previous||touched.Contains(cell.Z*n+cell.X))tiles.Add(new Vector2Int(cell.X/64,cell.Z/64));}
            var ordered=terraformCoast.Keys.OrderBy(k=>k).ToArray();
            void Relax(CoastCell cell,int dx,int dz,float cost){int x=cell.X+dx,z=cell.Z+dz;if(x>=0&&z>=0&&x<n&&z<n&&terraformCoast.TryGetValue(z*n+x,out var other))cell.Distance=Mathf.Min(cell.Distance,other.Distance+pixel*cost);}
            foreach(int i in ordered){var c=terraformCoast[i];Relax(c,-1,0,1);Relax(c,0,-1,1);Relax(c,-1,-1,1.41421356f);Relax(c,1,-1,1.41421356f);}
            for(int i=ordered.Length-1;i>=0;i--){var c=terraformCoast[ordered[i]];Relax(c,1,0,1);Relax(c,0,1,1);Relax(c,1,1,1.41421356f);Relax(c,-1,1,1.41421356f);}
            foreach(var tile in tiles){int x0=tile.x*64,z0=tile.y*64,w=Mathf.Min(64,n-x0),h=Mathf.Min(64,n-z0);var colors=terraformOceanClip.GetPixels(x0,z0,w,h);
                for(int z=0;z<h;z++)for(int x=0;x<w;x++)if(terraformCoast.TryGetValue((z0+z)*n+x0+x,out var c))colors[z*w+x]=new Color(c.Connected?0:c.BaseClip,0,0,1);
                terraformOceanClip.SetPixels(x0,z0,w,h,colors);
            }
            terraformOceanClip.Apply(false,false);
            RefreshHarborStones();if(Geography)Geography.Invalidate(area);
            foreach(var levels in GetComponentsInChildren<GroundFollowingWaterLevels>())levels.RefreshTerraformOcean(this,area);
            if(terraformOceanProbe)StartCoroutine(RefreshDepth(terraformOceanProbe));
        }
        static Rect Union(Rect a,Rect b)=>Rect.MinMaxRect(Mathf.Min(a.xMin,b.xMin),Mathf.Min(a.yMin,b.yMin),Mathf.Max(a.xMax,b.xMax),Mathf.Max(a.yMax,b.yMax));
    }
}
