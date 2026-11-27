#if UNITY_EDITOR
using UnityEditor;
namespace CityForge.EditorTools {
 internal static class SteppedGardenGreenerySetup {
 [MenuItem("CityForge/Buildings/Install Low Poly Garden Greenery")]
 internal static void Install()=>TownhouseMiniGardenSetup.InstallOne("SteppedBrickTownhouse01");
 }
}
#endif
