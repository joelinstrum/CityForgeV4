using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Terrain-derived cartography. Exaggerated relief is presentation only; district heights stay authoritative.</summary>
    public sealed class RegionReliefMap : MonoBehaviour
    {
        Mesh slabMesh;Material slabMaterial;
        Vector3[] slabRim;
        float slabBottom;
        Mesh mesh;Material material;Texture2D atlas;MeshCollider picker;PrototypeRegionView view;
        const int Samples=513;
        Vector3[] surfaceVertices;
        internal bool SurfaceReady => surfaceVertices != null;
        public void Build(PrototypeRegionView source)=>RegionBuildSteps.Complete(BuildStages(source));
        public System.Collections.IEnumerator BuildStages(PrototypeRegionView source,System.Action<string,float> progress=null)
        {
            progress?.Invoke("Sampling regional relief",0);yield return null;
            view=source;var layout=source.CreationSettings?.version>=6?new RegionTerrainLayout(source.CreationSettings):null;int n=Samples;var vertices=new Vector3[n*n];var uv=new Vector2[n*n];var triangles=new int[(n-1)*(n-1)*6];
            var snowPeaks=MountainSummitSnow.Applies(source)?MountainSummitSnow.Select(source.Data,MountainBorderVariation.CapCount(source)):System.Array.Empty<MountainSummitSnow.Summit>();
            float height=source.GetComponentInChildren<Terrain>().terrainData.size.y;float sea=source.SeaLevel;
            bool continuousChannels=RegionalRiverNetwork.HasContinuousChannels(source.Data);
            var heightCache=new RegionalReliefHeightCache(source.Data);
            yield return heightCache.BuildOverviewStages();
            float H(float u,float v)=>heightCache.Sample(u,v)*height;
            var bodies=source.Data.WaterBodies??System.Array.Empty<RegionWaterBody>();
            var standingWater=System.Array.FindAll(bodies,body=>body.kind==WaterBodyKind.Lake||body.kind==WaterBodyKind.Pond);
            bool hasStreams=System.Array.Exists(bodies,body=>body.isStream);
            bool groundFollowing=System.Array.Exists(bodies,body=>!body.isStream&&body.channelVersion>=2);
            bool coastal=layout!=null&&layout.HasOcean;
            const float riverFalloff=.020f;
            var rivers=System.Array.ConvertAll(System.Array.FindAll(bodies,body=>
                body.kind==WaterBodyKind.River&&!body.isStream&&body.centerline?.Length>1),body=>{
                    var bounds=body.Bounds;
                    float reach=body.id=="tidal-river"?RiverBayLayout.MouthRadius:riverFalloff;
                    bounds.min-=Vector2.one*reach;
                    bounds.max+=Vector2.one*reach;
                    return (body,bounds);
                });
            float RiverDistance(Vector2 point,out RegionWaterBody nearest)
            {
                nearest=null;
                float closest=float.PositiveInfinity,second=float.PositiveInfinity;
                RegionWaterBody secondBody=null;
                foreach(var (body,bounds) in rivers){
                    if(!bounds.Contains(point))continue;
                    float edge=body.IndexedCenterlineDistance(point)-body.halfWidthNormalized;
                    if(edge<closest){second=closest;secondBody=nearest;closest=edge;nearest=body;}
                    else if(edge<second){second=edge;secondBody=body;}
                }
                if(secondBody==null)return closest;
                float radius=Mathf.Clamp(2*Mathf.Min(nearest.halfWidthNormalized,secondBody.halfWidthNormalized),.001f,.018f);
                return RoundJoin(closest,second,radius);
            }
            float CoastJoin(float coast,float river,RegionWaterBody nearest)
            {
                if(!coastal||nearest==null)return Mathf.Min(coast,river);
                float radius=nearest.id=="tidal-river"?RiverBayLayout.MouthRadius:
                    Mathf.Clamp(2*nearest.halfWidthNormalized,.001f,.018f);
                return RoundJoin(coast,river,radius);
            }
            static float RoundJoin(float a,float b,float radius)
            {
                float blend=Mathf.Max(0,1-Mathf.Abs(a-b)/radius);
                return Mathf.Min(a,b)-radius*.25f*blend*blend;
            }
            float Surface(Vector2 point,float h,float river,RegionWaterBody nearest)
            {
                foreach(var body in standingWater)
                    if(body.Bounds.Contains(point)&&body.Contains(point))
                    {
                        float level=body.hasSurfaceLevel?body.surfaceLevelNormalized*height:sea;
                        return body.playerCreated?level:Mathf.Max(h,level);
                    }
                if(river<0&&nearest!=null){
                    if(nearest.playerChannel)return PlayerChannelGeometry.Level(nearest,(point-Vector2.one*.5f)*source.MapSide,source.MapSide);
                    if(groundFollowing&&nearest.channelVersion>=2)
                        return Mathf.Max(sea,RegionalRiverNetwork.GroundWaterLevel(source.Data,point.x,point.y));
                    return sea;
                }
                return Mathf.Max(h,sea);
            }
            var shoreLift=coastal||rivers.Length>0?new float[n,n]:null;

            static float BankProfile(float edge,float riseWidth,float shoulder,float falloff)
            {
                if(edge<=0||edge>=falloff)return 0;
                float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,riseWidth,edge));
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(shoulder,falloff,edge));
                return rise*fade;
            }
            float ShadedHeight(float u,float v)=>H(u,v)+(shoreLift==null?0:SmoothRiverSurface.Sample(shoreLift,u,v));
            progress?.Invoke("Building regional relief mesh",.20f);yield return null;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                int i=z*n+x;float u=x/(float)(n-1),v=z/(float)(n-1);
                var point=new Vector2(u,v);
                float coast=coastal?layout.LandDistance(point):float.PositiveInfinity;
                float riverEdge=RiverDistance(point,out var nearest);
                float waterEdge=CoastJoin(coast,riverEdge,nearest);
                float h=Surface(point,H(u,v),riverEdge,nearest);
                if(shoreLift!=null){
                    if(coast<0||(riverEdge>=0&&waterEdge<0))h=sea;
                    else if(h>sea){
                        float coastBank=nearest!=null&&Mathf.Abs(coast-riverEdge)<RiverBayLayout.MouthRadius?waterEdge:coast;
                        float coastProfile=BankProfile(coastBank,.0065f,.009f,.035f);
                        float riverProfile=.55f*BankProfile(riverEdge,.004f,.0055f,riverFalloff);
                        float profile=Mathf.Max(coastProfile,riverProfile);
                        if(profile>0&&riverEdge>=0){
                            float variation=Mathf.PerlinNoise(source.Data.Seed%997+u*33,v*33);
                            float lift=source.MapSide*.0025f*Mathf.Lerp(.36f,1.12f,variation)*profile;
                            // Keep this rise in the display mesh and its hillshade.
                            shoreLift[z,x]=lift;
                            h+=lift;
                        }
                    }
                }
                vertices[i]=new Vector3((u-.5f)*source.MapSide,sea+(h-sea)*1.68f+1,(v-.5f)*source.MapSide);uv[i]=new Vector2(u,v);
            }
            progress?.Invoke("Shading regional relief atlas",.30f);yield return null;
            const int pixels=1025;var colors=new Color[pixels*pixels];float seed=source.Data.Seed%997;
            var data=source.Data;
            float mapSide=source.MapSide;
            bool riverRecipe=source.CreationSettings?.version>=5,waterless=source.Waterless;
            bool tropical=source.Climate==RegionClimate.Tropical;
            void ShadeRow(int z)
            {
            for(int x=0;x<pixels;x++){
                float u=x/(float)(pixels-1),v=z/(float)(pixels-1),h=H(u,v);
                var point=new Vector2(u,v);
                float coastalDistance=coastal?layout.LandDistance(point):float.PositiveInfinity;
                float riverDistance=RiverDistance(point,out var nearest);
                float bank=CoastJoin(coastalDistance,riverDistance,nearest);
                if(!continuousChannels&&!hasStreams)
                    bank=Mathf.Min(bank,SmoothRiverSurface.Sample(data.WaterDistance,u,v)/PrototypeRegionData.Side);
                bool ocean=coastal?coastalDistance<0:IsOcean(bodies,point);
                bool water=!waterless&&(bank<0||ocean||(!riverRecipe&&!hasStreams&&h<sea+.1f));
                foreach(var body in standingWater)if(body.Bounds.Contains(point)&&body.Contains(point)){water=true;break;}
                if(water){
                    float offshore=coastal?Mathf.Max(0,-coastalDistance):Mathf.Max(0,-bank);
                    if(tropical&&ocean&&offshore>0)
                        colors[z*pixels+x]=TropicalOceanDepth.MapColor(offshore);
                    else{
                        float depth=Mathf.Max(0,sea-h);
                        // Grade rivers across their width and coastlines over a broader shelf.
                        float channelDistance=bank>-.1f?Mathf.Max(0,-bank):0;
                        float mouthReach=coastal&&nearest!=null&&Mathf.Abs(coastalDistance)<RiverBayLayout.MouthRadius
                            ?Mathf.Max(0,-bank):0;
                        float riverShallow=1-Mathf.SmoothStep(0,1,
                            Mathf.Max(channelDistance/.012f,depth/7f));
                        float coastReach=coastal?Mathf.Max(offshore,mouthReach)/.038f:depth/11f;
                        float coastShallow=1-Mathf.SmoothStep(0,1,coastReach);
                        var river=Color.Lerp(new Color(.282f,.409f,.549f),
                            new Color(.345f,.488f,.598f),riverShallow);
                        var shelf=Color.Lerp(new Color(.252f,.389f,.548f),
                            new Color(.371f,.531f,.640f),coastShallow);
                        // Feather the two palettes through the estuary instead of drawing a color seam.
                        float oceanMix=coastal?1-Mathf.SmoothStep(0,1,
                            Mathf.InverseLerp(-.007f,.018f,coastalDistance)):ocean?1:0;
                        colors[z*pixels+x]=Color.Lerp(river,shelf,oceanMix);
                    }
                    continue;
                }
                var ecology=data.Biomes?.At(new Vector2(u,v));
                float woodland=ecology?.woodland??.4f;
                float patch=Mathf.PerlinNoise(seed+u*17,seed+v*17);
                float cover=Mathf.Clamp01(woodland*.75f+patch*.20f);
                var grass=Color.Lerp(new Color(.418f,.445f,.308f),new Color(.366f,.406f,.269f),cover);
                float dry=Mathf.Clamp01((ecology?.dryness??.2f)*.75f+patch*.25f);
                grass=Color.Lerp(grass,new Color(.47f,.445f,.329f),dry*.30f);
                float dx=(ShadedHeight(Mathf.Clamp01(u+1/512f),v)-ShadedHeight(Mathf.Clamp01(u-1/512f),v))/(mapSide/256f);
                float dz=(ShadedHeight(u,Mathf.Clamp01(v+1/512f))-ShadedHeight(u,Mathf.Clamp01(v-1/512f)))/(mapSide/256f);
                float slope=new Vector2(dx,dz).magnitude;
                float rock=Mathf.SmoothStep(0,1,Mathf.Max(Mathf.InverseLerp(.105f,.32f,h/height),Mathf.InverseLerp(.16f,.59f,slope)));
                var stone=Color.Lerp(new Color(.59f,.49f,.365f),new Color(.73f,.62f,.46f),Mathf.Clamp01(h/height*1.7f));
                // Shade fine drainage folds from curvature of the actual shared heightfield.
                float lap=H(Mathf.Clamp01(u+1/512f),v)+H(Mathf.Clamp01(u-1/512f),v)+H(u,Mathf.Clamp01(v+1/512f))+H(u,Mathf.Clamp01(v-1/512f))-4*h;
                // Warmer exposed uplands distinguish ridges from the green valley floors.
                float upland=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.065f,.24f,h/height));
                grass=Color.Lerp(grass,new Color(.51f,.462f,.34f),upland*.42f);
                var color=Color.Lerp(grass,stone,rock);
                color=Color.Lerp(color,new Color(.94f,.96f,.98f),MountainSummitSnow.Cover(snowPeaks,new Vector2(u,v),h/height,data.Seed));
                float coast=coastal?Mathf.Abs(coastalDistance):OceanEdgeDistance(bodies,point);
                // A narrow dry shoreline accent at region scale, shared by riverbanks and coasts.
                float shore=Mathf.Min(coast,bank);
                float shoreWidth=coast<.012f?.004f+.0017f*patch:.0032f+.0011f*patch;
                if(!waterless&&shore>=0&&shore<shoreWidth)
                    color=Color.Lerp(color,new Color(.80f,.735f,.64f),.88f*(1-Mathf.SmoothStep(0,1,shore/shoreWidth)));
                color*=Mathf.Clamp(1-lap*.008f,.94f,1.04f);
                var normal=new Vector3(-dx*2.4f,1,-dz*2.4f).normalized;float light=Vector3.Dot(normal,new Vector3(-.55f,.75f,-.37f).normalized);
                float shadow=1;
                for(int k=1;k<=10;k++){
                    float offset=k*3/512f;
                    float rise=H(Mathf.Clamp01(u-offset*.82f),Mathf.Clamp01(v-offset*.55f))-h;
                    float slopeToLight=rise/(mapSide*offset);
                    shadow=Mathf.Min(shadow,Mathf.Lerp(1,.61f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.53f,slopeToLight))));
                    if(shadow<=.61f)break;
                }
                float shade=(.37f+.85f*Mathf.Clamp01(light))*shadow;
                float fine=.90f+.14f*Mathf.PerlinNoise(seed+u*205,v*205)+.06f*Mathf.PerlinNoise(u*487,v*487+seed);color*=shade*fine;color.a=1;colors[z*pixels+x]=color;
            }
            }
            yield return RegionBuildSteps.ComputeRows(pixels,ShadeRow);
            progress?.Invoke("Uploading regional relief",.95f);yield return null;
            atlas=new Texture2D(pixels,pixels,TextureFormat.RGBA32,true){name="Regional shaded relief atlas",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};atlas.SetPixels(colors);atlas.Apply(true,true);
            int t=0;for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++){int i=z*n+x;triangles[t++]=i;triangles[t++]=i+n;triangles[t++]=i+1;triangles[t++]=i+1;triangles[t++]=i+n;triangles[t++]=i+n+1;}
            mesh=new Mesh{name="Regional relief — 513 samples",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;material=new Material(Resources.Load<Shader>("Region/RegionalRelief"));material.SetFloat("_Grid",source.GridSide);material.SetTexture("_BaseMap",atlas);
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            surfaceVertices = vertices;
            picker=gameObject.AddComponent<MeshCollider>();picker.sharedMesh=mesh;
            BuildSlab(vertices,n,source.MapSide);
        }
        static bool IsOcean(RegionWaterBody[] bodies,Vector2 p){foreach(var body in bodies)if(body.kind==WaterBodyKind.Ocean&&body.Bounds.Contains(p)&&body.Contains(p))return true;return false;}
        // Only semantic ocean outlines receive a beach. Map boundary edges are not shorelines.
        static float OceanEdgeDistance(RegionWaterBody[] bodies,Vector2 p)
        {
            float best=float.PositiveInfinity;if(bodies==null)return best;
            foreach(var body in bodies){if(body.kind!=WaterBodyKind.Ocean||body.outline==null)continue;
                foreach(var points in body.Contours)
                for(int i=0;i<points.Length;i++){var a=points[i];var b=points[(i+1)%points.Length];
                    if((a.x==0&&b.x==0)||(a.x==1&&b.x==1)||(a.y==0&&b.y==0)||(a.y==1&&b.y==1))continue;
                    var delta=b-a;if(delta.sqrMagnitude==0)continue;
                    float t=Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude);
                    best=Mathf.Min(best,Vector2.Distance(p,a+delta*t));
                }
            }return best;
        }
        void BuildSlab(Vector3[] top,int n,float side)
        {
            int edge=n-1,count=edge*4;
            slabRim=new Vector3[count];
            for(int i=0;i<edge;i++){
                slabRim[i]=top[i];
                slabRim[edge+i]=top[i*n+edge];
                slabRim[2*edge+i]=top[edge*n+edge-i];
                slabRim[3*edge+i]=top[(edge-i)*n];
            }
            slabBottom=mesh.bounds.min.y-side*.031f;
            var vertices=new Vector3[count*4];var colors=new Color[vertices.Length];var indices=new int[count*6];
            for(int i=0;i<count;i++){int k=i*4,t=i*6;var a=slabRim[i];var b=slabRim[(i+1)%count];
                vertices[k]=a;vertices[k+1]=b;vertices[k+2]=new Vector3(a.x,slabBottom,a.z);vertices[k+3]=new Vector3(b.x,slabBottom,b.z);
                float face=1-.05f*(i/(n-1));var upper=new Color(.48f,.47f,.47f)*face;var lower=new Color(.33f,.33f,.35f)*face;upper.a=lower.a=1;
                colors[k]=colors[k+1]=upper;colors[k+2]=colors[k+3]=lower;
                indices[t]=k;indices[t+1]=k+1;indices[t+2]=k+2;indices[t+3]=k+1;indices[t+4]=k+3;indices[t+5]=k+2;
            }
            slabMesh=new Mesh{name="Regional slab sides"};slabMesh.vertices=vertices;slabMesh.colors=colors;slabMesh.triangles=indices;slabMesh.RecalculateBounds();
            var slab=new GameObject("Regional slab");slab.transform.SetParent(transform,false);slab.AddComponent<MeshFilter>().sharedMesh=slabMesh;
            slabMaterial=new Material(Resources.Load<Shader>("Region/RegionalSlab"));var renderer=slab.AddComponent<MeshRenderer>();renderer.sharedMaterial=slabMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        public void SetDistrictGroups(Texture2D ids){if(material){material.SetTexture("_DistrictIds",ids);material.SetFloat("_Grouped",1);}}
        internal bool HitsSlabSide(Ray ray)
        {
            if(slabRim==null)return false;
            var origin=transform.InverseTransformPoint(ray.origin);
            var direction=transform.InverseTransformVector(ray.direction);
            int edge=slabRim.Length/4;
            float half=view.MapSide/2;
            float nearest=float.PositiveInfinity;
            CheckFace(-half,origin.z,direction.z,origin.x,direction.x,0);
            CheckFace(half,origin.x,direction.x,origin.z,direction.z,1);
            CheckFace(half,origin.z,direction.z,-origin.x,-direction.x,2);
            CheckFace(-half,origin.x,direction.x,-origin.z,-direction.z,3);
            return !float.IsPositiveInfinity(nearest)&&!picker.Raycast(ray,out _,nearest);

            void CheckFace(float plane,float start,float axis,float across,float acrossAxis,int face)
            {
                // Only the exterior face is visible. The far wall cannot steal a top click.
                if((plane<0?axis:-axis)<=.000001f)return;
                float distance=(plane-start)/axis;
                if(distance<0||distance>=nearest)return;
                float along=(across+acrossAxis*distance+half)/(2*half)*edge;
                if(along<0||along>edge)return;
                int segment=Mathf.Min(edge-1,(int)along);
                int i=face*edge+segment;
                float top=Mathf.Lerp(slabRim[i].y,slabRim[(i+1)%slabRim.Length].y,along-segment);
                float y=origin.y+direction.y*distance;
                if(y>=slabBottom&&y<top)nearest=distance;
            }
        }

        internal Vector3 SurfacePoint(Vector2 position)
        {
            float x = Mathf.Clamp01(position.x) * (Samples - 1);
            float z = Mathf.Clamp01(position.y) * (Samples - 1);
            int column = Mathf.Min((int)x, Samples - 2);
            int row = Mathf.Min((int)z, Samples - 2);
            int index = row * Samples + column;
            float u = x - column, v = z - row;
            var a = surfaceVertices[index];
            var b = surfaceVertices[index + 1];
            var c = surfaceVertices[index + Samples];
            var d = surfaceVertices[index + Samples + 1];
            return u + v <= 1 ? a + (b - a) * u + (c - a) * v
                : d + (c - d) * (1 - u) + (b - d) * (1 - v);
        }

        public bool TryPick(Ray ray, out int district) => TryPick(ray, out district, out _);
        internal bool TryPick(Ray ray, out int district, out Vector3 point)
        {
            district = -1;
            point = default;
            if (!picker || !picker.Raycast(ray, out var hit, 1000000)) return false;
            point = hit.point;
            district = view.DistrictAtWorld(point);
            return district >= 0;
        }
        void OnDestroy(){if(slabMesh)Destroy(slabMesh);if(slabMaterial)Destroy(slabMaterial);if(mesh)Destroy(mesh);if(material)Destroy(material);if(atlas)Destroy(atlas);}
    }
}
