using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    // Two joined masonry retaining walls, rebuilt from the saved canal stations/grades.
    internal sealed class CanalBankRenderer : MonoBehaviour
    {
        private Mesh mesh;
        private Material material;
        internal void Initialize(MeadowDistrictLandscape land, RegionWaterBody body)
        {
            var points = new List<Vector2>();
            foreach (var p in body.centerline) points.Add((p - Vector2.one * .5f) * land.View.MapSide);
            float width = body.halfWidthNormalized * land.View.MapSide * 2f;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (!land.Contains((points[i] + points[i + 1]) * .5f)) continue;
                var a = points[i] + LotPathSurface.RibbonSide(points, null, false, i, width + .35f) * side;
                var b = points[i + 1] + LotPathSurface.RibbonSide(points, null, false, i + 1, width + .35f) * side;
                var normal = Vector2.Perpendicular((b - a).normalized) * .35f * side;
                float bottomA = body.playerChannelLevels[i] - body.playerChannelDepth;
                float bottomB = body.playerChannelLevels[i + 1] - body.playerChannelDepth;
                float topA = Mathf.Max(body.playerChannelLevels[i] + .75f, land.Height(a) + .2f);
                float topB = Mathf.Max(body.playerChannelLevels[i + 1] + .75f, land.Height(b) + .2f);
                Quad(P(a,bottomA),P(b,bottomB),P(b,topB),P(a,topA),side>0);
                Quad(P(a,topA),P(b,topB),P(b+normal,topB),P(a+normal,topA),side>0);
                Quad(P(b+normal,bottomB),P(a+normal,bottomA),P(a+normal,topA),P(b+normal,topB),side>0);
            }
            mesh = new Mesh { name = "Joined canal masonry banks", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Canal bank masonry" };
            material.SetTexture("_BaseMap", LotPathMaterial.Find(LotPathMaterial.Bluestone).Texture);
            material.SetColor("_BaseColor", new Color(.58f,.56f,.49f)); material.SetFloat("_Smoothness",.12f);
            material.SetFloat("_Cull",0);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            Vector3 P(Vector2 p,float height) => new(p.x,height,p.y);
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool reverse)
            {
                int start = vertices.Count; vertices.AddRange(new[]{a,b,c,d});
                float length = Vector3.Distance(a,b)/2f, height = Mathf.Max(.2f,Vector3.Distance(a,d))/2f;
                uv.AddRange(new[]{Vector2.zero,new Vector2(length,0),new Vector2(length,height),new Vector2(0,height)});
                triangles.AddRange(reverse ? new[]{start,start+2,start+1,start,start+3,start+2} : new[]{start,start+1,start+2,start,start+2,start+3});
            }
        }
        private void OnDestroy() { if(mesh)Destroy(mesh); if(material)Destroy(material); }
    }
}
