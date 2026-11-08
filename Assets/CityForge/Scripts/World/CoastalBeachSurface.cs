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

        void PrepareCoastalShore()
        {
            if(IsRegionOverview||View.Coastline)return;
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

        float ShapeEstuaryHeight(Vector2 uv,float height,float originalHeight)
        {
            if(!TryEstuaryDistances(uv,out var distances,out float shoreDistance))return height;
            float coast=distances.x,river=distances.y;
            if(Mathf.Abs(coast)>=RiverBayLayout.MouthRadius)return height;
            float edge=RiverBayLayout.RoundedMouthDistance(coast,river);
            if(edge>.004f)return height;
            float datum=estuaryRiver.surfaceLevelNormalized;
            // Sink the original shallow shelf through the opening. A depth profile
            // measured from the old coast leaves a sand ridge under the water,
            // even when the rounded shoreline looks correct above the surface.
            if(edge<0){
                float patchEdge=Mathf.Min(Mathf.Min(uv.x-estuaryBounds.xMin,estuaryBounds.xMax-uv.x),
                    Mathf.Min(uv.y-estuaryBounds.yMin,estuaryBounds.yMax-uv.y));
                float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.016f,patchEdge))*
                    (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.027f,RiverBayLayout.MouthRadius,Mathf.Abs(coast))));
                float shelfDepth=Mathf.SmoothStep(0,10,Mathf.InverseLerp(0,.019f,shoreDistance));
                // Keep the new shelf flush with the waterline, then taper back
                // to the natural floor beyond the area where sand is visible.
                float bed=datum-shelfDepth/PrototypeRegionData.Height;
                float offshore=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.005f,-coast));
                float floor=Mathf.Lerp(height,originalHeight,offshore);
                height=Mathf.Lerp(height,Mathf.Min(floor,bed),blend);
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
                b.kind==WaterBodyKind.River&&b.centerline?.Length>1&&b.Bounds.Overlaps(area)),
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
                var p=origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*side;
                Sample(z,x,Height(p)-sea);
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
                        // Keep the riverbed upstream, but treat the rounded
                        // corners as coastal shelf.
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
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(wet[z,x])coastalBeachDistances[z,x]=riverOnly!=null&&riverOnly[z,x]?-4*Side:-coastalBeachDistances[z,x];
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
            return CoastalBeachWeight(p,CoastalBeachDistance(p));
        }

        float CoastalBeachWeight(Vector2 p,float d,bool checkFootprint=true)
        {
            if(d>44||d<-Side||(checkFootprint&&!Contains(p)))return 0;
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

        float CoastalBeachDirtWeight(Vector2 p,float beach,float d)
        {
            if(beach<=0)return 0;
            if(d<=0)return 0;
            float dry=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,12,d));
            return beach*dry*(.10f+.25f*Noise(p,12,716))*Mathf.Lerp(.5f,1,1-beach);
        }

        IEnumerator PaintCoastalBeachStages(float[,,] baseMaps,float[,] districtHeights)
        {
            var source=Resources.Load<TerrainLayer>("RiverBeds/RichSand");
            if(!source)throw new InvalidOperationException("Missing approved coastal sand terrain layer.");
            yield return BuildCoastalBeachDistances(districtHeights);
            var layer=Own(Instantiate(source));layer.name="Coastal beach sand";
            layer.diffuseTexture=Own(MatteStreamSoil(source.diffuseTexture));
            layer.specular=new Color(.025f,.025f,.025f);layer.smoothness=0;layer.normalScale=.45f;
            int n=terrainData.alphamapResolution,count=terrainData.terrainLayers.Length;
            coastalBaseAlphamaps=baseMaps;
            var maps=new float[n,n,count+1];
            var weights=new Vector2[n,n];
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
                    if(distance<=0){weights[z,x]=Vector2.right;wetCounts[z]++;continue;}
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
                    if(weight<=0)continue;
                    if(weight>.5f)CoastalBeachPaintedSamples++;
                    float dirt=CoastalBeachDirtWeight(p,weight,distance);
                    if(dirt>.01f)CoastalBeachDirtSamples++;
                    weights[z,x]=new Vector2(weight,dirt);
                }
                if(timer.ElapsedMilliseconds>12){yield return null;timer.Restart();}
            }
            yield return RegionBuildSteps.ComputeRows(n,z=>{
                for(int x=0;x<n;x++){
                    var weight=weights[z,x];
                    if(weight.x==1&&weight.y==0){maps[z,x,count]=1;continue;}
                    for(int i=0;i<count;i++)maps[z,x,i]=baseMaps[z,x,i]*(1-weight.x);
                    maps[z,x,0]+=weight.y;
                    maps[z,x,count]=weight.x-weight.y;
                }
            });
            var layers=new TerrainLayer[count+1];Array.Copy(terrainData.terrainLayers,layers,count);layers[count]=layer;
            terrainData.terrainLayers=layers;terrainData.SetAlphamaps(0,0,maps);CoastalBeachLayer=count;
        }
    }
}
