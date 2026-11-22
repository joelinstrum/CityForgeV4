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

        RegionWaterBody[] coastalOceans=Array.Empty<RegionWaterBody>();
        RegionTerrainLayout coastalLayout;
        bool coastalGradedRivers;

        void PrepareCoastalShore()
        {
            if(IsRegionOverview||View.Coastline)return;
            coastalGradedRivers=Array.Exists(View.Data.WaterBodies,b=>!b.playerChannel&&b.kind==WaterBodyKind.River&&b.channelVersion>=2);
            if(HasOcean())coastalOceans=Array.FindAll(View.Data.WaterBodies,b=>b.kind==WaterBodyKind.Ocean);
            var settings=View.CreationSettings;
            if(settings?.version>=6&&(coastalOceans.Length>0||settings.terrainShape==RegionTerrainShape.RiverAndBay))
                coastalLayout=new RegionTerrainLayout(settings);
            PrepareEstuary();
        }

        // The ocean owns its seabed material, including where a channel crosses
        // the coastline. On the landward side fade the river paint into the beach.
        float RiverSedimentWeight(Vector2 uv)
        {
            if(RoundedEstuaryDistance(uv)<0)return 0;
            if(coastalOceans.Length==0)return 1;
            foreach(var ocean in coastalOceans)if(ocean.Contains(uv))return 0;
            if(coastalLayout==null)return 1;
            return Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,12,coastalLayout.LandDistance(uv)*View.MapSide));
        }

        RegionWaterBody estuaryRiver;
        Rect estuaryBounds;
        const int EstuarySamples=257;
        Vector2[,] estuaryDistances;
        float[,] estuaryShoreDepthDistances;

        void PrepareEstuary()
        {
            if(coastalLayout?.RiverBay==null)return;
            var river=Array.Find(View.Data.WaterBodies,body=>body.id=="tidal-river"&&body.centerline?.Length>1);
            if(river==null)return;
            var layout=coastalLayout.RiverBay;
            var padding=Vector2.one*(RiverBayLayout.MouthRadius+river.halfWidthNormalized);
            for(int i=1;i<river.centerline.Length;i++){
                var previous=river.centerline[i-1];var next=river.centerline[i];
                if((layout.LandDistance(previous)<0)==(layout.LandDistance(next)<0))continue;
                var min=Vector2.Min(previous,next)-padding;
                var max=Vector2.Max(previous,next)+padding;
                if(estuaryRiver!=null){min=Vector2.Min(min,estuaryBounds.min);max=Vector2.Max(max,estuaryBounds.max);}
                estuaryRiver=river;estuaryBounds=new Rect(min,max-min);
            }
            float mapSide=View.MapSide;
            var district=new Rect(Origin/mapSide+Vector2.one*.5f,Vector2.one*Side/mapSide);
            if(estuaryRiver==null||!estuaryBounds.Overlaps(district)){estuaryRiver=null;return;}
            estuaryDistances=new Vector2[EstuarySamples,EstuarySamples];
            int last=EstuarySamples-1;
            for(int z=0;z<EstuarySamples;z++)for(int x=0;x<EstuarySamples;x++){
                var uv=estuaryBounds.min+Vector2.Scale(estuaryBounds.size,new Vector2(x/(float)last,z/(float)last));
                estuaryDistances[z,x]=new Vector2(layout.LandDistance(uv),
                    river.IndexedCenterlineDistance(uv)-river.halfWidthNormalized);
            }
            BuildEstuaryShoreDepthDistances();
        }

        void BuildEstuaryShoreDepthDistances()
        {
            int n=EstuarySamples;
            var shoreline=new Vector2[n,n];
            var signed=new float[n,n];
            var spacing=estuaryBounds.size/(n-1);
            estuaryShoreDepthDistances=new float[n,n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                var d=estuaryDistances[z,x];
                signed[z,x]=RiverBayLayout.RoundedMouthDistance(d.x,d.y);
                estuaryShoreDepthDistances[z,x]=float.PositiveInfinity;
            }

            // Seed the subpixel waterline, then propagate nearest shore points in
            // both directions. Measure diagonals in world units, not grid cells.
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                float here=signed[z,x];
                if(x+1<n)SeedEdge(x,z,x+1,z,here);
                if(z+1<n)SeedEdge(x,z,x,z+1,here);
            }
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                Propagate(x,z,x-1,z);
                Propagate(x,z,x-1,z-1);Propagate(x,z,x,z-1);Propagate(x,z,x+1,z-1);
            }
            for(int z=n-1;z>=0;z--)for(int x=n-1;x>=0;x--){
                Propagate(x,z,x+1,z);
                Propagate(x,z,x-1,z+1);Propagate(x,z,x,z+1);Propagate(x,z,x+1,z+1);
            }
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                estuaryShoreDepthDistances[z,x]=Mathf.Min(.05f,Mathf.Sqrt(estuaryShoreDepthDistances[z,x]));

            void SeedEdge(int x,int z,int nx,int nz,float here)
            {
                float there=signed[nz,nx];
                if((here<0)==(there<0))return;
                float t=here/(here-there);
                var point=new Vector2(Mathf.Lerp(x,nx,t)*spacing.x,Mathf.Lerp(z,nz,t)*spacing.y);
                SetNearest(x,z,point);SetNearest(nx,nz,point);
            }
            void Propagate(int x,int z,int nx,int nz)
            {
                if(nx<0||nz<0||nx>=n||nz>=n)return;
                if(estuaryShoreDepthDistances[nz,nx]==float.PositiveInfinity)return;
                SetNearest(x,z,shoreline[nz,nx]);
            }
            void SetNearest(int x,int z,Vector2 point)
            {
                float square=(new Vector2(x*spacing.x,z*spacing.y)-point).sqrMagnitude;
                if(square>=estuaryShoreDepthDistances[z,x])return;
                estuaryShoreDepthDistances[z,x]=square;
                shoreline[z,x]=point;
            }
        }

        bool TryEstuaryDistances(Vector2 uv,out Vector2 distances,out float shoreDepthDistance)
        {
            distances=default;shoreDepthDistance=0;
            if(estuaryDistances==null||!estuaryBounds.Contains(uv))return false;
            int last=EstuarySamples-1;
            float x=(uv.x-estuaryBounds.xMin)/estuaryBounds.width*last;
            float z=(uv.y-estuaryBounds.yMin)/estuaryBounds.height*last;
            int ix=Mathf.Min(last-1,(int)x),iz=Mathf.Min(last-1,(int)z);
            distances=Vector2.Lerp(Vector2.Lerp(estuaryDistances[iz,ix],estuaryDistances[iz,ix+1],x-ix),
                Vector2.Lerp(estuaryDistances[iz+1,ix],estuaryDistances[iz+1,ix+1],x-ix),z-iz);
            shoreDepthDistance=Mathf.Lerp(Mathf.Lerp(estuaryShoreDepthDistances[iz,ix],estuaryShoreDepthDistances[iz,ix+1],x-ix),
                Mathf.Lerp(estuaryShoreDepthDistances[iz+1,ix],estuaryShoreDepthDistances[iz+1,ix+1],x-ix),z-iz);
            return true;
        }

        internal float RoundedEstuaryDistance(Vector2 uv)
        {
            if(!TryEstuaryDistances(uv,out var distances,out _))return float.PositiveInfinity;
            float coast=distances.x,river=distances.y;
            if(Mathf.Abs(coast)>RiverBayLayout.MouthRadius||Mathf.Abs(coast-river)>=RiverBayLayout.MouthRadius)
                return float.PositiveInfinity;
            return RiverBayLayout.RoundedMouthDistance(coast,river);
        }

        float ShapeEstuaryHeight(Vector2 uv,float height)
        {
            if(!TryEstuaryDistances(uv,out var distances,out float shoreDistance))return height;
            float coast=distances.x,river=distances.y;
            if(Mathf.Abs(coast)>=RiverBayLayout.MouthRadius)return height;
            float edge=RiverBayLayout.RoundedMouthDistance(coast,river);
            if(edge>.004f)return height;
            float datum=estuaryRiver.surfaceLevelNormalized;
            if(edge>=0&&estuaryRiver.bankClearanceMeters>0)
                return Mathf.Max(height,datum+estuaryRiver.bankClearanceMeters/PrototypeRegionData.Height);
            // Sink the original shallow shelf through the opening. A depth profile
            // measured from the old coast leaves a sand ridge under the water,
            // even when the rounded shoreline looks correct above the surface.
            if(edge<0){
                float patchEdge=Mathf.Min(Mathf.Min(uv.x-estuaryBounds.xMin,estuaryBounds.xMax-uv.x),
                    Mathf.Min(uv.y-estuaryBounds.yMin,estuaryBounds.yMax-uv.y));
                float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.016f,patchEdge))*
                    (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.027f,RiverBayLayout.MouthRadius,Mathf.Abs(coast))));
                // Use the rounded waterline for the entire mouth, including
                // both old riverbank shoulders on the landward side of the coast.
                float depthDistance=Mathf.Max(shoreDistance,-edge);
                float shelfDepth=Mathf.SmoothStep(0,10,Mathf.InverseLerp(0,.008f,depthDistance));
                float bed=datum-shelfDepth/PrototypeRegionData.Height;
                height=Mathf.Lerp(height,Mathf.Min(height,bed),blend);
            }
            float cut=Mathf.Min(coast,river)-edge;
            if(cut<=0)return height;
            float depth=Mathf.SmoothStep(0,2.5f,Mathf.InverseLerp(.001f,-.009f,edge))
                *Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.004f,cut));
            float bank=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.001f,.004f,edge));
            float existingDepth=Mathf.Max(0,(datum-height)*PrototypeRegionData.Height);
            float shelf=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,2.5f,existingDepth));
            return Mathf.Lerp(height,Mathf.Min(height,datum-depth/PrototypeRegionData.Height),(1-bank)*shelf);
        }

        IEnumerator BuildCoastalBeachDistances(float[,] districtHeights)
        {
            int n=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Side/2)),256,1024)+1;
            float pixel=Side/(n-1);var heights=new float[n,n];var wet=new bool[n,n];
            float mapSide=View.MapSide;
            var area=new Rect(Origin/mapSide+Vector2.one*.5f,Vector2.one*Side/mapSide);
            var rivers=Array.ConvertAll(Array.FindAll(View.Data.WaterBodies,b=>
                !b.playerChannel&&b.kind==WaterBodyKind.River&&b.centerline?.Length>1&&b.Bounds.Overlaps(area)),
                b=>(body:b,bounds:b.Bounds));
            var riverOnly=rivers.Length==0?null:new bool[n,n];
            coastalBeachDistances=new float[n,n];
            float side=Side,sea=View.SeaLevel,vertical=terrainData.size.y;
            var origin=Origin;
            int last=districtHeights.GetLength(0)-1;
            int stride=last/(n-1);
            if(stride>0){
                yield return RegionBuildSteps.ComputeRows(n,z=>{
                    for(int x=0;x<n;x++)Sample(z,x,districtHeights[z*stride,x*stride]*vertical-sea);
                });
            }else for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                float gx=x/(float)(n-1)*last,gz=z/(float)(n-1)*last;
                int ix=Mathf.Min(last-1,(int)gx),iz=Mathf.Min(last-1,(int)gz);
                float h=Mathf.Lerp(Mathf.Lerp(districtHeights[iz,ix],districtHeights[iz,ix+1],gx-ix),
                    Mathf.Lerp(districtHeights[iz+1,ix],districtHeights[iz+1,ix+1],gx-ix),gz-iz);
                Sample(z,x,h*vertical-sea);
            }

            void Sample(int z,int x,float h)
            {
                heights[z,x]=h;
                if(h<0){
                    var uv=(origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*side)/mapSide+Vector2.one*.5f;
                    foreach(var ocean in coastalOceans)if(ocean.Contains(uv)){wet[z,x]=true;break;}
                    if(!wet[z,x]){
                        foreach(var (body,bounds) in rivers){
                            if(!bounds.Contains(uv)||body.IndexedCenterlineDistance(uv)>=body.halfWidthNormalized)continue;
                            riverOnly[z,x]=wet[z,x]=true;
                            break;
                        }
                        // The connected tidal bed and rounded corners share the coastal
                        // sand floor; graded upstream channels keep their own palette.
                        wet[z,x]|=RoundedEstuaryDistance(uv)<0;
                    }
                }
                coastalBeachDistances[z,x]=side;
            }
            for(int z=1;z<n-1;z++)for(int x=1;x<n-1;x++){
                float nearest=Side,h=heights[z,x];
                void Edge(int nx,int nz){if(wet[z,x]==wet[nz,nx]||(riverOnly!=null&&(riverOnly[z,x]||riverOnly[nz,nx])))return;nearest=Mathf.Min(nearest,pixel*Mathf.Clamp01(Mathf.Abs(h)/Mathf.Max(.00001f,Mathf.Abs(h-heights[nz,nx]))));}
                Edge(x-1,z);Edge(x+1,z);Edge(x,z-1);Edge(x,z+1);
                coastalBeachDistances[z,x]=nearest;
            }
            for(int z=1;z<n;z++)for(int x=1;x<n;x++)coastalBeachDistances[z,x]=Mathf.Min(coastalBeachDistances[z,x],Mathf.Min(coastalBeachDistances[z,x-1]+pixel,Mathf.Min(coastalBeachDistances[z-1,x]+pixel,coastalBeachDistances[z-1,x-1]+pixel*1.41421356f)));
            for(int z=n-2;z>=0;z--)for(int x=n-2;x>=0;x--)coastalBeachDistances[z,x]=Mathf.Min(coastalBeachDistances[z,x],Mathf.Min(coastalBeachDistances[z,x+1]+pixel,Mathf.Min(coastalBeachDistances[z+1,x]+pixel,coastalBeachDistances[z+1,x+1]+pixel*1.41421356f)));
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(wet[z,x])coastalBeachDistances[z,x]=-coastalBeachDistances[z,x];
        }

        float CoastalBeachDistance(Vector2 p)
        {
            if(TerraformShore(p,out float edited))return edited;
            return OriginalCoastalBeachDistance(p);
        }
        float OriginalCoastalBeachDistance(Vector2 p)
        {
            if(coastalBeachDistances==null)return Side;
            int last=coastalBeachDistances.GetLength(0)-1;
            float gx=Mathf.Clamp((p.x-Origin.x)/Side*last,0,last-.001f),gz=Mathf.Clamp((p.y-Origin.y)/Side*last,0,last-.001f);int x=(int)gx,z=(int)gz;
            return Mathf.Lerp(Mathf.Lerp(coastalBeachDistances[z,x],coastalBeachDistances[z,x+1],gx-x),Mathf.Lerp(coastalBeachDistances[z+1,x],coastalBeachDistances[z+1,x+1],gx-x),gz-z);
        }

        public float CoastalBeachWeight(Vector2 p)
        {
            if(coastalBeachDistances==null)return 0;
            float distance = CoastalBeachDistance(p);
            float sand = CoastalBeachWeight(p, distance);
            if (distance < 0 || distance > 10f || !hasDeepHarbor || !Contains(p) || View.InCampClearing(p) || RoadDistance(p) <= Baseline.roadWidth)
                return sand;
            return Mathf.Max(sand, HarborDryCover(p, distance).x);
        }

        // A sea-connected river mouth shares the ocean floor. Graded rivers
        // recover their original bed palette gradually upstream, never at a box edge.
        float CoastalFloorWeight(Vector2 p)
        {
            if(!coastalGradedRivers||coastalLayout==null)return 1;
            float landward=coastalLayout.LandDistance(p/View.MapSide+Vector2.one*.5f)*View.MapSide;
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,96,landward));
        }

        float CoastalBeachWeight(Vector2 p,float d,bool checkFootprint=true)
        {
            if(d>44||d<-Side||(checkFootprint&&!Contains(p)))return 0;
            // The ocean floor stays sand at every depth. Fading the submerged
            // apron back to canonical meadow makes grass show through the water.
            if(d<=0)return Mathf.Lerp(CoastalFloorWeight(p),1,TerraformGroundBlend(p));
            // A narrow textured bank feathers into detailed meadow on sculpted ground.
            float edited=TerraformGroundBlend(p);
            float bank=ShoreMaterialCoverage(p,d,Mathf.Lerp(5,14,Noise(p,43,717)));
            if(edited>=.999f)return bank;
            if(View.InCampClearing(p)||RoadDistance(p)<=Baseline.roadWidth)return 0;
            float width=30+6*Noise(p,100,712);
            // Keep the boundary broad and gradual; grain belongs inside the texture.
            float edge=ShoreMaterialCoverage(p,d,width);
            float high=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,7,Height(p)-View.SeaLevel));
            float steep=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(20,35,MountainSlope(p)));
            return Mathf.Lerp(edge*high*steep*(1-DeepHarborWeight(p)),bank,edited);
        }

        // Irregular MATERIAL coverage only: never displace terrain or its waterline.
        float ShoreMaterialCoverage(Vector2 p,float distance,float width)
        {
            float patches=.65f*Noise(p,7,721)+.35f*Noise(p,21,722);
            patches=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.32f,.68f,patches));
            float breakup=(patches-.5f)*.65f*Mathf.SmoothStep(0,1,distance/2);
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.85f,distance/width+breakup));
        }

        float CoastalBeachDirtWeight(Vector2 p,float beach,float d)
        {
            if(beach<=0)return 0;
            if(d<=0)return 0;
            float dry=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,12,d));
            // Reuse the approved dirt grain, with a broad continuous blend rather
            // than a flat sand strip. Keep the submerged floor sand.
            float edited=TerraformGroundBlend(p);
            float soil=Mathf.Lerp(dry*(.16f+.22f*Noise(p,12,716)),dry*(.18f+.26f*Noise(p,8,718)),edited);
            return beach*soil;
        }

        int[,] coastalDefaultSources;
        internal Vector2 TerraformGroundSource(Vector2 p)
        {
            if(coastalBaseAlphamaps==null||TerraformGroundBlend(p)<=0)return p;
            int n=coastalBaseAlphamaps.GetLength(0);
            int localX=Mathf.Clamp(Mathf.RoundToInt((p.x-Origin.x)/Side*(n-1)),0,n-1),localZ=Mathf.Clamp(Mathf.RoundToInt((p.y-Origin.y)/Side*(n-1)),0,n-1);
            float localGrass=0;for(int i=1;i<Mathf.Min(View.RiverBedVersion==0?5:4,coastalBaseAlphamaps.GetLength(2));i++)localGrass+=coastalBaseAlphamaps[localZ,localX,i];
            // Beach painting scales down the pre-existing grass but does not erase
            // its spatial pattern. Recover that exact world-position mixture first.
            if(localGrass>.0001f)return p;
            if(coastalDefaultSources==null){
                int m=Mathf.Min(n,257);
                coastalDefaultSources=new int[m,m];
                // Canonical dry meadow samples, outside the old beach. Build once;
                // only coverage weights move, never the source textures or their UVs.
                for(int z=0;z<m;z++)for(int x=0;x<m;x++){
                    int sx=Mathf.RoundToInt(x/(float)(m-1)*(n-1)),sz=Mathf.RoundToInt(z/(float)(m-1)*(n-1));
                    var q=Origin+new Vector2(sx/(float)(n-1),sz/(float)(n-1))*Side;
                    float grass=coastalBaseAlphamaps[sz,sx,1]+coastalBaseAlphamaps[sz,sx,2]+coastalBaseAlphamaps[sz,sx,3];
                    if(View.RiverBedVersion==0&&coastalBaseAlphamaps.GetLength(2)>4)grass+=coastalBaseAlphamaps[sz,sx,4];
                    coastalDefaultSources[z,x]=Contains(q)&&TerraformGroundBlend(q)<.001f&&OriginalCoastalBeachDistance(q)>40&&grass>.75f&&coastalBaseAlphamaps[sz,sx,0]<.18f&&(!Geography||Geography.IsDryGrassReference(q))?sz*n+sx:-1;
                }
                void Relax(int x,int z,int xx,int zz){
                    if(xx<0||zz<0||xx>=m||zz>=m)return;int source=coastalDefaultSources[zz,xx];if(source<0)return;
                    int old=coastalDefaultSources[z,x];
                    long Distance(int key){long dx=key%n-Mathf.RoundToInt(x/(float)(m-1)*(n-1)),dz=key/n-Mathf.RoundToInt(z/(float)(m-1)*(n-1));return dx*dx+dz*dz;}
                    if(old<0||Distance(source)<Distance(old))coastalDefaultSources[z,x]=source;
                }
                for(int z=0;z<m;z++)for(int x=0;x<m;x++){Relax(x,z,x-1,z);Relax(x,z,x-1,z-1);Relax(x,z,x,z-1);Relax(x,z,x+1,z-1);}
                for(int z=m-1;z>=0;z--)for(int x=m-1;x>=0;x--){Relax(x,z,x+1,z);Relax(x,z,x-1,z+1);Relax(x,z,x,z+1);Relax(x,z,x+1,z+1);}
            }
            int size=coastalDefaultSources.GetLength(0);
            int px=Mathf.Clamp(Mathf.RoundToInt((p.x-Origin.x)/Side*(size-1)),0,size-1),pz=Mathf.Clamp(Mathf.RoundToInt((p.y-Origin.y)/Side*(size-1)),0,size-1);
            int key=coastalDefaultSources[pz,px];if(key<0)return p;
            var nearest=Origin+new Vector2(key%n/(float)(n-1),key/n/(float)(n-1))*Side;
            // Continue a two-dimensional swath of the existing meadow. Reusing
            // only the nearest boundary pixel extrudes one row into a flat band.
            var reflected=nearest*2-p;
            int rx=Mathf.Clamp(Mathf.RoundToInt((reflected.x-Origin.x)/Side*(size-1)),0,size-1),rz=Mathf.Clamp(Mathf.RoundToInt((reflected.y-Origin.y)/Side*(size-1)),0,size-1);
            int donor=coastalDefaultSources[rz,rx];
            return donor<0?nearest:Origin+new Vector2(donor%n/(float)(n-1),donor/n/(float)(n-1))*Side;
        }

        // Reuse the original ground weights and existing layers; only repaint edited coast.
        void RefreshTerraformBeach(Rect area)
        {
            if(coastalBaseAlphamaps==null||CoastalBeachLayer<0)return;
            int n=terrainData.alphamapResolution,count=CoastalBeachLayer,layers=terrainData.alphamapLayers;
            var lo=Vector2Int.Max(Vector2Int.zero,Vector2Int.FloorToInt((area.min-Origin)/Side*(n-1)));
            var hi=Vector2Int.Min(Vector2Int.one*(n-1),Vector2Int.CeilToInt((area.max-Origin)/Side*(n-1)));
            if(hi.x<lo.x||hi.y<lo.y)return;
            var maps=terrainData.GetAlphamaps(lo.x,lo.y,hi.x-lo.x+1,hi.y-lo.y+1);
            for(int z=lo.y;z<=hi.y;z++)for(int x=lo.x;x<=hi.x;x++){
                var p=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                if(!View.InActiveDistrict(p)||!TerraformShore(p,out float distance))continue;
                float beach=CoastalBeachWeight(p,distance),dirt=CoastalBeachDirtWeight(p,beach,distance);
                var cover=Vector2.zero;
                if(layers>count+1){
                    if(distance<=0&&distance>=-12)cover.x=HarborWetRock(p,distance);
                    else if(distance>0&&distance<=10&&!View.InCampClearing(p)&&RoadDistance(p)>Baseline.roadWidth)cover=HarborDryCover(p,distance);
                    if(layers<=count+2)cover.y=0;
                }
                float edited=distance>0?TerraformGroundBlend(p):0;
                cover*=1-edited;
                float weight=Mathf.Clamp01(beach+cover.x+cover.y),sum=0;
                for(int i=0;i<layers;i++)maps[z-lo.y,x-lo.x,i]=0;
                for(int i=0;i<count;i++)maps[z-lo.y,x-lo.x,i]=coastalBaseAlphamaps[z,x,i]*(1-weight);
                if(edited>0){
                    // Extend the actual approved ground mixture, including its coarse
                    // fifth layer. Texture assets, tiling and world coordinates stay shared.
                    var source=TerraformGroundSource(p);
                    int sx=Mathf.Clamp(Mathf.RoundToInt((source.x-Origin.x)/Side*(n-1)),0,n-1);
                    int sz=Mathf.Clamp(Mathf.RoundToInt((source.y-Origin.y)/Side*(n-1)),0,n-1);
                    int grassLayers=Mathf.Min(View.RiverBedVersion==0?5:4,count);float grassMass=0;
                    for(int i=1;i<grassLayers;i++)grassMass+=coastalBaseAlphamaps[sz,sx,i];
                    for(int i=0;i<count;i++){
                        float native=grassMass>.00001f?(i>0&&i<grassLayers?coastalBaseAlphamaps[sz,sx,i]/grassMass:0):(i==1?1:0);
                        maps[z-lo.y,x-lo.x,i]=Mathf.Lerp(maps[z-lo.y,x-lo.x,i],native*(1-weight),edited);
                    }
                }
                maps[z-lo.y,x-lo.x,0]+=dirt;
                maps[z-lo.y,x-lo.x,count]=Mathf.Max(0,weight-dirt-cover.x-cover.y);
                if(layers>count+1)maps[z-lo.y,x-lo.x,count+1]=cover.x;
                if(layers>count+2)maps[z-lo.y,x-lo.x,count+2]=cover.y;
                for(int i=0;i<layers;i++)sum+=maps[z-lo.y,x-lo.x,i];
                if(sum>0)for(int i=0;i<layers;i++)maps[z-lo.y,x-lo.x,i]/=sum;
            }
            terrainData.SetAlphamaps(lo.x,lo.y,maps);
        }

        IEnumerator PaintCoastalBeachStages(float[,,] baseMaps,float[,] districtHeights)
        {
            var source=Resources.Load<TerrainLayer>("RiverBeds/RichSand");
            if(!source)throw new InvalidOperationException("Missing approved coastal sand terrain layer.");
            if (coastalBeachDistances == null) yield return BuildCoastalBeachDistances(districtHeights);
            var layer=Own(Instantiate(source));layer.name="Coastal beach sand";
            layer.diffuseTexture=Own(MatteStreamSoil(source.diffuseTexture));
            layer.specular=new Color(.025f,.025f,.025f);layer.smoothness=0;layer.normalScale=.65f;layer.tileSize=new Vector2(5,5);
            int n=terrainData.alphamapResolution,count=terrainData.terrainLayers.Length;
            coastalBaseAlphamaps=baseMaps;
            var harborLayer = CreateHarborRockLayer();
            var harborSoilLayer = harborLayer ? CreateHarborSoilLayer() : null;
            bool hasHarborSoil = harborSoilLayer != null;
            var maps=new float[n,n,count+1+(harborLayer ? 1 : 0)+(hasHarborSoil ? 1 : 0)];
            var weights=new Vector2[n,n];
            var harborCover = harborLayer ? new Vector2[n,n] : null;
            int last=coastalBeachDistances.GetLength(0)-1;
            var coordinates=new float[n];
            for(int i=0;i<n;i++)coordinates[i]=Mathf.Min(i/(float)(n-1)*last,last-.001f);
            float side=Side;
            var origin=Origin;
            bool[] footprint=null;
            int[] columnCells=null,rowCells=null;
            if(needsTerrainHoles){
                int grid=View.GridSide;
                footprint=new bool[grid*grid];
                foreach(int member in View.ActiveDistrictMembers)footprint[member]=true;
                columnCells=new int[n];rowCells=new int[n];
                for(int i=0;i<n;i++){
                    float offset=i/(float)(n-1)*side;
                    columnCells[i]=View.DistrictAtWorld(new Vector3(origin.x+offset,0,origin.y))%grid;
                    int cell=View.DistrictAtWorld(new Vector3(origin.x,0,origin.y+offset));
                    rowCells[i]=cell<0?-1:cell/grid*grid;
                }
            }
            var wetCounts=new int[n];
            var drySamples=new System.Collections.Generic.List<(int x,float distance)>[n];
            yield return RegionBuildSteps.ComputeRows(n,z=>{
                float gz=coordinates[z];int iz=(int)gz;
                for(int x=0;x<n;x++){
                    if(footprint!=null&&(rowCells[z]<0||columnCells[x]<0||!footprint[rowCells[z]+columnCells[x]]))continue;
                    float gx=coordinates[x];int ix=(int)gx;
                    float distance=Mathf.Lerp(
                        Mathf.Lerp(coastalBeachDistances[iz,ix],coastalBeachDistances[iz,ix+1],gx-ix),
                        Mathf.Lerp(coastalBeachDistances[iz+1,ix],coastalBeachDistances[iz+1,ix+1],gx-ix),gz-iz);
                    if(distance>44||distance<-side)continue;
                    if(distance<=0)
                    {
                        weights[z,x]=Vector2.right*CoastalFloorWeight(origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*side);
                        wetCounts[z]++;
                        if (harborCover != null && distance >= -12)
                            harborCover[z,x] = new Vector2(HarborWetRock(origin +
                                new Vector2(x/(float)(n-1),z/(float)(n-1))*side, distance), 0);
                        continue;
                    }
                    (drySamples[z]??=new System.Collections.Generic.List<(int,float)>()).Add((x,distance));
                }
            });

            // Only the dry shoreline strip needs Unity terrain, road and camp queries.
            var timer=System.Diagnostics.Stopwatch.StartNew();
            for(int z=0;z<n;z++){
                CoastalBeachPaintedSamples+=wetCounts[z];
                if(drySamples[z]==null)continue;
                foreach(var (x,distance) in drySamples[z]){
                    var p=origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*side;
                    float weight=CoastalBeachWeight(p,distance,false);
                    if(weight>.5f)CoastalBeachPaintedSamples++;
                    float dirt=CoastalBeachDirtWeight(p,weight,distance);
                    if(dirt>.01f)CoastalBeachDirtSamples++;
                    if (harborCover != null && distance <= 10f && !View.InCampClearing(p) && RoadDistance(p) > Baseline.roadWidth)
                    {
                        var cover = HarborDryCover(p, distance);
                        if (!hasHarborSoil) cover.y = 0;
                        harborCover[z,x] = cover;
                        weight += cover.x + cover.y;
                    }
                    weights[z,x]=new Vector2(weight,dirt);
                }
                if(timer.ElapsedMilliseconds>12){yield return null;timer.Restart();}
            }
            yield return RegionBuildSteps.ComputeRows(n,z=>{
                for(int x=0;x<n;x++){
                    var weight=weights[z,x];
                    var cover = harborCover == null ? Vector2.zero : harborCover[z,x];
                    if(weight.x==1&&weight.y==0&&cover == Vector2.zero){maps[z,x,count]=1;continue;}
                    for(int i=0;i<count;i++)maps[z,x,i]=baseMaps[z,x,i]*(1-weight.x);
                    maps[z,x,0]+=weight.y;
                    maps[z,x,count]=Mathf.Max(0, weight.x-weight.y-cover.x-cover.y);
                    if (harborCover != null) maps[z,x,count+1]=cover.x;
                    if (hasHarborSoil) maps[z,x,count+2]=cover.y;
                }
            });
            var layers=new TerrainLayer[maps.GetLength(2)];Array.Copy(terrainData.terrainLayers,layers,count);layers[count]=layer;
            if (harborLayer) layers[count+1]=harborLayer;
            if (hasHarborSoil) layers[count+2]=harborSoilLayer;
            terrainData.terrainLayers=layers;terrainData.SetAlphamaps(0,0,maps);CoastalBeachLayer=count;
        }
    }
}
