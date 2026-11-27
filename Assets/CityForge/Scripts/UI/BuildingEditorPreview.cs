using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.UI
{
    // Isolated render request: no world camera, lighting settings or source material is retained/mutated.
    internal sealed class BuildingEditorPreview : IDisposable
    {
        const int Layer = 30;
        readonly GameObject stage;
        readonly Camera camera;
        readonly Light key;
        readonly RenderTexture target;
        readonly Volume volume;
        GameObject model;
        Bounds bounds;
        bool night, disposed;
        float yaw = 225;
        internal Texture Texture => target;
        internal BuildingEditorPreview(int width=624, int height=816)
        {
            stage = new GameObject("Building customization preview"); stage.transform.position = new Vector3(12000, -6000, 0);
            camera = new GameObject("Building editor camera").AddComponent<Camera>(); camera.transform.SetParent(stage.transform, false);
            camera.enabled = false; camera.orthographic = true; camera.aspect = width/(float)height; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f,.15f,.21f); camera.cullingMask = 1 << Layer; camera.allowHDR = true;
            var data = camera.GetUniversalAdditionalCameraData(); data.volumeLayerMask = 1 << Layer; data.renderPostProcessing = true;
            key = new GameObject("Building editor key").AddComponent<Light>(); key.transform.SetParent(stage.transform,false);
            key.type = LightType.Directional; key.cullingMask = 1 << Layer; key.enabled = false;
            var v = new GameObject("Building editor glow"); v.layer = Layer; v.transform.SetParent(stage.transform,false);
            volume = v.AddComponent<Volume>(); volume.isGlobal=true; volume.priority=1000; volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom=volume.profile.Add<Bloom>(); bloom.threshold.Override(1); bloom.intensity.Override(.12f); bloom.scatter.Override(.45f);
            target = new RenderTexture(width,height,24,RenderTextureFormat.ARGBHalf) { name="Building editor preview" }; target.Create();
        }
        internal void Set(LotAssetCatalogEntry asset, BuildingAppearanceSettings appearance=default,string buildingId=null)
        {
            if(model){model.SetActive(false);DestroyTransient(model);}
            model=LotRuntimeLoader.InstantiateObject(new LotObjectDefinition{id=buildingId??"appearance-preview-"+asset.id,assetId=asset.id,buildingAppearance=appearance},asset,stage.transform);
            foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            foreach(var c in model.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(var l in model.GetComponentsInChildren<LODGroup>())l.ForceLOD(0);
            var rs=model.GetComponentsInChildren<Renderer>();
            bounds=rs.Length>0?rs[0].bounds:new Bounds(stage.transform.position,Vector3.one);
            foreach(var r in rs)bounds.Encapsulate(r.bounds);
            SetTime(night);
        }
        internal void Paint(BuildingAppearanceSettings appearance){if(model){model.GetComponent<BuildingPaint>()?.Set(appearance);model.GetComponent<BuildingWindowFrames>()?.Set(appearance);BuildingArchitecturalWeathering.Set(model,appearance);model.GetComponent<BuildingSignInstances>()?.Set(appearance);SetTime(night);}}
        internal void Turn(float degrees){yaw+=degrees;}
        internal void SetTime(bool value)
        {
            night=value;if(!model)return;
            foreach(var light in model.GetComponentsInChildren<AuthoredBuildingLighting>(true))light.SetPreviewTime(night?RegionTimeOfDay.Night:RegionTimeOfDay.Afternoon,1<<Layer);
            foreach(var sign in model.GetComponentsInChildren<AuthoredSignLighting>(true))sign.SetPreviewTime(night?RegionTimeOfDay.Night:RegionTimeOfDay.Afternoon);
            var probe=PrototypeRegionView.LotDaylightProbe();
            if(night)for(int rgb=0;rgb<3;rgb++)for(int i=0;i<9;i++)probe[rgb,i]*=.2f;
            var block=new MaterialPropertyBlock();
            foreach(var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.lightProbeUsage=LightProbeUsage.CustomProvided;r.GetPropertyBlock(block);block.CopySHCoefficientArraysFrom(new[]{probe});r.SetPropertyBlock(block);
                for(int i=0;i<r.sharedMaterials.Length;i++){block.Clear();r.GetPropertyBlock(block,i);if(block.isEmpty)continue;block.CopySHCoefficientArraysFrom(new[]{probe});r.SetPropertyBlock(block,i);}
            }
        }
        internal void Render()
        {
            if(!model)return;
            var rotation=Quaternion.Euler(17,yaw,0);float radius=Mathf.Max(1,bounds.extents.magnitude);
            camera.transform.SetPositionAndRotation(bounds.center-rotation*Vector3.forward*(radius*3),rotation);
            var inv=Quaternion.Inverse(rotation);float extent=.1f;
            for(int i=0;i<8;i++){var p=inv*Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));extent=Mathf.Max(extent,Mathf.Abs(p.y),Mathf.Abs(p.x)/camera.aspect);}
            camera.orthographicSize=extent*1.12f;camera.nearClipPlane=.1f;camera.farClipPlane=radius*6;
            key.transform.rotation=Quaternion.Euler(40,yaw+30,0);key.intensity=night?.18f:1.3f;
            var sun=RenderSettings.sun;var masks=new List<(Light light,int mask)>();
            try
            {
                foreach(var l in Light.GetLights(LightType.Directional,Layer)){if(!l||l==key)continue;masks.Add((l,l.cullingMask));l.cullingMask&=~(1<<Layer);}
                key.enabled=true;RenderSettings.sun=key;
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);
            }
            finally{RenderSettings.sun=sun;key.enabled=false;foreach(var m in masks)if(m.light)m.light.cullingMask=m.mask;}
        }
        static void DestroyTransient(UnityEngine.Object obj){if(!obj)return;if(Application.isPlaying)UnityEngine.Object.Destroy(obj);else UnityEngine.Object.DestroyImmediate(obj);}
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            if(target){target.Release();DestroyTransient(target);}
            if(volume&&volume.profile){foreach(var component in volume.profile.components)DestroyTransient(component);DestroyTransient(volume.profile);}
            if(stage){stage.SetActive(false);DestroyTransient(stage);}
        }
    }
}
