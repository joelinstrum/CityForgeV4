#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using CityForge.Lots;
namespace CityForge.EditorTools {
 internal static class SandstoneFootprintSetup {
 const string Root="Assets/CityForge/Resources/Lots/RebuiltBuildings/SandstoneArtDecoTownhouse01/",Marker="Footprint 2x5";
 [Serializable] sealed class Catalog {public float depth;}
 [Serializable] sealed class Prep {public Tier[] tiers;}
 [Serializable] sealed class Tier {public string tier;public Mat[] materials;}
 [Serializable] sealed class Mat {public string name;public float strength;}
 [MenuItem("CityForge/Buildings/Apply Sandstone Townhouse 2x5 Footprint")]
 internal static void Install(){
 if(EditorApplication.isPlaying)throw new InvalidOperationException("Apply footprint revision outside Play mode.");
 var prep=JsonUtility.FromJson<Prep>(File.ReadAllText(Root+"Preparation.json"));
 foreach(string tier in new[]{"Near","Medium","Far"}){
 var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+tier+".fbx");if(!source)throw new InvalidDataException("Missing revised "+tier+" FBX");
 var sourceFilters=source.GetComponentsInChildren<MeshFilter>(true);var newBody=sourceFilters.Length==1?sourceFilters[0]:sourceFilters.Single(f=>f.name=="Townhouse.Structure");
 var recipe=prep.tiers.Single(t=>t.tier==tier);
 foreach(string mode in new[]{"Warm","Off"}){
 string path=Root+tier+(mode=="Warm"?"":"-Off")+".prefab";if(!File.Exists(path))continue;var root=PrefabUtility.LoadPrefabContents(path);
 try{
 var bodyFilters=root.GetComponentsInChildren<MeshFilter>(true);var body=bodyFilters.Length==1?bodyFilters[0]:bodyFilters.Single(f=>f.name=="Townhouse.Structure");body.sharedMesh=newBody.sharedMesh;
 body.GetComponent<Renderer>().sharedMaterials=newBody.GetComponent<Renderer>().sharedMaterials.Select(m=>{var r=recipe.materials.Single(r=>r.name==m.name);var material=AssetDatabase.LoadAssetAtPath<Material>(Root+m.name+(r.strength>0?"-"+mode:"")+".mat");if(!material)throw new InvalidDataException("Missing preserved native material "+m.name);return material;}).ToArray();
 if(!root.transform.Find(Marker)){
 foreach(var transform in root.GetComponentsInChildren<Transform>(true))if((transform.name.StartsWith("Door.")&&transform.name.EndsWith(".Hinge"))||transform.name=="Frontage.Threshold"||transform.name=="Entrance lantern pool")transform.localPosition-=new Vector3(0,0,2);
 var marker=new GameObject(Marker);marker.transform.SetParent(root.transform,false);
 }
 PrefabUtility.SaveAsPrefabAsset(root,path);
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 }
 TownhouseMiniGardenSetup.InstallOne("SandstoneArtDecoTownhouse01");
 string catalogPath=Root+"UnityCatalog.json";string text=File.ReadAllText(catalogPath);text=System.Text.RegularExpressions.Regex.Replace(text,"\\\"depth\\\"\\s*:\\s*24", "\"depth\": 20");File.WriteAllText(catalogPath,text);AssetDatabase.ImportAsset(catalogPath,ImportAssetOptions.ForceSynchronousImport);
 var catalog=AssetDatabase.LoadAssetAtPath<LotAssetCatalog>("Assets/CityForge/Resources/Lots/LocalCatalog.asset");if(!catalog)throw new InvalidDataException("Missing local catalog");var entry=catalog.Find("cf-building-sandstone-artdeco-townhouse-001");if(entry==null)throw new InvalidDataException("Missing sandstone entry");entry.footprint=new Vector2(8,20);entry.buildingDimensionsMeters=new Vector3(8,entry.buildingDimensionsMeters.y,20);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
 }
 }
}
#endif
