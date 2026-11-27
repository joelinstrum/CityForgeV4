#if UNITY_EDITOR
using UnityEditor;
namespace CityForge.EditorTools {
 internal static class SteppedGardenFlowerSetup {
 [MenuItem("CityForge/Buildings/Install Stepped Townhouse White Garden")]
 internal static void Install()=>TownhouseMiniGardenSetup.InstallOne("SteppedBrickTownhouse01");
 }
}
#endif
