using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Local material study above the existing bed. Does not repaint or save terrain.</summary>
    public sealed class CrestRiverBedPreview : MonoBehaviour
    {
        Mesh mesh;Material material;
        public void Initialize(MeadowDistrictLandscape land,float level)
        {
            var layer=Resources.Load<TerrainLayer>("RiverBeds/RichPebbles");if(!layer||!layer.diffuseTexture)return;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            int cells=Mathf.CeilToInt(land.Side/2);float step=land.Side/cells;
            var heights=new float[cells+1,cells+1];for(int z=0;z<=cells;z++)for(int x=0;x<=cells;x++)heights[z,x]=land.Height(land.Origin+new Vector2(x,z)*step);
            var terrain=land.DisplayTerrain;var origin=terrain.transform.position;var terrainSize=terrain.terrainData.size;
            for(int z=0;z<cells;z++)for(int x=0;x<cells;x++){
                float height=(heights[z,x]+heights[z+1,x]+heights[z,x+1]+heights[z+1,x+1])*.25f;
                if(height>level+.15f)continue;
                int first=vertices.Count;
                foreach(var offset in new[]{new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(1,0),new Vector2Int(1,1)}){
                    int xx=x+offset.x,zz=z+offset.y;var p=land.Origin+new Vector2(xx,zz)*step;
                    vertices.Add(new Vector3(p.x,heights[zz,xx]+.035f,p.y));uv.Add(p/Mathf.Max(.5f,layer.tileSize.x));normals.Add(terrain.terrainData.GetInterpolatedNormal((p.x-origin.x)/terrainSize.x,(p.y-origin.z)/terrainSize.z));
                }
                triangles.AddRange(new[]{first,first+1,first+2,first+2,first+1,first+3});
            }
            mesh=new Mesh{name="Shallow riverbed preview",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetTexture("_BaseMap",layer.diffuseTexture);material.SetColor("_BaseColor",new Color(layer.diffuseRemapMax.x,layer.diffuseRemapMax.y,layer.diffuseRemapMax.z,1));material.SetFloat("_Smoothness",.28f);
            if(layer.normalMapTexture){material.SetTexture("_BumpMap",layer.normalMapTexture);material.SetFloat("_BumpScale",.6f);material.EnableKeyword("_NORMALMAP");}
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
