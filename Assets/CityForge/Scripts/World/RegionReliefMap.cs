using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Terrain-derived cartography. Exaggerated relief is presentation only; district heights stay authoritative.</summary>
    public sealed class RegionReliefMap : MonoBehaviour
    {
        Mesh slabMesh;Material slabMaterial;
        Mesh mesh;Material material;Texture2D atlas;MeshCollider picker;PrototypeRegionView view;
        const int Samples=513;
        public void Build(PrototypeRegionView source)=>RegionBuildSteps.Complete(BuildStages(source));
        public System.Collections.IEnumerator BuildStages(PrototypeRegionView source)
        {
            view=source;var layout=source.CreationSettings?.version>=6?new RegionTerrainLayout(source.CreationSettings):null;int n=Samples;var vertices=new Vector3[n*n];var uv=new Vector2[n*n];var triangles=new int[(n-1)*(n-1)*6];
            var snowPeaks=MountainSummitSnow.Applies(source)?MountainSummitSnow.Select(source.Data,MountainBorderVariation.CapCount(source)):System.Array.Empty<MountainSummitSnow.Summit>();
            float height=source.GetComponentInChildren<Terrain>().terrainData.size.y;float sea=source.SeaLevel;
            float H(float u,float v)=>SmoothRiverSurface.Sample(source.Data.Heights,u,v)*height;
            float Surface(float u,float v,float h){float water=sea;if(source.Data.WaterBodies!=null)foreach(var body in source.Data.WaterBodies)if((body.kind==WaterBodyKind.Lake||body.kind==WaterBodyKind.Pond)&&body.Bounds.Contains(new Vector2(u,v))&&body.Contains(new Vector2(u,v)))water=body.hasSurfaceLevel?body.surfaceLevelNormalized*height:sea;return Mathf.Max(h,water);}
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                int i=z*n+x;float u=x/(float)(n-1),v=z/(float)(n-1),h=Surface(u,v,H(u,v));
                vertices[i]=new Vector3((u-.5f)*source.MapSide,sea+(h-sea)*1.25f+1,(v-.5f)*source.MapSide);uv[i]=new Vector2(u,v);
            }
            const int pixels=1025;var colors=new Color[pixels*pixels];float seed=source.Data.Seed%997;
            for(int z=0;z<pixels;z++){for(int x=0;x<pixels;x++){
                float u=x/(float)(pixels-1),v=z/(float)(pixels-1),h=H(u,v),bank=SmoothRiverSurface.Sample(source.Data.WaterDistance,u,v)/PrototypeRegionData.Side;
                bool riverRecipe=source.CreationSettings!=null&&source.CreationSettings.version>=5;
                bool water=!source.Waterless&&(bank<0||(!riverRecipe&&h<sea+.1f));
                if(source.Data.WaterBodies!=null)foreach(var body in source.Data.WaterBodies)if((body.kind==WaterBodyKind.Lake||body.kind==WaterBodyKind.Pond)&&body.Bounds.Contains(new Vector2(u,v))&&body.Contains(new Vector2(u,v)))water=true;
                if(water){float offshore=layout!=null&&layout.HasOcean?Mathf.Max(0,-layout.LandDistance(new Vector2(u,v))):Mathf.Max(0,-bank);
                    colors[z*pixels+x]=source.Climate==RegionClimate.Tropical&&offshore>0&&IsOcean(source,new Vector2(u,v))?TropicalOceanDepth.MapColor(offshore):new Color(.12f,.31f,.40f);continue;}
                var ecology=source.Data.Biomes?.At(new Vector2(u,v));float woodland=ecology?.woodland??.4f;
                float patch=Mathf.PerlinNoise(seed+u*37,seed+v*37)*.65f+Mathf.PerlinNoise(seed+u*93,v*93)*.35f;
                float cover=Mathf.Clamp01(woodland*.55f+patch*.65f);
                var grass=Color.Lerp(new Color(.43f,.47f,.285f),new Color(.265f,.325f,.18f),cover);
                float dx=(H(Mathf.Clamp01(u+1/512f),v)-H(Mathf.Clamp01(u-1/512f),v))/(source.MapSide/256f);
                float dz=(H(u,Mathf.Clamp01(v+1/512f))-H(u,Mathf.Clamp01(v-1/512f)))/(source.MapSide/256f);
                float slope=new Vector2(dx,dz).magnitude;
                float rock=Mathf.SmoothStep(0,1,Mathf.Max(Mathf.InverseLerp(.14f,.42f,h/height),Mathf.InverseLerp(.25f,.9f,slope)));
                var stone=Color.Lerp(new Color(.61f,.50f,.32f),new Color(.73f,.66f,.49f),Mathf.Clamp01(h/height*1.7f));
                // Shade fine drainage folds from curvature of the actual shared heightfield.
                float lap=H(Mathf.Clamp01(u+1/512f),v)+H(Mathf.Clamp01(u-1/512f),v)+H(u,Mathf.Clamp01(v+1/512f))+H(u,Mathf.Clamp01(v-1/512f))-4*h;
                var color=Color.Lerp(grass,stone,rock);
                color=Color.Lerp(color,new Color(.94f,.96f,.98f),MountainSummitSnow.Cover(snowPeaks,new Vector2(u,v),h/height,source.Data.Seed));
                float coast=layout!=null&&layout.HasOcean?Mathf.Abs(layout.LandDistance(new Vector2(u,v))):OceanEdgeDistance(source.Data.WaterBodies,new Vector2(u,v));
                if(coast<.0025f&&bank>=0)color=Color.Lerp(color,new Color(.70f,.62f,.43f),1-Mathf.SmoothStep(0,1,coast/.0025f));
                color*=Mathf.Clamp(1-lap*.008f,.94f,1.04f);
                var normal=new Vector3(-dx*2.4f,1,-dz*2.4f).normalized;float light=Vector3.Dot(normal,new Vector3(-.55f,.75f,-.37f).normalized);
                float shadow=1;for(int k=1;k<=10;k++){float offset=k*3/512f;float hu=H(Mathf.Clamp01(u-offset*.82f),Mathf.Clamp01(v-offset*.55f));float rise=hu-h;float allowed=source.MapSide*offset*.45f;if(rise>allowed){shadow=.63f;break;}}
                float shade=(.53f+.65f*Mathf.Clamp01(light))*shadow;
                float fine=.97f+.06f*Mathf.PerlinNoise(seed+u*190,v*190);color*=shade*fine;color.a=1;colors[z*pixels+x]=color;
            }
            if(z%8==0)yield return null;}
            atlas=new Texture2D(pixels,pixels,TextureFormat.RGBA32,true){name="Regional shaded relief atlas",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};atlas.SetPixels(colors);atlas.Apply(true,true);
            int t=0;for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++){int i=z*n+x;triangles[t++]=i;triangles[t++]=i+n;triangles[t++]=i+1;triangles[t++]=i+1;triangles[t++]=i+n;triangles[t++]=i+n+1;}
            mesh=new Mesh{name="Regional relief — 513 samples",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;material=new Material(Resources.Load<Shader>("Region/RegionalRelief"));material.SetFloat("_Grid",source.GridSide);material.SetTexture("_BaseMap",atlas);
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            picker=gameObject.AddComponent<MeshCollider>();picker.sharedMesh=mesh;
            BuildSlab(vertices,n,source.MapSide);
        }
        static bool IsOcean(PrototypeRegionView source,Vector2 p){if(source.Data.WaterBodies!=null)foreach(var body in source.Data.WaterBodies)if(body.kind==WaterBodyKind.Ocean&&body.Bounds.Contains(p)&&body.Contains(p))return true;return false;}
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
            var rim=new System.Collections.Generic.List<Vector3>();
            for(int x=0;x<n-1;x++)rim.Add(top[x]);
            for(int z=0;z<n-1;z++)rim.Add(top[z*n+n-1]);
            for(int x=n-1;x>0;x--)rim.Add(top[(n-1)*n+x]);
            for(int z=n-1;z>0;z--)rim.Add(top[z*n]);
            float bottom=mesh.bounds.min.y-side*.045f;int count=rim.Count;
            var vertices=new Vector3[count*4];var colors=new Color[vertices.Length];var indices=new int[count*6];
            for(int i=0;i<count;i++){int k=i*4,t=i*6;var a=rim[i];var b=rim[(i+1)%count];
                vertices[k]=a;vertices[k+1]=b;vertices[k+2]=new Vector3(a.x,bottom,a.z);vertices[k+3]=new Vector3(b.x,bottom,b.z);
                float face=1-.12f*(i/(n-1));var upper=new Color(.30f,.26f,.18f)*face;var lower=new Color(.12f,.105f,.08f)*face;upper.a=lower.a=1;
                colors[k]=colors[k+1]=upper;colors[k+2]=colors[k+3]=lower;
                indices[t]=k;indices[t+1]=k+1;indices[t+2]=k+2;indices[t+3]=k+1;indices[t+4]=k+3;indices[t+5]=k+2;
            }
            slabMesh=new Mesh{name="Regional slab sides"};slabMesh.vertices=vertices;slabMesh.colors=colors;slabMesh.triangles=indices;slabMesh.RecalculateBounds();
            var slab=new GameObject("Regional slab");slab.transform.SetParent(transform,false);slab.AddComponent<MeshFilter>().sharedMesh=slabMesh;
            slabMaterial=new Material(Resources.Load<Shader>("Region/RegionalSlab"));var renderer=slab.AddComponent<MeshRenderer>();renderer.sharedMaterial=slabMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        public void SetDistrictGroups(Texture2D ids){if(material){material.SetTexture("_DistrictIds",ids);material.SetFloat("_Grouped",1);}}
        public bool TryPick(Ray ray,out int district){district=-1;if(!picker.Raycast(ray,out var hit,1000000))return false;district=view.DistrictAtWorld(hit.point);return district>=0;}
        void OnDestroy(){if(slabMesh)Destroy(slabMesh);if(slabMaterial)Destroy(slabMaterial);if(mesh)Destroy(mesh);if(material)Destroy(material);if(atlas)Destroy(atlas);}
    }
}
