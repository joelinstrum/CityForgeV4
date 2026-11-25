using System;
using System.Linq;
using UnityEngine;
namespace CityForge.World
{
 [Serializable] internal sealed class EraPedestrianProfile
 {
  [Serializable] internal sealed class Zone {public string zone;public int pedestriansPerBuilding;}
  [Serializable] sealed class Catalog {public EraPedestrianProfile[] profiles;}
  public string eraId;public string[] assetIds;public float walkingFraction=1,cycleSeconds=40;public Zone[] zones;
  static EraPedestrianProfile[] profiles;
  internal static EraPedestrianProfile Find(string era)
  {
   if(profiles==null){var json=Resources.Load<TextAsset>("Config/PedestrianProfiles");profiles=json?JsonUtility.FromJson<Catalog>(json.text)?.profiles??Array.Empty<EraPedestrianProfile>():Array.Empty<EraPedestrianProfile>();}
   return profiles.FirstOrDefault(p=>p.eraId==era);
  }
  internal int Count(TownZone zone)=>Mathf.Clamp(zones?.FirstOrDefault(z=>z.zone==zone.ToString())?.pedestriansPerBuilding??0,0,8);
  internal bool Walking(float phase)=>Mathf.Repeat(phase,Mathf.Max(1,cycleSeconds))<Mathf.Clamp01(walkingFraction)*Mathf.Max(1,cycleSeconds);
 }
}
