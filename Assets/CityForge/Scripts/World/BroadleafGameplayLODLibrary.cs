using UnityEngine;
namespace CityForge.World
{
 public sealed class BroadleafGameplayLODLibrary:ScriptableObject
 {
  public GameObject[] meshTiers;
  public bool honorAuthoredBarkMaterial;
  public Material impostorMaterial;
  public Vector3 center;
  public float span;
  public int rotationViews=8,cameraViews=4;
 }
}
