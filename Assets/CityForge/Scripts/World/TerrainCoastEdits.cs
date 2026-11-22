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
        sealed class CoastCell {internal int X,Z;internal float BaseClip,Distance,Height;internal bool Seed,Wet,Connected,Previous;}
        readonly Dictionary<int,CoastCell> terraformCoast=new();
        readonly List<Rect> terraformCoastPending=new();
        readonly HashSet<ZoneGroundPlane> terraformGroundPending=new();
        readonly Dictionary<int,float> terraformGround=new();
        internal float TerraformGroundBlend(Vector2 p)
        {
            if(!terraformOceanClip||!Contains(p))return 0;
            int n=terraformOceanClip.width;var c=(p-Origin)/Side*n-Vector2.one*.5f;int x=Mathf.FloorToInt(c.x),z=Mathf.FloorToInt(c.y);
            float At(int xx,int zz)=>xx>=0&&zz>=0&&xx<n&&zz<n&&terraformGround.TryGetValue(zz*n+xx,out float v)?v:0;
            return Mathf.Lerp(Mathf.Lerp(At(x,z),At(x+1,z),c.x-x),Mathf.Lerp(At(x,z+1),At(x+1,z+1),c.x-x),c.y-z);
        }
        internal IEnumerable<Vector2> TerraformGroundPoints(){if(terraformOceanClip){float pixel=Side/terraformOceanClip.width;foreach(int key in terraformGround.Keys)yield return Origin+new Vector2(key%terraformOceanClip.width+.5f,key/terraformOceanClip.width+.5f)*pixel;}}
        bool terraformCoastScheduled,terraformCoastDeferred;
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
        internal void QueueTerraformCoast(Rect bounds,bool defer=false,ZoneGroundPlane plane=null)
        {
            if(!terraformOceanClip)return;if(plane!=null)terraformGroundPending.Add(plane);bounds.min-=Vector2.one*48;bounds.max+=Vector2.one*48;terraformCoastPending.Add(bounds);
            terraformCoastDeferred|=defer;if(terraformCoastDeferred)return;
            if(!terraformCoastScheduled){terraformCoastScheduled=true;StartCoroutine(RefreshTerraformCoastWhenReady());}
        }
        IEnumerator RefreshTerraformCoastWhenReady(){yield return null;while(View&&View.TerraformPainting)yield return null;if(View&&!terraformCoastDeferred)FlushTerraformCoast();terraformCoastScheduled=false;}
        internal bool TerraformOceanAt(Vector2 p)
        {
            if(!terraformOceanClip||!Contains(p))return false;int n=terraformOceanClip.width;var c=Vector2Int.FloorToInt((p-Origin)/Side*n);
            return terraformCoast.TryGetValue(c.y*n+c.x,out var sample)&&sample.Connected;
        }
        internal IEnumerable<Vector2> TerraformOceanPoints(){if(terraformOceanClip){float pixel=Side/terraformOceanClip.width;foreach(var c in terraformCoast.Values)if(c.Connected)yield return Origin+new Vector2(c.X+.5f,c.Z+.5f)*pixel;}}
        bool TerraformShore(Vector2 p,out float distance)
        {
            distance=0;if(!terraformOceanClip||!Contains(p))return false;int n=terraformOceanClip.width;var c=Vector2Int.FloorToInt((p-Origin)/Side*n);
            if(!terraformCoast.TryGetValue(c.y*n+c.x,out var sample))return false;
            // Sample the signed field continuously; floor-cell lookup stamped visible
            // square teeth into both the bank paint and grass/sand transition.
            float fallback=sample.Distance*(sample.Connected?-1:1);
            var g=(p-Origin)/Side*n-Vector2.one*.5f;int x=Mathf.FloorToInt(g.x),z=Mathf.FloorToInt(g.y);
            float At(int xx,int zz)=>xx>=0&&zz>=0&&xx<n&&zz<n&&terraformCoast.TryGetValue(zz*n+xx,out var cell)?cell.Distance*(cell.Connected?-1:1):fallback;
            distance=Mathf.Lerp(Mathf.Lerp(At(x,z),At(x+1,z),g.x-x),Mathf.Lerp(At(x,z+1),At(x+1,z+1),g.x-x),g.y-z);return true;
        }
        (RegionWaterBody Body,Rect Bounds)[] terraformNativeWaters;
        bool OriginalRenderedWater(Vector2 p)
        {
            var uv=p/View.MapSide+Vector2.one*.5f;
            // The shared Crest mask includes tidal rivers, not only the bay.
            // A surviving native riverbed must seed the same connected-water fill.
            terraformNativeWaters??=View.Data.WaterBodies.Where(b=>!b.playerChannel&&(b.kind==WaterBodyKind.Ocean||b.kind==WaterBodyKind.River)).Select(b=>(b,b.Bounds)).ToArray();
            foreach(var entry in terraformNativeWaters)if(entry.Bounds.Contains(uv)&&entry.Body.Contains(uv))return true;
            return false;
        }
        internal bool TerraformCoastDeferred=>terraformCoastDeferred;
        internal void FlushTerraformCoast()
        {
            if(!terraformOceanClip||terraformCoastPending.Count==0)return;
            int n=terraformOceanClip.width;float pixel=Side/n;var touched=new HashSet<int>();
            var area=terraformCoastPending[0];
            foreach(var bounds in terraformCoastPending){area=Union(area,bounds);var lo=Vector2Int.Max(Vector2Int.zero,Vector2Int.FloorToInt((bounds.min-Origin)/pixel)-Vector2Int.one*2);var hi=Vector2Int.Min(Vector2Int.one*(n-1),Vector2Int.CeilToInt((bounds.max-Origin)/pixel)+Vector2Int.one*2);
                for(int z=lo.y;z<=hi.y;z++)for(int x=lo.x;x<=hi.x;x++)touched.Add(z*n+x);
            }
            terraformCoastPending.Clear();terraformCoastDeferred=false;
            foreach(var plane in terraformGroundPending){
                float margin=PrototypeRegionView.GroundPlaneBlend(plane);var bounds=plane.Bounds;bounds.min-=Vector2.one*margin;bounds.max+=Vector2.one*margin;
                var lo=Vector2Int.Max(Vector2Int.zero,Vector2Int.FloorToInt((bounds.min-Origin)/pixel));var hi=Vector2Int.Min(Vector2Int.one*(n-1),Vector2Int.CeilToInt((bounds.max-Origin)/pixel));
                for(int z=lo.y;z<=hi.y;z++)for(int x=lo.x;x<=hi.x;x++){
                    var p=Origin+new Vector2(x+.5f,z+.5f)*pixel;if(!View.InActiveDistrict(p)||!plane.Contains(p,margin))continue;
                    float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(0,margin-8),margin,plane.Distance(p)));
                    int key=z*n+x;terraformGround.TryGetValue(key,out float previous);terraformGround[key]=Mathf.Max(previous,blend);
                }
            }
            terraformGroundPending.Clear();coastalDefaultSources=null;
            foreach(int index in touched){int x=index%n,z=index/n;var p=Origin+new Vector2(x+.5f,z+.5f)*pixel;
                if(!terraformCoast.TryGetValue(index,out var cell)){float clip=terraformOceanClip.GetPixel(x,z).r;cell=new CoastCell{X=x,Z=z,BaseClip=clip,Seed=clip<.5f&&OriginalRenderedWater(p)};terraformCoast[index]=cell;}
                cell.Height=Height(p)-View.SeaLevel;cell.Wet=View.InActiveDistrict(p)&&cell.Height<-.015f;
            }
            var queue=new Queue<CoastCell>();
            bool NativeSea(int x,int z){var p=Origin+new Vector2(x+.5f,z+.5f)*pixel;if(!OriginalRenderedWater(p))return false;if(x<0||z<0||x>=n||z>=n)return true;return terraformOceanClip.GetPixel(x,z).r<.5f;}
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
            float Clip(CoastCell cell){
                // Encode the actual sea crossing, not a binary square-cell edge.
                // Bilinear sampling then follows the sloping bank at subpixel precision.
                bool coast=false;float scale=.03f;
                for(int d=0;d<4;d++){
                    int x=cell.X+CoastDX[d],z=cell.Z+CoastDZ[d];
                    if(WetAt(x,z)!=cell.Connected)coast=true;
                    if(x>=0&&z>=0&&x<n&&z<n&&terraformCoast.TryGetValue(z*n+x,out var other))scale=Mathf.Max(scale,Mathf.Abs(other.Height-cell.Height));
                }
                if(!cell.Connected&&!coast)return 1;
                // Keep one dry shoreline cell under the terrain, as native harbor
                // banks do. The terrain/water intersection draws the visible edge;
                // the coarser Crest clip grid must not cut a staircase offshore.
                if(!cell.Connected&&coast&&cell.Height>=0)return 0;
                return Mathf.Clamp01(.5f+cell.Height/(scale*2));
            }
            foreach(var tile in tiles){int x0=tile.x*64,z0=tile.y*64,w=Mathf.Min(64,n-x0),h=Mathf.Min(64,n-z0);var colors=terraformOceanClip.GetPixels(x0,z0,w,h);
                for(int z=0;z<h;z++)for(int x=0;x<w;x++)if(terraformCoast.TryGetValue((z0+z)*n+x0+x,out var c))colors[z*w+x]=new Color(Clip(c),0,0,1);
                terraformOceanClip.SetPixels(x0,z0,w,h,colors);
            }
            terraformOceanClip.Apply(false,false);
            RefreshTerraformBeach(area);RefreshHarborStones();
            if(shorelinePlants)foreach(Transform plant in shorelinePlants){var p=new Vector2(plant.position.x,plant.position.z);if(TerraformGroundBlend(p)>.1f)plant.gameObject.SetActive(Mathf.Abs(ShoreDistance(p))<3&&Height(p)<View.SeaLevel+2.2f);}
            if(groundCover)groundCover.InvalidateRegion(area);if(Geography)Geography.Invalidate(area);
            foreach(var levels in GetComponentsInChildren<GroundFollowingWaterLevels>())levels.RefreshTerraformOcean(this,area);
            if(terraformOceanProbe)StartCoroutine(RefreshDepth(terraformOceanProbe));
        }
        static Rect Union(Rect a,Rect b)=>Rect.MinMaxRect(Mathf.Min(a.xMin,b.xMin),Mathf.Min(a.yMin,b.yMin),Mathf.Max(a.xMax,b.xMax),Mathf.Max(a.yMax,b.yMax));
    }
}
