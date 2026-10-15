#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using CityForge.World;
namespace CityForge.EditorTools
{
[InitializeOnLoad] public static class SeasonAssetTools
{
 const string Work="/Users/joelinstrum/Documents/Codex/2026-10-12/begin-from-this-handoff-and-start/work/";
 [Serializable] public class MeshRecord {public string id,name;public Vector3[] vertices;public int[] triangles;public int[] leafFaces;}
 [Serializable] public class Manifest {public List<MeshRecord> meshes=new();}
 static SeasonAssetTools(){EditorApplication.update+=Poll;}
 static void Poll(){const string path="Library/Seasons.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(path))return;var action=File.ReadAllText(path).Trim();File.Delete(path);try{if(action=="export")Export();else if(action=="build")Build();else if(action=="shadows"){EditorApplication.isPaused=false;new GameObject("Season shadow comparison").AddComponent<SeasonShadowComparison>();}else if(action=="check"){EditorApplication.isPaused=false;new GameObject("Season acceptance").AddComponent<SeasonAcceptance>();}}catch(Exception e){Debug.LogException(e);}}
 static bool Leaf(Material m)=>m&&(m.shader.name.EndsWith("/Foliage")||m.shader.name.EndsWith("/Cross")||m.name.ToLowerInvariant().Contains("leaves"));
 static void Export()
 {
 var baseline=Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");var manifest=new Manifest();var lines=new List<string>();
 foreach(var prefab in baseline.treePrefabs){lines.Add(prefab.name);foreach(var r in prefab.GetComponentsInChildren<MeshRenderer>(true)){var mesh=r.GetComponent<MeshFilter>().sharedMesh;lines.Add("  "+r.name+" "+mesh.vertexCount+" vertices; "+string.Join(", ",r.sharedMaterials.Select(m=>m.name+" ["+m.shader.name+"]")));if(!prefab.name.Contains("Poplar"))continue;var tris=new List<int>();var faces=new List<int>();for(int s=0;s<mesh.subMeshCount;s++){var t=mesh.GetTriangles(s);if(Leaf(r.sharedMaterials[s]))for(int i=0;i<t.Length/3;i++)faces.Add(tris.Count/3+i);tris.AddRange(t);}manifest.meshes.Add(new MeshRecord{id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh)),name=r.name,vertices=mesh.vertices,triangles=tris.ToArray(),leafFaces=faces.ToArray()});}}
 File.WriteAllText(Work+"SeasonTreeInventory.txt",string.Join("\n",lines));File.WriteAllText(Work+"Poplar-Family-Meshes.json",JsonUtility.ToJson(manifest));Debug.Log("Season tree export complete");
 }
 [Serializable] class WinterMesh {public string id;public int[] triangles;}
 [Serializable] class WinterManifest {public List<WinterMesh> meshes;}
 static void Build()
 {
 const string folder="Assets/CityForge/Resources/Seasons";
 var baseline=Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");
 var winterData=JsonUtility.FromJson<WinterManifest>(File.ReadAllText(Work+"Poplar-Winter-Faces.json"));
 var masks=winterData.meshes.ToDictionary(m=>m.id,m=>m.triangles);
 var library=AssetDatabase.LoadAssetAtPath<SeasonalTreeLibrary>(folder+"/SeasonalTrees.asset");
 if(!library){library=ScriptableObject.CreateInstance<SeasonalTreeLibrary>();AssetDatabase.CreateAsset(library,folder+"/SeasonalTrees.asset");}
 library.summer=baseline.treePrefabs.ToArray();library.fall=baseline.treePrefabs.ToArray();library.winter=baseline.treePrefabs.ToArray();
 var meshes=new Dictionary<Mesh,Mesh>();var mats=new Dictionary<(Material,int),Material>();int removed=0;
 for(int i=0;i<baseline.treePrefabs.Length;i++)
 {
 var source=baseline.treePrefabs[i];if(!source.name.Contains("Poplar"))continue;
 foreach(bool winter in new[]{false,true})
 {
 var go=UnityEngine.Object.Instantiate(source);go.name=source.name+(winter?" Winter":" Fall");
 foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true))
 {
 var filter=r.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;var materials=r.sharedMaterials;
 if(winter)
 {
 var id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh));
 if(!masks.TryGetValue(id,out var keep))throw new Exception("Blender mask missing: "+mesh.name);
 if(keep.Length==0){UnityEngine.Object.DestroyImmediate(r.gameObject);continue;}
 if(!meshes.TryGetValue(mesh,out var derived))
 {
 derived=UnityEngine.Object.Instantiate(mesh);derived.name=mesh.name+" Winter";
 var expected=new List<int>();for(int sub=0;sub<mesh.subMeshCount;sub++)if(!Leaf(materials[sub]))expected.AddRange(mesh.GetTriangles(sub));
 if(!expected.SequenceEqual(keep))throw new Exception("Blender changed bark geometry: "+mesh.name);
 for(int sub=0;sub<mesh.subMeshCount;sub++)if(Leaf(materials[sub])){removed+=mesh.GetTriangles(sub).Length/3;derived.SetTriangles(Array.Empty<int>(),sub,false);}
 // Original bounds and vertex attributes preserve terrain billboard sizing and wind weights.
 var meshPath=folder+"/"+id+"-Winter.asset";
 var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(old){EditorUtility.CopySerialized(derived,old);UnityEngine.Object.DestroyImmediate(derived);derived=old;}else AssetDatabase.CreateAsset(derived,meshPath);
 meshes[mesh]=derived;
 }
 filter.sharedMesh=derived;
 }
 else
 {
 for(int sub=0;sub<materials.Length;sub++)if(Leaf(materials[sub]))
 {
 var key=(materials[sub],i%3);if(!mats.TryGetValue(key,out var material))
 {
 material=new Material(materials[sub]);material.shader=Shader.Find(materials[sub].shader.name.EndsWith("/Cross")?"CityForge/Seasons/Autumn Cross":"CityForge/Seasons/Autumn Foliage");if(!material.shader)throw new Exception("Missing autumn shader");
 var palette=new[]{new Color(.95f,.63f,.08f,1),new Color(.95f,.43f,.045f,1),new Color(.8f,.29f,.045f,1)};
 material.SetColor("_AutumnColor",palette[i%3]);material.enableInstancing=true;
 var materialPath=folder+"/"+materials[sub].name+"-Fall-"+(i%3)+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(old){EditorUtility.CopySerialized(material,old);UnityEngine.Object.DestroyImmediate(material);material=old;}else AssetDatabase.CreateAsset(material,materialPath);mats[key]=material;
 }
 materials[sub]=material;
 }
 r.sharedMaterials=materials;
 }
 }
 if(winter)foreach(var group in go.GetComponentsInChildren<LODGroup>())
 {
 var lods=group.GetLODs().Select(l=>new LOD(l.screenRelativeTransitionHeight,l.renderers.Where(r=>r&&r.enabled).ToArray())).Where(l=>l.renderers.Length>0).ToArray();
 if(lods.Length==0)throw new Exception("Winter has no branch mesh");float previous=1f;for(int l=0;l<lods.Length;l++){lods[l].screenRelativeTransitionHeight=Mathf.Clamp(lods[l].screenRelativeTransitionHeight,0,previous-.001f);previous=lods[l].screenRelativeTransitionHeight;}lods[lods.Length-1].screenRelativeTransitionHeight=0;group.SetLODs(lods);group.RecalculateBounds();
 }
 var prefab=PrefabUtility.SaveAsPrefabAsset(go,folder+"/"+go.name+".prefab");if(winter)library.winter[i]=prefab;else library.fall[i]=prefab;UnityEngine.Object.DestroyImmediate(go);
 }
 }
 EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
 File.WriteAllText(Work+"Season-Asset-Build.txt","Built 11 poplar variants for Fall and Winter. Blender-validated winter meshes: "+meshes.Count+"; removed leaf triangles: "+removed+". Summer originals retained; maple/willow shrubs unchanged.");Debug.Log("Seasonal poplar assets built");
 }
}
}
#endif
