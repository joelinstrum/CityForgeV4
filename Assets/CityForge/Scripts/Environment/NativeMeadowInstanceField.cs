using System;using UnityEngine;using UnityEngine.Rendering;
namespace CityForge {
[Serializable]public class NativeMeadowBatch{public Mesh mesh;public Material material;public int submesh;public Bounds bounds;public Matrix4x4[] matrices;[NonSerialized]public Matrix4x4[] distant;}
[ExecuteAlways] public class NativeMeadowInstanceField:MonoBehaviour{
public NativeMeadowBatch[] batches;public Camera presentationCamera;
void LateUpdate(){if(batches==null)return;bool far=presentationCamera&&presentationCamera.orthographic&&presentationCamera.orthographicSize>130;bool culled=presentationCamera&&presentationCamera.orthographic&&presentationCamera.orthographicSize>700;if(culled)return;foreach(var b in batches){if(b.matrices==null||b.matrices.Length==0)continue;if(b.distant==null){b.distant=new Matrix4x4[(b.matrices.Length+3)/4];for(int i=0;i<b.distant.Length;i++)b.distant[i]=b.matrices[i*4];}var rp=new RenderParams(b.material){worldBounds=b.bounds,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true};Graphics.RenderMeshInstanced(rp,b.mesh,b.submesh,far?b.distant:b.matrices);}}
}
}
