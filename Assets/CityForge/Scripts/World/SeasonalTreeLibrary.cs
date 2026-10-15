using UnityEngine;
namespace CityForge.World
{
 public enum RegionSeason { Summer, Fall, Winter }
 public sealed class SeasonalTreeLibrary : ScriptableObject
 {
  // Matching prototype indices retain every tree's location, size and rotation.
  public GameObject[] summer,fall,winter;
  public GameObject[] Prefabs(RegionSeason season)=>season==RegionSeason.Fall?fall:season==RegionSeason.Winter?winter:summer;
 }
}
