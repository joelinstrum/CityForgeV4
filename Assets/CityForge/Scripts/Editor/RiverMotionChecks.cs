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
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class RiverMotionChecks
    {
        const string Output="Library/RiverMotionChecks/";
        static RiverMotionChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/RiverMotion.request";if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||!EditorApplication.isPlaying)return;
            File.Delete(request);Directory.CreateDirectory(Output);CityForge.Editor.EditorCoroutineRunner.Begin(Check());}
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static IEnumerator Check(){
            Exception failure=null;
            try{Verify();}catch(Exception e){failure=e;}
            if(failure!=null){File.WriteAllText(Output+"Checks.txt","FAIL: "+failure);Debug.LogException(failure);yield break;}
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            view.SetAnimalsEnabled(true);var wildlife=view.ApprovedDistrict.Wildlife;
            try{
                Require(wildlife.Animals.Count(a=>a.species.id=="crab")>=6,"River banks/shallows contain crabs");
                Require(wildlife.Animals.Count(a=>a.species.habitat==WildlifeHabitat.Freshwater||a.species.habitat==WildlifeHabitat.River)>=12,"River contains fish");
                foreach(var a in wildlife.Animals)Require(wildlife.Suitable(a.species,new Vector2(a.root.position.x,a.root.position.z),out _),"Initial river life habitat");
                wildlife.FindRiverLife();
                VerifyFish(view,wildlife.Animals.First(a=>a.species.habitat==WildlifeHabitat.Freshwater));
            }catch(Exception e){File.WriteAllText(Output+"Checks.txt","FAIL: "+e);Debug.LogException(e);yield break;}
            float time=Time.time;var crabs=wildlife.Animals.Where(a=>a.species.id=="crab").ToArray();var positions=crabs.Select(a=>a.root.position).ToArray();
            while(Time.time<time+15){
                bool valid=wildlife.Animals.All(a=>wildlife.Suitable(a.species,new Vector2(a.root.position.x,a.root.position.z),out _));
                if(!valid){File.WriteAllText(Output+"Checks.txt","FAIL: wandering left suitable habitat");yield break;}yield return null;
            }
            bool crawled=crabs.Where((a,i)=>(a.root.position-positions[i]).sqrMagnitude>.0025f).Any();
            Require(crawled,"Crabs actually crawl");
            int frame=Time.frameCount;while(Time.frameCount<frame+8)yield return null;
            ScreenCapture.CaptureScreenshot(Output+"River-Life.png");
            File.AppendAllText(Output+"Checks.txt","PASS: river life opt-in; "+string.Join(", ",wildlife.Animals.Where(a=>a.species.id=="crab"||a.species.habitat==WildlifeHabitat.Freshwater||a.species.habitat==WildlifeHabitat.River).GroupBy(a=>a.species.id).Select(g=>g.Key+"="+g.Count()))+"; habitat remains valid during15 seconds; crabs crawl; close river-life view left open. No player saves written.\n");
        }
        static void VerifyFish(PrototypeRegionView view,DistrictWildlife.Animal fish){
            var renderers=fish.root.GetComponentsInChildren<Renderer>();var layers=fish.root.GetComponentsInChildren<Transform>();var originalLayers=layers.Select(t=>t.gameObject.layer).ToArray();
            var host=new GameObject("Underwater fish verification");var camera=host.AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=1.3f;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=30;
            camera.transform.SetPositionAndRotation(fish.root.position+Vector3.up*10,Quaternion.Euler(90,0,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=(1<<4)|(1<<31);
            var settings=camera.GetUniversalAdditionalCameraData();settings.renderPostProcessing=false;settings.requiresDepthTexture=true;
            var target=RenderTexture.GetTemporary(256,256,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);var read=new Texture2D(256,256,TextureFormat.RGBAHalf,false,true);var previous=RenderTexture.active;
            try{
                foreach(var t in layers)t.gameObject.layer=31;
                foreach(var r in renderers)r.forceRenderingOff=true;var hidden=Frame("Fish-Without.png");
                foreach(var r in renderers)r.forceRenderingOff=false;var visible=Frame("Fish-Underwater.png");
                int changed=0;for(int i=0;i<visible.Length;i++)if(Mathf.Abs(hidden[i].r-visible[i].r)+Mathf.Abs(hidden[i].g-visible[i].g)+Mathf.Abs(hidden[i].b-visible[i].b)>.015f)changed++;
                Require(changed>30,"Submerged fish visibly changes rendered river pixels: "+changed);
                File.AppendAllText(Output+"Checks.txt","PASS: submerged fish visible through river surface; changed pixels="+changed+".\n");
                Color[] Frame(string name){RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();File.WriteAllBytes(Output+name,read.EncodeToPNG());return read.GetPixels();}
            }finally{foreach(var r in renderers)r.forceRenderingOff=false;for(int i=0;i<layers.Length;i++)layers[i].gameObject.layer=originalLayers[i];RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(read);UnityEngine.Object.Destroy(host);}
        }
        static void Verify(){
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var land=view.ApprovedDistrict;
            var surface=land.GetComponentsInChildren<LakeWaterRenderer>().First(s=>s.SurfaceMaterial.GetFloat("_RiverSurface")>.5f);
            var material=new Material(surface.SurfaceMaterial);material.SetFloat("_RiverSceneDepth",0);
            var mesh=surface.GetComponent<MeshFilter>().sharedMesh;var bounds=mesh.bounds;var center=bounds.center;
            for(int z=2;z<30;z++)for(int x=2;x<30;x++){
                var point=land.Origin+new Vector2(x/32f,z/32f)*land.Side;
                if(land.Height(point)<surface.SurfaceLevel-2&&land.ShoreDistance(point)<-35){center=new Vector3(point.x,surface.SurfaceLevel,point.y);goto FoundWater;}
            }
            FoundWater:;
            var target=RenderTexture.GetTemporary(512,512,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
            var read=new Texture2D(512,512,TextureFormat.RGBAHalf,false,true);var previous=RenderTexture.active;
            var renderer=surface.GetComponent<MeshRenderer>();var originalMaterial=renderer.sharedMaterial;int layer=surface.gameObject.layer;
            var host=new GameObject("River GPU verification camera");var proof=host.AddComponent<Camera>();proof.enabled=false;
            proof.orthographic=true;proof.orthographicSize=18;proof.aspect=1;proof.nearClipPlane=1;proof.farClipPlane=200;
            var sun=land.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.type==LightType.Directional);
            var eye=center+(sun?Vector3.Reflect(sun.transform.forward,Vector3.up):new Vector3(0,.7f,-.7f))*100;
            proof.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(center-eye,Vector3.up));
            proof.clearFlags=CameraClearFlags.SolidColor;proof.backgroundColor=Color.clear;proof.cullingMask=1<<4;proof.allowHDR=true;
            var pipeline=proof.GetUniversalAdditionalCameraData();pipeline.renderPostProcessing=false;pipeline.requiresColorTexture=false;pipeline.requiresDepthTexture=false;
            surface.gameObject.layer=4;renderer.sharedMaterial=material;
            try{
                var first=Frame(0);var second=Frame(2);int wet=0,maskChanged=0;double change=0;float maximum=0;
                for(int i=0;i<first.Length;i++){
                    bool a=first[i].r+first[i].g+first[i].b>.001f,b=second[i].r+second[i].g+second[i].b>.001f;if(a!=b)maskChanged++;
                    if(!a||!b)continue;wet++;float delta=Mathf.Abs(first[i].r-second[i].r)+Mathf.Abs(first[i].g-second[i].g)+Mathf.Abs(first[i].b-second[i].b);change+=delta;maximum=Mathf.Max(maximum,delta);
                }
                Require(wet>1000,"GPU proof renders river pixels: count="+wet+", renderer="+renderer.enabled+", active="+surface.gameObject.activeInHierarchy+", bounds="+renderer.bounds+", region="+material.GetVector("_DepthRegion")+", shader="+material.shader.name);Require(change/wet>.001&&maximum>.01,"GPU pixels visibly change with current time");Require(maskChanged==0,"Animation leaves water footprint fixed");
                File.WriteAllText(Output+"Checks.txt",$"PASS: rendered river at0s/2s: wet pixels={wet}, mean RGB change={change/wet:F5}, maximum={maximum:F5}, footprint changes={maskChanged}.\n");
                Color[] Frame(float time){
                    material.SetFloat("_CurrentPreviewTime",time);
                    RenderPipeline.SubmitRenderRequest(proof,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    RenderTexture.active=target;read.ReadPixels(new Rect(0,0,512,512),0,0);read.Apply();
                    File.WriteAllBytes(Output+"Current-"+time+"s.png",read.EncodeToPNG());return read.GetPixels();
                }
            }finally{renderer.sharedMaterial=originalMaterial;surface.gameObject.layer=layer;UnityEngine.Object.Destroy(host);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(read);UnityEngine.Object.Destroy(material);}
        }
    }
}
#endif
