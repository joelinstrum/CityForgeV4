using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForge.World
{
 /// <summary>Runtime lighting guides and property blocks. Source vertices, normals, indices and materials remain unchanged.</summary>
 public sealed class FrozenCrownLighting:MonoBehaviour
 {
  struct Guide{public Vector3 position,normal;public float exposure;}
  readonly Dictionary<Vector3Int,Guide> guides=new();
  readonly Dictionary<Mesh,Mesh> meshes=new();
  Renderer[] foliage;
  public int MatchedVertices{get;private set;} public int MissingVertices{get;private set;}
  static Vector3Int Key(Vector3 p)=>new Vector3Int(Mathf.RoundToInt(p.x*1000),Mathf.RoundToInt(p.y*1000),Mathf.RoundToInt(p.z*1000));
  static float Axis(Vector3 p,int a)=>a==0?p.x:a==1?p.y:p.z;
  public void Initialize(TextAsset guideData)
  {
   using(var b=new BinaryReader(new MemoryStream(guideData.bytes))){int n=b.ReadInt32();for(int i=0;i<n;i++){var g=new Guide{position=new Vector3(b.ReadSingle(),b.ReadSingle(),b.ReadSingle()),normal=new Vector3(b.ReadSingle(),b.ReadSingle(),b.ReadSingle()),exposure=b.ReadSingle()};guides[Key(g.position)]=g;}}
   var list=new List<Renderer>();
   foreach(Transform tree in transform)foreach(var renderer in tree.GetComponentsInChildren<MeshRenderer>())
   {
    if(!renderer.sharedMaterials.Any(m=>m.shader.name=="CityForge/Evaluation/Frozen Blender Leaf"))continue;
    list.Add(renderer);var filter=renderer.GetComponent<MeshFilter>();var source=filter.sharedMesh;
    if(!meshes.TryGetValue(source,out var shaded))
    {
     var vertices=source.vertices;var points=vertices.Select(v=>tree.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray();
     int[] bestAxes=null;Vector3 bestSigns=Vector3.one;int bestScore=-1;
     foreach(var axes in new[]{new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}})for(int bits=0;bits<8;bits++)
     {
      var signs=new Vector3((bits&1)==0?1:-1,(bits&2)==0?1:-1,(bits&4)==0?1:-1);int score=0;
      for(int j=0;j<points.Length;j+=Mathf.Max(1,points.Length/256)){var p=points[j];var q=new Vector3(Axis(p,axes[0])*signs.x,Axis(p,axes[1])*signs.y,Axis(p,axes[2])*signs.z);if(guides.ContainsKey(Key(q)))score++;}
      if(score>bestScore){bestScore=score;bestAxes=axes;bestSigns=signs;}
     }
     if(bestScore<128)throw new InvalidOperationException("Cannot register source lighting guides to imported metric mesh. Score="+bestScore);
     var uv=new List<Vector4>(points.Length);
     foreach(var p in points)
     {
      var q=new Vector3(Axis(p,bestAxes[0])*bestSigns.x,Axis(p,bestAxes[1])*bestSigns.y,Axis(p,bestAxes[2])*bestSigns.z);var key=Key(q);
      if(!guides.TryGetValue(key,out var g))
      {
       float nearest=.000004f;bool found=false;
       for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)if(guides.TryGetValue(key+new Vector3Int(x,y,z),out var c)){float distance=(q-c.position).sqrMagnitude;if(distance<nearest){nearest=distance;g=c;found=true;}}
       if(!found){MissingVertices++;uv.Add(new Vector4(0,1,0,.5f));continue;}
      }
      MatchedVertices++;var rootNormal=Vector3.zero;rootNormal[bestAxes[0]]=g.normal.x*bestSigns.x;rootNormal[bestAxes[1]]=g.normal.y*bestSigns.y;rootNormal[bestAxes[2]]=g.normal.z*bestSigns.z;
      var objectNormal=renderer.transform.InverseTransformDirection(tree.TransformDirection(rootNormal)).normalized;uv.Add(new Vector4(objectNormal.x,objectNormal.y,objectNormal.z,g.exposure));
     }
     shaded=Instantiate(source);shaded.name=source.name+" runtime lobe lighting UV guide";shaded.SetUVs(1,uv);
     if(!source.vertices.SequenceEqual(shaded.vertices) || !source.normals.SequenceEqual(shaded.normals) || !source.triangles.SequenceEqual(shaded.triangles))throw new InvalidOperationException("Lighting metadata changed frozen geometry");
     meshes.Add(source,shaded);
    }
    filter.sharedMesh=shaded;
   }
   foliage=list.ToArray();if(MissingVertices>MatchedVertices*.001f)throw new InvalidOperationException("Too many unmatched shading vertices");
   Debug.Log("Frozen crown lighting guide registration: "+MatchedVertices+" matched; "+MissingVertices+" missing. Source positions/normals/indices unchanged.");
  }
  public void SetProfile(bool enabled,float normalBlend=.70f,float skyFill=.65f)
  {
   var block=new MaterialPropertyBlock();foreach(var renderer in foliage){renderer.GetPropertyBlock(block);block.SetFloat("_FoliageLighting",enabled?1:0);block.SetFloat("_LobeNormalBlend",normalBlend);block.SetFloat("_DiffuseSkyFill",skyFill);renderer.SetPropertyBlock(block);}
  }
  void OnDestroy(){foreach(var mesh in meshes.Values)if(mesh!=null)Destroy(mesh);}
 }
}
