#if UNITY_EDITOR
using System.IO;using UnityEditor;using UnityEngine;
public static class PathExperienceImport
{
    public static void Configure()
    {
        AssetDatabase.Refresh();CitySoundsImport.Configure();
        foreach(var file in Directory.GetFiles("Assets/CityForge/Resources/Lots/PathBorders","*.png"))
        {var t=(TextureImporter)AssetImporter.GetAtPath(file);t.maxTextureSize=2048;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Repeat;t.anisoLevel=4;t.SaveAndReimport();}
        var header=(TextureImporter)AssetImporter.GetAtPath("Assets/CityForge/Resources/UI/PedestrianPathConcept.png");header.maxTextureSize=2048;header.mipmapEnabled=false;header.wrapMode=TextureWrapMode.Clamp;header.SaveAndReimport();AssetDatabase.SaveAssets();Debug.Log("PATH EXPERIENCE IMPORT PASS");
    }
}
#endif
