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
        bool[] presentationMask;
        public int PresentedTreeCount {get;private set;}
        public void SetDiagnosticDensity(int percent){presentationMask=PanningTestDistricts.Mask(trees.Count,percent);PresentedTreeCount=trees.Count*percent/100;}
        // Runtime art-direction adjustment; the immutable refined-C recipe stays unchanged.
        public float FoliageBrightness {get;set;}=1.25f;
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
                if(land.WaterDistance(q)<9||land.RoadDistance(q)<5||land.DisplayTerrain.terrainData.GetSteepness(local.x/land.Side,local.y/land.Side)>18||(land.View.CreationSettings?.version>=5&&land.Height(q)<land.View.SeaLevel+.2f))continue;
                if(land.View.Data.Biomes!=null&&land.Woodland(q)<.2f)continue;
                bool occupied=false;var cell=Vector2Int.FloorToInt(q/8);
                for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)if(native.TryGetValue(cell+new Vector2Int(x,z),out var nearby))foreach(var n in nearby)if((n-q).sqrMagnitude<6.25f)occupied=true;
                foreach(var n in positions)if((n-q).sqrMagnitude<20.25f){occupied=true;break;}if(occupied)continue;
                int serial=trees.Count,variant=p.zone=="core"?new[]{4,0,1,3,4,2,0}[serial%7]:p.zone=="edge"?new[]{0,5,1,6,2}[serial%5]:new[]{5,6,0}[serial%3];
                int color=serial%10==0?2:serial%4==0?1:0;float scale=1.65f*(variant==4?1+(serial%3)*.035f:variant>=5?.91f+(serial%3)*.035f:.96f+(serial%3)*.04f);
                var position=new Vector3(q.x,land.Height(q),q.y);var bounds=assets.meshes[variant*10].bounds;bounds.Encapsulate(assets.meshes[variant*10+1].bounds);
                trees.Add(new Tree{matrix=Matrix4x4.TRS(position,Quaternion.Euler(0,serial*137.508f+17,0),Vector3.one*scale),center=position+Vector3.up*bounds.center.y*scale,height=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)*scale,variant=variant,color=color});positions.Add(q);counts[p.zone]++;
            }
            PresentedTreeCount=trees.Count;CoreCount=counts["core"];EdgeCount=counts["edge"];OutlierCount=counts["outlier"];
            var capacity=new int[7];foreach(var tree in trees)capacity[tree.variant]++;
            for(int i=0;i<batches.Length;i++)batches[i]=new List<Tree>(capacity[i/5]);
            var layers=land.DisplayTerrain.terrainData.terrainLayers;
            foreach(int i in new[]{1,2}){var layer=Instantiate(layers[i]);layer.diffuseTexture=i==1?assets.grass1:assets.grass2;layer.diffuseRemapMin=Vector4.zero;layer.diffuseRemapMax=Vector4.one;layers[i]=layer;ownedLayers.Add(layer);}
            land.DisplayTerrain.terrainData.terrainLayers=layers;
        }
        readonly List<TerrainLayer> ownedLayers=new List<TerrainLayer>();
        void LateUpdate()
        {
            // The accepted authored family is a summer baseline. Native seasonal trees remain authoritative in fall/winter.
            if(PresentedTreeCount==0||!land||!viewerCamera||!assets||!land.TreesEnabled||land.View.Season!=RegionSeason.Summer)return;
            GeometryUtility.CalculateFrustumPlanes(viewerCamera,planes);foreach(var batch in batches)batch.Clear();
            for(int treeIndex=0;treeIndex<trees.Count;treeIndex++){if(presentationMask!=null&&!presentationMask[treeIndex])continue;var t=trees[treeIndex];if(!GeometryUtility.TestPlanesAABB(planes,new Bounds(t.center,Vector3.one*t.height*1.5f)))continue;
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
