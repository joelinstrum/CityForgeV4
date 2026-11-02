using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    /// <summary>District-owned, deterministic presentation; canonical native instances remain untouched.</summary>
    public sealed class ApprovedDistrictForest : MonoBehaviour
    {
        [Serializable] sealed class Placement { public float x,z; public string zone; }
        [Serializable] sealed class Layout { public Placement[] placements; }
        struct Tree { public Matrix4x4 matrix; public Vector3 center; public float height; public int variant,color; }
        readonly List<Tree> trees=new List<Tree>();
        readonly List<Tree>[] batches=new List<Tree>[35];
        readonly Matrix4x4[] matrices=new Matrix4x4[1023];
        readonly List<Vector4> tints=new List<Vector4>(1023),targets=new List<Vector4>(1023);
        readonly SphericalHarmonicsL2[] probes=new SphericalHarmonicsL2[1023];
        readonly Plane[] planes=new Plane[6];
        MaterialPropertyBlock block;
        ApprovedForestAssets assets; MeadowDistrictLandscape land; Camera viewerCamera;
        public int TreeCount=>trees.Count;
        readonly HashSet<Vector3> replacedNative=new(),evergreenPalmNative=new();
        readonly List<Vector3> replacementOrder=new();
        public bool ReplacesPosition(Vector3 position)=>replacedNative.Contains(position);
        public bool ReplacesNativeTrees=>land&&land.View.CreationSettings?.version>=2&&!land.IsRegionOverview;
        public TreeInstance[] NativePresentation(TreeInstance[] source)
        {
            if(!ReplacesNativeTrees)return source;
            var remaining=new List<TreeInstance>(source.Length);
            var hidden=land.View.VisualSeason==RegionSeason.Summer?replacedNative:evergreenPalmNative;
            foreach(var tree in source)if(!hidden.Contains(tree.position))remaining.Add(tree);
            return remaining.ToArray();
        }
        bool[] presentationMask,palmMask;
        public int PresentedTreeCount {get;private set;}
        public void SetDiagnosticDensity(int percent){presentationMask=PanningTestDistricts.Mask(trees.Count,percent);PresentedTreeCount=trees.Count*percent/100;RefreshPalms();}
        // Lift the dark authored crowns while retaining their palette, shading and cast shadows.
        public float FoliageBrightness {get;set;}=1.5f;
        public int CoreCount {get;private set;}
        public int EdgeCount {get;private set;}
        public int OutlierCount {get;private set;}
        public void Initialize(MeadowDistrictLandscape landscape)
        {
            block=new MaterialPropertyBlock();land=landscape;viewerCamera=land.View.GetComponentInChildren<Camera>();
            assets=Resources.Load<ApprovedForestAssets>("ApprovedForest/Forest");
            if(!assets||assets.meshes==null||assets.meshes.Length!=70||!assets.layout||!assets.leaf||!assets.wood)
                throw new InvalidOperationException("Approved forest runtime assets are missing. Build the verified local checkpoint using CityForge/Forest/Build approved runtime assets.");
            var sh=new SphericalHarmonicsL2();for(int c=0;c<3;c++)for(int i=0;i<9;i++)sh[c,i]=land.Baseline.ambientSH[c*9+i];
            for(int i=0;i<1023;i++)probes[i]=sh;
            if(ReplacesNativeTrees)BuildNativeReplacements();
            else {
            var native=new Dictionary<Vector2Int,List<Vector2>>();
            foreach(var t in land.CanonicalTreeInstances){if(!((t.prototypeIndex>=3&&t.prototypeIndex<=10)||t.prototypeIndex>=19))continue;
                var p=land.Origin+new Vector2(t.position.x,t.position.z)*land.Side;var key=Vector2Int.FloorToInt(p/8);
                if(!native.TryGetValue(key,out var list))native[key]=list=new List<Vector2>();list.Add(p);}
            var center=land.Origin+Vector2.one*land.Side*.5f;
            var layout=JsonUtility.FromJson<Layout>(assets.layout.text);
            var positions=new List<Vector2>();
            var quotas=new Dictionary<string,int>{{"core",400},{"edge",150},{"outlier",50}};
            var counts=new Dictionary<string,int>{{"core",0},{"edge",0},{"outlier",0}};
            foreach(var p in layout.placements){if(counts[p.zone]>=quotas[p.zone])continue;var q=center+new Vector2(p.x,p.z);
                var local=q-land.Origin;if(local.x<12||local.y<12||local.x>land.Side-12||local.y>land.Side-12)continue;
                if(land.View.InCampClearing(q,5)||!land.Contains(q)||land.WaterDistance(q)<9||land.RoadDistance(q)<5||land.DisplayTerrain.terrainData.GetSteepness(local.x/land.Side,local.y/land.Side)>18||(land.View.CreationSettings?.version>=5&&land.Height(q)<land.View.SeaLevel+.2f))continue;
                if(land.View.Data.Biomes!=null&&land.Woodland(q)<.2f)continue;
                bool occupied=false;var cell=Vector2Int.FloorToInt(q/8);
                for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)if(native.TryGetValue(cell+new Vector2Int(x,z),out var nearby))foreach(var n in nearby)if((n-q).sqrMagnitude<6.25f)occupied=true;
                foreach(var n in positions)if((n-q).sqrMagnitude<20.25f){occupied=true;break;}if(occupied)continue;
                int serial=trees.Count,variant=p.zone=="core"?new[]{4,0,1,3,4,2,0}[serial%7]:p.zone=="edge"?new[]{0,5,1,6,2}[serial%5]:new[]{5,6,0}[serial%3];
                int color=serial%10==0?2:serial%4==0?1:0;float scale=1.65f*(variant==4?1+(serial%3)*.035f:variant>=5?.91f+(serial%3)*.035f:.96f+(serial%3)*.04f);
                var position=new Vector3(q.x,land.Height(q),q.y);var bounds=assets.meshes[variant*10].bounds;bounds.Encapsulate(assets.meshes[variant*10+1].bounds);
                trees.Add(new Tree{matrix=Matrix4x4.TRS(position,Quaternion.Euler(0,serial*137.508f+17,0),Vector3.one*scale),center=position+Vector3.up*bounds.center.y*scale,height=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)*scale,variant=variant,color=color});positions.Add(q);counts[p.zone]++;
            }
            CoreCount=counts["core"];EdgeCount=counts["edge"];OutlierCount=counts["outlier"];
            }
            RefreshPalms();
            PresentedTreeCount=trees.Count;
            var capacity=new int[7];foreach(var tree in trees)capacity[tree.variant]++;
            for(int i=0;i<batches.Length;i++)batches[i]=new List<Tree>(capacity[i/5]);
            var layers=land.DisplayTerrain.terrainData.terrainLayers;
            foreach(int i in new[]{1,2}){
                var layer=Instantiate(layers[i]);layer.diffuseTexture=i==1?assets.grass1:assets.grass2;
                layer.diffuseRemapMin=Vector4.zero;
                // Lift and mute the meadow below the green crowns. Existing layer weights
                // blend a fresher olive with a warmer, subtly straw-toned grass treatment.
                layer.diffuseRemapMax=i==1?new Vector4(1.25f,1.12f,1.20f,1):new Vector4(1.42f,1.18f,1.18f,1);
                layers[i]=layer;ownedLayers.Add(layer);
            }
            land.DisplayTerrain.terrainData.terrainLayers=layers;
            land.gameObject.AddComponent<MeadowGrassAppearance>().Initialize(land.DisplayTerrain);
        }
        struct Candidate
        {
            public TreeInstance native; public Vector2 point; public int variant; public float scale,radius; public uint hash;
        }
        static uint PlacementHash(int index,int seed)
        {
            unchecked {uint h=(uint)(index*73856093^seed);h^=h>>16;h*=0x7feb352d;h^=h>>15;return h;}
        }
        void BuildNativeReplacements()
        {
            // Derive presentation from canonical ecology placements; never relocate or serialize trees.
            var candidates=new List<Candidate>();int eligible=0,index=0;
            foreach(var native in land.CanonicalTreeInstances){
                int id=index++;if(!((native.prototypeIndex>=3&&native.prototypeIndex<=10)||native.prototypeIndex>=19))continue;eligible++;
                var p=land.Origin+new Vector2(native.position.x,native.position.z)*land.Side;var local=p-land.Origin;if(!land.Contains(p))continue;
                if(local.x<9||local.y<9||local.x>land.Side-9||local.y>land.Side-9||land.WaterDistance(p)<9||land.RoadDistance(p)<5||land.DisplayTerrain.terrainData.GetSteepness(native.position.x,native.position.z)>18)continue;
                if(land.View.Data.Biomes!=null&&land.Woodland(p)<.2f)continue;
                if(land.View.CreationSettings?.version>=5&&land.Height(p)<land.View.SeaLevel+.2f)continue;
                uint h=PlacementHash(id,land.Seed);int variant=(int)(h%7);
                float scale=1.65f*(variant==4?1+(h%3)*.035f:variant>=5?.91f+(h%3)*.035f:.96f+(h%3)*.04f);
                var crown=assets.meshes[variant*10+1].bounds;
                candidates.Add(new Candidate{native=native,point=p,variant=variant,scale=scale,radius=Mathf.Min(crown.extents.x,crown.extents.z)*scale,hash=h});
            }
            // A spatial crown graph makes each grown patch contiguous, with real trunk clearance.
            var cells=new Dictionary<Vector2Int,List<int>>();const float cellSize=18;
            for(int i=0;i<candidates.Count;i++){var key=Vector2Int.FloorToInt(candidates[i].point/cellSize);if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<int>();list.Add(i);}
            var links=new List<int>[candidates.Count];
            for(int i=0;i<candidates.Count;i++){
                links[i]=new List<int>();var a=candidates[i];var cell=Vector2Int.FloorToInt(a.point/cellSize);
                for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)if(cells.TryGetValue(cell+new Vector2Int(x,z),out var nearby))foreach(int j in nearby){
                    if(i==j)continue;var b=candidates[j];float d=(a.point-b.point).sqrMagnitude;
                    if(d>=9&&d<=Mathf.Pow((a.radius+b.radius)*.92f,2)&&Mathf.Abs(land.Height(a.point)-land.Height(b.point))<4)links[i].Add(j);
                }
                links[i].Sort((aIndex,bIndex)=>candidates[aIndex].hash.CompareTo(candidates[bIndex].hash));
            }
            var seeds=new List<int>();for(int i=0;i<candidates.Count;i++)seeds.Add(i);
            seeds.Sort((a,b)=>candidates[a].hash.CompareTo(candidates[b].hash));
            var chosen=new bool[candidates.Count];var order=new List<int>();int budget=Mathf.Min(eligible/4,candidates.Count);
            foreach(int seed in seeds){
                if(order.Count>=budget)break;if(chosen[seed])continue;
                // Variable patch sizes and traversal produce irregular margins and small peripheral groups.
                int quota=14+(int)(candidates[seed].hash%39),added=0;var frontier=new Queue<int>();var queued=new HashSet<int>();frontier.Enqueue(seed);queued.Add(seed);
                while(frontier.Count>0&&added<quota&&order.Count<budget){int i=frontier.Dequeue();if(chosen[i])continue;
                    bool crowded=false;var cell=Vector2Int.FloorToInt(candidates[i].point/cellSize);
                    for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)if(cells.TryGetValue(cell+new Vector2Int(x,z),out var nearby))foreach(int j in nearby)if(chosen[j]&&(candidates[i].point-candidates[j].point).sqrMagnitude<9)crowded=true;
                    if(crowded)continue;chosen[i]=true;order.Add(i);added++;
                    foreach(int j in links[i])if(!chosen[j]&&queued.Add(j))frontier.Enqueue(j);
                }
            }
            foreach(int i in order){var c=candidates[i];replacedNative.Add(c.native.position);replacementOrder.Add(c.native.position);
                var position=new Vector3(c.point.x,land.Height(c.point),c.point.y);var bounds=assets.meshes[c.variant*10].bounds;bounds.Encapsulate(assets.meshes[c.variant*10+1].bounds);
                trees.Add(new Tree{matrix=Matrix4x4.TRS(position,Quaternion.Euler(0,c.native.rotation*Mathf.Rad2Deg,0),Vector3.one*c.scale),center=position+Vector3.up*bounds.center.y*c.scale,height=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)*c.scale,variant=c.variant,color=c.hash%10==0?2:c.hash%4==0?1:0});
                int degree=0;foreach(int j in links[i])if(chosen[j])degree++;if(degree>=3)CoreCount++;else if(degree>0)EdgeCount++;else OutlierCount++;
            }
        }
        void RefreshPalms()
        {
            if(!land||!land.Palms)return;
            var matrices=new List<Matrix4x4>(trees.Count);foreach(var tree in trees)matrices.Add(tree.matrix);
            var radii=new List<float>(trees.Count);foreach(var tree in trees){var crown=assets.meshes[tree.variant*10+1].bounds;radii.Add(Mathf.Min(crown.extents.x,crown.extents.z)*tree.matrix.lossyScale.x);}
            palmMask=land.Palms.PresentAuthored(matrices,presentationMask,radii);
            if(ReplacesNativeTrees){evergreenPalmNative.Clear();for(int i=0;i<palmMask.Length;i++)if(palmMask[i])evergreenPalmNative.Add(replacementOrder[i]);}
        }
        readonly List<TerrainLayer> ownedLayers=new List<TerrainLayer>();
        void LateUpdate()
        {
            // The accepted authored family is a summer baseline. Native seasonal trees remain authoritative in fall/winter.
            if(PresentedTreeCount==0||!land||!viewerCamera||!assets||!land.TreesEnabled||land.View.VisualSeason!=RegionSeason.Summer)return;
            GeometryUtility.CalculateFrustumPlanes(viewerCamera,planes);foreach(var batch in batches)batch.Clear();
            for(int treeIndex=0;treeIndex<trees.Count;treeIndex++){if((presentationMask!=null&&!presentationMask[treeIndex])||(palmMask!=null&&palmMask[treeIndex]))continue;var t=trees[treeIndex];if(!GeometryUtility.TestPlanesAABB(planes,new Bounds(t.center,Vector3.one*t.height*1.5f)))continue;
                float denominator=viewerCamera.orthographic?2*viewerCamera.orthographicSize:2*Mathf.Max(.01f,Vector3.Dot(t.center-viewerCamera.transform.position,viewerCamera.transform.forward))*Mathf.Tan(viewerCamera.fieldOfView*Mathf.Deg2Rad*.5f);
                float coverage=t.height/denominator*QualitySettings.lodBias;int lod=coverage>=.16f?0:coverage>=.09f?1:coverage>=.045f?2:coverage>=.0225f?3:4;
                batches[t.variant*5+lod].Add(t);}
            // Palette is instanced shader data, so colors share each mesh/LOD draw.
            // Keep uploads proportional to the actual batch instead of copying 1,023 probes per draw.
            for(int key=0;key<batches.Length;key++){var batch=batches[key];if(batch.Count==0)continue;int lod=key%5,variant=key/5;
                for(int start=0;start<batch.Count;start+=1023){int count=Mathf.Min(1023,batch.Count-start);targets.Clear();
                    for(int i=0;i<count;i++){var tree=batch[start+i];matrices[i]=tree.matrix;targets.Add(ApprovedForestColors.PaletteTarget(tree.color));}
                    block.Clear();block.SetVectorArray("_PaletteTarget",targets);block.CopySHCoefficientArraysFrom(probes,0,0,count);
                    for(int part=0;part<2;part++){var material=part==0?assets.wood:assets.leaf;tints.Clear();
                        for(int i=0;i<count;i++){
                            int color=batch[start+i].color;var tint=part==0?Color.white:color==0?new Color(.28f,.46f,.66f,1):color==1?new Color(.40f,.59f,.73f,1):new Color(.52f,.68f,.72f,1);
                            if(part==1){tint.r*=FoliageBrightness;tint.g*=FoliageBrightness;tint.b*=FoliageBrightness;}tints.Add(tint);
                        }
                        block.SetVectorArray("_InstanceTint",tints);
                        Graphics.DrawMeshInstanced(assets.meshes[variant*10+lod*2+part],0,material,matrices,count,block,ShadowCastingMode.Off,true,0,null,LightProbeUsage.CustomProvided);
                        int shadowLod=Mathf.Min(4,lod<2?2:lod+1);
                        Graphics.DrawMeshInstanced(assets.meshes[variant*10+shadowLod*2+part],0,material,matrices,count,block,ShadowCastingMode.ShadowsOnly,false,0,null,LightProbeUsage.CustomProvided);
                    }
                }
            }
        }

        void OnDestroy(){foreach(var layer in ownedLayers)if(layer)Destroy(layer);}
    }
}
