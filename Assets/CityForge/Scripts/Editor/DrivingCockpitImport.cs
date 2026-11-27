#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEngine;using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    internal static class DrivingCockpitImport
    {
        internal const string Folder="Assets/CityForge/Resources/Driving/Cockpit/";
        [MenuItem("CityForge/Driving/Import Cockpit PNG…")]
        static void Choose(){string path=EditorUtility.OpenFilePanel("Import driving cockpit PNG","","png");if(!string.IsNullOrEmpty(path))Import(path);}
        public static void FromCommandLine(){var args=System.Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-cockpitPng");if(i<0||i+1>=args.Length)throw new ArgumentException("Supply -cockpitPng with the original PNG path.");Import(args[i+1]);}
        internal static void Import(string source)
        {
            if(!File.Exists(source))throw new FileNotFoundException("Cockpit PNG not found",source);
            Directory.CreateDirectory(Folder);string path=Folder+"VintageCockpit.png";
            if(Path.GetFullPath(source)!=Path.GetFullPath(path))File.Copy(source,path,true);
            AssetDatabase.Refresh();var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
            importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            var options=new TextureImporterSettings();importer.ReadTextureSettings(options);options.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(options);
            importer.isReadable=true;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();int clear=0;
            foreach(var pixel in pixels)if(pixel.a==0)clear++;
            if(clear<texture.width*texture.height*.25f)throw new InvalidDataException("Cockpit needs a genuinely transparent windshield.");
            for(int y=texture.height/2;y<texture.height*4/5;y+=8)for(int x=texture.width/4;x<texture.width*3/4;x+=8)if(pixels[y*texture.width+x].a>1)throw new InvalidDataException("The central windshield opening contains opaque pixels.");
            importer.isReadable=false;importer.SaveAndReimport();
            if(!AssetDatabase.LoadAssetAtPath<Sprite>(path))throw new InvalidDataException("Cockpit sprite import failed.");
            if(!AssetDatabase.LoadAssetAtPath<DrivingCockpitSettings>(Folder+"Settings.asset"))AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DrivingCockpitSettings>(),Folder+"Settings.asset");
            AssetDatabase.SaveAssets();Debug.Log("Driving cockpit imported as a UI sprite. True-alpha pixels: "+clear+" / "+pixels.Length+". Central windshield verified transparent.");
        }
    }
}
#endif
