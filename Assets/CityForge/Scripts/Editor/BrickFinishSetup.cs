#if UNITY_EDITOR
using UnityEditor;
namespace CityForge.EditorTools {
 // Keep existing menu/automation entry points on the reviewed supplied texture set.
 internal static class BrickFinishSetup {
  [MenuItem("CityForge/Buildings/Refresh Fixed Brick Finishes")]
  internal static void Install()=>SuppliedBrickFinishSetup.Install();
  [MenuItem("CityForge/Buildings/Refresh Yellow Brick Finish")]
  internal static void InstallYellow()=>SuppliedBrickFinishSetup.Install();
 }
}
#endif
