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
 void LateUpdate(){if(water&&water.TimeProvider!=this)water.TimeProviders.Push(this);}
 void OnDestroy(){if(water)water.TimeProviders.Pop(this);}
}
}
