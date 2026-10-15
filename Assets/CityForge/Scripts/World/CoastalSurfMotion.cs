using UnityEngine;
namespace CityForge.World {
// Explicit time avoids depending on camera shader globals during Crest's simulation pass.
public sealed class CoastalSurfMotion : MonoBehaviour {
 Material material;
 public void Initialize(Material surface){material=surface;}
 void Update(){if(material)material.SetFloat("_SurfTime",Time.time*CoastalWaterClock.Speed);}
}
}
