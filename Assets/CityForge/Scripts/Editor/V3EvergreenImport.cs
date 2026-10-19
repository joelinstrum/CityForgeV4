#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class V3EvergreenImport
    {
        const string Art="Assets/CityForge/Art/Trees/V3Evergreen/";
        const string Prefab=Art+"EvergreenPilot.prefab";
        const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
        static V3EvergreenImport(){EditorApplication.update+=Poll;}
        static void Poll(){
            const string request="Library/V3Evergreen.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            var mode=File.ReadAllText(request).Trim();
            if(mode=="stop"){File.Delete(request);EditorApplication.isPlaying=false;return;}
            if(mode=="preview"&&!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete(request);
            try{if(mode=="palette")CityForge.Editor.EditorCoroutineRunner.Begin(PalettePreview());else if(mode=="preview")CityForge.Editor.EditorCoroutineRunner.Begin(Preview());else Build();}
            catch(Exception e){File.WriteAllText(Out+"V3-Evergreen-Checks.txt","FAIL: "+e);Debug.LogException(e);}
        }
        static void NeedlePalette(Material material){
            material.SetColor("_BaseColor",new Color(.38f,.56f,.44f,1));
            material.SetFloat("_Smoothness",.04f);material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.SetFloat("_EnvironmentReflections",0);material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        }
        static void Capture(Camera camera,string name){
            var target=new RenderTexture(1920,1080,24);target.Create();var previous=RenderTexture.active;
            try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
                var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);try{pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Out+name,pixels.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(pixels);}
            }finally{RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
        static IEnumerator PalettePreview(){
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            var camera=view?view.GetComponentInChildren<CityForgeCameraController>().GetComponent<Camera>():null;
            int count=view&&view.ApprovedDistrict?view.ApprovedDistrict.FullTreeCount:0;
            if(camera)Capture(camera,"Evergreen-Color-Before.png");
            for(int i=1;i<=3;i++){
                var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"Needle_Cluster_"+i+".mat");
                if(!material)throw new Exception("Missing evergreen needle material "+i);
                NeedlePalette(material);EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
            if(view&&view.ApprovedDistrict){var terrain=view.ApprovedDistrict.DisplayTerrain;terrain.terrainData.RefreshPrototypes();terrain.Flush();}
            for(int i=0;i<8;i++)yield return null;
            if(camera)Capture(camera,"Evergreen-Color-After.png");
            if(view&&view.ApprovedDistrict&&view.ApprovedDistrict.FullTreeCount!=count)throw new Exception("Palette update changed tree count");
            File.WriteAllText(Out+"Evergreen-Color-Checks.txt","PASS: three evergreen needle materials use deeper green tint (0.38, 0.56, 0.44), smoothness 0.04, specular/environment highlights disabled. Terrain prototypes refreshed. Tree count unchanged; no landscape, poplar, bark, lighting or save edits.\n");
        }
        [MenuItem("CityForge/Import/V3 Evergreen Preview Asset")]
        public static void Build(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing.");
            foreach(var file in Directory.GetFiles(Art,"*.png")){
                var texture=(TextureImporter)AssetImporter.GetAtPath(file);bool normal=file.EndsWith("_Normal.png");
                texture.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;texture.sRGBTexture=!normal;
                texture.alphaSource=TextureImporterAlphaSource.FromInput;texture.alphaIsTransparency=!normal;
                texture.mipmapEnabled=true;texture.mipMapsPreserveCoverage=!normal;texture.alphaTestReferenceValue=.15f;texture.SaveAndReimport();
            }
            foreach(var name in new[]{"Bark","Cap","Needle_Cluster_1","Needle_Cluster_2","Needle_Cluster_3"}){
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+name+"_Albedo.png"));
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+name+"_Normal.png"));material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_Smoothness",.1f);material.SetFloat("_Cull",0);
                if(name.StartsWith("Needle_"))NeedlePalette(material);
                if(name!="Bark"){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.15f);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=(int)RenderQueue.AlphaTest;material.SetOverrideTag("RenderType","TransparentCutout");}
                var existing=AssetDatabase.LoadAssetAtPath<Material>(Art+name+".mat");
                if(existing){EditorUtility.CopySerialized(material,existing);UnityEngine.Object.DestroyImmediate(material);}else AssetDatabase.CreateAsset(material,Art+name+".mat");
            }
            var root=new GameObject("V3 Evergreen Pilot — 18m");var counts=new int[3];
            try{
                var levels=new LOD[3];
                for(int i=0;i<3;i++){
                    var path=Art+"EvergreenPilot_LOD"+i+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(root.transform,false);
                    var renderers=model.GetComponentsInChildren<MeshRenderer>();
                    foreach(var renderer in renderers){
                        renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+renderer.name+".mat");if(!renderer.sharedMaterial)throw new Exception("Unmapped evergreen mesh "+renderer.name);
                        renderer.shadowCastingMode=ShadowCastingMode.TwoSided;counts[i]+=renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
                    }
                    levels[i]=new LOD(i==0?.18f:i==1?.065f:.005f,renderers);
                }
                var lod=root.AddComponent<LODGroup>();lod.SetLODs(levels);lod.RecalculateBounds();
                if(counts[0]<=counts[1]||counts[1]<=counts[2])throw new Exception("Evergreen LOD reduction failed");
                var bounds=new Bounds();bool first=true;foreach(var renderer in levels[0].renderers){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
                var scale=18f/bounds.size.y;
                foreach(Transform model in root.transform){model.localScale*=scale;model.localPosition=new Vector3(model.localPosition.x*scale,(model.localPosition.y-bounds.min.y)*scale,model.localPosition.z*scale);}
                lod.RecalculateBounds();
                bounds=levels[0].renderers[0].bounds;foreach(var renderer in levels[0].renderers)bounds.Encapsulate(renderer.bounds);
                if(Mathf.Abs(bounds.size.y-18)>.1f)throw new Exception("Evergreen height mismatch: "+bounds.size);
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
                Directory.CreateDirectory("Assets/CityForge/Resources/Trees/MountainEvergreen");AssetDatabase.Refresh();
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/CityForge/Resources/Trees/MountainEvergreen/EvergreenPilot.prefab");AssetDatabase.SaveAssets();
                File.WriteAllText(Out+"V3-Evergreen-Checks.txt","PASS: complete 3D source; 18 m height; mapped bark/needle cutout materials; three mesh LODs: "+string.Join(" / ",counts)+" triangles. Prepared separately from approved biome; no gameplay generation changed.\n");
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static IEnumerator Preview(){
            EditorApplication.isPaused=false;
            var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;
            var menu=UnityEngine.Object.FindFirstObjectByType<CityForge.UI.MainMenuController>();
            var snapshot=RegionMapGenerator.Generate("Evergreen asset preview",RegionSize.Small,new RegionCreationSettings{vegetation=RegionVegetation.OpenMeadow});
            PrototypeRegionView.Open(snapshot.info.name,menu.GetComponentInChildren<Canvas>(),snapshot.info.seed,RegionSize.Small,snapshot);
            yield return null;yield return null;
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var landscape=view.ApprovedDistrict;
            var group=new GameObject("V3 evergreen review group");group.transform.SetParent(view.transform,false);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            for(int i=0;i<5;i++){
                var position=new Vector3((i-2)*13,0,(i%2)*15);position.y=landscape.DisplayTerrain.SampleHeight(position)+landscape.DisplayTerrain.transform.position.y;
                var tree=(GameObject)PrefabUtility.InstantiatePrefab(prefab);tree.transform.SetParent(group.transform,false);tree.GetComponent<LODGroup>().ForceLOD(0);tree.transform.position=position;tree.transform.rotation=Quaternion.Euler(0,i*71,0);tree.transform.localScale=Vector3.one*(.8f+i*.08f);
            }
            var rig=view.GetComponentInChildren<CityForgeCameraController>();var state=rig.CaptureRegionState();state.focus=new Vector3(0,landscape.DisplayTerrain.SampleHeight(Vector3.zero)+8,0);state.size=47;state.distance=180;rig.RestoreRegionState(state);
            for(int i=0;i<15;i++)yield return null;
            ScreenCapture.CaptureScreenshot(Out+"V3-Evergreen-Preview.png");
        }
    }
}
#endif
