using System.Collections;using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WaveHarmonic.Crest;
using CityForge.CameraSystem;
namespace CityForge.World
{
    /// <summary>Isolated Crest surface trial. Uses a stationary geographic clip boundary and restores the fallback on removal.</summary>
    public sealed class CrestDistrictRiverPreview : MonoBehaviour
    {
        WaterRenderer water; Material master; Texture2D flow,shoreClip;
        Transform clipRegion;
        Texture originalNormals;Texture2D blendedNormals;float blendedAmount=-1;
        public void SetNormalBlend(float amount){
            if(!originalNormals)originalNormals=master.GetTexture("_Crest_NormalMapTexture");
            if(amount<=0){master.SetTexture("_Crest_NormalMapTexture",originalNormals);return;}
            if(!blendedNormals||!Mathf.Approximately(amount,blendedAmount)){
                if(blendedNormals)Destroy(blendedNormals);
                var shader=Shader.Find("Hidden/CityForge/RiverNormalBlend");if(!shader)throw new System.InvalidOperationException("Missing river normal blend study shader");
                var material=new Material(shader);material.SetFloat("_Blend",amount);
                var target=RenderTexture.GetTemporary(1024,1024,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var old=RenderTexture.active;
                try{Graphics.Blit(originalNormals,target,material);RenderTexture.active=target;blendedNormals=new Texture2D(1024,1024,TextureFormat.RGBA32,true,true){name="Layered river normal study",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};blendedNormals.ReadPixels(new Rect(0,0,1024,1024),0,0);blendedNormals.Apply(true,true);blendedAmount=amount;}
                finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);Destroy(material);}
            }
            master.SetTexture("_Crest_NormalMapTexture",blendedNormals);
        }
        MeshRenderer fallback; GameObject engine; Transform viewpoint;
        MeadowDistrictLandscape landscape;float level;
        Camera viewer; CityForgeCameraController rig; bool oldColor, oldDepth;
        public WaterRenderer WaterEngine => water;
        public Material Surface => master;
        public Transform ClipRegion => clipRegion;
        public bool BoundaryClipEnabled => water && water.ClipLod.Enabled && shoreClip;
        public Bounds WaterDomain => new Bounds(new Vector3(landscape.Origin.x+landscape.Side/2,level,landscape.Origin.y+landscape.Side/2),new Vector3(landscape.Side,1,landscape.Side));
        public Material Settings => master;
        struct RiverReach { public Vector2 a,b;public float radiusSquared; }
        const float RiverCellSide=32;
        readonly Dictionary<Vector2Int,List<RiverReach>> legacyRiverCells=new();
        // Legacy WaterDistance is unsigned distance to the centerline, not a wet/dry mask.
        // Index the canonical river widths; broad low meadow must remain dry.
        void BuildLegacyRiverFootprint(){
            if(landscape.View.CreationSettings?.version>=5)return;
            foreach(var body in landscape.View.Data.WaterBodies){
                if(body.kind!=WaterBodyKind.River||body.centerline==null)continue;
                float radius=body.halfWidthNormalized*landscape.View.MapSide;
                for(int i=1;i<body.centerline.Length;i++){
                    var a=(body.centerline[i-1]-Vector2.one*.5f)*landscape.View.MapSide;var b=(body.centerline[i]-Vector2.one*.5f)*landscape.View.MapSide;
                    var min=Vector2Int.FloorToInt((Vector2.Min(a,b)-Vector2.one*radius)/RiverCellSide);var max=Vector2Int.FloorToInt((Vector2.Max(a,b)+Vector2.one*radius)/RiverCellSide);
                    var reach=new RiverReach{a=a,b=b,radiusSquared=radius*radius};
                    for(int z=min.y;z<=max.y;z++)for(int x=min.x;x<=max.x;x++){var cell=new Vector2Int(x,z);if(!legacyRiverCells.TryGetValue(cell,out var list))legacyRiverCells[cell]=list=new List<RiverReach>();list.Add(reach);}
                }
            }
        }
        public bool ContainsWetPoint(Vector2 point){
            if(landscape.Height(point)>=level-.02f)return false;
            if(landscape.View.CreationSettings?.version>=5)return true;
            if(!legacyRiverCells.TryGetValue(Vector2Int.FloorToInt(point/RiverCellSide),out var reaches))return false;
            foreach(var reach in reaches){var delta=reach.b-reach.a;float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-reach.a,delta)/delta.sqrMagnitude):0;if((point-reach.a-delta*t).sqrMagnitude<=reach.radiusSquared)return true;}
            return false;
        }
        public int GeneratedRendererCount => engine?engine.GetComponentsInChildren<MeshRenderer>(true).Length:0;
        public int EnabledGeneratedRendererCount {get{int count=0;if(engine)foreach(var renderer in engine.GetComponentsInChildren<MeshRenderer>(true))if(renderer.enabled)count++;return count;}}
        public IEnumerator Initialize(MeadowDistrictLandscape land, LakeWaterRenderer original, Material template=null)
        {
            landscape=land;level=original.SurfaceLevel;viewer=land.View.GetComponentInChildren<Camera>();rig=viewer.GetComponent<CityForgeCameraController>();
            BuildLegacyRiverFootprint();
            var cameraData=viewer.GetUniversalAdditionalCameraData();oldColor=cameraData.requiresColorTexture;oldDepth=cameraData.requiresDepthTexture;
            cameraData.requiresColorTexture=true;cameraData.requiresDepthTexture=true;
            fallback=original.GetComponent<MeshRenderer>();
            master=new Material(template?template:land.Baseline.waterTemplate.GetComponent<WaterRenderer>().Surface.Material);
            engine=Instantiate(land.Baseline.waterTemplate,transform);engine.SetActive(false);engine.transform.position=Vector3.up*original.SurfaceLevel;
            water=engine.GetComponent<WaterRenderer>();water.Viewer=viewer;
            viewpoint=new GameObject("River simulation focus").transform;viewpoint.SetParent(transform,false);water.Viewpoint=viewpoint;
            water.Surface.Material=master;water.Surface.SupportCustomRenderers=true;
            water.Reflections.Enabled=true;water.Reflections.Resolution=512;water.Reflections.AllowMSAA=false;water.Reflections.DisableShadows=true;water.Reflections.RenderOnlySingleCamera=true;water.Reflections.Layers=~(1<<4);
            water.Underwater.Enabled=false;water.AnimatedWavesLod.Enabled=false;water.DynamicWavesLod.Enabled=false;water.FoamLod.Enabled=false;water.ShadowLod.Enabled=false;water.LevelLod.Enabled=false;water.AlbedoLod.Enabled=false;water.AbsorptionLod.Enabled=false;water.ClipLod.Enabled=true;water.ClipLod.Resolution=1024;water.ClipLod.DefaultClippingState=DefaultClippingState.EverythingClipped;
            water.FlowLod.Enabled=true;water.FlowLod.Resolution=128;water.DepthLod.Enabled=true;water.DepthLod.Resolution=128;
            // Keep the outer simulation cascade beyond the complete district even at close zoom.
            // The ocean template caps scale at32; four cascades only cover1024m around the moving focus.
            water.ScaleRange=new Vector2(8,256);
            water.LodResolution=128;water.LodLevels=8;water.GeometryDownSampleFactor=2;water.DefaultExcludes=WaterBodyAffects.Nothing;
            var center=new Vector3(land.Origin.x+land.Side/2,original.SurfaceLevel,land.Origin.y+land.Side/2);
            var clipObject=new GameObject("Stationary district river boundary");clipRegion=clipObject.transform;clipRegion.SetParent(transform,false);clipRegion.position=center;
            // Unclip the enclosing area first; the following authoritative wet mask
            // removes both the exterior and water hidden underneath raised land.
            const float padding=32;float clipSide=land.Side+padding*2;
            clipRegion.localScale=new Vector3(clipSide,40,clipSide);
            var clip=WaveHarmonic.Crest._Extensions.AddComponent<ClipLodInput>(clipObject,LodInputMode.Primitive);clip.Primitive=LodInputPrimitive.Quad;clip.Inverted=true;
            clip.Queue=0;
            const int clipSize=1024;var clipPixels=new Color[clipSize*clipSize];
            for(int z=0;z<clipSize;z++)for(int x=0;x<clipSize;x++){
                var point=land.Origin-Vector2.one*padding+new Vector2((x+.5f)/clipSize,(z+.5f)/clipSize)*clipSide;
                bool within=point.x>=land.Origin.x+.5f&&point.x<=land.Origin.x+land.Side-.5f&&point.y>=land.Origin.y+.5f&&point.y<=land.Origin.y+land.Side-.5f;
                clipPixels[z*clipSize+x]=within&&ContainsWetPoint(point)?Color.black:Color.white;
            }
            shoreClip=new Texture2D(clipSize,clipSize,TextureFormat.R8,false,true){name="Authoritative river wet footprint",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};shoreClip.SetPixels(clipPixels);shoreClip.Apply(false,true);
            var shoreObject=new GameObject("River wet footprint and dry-land exclusion");shoreObject.transform.SetParent(transform,false);shoreObject.transform.position=center;shoreObject.transform.localScale=new Vector3(clipSide,1,clipSide);
            var shoreInput=WaveHarmonic.Crest._Extensions.AddComponent<ClipLodInput>(shoreObject,LodInputMode.Texture);shoreInput.Queue=10;shoreInput.GetData<ClipTextureLodInputData>().Texture=shoreClip;
            var inputObject=new GameObject("Seeded downstream current");inputObject.transform.SetParent(transform,false);inputObject.transform.position=center;inputObject.transform.localScale=new Vector3(land.Side,1,land.Side);
            const int size=128;flow=new Texture2D(size,size,TextureFormat.RGBAHalf,false,true){wrapMode=TextureWrapMode.Clamp};var pixels=new Color[size*size];
            for(int z=0;z<size;z++)for(int x=0;x<size;x++){var p=land.Origin+new Vector2((x+.5f)/size,(z+.5f)/size)*land.Side;var direction=LakeWaterRenderer.RiverCurrentCoordinates(land.View.Data,land.View.MapSide,p);pixels[z*size+x]=new Color(direction.z*.5f+.5f,direction.w*.5f+.5f,0,1);}
            flow.SetPixels(pixels);flow.Apply();var input=WaveHarmonic.Crest._Extensions.AddComponent<FlowLodInput>(inputObject,LodInputMode.Texture);var data=input.GetData<FlowTextureLodInputData>();data.Texture=flow;data.Multiplier=new Vector4(.55f,.55f,1,1);data.NegativeValues=false;
            var probe=new GameObject("River bed depth").AddComponent<DepthProbe>();probe.transform.SetParent(transform,false);probe.transform.position=center;probe.transform.localScale=new Vector3(land.Side,1,land.Side);probe.Type=DepthProbeMode.RealTime;probe.RefreshMode=DepthProbeRefreshMode.ViaScripting;probe.Layers=1<<30;probe.Resolution=512;probe.SavedTexture=null;probe.CaptureRange=new Vector2(-15,land.DisplayTerrain.terrainData.size.y+10);probe.GenerateSignedDistanceField=false;
            Configure(0);LateUpdate();engine.SetActive(true);fallback.enabled=false;
            yield return null;
            var current=gameObject.AddComponent<RiverFlowAnimation>();current.water=water;current.flowMap=flow;current.flowRegion=new Vector4(land.Origin.x,land.Origin.y,1/land.Side,.55f);current.visualSpeedMultiplier=1;
            yield return null;probe.Populate();
        }
        public void ShowBed(bool enabled)
        {
            var bed=GetComponentInChildren<CrestRiverBedPreview>();
            if(!bed&&enabled){var host=new GameObject("Shallow pebble bed trial");host.transform.SetParent(transform,false);bed=host.AddComponent<CrestRiverBedPreview>();bed.Initialize(landscape,level);}
            if(bed&&bed.TryGetComponent<MeshRenderer>(out var renderer))renderer.enabled=enabled;
        }
        public void Configure(int style)
        {
            if(!master)return;
            master.SetFloat("_Crest_NormalMapEnabled",1);master.SetFloat("_Crest_NormalMapStrength",style==1?.08f:.16f);master.SetFloat("_Crest_NormalMapScale",style==2?8:3);master.SetFloat("_Crest_NormalMapScrollSpeed",0);
            master.SetFloat("_Crest_Smoothness",style==1?.82f:.9f);master.SetFloat("_Crest_SmoothnessFar",.82f);master.SetFloat("_Crest_FoamEnabled",0);master.SetFloat("_Crest_CausticsEnabled",0);master.SetFloat("_Crest_PlanarReflectionsEnabled",1);master.SetFloat("_Crest_PlanarReflectionsIntensity",.7f);
            master.SetFloat("_Crest_PlanarReflectionsRoughness",0);master.SetFloat("_Crest_PlanarReflectionsApplySmoothness",0);
            master.SetColor("_Crest_Absorption",new Color(.35f,.12f,.08f,0));master.SetColor("_Crest_Scattering",new Color(.018f,.07f,.085f,1));master.EnableKeyword("_CREST_FLOW_LOD");
        }
        void LateUpdate(){if(!viewpoint||!rig||!engine)return;var focus=rig.CaptureRegionState().focus;viewpoint.position=new Vector3(focus.x,engine.transform.position.y+Mathf.Max(8,viewer.orthographicSize*.3f),focus.z);
        }
        void OnDestroy(){if(fallback)fallback.enabled=true;if(viewer){var data=viewer.GetUniversalAdditionalCameraData();data.requiresColorTexture=oldColor;data.requiresDepthTexture=oldDepth;}if(master)Destroy(master);if(flow)Destroy(flow);if(shoreClip)Destroy(shoreClip);if(blendedNormals)Destroy(blendedNormals);}
    }
}
