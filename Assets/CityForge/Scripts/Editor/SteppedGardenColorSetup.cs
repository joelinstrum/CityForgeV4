#if UNITY_EDITOR
using UnityEditor;
namespace CityForge.EditorTools {
 internal static class SteppedGardenColorSetup {
 [MenuItem("CityForge/Buildings/Refresh Stepped Garden Colors")]
 internal static void Install()=>TownhouseMiniGardenSetup.InstallOne("SteppedBrickTownhouse01");
 }
}
#endif
