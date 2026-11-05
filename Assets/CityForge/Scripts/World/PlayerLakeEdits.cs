using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CityForge.Geometry;
namespace CityForge.World
{
    public static class PlayerLakeGeometry
    {
        public static void ConfigureApprovedProfile(RegionWaterBody body,float mapSide)
        {
            float span=Mathf.Min(body.Bounds.width,body.Bounds.height)*mapSide;
            body.appearanceProfile="calm-lake";body.playerDepth=Mathf.Clamp(span*.06f,2,6);body.playerBankWidth=Mathf.Clamp(span*.35f,6,35);body.playerOuterBankWidth=Mathf.Clamp(span*.14f,4,14);
        }
        public static float Distance(RegionWaterBody body,Vector2 world,float side)=>PointWireGeometry.SignedDistance(body.outline,world/side+Vector2.one*.5f)*side;
        public static float Distance(PrototypeRegionData data,Vector2 world,float side,float limit=20)
        {
            float best=limit;foreach(var body in data.WaterBodies)if(body.playerCreated){var bounds=body.Bounds;bounds.min-=Vector2.one*(limit/side);bounds.max+=Vector2.one*(limit/side);if(bounds.Contains(world/side+Vector2.one*.5f))best=Mathf.Min(best,Distance(body,world,side));}return best;
        }
        public static void Apply(RegionWaterBody body,float[,] heights,Vector2 origin,float side,float vertical,float mapSide)
        {
            int n=heights.GetLength(0);float bank=body.playerBankWidth;float outer=body.playerOuterBankWidth>0?body.playerOuterBankWidth:bank;var min=(body.Bounds.min-Vector2.one*.5f)*mapSide-Vector2.one*outer;var max=(body.Bounds.max-Vector2.one*.5f)*mapSide+Vector2.one*outer;
            int x0=Mathf.Clamp(Mathf.FloorToInt((min.x-origin.x)/side*(n-1)),0,n-1),x1=Mathf.Clamp(Mathf.CeilToInt((max.x-origin.x)/side*(n-1)),0,n-1),z0=Mathf.Clamp(Mathf.FloorToInt((min.y-origin.y)/side*(n-1)),0,n-1),z1=Mathf.Clamp(Mathf.CeilToInt((max.y-origin.y)/side*(n-1)),0,n-1);
            // Reach the same lake depth in 45% of the former underwater run.
            // Keep the saved bank width and dry-side grading intact for existing lakes.
            float underwaterRun=Mathf.Max(2,bank*.45f);
            float level=body.surfaceLevelNormalized*vertical;
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){
                var p=origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*side;float d=Distance(body,p,mapSide);if(d>outer)continue;
                float h=heights[z,x]*vertical;
                float target=d<=0?level-.08f-body.playerDepth*Mathf.SmoothStep(0,1,-d/underwaterRun):Mathf.Lerp(level+.08f,h,Mathf.SmoothStep(0,1,d/outer));
                heights[z,x]=Mathf.Clamp01(Mathf.Min(h,target)/vertical);
            }
        }
        public static void Validate(RegionWaterBody[] bodies)
        {
            if(bodies==null)return;int count=0;foreach(var b in bodies)if(b!=null&&b.playerCreated){
                if(++count>32||b.kind!=WaterBodyKind.Lake&&b.kind!=WaterBodyKind.Pond||b.outline==null||b.outline.Length<3||b.outline.Length>512||!b.hasSurfaceLevel||!float.IsFinite(b.surfaceLevelNormalized)||b.surfaceLevelNormalized<=0||b.surfaceLevelNormalized>=1||!float.IsFinite(b.playerDepth)||b.playerDepth<.5f||b.playerDepth>10||!float.IsFinite(b.playerBankWidth)||b.playerBankWidth<2||b.playerBankWidth>35||!float.IsFinite(b.playerOuterBankWidth)||b.playerOuterBankWidth<0||b.playerOuterBankWidth>14)throw new InvalidDataException("Invalid player lake.");
                foreach(var p in b.outline)if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||p.x<0||p.x>1||p.y<0||p.y>1)throw new InvalidDataException("Invalid player lake footprint.");
            }
        }
    }
    public sealed partial class PrototypeRegionView
    {
        internal void RecordPlayerLake(RegionWaterBody body){var bodies=new List<RegionWaterBody>(Data.WaterBodies);bodies.Add(body);Data.WaterBodies=bodies.ToArray();Waterless=false;LakeWaterVersion=1;WoodlandRevision++;}
        public bool InPlayerLake(Vector2 p)=>Data!=null&&PlayerLakeGeometry.Distance(Data,p,MapSide,1)<0;
        public bool InPlayerLakeClearing(Vector2 p){if(Data==null)return false;foreach(var body in Data.WaterBodies)if(body.playerCreated){float margin=body.playerOuterBankWidth>0?body.playerOuterBankWidth:8;var bounds=body.Bounds;bounds.min-=Vector2.one*(margin/MapSide);bounds.max+=Vector2.one*(margin/MapSide);if(bounds.Contains(p/MapSide+Vector2.one*.5f)&&PlayerLakeGeometry.Distance(body,p,MapSide)<margin)return true;}return false;}
    }
    public sealed partial class MeadowDistrictLandscape
    {
        TerrainData playerLakeBaseTerrain;
        // Eligibility for percentile woodland coverage must use the pre-edit ground.
        // Lake masks are applied after ranking; they must not re-rank the district.
        public float CoverageWaterDistance(Vector2 p)=>BaseWaterDistance(p);
        public float CoverageHeight(Vector2 p)=>playerLakeBaseTerrain?playerLakeBaseTerrain.GetInterpolatedHeight((p.x-Origin.x)/Side,(p.y-Origin.y)/Side):Height(p);
        public float CoverageSlope(Vector2 p){float dx=(CoverageHeight(p+Vector2.right*2)-CoverageHeight(p-Vector2.right*2))/4,dz=(CoverageHeight(p+Vector2.up*2)-CoverageHeight(p-Vector2.up*2))/4;return Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;}
        void CaptureCoverageGround(float[,] heights){if(playerLakeBaseTerrain)return;playerLakeBaseTerrain=Own(new TerrainData{heightmapResolution=heights.GetLength(0),size=terrainData.size});playerLakeBaseTerrain.SetHeights(0,0,heights);}
        float LakeRoadDistance(Vector2 p){float best=float.PositiveInfinity;for(int i=1;i<Road.Count;i++){var a=Road[i-1];var d=Road[i]-a;float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude):0;best=Mathf.Min(best,Vector2.Distance(p,a+d*t));}return best;}
        /// <returns>Empty on success; user-facing validation message otherwise.</returns>
        public string CreatePlayerLake(IReadOnlyList<Vector2> guide)
        {
            if(IsRegionOverview||View.IsSaving||View.DistrictLoading)return "Wait for the district to finish loading.";
            if(guide.Count>64)return "Use up to 64 markers for one lake.";
            if(!PointWireGeometry.IsSimple(guide))return "The outline crosses itself. Undo a marker and try again.";
            var curve=PointWireGeometry.RoundClosed(guide);if(!PointWireGeometry.IsSimple(curve))return "This outline is too narrow to round safely.";
            float area=PointWireGeometry.Area(curve);if(area<100)return "Draw a larger pond (at least 100 square metres).";if(area>Side*Side*.3f)return "Draw a smaller lake for this first pass.";
            int existing=0;foreach(var b in View.Data.WaterBodies)if(b.playerCreated)existing++;if(existing>=32)return "This region already has 32 added lakes.";
            var min=curve[0];var max=min;float low=float.PositiveInfinity,high=float.NegativeInfinity;
            string Check(Vector2 p){if(!Contains(p)||!Contains(p+Vector2.one*15)||!Contains(p-Vector2.one*15)||!Contains(p+new Vector2(15,-15))||!Contains(p+new Vector2(-15,15)))return "Keep the lake and its banks inside the district.";if(WaterDistance(p)<18)return "Leave space between this lake and existing water.";if(LakeRoadDistance(p)<Baseline.roadWidth+15||View.InCampClearing(p,14))return "Keep the lake clear of roads and camps.";return null;}
            for(int i=0;i<curve.Length;i++){
                var a=curve[i];var b=curve[(i+1)%curve.Length];min=Vector2.Min(min,a);max=Vector2.Max(max,a);int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/4));
                for(int j=0;j<steps;j++){var p=Vector2.Lerp(a,b,j/(float)steps);string error=Check(p);if(error!=null)return error;float h=Height(p);low=Mathf.Min(low,h);high=Mathf.Max(high,h);}
            }
            if(high-low>12)return "Choose flatter ground for a lake; these banks span more than 12 m in height.";
            for(float z=min.y;z<=max.y;z+=8)for(float x=min.x;x<=max.x;x+=8){var p=new Vector2(x,z);if(PointWireGeometry.Contains(curve,p)){string error=Check(p);if(error!=null)return error;}}
            if(low<3.5f)return "Choose higher ground so the pond has room for a basin.";
            if(!Resources.Load<Material>("Water/LakeWater"))return "The lake water material is unavailable.";
            var outline=new Vector2[curve.Length];for(int i=0;i<outline.Length;i++)outline[i]=curve[i]/View.MapSide+Vector2.one*.5f;
            var body=new RegionWaterBody{id="player-lake-"+Guid.NewGuid().ToString("N"),kind=area<5000?WaterBodyKind.Pond:WaterBodyKind.Lake,outline=outline,hasSurfaceLevel=true,surfaceLevelNormalized=(low-.12f)/terrainData.size.y,appearanceProfile="calm-lake",playerCreated=true,playerDepth=6,playerBankWidth=35,playerOuterBankWidth=14};
            PlayerLakeGeometry.ConfigureApprovedProfile(body,View.MapSide);
            var heights=terrainData.GetHeights(0,0,terrainData.heightmapResolution,terrainData.heightmapResolution);CaptureCoverageGround(heights);View.RecordPlayerLake(body);
            PlayerLakeGeometry.Apply(body,heights,Origin,Side,terrainData.size.y,View.MapSide);terrainData.SetHeights(0,0,heights);
            bodyShores.Clear();BuildShoreField();
            var removed=new List<Vector2>();var kept=new List<TreeInstance>();foreach(var tree in CanonicalTreeInstances){var p=Origin+new Vector2(tree.position.x,tree.position.z)*Side;if(View.InPlayerLakeClearing(p))removed.Add(p);else kept.Add(tree);}
            if(removed.Count>0){View.RemoveForestAdditions(removed,.6f);if(MixedForest)MixedForest.RemoveWithin(removed,.6f);fullTreeInstances=kept.ToArray();RefreshEditedForest();}
            BuildPlayerLake(body);DisplayTerrain.Flush();View.ReleaseLoadedMeadow();return null;
        }
        void ApplyPlayerLakeBasins(float[,] heights){foreach(var body in View.Data.WaterBodies)if(body.playerCreated){CaptureCoverageGround(heights);PlayerLakeGeometry.Apply(body,heights,Origin,Side,terrainData.size.y,View.MapSide);}}
        void BuildPlayerLake(RegionWaterBody body){foreach(var existing in GetComponentsInChildren<LakeWaterRenderer>())if(existing.BodyId==body.id)return;var go=new GameObject("Player lake — "+body.id);go.transform.SetParent(transform,false);go.AddComponent<LakeWaterRenderer>().Initialize(this,body);}
        void BuildPlayerLakes(){foreach(var body in View.Data.WaterBodies)if(body.playerCreated&&body.Intersects(new Rect(Origin/View.MapSide+Vector2.one*.5f,Vector2.one*Side/View.MapSide)))BuildPlayerLake(body);}
    }
}
