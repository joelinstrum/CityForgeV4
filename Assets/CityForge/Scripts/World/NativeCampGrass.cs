using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.CameraSystem;
namespace CityForge.World {
/// <summary>Sparse short native meadow grass at the worn courtyard margins, never a central lawn.</summary>
public sealed class NativeCampGrass:MonoBehaviour {
 readonly List<(MeadowPlantPiece piece,Matrix4x4[] matrices)> batches=new();
 CityForgeCameraController rig;Bounds bounds;
 public int TuftCount {get;private set;}
 public void Initialize(MeadowDistrictLandscape land,NativeCampRecord camp,IReadOnlyList<Bounds> obstacles){
  rig=land.View.GetComponentInChildren<CityForgeCameraController>();
  bounds=new Bounds(new Vector3(camp.center.x,land.Height(camp.center)+2,camp.center.y),new Vector3(camp.Clearance*2,16,camp.Clearance*2));
  var rng=new System.Random(unchecked(camp.seed+6719));float R()=>(float)rng.NextDouble();
  for(int type=0;type<2&&type<land.Baseline.plants.Length;type++){
   var placements=new List<Matrix4x4>();
   for(int attempt=0;attempt<180&&placements.Count<32;attempt++){
    float angle=R()*Mathf.PI*2,radius=Mathf.Lerp(.51f,.94f,R())*camp.Radius;
    var p=camp.center+new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*radius;
    float wear=MeadowDistrictLandscape.CampGroundWear(camp,p);
    if(wear<.2f||wear>.88f||!land.Contains(p))continue;
    var point=new Vector3(p.x,land.Height(p),p.y);bool blocked=false;
    foreach(var obstacle in obstacles){var b=obstacle;b.Expand(.7f);if(b.Contains(point+Vector3.up*.4f)){blocked=true;break;}}
    if(blocked)continue;
    float scale=Mathf.Lerp(.55f,.85f,R());placements.Add(Matrix4x4.TRS(point,Quaternion.Euler(0,R()*360,0),new Vector3(scale,scale*.38f,scale)));TuftCount++;
   }
   foreach(var piece in land.Baseline.plants[type].pieces){var matrices=new Matrix4x4[placements.Count];for(int i=0;i<matrices.Length;i++)matrices[i]=placements[i]*piece.local;batches.Add((piece,matrices));}
  }
 }
 void LateUpdate(){
  if(!rig||rig.ZoomLevel>2)return;
  foreach(var batch in batches)if(batch.matrices.Length>0){var p=new RenderParams(batch.piece.material){worldBounds=bounds,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true};Graphics.RenderMeshInstanced(p,batch.piece.mesh,batch.piece.submesh,batch.matrices);}
 }
}
}
