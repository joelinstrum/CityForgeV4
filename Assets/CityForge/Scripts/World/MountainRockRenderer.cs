using System;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;
namespace CityForge.World {
/// <summary>Deterministic scanned outcrops and talus. Spatially bounded instanced batches; resources owned by the project.</summary>
public sealed class MountainRockRenderer:MonoBehaviour {
struct Batch{public Mesh mesh;public Material material;public Matrix4x4[] matrices;public Bounds bounds;}
readonly List<Batch> batches=new();[SerializeField] List<Material> owned=new();public int OutcropCount{get;private set;}public int ScreeCount{get;private set;}public ulong Fingerprint{get;private set;}=14695981039346656037UL;
public void Initialize(MeadowDistrictLandscape land){
var assets=Resources.Load<RiverBedRockAssets>("RiverBeds/RichRockAssets");if(!assets)throw new Exception("Missing scanned rock assets");
var materials=new Material[6];for(int i=0;i<6;i++){
var layer=Resources.Load<TerrainLayer>(i%2==0?"Mountains/CliffDark":"Mountains/CliffBright");var material=new Material(assets.materials[i]){name="Mountain rock surface "+i,enableInstancing=true};
material.SetTexture("_BaseMap",layer.diffuseTexture);material.SetTexture("_BumpMap",layer.normalMapTexture);material.SetColor("_BaseColor",i>=3?new Color(.62f,.66f,.57f):new Color(.77f,.78f,.74f));material.SetFloat("_Smoothness",.05f);material.EnableKeyword("_NORMALMAP");materials[i]=material;owned.Add(material);
}
for(float tz=land.Origin.y;tz<land.Origin.y+land.Side;tz+=128)for(float tx=land.Origin.x;tx<land.Origin.x+land.Side;tx+=128){
var lists=new List<Matrix4x4>[6];for(int i=0;i<6;i++)lists[i]=new();
for(float z=tz+5;z<tz+128;z+=10)for(float x=tx+5;x<tx+128;x+=10){
var rng=land.RandomFor(Mathf.RoundToInt(x/10),Mathf.RoundToInt(z/10),4501);float R()=>(float)rng.NextDouble();var q=new Vector2(x+(R()-.5f)*7,z+(R()-.5f)*7);
if(!land.Contains(q)||land.Height(q)<85||land.RoadDistance(q)<12)continue;
land.MountainSurface(q,out float rock,out float scree);bool outcrop=rock>.70f&&R()<.045f;if(!outcrop&&(scree<.12f||R()>scree*.65f))continue;
int type=rng.Next(3);if(!outcrop&&land.Height(q)<155&&R()<.35f)type+=3;
var mesh=assets.meshes[type];float size=outcrop?Mathf.Lerp(10,24,R()):Mathf.Lerp(.55f,2.2f,R());
float scale=size/Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));
float dx=(land.Height(q+Vector2.right*3)-land.Height(q-Vector2.right*3))/6,dz=(land.Height(q+Vector2.up*3)-land.Height(q-Vector2.up*3))/6;
var normal=new Vector3(-dx,1,-dz).normalized;
var rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler((R()-.5f)*15,R()*360,(R()-.5f)*15);
var pos=new Vector3(q.x,land.Height(q)+(outcrop?-size*.25f:size*.05f),q.y);
var matrix=Matrix4x4.TRS(pos,rotation,new Vector3(scale,scale*(outcrop?1.25f:1),scale))*Matrix4x4.Translate(-mesh.bounds.center);
// Remove the prominent cliff outcrops while preserving the original random sequence and smaller talus placements.
if(outcrop)continue;
lists[type].Add(matrix);ScreeCount++;
for(int c=0;c<16;c++){Fingerprint^=BitConverter.ToUInt32(BitConverter.GetBytes(matrix[c]),0);Fingerprint*=1099511628211UL;}
}
var bounds=new Bounds(new Vector3(tx+64,180,tz+64),new Vector3(170,400,170));
for(int i=0;i<6;i++)for(int start=0;start<lists[i].Count;start+=128){int count=Mathf.Min(128,lists[i].Count-start);batches.Add(new Batch{mesh=assets.meshes[i],material=materials[i],matrices=lists[i].GetRange(start,count).ToArray(),bounds=bounds});}
}
}
void OnDestroy(){foreach(var material in owned)if(material)Destroy(material);}
void LateUpdate(){foreach(var batch in batches)Graphics.RenderMeshInstanced(new RenderParams(batch.material){worldBounds=batch.bounds,shadowCastingMode=ShadowCastingMode.On,receiveShadows=true},batch.mesh,0,batch.matrices);}
}
}
