using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForge.Lots
{
    // Coordinates are measured with the model rotated to face South (-Z).
    [Serializable] public sealed class LotRowGeometry
    {
        public float leftWall, rightWall, frontStep;
        internal bool Valid => float.IsFinite(leftWall)&&float.IsFinite(rightWall)&&float.IsFinite(frontStep)&&rightWall-leftWall>.25f&&rightWall-leftWall<128;
    }

    internal static class LotRowBuildings
    {
        const float Step=.025f;
        const int MaxBuildings=64;
        sealed class Choice { internal LotAssetCatalogEntry Asset; internal LotRowGeometry Geometry; internal float Yaw; internal Vector2 Envelope; }
        sealed class State { internal int[] Row; internal int Cost; }
        internal static LotRowGeometry Geometry(LotAssetCatalogEntry asset, bool applyDefaultScale = true)
        {
            var saved=LotBuildingInfo.Load(asset.id)?.rowGeometry;
            if(saved?.Valid==true)return Scaled(saved);
            if(asset.rowGeometry?.Valid==true)return Scaled(asset.rowGeometry);
            float yaw=LotGrowableBuildings.FrontageOffset(asset.id);
            var bounds=LotRuntimeLoader.Footprint(new LotObjectDefinition{assetId=asset.id,yaw=yaw},asset);
            return Scaled(new LotRowGeometry{leftWall=bounds.xMin,rightWall=bounds.xMax,frontStep=bounds.yMin});
            LotRowGeometry Scaled(LotRowGeometry source){float scale=applyDefaultScale?asset.DefaultPlacementScale:1;return new LotRowGeometry{leftWall=source.leftWall*scale,rightWall=source.rightWall*scale,frontStep=source.frontStep*scale};}
        }
        internal static Rect FrontBounds(LotDefinition lot)=>LotGroundOverlay.QuarterTurn(lot.zoningLayout.BuildingSlot,-(int)lot.zoningLayout.frontage);
        internal static float FrontLine(LotDefinition lot)
        {
            var layout=lot.zoningLayout;
            return layout.hasEntrance?LotGroundOverlay.QuarterTurn(layout.entrance,-(int)layout.frontage).y:FrontBounds(lot).yMin;
        }
        static Choice Profile(LotAssetCatalogEntry asset)
        {
            var geometry=Geometry(asset);if(!geometry.Valid)return null;
            float yaw=LotGrowableBuildings.FrontageOffset(asset.id);
            var info=LotBuildingInfo.LoadEntry(asset.id);
            if(info!=null)yaw=-90*(int)info.facing;
            var envelope=LotRuntimeLoader.Footprint(new LotObjectDefinition{assetId=asset.id,yaw=yaw,scale=asset.DefaultPlacementScale},asset).size;
            return new Choice{Asset=asset,Geometry=geometry,Yaw=yaw,Envelope=envelope};
        }
        static LotObjectDefinition Place(LotDefinition lot,Choice profile,float wallCenter)
        {
            var layout=lot.zoningLayout;var g=profile.Geometry;
            var local=new Vector2(wallCenter-(g.leftWall+g.rightWall)*.5f,FrontLine(lot)-g.frontStep);
            return new LotObjectDefinition{assetId=profile.Asset.id,scale=profile.Asset.DefaultPlacementScale,position=LotGroundOverlay.QuarterTurn(local,(int)layout.frontage),
                yaw=Mathf.Repeat(profile.Yaw+90*(int)layout.frontage,360),generatedRowBuilding=true};
        }
        internal static string PlacementProblem(LotDefinition lot,LotAssetCatalogEntry asset,LotAssetCatalog catalog,out LotObjectDefinition placement)
        {
            placement=null;var profile=Profile(asset);if(profile==null)return "Set valid row wall and step markers in Building info.";
            var slot=FrontBounds(lot);var g=profile.Geometry;
            if(g.rightWall-g.leftWall>slot.width+.001f)return "Building is wider than the row footprint.";
            var item=Place(lot,profile,slot.center.x);
            var bounds=LotRuntimeLoader.Footprint(item,asset);
            var canonical=LotGroundOverlay.QuarterTurn(bounds,-(int)lot.zoningLayout.frontage);
            if(canonical.yMax>slot.yMax+.001f)return "Building depth or front steps do not fit the row footprint.";
            var problem=lot.zoningLayout.BuildingFitProblem(lot,bounds,catalog);if(problem!=null)return problem;
            placement=item;return null;
        }
        static Choice[] Choices(LotDefinition lot,LotAssetCatalog catalog)=>!catalog?Array.Empty<Choice>():catalog.entries.Where(a=>a!=null&&lot.zoningLayout.AllowsGrowthBuilding(a.id)&&
            a.CanPreviewGrowable(lot.zoningLayout.zone,lot.zoningLayout.wealth,ResidentialGround.LotEra(lot))&&PlacementProblem(lot,a,catalog,out _)==null)
            .OrderBy(a=>a.id,StringComparer.Ordinal).Take(64).Select(Profile).ToArray();
        internal static LotObjectDefinition[] Candidates(LotDefinition lot,LotAssetCatalog catalog)
        {
            var choices=Choices(lot,catalog);
            if(!TryPack(lot,catalog,lot.id,out _,out _))return Array.Empty<LotObjectDefinition>();
            return choices.Select(choice=>Place(lot,choice,FrontBounds(lot).center.x)).ToArray();
        }
        internal static bool TryPack(LotDefinition lot,LotAssetCatalog catalog,string seed,out LotObjectDefinition[] result,out string problem)
        {
            result=Array.Empty<LotObjectDefinition>();problem="Choose buildings that fit the row footprint.";
            var choices=Choices(lot,catalog);if(choices.Length==0)return false;
            var slot=FrontBounds(lot);float gap=lot.zoningLayout.rowGap;
            float left=choices.Max(c=>Mathf.Max(0,c.Envelope.x*.5f+c.Geometry.leftWall));
            float right=choices.Max(c=>Mathf.Max(0,c.Envelope.x*.5f-c.Geometry.rightWall));
            float available=slot.width-left-right;
            int capacity=Mathf.FloorToInt((available+gap)/Step);if(capacity<1)return false;
            var widths=choices.Select(c=>Mathf.CeilToInt((c.Geometry.rightWall-c.Geometry.leftWall+gap)/Step)).ToArray();
            var states=new State[capacity+1];states[0]=new State{Row=Array.Empty<int>()};
            uint hash=Hash(seed??lot.id);
            for(int used=0;used<=capacity;used++){
                var state=states[used];if(state==null||state.Row.Length>=MaxBuildings)continue;
                for(int turn=0;turn<choices.Length;turn++){
                    int index=(turn+(int)(hash%(uint)choices.Length))%choices.Length;
                    int next=used+widths[index];if(next>capacity)continue;
                    bool repeat=state.Row.Length>0&&state.Row[^1]==index;
                    int cost=state.Cost+(repeat?10000:0)+state.Row.Count(i=>i==index)*100+(int)(Hash(seed+":"+used+":"+index)%37);
                    if(states[next]!=null&&states[next].Cost<=cost)continue;
                    var row=new int[state.Row.Length+1];Array.Copy(state.Row,row,state.Row.Length);row[^1]=index;
                    states[next]=new State{Row=row,Cost=cost};
                }
            }
            State best=null;
            for(int i=capacity;i>0;i--)if(states[i]?.Row.Length>=2){best=states[i];break;}
            if(best==null){problem="The row footprint needs room for at least two buildings. Widen it or choose narrower buildings.";return false;}
            float width=best.Row.Sum(i=>choices[i].Geometry.rightWall-choices[i].Geometry.leftWall)+gap*(best.Row.Length-1);
            float cursor=slot.xMin+left+(available-width)*.5f;
            var rowItems=new List<LotObjectDefinition>();
            for(int i=0;i<best.Row.Length;i++){
                var choice=choices[best.Row[i]];var g=choice.Geometry;float w=g.rightWall-g.leftWall;
                var item=Place(lot,choice,cursor+w*.5f);item.id=$"{Hash(seed??lot.id):x8}{Hash(choice.Asset.id):x8}{i:x8}00000001";
                if(lot.zoningLayout.BuildingFitProblem(lot,LotRuntimeLoader.Footprint(item,choice.Asset),catalog)!=null){problem="The row overlaps fixed buildings or extends outside the lot.";return false;}
                rowItems.Add(item);cursor+=w+gap;
            }
            if(lot.objects.Count+rowItems.Count>512){problem="Leave room for the row buildings in the lot object limit.";return false;}
            result=rowItems.ToArray();problem=null;return true;
        }
        internal static LotDefinition Materialize(LotDefinition layout,LotAssetCatalog catalog,string seed)
        {
            if(!TryPack(layout,catalog,seed,out var buildings,out var problem))throw new InvalidOperationException(problem);
            var grown=LotDefinition.Parse(JsonUtility.ToJson(layout));grown.objects.AddRange(buildings);return grown;
        }
        static uint Hash(string text){uint value=2166136261;foreach(char c in text??"")value=(value^c)*16777619;return value;}
    }
}
