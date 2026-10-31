using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Climate-specific presentation; canonical native trees remain unchanged.</summary>
    public sealed class TropicalPalmForest:MonoBehaviour
    {
        public const float CoastalBandMetres=75;
        public const int InlandPercent=30,CoastalPercent=50;
        struct Palm { public Matrix4x4 matrix;public int variant; }
        readonly List<Palm> native=new(),authored=new();
        readonly List<(Vector2 a,Vector2 b)> shore=new();
        List<Matrix4x4>[] batches;
        readonly Matrix4x4[] upload=new Matrix4x4[1023];
        readonly Plane[] planes=new Plane[6];
        MeadowDistrictLandscape land;Camera viewer;TropicalPalmAssets assets;
        CityForge.CameraSystem.CityForgeCameraController navigation;
        MaterialPropertyBlock windBlock;
        static readonly int WindWeightId=Shader.PropertyToID("_PalmWindWeight"),WindTimeId=Shader.PropertyToID("_PalmWindTime"),PalmHeightId=Shader.PropertyToID("_PalmHeight");
        public bool WindRequested=>navigation&&navigation.ZoomLevel<=1;
        public float WindWeight {get;private set;}
        public float WindTime {get;private set;}
        public int NativePalmCount=>native.Count;
        public int AuthoredPalmCount=>authored.Count;
        public int NativeCoastalCandidates {get;private set;}
        public int NativeCoastalPalms {get;private set;}
        public int AuthoredCoastalCandidates {get;private set;}
        public int AuthoredCoastalPalms {get;private set;}
        public void Initialize(MeadowDistrictLandscape landscape)
        {
            windBlock=new MaterialPropertyBlock();land=landscape;viewer=land.View.GetComponentInChildren<Camera>();navigation=viewer.GetComponent<CityForge.CameraSystem.CityForgeCameraController>();
            assets=Resources.Load<TropicalPalmAssets>("TropicalCoconut/Palms");
            if(!assets||assets.variants==null||assets.variants.Length!=8||!assets.material)
                throw new InvalidOperationException("Tropical coconut assets missing. Run CityForge/Forest/Prepare tropical palm assets.");
            batches=new List<Matrix4x4>[assets.variants.Length*4];for(int i=0;i<batches.Length;i++)batches[i]=new List<Matrix4x4>();
            foreach(var variant in assets.variants)if(variant.lods==null||variant.lods.Length!=4)throw new InvalidOperationException("Incomplete coconut palm LODs.");
            // Only semantic ocean shoreline segments count. Map borders and rivers do not.
            foreach(var body in land.View.Data.WaterBodies){
                if(body.kind!=WaterBodyKind.Ocean||body.outline==null)continue;
                foreach(var points in body.Contours)for(int i=0;i<points.Length;i++){
                    var a=points[i];var b=points[(i+1)%points.Length];
                    if((a.x==0&&b.x==0)||(a.x==1&&b.x==1)||(a.y==0&&b.y==0)||(a.y==1&&b.y==1))continue;
                    shore.Add(((a-Vector2.one*.5f)*land.View.MapSide,(b-Vector2.one*.5f)*land.View.MapSide));
                }
            }
        }
        public bool NearCoast(Vector3 position)
        {
            var p=new Vector2(position.x,position.z);
            foreach(var edge in shore){var d=edge.b-edge.a;float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-edge.a,d)/d.sqrMagnitude):0;
                if((p-(edge.a+d*t)).sqrMagnitude<=CoastalBandMetres*CoastalBandMetres)return true;}
            return false;
        }
        bool[] Select(List<Vector3> positions,out int nearCount,out int nearPalms,List<float> radii=null)
        {
            var near=new List<int>();var inland=new List<int>();
            for(int i=0;i<positions.Count;i++)(NearCoast(positions[i])?near:inland).Add(i);
            var selected=new bool[positions.Count];
            foreach(var group in new[]{near,inland}){
                if(radii==null){var mask=PanningTestDistricts.Mask(group.Count,group==near?CoastalPercent:InlandPercent);
                    for(int i=0;i<group.Count;i++)if(mask[i])selected[group[i]]=true;
                }else{
                    // Keep dense crown interiors: palm replacements begin at isolated crowns and patch fringes.
                    var degree=new int[positions.Count];
                    foreach(int i in group)for(int j=0;j<positions.Count;j++)if(i!=j){var d=positions[i]-positions[j];d.y=0;if(d.sqrMagnitude<=Mathf.Pow(radii[i]+radii[j],2))degree[i]++;}
                    group.Sort((a,b)=>{int c=degree[a].CompareTo(degree[b]);return c!=0?c:a.CompareTo(b);});
                    int count=group.Count*(group==near?CoastalPercent:InlandPercent)/100;
                    for(int i=0;i<count;i++)selected[group[i]]=true;
                }
            }
            nearCount=near.Count;nearPalms=near.Count*CoastalPercent/100;return selected;
        }
        public TreeInstance[] PresentNative(TreeInstance[] source)
        {
            native.Clear();var eligible=new List<int>();var positions=new List<Vector3>();
            var approved=land.GetComponent<ApprovedDistrictForest>();
            for(int i=0;i<source.Length;i++){int p=source[i].prototypeIndex;if(!((p>=3&&p<=10)||p>=15))continue;
                if(approved&&approved.ReplacesNativeTrees&&approved.ReplacesPosition(source[i].position))continue;
                eligible.Add(i);var point=land.Origin+new Vector2(source[i].position.x,source[i].position.z)*land.Side;
                positions.Add(new Vector3(point.x,land.Height(point),point.y));}
            var chosen=Select(positions,out int nearCount,out int nearPalms);NativeCoastalCandidates=nearCount;NativeCoastalPalms=nearPalms;
            var removed=new bool[source.Length];
            for(int i=0;i<eligible.Count;i++)if(chosen[i]){var tree=source[eligible[i]];removed[eligible[i]]=true;native.Add(MakePalm(positions[i],tree.rotation*Mathf.Rad2Deg));}
            var remaining=new List<TreeInstance>(source.Length-native.Count);
            for(int i=0;i<source.Length;i++)if(!removed[i])remaining.Add(source[i]);return remaining.ToArray();
        }
        public bool[] PresentAuthored(List<Matrix4x4> source,bool[] visible,List<float> crownRadii=null)
        {
            authored.Clear();var eligible=new List<int>();var positions=new List<Vector3>();var radii=crownRadii==null?null:new List<float>();
            for(int i=0;i<source.Count;i++)if(visible==null||visible[i]){eligible.Add(i);positions.Add(source[i].GetColumn(3));if(radii!=null)radii.Add(crownRadii[i]);}
            var chosen=Select(positions,out int nearCount,out int nearPalms,radii);AuthoredCoastalCandidates=nearCount;AuthoredCoastalPalms=nearPalms;
            var removed=new bool[source.Count];
            for(int i=0;i<eligible.Count;i++)if(chosen[i]){int index=eligible[i];removed[index]=true;authored.Add(MakePalm(positions[i],source[index].rotation.eulerAngles.y));}
            return removed;
        }
        Palm MakePalm(Vector3 position,float yaw)
        {
            uint hash;unchecked{hash=(uint)(Mathf.RoundToInt(position.x*100)*73856093^Mathf.RoundToInt(position.z*100)*19349663^land.Seed);hash^=hash>>16;hash*=0x7feb352d;hash^=hash>>15;}
            return new Palm{variant=(int)(hash%8),matrix=Matrix4x4.TRS(position,Quaternion.Euler(0,yaw,0),Vector3.one*Mathf.Lerp(.92f,1.08f,((hash>>8)&65535)/65535f))};
        }
        void LateUpdate()
        {
            if(!land||!viewer||!land.TreesEnabled)return;
            WindWeight=Mathf.MoveTowards(WindWeight,WindRequested?1:0,Time.deltaTime/.35f);
            if(WindWeight>0)WindTime+=Time.deltaTime;
            GeometryUtility.CalculateFrustumPlanes(viewer,planes);foreach(var batch in batches)batch.Clear();
            Collect(native);var approved=land.GetComponent<ApprovedDistrictForest>();
            if(land.View.VisualSeason==RegionSeason.Summer||(approved&&approved.ReplacesNativeTrees))Collect(authored);
            for(int key=0;key<batches.Length;key++)for(int start=0;start<batches[key].Count;start+=1023){
                int lod=key%4;var meshes=assets.variants[key/4].lods;int count=Mathf.Min(1023,batches[key].Count-start);batches[key].CopyTo(start,upload,0,count);
                windBlock.Clear();windBlock.SetFloat(WindWeightId,WindWeight);windBlock.SetFloat(WindTimeId,WindTime);windBlock.SetFloat(PalmHeightId,meshes[0].bounds.max.y);
                Graphics.DrawMeshInstanced(meshes[lod],0,assets.material,upload,count,windBlock,ShadowCastingMode.Off,true,0,viewer,LightProbeUsage.Off);
                Graphics.DrawMeshInstanced(meshes[Mathf.Max(2,lod)],0,assets.material,upload,count,windBlock,ShadowCastingMode.ShadowsOnly,false,0,viewer,LightProbeUsage.Off);
            }
        }
        void Collect(List<Palm> trees)
        {
            foreach(var palm in trees){var matrix=palm.matrix;var bounds=assets.variants[palm.variant].lods[0].bounds;
                var center=matrix.MultiplyPoint3x4(bounds.center);float scale=matrix.lossyScale.x;
                if(!GeometryUtility.TestPlanesAABB(planes,new Bounds(center,bounds.size*scale+Vector3.one*.9f)))continue;
                float denominator=viewer.orthographic?2*viewer.orthographicSize:2*Mathf.Max(.01f,Vector3.Dot(center-viewer.transform.position,viewer.transform.forward))*Mathf.Tan(viewer.fieldOfView*Mathf.Deg2Rad*.5f);
                float coverage=bounds.size.y*scale/denominator*QualitySettings.lodBias;
                batches[palm.variant*4+(coverage>=.12f?0:coverage>=.055f?1:coverage>=.022f?2:3)].Add(matrix);
            }
        }
    }
}
