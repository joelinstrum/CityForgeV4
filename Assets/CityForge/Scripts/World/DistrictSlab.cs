using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World {
public sealed partial class MeadowDistrictLandscape {
 void BuildDistrictSlab(){
  if(IsRegionOverview)return;
  var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
  var members=new HashSet<int>(View.ActiveDistrictMembers);float cellSide=View.DistrictSide;
  float floor=float.PositiveInfinity;foreach(float h in View.Data.Heights)floor=Mathf.Min(floor,h*terrainData.size.y);
  // A common base avoids seams and stepped bottoms around a merged district.
  float bottom=floor-Mathf.Max(10,cellSide*.02f);
  void Edge(Vector2 a,Vector2 b,int facing){
   const int segments=64;
   float Top(Vector2 p)=>Mathf.Max(Height(p),View.Waterless?float.NegativeInfinity:View.SeaLevel);
   for(int j=0;j<segments;j++){
    var p=Vector2.Lerp(a,b,j/(float)segments);var q=Vector2.Lerp(a,b,(j+1f)/segments);int k=vertices.Count;
    vertices.Add(new Vector3(p.x,Top(p),p.y));vertices.Add(new Vector3(q.x,Top(q),q.y));vertices.Add(new Vector3(p.x,bottom,p.y));vertices.Add(new Vector3(q.x,bottom,q.y));
    float shade=facing%2==0?.85f:1;var upper=new Color(.34f,.25f,.115f,1)*shade;var lower=new Color(.19f,.135f,.065f,1)*shade;upper.a=lower.a=1;
    colors.Add(upper);colors.Add(upper);colors.Add(lower);colors.Add(lower);
    triangles.Add(k);triangles.Add(k+1);triangles.Add(k+2);triangles.Add(k+1);triangles.Add(k+3);triangles.Add(k+2);
   }
  }
  foreach(int cell in members){int x=cell%View.GridSide,z=cell/View.GridSide;var a=View.DistrictOrigin(cell);var b=a+Vector2.one*cellSide;
   if(z==0||!members.Contains(cell-View.GridSide))Edge(a,new Vector2(b.x,a.y),0);
   if(x==View.GridSide-1||!members.Contains(cell+1))Edge(new Vector2(b.x,a.y),b,1);
   if(z==View.GridSide-1||!members.Contains(cell+View.GridSide))Edge(b,new Vector2(a.x,b.y),2);
   if(x==0||!members.Contains(cell-1))Edge(new Vector2(a.x,b.y),a,3);
  }
  var mesh=Own(new Mesh{name="District exterior earth slab",indexFormat=IndexFormat.UInt32});mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
  var go=new GameObject("District earth slab");go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Own(new Material(Resources.Load<Shader>("Region/RegionalSlab")));renderer.shadowCastingMode=ShadowCastingMode.Off;
 }
}
}
