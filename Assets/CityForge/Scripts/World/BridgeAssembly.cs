using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    internal static class BridgeAssembly
    {
        internal static string Fit(BridgeInstance bridge, BridgeStyle style, Func<Vector2,float> ground, Func<Vector2,float?> water, bool initialize)
        {
            float length=bridge.Length;
            if(!float.IsFinite(length) || length<16 || length>style.maxLength) return $"Drag a straight crossing 16–{style.maxLength:0} m long.";
            float first=float.PositiveInfinity,last=0,highestWater=float.NegativeInfinity;
            int steps=Mathf.CeilToInt(length);
            for(int i=0;i<=steps;i++)
            {
                float station=length*i/steps;var p=bridge.Point(station);
                for(int side=-1;side<=1;side++)
                {
                    var level=water(p+bridge.Right*(side*(bridge.TotalWidth*.5f+bridge.earthworkFeather)));
                    if(!level.HasValue)continue;
                    first=Mathf.Min(first,station);last=Mathf.Max(last,station);highestWater=Mathf.Max(highestWater,level.Value);
                }
            }
            if(!float.IsFinite(first))return "Drag across water between two dry banks.";
            bridge.deckStart=Mathf.Max(1,first-3);bridge.deckEnd=Mathf.Min(length-1,last+3);
            bridge.channelCenter=(first+last)*.5f;
            bridge.channelWidth=Mathf.Min(bridge.channelWidth,Mathf.Max(0,bridge.deckEnd-bridge.deckStart-2));
            if(initialize)
            {
                bridge.startHeight=ground(bridge.start)+.13f;bridge.endHeight=ground(bridge.end)+.13f;
                bridge.deckHeight=Mathf.Max(highestWater+bridge.clearance+style.deckThickness,Mathf.Max(bridge.startHeight,bridge.endHeight));
                for(float at=bridge.deckStart;at<=bridge.deckEnd;at+=2)bridge.deckHeight=Mathf.Max(bridge.deckHeight,ground(bridge.Point(at))+.4f);
            }
            var supports=new List<BridgeSupport>();
            float gapStart=bridge.channelCenter-bridge.channelWidth*.5f, gapEnd=bridge.channelCenter+bridge.channelWidth*.5f;
            AddSide(bridge.deckStart,Mathf.Max(bridge.deckStart,gapStart));
            AddSide(Mathf.Min(bridge.deckEnd,gapEnd),bridge.deckEnd);
            bridge.supports=supports.ToArray();
            return Validate(bridge,style,ground,water);
            void AddSide(float a,float b)
            {
                int spans=Mathf.Max(1,Mathf.CeilToInt((b-a)/style.supportSpacing));
                for(int j=0;j<=spans;j++)
                {
                    float at=Mathf.Lerp(a,b,(float)j/spans);
                    if(supports.Count>0 && at<=supports[^1].station+.01f)continue;
                    var p=bridge.Point(at);float offset=bridge.TotalWidth*.4f;
                    supports.Add(new BridgeSupport{station=at,leftBed=ground(p-bridge.Right*offset),rightBed=ground(p+bridge.Right*offset)});
                }
            }
        }
        internal static string Validate(BridgeInstance b,BridgeStyle s,Func<Vector2,float> ground,Func<Vector2,float?> water)
        {
            if(Array.IndexOf(s.widths,b.width)<0)return "This style does not support that carriageway width.";
            if(b.sidewalkWidth<.75f)return "The prototype needs at least 0.75 m of sidewalk on each side.";
            if(b.width/b.lanes<2)return "Allow at least 2 m per lane.";
            if(b.clearance<s.minClearance)return $"This style needs at least {s.minClearance:0.0} m under-deck water clearance.";
            if(Mathf.Abs(b.deckHeight-b.startHeight)/b.deckStart>s.maxSlope || Mathf.Abs(b.deckHeight-b.endHeight)/(b.Length-b.deckEnd)>s.maxSlope)
                return $"Approaches exceed {s.maxSlope*100:0}% slope. Drag farther inland or lower the deck.";
            int steps=Mathf.CeilToInt(b.Length);
            for(int i=0;i<=steps;i++)
            {
                float at=b.Length*i/steps;var p=b.Point(at);float height=b.Height(at);
                for(int side=-1;side<=1;side++)
                {
                    var sample=p+b.Right*(side*b.TotalWidth*.5f);var level=water(sample);
                    if(level.HasValue && (at<b.deckStart || at>b.deckEnd))return "Both approaches must be on dry banks.";
                    if(level.HasValue && height-s.deckThickness-level.Value<b.clearance-.025f)return "Raise the deck to provide the requested under-deck water clearance.";
                    if(at>=b.deckStart && at<=b.deckEnd && ground(sample)>height-s.deckThickness)return "The deck intersects a bank. Raise the deck or choose a different crossing.";
                    if((at<b.deckStart || at>b.deckEnd) && !b.earthwork && ground(sample)>height-.1f)return "Enable approach grading or raise the deck above the banks.";
                    if((at<.5f || at>b.Length-.5f) && Mathf.Abs(ground(sample)+.13f-height)>.4f)return "Choose flatter dry road connections at both ends.";
                }
            }
            if(b.supports==null || b.supports.Length<2)return "The trestle needs at least two bents.";
            float previous=b.deckStart;
            foreach(var support in b.supports)
            {
                if(support.station-previous>s.maxSpan+.01f)return "The navigation opening exceeds the style's maximum span.";
                if(Mathf.Abs(support.station-b.channelCenter)<b.channelWidth*.5f-.01f)return "A pier intersects the navigation opening.";
                if(Mathf.Max(support.leftBed,support.rightBed)>b.deckHeight-s.deckThickness-.1f)return "A trestle bent intersects the deck. Raise the deck.";
                if(b.deckHeight-s.deckThickness-Mathf.Min(support.leftBed,support.rightBed)>s.maxSupportHeight)return "The bed is too deep for this trestle style.";
                previous=support.station;
            }
            return b.deckEnd-previous>s.maxSpan+.01f?"Support spacing exceeds the style's maximum span.":null;
        }
    }
}
