using UnityEngine;
using WaveHarmonic.Crest;
namespace CityForge.World {
// Slow only this ocean's waves, foam simulation and shoreline animation.
public sealed class CoastalWaterClock : MonoBehaviour, ITimeProvider {
 public const float Speed=.45f;
 WaterRenderer water;
 CityForge.CameraSystem.CityForgeCameraController rig;
 Transform focus;
 public float Time=>UnityEngine.Time.time*Speed;
 public float Delta=>UnityEngine.Time.deltaTime*Speed;
 public void Initialize(WaterRenderer renderer){
  water=renderer;
  var land=GetComponentInParent<MeadowDistrictLandscape>();
  if(land&&RegionalRiverNetwork.HasGroundFollowingWater(land.View.Data)){
   rig=renderer.Viewer.GetComponent<CityForge.CameraSystem.CityForgeCameraController>();
   focus=new GameObject("Coastal channel simulation focus").transform;focus.SetParent(transform,false);
   water.Viewpoint=focus;UpdateFocus();
  }
 }
 void UpdateFocus(){
  if(!focus||!rig||!water)return;
  var state=rig.CaptureRegionState();
  focus.position=new Vector3(state.focus.x,water.SeaLevel+Mathf.Max(8,state.size*.3f),state.focus.z);
 }
 void LateUpdate(){
  UpdateFocus();
  // Crest initializes its provider stack when enabled. During staged loading or
  // teardown the renderer can exist before that stack has a default provider.
  if(!water||!water.isActiveAndEnabled||water.TimeProviders.Count==0)return;
  if(water.TimeProvider!=this)water.TimeProviders.Push(this);
 }
 void OnDisable(){if(water)water.TimeProviders.Pop(this);}
 void OnDestroy(){if(water)water.TimeProviders.Pop(this);}
}
}
