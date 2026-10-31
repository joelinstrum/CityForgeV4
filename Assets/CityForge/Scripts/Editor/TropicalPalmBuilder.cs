#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.World;
namespace CityForge.EditorTools
{
    public static class TropicalPalmBuilder
    {
        const string Output="Assets/CityForge/Resources/TropicalCoconut";
        const string Source="SourceAssets/TropicalPalm/CoconutPack/Runtime";
        [MenuItem("CityForge/Forest/Prepare tropical palm wind shader")]
        public static void PrepareWindShader()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Prepare wind in Edit mode.");
            var path=AssetDatabase.GetAssetPath(Shader.Find("Universal Render Pipeline/Lit"));
            if(!File.Exists(path)){
                var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
                if(package==null)throw new FileNotFoundException("Installed URP Lit shader source missing.",path);
                path=Path.Combine(package.resolvedPath,path.Substring(package.assetPath.Length+1));
            }
            string source=File.ReadAllText(path).Replace("Shader \"Universal Render Pipeline/Lit\"","Shader \"CityForge/Tropical Coconut Wind\"");
            int properties=source.IndexOf("{",source.IndexOf("Properties",StringComparison.Ordinal),StringComparison.Ordinal)+1;
            source=source.Insert(properties,"\n        _PalmWindWeight(\"Wind Weight\", Float)=0\n        _PalmWindTime(\"Wind Time\", Float)=0\n        _PalmHeight(\"Palm Height\", Float)=17\n");
            string helper="#include \"Assets/CityForge/Rendering/Region/TropicalPalmWind.hlsl\"\n";
            var passes=new[]{
                ("LitForwardPass.hlsl","LitPassVertex","positionOS","normalOS"),
                ("LitGBufferPass.hlsl","LitGBufferPassVertex","positionOS","normalOS"),
                ("ShadowCasterPass.hlsl","ShadowPassVertex","positionOS","normalOS"),
                ("DepthOnlyPass.hlsl","DepthOnlyVertex","position",""),
                ("LitDepthNormalsPass.hlsl","DepthNormalsVertex","positionOS","normal")};
            foreach(var pass in passes){
                string include="#include \"Packages/com.unity.render-pipelines.universal/Shaders/"+pass.Item1+"\"";
                if(!source.Contains(include)||!source.Contains("#pragma vertex "+pass.Item2))throw new InvalidOperationException("Unsupported URP palm pass: "+pass.Item1);
                string deform=pass.Item4.Length==0?"input."+pass.Item3+".xyz=PalmWindPosition(input."+pass.Item3+".xyz,input.texcoord);":"PalmWindDeform(input."+pass.Item3+".xyz,input."+pass.Item4+",input.texcoord);";
                string wrapper="Varyings Palm"+pass.Item2+"(Attributes input){UNITY_SETUP_INSTANCE_ID(input);"+deform+"return "+pass.Item2+"(input);}";
                source=source.Replace(include,include+"\n"+helper+wrapper).Replace("#pragma vertex "+pass.Item2,"#pragma vertex Palm"+pass.Item2);
            }
            string destination=Output+"/CoconutWind.shader";File.WriteAllText(destination,source);AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(destination);if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Palm wind shader failed compilation.");
            var assets=Resources.Load<TropicalPalmAssets>("TropicalCoconut/Palms");if(!assets||!assets.material)throw new InvalidOperationException("Prepare coconut palm assets first.");
            assets.material.shader=shader;EditorUtility.SetDirty(assets.material);AssetDatabase.SaveAssets();
        }
        [MenuItem("CityForge/Forest/Prepare tropical palm assets")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Prepare palms in Edit mode.");
            if(Directory.Exists(Output))throw new InvalidOperationException("Coconut output exists; preserve it before rebuilding.");
            foreach(string name in new[]{"Atlas.png","Normal.png"})if(!File.Exists(Source+"/"+name))throw new FileNotFoundException("Prepare coconut texture atlases first.",name);
            for(int variant=0;variant<8;variant++)for(int lod=0;lod<4;lod++)if(!File.Exists(Source+$"/Palm{variant}_LOD{lod}.fbx"))throw new FileNotFoundException("Prepare coconut LODs first.");
            Directory.CreateDirectory(Output);
            foreach(string name in new[]{"Atlas.png","Normal.png"})File.Copy(Source+"/"+name,Output+"/"+name);
            for(int variant=0;variant<8;variant++)for(int lod=0;lod<4;lod++)File.Copy(Source+$"/Palm{variant}_LOD{lod}.fbx",Output+$"/Palm{variant}_LOD{lod}.fbx");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string name in new[]{"Atlas.png","Normal.png"}){
                var importer=(TextureImporter)AssetImporter.GetAtPath(Output+"/"+name);
                importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.wrapModeU=TextureWrapMode.Clamp;importer.wrapModeV=TextureWrapMode.Repeat;
                if(name=="Normal.png")importer.textureType=TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            var assets=ScriptableObject.CreateInstance<TropicalPalmAssets>();assets.variants=new TropicalPalmAssets.Variant[8];
            for(int variant=0;variant<8;variant++){
                var entry=new TropicalPalmAssets.Variant{lods=new Mesh[4]};assets.variants[variant]=entry;
                for(int lod=0;lod<4;lod++){
                    string path=Output+$"/Palm{variant}_LOD{lod}.fbx";
                    var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.SaveAndReimport();
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var filter=model.GetComponentInChildren<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name=$"Coconut {variant+1} LOD{lod}";
                    var transform=filter.transform.localToWorldMatrix;var normalTransform=transform.inverse.transpose;
                    var vertices=mesh.vertices;var normals=mesh.normals;
                    for(int v=0;v<vertices.Length;v++){vertices[v]=transform.MultiplyPoint3x4(vertices[v]);normals[v]=normalTransform.MultiplyVector(normals[v]).normalized;}
                    mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();mesh.RecalculateTangents();
                    entry.lods[lod]=mesh;AssetDatabase.CreateAsset(mesh,Output+$"/Mesh{variant}_{lod}.asset");
                }
            }
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Tropical coconut palms",enableInstancing=true};
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/Atlas.png"));material.SetColor("_BaseColor",Color.white);
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/Normal.png"));material.SetFloat("_BumpScale",.35f);material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness",.12f);material.SetFloat("_Cull",0);
            AssetDatabase.CreateAsset(material,Output+"/Palms.mat");assets.material=material;
            AssetDatabase.CreateAsset(assets,Output+"/Palms.asset");AssetDatabase.SaveAssets();PrepareWindShader();
        }
    }
}
#endif
