#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEditor;
using UnityEngine;

internal static class DeliveryCargoAssetSetup
{
    [InitializeOnLoadMethod] private static void Queue() => EditorApplication.delayCall += Prepare;
    [MenuItem("CityForge/Lots/Prepare delivery cargo")]
    internal static void Prepare()
    {
        foreach(var profile in VehicleProfile.All.Where(item=>!string.IsNullOrEmpty(item.emptyCargoMesh)))
        {
            string path="Assets/CityForge/Resources/"+profile.emptyCargoMesh+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            string stamp="empty-cargo-v1:"+JsonUtility.ToJson(profile);
            if(existing&&AssetImporter.GetAtPath(path).userData==stamp)continue;
            string folder=Path.GetDirectoryName(path);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Team.prefab");
            if(!prefab)continue;
            var root=Object.Instantiate(prefab);
            try
            {
                var wagon=root.transform.GetChild(0);
                var filter=wagon.GetComponentsInChildren<MeshFilter>().FirstOrDefault(part=>part.name==profile.cargoMeshPart);
                if(!filter||!filter.sharedMesh)continue;
                var source=filter.sharedMesh;var mesh=Object.Instantiate(source);mesh.name=source.name+" Empty Cargo";
                var vertices=source.vertices;var cut=new Bounds(profile.cargoCutCenter,profile.cargoCutSize);
                var keep=new Bounds(profile.cargoKeepCenter,profile.cargoKeepSize);
                int removed=0;
                for(int sub=0;sub<source.subMeshCount;sub++)
                {
                    var triangles=source.GetTriangles(sub);var kept=new List<int>(triangles.Length);
                    for(int i=0;i<triangles.Length;i+=3)
                    {
                        var center=(vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3;
                        var point=wagon.InverseTransformPoint(filter.transform.TransformPoint(center));
                        bool protectedPart=profile.cargoKeepSize.sqrMagnitude>0 && (keep.Contains(point) ||
                            keep.Contains(wagon.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i]]))) ||
                            keep.Contains(wagon.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i+1]]))) ||
                            keep.Contains(wagon.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i+2]]))));
                        if(cut.Contains(point)&&!protectedPart){removed++;continue;}
                        kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);
                    }
                    mesh.SetTriangles(kept,sub);
                }
                mesh.RecalculateBounds();
                if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
                else{AssetDatabase.CreateAsset(mesh,path);existing=mesh;}
                var importer=AssetImporter.GetAtPath(path);importer.userData=stamp;importer.SaveAndReimport();
                Debug.Log($"Prepared empty {profile.assetId}: removed {removed} fixed-load triangles.");
            }
            finally{Object.DestroyImmediate(root);}
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
