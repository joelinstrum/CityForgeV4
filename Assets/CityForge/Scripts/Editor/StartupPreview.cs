using UnityEditor;
using UnityEditor.SceneManagement;

namespace CityForge.Editor
{
    [InitializeOnLoad]
    internal static class StartupPreview
    {
        private const string StartupPath = "Assets/CityForge/Scenes/Gameplay/Startup.unity";
        private const string MenuPath = "Assets/CityForge/Scenes/Gameplay/MainMenu.unity";

        static StartupPreview()
        {
            EditorApplication.delayCall += SetPlayStartScene;
            EditorSceneManager.activeSceneChangedInEditMode += (_, _) => SetPlayStartScene();
        }

        private static void SetPlayStartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string current = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.playModeStartScene = current == MenuPath
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(StartupPath)
                : null;
        }
    }
}
