#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using CityForge.World;
namespace CityForge.Editor
{
    public static class StreamWhiteStreakChecks
    {
        public static IEnumerator Compare(PrototypeRegionView view,string output)
        {
            var crest=view.GetComponentInChildren<CrestDistrictRiverPreview>();var land=view.ApprovedDistrict;var m=crest.Settings;
            string[] properties={"_CityForgeGlintGain","_Crest_Specular","_Crest_PlanarReflectionsIntensity"};var original=new float[properties.Length];for(int i=0;i<properties.Length;i++)original[i]=m.GetFloat(properties[i]);
            for(int frame=0;frame<10;frame++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-Baseline.png"));yield return null;
            m.SetFloat(properties[0],0);for(int frame=0;frame<10;frame++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-No-Glints.png"));yield return null;
            m.SetFloat(properties[1],0);m.SetFloat(properties[2],0);for(int frame=0;frame<10;frame++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"03-No-Water-Reflections.png"));yield return null;
            var renderers=crest.GetComponentsInChildren<MeshRenderer>();var enabled=new bool[renderers.Length];for(int i=0;i<renderers.Length;i++){enabled[i]=renderers[i].enabled;renderers[i].enabled=false;}
            for(int frame=0;frame<10;frame++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"04-Water-Hidden.png"));yield return null;
            // Terrain shader consumes diffuse alpha as smoothness even when the
            // TerrainLayer smoothness slider is zero. Isolate that source next.
            var td=land.DisplayTerrain.terrainData;var originalLayers=td.terrainLayers;var layers=(TerrainLayer[])originalLayers.Clone();var owned=new TerrainLayer[layers.Length];var textures=new Texture2D[layers.Length];
            foreach(int i in new[]{0,layers.Length-1}){
                owned[i]=Object.Instantiate(layers[i]);var source=layers[i].diffuseTexture;
                var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Graphics.Blit(source,rt);var previous=RenderTexture.active;RenderTexture.active=rt;
                var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,true);copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);var pixels=copy.GetPixels32();for(int k=0;k<pixels.Length;k++)pixels[k].a=8;copy.SetPixels32(pixels);copy.Apply(true,true);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
                textures[i]=copy;owned[i].diffuseTexture=copy;layers[i]=owned[i];
            }
            td.terrainLayers=layers;for(int frame=0;frame<10;frame++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"05-Matte-Bed-Water-Hidden.png"));yield return null;
            for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];for(int i=0;i<properties.Length;i++)m.SetFloat(properties[i],original[i]);
            for(int frame=0;frame<10;frame++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"06-Matte-Bed-With-Water.png"));yield return null;
            File.WriteAllText(Path.Combine(output,"Diagnostic.txt"),"PASS comparisons captured. Frame06 uses matte soil copies; all original terrain layers and water settings restored afterward. Sources unchanged.");
            td.terrainLayers=originalLayers;
            foreach(var layer in owned)if(layer)Object.Destroy(layer);
            foreach(var texture in textures)if(texture)Object.Destroy(texture);
        }
    }
}
#endif
