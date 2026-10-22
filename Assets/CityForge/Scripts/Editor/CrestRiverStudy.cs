#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;
using UnityEditor;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using CityForge.World;using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class CrestRiverStudy
    {
        const string ProfilePath="Assets/CityForge/Scripts/Editor/RiverSunGlintPreview.json";
        [MenuItem("CityForge/Water/Preview bounded Crest river")] static void Preview()=>File.WriteAllText("Library/CrestRiver.request",File.ReadAllText(ProfilePath));
        [MenuItem("CityForge/Water/Preview river sunlight glints")] static void SunlightPreview(){var tuning=JsonUtility.FromJson<Tuning>(File.ReadAllText(ProfilePath));tuning.sunlightView=true;File.WriteAllText("Library/CrestRiver.request",JsonUtility.ToJson(tuning));}
        [MenuItem("CityForge/Water/Preview selective river glints")] static void SelectivePreview(){var tuning=JsonUtility.FromJson<Tuning>(File.ReadAllText(ProfilePath));tuning.glintResponse=true;tuning.specularWorkflow=true;tuning.specular=.35f;File.WriteAllText("Library/CrestRiver.request",JsonUtility.ToJson(tuning));}
        [MenuItem("CityForge/Water/Remove Crest river preview")] static void Remove()=>File.WriteAllText("Library/CrestRiver.request","remove");
        static CrestRiverStudy(){EditorApplication.update+=Poll;}
        static void Poll(){const string path="Library/CrestRiver.request";if(!File.Exists(path)||EditorApplication.isCompiling||EditorApplication.isUpdating||!EditorApplication.isPlaying)return;var command=File.ReadAllText(path).Trim();File.Delete(path);Directory.CreateDirectory("Library/CrestRiverStudy");try{CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Run(command)));}catch(Exception e){File.WriteAllText("Library/CrestRiverStudy/Status.txt",e.ToString());}}
        static IEnumerator Guard(IEnumerator routine){while(true){object current;try{if(!routine.MoveNext())yield break;current=routine.Current;}catch(Exception e){File.WriteAllText("Library/CrestRiverStudy/Status.txt","FAIL: "+e);Debug.LogException(e);yield break;}yield return current;}}
        [Serializable] class Tuning {public bool glintResponse=false,specularWorkflow=false,bed=false,sunlightView=false,shoreView=false,boundaryCheck=false,diagnostics=false,adaptive=false,waterView=false,natural=false;public int captureFrames=24;public float captureInterval=.125f,rippleEvolution=.018f,rippleEvolutionSpeed=1;public float viewPitch=-1;public float glintStart=.15f,glintEnd=.5f,glintGain=1;public float normalBlend=0;public string label="";public int projection=-1;public float naturalScale=20;public float yaw=45,pitch=CityForgeCameraController.IsometricPitch,normal=.035f,scale=60,smoothness=.7f,reflection=.4f,specular=.02f;}
        static IEnumerator Run(string command)
        {
            var land=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>().ApprovedDistrict;
            var preview=land.GetComponentInChildren<CrestDistrictRiverPreview>();
            var rig=land.View.GetComponentInChildren<CityForgeCameraController>();
            if(!preview&&command!="remove"){if(!land.View.AnimalsEnabled)land.View.SetAnimalsEnabled(true);land.Wildlife.FindRiverLife();}rig.SetZoomLevel(1);
            if(command=="remove"){if(preview)UnityEngine.Object.Destroy(preview.gameObject);yield break;}
            if(!preview){var surface=land.GetComponentsInChildren<LakeWaterRenderer>().First(s=>s.SurfaceMaterial.GetFloat("_RiverSurface")>.5f);var holder=new GameObject("Crest bounded river trial");holder.transform.SetParent(land.transform,false);preview=holder.AddComponent<CrestDistrictRiverPreview>();var initialization=preview.Initialize(land,surface);while(initialization.MoveNext())yield return initialization.Current;}
            var clipMatrix=preview.ClipRegion.localToWorldMatrix;
            var camera=land.View.GetComponentInChildren<Camera>();
            var target=RenderTexture.GetTemporary(1600,Mathf.RoundToInt(1600/camera.aspect),24,RenderTextureFormat.ARGBHalf);var read=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try {
                foreach(int style in command=="capture"?new[]{0,1,2}:new[]{int.TryParse(command,out var parsed)?parsed:0}) {
                    preview.Configure(style);
                    if(command.StartsWith("{")){var tuning=JsonUtility.FromJson<Tuning>(command);if(tuning.projection>=0&&rig.CaptureRegionState().lowerView!=(tuning.projection==1))rig.ToggleViewAngle();if(tuning.shoreView)land.Wildlife.FindRiverLife();if(tuning.sunlightView){var pose=rig.CaptureRegionState();if(tuning.waterView){
                        var direction=Quaternion.Euler(12,tuning.yaw,0)*Vector3.forward;float distance=pose.size/Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);float best=float.PositiveInfinity;
                        for(int z=1;z<32;z++)for(int x=1;x<32;x++){var point=land.Origin+new Vector2(x/32f,z/32f)*land.Side;var position=new Vector3(point.x,preview.WaterDomain.center.y+1,point.y);var eye=position-direction*distance;
                            if(land.Height(point)>=position.y-2||!land.Contains(new Vector2(eye.x,eye.z))||land.Height(new Vector2(eye.x,eye.z))>eye.y-9)continue;
                            float score=(position-preview.WaterDomain.center).sqrMagnitude;if(score<best){best=score;pose.focus=position;}}
                        if(float.IsPositiveInfinity(best))throw new Exception("No unobstructed river inspection camera position");
                    }pose.yaw=tuning.yaw;pose.pitch=tuning.pitch;rig.RestoreRegionState(pose);}preview.ShowBed(tuning.bed);var m=preview.Settings;var targetShader=tuning.glintResponse?AssetDatabase.LoadAssetAtPath<Shader>(RiverGlintShaderStudy.ShaderPath):land.Baseline.waterTemplate.GetComponent<WaveHarmonic.Crest.WaterRenderer>().Surface.Material.shader;if(!targetShader)throw new Exception("Build the local glint shader before using the selective preview");if(m.shader!=targetShader){if(m.isVariant)m.parent=null;m.shader=targetShader;}if(tuning.glintResponse){m.SetFloat("_CityForgeGlintStart",tuning.glintStart);m.SetFloat("_CityForgeGlintEnd",tuning.glintEnd);m.SetFloat("_CityForgeGlintGain",tuning.glintGain);m.SetFloat("_CityForgeRippleEvolution",tuning.rippleEvolution);m.SetFloat("_CityForgeRippleEvolutionSpeed",tuning.rippleEvolutionSpeed);}m.SetFloat("_Crest_NormalMapStrength",tuning.normal);m.SetFloat("_Crest_NormalMapScale",tuning.scale);m.SetFloat("_Crest_Smoothness",tuning.smoothness);m.SetFloat("_Crest_SmoothnessFar",tuning.smoothness);m.SetFloat("_Crest_PlanarReflectionsIntensity",tuning.reflection);m.SetFloat("_Crest_Specular",tuning.specular);m.SetFloat("_WorkflowMode",tuning.specularWorkflow?0:1);if(tuning.specularWorkflow)m.EnableKeyword("_SPECULAR_SETUP");else m.DisableKeyword("_SPECULAR_SETUP");preview.SetNormalBlend(tuning.normalBlend);if(tuning.viewPitch>=12)rig.SetViewTilt(Mathf.InverseLerp(12,CityForgeCameraController.IsometricPitch,tuning.viewPitch));}
                    File.WriteAllText("Library/CrestRiverStudy/Material.txt",preview.Settings.shader.name+"\n"+string.Join("\n",new[]{"_Crest_NormalMapStrength","_Crest_NormalMapScale","_Crest_Smoothness","_Crest_Specular"}.Select(p=>p+"="+preview.Settings.GetFloat(p)))+"\n"+string.Join(",",preview.Settings.shaderKeywords));
                    foreach(int zoom in (command.StartsWith("{")&&JsonUtility.FromJson<Tuning>(command).boundaryCheck)?new[]{0,2,5}:(command=="capture"||command.StartsWith("{"))?new[]{1,2}:new[]{1}) {
                        rig.SetZoomLevel(zoom);var initialPose=rig.CaptureRegionState();float warm=Time.realtimeSinceStartup+1;while(Time.realtimeSinceStartup<warm)yield return null;
                        string label=command.StartsWith("{")?JsonUtility.FromJson<Tuning>(command).label:"";
                        string folder="Library/CrestRiverStudy/"+label+"Style"+style+"-Zoom"+zoom+"/";Directory.CreateDirectory(folder);
                        for(int frame=0;frame<(command.StartsWith("{")?Mathf.Clamp(JsonUtility.FromJson<Tuning>(command).captureFrames,2,120):command=="capture"?24:1);frame++) {
                            if(command.StartsWith("{")&&JsonUtility.FromJson<Tuning>(command).boundaryCheck&&frame==12){var pan=rig.CaptureRegionState();pan.focus+=new Vector3(100,0,-80);rig.RestoreRegionState(pan);float settle=Time.realtimeSinceStartup+.5f;while(Time.realtimeSinceStartup<settle)yield return null;}
                            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                            var cascades=Shader.GetGlobalVectorArray("g_Crest_SamplingParametersCascadeClip");var outer=cascades[preview.WaterEngine.LodLevels-1];float half=outer.z*preview.WaterEngine.ClipLod.Resolution*.5f-outer.z*2;var domain=preview.WaterDomain;
                            if(outer.x-half>domain.min.x||outer.x+half<domain.max.x||outer.y-half>domain.min.z||outer.y+half<domain.max.z)throw new Exception("Crest clip cascades do not cover the complete stationary district");
                            if(preview.GeneratedRendererCount==0||!preview.BoundaryClipEnabled||preview.ClipRegion.localToWorldMatrix!=clipMatrix)throw new Exception("Native Crest must use the same stationary district clip boundary at every zoom");RenderTexture.active=target;read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply();File.WriteAllBytes(folder+frame.ToString("D3")+".png",read.EncodeToPNG());
                            File.WriteAllText("Library/CrestRiverStudy/Status.txt",$"Style {style}, zoom {zoom}, frame {frame}");float next=Time.realtimeSinceStartup+(command.StartsWith("{")?Mathf.Clamp(JsonUtility.FromJson<Tuning>(command).captureInterval,.04f,.5f):.125f);while(Time.realtimeSinceStartup<next)yield return null;
                        }
                        rig.RestoreRegionState(initialPose);
                    }
                }
                var original=land.GetComponentsInChildren<LakeWaterRenderer>().First(s=>s.SurfaceMaterial.GetFloat("_RiverSurface")>.5f);
                var bounds=original.GetComponent<MeshFilter>().sharedMesh.bounds;var center=preview.WaterDomain.center;var size=preview.WaterDomain.size;
                bool inside=center.x-size.x/2>=bounds.min.x&&center.x+size.x/2<=bounds.max.x&&center.z-size.z/2>=bounds.min.z&&center.z+size.z/2<=bounds.max.z;
                if(original.GetComponent<MeshRenderer>().enabled||!inside||ShaderUtil.ShaderHasError(preview.Settings.shader))throw new Exception("Crest preview material, fallback visibility or bounded geometry check failed");
                File.WriteAllText("Library/CrestRiverStudy/Checks.txt","PASS: native Crest/Water material; fallback disabled; fixed world-space clip boundary inside geographic mesh bounds; clip transform unchanged across captured zooms and100m/-80m pans; outer GPU clip cascade covers the complete district with a two-texel margin on every captured frame; native shader compiles. Appearance and performance remain a preview study; no player saves written.");
                if(command.StartsWith("{")&&JsonUtility.FromJson<Tuning>(command).diagnostics){
                    var water=preview.WaterEngine;
                    var report=new System.Text.StringBuilder($"Origin {land.Origin} side {land.Side} level {preview.WaterDomain.center.y} water {water.transform.position} scale {water.Scale} range {water.ScaleRange} focus {rig.CaptureRegionState().focus}\n");
                    foreach(var key in new[]{"g_Crest_SamplingParametersCascadeClip","g_Crest_CascadeData"}){report.AppendLine(key);foreach(var v in Shader.GetGlobalVectorArray(key))report.AppendLine(v.ToString("F3"));}
                    foreach(var r in water.GetComponentsInChildren<MeshRenderer>())report.AppendLine($"{r.name}: {r.bounds} enabled {r.enabled}");
                    File.WriteAllText("Library/CrestRiverStudy/Diagnostics.txt",report.ToString());
                    water.ClipLod.Enabled=false;
                    float wait=Time.realtimeSinceStartup+2;while(Time.realtimeSinceStartup<wait)yield return null;
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply();File.WriteAllBytes("Library/CrestRiverStudy/Unclipped.png",read.EncodeToPNG());
                    water.ClipLod.Enabled=true;
                }
                File.WriteAllText("Library/CrestRiverStudy/Status.txt","COMPLETE");
            }finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(read);}
        }
    }
}
#endif
