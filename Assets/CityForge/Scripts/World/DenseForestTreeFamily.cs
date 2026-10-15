using UnityEngine;
namespace CityForge.World
{
 public enum DenseForestSeason { SpringSummer, Fall, Winter }
 /// <summary>Forest-only family. One summer asset serves spring and summer.</summary>
 [CreateAssetMenu(menuName="CityForge/Trees/Dense Forest Tree Family")]
 public sealed class DenseForestTreeFamily:ScriptableObject
 {
  public string category="Dense Forest Trees";
  public string[] variantNames;
  public BroadleafGameplayLODLibrary[] springSummer,fall,winter;
  public GameObject[] summerPrefabs,fallPrefabs,winterPrefabs;
  public BroadleafGameplayLODLibrary[] Libraries(DenseForestSeason season)=>season==DenseForestSeason.Winter?winter:season==DenseForestSeason.Fall?fall:springSummer;
  public GameObject[] Prefabs(DenseForestSeason season)=>season==DenseForestSeason.Winter?winterPrefabs:season==DenseForestSeason.Fall?fallPrefabs:summerPrefabs;
 }
}
