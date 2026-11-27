#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using CityForge.World;

namespace CityForge.EditorTools
{
    [InitializeOnLoad] static class SurfaceWeatheringPerformance
    {
        static IEnumerator job;
        static SurfaceWeatheringPerformance() { EditorApplication.update += Update; }
        static void Update()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if(job != null)
            {
                try { if(!job.MoveNext()) job=null; }
                catch(Exception e) { Debug.LogException(e); (job as IDisposable)?.Dispose();job=null; }
                return;
            }
            const string request="Library/SurfaceWeatheringPerformance.request";
            if(!File.Exists(request))return;
            string output=File.ReadAllText(request).Trim();File.Delete(request);job=Run(output);
        }
        static IEnumerator Run(string output)
        {
            Directory.CreateDirectory(output);
            var scene=EditorSceneManager.NewPreviewScene();
            var mat=new Material(Resources.Load<Shader>("Region/LotPathBlend"));
            var profile=UnityEngine.Object.Instantiate(Resources.Load<SurfaceWeatheringProfile>("SurfaceWeathering/Road"));
            float defaultIntensity=profile.intensity;
            var rt=new RenderTexture(1024,768,24);rt.Create();
            var gpuProperty=typeof(EditorApplication).Assembly.GetType("UnityEditorInternal.ProfilerDriver")?.GetProperty("gpuProfiling",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
            object previousGpu=gpuProperty?.GetValue(null);
            bool previousProfiler=Profiler.enabled;
            bool previousAsync=ShaderUtil.allowAsyncCompilation;
            try
            {
                if(gpuProperty?.CanWrite==true)gpuProperty.SetValue(null,true);
                Profiler.enabled=true;ShaderUtil.allowAsyncCompilation=false;
                var cameraObject=new GameObject("Weathering performance camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.targetTexture=rt;camera.transform.position=new Vector3(0,10,0);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.orthographic=true;camera.orthographicSize=5;
                var plane=GameObject.CreatePrimitive(PrimitiveType.Plane);SceneManager.MoveGameObjectToScene(plane,scene);plane.GetComponent<MeshRenderer>().sharedMaterial=mat;
                mat.SetFloat("_PathOpacity",1);profile.Apply(mat,false);
                var sampler=CustomSampler.Create("CityForge.SurfaceWeathering.GPU",true);var recorder=sampler.GetRecorder();recorder.enabled=true;
                var log=new List<string>{"1024x768 fixed preview camera, one surface draw; 20 warmup + 80 measured editor updates per condition.","GPU: "+SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType};
                for(int condition=0;condition<2;condition++)
                {
                    profile.intensity=condition==0?0:defaultIntensity;profile.Apply(mat,false);
                    var gpu=new List<double>();var cpu=new List<double>();
                    for(int frame=0;frame<100;frame++)
                    {
                        sampler.Begin();camera.Render();sampler.End();
                        EditorApplication.QueuePlayerLoopUpdate();
                        yield return null;
                        if(frame<20)continue;
                        if(recorder.gpuElapsedNanoseconds>0)gpu.Add(recorder.gpuElapsedNanoseconds/1e6);
                        if(recorder.elapsedNanoseconds>0)cpu.Add(recorder.elapsedNanoseconds/1e6);
                    }
                    string stats(List<double> values)=>values.Count==0?"unavailable":($"median={values.OrderBy(x=>x).ElementAt(values.Count/2):F4}ms, n={values.Count}");
                    log.Add((condition==0?"Disabled":"Enabled")+": GPU "+stats(gpu)+"; CPU render submission "+stats(cpu));
                }
                recorder.enabled=false;
                log.Add("CPU submission timing is not GPU time. This isolated editor workload is not a city-scale gameplay benchmark.");
                File.WriteAllLines(output+"/performance.txt",log);
            }
            finally
            {
                Profiler.enabled=previousProfiler;ShaderUtil.allowAsyncCompilation=previousAsync;
                if(gpuProperty?.CanWrite==true && previousGpu!=null)gpuProperty.SetValue(null,previousGpu);
                EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(mat);UnityEngine.Object.DestroyImmediate(profile);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
#endif
