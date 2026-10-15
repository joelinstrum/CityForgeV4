using System.IO;using UnityEditor;using UnityEngine;using CityForge.World;
namespace CityForge.Editor {
[InitializeOnLoad]public static class MountainSurfaceTools {
static MountainSurfaceTools(){EditorApplication.update+=Tick;}
static void Tick(){const string request="Library/MountainSurface.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||MountainSurfaceAcceptance.Running||!File.Exists(request))return;var action=File.ReadAllText(request).Trim();if(action=="build"){if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}File.Delete(request);HillsMountainTestTools.Build();return;}EditorApplication.isPaused=false;if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}File.Delete(request);new GameObject("Mountain surface comparison").AddComponent<MountainSurfaceAcceptance>();}
[MenuItem("CityForge/Terrain/Compare Mountain Surface Treatments")]static void Compare(){File.WriteAllText("Library/MountainSurface.request","check");}
}
}
