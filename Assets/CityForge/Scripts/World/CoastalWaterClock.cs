using UnityEngine;
using WaveHarmonic.Crest;
namespace CityForge.World {
// Slow only this ocean's waves, foam simulation and shoreline animation.
public sealed class CoastalWaterClock : MonoBehaviour, ITimeProvider {
 public const float Speed=.45f;
 WaterRenderer water;
 public float Time=>UnityEngine.Time.time*Speed;
 public float Delta=>UnityEngine.Time.deltaTime*Speed;
 public void Initialize(WaterRenderer renderer){water=renderer;}
 void LateUpdate(){
  // Crest initializes its provider stack when enabled. During staged loading or
  // teardown the renderer can exist before that stack has a default provider.
  if(!water||!water.isActiveAndEnabled||water.TimeProviders.Count==0)return;
  if(water.TimeProvider!=this)water.TimeProviders.Push(this);
 }
 void OnDisable(){if(water)water.TimeProviders.Pop(this);}
 void OnDestroy(){if(water)water.TimeProviders.Pop(this);}
}
}
