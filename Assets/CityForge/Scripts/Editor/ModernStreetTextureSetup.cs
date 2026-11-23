#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using UnityEngine;
using CityForge.Lots;
internal static class ModernStreetTextureSetup
{
    [InitializeOnLoadMethod]private static void Queue()=>EditorApplication.delayCall+=Prepare;
    private static void Prepare()
    {
        _ = LotPathMaterial.All.Length;
        foreach(var id in LotPathMaterial.All.Where(m=>m.Id.StartsWith("artdeco-",System.StringComparison.Ordinal)||m.Id is "weathered-concrete-pavement" or "weathered-charcoal-asphalt").Select(m=>m.Id))
        {
            if(AssetImporter.GetAtPath("Assets/CityForge/Resources/Lots/PathMaterials/"+id+".png") is not TextureImporter t)continue;
            if(t.wrapMode==TextureWrapMode.Repeat&&t.mipmapEnabled&&t.anisoLevel==8&&t.filterMode==FilterMode.Trilinear&&t.sRGBTexture)continue;
            t.wrapMode=TextureWrapMode.Repeat;t.mipmapEnabled=true;t.anisoLevel=8;t.filterMode=FilterMode.Trilinear;t.sRGBTexture=true;t.SaveAndReimport();
        }
    }
}
#endif
