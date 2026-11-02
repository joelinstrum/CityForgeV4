using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.World
{
    /// <summary>Bounded calm water with cached terrain depth; lakes/ponds also capture the local environment.</summary>
    public sealed class LakeWaterRenderer : MonoBehaviour
    {
        // 0 uses the saved production route, +/-1 explicitly selects a comparison alternative.
        static int evaluationOverride;
        public static bool EnabledForEvaluation {get=>evaluationOverride==1;set=>evaluationOverride=value?1:-1;}
        public static void UseSavedRenderer() {evaluationOverride=0;}
        public static bool UsesCalmWater(PrototypeRegionView view)=>evaluationOverride==0?view.LakeWaterVersion>0:evaluationOverride>0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetEvaluation() { evaluationOverride=0;LakeShoreRenderer.EnabledForEvaluation=true; }
        public static bool Applies(RegionWaterBody body,PrototypeRegionView view) => UsesCalmWater(view) &&
            (body.kind == WaterBodyKind.Lake || body.kind == WaterBodyKind.Pond);
        Mesh mesh;
        Material material;
        Texture2D depth,riverFlow;
        ReflectionProbe probe;
        public bool CaptureReady { get; private set; }
        public Texture CapturedEnvironment => probe ? probe.texture : null;
        public float GenerationMilliseconds { get; private set; }
        public int CaptureCount {get;private set;}
        public Material SurfaceMaterial=>material;
        public float SurfaceLevel {get;private set;}
        public string BodyId {get;private set;}
        public void Initialize(MeadowDistrictLandscape land, RegionWaterBody body)
        {
            if(!Applies(body,land.View))throw new System.ArgumentException("Calm water requires a typed lake or pond.");
            var start=Time.realtimeSinceStartup;
            float level=body.hasSurfaceLevel?body.surfaceLevelNormalized*land.DisplayTerrain.terrainData.size.y:land.View.SeaLevel;
            if(LakeShoreRenderer.EnabledForEvaluation)gameObject.AddComponent<LakeShoreRenderer>().Initialize(land,body,level);
            InitializeSurface(land.View,body,new Rect(land.Origin,Vector2.one*land.Side),level,land.Height,p=>land.Noise(p,18,252));
            GenerationMilliseconds=(Time.realtimeSinceStartup-start)*1000;
        }
        // Generated river beds extend beyond the nominal centerline ribbon. Cover the
        // saved bed up to its real terrain/level intersection, using fixed world geometry.
        public void InitializeGeneratedRiver(MeadowDistrictLandscape land,RegionWaterBody body)
        {
            if(body.kind!=WaterBodyKind.River)
                throw new System.ArgumentException("Expected a generated regional river.");
            float level=body.hasSurfaceLevel?body.surfaceLevelNormalized*land.DisplayTerrain.terrainData.size.y:land.View.SeaLevel;
            InitializeSurface(land.View,body,new Rect(land.Origin,Vector2.one*land.Side),level,land.Height,
                p=>land.Noise(p,18,252),true);
            material.name="Bounded generated river currents";
            BuildRiverCurrentField(land.View,land);
            land.View.GetComponentInChildren<Camera>().GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            material.SetFloat("_RiverSceneDepth",1);material.SetFloat("_CurrentPreviewTime",-1);
            material.SetColor("_Deep",new Color(.025f,.095f,.16f,1));
            material.SetColor("_Shallow",new Color(.065f,.16f,.19f,1));
        }
        // Match RegionWaterBody.DistanceToCenterline: downstream is opposite centerline order.
        public static Vector4 RiverCurrentCoordinates(PrototypeRegionData data,float side,Vector2 world){
            float best=float.PositiveInfinity;Vector4 result=new Vector4(0,0,0,-1);
            var point=world/side+Vector2.one*.5f;
            foreach(var river in data.WaterBodies){if(river.kind!=WaterBodyKind.River||river.centerline==null)continue;
                float length=0;
                for(int i=1;i<river.centerline.Length;i++){
                    var a=river.centerline[i-1];var delta=river.centerline[i]-a;float segment=delta.magnitude;if(segment<.000001f)continue;
                    float t=Mathf.Clamp01(Vector2.Dot(point-a,delta)/(segment*segment));var offset=point-a-delta*t;float distance=offset.sqrMagnitude;
                    if(distance<best){best=distance;var downstream=-delta/segment;var cross=new Vector2(-downstream.y,downstream.x);
                        result=new Vector4(-(length+t*segment)*side,Vector2.Dot(offset,cross)*side,downstream.x,downstream.y);}
                    length+=segment;
                }
            }
            return result;
        }
        void BuildRiverCurrentField(PrototypeRegionView view,MeadowDistrictLandscape land){
            const int resolution=128;var pixels=new Color[resolution*resolution];
            for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++){
                var point=land.Origin+new Vector2((x+.5f)/resolution,(z+.5f)/resolution)*land.Side;
                var flow=RiverCurrentCoordinates(view.Data,view.MapSide,point);pixels[z*resolution+x]=new Color(flow.x,flow.y,flow.z,flow.w);
            }
            riverFlow=new Texture2D(resolution,resolution,TextureFormat.RGBAFloat,false,true){name="River lengthwise current coordinates",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            riverFlow.SetPixels(pixels);riverFlow.Apply(false,true);material.SetTexture("_RiverFlow",riverFlow);material.SetFloat("_CurrentSpeed",1.1f);
        }
        public static Rect GeneratedRiverBounds(RegionWaterBody body)
        {
            var bounds=body.Bounds;
            // v5 carving blends back to saved uplands across .065 regional units.
            bounds.min-=Vector2.one*(.065f+2f/512);bounds.max+=Vector2.one*(.065f+2f/512);
            return bounds;
        }
        public void InitializeOverview(PrototypeRegionView view,RegionWaterBody body)
        {
            if(!Applies(body,view))throw new System.ArgumentException("Overview requires a lake or pond.");
            var terrain=view.transform.Find("ContinuousRegionTerrain").GetComponent<Terrain>();
            var rng=new System.Random(view.Data.Seed);var offset=new Vector2(rng.Next(100,10000),rng.Next(100,10000));
            float level=body.hasSurfaceLevel?body.surfaceLevelNormalized*terrain.terrainData.size.y:view.SeaLevel;
            InitializeSurface(view,body,new Rect(-Vector2.one*view.MapSide*.5f,Vector2.one*view.MapSide),level,
                p=>terrain.SampleHeight(new Vector3(p.x,0,p.y)),p=>Mathf.PerlinNoise(p.x/18+offset.x+252,p.y/18+offset.y-252));
        }
        void InitializeSurface(PrototypeRegionView view,RegionWaterBody body,Rect region,float level,System.Func<Vector2,float> height,System.Func<Vector2,float> noise,bool generatedRiver=false)
        {
            var start=Time.realtimeSinceStartup;SurfaceLevel=level;BodyId=body.id;
            var bounds=generatedRiver?new Rect(region.min/view.MapSide+Vector2.one*.5f,region.size/view.MapSide):body.Bounds;
            var min=Vector2.Max((bounds.min-Vector2.one*.5f)*view.MapSide,region.min);
            var max=Vector2.Min((bounds.max-Vector2.one*.5f)*view.MapSide,region.max);
            var size = max - min;
            int resolution = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Max(size.x,size.y)*3.5f)),64,512);
            var pixels = new Color[resolution * resolution];
            for (int z = 0; z < resolution; z++) for (int x = 0; x < resolution; x++)
            {
                var p = min + Vector2.Scale(new Vector2((x + .5f) / resolution, (z + .5f) / resolution), size);
                var normalized=p/view.MapSide+Vector2.one*.5f;
                // v5 rivers share a level and their saved terrain is the authoritative
                // shoreline. A single network surface avoids doubled alpha at confluences.
                bool inside = (view.SelectedDistrict<0||view.InActiveDistrict(p))&&(generatedRiver?(RegionalRiverNetwork.HasContinuousChannels(view.Data)?RegionalRiverNetwork.SignedDistance(view.Data,normalized)<=0:true):body.Contains(normalized));
                float sampleLevel=generatedRiver&&RegionalRiverNetwork.HasGroundFollowingWater(view.Data)?RegionalRiverNetwork.GroundWaterLevel(view.Data,normalized.x,normalized.y):level;
                float waterDepth=Mathf.Max(0,sampleLevel-height(p));
                float optical=LakeShoreRenderer.EnabledForEvaluation?Mathf.Lerp(.65f,1.4f,noise(p)):1;
                optical=Mathf.Lerp(optical,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,3,waterDepth)));
                pixels[z * resolution + x] = new Color(waterDepth, inside ? 1 : 0, optical, 1);
            }
            depth = new Texture2D(resolution, resolution, TextureFormat.RGBAHalf, false, true)
                { name = "Lake/pond generation depth", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            depth.SetPixels(pixels); depth.Apply(false, true);
            var source = Resources.Load<Material>("Water/LakeWater");
            if (!source) throw new System.InvalidOperationException("Missing lake prototype material.");
            material = new Material(source) { name = "Calm lake/pond water (runtime)" };
            material.SetTexture("_DepthMask", depth);
            material.SetVector("_DepthRegion", new Vector4(min.x, min.y, 1 / size.x, 1 / size.y));
            mesh = new Mesh { name = "Bounded lake/pond" };
            mesh.vertices = new[] { new Vector3(min.x, level, min.y), new Vector3(min.x, level, max.y), new Vector3(max.x, level, min.y), new Vector3(max.x, level, max.y) };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 }; mesh.RecalculateBounds();
            if(generatedRiver&&RegionalRiverNetwork.HasGroundFollowingWater(view.Data)){
                const int n=257;var vertices=new Vector3[n*n];var triangles=new int[(n-1)*(n-1)*6];int t=0;
                for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                    var p=min+Vector2.Scale(new Vector2(x/(float)(n-1),z/(float)(n-1)),size);var uv=p/view.MapSide+Vector2.one*.5f;
                    vertices[z*n+x]=new Vector3(p.x,RegionalRiverNetwork.GroundWaterLevel(view.Data,uv.x,uv.y),p.y);
                    if(x<n-1&&z<n-1){int i=z*n+x;triangles[t++]=i;triangles[t++]=i+n;triangles[t++]=i+1;triangles[t++]=i+1;triangles[t++]=i+n;triangles[t++]=i+n+1;}
                }
                mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();
            }
            gameObject.layer = 4;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var tag = gameObject.AddComponent<RegionWaterBodyTag>(); tag.bodyId = body.id; tag.kind = body.kind;
            // A shallow district-wide reflection box stretches shoreline trees into
            // long streaks. Generated rivers use stable sky/scattering instead.
            if(generatedRiver){material.SetFloat("_RiverSurface",1);CaptureReady=true;return;}
            var capture = new GameObject("Lake one-time environment capture"); capture.transform.SetParent(transform, false);
            capture.transform.position = new Vector3((min.x + max.x) / 2, level + .5f, (min.y + max.y) / 2);
            material.SetVector("_CapturePosition", capture.transform.position);
            material.SetVector("_CaptureBoxMin", new Vector4(min.x, level - 2, min.y, 0));
            material.SetVector("_CaptureBoxMax", new Vector4(max.x, level + Mathf.Clamp(Mathf.Max(size.x,size.y)*.15f,5,22), max.y, 0));
            probe = capture.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce; probe.resolution = Mathf.Max(size.x,size.y)<50?256:512;
            probe.hdr = true; probe.cullingMask = view.GetComponentInChildren<Camera>().cullingMask & ~((1 << 4) | (1 << 2));
            probe.nearClipPlane = .3f; probe.farClipPlane = 250; probe.shadowDistance = 0;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox; probe.backgroundColor = new Color(.16f, .25f, .28f);
            probe.size = new Vector3(size.x, 40, size.y); probe.boxProjection = false;
            GenerationMilliseconds = (Time.realtimeSinceStartup - start) * 1000;
            material.SetFloat("_ReflectionMip",.25f);material.SetFloat("_ReflectionStrength",.20f);material.SetFloat("_NormalDetail",.008f);
            StartCoroutine(CaptureOnce());
        }
        public void RefreshEnvironment()
        {
            if(!probe||!isActiveAndEnabled)return;
            StopAllCoroutines();CaptureReady=false;StartCoroutine(CaptureOnce());
        }
        public void SetSharperReflections(bool sharp)
        {
            StopAllCoroutines();CaptureReady=false;
            material.SetFloat("_EnvironmentReady",0);
            material.SetFloat("_ReflectionMip",sharp?.25f:1.5f);
            material.SetFloat("_ReflectionStrength",sharp?.20f:.10f);
            material.SetFloat("_NormalDetail",sharp?.008f:.022f);
            var previous=probe.realtimeTexture;probe.realtimeTexture=null;
            if(previous){previous.Release();Destroy(previous);}
            probe.resolution=sharp?512:128;StartCoroutine(CaptureOnce());
        }
        IEnumerator CaptureOnce()
        {
            // Wait for generation/presentation to finish. No camera-dependent projection or per-frame capture.
            yield return null; yield return null;
            int id = probe.RenderProbe();CaptureCount++;
            while (probe && !probe.IsFinishedRendering(id)) yield return null;
            if (!probe || !probe.texture) yield break;
            probe.texture.name="Lake capture — "+BodyId;
            material.SetTexture("_Environment", probe.texture);
            material.SetVector("_EnvironmentHDR", probe.textureHDRDecodeValues);
            material.SetFloat("_EnvironmentReady", 1); CaptureReady = true;
        }
        void OnEnable(){if(probe&&!CaptureReady)StartCoroutine(CaptureOnce());}
        void OnDestroy()
        {
            if (probe) { var texture = probe.realtimeTexture; probe.realtimeTexture = null; if (texture) { texture.Release(); Destroy(texture); } }
            if (mesh) Destroy(mesh); if (material) Destroy(material); if (depth) Destroy(depth);if(riverFlow)Destroy(riverFlow);
        }
    }
}
