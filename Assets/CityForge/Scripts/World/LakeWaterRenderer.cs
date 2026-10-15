using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    /// <summary>Bounded still water for typed lakes and ponds, with generation-time depth and environment capture.</summary>
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
        Texture2D depth;
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
        public void InitializeOverview(PrototypeRegionView view,RegionWaterBody body)
        {
            if(!Applies(body,view))throw new System.ArgumentException("Overview requires a lake or pond.");
            var terrain=view.transform.Find("ContinuousRegionTerrain").GetComponent<Terrain>();
            var rng=new System.Random(view.Data.Seed);var offset=new Vector2(rng.Next(100,10000),rng.Next(100,10000));
            float level=body.hasSurfaceLevel?body.surfaceLevelNormalized*terrain.terrainData.size.y:view.SeaLevel;
            InitializeSurface(view,body,new Rect(-Vector2.one*view.MapSide*.5f,Vector2.one*view.MapSide),level,
                p=>terrain.SampleHeight(new Vector3(p.x,0,p.y)),p=>Mathf.PerlinNoise(p.x/18+offset.x+252,p.y/18+offset.y-252));
        }
        void InitializeSurface(PrototypeRegionView view,RegionWaterBody body,Rect region,float level,System.Func<Vector2,float> height,System.Func<Vector2,float> noise)
        {
            var start=Time.realtimeSinceStartup;SurfaceLevel=level;BodyId=body.id;
            var min=Vector2.Max((body.Bounds.min-Vector2.one*.5f)*view.MapSide,region.min);
            var max=Vector2.Min((body.Bounds.max-Vector2.one*.5f)*view.MapSide,region.max);
            var size = max - min;
            int resolution = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Max(size.x,size.y)*3.5f)),64,512);
            var pixels = new Color[resolution * resolution];
            for (int z = 0; z < resolution; z++) for (int x = 0; x < resolution; x++)
            {
                var p = min + Vector2.Scale(new Vector2((x + .5f) / resolution, (z + .5f) / resolution), size);
                bool inside = body.Contains(p / view.MapSide + Vector2.one * .5f);
                float waterDepth=Mathf.Max(0,level-height(p));
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
            gameObject.layer = 4;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var tag = gameObject.AddComponent<RegionWaterBodyTag>(); tag.bodyId = body.id; tag.kind = body.kind;
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
            if (mesh) Destroy(mesh); if (material) Destroy(material); if (depth) Destroy(depth);
        }
    }
}
