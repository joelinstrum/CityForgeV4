#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class RiverVisualStudy
    {
        const string Output="Library/RiverVisualStudy/";
        static RiverVisualStudy(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/RiverVisual.request";if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            string command=File.ReadAllText(request).Trim();File.Delete(request);Directory.CreateDirectory(Output);
            try{
                Prepare();
                if(command.StartsWith("apply")){Apply(int.Parse(command.Substring(5).Trim()));File.WriteAllText(Output+"Status.txt","Applied "+command);return;}
                if(command=="capture"&&EditorApplication.isPlaying)CityForge.Editor.EditorCoroutineRunner.Begin(Capture());
                else File.WriteAllText(Output+"Status.txt","Prepared local river normals");
            }catch(Exception e){File.WriteAllText(Output+"Status.txt","FAIL: "+e);Debug.LogException(e);}
        }
        static void Prepare(){
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.waveharmonic.crest/Runtime/Textures/WaveNormals/WaveNormals.png");
            if(!texture)throw new Exception("Local Crest normal texture missing");
            var source=Resources.Load<Material>("Water/LakeWater");source.SetTexture("_RiverNormal",texture);EditorUtility.SetDirty(source);AssetDatabase.SaveAssets();
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(view&&view.ApprovedDistrict)foreach(var water in view.ApprovedDistrict.GetComponentsInChildren<LakeWaterRenderer>())if(water.SurfaceMaterial.GetFloat("_RiverSurface")>.5f)water.SurfaceMaterial.SetTexture("_RiverNormal",texture);
        }
        static void Style(Material material,int style){
            float scale=style==0?40:style==1?28:style==2?80:44;
            float strength=style==0?.12f:style==1?.22f:style==2?.65f:.45f;
            float roughness=style==0?.42f:style==1?.36f:style==2?.32f:.36f;
            float sun=style==0?.014f:style==1?.025f:style==2?.03f:.025f;
            material.SetFloat("_RippleScale",scale);material.SetFloat("_RippleStrength",strength);
            material.SetFloat("_RiverRoughness",roughness);material.SetFloat("_SunStrength",sun);
        }
        static void Apply(int style){
            var source=Resources.Load<Material>("Water/LakeWater");Style(source,style);EditorUtility.SetDirty(source);AssetDatabase.SaveAssets();
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(view&&view.ApprovedDistrict)foreach(var surface in view.ApprovedDistrict.GetComponentsInChildren<LakeWaterRenderer>())if(surface.SurfaceMaterial.GetFloat("_RiverSurface")>.5f){Style(surface.SurfaceMaterial,style);surface.SurfaceMaterial.SetFloat("_CurrentPreviewTime",-1);}
        }
        static IEnumerator Capture(){
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var land=view.ApprovedDistrict;
            var material=land.GetComponentsInChildren<LakeWaterRenderer>().First(s=>s.SurfaceMaterial.GetFloat("_RiverSurface")>.5f).SurfaceMaterial;
            if(ShaderUtil.ShaderHasError(material.shader))throw new Exception("River shader failed compilation");
            if(!view.AnimalsEnabled)view.SetAnimalsEnabled(true);land.Wildlife.FindRiverLife();
            var rig=view.GetComponentInChildren<CityForgeCameraController>();var pose=rig.CaptureRegionState();var camera=rig.GetComponent<Camera>();
            int width=1600,height=Mathf.RoundToInt(width/camera.aspect);height=(height/2)*2;
            var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGBHalf);var read=new Texture2D(width,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{
                foreach(int style in new[]{2,3})foreach(int zoom in new[]{1,2}){
                    Style(material,style);rig.SetZoomLevel(zoom);string folder=Output+"Style"+style+"-Zoom"+zoom+"/";Directory.CreateDirectory(folder);
                    for(int frame=0;frame<32;frame++){
                        material.SetFloat("_CurrentPreviewTime",frame*.125f);
                        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
                        read.ReadPixels(new Rect(0,0,width,height),0,0);read.Apply();File.WriteAllBytes(folder+frame.ToString("D3")+".png",read.EncodeToPNG());
                        File.WriteAllText(Output+"Status.txt",$"Capturing style{style} zoom{zoom} frame{frame}/31");yield return null;
                    }
                }
                File.WriteAllText(Output+"Status.txt","COMPLETE: two styles at gameplay zoom1/2;32 frames each,0.125s spacing. Camera heading/lighting unchanged. Selection remains pending visual review.");
            }finally{material.SetFloat("_CurrentPreviewTime",-1);Style(material,0);rig.RestoreRegionState(pose);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(read);}
        }
    }
}
#endif
