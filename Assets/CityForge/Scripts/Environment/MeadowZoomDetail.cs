using UnityEngine;
using CityForge.CameraSystem;
namespace CityForge.Environment {
[ExecuteAlways] public sealed class MeadowZoomDetail : MonoBehaviour {
public CityForgeCameraController cameraRig;public Mesh nearMesh,wideMesh;public Material[] nearMaterials,wideMaterials;int last=-1;
void OnEnable(){last=-1;}
void LateUpdate(){if(!cameraRig)return;int level=cameraRig.ZoomLevel==0?0:1;if(last==level)return;last=level;GetComponent<MeshFilter>().sharedMesh=level==0?nearMesh:wideMesh;GetComponent<MeshRenderer>().sharedMaterials=level==0?nearMaterials:wideMaterials;}
}}
