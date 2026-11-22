using System;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Flat lot caps and retaining walls for raised platforms or cut-down zoning sites.</summary>
    internal sealed class DistrictLotFoundation : MonoBehaviour
    {
        internal const float MaximumRise=12f;
        readonly List<UnityEngine.Object> owned=new();
        readonly List<GameObject> pieces=new();
        internal void Regrade(LotDefinition lot,float level,float offset,Func<Vector2,float> ground)
        {
            foreach(var piece in pieces)if(piece){piece.SetActive(false);Destroy(piece);}pieces.Clear();
            foreach(var asset in owned)if(asset)Destroy(asset);owned.Clear();
            Build(lot,level,offset,ground,true);
        }
        internal static float SampleLevel(Vector2 size,Func<Vector2,Vector2> world,Func<Vector2,float> ground) => SampleLevel(size,world,ground,out _);
        internal static float SampleLevel(Vector2 size,Func<Vector2,Vector2> world,Func<Vector2,float> ground,out float low)
        {
            int nx=Mathf.Max(1,Mathf.CeilToInt(size.x/2)),nz=Mathf.Max(1,Mathf.CeilToInt(size.y/2));float high=float.NegativeInfinity;low=float.PositiveInfinity;
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {float height=ground(world(new Vector2(size.x*((float)x/nx-.5f),size.y*((float)z/nz-.5f))));high=Mathf.Max(high,height);low=Mathf.Min(low,height);}
            return high;
        }
        internal void Build(LotDefinition lot,float level,float offset,Func<Vector2,float> natural,bool cut=false)
        {
            var half=lot.Size*.5f;float low=level;
            Vector2 World(Vector2 p){var v=transform.TransformPoint(new Vector3(p.x,0,p.y));return new Vector2(v.x,v.z);}
            var corners=new[]{new Vector2(-half.x,-half.y),new Vector2(-half.x,half.y),new Vector2(half.x,half.y),new Vector2(half.x,-half.y)};
            SampleLevel(lot.Size,World,natural,out low);
            // Flat ground with no raised lot needs no additional slab or wall draw calls.
            if(!cut && level+offset-low<.12f)return;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();float along=0;
            for(int side=0;side<4;side++)
            {
                var a=corners[side];var b=corners[(side+1)%4];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)));
                for(int i=0;i<steps;i++)
                {
                    var p=Vector2.Lerp(a,b,(float)i/steps);var q=Vector2.Lerp(a,b,(float)(i+1)/steps);float length=Vector2.Distance(p,q);
                    float py=natural(World(p))-offset+(cut?.05f:-.18f),qy=natural(World(q))-offset+(cut?.05f:-.18f);
                    if((!cut && level-Mathf.Min(py,qy)<.12f) || (cut && Mathf.Max(Mathf.Abs(level-py),Mathf.Abs(level-qy))<.12f)){along+=length;continue;}
                    int n=vertices.Count;vertices.Add(new Vector3(p.x,py,p.y));vertices.Add(new Vector3(q.x,qy,q.y));vertices.Add(new Vector3(q.x,level,q.y));vertices.Add(new Vector3(p.x,level,p.y));
                    uv.Add(new Vector2(along/2,py/2));uv.Add(new Vector2((along+length)/2,qy/2));uv.Add(new Vector2((along+length)/2,level/2));uv.Add(new Vector2(along/2,level/2));
                    indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});along+=length;
                }
            }
            if(vertices.Count>0)
            {
                var wall=new Mesh{name="Terrain-fitted retaining walls"};wall.SetVertices(vertices);wall.SetUVs(0,uv);wall.SetTriangles(indices,0);wall.RecalculateNormals();wall.RecalculateTangents();
                var style=LotPathMaterial.Find("mossy-fieldstone");var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Fieldstone retaining wall"};material.SetTexture("_BaseMap",style?.Texture);material.SetColor("_BaseColor",new Color(.72f,.70f,.64f));material.SetFloat("_Smoothness",.08f);if(cut)material.SetFloat("_Cull",0);Add("Fieldstone retaining walls",wall,material);
            }
            if(cut)return; // The actual cut terrain supplies the floor; no extra slab draw call.
            // A real cap covers the fill; existing lot grass, paint and paths draw over it.
            var cap=new Mesh{name="Level earth platform"};cap.vertices=new[]{new Vector3(-half.x,level,-half.y),new Vector3(-half.x,level,half.y),new Vector3(half.x,level,half.y),new Vector3(half.x,level,-half.y)};cap.uv=new[]{Vector2.zero,new Vector2(0,lot.Size.y/4),lot.Size/4,new Vector2(lot.Size.x/4,0)};cap.triangles=new[]{0,1,2,0,2,3};cap.RecalculateNormals();cap.RecalculateTangents();
            var surface=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Leveled lot earth"};surface.SetTexture("_BaseMap",LotGroundTexture.Find(false,"patchy-grass")?.Texture);surface.SetColor("_BaseColor",new Color(.66f,.66f,.53f));surface.SetFloat("_Smoothness",.05f);Add("Level earth platform",cap,surface);
        }
        void Add(string name,Mesh mesh,Material material){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);pieces.Add(go);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;owned.Add(mesh);owned.Add(material);}
        void OnDestroy(){foreach(var asset in owned)if(asset)Destroy(asset);}
    }
    public sealed partial class PrototypeRegionView
    {
        readonly Dictionary<Vector2Int,List<(DistrictLotRecord Lot,DistrictLotFootprint Footprint,float Height)>> platformCells=new();
        void RegisterPlatform(DistrictLotRecord lot,float height)
        {
            var footprint=new DistrictLotFootprint(lot);var bounds=footprint.Bounds;var lo=Vector2Int.FloorToInt(bounds.min/32);var hi=Vector2Int.FloorToInt(bounds.max/32);
            for(int z=lo.y;z<=hi.y;z++)for(int x=lo.x;x<=hi.x;x++){var key=new Vector2Int(x,z);if(!platformCells.TryGetValue(key,out var list))platformCells[key]=list=new();list.RemoveAll(p=>p.Lot==lot);list.Add((lot,footprint,height));}
        }
        bool TryPlatformHeight(Vector2 p,out float height)
        {
            height=0;if(platformCells.TryGetValue(Vector2Int.FloorToInt(p/32),out var list))foreach(var platform in list)if(platform.Footprint.Contains(p,.026f)){height=platform.Height;return true;}return false;
        }
    }
}
