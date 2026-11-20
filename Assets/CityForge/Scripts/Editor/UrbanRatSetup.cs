#if UNITY_EDITOR
using System.IO;using UnityEditor;using UnityEngine;
namespace CityForge.Editor
{
 internal static class UrbanRatSetup
 {
  const string Root="Assets/CityForge/Resources/UrbanRats/";
  [InitializeOnLoadMethod]static void Queue()=>EditorApplication.delayCall+=Prepare;
  static void Prepare(){if(!File.Exists(Root+"Body.png"))return;var normal=AssetImporter.GetAtPath(Root+"Normal.png") as TextureImporter;if(normal&&normal.textureType!=TextureImporterType.NormalMap){normal.textureType=TextureImporterType.NormalMap;normal.SaveAndReimport();}Make("Body",Color.white,"Body.png",false);Make("Whiskers",Color.white,"Whiskers.png",true);Make("Eyes",new Color(.025f,.018f,.015f),null,false);}
  static void Make(string name,Color color,string texture,bool cutout){string path=Root+name+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(path))return;var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",name=="Eyes"?.65f:.15f);if(texture!=null)material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+texture));if(name=="Body"){material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Normal.png"));material.EnableKeyword("_NORMALMAP");}if(cutout){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.4f);material.SetFloat("_Cull",0);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;}AssetDatabase.CreateAsset(material,path);}
 }
}
#endif
