using System.Collections.Generic;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    /// <summary>One NE woodland experiment. Crown centers follow the existing tree transforms.</summary>
    public sealed class WoodlandCanopyRenderer : MonoBehaviour
    {
        private readonly HashSet<TreeImpostor> members = new();
        private readonly List<Mesh> meshes = new();
        private readonly List<Material> materials = new();
        private GameObject canopy;
        private Texture2D foliage,foliageNormal;
        private PrototypeRegionView view;
        private int count = -1;
        public int MemberCount => members.Count;
        public int TriangleCount { get; private set; }
        public bool IsVisible => canopy != null && canopy.activeSelf;
        public bool Owns(TreeImpostor tree) => members.Contains(tree);

        public void Refresh(IReadOnlyList<TreeImpostor> trees, int zoom, bool diagnostic)
        {
            if (view == null) view = GetComponent<PrototypeRegionView>();
            if (count != trees.Count) Build(trees);
            canopy.SetActive(!diagnostic && zoom >= 3 && (view.SelectedDistrict < 0 || view.SelectedDistrict == 3));
        }

        private void Build(IReadOnlyList<TreeImpostor> trees)
        {
            if (canopy != null) Destroy(canopy);
            foreach (var mesh in meshes) Destroy(mesh);
            foreach (var ownedMaterial in materials) Destroy(ownedMaterial);
            meshes.Clear(); materials.Clear(); members.Clear(); TriangleCount = 0;
            canopy = new GameObject("Northeast woodland canopy prototype");
            canopy.transform.SetParent(transform, false);
            var stems=new List<Vector3>();var stemNormals=new List<Vector3>();var stemIndices=new List<int>();
            var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var uv = new List<Vector2>(); var indices = new List<int>();
            foreach (var tree in trees)
            {
                var position = tree.transform.position;
                if (PrototypeRegionView.DistrictAt(position) != 3 || tree.name.Contains("Fir")) continue;
                // Limit the experiment to the existing NE stand; retain outlying trees.
                var p = new Vector2(position.x, position.z) / view.MapSide;
                var a = new Vector2(.19f,.29f); var axis = new Vector2(.13f,.04f);
                var nearest = a + axis * Mathf.Clamp01(Vector2.Dot(p-a,axis)/axis.sqrMagnitude);
                if (Vector2.Distance(p,nearest) > .049f) continue;
                members.Add(tree);
                var height = tree.span / 1.5f * tree.transform.lossyScale.y;
                var stemStart=stems.Count;
                for(var ring=0;ring<2;ring++) for(var segment=0;segment<6;segment++)
                {
                    var angle=segment*Mathf.PI/3;
                    var normal=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    stems.Add(position+Vector3.up*height*(ring==0 ? 0 : .58f)+normal*height*(ring==0 ? .035f : .018f));stemNormals.Add(normal);
                    if(ring==0)
                    {
                        var i=stemStart+segment;var next=stemStart+(segment+1)%6;
                        stemIndices.AddRange(new[]{i,i+6,next,next,i+6,next+6});
                    }
                }
                var crown = position + Vector3.up * height * .66f;
                var phase = position.x * .73f + position.z * .41f;
                for (var lobe = 0; lobe < 3; lobe++)
                {
                    var angle = phase + lobe * 2.094f;
                    var offset = new Vector3(Mathf.Cos(angle), .12f*Mathf.Sin(angle*2), Mathf.Sin(angle)) * height * .18f;
                    AddCrown(crown+offset, new Vector3(height*.56f,height*.34f,height*.52f),phase+lobe,vertices,normals,uv,indices);
                }
            }
            var combined = new Mesh { name = "Overlapping woodland crowns", indexFormat = IndexFormat.UInt32 };
            combined.SetVertices(vertices); combined.SetNormals(normals); combined.SetUVs(0,uv);
            combined.SetTriangles(indices,0); combined.RecalculateBounds(); combined.RecalculateTangents(); meshes.Add(combined);
            TriangleCount = indices.Count / 3;
            canopy.AddComponent<MeshFilter>().sharedMesh = combined;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (foliage == null)
            {
                foliage = new Texture2D(128,128,TextureFormat.RGB24,true);
                foliage.wrapMode=TextureWrapMode.Repeat;
                var pixels=new Color[128*128];
                for(var y=0;y<128;y++) for(var x=0;x<128;x++)
                {
                    var grain=Mathf.PerlinNoise(x*.28f,y*.28f)*.7f+Mathf.PerlinNoise(x*.07f+13,y*.07f+7)*.3f;
                    pixels[y*128+x]=Color.Lerp(new Color(.38f,.47f,.19f),new Color(.58f,.64f,.32f),grain);
                }
                foliage.SetPixels(pixels);foliage.Apply(true,true);
                foliageNormal=new Texture2D(128,128,TextureFormat.RGBA32,true,true);
                foliageNormal.wrapMode=TextureWrapMode.Repeat;
                for(var y=0;y<128;y++) for(var x=0;x<128;x++)
                {
                    var dx=(Mathf.PerlinNoise((x+1)*.28f,y*.28f)-Mathf.PerlinNoise((x-1)*.28f,y*.28f))*2.2f;
                    var dy=(Mathf.PerlinNoise(x*.28f,(y+1)*.28f)-Mathf.PerlinNoise(x*.28f,(y-1)*.28f))*2.2f;
                    var n=new Vector3(-dx,-dy,1).normalized;
                    pixels[y*128+x]=new Color(1,n.y*.5f+.5f,n.z*.5f+.5f,n.x*.5f+.5f);
                }
                foliageNormal.SetPixels(pixels);foliageNormal.Apply(true,true);
            }
            material.SetTexture("_BaseMap",foliage);
            material.SetColor("_BaseColor",Color.white);
            material.SetTexture("_BumpMap",foliageNormal);material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",1);
            material.SetFloat("_Smoothness",.08f); materials.Add(material);
            var renderer = canopy.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            var stemMesh=new Mesh {name="Canopy support trunks",indexFormat=IndexFormat.UInt32};
            stemMesh.SetVertices(stems);stemMesh.SetNormals(stemNormals);stemMesh.SetTriangles(stemIndices,0);stemMesh.RecalculateBounds();meshes.Add(stemMesh);
            var stemRoot=new GameObject("Simple trunks");stemRoot.transform.SetParent(canopy.transform,false);
            stemRoot.AddComponent<MeshFilter>().sharedMesh=stemMesh;
            var bark=new Material(Shader.Find("Universal Render Pipeline/Lit"));bark.SetColor("_BaseColor",new Color(.23f,.19f,.13f));bark.SetFloat("_Smoothness",0);materials.Add(bark);
            stemRoot.AddComponent<MeshRenderer>().sharedMaterial=bark;
            TriangleCount+=stemIndices.Count/3;
            count = trees.Count;
            Debug.Log($"CityForge canopy prototype: {MemberCount} existing deciduous trees, {TriangleCount} triangles, one crown batch plus simple trunks.");
        }

        private static void AddCrown(Vector3 center,Vector3 radius,float phase,List<Vector3> vertices,List<Vector3> normals,List<Vector2> uv,List<int> indices)
        {
            const int rings=6, segments=10;
            var start=vertices.Count;
            for(var ring=0;ring<=rings;ring++)
            for(var segment=0;segment<=segments;segment++)
            {
                var latitude=Mathf.PI*ring/rings; var longitude=2*Mathf.PI*segment/segments;
                var direction=new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude));
                var irregularity=1+.10f*Mathf.Sin(longitude*3+phase)*Mathf.Sin(latitude)+.05f*Mathf.Sin(latitude*4+phase);
                vertices.Add(center+Vector3.Scale(direction,radius)*irregularity);
                normals.Add(new Vector3(direction.x/radius.x,direction.y/radius.y,direction.z/radius.z).normalized);
                uv.Add(new Vector2(vertices[vertices.Count-1].x,vertices[vertices.Count-1].z)*.32f);
                if(ring==rings || segment==segments) continue;
                var i=start+ring*(segments+1)+segment;
                indices.AddRange(new[]{i,i+1,i+segments+1,i+1,i+segments+2,i+segments+1});
            }
        }
        private void OnDestroy()
        {
            if(foliage!=null) Destroy(foliage);
            if(foliageNormal!=null) Destroy(foliageNormal);
            foreach(var mesh in meshes) Destroy(mesh);
            foreach(var material in materials) Destroy(material);
        }
    }
}
