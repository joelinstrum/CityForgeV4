using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    // A planting designation, independent of the ordinary tree catalog and era palette.
    internal sealed class StreetTreePalette
    {
        internal readonly string Id, Label, AssetId;
        internal readonly float Scale, Spacing, CrownWidth;
        internal readonly bool HasPlanter;
        private StreetTreePalette(string id,string label,string asset,float scale,float spacing,float crown,bool planter=false)
        {Id=id;Label=label;AssetId=asset;Scale=scale;Spacing=spacing;CrownWidth=crown;HasPlanter=planter;}
        internal static readonly StreetTreePalette[] Styles={
            new("pencil-cypress","Narrow cypress","pencil-cypress-small",1,10,1.7f),
            new("small-elm","Small elm","siberian-elm-young",.7f,12,3.5f),
            new("coconut-palm","Palms",null,1,12,5f),
            new("city-tree-small","City tree · small","city-tree-04-bare",1,10,3f),
            new("city-tree-small-planter","Small · planter","city-tree-04-planter",1,10,3f,true),
            new("city-tree-medium","City tree · medium","city-tree-03-bare",1,12,4f),
            new("city-tree-medium-planter","Medium · planter","city-tree-03-planter",1,12,4f,true)
        };
        internal static StreetTreePalette Find(string id)=>Array.Find(Styles,s=>s.Id==id);
        internal static bool Valid(string id)=>string.IsNullOrEmpty(id)||Find(id)!=null;
        internal static string DisplayName(string id)=>Find(id)?.Label??"No trees";
        internal readonly struct Plant
        {
            internal readonly Vector2 Point;internal readonly float Yaw;
            internal Plant(Vector2 point,float yaw){Point=point;Yaw=yaw;}
        }
        internal static List<Plant> Plan(DistrictRoadRecord road,IReadOnlyList<Vector2> line,Func<Vector2,bool> clear)
        {
            var result=new List<Plant>();var style=Find(road.streetTreeStyle);
            if(style==null||line==null||line.Count<2)return result;
            float total=0;int count=line.Count-(road.closed?0:1);
            for(int i=0;i<count;i++)total+=Vector2.Distance(line[i],line[(i+1)%line.Count]);
            float start=0,next=8;
            for(int i=0;i<count&&result.Count<128;i++)
            {
                var a=line[i];var b=line[(i+1)%line.Count];float length=Vector2.Distance(a,b);if(length<.001f)continue;
                var tangent=(b-a)/length;var normal=Vector2.Perpendicular(tangent);
                for(;next<=start+length;next+=style.Spacing)
                {
                    if(!road.closed&&next>total-8)break;
                    var center=a+tangent*(next-start);
                    for(int side=PrototypeRegionView.IsDividedBoulevard(road.surfaceId)?0:-1;side<=1;side+=2)
                    {
                        var p=center+normal*side*(road.width*.5f+.9f);
                        if(!result.Exists(other=>(other.Point-p).sqrMagnitude<25)&&clear(p))result.Add(new Plant(p,(next*37+side*83)%360));
                    }
                    if(result.Count>=128)break;
                }
                start+=length;
            }
            return result;
        }
    }
}
