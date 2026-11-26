using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    // Own only bounded approach samples. Restoration never overwrites a later external terrain edit.
    internal sealed class BridgeEarthwork : MonoBehaviour
    {
        readonly Dictionary<Vector2Int,(float original,float applied)> samples=new();
        Terrain terrain;
        void Awake(){terrain=GetComponent<Terrain>();}
        internal float SourceHeight(Vector2 p)
        {
            var data=terrain.terrainData;
            if(samples.Count==0)return terrain.SampleHeight(new Vector3(p.x,0,p.y))+terrain.transform.position.y;
            var step=new Vector2(data.size.x,data.size.z)/(data.heightmapResolution-1);
            var origin=new Vector2(transform.position.x,transform.position.z);
            var sample=(p-origin)/step;float x=Mathf.Clamp(sample.x,0,data.heightmapResolution-1),z=Mathf.Clamp(sample.y,0,data.heightmapResolution-1);
            int ix=Mathf.Min(Mathf.FloorToInt(x),data.heightmapResolution-2),iz=Mathf.Min(Mathf.FloorToInt(z),data.heightmapResolution-2);
            if(!samples.ContainsKey(new Vector2Int(ix,iz))&&!samples.ContainsKey(new Vector2Int(ix+1,iz))&&!samples.ContainsKey(new Vector2Int(ix,iz+1))&&!samples.ContainsKey(new Vector2Int(ix+1,iz+1)))
                return terrain.SampleHeight(new Vector3(p.x,0,p.y))+terrain.transform.position.y;
            float Source(int sx,int sz)=>samples.TryGetValue(new Vector2Int(sx,sz),out var owned)?owned.original:data.GetHeight(sx,sz)/data.size.y;
            return Mathf.Lerp(Mathf.Lerp(Source(ix,iz),Source(ix+1,iz),x-ix),Mathf.Lerp(Source(ix,iz+1),Source(ix+1,iz+1),x-ix),z-iz)*data.size.y+terrain.transform.position.y;
        }
        internal void Restore()
        {
            if(!terrain || samples.Count==0)return;
            var data=terrain.terrainData;
            // Group sparse ownership by 32-sample tiles rather than issuing one terrain write per sample.
            var tiles=new Dictionary<Vector2Int,List<Vector2Int>>();
            foreach(var at in samples.Keys)
            {
                var tile=new Vector2Int(at.x/32,at.y/32);
                if(!tiles.TryGetValue(tile,out var list))tiles[tile]=list=new List<Vector2Int>();
                list.Add(at);
            }
            foreach(var tile in tiles)
            {
                int x=tile.Key.x*32,z=tile.Key.y*32;
                int width=Mathf.Min(32,data.heightmapResolution-x),height=Mathf.Min(32,data.heightmapResolution-z);
                var values=data.GetHeights(x,z,width,height);bool changed=false;
                foreach(var at in tile.Value)
                {
                    var sample=samples[at];
                    if(Mathf.Abs(values[at.y-z,at.x-x]-sample.applied)>2f/32766)continue;
                    values[at.y-z,at.x-x]=sample.original;changed=true;
                }
                if(changed)data.SetHeightsDelayLOD(x,z,values);
            }
            samples.Clear();data.SyncHeightmap();terrain.Flush();
        }
        internal void Apply(BridgeInstance b,System.Func<Vector2,bool> contains)
        {
            if(!b.earthwork)return;
            var data=terrain.terrainData;var step=new Vector2(data.size.x,data.size.z)/(data.heightmapResolution-1);
            var origin=new Vector2(terrain.transform.position.x,terrain.transform.position.z);
            for(int end=0;end<2;end++)
            {
                float start=end==0?0:b.deckEnd,finish=end==0?b.deckStart:b.Length;
                var a=b.Point(start);var c=b.Point(finish);float reach=b.TotalWidth*.5f+b.earthworkFeather;
                var low=Vector2Int.Max(Vector2Int.zero,Vector2Int.FloorToInt((Vector2.Min(a,c)-Vector2.one*reach-origin)/step));
                var high=Vector2Int.Min(Vector2Int.one*(data.heightmapResolution-1),Vector2Int.CeilToInt((Vector2.Max(a,c)+Vector2.one*reach-origin)/step));
                if(high.x<low.x || high.y<low.y)continue;
                var heights=data.GetHeights(low.x,low.y,high.x-low.x+1,high.y-low.y+1);
                bool changed=false;
                for(int z=low.y;z<=high.y;z++)for(int x=low.x;x<=high.x;x++)
                {
                    var p=origin+Vector2.Scale(new Vector2(x,z),step);if(!contains(p))continue;
                    var delta=p-b.start;float at=Vector2.Dot(delta,b.Direction),across=Mathf.Abs(Vector2.Dot(delta,b.Right));
                    if(at<start || at>finish || across>reach)continue;
                    float outer=Mathf.Clamp01((across-b.TotalWidth*.5f)/Mathf.Max(.01f,b.earthworkFeather));outer=outer*outer*(3-2*outer);
                    int lx=x-low.x,lz=z-low.y;float old=heights[lz,lx];
                    float target=(b.Height(at)-.13f-terrain.transform.position.y)/data.size.y;
                    var key=new Vector2Int(x,z);
                    float original=samples.TryGetValue(key,out var known) && Mathf.Abs(old-known.applied)<2f/32766?known.original:old;
                    float next=Mathf.Lerp(target,original,outer);if(Mathf.Abs(old-next)<1f/32766)continue;
                    samples[key]=(original,next);heights[lz,lx]=next;changed=true;
                }
                if(changed)data.SetHeightsDelayLOD(low.x,low.y,heights);
            }
            data.SyncHeightmap();terrain.Flush();
        }
    }
}
