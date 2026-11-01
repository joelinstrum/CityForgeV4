using UnityEngine;
namespace CityForge.World {
/// <summary>Opt-in stocking. The default path never loads or instantiates fish assets.</summary>
public sealed class StreamFishOption:MonoBehaviour {
 const string Preference="CityForge.StreamFish.Enabled";
 MeadowDistrictLandscape land;StreamFishStudy population;
 public bool FishEnabled=>PlayerPrefs.GetInt(Preference,0)==1;
 public void Initialize(MeadowDistrictLandscape source){land=source;if(FishEnabled)Spawn();}
 public void SetFishEnabled(bool value){
  PlayerPrefs.SetInt(Preference,value?1:0);PlayerPrefs.Save();
  if(value)Spawn();else if(population){population.Shutdown();Destroy(population);population=null;}
 }
 void Spawn(){if(population||!land)return;population=gameObject.AddComponent<StreamFishStudy>();population.Initialize(land);}
}
}
