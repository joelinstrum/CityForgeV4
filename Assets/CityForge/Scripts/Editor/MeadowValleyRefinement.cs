using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace CityForge.Editor
{
    [InitializeOnLoad] public static class MeadowValleyRefinement
    {
        const string W = "/Users/joelinstrum/Documents/Codex/2026-10-09/pl/work/meadow-refine/";
        const string O = "/Users/joelinstrum/Documents/Codex/2026-10-09/pl/outputs/";
        const string S = "Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/RefinedMeadowValley.unity";
        static MeadowValleyRefinement() { EditorApplication.update += Tick; }
        static bool Take(string n) { if (!File.Exists(W+n)) return false; File.Delete(W+n); return true; }
        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            try
            {
                if (!EditorApplication.isPlaying && Take("refine")) Refine();
                if(!EditorApplication.isPlaying && Take("roads"))
                {
                    if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=S) throw new Exception("Refined scene required");
                    var t=Terrain.activeTerrain;int count=0;
                    foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("RamSpline") && f.sharedMesh))
                    {
                        var mesh=UnityEngine.Object.Instantiate(f.sharedMesh);mesh.name=f.name+" RefinedGround";
                        var vertices=mesh.vertices;
                        for(int i=0;i<vertices.Length;i++){var p=f.transform.TransformPoint(vertices[i]);p.y=t.SampleHeight(p)+.065f;vertices[i]=f.transform.InverseTransformPoint(p);}
                        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
                        AssetDatabase.CreateAsset(mesh,"Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/"+mesh.name+".asset");f.sharedMesh=mesh;
                        var collider=f.GetComponent<MeshCollider>();if(collider)collider.sharedMesh=mesh;count++;
                    }
                    AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                    File.AppendAllText(O+"MeadowRefinement_Build.txt","Re-seated "+count+" native road/path meshes onto refined terrain using separate mesh assets.\n");
                }
                if(Take("geometry"))
                {
                    File.WriteAllLines(O+"MeadowRefinement_Geometry.txt", UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.bounds.size.x>15 && r.bounds.size.y>3 && r.bounds.center.x>0 && r.bounds.center.x<512 && r.bounds.center.z>0 && r.bounds.center.z<512).Select(r=>r.name+" ROOT="+(PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject)?.name)+" "+r.bounds));
                }
                if (!EditorApplication.isPlaying && Take("smooth")) Smooth();
                if (!EditorApplication.isPlaying && Take("banks")) Banks();
                if (!EditorApplication.isPlaying && Take("cliffs"))
                {
                    if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=S) throw new Exception("Refined scene required");
                    var removed=new HashSet<GameObject>();
                    foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    {
                        var root=PrefabUtility.GetOutermostPrefabInstanceRoot(tr.gameObject);
                        if(!root || !root.name.ToLowerInvariant().Contains("cliff")) continue;
                        var p=root.transform.position;
                        if(p.x<0 || p.x>512 || p.z<0 || p.z>512) continue;
                        if(removed.Add(root)) root.SetActive(false);
                    }
                    EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                    File.AppendAllText(O+"MeadowRefinement_Build.txt","Disabled "+removed.Count+" old demo cliff prefab instances within valley; their grass-topped ledges conflicted with carved floodplain. Ordinary rocks retained.\n");
                }
                if (!EditorApplication.isPlaying && Take("layers"))
                {
                    var t=Terrain.activeTerrain;
                    var source=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/ValleyTerrain.asset");
                    t.terrainData.terrainLayers=source.terrainLayers;
                    t.terrainData.SetAlphamaps(0,0,source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight));
                    EditorUtility.SetDirty(t.terrainData); AssetDatabase.SaveAssets();
                    EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }
                if (EditorApplication.isPlaying && Take("iso-check"))
                {
                    var c=Camera.main; var nav=c.GetComponent<CameraSystem.MeadowShowcaseNavigation>();
                    var pos=c.transform.position; var rot=c.transform.rotation; var ortho=c.orthographic; var fog=RenderSettings.fog;
                    bool startedIsometric=nav.Isometric;
                    try
                    {
                        if(!startedIsometric)nav.ToggleIsometric();
                        if(!nav.Isometric || !c.orthographic || Mathf.Abs(c.transform.eulerAngles.x-35.26439f)>.01f) throw new Exception("Isometric framing failed");
                    }
                    finally { if(!startedIsometric && nav.Isometric)nav.ToggleIsometric(); }
                    if(c.orthographic!=ortho || RenderSettings.fog!=fog || Vector3.Distance(pos,c.transform.position)>.001f || Quaternion.Angle(rot,c.transform.rotation)>.001f) throw new Exception("Camera restore failed");
                    File.WriteAllText(O+"MeadowRefinement_CameraChecks.txt",startedIsometric?"Already-isometric projection/pitch and unchanged camera state PASS.\n":"Isometric projection/pitch and exact prior pose/projection/fog restoration PASS.\n");
                }
            }
            catch(Exception e) { File.WriteAllText(O+"MeadowRefinement_Error.txt",e.ToString()); Debug.LogException(e); }
        }
        static void Banks()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=S) throw new Exception("Refined scene required");
            var t=Terrain.activeTerrain; var td=t.terrainData;
            var baseData=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/CityForge/Art/Environment/MeadowNativeURP/Meadow Environment Dynamic Nature/Demo Scenes/Meadow Terrain Demo.asset");
            int n=td.heightmapResolution; var h=baseData.GetHeights(0,0,n,n);
            var cp=new[]{new Vector2(530,135),new Vector2(460,185),new Vector2(405,212),new Vector2(420,257),new Vector2(365,285),new Vector2(399,328),new Vector2(345,365),new Vector2(368,401),new Vector2(312,440),new Vector2(304,530)};
            var route=new List<Vector2>();
            for(int i=0;i<cp.Length-1;i++) for(int j=0;j<8;j++)
            {
                var a=cp[Mathf.Max(0,i-1)];var b=cp[i];var c=cp[i+1];var d=cp[Mathf.Min(cp.Length-1,i+2)];float f=j/8f;
                route.Add(.5f*((2*b)+(-a+c)*f+(2*a-5*b+4*c-d)*f*f+(-a+3*b-3*c+d)*f*f*f));
            }
            route.Add(cp.Last());
            for(int z=0;z<n;z++) for(int x=0;x<n;x++)
            {
                var p=new Vector2(x*512f/(n-1),z*512f/(n-1)); float dist=10000f;
                for(int i=1;i<route.Count;i++){var a=route[i-1];var v=route[i]-a;dist=Mathf.Min(dist,Vector2.Distance(p,a+v*Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude)));}
                if(dist>60f) { h[z,x]=Mathf.Max(h[z,x],9.5f/250f); continue; }
                float y=dist<7f?Mathf.Lerp(6.4f,8.8f,Mathf.Pow(dist/7f,2f)):Mathf.Lerp(8.8f,Mathf.Max(h[z,x]*250f,9.5f),Mathf.SmoothStep(0,1,(dist-7f)/53f));
                h[z,x]=y/250f;
            }
            td.SetHeights(0,0,h);td.SetTreeInstances(td.treeInstances,true);
            foreach(var field in UnityEngine.Object.FindObjectsByType<NativeMeadowInstanceField>(FindObjectsSortMode.None))
            {
                foreach(var batch in field.batches) for(int i=0;i<batch.matrices.Length;i++){var m=batch.matrices[i];var p=m.GetColumn(3);p.y=t.SampleHeight(p);m.SetColumn(3,p);batch.matrices[i]=m;}
                EditorUtility.SetDirty(field);
            }
            var depth=UnityEngine.Object.FindFirstObjectByType<WaveHarmonic.Crest.DepthProbe>();if(depth)depth.Populate();
            EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            File.AppendAllText(O+"MeadowRefinement_Build.txt","Rebuilt bank shoulders from original height data and the full 73-point river centerline, with a smooth 53m transition. This replaces mismatched earlier centerline shoulder cuts. Re-seated vegetation.\n");
        }
        static void Smooth()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != S) throw new Exception("Refined scene required");
            var t=Terrain.activeTerrain;
            var td=UnityEngine.Object.Instantiate(t.terrainData); td.name="RefinedValleyTerrain";
            AssetDatabase.CreateAsset(td,"Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/RefinedValleyTerrain.asset");
            t.terrainData=td; var collider=t.GetComponent<TerrainCollider>(); if(collider) collider.terrainData=td;
            int n=td.heightmapResolution; var h=td.GetHeights(0,0,n,n); var original=(float[,])h.Clone();
            // Smooth abrupt shoulders created where the new floodplain met the demo's original relief.
            for(int pass=0;pass<6;pass++)
            {
                var next=(float[,])h.Clone();
                for(int z=2;z<n-2;z++) for(int x=2;x<n-2;x++)
                {
                    if(original[z,x]*250f<9.05f) continue;
                    float sum=0f;
                    for(int dz=-2;dz<=2;dz++) for(int dx=-2;dx<=2;dx++) sum+=h[z+dz,x+dx];
                    next[z,x]=Mathf.Max(9.05f/250f,Mathf.Lerp(h[z,x],sum/25f,.65f));
                }
                h=next;
            }
            td.SetHeights(0,0,h); td.SetTreeInstances(td.treeInstances,true);
            foreach(var field in UnityEngine.Object.FindObjectsByType<NativeMeadowInstanceField>(FindObjectsSortMode.None))
            {
                foreach(var batch in field.batches) for(int i=0;i<batch.matrices.Length;i++)
                {
                    var matrix=batch.matrices[i]; var p=matrix.GetColumn(3);
                    p.y=t.SampleHeight(p); matrix.SetColumn(3,p); batch.matrices[i]=matrix;
                }
                EditorUtility.SetDirty(field);
            }
            var depth=UnityEngine.Object.FindFirstObjectByType<WaveHarmonic.Crest.DepthProbe>(); if(depth) depth.Populate();
            EditorUtility.SetDirty(td); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            File.AppendAllText(O+"MeadowRefinement_Build.txt","Own RefinedValleyTerrain asset: softened abrupt relief with six local smoothing passes, protected underwater bed; grass and trees re-snapped. Original ValleyTerrain unchanged.\n");
        }
        struct Piece { public Mesh mesh; public Material material; public int submesh; public Matrix4x4 local; }
        static void Refine()
        {
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.EndsWith("MeadowValleyShowcase.unity")) throw new Exception("Open original showcase before refining.");
            var terrain=Terrain.activeTerrain; var td=terrain.terrainData;
            var old=UnityEngine.Object.FindObjectsByType<NativeMeadowInstanceField>(FindObjectsSortMode.None);
            var existing=old.SelectMany(f=>f.batches).Select(b=>b.material).Distinct().ToArray();
            string b="Assets/CityForge/Art/Environment/MeadowNativeURP/Meadow Environment Dynamic Nature/";
            string[] paths={"Grass/Prefabs Grass/prefab_grass_meadow_01_detailed_1.prefab","Grass/Prefabs Grass/prefab_grass_meadow_02_detailed_1.prefab","Details/Prefabs/prefab_detail_meadow_clover_01.prefab","Grass/Prefabs Flowers/prefab_flower_chamomile_01_1.prefab","Grass/Prefabs Flowers/prefab_flower_common_poppy_01_1.prefab","Grass/Prefabs Flowers/prefab_flower_cornflower_01_1.prefab"};
            var pieces=new List<Piece[]>();
            foreach(var path in paths)
            {
                var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(b+path));
                go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var lod=go.GetComponentInChildren<LODGroup>(); var renderers=lod?lod.GetLODs()[0].renderers:go.GetComponentsInChildren<Renderer>();
                var list=new List<Piece>();
                foreach(var r in renderers)
                {
                    var mf=r.GetComponent<MeshFilter>(); if(!mf) continue;
                    for(int sm=0;sm<mf.sharedMesh.subMeshCount;sm++)
                    {
                        var mat=r.sharedMaterials[Mathf.Min(sm,r.sharedMaterials.Length-1)];
                        var copy=existing.FirstOrDefault(m=>m.name==mat.name+" ValleyField");
                        if(!copy) throw new Exception("Missing isolated field material "+mat.name);
                        list.Add(new Piece{mesh=mf.sharedMesh,material=copy,submesh=sm,local=r.transform.localToWorldMatrix});
                    }
                }
                pieces.Add(list.ToArray()); UnityEngine.Object.DestroyImmediate(go);
            }
            int res=td.detailResolution;
            var masks=new[]{td.GetDetailLayer(0,0,res,res,0),td.GetDetailLayer(0,0,res,res,1),td.GetDetailLayer(0,0,res,res,2)};
            var centers=UnityEngine.Object.FindObjectsByType<WaveHarmonic.Crest.WaterBody>(FindObjectsSortMode.None).Select(w=>new Vector2(w.transform.position.x,w.transform.position.z)).ToArray();
            var random=new System.Random(83746); float R(){return (float)random.NextDouble();}
            var batches=new List<NativeMeadowBatch>(); int[] counts=new int[6];
            for(int cz=0;cz<16;cz++) for(int cx=0;cx<16;cx++)
            {
                var lists=new Dictionary<(Mesh,Material,int),List<Matrix4x4>>();
                for(int k=0;k<3200;k++)
                {
                    float x=(cx+R())*32,z=(cz+R())*32;
                    int mx=Mathf.Min(res-1,(int)(x/512*res)),mz=Mathf.Min(res-1,(int)(z/512*res));
                    if(masks.Sum(m=>m[mz,mx])==0 && centers.All(p=>Vector2.Distance(p,new Vector2(x,z))>28)) continue;
                    float y=terrain.SampleHeight(new Vector3(x,0,z)); if(y<8.98f) continue;
                    float roll=R(); int type=roll<.48f?0:roll<.94f?1:2;
                    float flowers=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.52f,.72f,Mathf.PerlinNoise(x/24+12,z/24+8)));
                    if(R()<flowers*.055f) type=3+(int)(R()*3);
                    float scale=.72f+R()*.34f;
                    // Shorter, overlapping blades create a meadow carpet; rare broadleafs and flowers retain height.
                    Vector3 size=type<2?new Vector3(scale*.95f,scale*.52f,scale*.95f):Vector3.one*(type==2?.48f+R()*.2f:.65f+R()*.25f);
                    var matrix=Matrix4x4.TRS(new Vector3(x,y,z),Quaternion.Euler(0,R()*360,0),size);
                    foreach(var p in pieces[type])
                    {
                        var key=(p.mesh,p.material,p.submesh);
                        if(!lists.TryGetValue(key,out var matrices)){matrices=new List<Matrix4x4>();lists[key]=matrices;}
                        matrices.Add(matrix*p.local);
                    }
                    counts[type]++;
                }
                foreach(var list in lists) for(int i=0;i<list.Value.Count;i+=256)
                    batches.Add(new NativeMeadowBatch{mesh=list.Key.Item1,material=list.Key.Item2,submesh=list.Key.Item3,matrices=list.Value.Skip(i).Take(256).ToArray(),bounds=new Bounds(new Vector3(cx*32+16,25,cz*32+16),new Vector3(40,70,40))});
            }
            foreach(var f in old) UnityEngine.Object.DestroyImmediate(f.gameObject);
            var root=new GameObject("Refined short meadow grass and occasional wildflowers");
            var field=root.AddComponent<NativeMeadowInstanceField>(); field.batches=batches.ToArray(); field.presentationCamera=Camera.main;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),S);
            File.WriteAllText(O+"MeadowRefinement_Build.txt","Separate refined scene: "+S+"\nGrass/clover/flower placement counts: "+string.Join(",",counts)+"\nTotal "+counts.Sum()+" placements; "+batches.Count+" batches. Short grass, 6% clover, rare 24m wildflower patches. Existing trail masks and shoreline height retained. Terrain, water, lighting and original showcase assets unchanged.\n");
        }
    }
}
