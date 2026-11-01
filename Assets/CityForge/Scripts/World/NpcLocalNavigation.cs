using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Bounded local grid. Samples displayed terrain, tree trunks and explicit obstacles once.
    /// Paths cannot cross water, steep slopes or diagonal blocked corners. No district-wide navigation claim.</summary>
    public sealed class NpcLocalNavigation
    {
        readonly int Side;
        readonly bool[] open;
        readonly Vector3[] points;
        readonly List<int> cells=new();
        readonly MeadowDistrictLandscape land;
        readonly Vector2 origin;
        public int OpenCount=>cells.Count;
        public NpcLocalNavigation(MeadowDistrictLandscape land,Vector2 center,float radius,IReadOnlyList<Bounds> obstacles,int side=33,float maximumDistance=float.PositiveInfinity)
        {
            if(side<9||side>129||side%2==0)throw new ArgumentException("Use an odd bounded navigation grid");
            Side=side;open=new bool[Side*Side];points=new Vector3[Side*Side];
            this.land=land;origin=center-Vector2.one*(Side/2);
            var trees=land.DisplayTerrain.terrainData.treeInstances;
            var trunks=new List<Vector2>();
            foreach(var t in trees){var p=land.Origin+new Vector2(t.position.x,t.position.z)*land.Side;if(Vector2.Distance(p,center)<Side)trunks.Add(p);}
            for(int z=0;z<Side;z++)for(int x=0;x<Side;x++){
                int i=z*Side+x;var p=origin+new Vector2(x,z);float h=land.Height(p);bool valid=x>0&&z>0&&x<Side-1&&z<Side-1&&Vector2.Distance(p,center)<=maximumDistance;
                for(int k=0;k<9&&valid;k++){
                    float angle=k*Mathf.PI/4;var q=p+(k==8?Vector2.zero:new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
                    valid=land.Contains(q)&&land.Height(q)>land.View.SeaLevel+.35f&&Mathf.Abs(land.Height(q)-h)<.22f;
                }
                foreach(var tree in trunks)if((tree-p).sqrMagnitude<(radius+.6f)*(radius+.6f)){valid=false;break;}
                points[i]=new Vector3(p.x,h,p.y);
                foreach(var b in obstacles)if(b.Contains(points[i]+Vector3.up*.5f)){valid=false;break;}
                open[i]=valid;if(valid)cells.Add(i);
            }
            // Keep one connected component so people cannot spawn in unreachable pockets behind tents.
            if(cells.Count>0){
                int start=cells[0];float nearest=float.PositiveInfinity;
                foreach(int i in cells){float d=(new Vector2(points[i].x,points[i].z)-center).sqrMagnitude;if(d<nearest){nearest=d;start=i;}}
                var reachable=new bool[open.Length];var queue=new Queue<int>();queue.Enqueue(start);reachable[start]=true;
                while(queue.Count>0){int a=queue.Dequeue();foreach(int b in new[]{a-1,a+1,a-Side,a+Side})if(b>=0&&b<open.Length&&open[b]&&!reachable[b]&&Mathf.Abs(points[a].y-points[b].y)<=.3f){reachable[b]=true;queue.Enqueue(b);}}
                cells.Clear();for(int i=0;i<open.Length;i++){open[i]&=reachable[i];if(open[i])cells.Add(i);}
            }
        }
        int Index(Vector3 p){int x=Mathf.RoundToInt(p.x-origin.x),z=Mathf.RoundToInt(p.z-origin.y);return x<0||z<0||x>=Side||z>=Side?-1:z*Side+x;}
        public bool Suitable(Vector3 p){int i=Index(p);return i>=0&&open[i]&&land.Contains(new Vector2(p.x,p.z));}
        public Vector3 Sample(System.Random random)=>points[cells[random.Next(cells.Count)]];
        public List<Vector3> FindPath(Vector3 from,Vector3 to)
        {
            int start=Index(from),goal=Index(to);if(start<0||goal<0||!open[start]||!open[goal])return null;
            var previous=new int[open.Length];Array.Fill(previous,-1);var queue=new Queue<int>();queue.Enqueue(start);previous[start]=start;
            // Four-neighbor BFS is sufficient for this small fixed grid, and prevents corner cutting.
            while(queue.Count>0&&previous[goal]<0){int a=queue.Dequeue();foreach(int b in new[]{a-1,a+1,a-Side,a+Side}){
                if(b<0||b>=open.Length||!open[b]||previous[b]>=0||Mathf.Abs(points[a].y-points[b].y)>.3f)continue;
                previous[b]=a;queue.Enqueue(b);
            }}
            if(previous[goal]<0)return null;
            var route=new List<Vector3>();for(int a=goal;a!=start;a=previous[a])route.Add(points[a]);route.Reverse();return route;
        }
        public Vector3 Ground(Vector3 p){p.y=land.Height(new Vector2(p.x,p.z));return p;}
    }
}
