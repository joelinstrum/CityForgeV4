#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.World;
namespace CityForge.Editor
{
    /// <summary>References the locally approved art; never exports purchased artwork into the source repository.</summary>
    public static class MixedForestLibraryBuilder
    {
        const string Source="Assets/CityForge/Art/Trees/ForestTrees1Trial/";
        const string Destination="Assets/CityForge/Resources/Trees/ApprovedMixedForest/V1/";
        [MenuItem("CityForge/Forest/Build approved mixed forest library")]
        public static void Build()
        {
            var stand=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"FullerLargeMeadowsTrial.prefab");
            var old=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"OptimizedForestLibrary3.fbx");
            var full=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"FullerForestLibrary.fbx");
            if(!stand||!old||!full)throw new InvalidOperationException("Restore the private approved fuller chestnut/locust/larch assets before building the forest library.");
            Directory.CreateDirectory(Destination);AssetDatabase.Refresh();
            var species=new Dictionary<Mesh,int>();foreach(Transform tree in old.transform){if(!tree.name.StartsWith("Tree_"))continue;int variant=int.Parse(tree.name.Substring(5))-1;foreach(var m in tree.GetComponentsInChildren<MeshFilter>())species[m.sharedMesh]=variant;}
            var templates=new GameObject[8];foreach(Transform tree in stand.transform){
                if(tree.name.StartsWith("Larch"))continue;Transform level=null;foreach(Transform l in tree)if(l.name.StartsWith("LOD0"))level=l;
                if(!level)continue;foreach(var m in level.GetComponentsInChildren<MeshFilter>())if(species.TryGetValue(m.sharedMesh,out int variant)&&!templates[variant])templates[variant]=tree.gameObject;
            }
            templates[6]=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Evergreen/Larch1.prefab");
            templates[7]=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Evergreen/Larch2.prefab");
            var prefabs=new GameObject[8];var names=new[]{"Chestnut01","Chestnut02","Chestnut03","Chestnut04","Chestnut05","Locust","LarchTall","LarchMedium"};
            for(int i=0;i<8;i++){
                if(!templates[i])throw new InvalidOperationException("Missing approved tree "+names[i]);
                var tree=UnityEngine.Object.Instantiate(templates[i]);
                try{
                    if(PrefabUtility.IsPartOfPrefabInstance(tree))PrefabUtility.UnpackPrefabInstance(tree,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    tree.name=names[i];tree.transform.position=Vector3.zero;tree.transform.rotation=i<6?Quaternion.Euler(-90,0,0):Quaternion.identity;
                    tree.transform.localScale=Vector3.one*(i<6?100:i==6?1.65f:1.75f);
                    tree.SetActive(true);foreach(var r in tree.GetComponentsInChildren<Renderer>(true))r.enabled=true;
                    foreach(var lod in tree.GetComponentsInChildren<LODGroup>(true)){lod.enabled=true;lod.RecalculateBounds();}
                    prefabs[i]=PrefabUtility.SaveAsPrefabAsset(tree,Destination+names[i]+".prefab");
                }finally{UnityEngine.Object.DestroyImmediate(tree);}
            }
            var shadowMap=new Dictionary<Mesh,Mesh>();foreach(Transform oldTree in old.transform){
                Transform far=null;foreach(Transform l in oldTree)if(l.name.StartsWith("LOD2"))far=l;if(!far)continue;
                var targets=new Dictionary<string,Mesh>();foreach(var m in far.GetComponentsInChildren<MeshFilter>())targets[m.name.Split('.')[0]]=m.sharedMesh;
                foreach(var model in new[]{old,full})foreach(Transform tree in model.transform)if(tree.name==oldTree.name)
                    foreach(var m in tree.GetComponentsInChildren<MeshFilter>())if(targets.TryGetValue(m.name.Split('.')[0],out var low))shadowMap[m.sharedMesh]=low;
            }
            var library=AssetDatabase.LoadAssetAtPath<MixedForestLibrary>(Destination+"Library.asset");
            if(!library){library=ScriptableObject.CreateInstance<MixedForestLibrary>();AssetDatabase.CreateAsset(library,Destination+"Library.asset");}
            library.prefabs=prefabs;library.weights=new[]{.112f,.112f,.112f,.112f,.112f,.16f,.21f,.07f};
            library.shadowSources=new Mesh[shadowMap.Count];library.shadowReplacements=new Mesh[shadowMap.Count];int n=0;
            foreach(var pair in shadowMap){library.shadowSources[n]=pair.Key;library.shadowReplacements[n]=pair.Value;n++;}
            library.Validate();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            Debug.Log("Approved mixed forest library ready: eight tree variants, "+shadowMap.Count+" distant shadow mappings. Licensed art remains private.");
        }
    }
}
#endif
