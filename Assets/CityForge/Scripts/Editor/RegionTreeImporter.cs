using System.IO;
using UnityEngine;
using UnityEditor;

namespace CityForge.Editor
{
    public static class RegionTreeImporter
    {
        private const string Art = "Assets/CityForge/Art/Trees/";
        [System.Serializable] private class TreeSpec { public string name; public float height; public MaterialSpec[] materials; }
        [System.Serializable] private class MaterialSpec { public string name; public bool normal, cutout; }

        [MenuItem("CityForge/Import Region Trees")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var count = 0;
            foreach (var manifest in Directory.GetFiles(Art,"*.json"))
            {
                if (manifest.Contains("_LOD1")) continue;
                var spec = JsonUtility.FromJson<TreeSpec>(File.ReadAllText(manifest));
                // Remove a temporary import object left by an interrupted import.
                foreach(var temporary in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    if(temporary.name==spec.name && temporary.GetComponentInChildren<Renderer>()!=null)
                        Object.DestroyImmediate(temporary);
                var root = new GameObject(spec.name);
                try
                {
                var detailed = ImportModel(spec.name, root, spec);
                var bounds = BoundsOf(detailed);
                var normalization = spec.height / bounds.size.y;
                detailed.transform.localScale *= normalization;
                bounds = BoundsOf(detailed);
                var offset = new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                detailed.transform.position -= offset;
                var distant = ImportModel(spec.name+"_LOD1",root,spec);
                distant.transform.localScale *= normalization;
                distant.transform.position -= offset;
                var lod = root.AddComponent<LODGroup>();
                lod.SetLODs(new[] {
                    new LOD(.12f,detailed.GetComponentsInChildren<Renderer>()),
                    new LOD(.001f,distant.GetComponentsInChildren<Renderer>()) });
                lod.RecalculateBounds();
                var existing=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Resources/Trees/"+spec.name+".prefab");
                var baked=existing!=null ? existing.GetComponent<CityForge.World.TreeImpostor>() : null;
                if(baked!=null) EditorUtility.CopySerialized(baked,root.AddComponent<CityForge.World.TreeImpostor>());
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/CityForge/Resources/Trees/"+spec.name+".prefab");
                count++;
                }
                finally { Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"CityForge: imported {count} tree variants with original foliage materials and two mesh detail levels.");
        }

        private static Bounds BoundsOf(GameObject model)
        {
            var renderers=model.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static GameObject ImportModel(string name,GameObject root,TreeSpec spec)
        {
            var path=Art+name+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            instance.transform.SetParent(root.transform,true);
            var manifest=JsonUtility.FromJson<TreeSpec>(File.ReadAllText(Art+name+".json"));
            var materials=new System.Collections.Generic.Dictionary<string,Material>();
            foreach(var definition in manifest.materials) materials[definition.name]=MakeMaterial(definition);
            foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>())
            {
                var slots=renderer.sharedMaterials;
                for(var i=0;i<slots.Length;i++)
                {
                    if(slots[i]==null || !materials.TryGetValue(slots[i].name,out var replacement))
                        throw new System.InvalidOperationException("Tree material mapping missing: "+name+" / "+slots[i]?.name);
                    slots[i]=replacement;
                }
                renderer.sharedMaterials=slots;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            }
            return instance;
        }

        private static Material MakeMaterial(MaterialSpec spec)
        {
            var path=Art+spec.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) {material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+spec.name+".png"));
            material.SetColor("_BaseColor",Color.white);
            if(spec.normal)
            {
                var normalPath=Art+spec.name+"_Normal.png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(normalPath);
                if(importer.textureType!=TextureImporterType.NormalMap)
                {importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Smoothness",.12f); material.enableInstancing=true;
            if(spec.cutout)
            {
                var foliageImporter=(TextureImporter)AssetImporter.GetAtPath(Art+spec.name+".png");
                if(!foliageImporter.alphaIsTransparency || !foliageImporter.mipMapsPreserveCoverage ||
                    !Mathf.Approximately(foliageImporter.alphaTestReferenceValue,.3f))
                {
                    foliageImporter.alphaIsTransparency=true;
                    foliageImporter.mipMapsPreserveCoverage=true;
                    foliageImporter.alphaTestReferenceValue=.3f;
                    foliageImporter.SaveAndReimport();
                }
                material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.3f);
                material.SetFloat("_Cull",0);material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;
            }
            EditorUtility.SetDirty(material);return material;
        }
    }
}
