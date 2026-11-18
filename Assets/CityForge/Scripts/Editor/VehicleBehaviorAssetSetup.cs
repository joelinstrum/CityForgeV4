#if UNITY_EDITOR
using UnityEditor;
internal static class VehicleBehaviorAssetSetup
{
    [InitializeOnLoadMethod] private static void Queue() => EditorApplication.delayCall += Prepare;
    [MenuItem("CityForge/Lots/Prepare vehicle behavior artwork")]
    private static void Prepare()
    {
        foreach(var file in new[]{"NavigationInactive","NavigationActive","InteriorAtlas","NavigationIcons"})
        {
            string path="Assets/CityForge/Resources/UI/VehicleBehavior/"+file+".png";
            if(AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
            if(importer.maxTextureSize==1024&&!importer.mipmapEnabled&&importer.alphaIsTransparency&&importer.textureCompression==TextureImporterCompression.Compressed)continue;
            importer.textureType=TextureImporterType.Default;importer.maxTextureSize=1024;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode=UnityEngine.FilterMode.Bilinear;importer.isReadable=false;
            importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
        }
    }
}
#endif
