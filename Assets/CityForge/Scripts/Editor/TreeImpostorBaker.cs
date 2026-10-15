using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.World;
namespace CityForge.Editor
{
    public static class TreeImpostorBaker
    {
        [MenuItem("CityForge/Bake Distant Trees")]
        public static void Bake() => Bake(ForestPresentationSettings.Load().resourceFolder);

        public static void Bake(string resourceFolder)
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode before baking trees.");
            const string folder="Assets/CityForge/Art/Trees/";
            var stage=new GameObject("Tree bake stage");
            stage.transform.position=new Vector3(100000,100000,100000);
            var camera=new GameObject("Tree bake camera").AddComponent<Camera>();
            camera.orthographic=true; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=Color.clear; camera.cullingMask=1<<31;
            camera.nearClipPlane=.1f; camera.farClipPlane=200;
            var sun=new GameObject("Tree bake sun").AddComponent<Light>();
            sun.type=LightType.Directional; sun.intensity=1.55f; sun.color=new Color(1,.94f,.82f);
            sun.transform.rotation=Quaternion.Euler(32,205,0); sun.cullingMask=1<<31;
            // Existing scene directional lights must not illuminate the isolated bake a second time.
            var sceneLights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var masks=new int[sceneLights.Length];
            for(int i=0;i<sceneLights.Length;i++) { masks[i]=sceneLights[i].cullingMask; if(sceneLights[i]!=sun) sceneLights[i].cullingMask &= ~(1<<31); }
            var ambient=RenderSettings.ambientLight; var ambientMode=RenderSettings.ambientMode;
            var ambientProbe=RenderSettings.ambientProbe;
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.32f,.35f,.39f);
            var skyFill=new SphericalHarmonicsL2();
            skyFill.AddAmbientLight(new Color(.32f,.35f,.39f));
            skyFill.AddDirectionalLight(Vector3.up,new Color(.30f,.38f,.48f),.65f);
            skyFill.AddDirectionalLight(Vector3.down,new Color(.22f,.19f,.14f),.35f);
            RenderSettings.ambientProbe=skyFill;
            var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);
            var previousRT=RenderTexture.active;
            try
            {
                foreach(var prefab in Resources.LoadAll<GameObject>(resourceFolder))
                {
                    var model=Object.Instantiate(prefab,stage.transform);
                    foreach(var t in model.GetComponentsInChildren<Transform>()) t.gameObject.layer=31;
                    var lod=model.GetComponent<LODGroup>(); lod.ForceLOD(0);
                    var renderers=lod.GetLODs()[0].renderers;
                    var bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
                    var center=bounds.center-stage.transform.position;
                    var span=Mathf.Max(bounds.size.y,bounds.size.x,bounds.size.z)*1.5f;
                    camera.orthographicSize=span/2; camera.targetTexture=rt;
                    var atlas=new Texture2D(2048,256,TextureFormat.RGBA32,false);
                    var tile=new Texture2D(256,256,TextureFormat.RGBA32,false);
                    for(int angle=0;angle<8;angle++)
                    {
                        camera.transform.rotation=Quaternion.Euler(35.264f,angle*45,0);
                        camera.transform.position=bounds.center-camera.transform.forward*70;
                        camera.Render(); RenderTexture.active=rt;
                        tile.ReadPixels(new Rect(0,0,256,256),0,0);tile.Apply();
                        atlas.SetPixels(angle*256,0,256,256,tile.GetPixels());
                    }
                    atlas.Apply(); var path=folder+prefab.name+"_Impostor.png";
                    File.WriteAllBytes(path,atlas.EncodeToPNG()); AssetDatabase.ImportAsset(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    importer.alphaIsTransparency=true; importer.mipmapEnabled=true;
                    importer.mipMapsPreserveCoverage=true; importer.alphaTestReferenceValue=.25f;
                    importer.wrapMode=TextureWrapMode.Clamp; importer.textureCompression=TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    var materialPath=folder+prefab.name+"_Impostor.mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(material==null) {material=new Material(Shader.Find("CityForge/Distant Tree")); AssetDatabase.CreateAsset(material,materialPath);}
                    material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                    EditorUtility.SetDirty(material);
                    var prefabPath=AssetDatabase.GetAssetPath(prefab);
                    var contents=PrefabUtility.LoadPrefabContents(prefabPath);
                    try {
                        var data=contents.GetComponent<TreeImpostor>() ?? contents.AddComponent<TreeImpostor>();
                        data.material=material;data.center=center;data.span=span;
                        PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);
                    } finally {PrefabUtility.UnloadPrefabContents(contents);}
                    Object.DestroyImmediate(model);Object.DestroyImmediate(tile);Object.DestroyImmediate(atlas);
                    Debug.Log("CityForge: baked eight distant views for "+prefab.name);
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                RenderTexture.active=previousRT; camera.targetTexture=null; rt.Release();Object.DestroyImmediate(rt);
                RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=ambientMode;RenderSettings.ambientProbe=ambientProbe;
                for(int i=0;i<sceneLights.Length;i++) if(sceneLights[i]!=null) sceneLights[i].cullingMask=masks[i];
                Object.DestroyImmediate(stage);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(sun.gameObject);
            }
        }
    }
}
