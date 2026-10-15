using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    /// <summary>Seeded lake-only bed painting and static batches. Does not modify authoritative terrain heights.</summary>
    public sealed class LakeShoreRenderer : MonoBehaviour
    {
        public static bool EnabledForEvaluation=true;
        sealed class Batch {public Mesh mesh;public Material material;public int submesh;public readonly List<Matrix4x4> placements=new();public Matrix4x4[][] chunks;public bool plant;}
        readonly List<Batch> batches=new();readonly List<Object> owned=new();
        MeadowDistrictLandscape land;Bounds bounds;RegionWaterBody body;float waterLevel;
        float ShoreDistance(Vector2 p)=>land.LakeShoreDistance(body,waterLevel,p);
        public int Rocks {get;private set;} public int Plants {get;private set;} public int Leaves {get;private set;}
        public ulong PlacementFingerprint {get;private set;}
        public void Initialize(MeadowDistrictLandscape landscape,RegionWaterBody body,float level)
        {
            land=landscape;this.body=body;waterLevel=level;var min=(body.Bounds.min-Vector2.one*.5f)*land.View.MapSide-Vector2.one*8;
            var max=(body.Bounds.max-Vector2.one*.5f)*land.View.MapSide+Vector2.one*8;
            bounds=new Bounds(new Vector3((min.x+max.x)/2,level+4,(min.y+max.y)/2),new Vector3(max.x-min.x,32,max.y-min.y));
            PaintBed(min,max,level);
            var rocks=Resources.Load<RiverBedRockAssets>("RiverBeds/RichRockAssets");
            for(int i=0;i<rocks.meshes.Length;i++)batches.Add(new Batch{mesh=rocks.meshes[i],material=rocks.materials[i]});
            var reeds=Extract(land.Baseline.shorelineReeds);var grass=Extract(land.Baseline.shorelineGrass);
            int salt=26012;if(body.id!="hill-lake")foreach(char c in body.id??"")salt=unchecked(salt*31+c);
            var rng=land.RandomFor(0,0,salt);float R()=>(float)rng.NextDouble();
            // Jittered cluster centers filtered by continuous broad noise; long open gaps are retained.
            for(float z=min.y;z<max.y;z+=5)for(float x=min.x;x<max.x;x+=5){
                var center=new Vector2(x+R()*5,z+R()*5);if(!land.Contains(center))continue;
                float d=ShoreDistance(center),h=land.Height(center),patch=land.Noise(center,17,219);
                if(d< -14||d>6||h>level+2||patch<.49f||land.RoadDistance(center)<land.Baseline.roadWidth+1)continue;
                int count=2+rng.Next(5);
                for(int j=0;j<count&&Rocks<2048;j++){
                    var p=center+new Vector2(R()-.5f,R()-.5f)*5;
                    if(!land.Contains(p))continue;float height=land.Height(p),depth=level-height;
                    if(depth< -1.5f||depth>2.2f||ShoreDistance(p)<-14||ShoreDistance(p)>6)continue;
                    int type=rng.Next(3)+(depth<.1f&&R()<.55f?3:0);var mesh=rocks.meshes[type];
                    float size=Mathf.Lerp(.28f,1.25f,R())*(body.kind==WaterBodyKind.Pond?.7f:1);if(j==0&&R()<.2f)size=1.7f;
                    float scale=size/Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));
                    batches[type].placements.Add(Matrix4x4.TRS(new Vector3(p.x,height+size*.1f,p.y),Quaternion.Euler(R()*20,R()*360,R()*15),Vector3.one*scale)*Matrix4x4.Translate(-mesh.bounds.center));Rocks++;
                }
                if(d> -3&&d<5&&patch>.54f){
                    int plants=3+rng.Next(6);
                    for(int j=0;j<plants&&Plants<768;j++){
                        var p=center+new Vector2(R()-.5f,R()-.5f)*5;float height=land.Height(p);
                        if(!land.Contains(p)||height<level-.45f||height>level+1.7f)continue;
                        var pieces=height<level+.25f?reeds:grass;
                        var pose=Matrix4x4.TRS(new Vector3(p.x,height-.025f,p.y),Quaternion.Euler(0,R()*360,0),Vector3.one*Mathf.Lerp(.7f,1.25f,R()));
                        foreach(var piece in pieces)piece.batch.placements.Add(pose*piece.local);Plants++;
                    }
                }
            }
            BuildLeaves(min,max,level,rng);
            PlacementFingerprint=1469598103934665603UL;
            foreach(var batch in batches){
                foreach(var matrix in batch.placements)for(int k=0;k<16;k++)PlacementFingerprint=unchecked((PlacementFingerprint^(uint)System.BitConverter.SingleToInt32Bits(matrix[k]))*1099511628211UL);
                batch.chunks=new Matrix4x4[(batch.placements.Count+255)/256][];
                for(int i=0;i<batch.chunks.Length;i++){int count=Mathf.Min(256,batch.placements.Count-i*256);batch.chunks[i]=batch.placements.GetRange(i*256,count).ToArray();}
                batch.placements.Clear();
            }
            Debug.Log($"Lake phase two: {Rocks} rocks, {Plants} reed/grass clumps, {Leaves} aquatic leaves; {batches.Count} static mesh batches.");
        }
        void PaintBed(Vector2 min,Vector2 max,float level)
        {
            var td=land.DisplayTerrain.terrainData;int n=td.alphamapResolution,oldLayers=land.EnsureLakeBedLayers();
            int lowX=Mathf.Clamp(Mathf.FloorToInt((min.x-land.Origin.x)/land.Side*(n-1)),0,n-1),lowZ=Mathf.Clamp(Mathf.FloorToInt((min.y-land.Origin.y)/land.Side*(n-1)),0,n-1);
            int highX=Mathf.Clamp(Mathf.CeilToInt((max.x-land.Origin.x)/land.Side*(n-1)),0,n-1),highZ=Mathf.Clamp(Mathf.CeilToInt((max.y-land.Origin.y)/land.Side*(n-1)),0,n-1);
            int width=highX-lowX+1,height=highZ-lowZ+1;
            var original=td.GetAlphamaps(lowX,lowZ,width,height);var maps=(float[,,])original.Clone();
            for(int z=0;z<height;z++)for(int x=0;x<width;x++){
                var p=land.Origin+new Vector2((x+lowX)/(float)(n-1),(z+lowZ)/(float)(n-1))*land.Side;
                float amount=0,pebbles=0,sand=0,soil=0,moss=0;
                if(p.x>=min.x&&p.x<=max.x&&p.y>=min.y&&p.y<=max.y){
                    float d=ShoreDistance(p),depth=level-land.Height(p),patch=land.Noise(p,14,217),small=land.Noise(p,4,223);
                    float bankWidth=Mathf.Lerp(2,7,land.Noise(p,24,229));
                    float wet=body.Contains(p/land.View.MapSide+Vector2.one*.5f)?Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.05f,.25f,depth)):0;
                    float dry=(1-Mathf.SmoothStep(0,1,Mathf.Max(0,d)/bankWidth))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.7f,patch));
                    amount=Mathf.Max(wet*.96f,dry*.82f);
                    pebbles=.1f+small*.35f;sand=(1-patch)*.2f;soil=.7f+patch*.55f;moss=d>0?patch*.4f:patch*.08f;
                    float total=pebbles+sand+soil+moss;pebbles/=total;sand/=total;soil/=total;moss/=total;
                }
                for(int l=0;l<maps.GetLength(2);l++)maps[z,x,l]=original[z,x,l]*(1-amount);
                maps[z,x,oldLayers]+=amount*pebbles;maps[z,x,oldLayers+1]+=amount*sand;maps[z,x,oldLayers+2]+=amount*soil;maps[z,x,oldLayers-1]+=amount*moss;
            }
            td.SetAlphamaps(lowX,lowZ,maps);
        }
        List<(Batch batch,Matrix4x4 local)> Extract(GameObject prefab)
        {
            var pieces=new List<(Batch,Matrix4x4)>();if(!prefab)return pieces;
            var instance=Instantiate(prefab);instance.SetActive(false);instance.transform.position=Vector3.zero;instance.transform.rotation=Quaternion.identity;
            var lod=instance.GetComponentInChildren<LODGroup>();var renderers=lod?lod.GetLODs()[0].renderers:instance.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renderers){var filter=renderer.GetComponent<MeshFilter>();if(!filter)continue;
                for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++){
                    var material=new Material(renderer.sharedMaterials[Mathf.Min(sub,renderer.sharedMaterials.Length-1)]){enableInstancing=true};owned.Add(material);
                    var batch=new Batch{mesh=filter.sharedMesh,material=material,submesh=sub,plant=true};batches.Add(batch);pieces.Add((batch,renderer.transform.localToWorldMatrix));
                }
            }Destroy(instance);return pieces;
        }
        void BuildLeaves(Vector2 min,Vector2 max,float level,System.Random rng)
        {
            float R()=>(float)rng.NextDouble();var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
            int clusters=0;
            for(int attempt=0;attempt<1500&&clusters<(body.kind==WaterBodyKind.Pond?3:9);attempt++){
                var center=new Vector2(Mathf.Lerp(min.x,max.x,R()),Mathf.Lerp(min.y,max.y,R()));float depth=level-land.Height(center);
                if(!body.Contains(center/land.View.MapSide+Vector2.one*.5f)||!land.Contains(center)||depth<.25f||depth>1.1f||land.Noise(center,23,307)<.56f)continue;
                clusters++;int count=10+rng.Next(16);
                for(int j=0;j<count;j++){
                    var p=center+new Vector2(R()-.5f,R()-.5f)*4;float d=level-land.Height(p);if(!land.Contains(p)||!body.Contains(p/land.View.MapSide+Vector2.one*.5f)||d<.12f||d>1.5f)continue;
                    float radius=Mathf.Lerp(.13f,.30f,R()),rotation=R()*Mathf.PI*2;int start=vertices.Count;var color=Color.Lerp(new Color(.24f,.33f,.07f),new Color(.46f,.51f,.14f),R());
                    vertices.Add(new Vector3(p.x,level+.022f,p.y));colors.Add(color);
                    // An open sector gives each leaf a modest notch; all leaves share one mesh.
                    for(int k=0;k<=10;k++){float angle=rotation+.22f+k*(Mathf.PI*2-.44f)/10;vertices.Add(new Vector3(p.x+Mathf.Cos(angle)*radius,level+.022f,p.y+Mathf.Sin(angle)*radius*.85f));colors.Add(color);}
                    for(int k=0;k<10;k++){triangles.Add(start);triangles.Add(start+k+2);triangles.Add(start+k+1);}Leaves++;
                }
            }
            var mesh=new Mesh{name="Seeded sparse aquatic leaf clusters"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            var source=Resources.Load<Material>("Water/LakeLeaves");var mat=new Material(source);owned.Add(mat);
            var batch=new Batch{mesh=mesh,material=mat,plant=true};batch.placements.Add(Matrix4x4.identity);batches.Add(batch);
        }
        void LateUpdate()=>DrawBatches();
        public void DrawBatches(){foreach(var batch in batches){if(batch.chunks==null||batch.chunks.Length==0||(batch.plant&&!land.SmallPlantsEnabled))continue;
            var rp=new RenderParams(batch.material){worldBounds=bounds,layer=30,shadowCastingMode=batch.plant?ShadowCastingMode.Off:ShadowCastingMode.On,receiveShadows=true};foreach(var chunk in batch.chunks)Graphics.RenderMeshInstanced(rp,batch.mesh,batch.submesh,chunk);}}
        void OnDestroy(){foreach(var obj in owned)if(obj)Destroy(obj);}
    }
}
