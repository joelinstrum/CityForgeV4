using UnityEngine;using WaveHarmonic.Crest;
namespace CityForge.World {
public sealed partial class MeadowDistrictLandscape {
 void BuildCoastalWater(){
 var holder=new GameObject("Crest coastal ocean");holder.transform.SetParent(transform,false);holder.SetActive(false);
 var root=Instantiate(Baseline.waterTemplate,holder.transform);root.SetActive(false);root.transform.position=Vector3.up*View.SeaLevel;
 Water=root.GetComponent<WaterRenderer>();Water.Viewer=View.GetComponentInChildren<Camera>();Water.Viewpoint=Water.Viewer.transform;
 Water.AnimatedWavesLod.Enabled=true;Water.DynamicWavesLod.Enabled=false;Water.FoamLod.Enabled=true;Water.FlowLod.Enabled=false;
 var surf=Own(ScriptableObject.CreateInstance<FoamLodSettings>());surf.name="Coastal breaking surf";surf.ShorelineFoamMaximumDepth=1.8f;surf.ShorelineFoamStrength=.12f;surf.ShorelineFoamPriming=0;surf.FoamFadeRate=1.5f;surf.WaveFoamCoverage=.42f;surf.WaveFoamStrength=.65f;surf.FilterWaves=2;Water.FoamLod.Settings=surf;
 Water.DepthLod.Enabled=true;Water.DepthLod.Resolution=256;Water.ShadowLod.Enabled=false;Water.LevelLod.Enabled=false;Water.AlbedoLod.Enabled=false;Water.AbsorptionLod.Enabled=false;
 Water.LodResolution=256;Water.LodLevels=7;Water.GeometryDownSampleFactor=2;
 Water.ClipLod.Enabled=true;Water.ClipLod.Resolution=512;Water.ClipLod.DefaultClippingState=DefaultClippingState.EverythingClipped;Water.DefaultExcludes=WaterBodyAffects.Surface;
 var bounds=new GameObject("District-bounded ocean footprint").AddComponent<WaterBody>();bounds.transform.SetParent(holder.transform,false);bounds.transform.position=new Vector3(Origin.x+Side/2,View.SeaLevel,Origin.y+Side/2);bounds.transform.localScale=new Vector3(Side,40,Side);bounds.Affects=WaterBodyAffects.Surface;bounds.Exclusion=WaterBodyExclusion.Include;bounds.Precise=true;
 // Clip dry inland water as well as the district perimeter, so lower water cannot peek
 // out below raised terrain edges in the isometric projection.
 const int maskSize=512;var pixels=new Color[maskSize*maskSize];
 for(int z=0;z<maskSize;z++)for(int x=0;x<maskSize;x++){
 var worldX=Origin.x+(x+.5f)*Side/maskSize;var worldZ=Origin.y+(z+.5f)*Side/maskSize;
 pixels[z*maskSize+x]=worldX<CoastlineTestRegion.ShoreX(worldZ)+12?Color.black:Color.white;
 }
 var mask=Own(new Texture2D(maskSize,maskSize,TextureFormat.R8,false,true){name="Coastal ocean inclusion mask",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear});mask.SetPixels(pixels);mask.Apply(false,true);
 var maskObject=new GameObject("Coastal shoreline clipping");maskObject.transform.SetParent(holder.transform,false);maskObject.transform.position=new Vector3(Origin.x+Side/2,View.SeaLevel,Origin.y+Side/2);maskObject.transform.localScale=new Vector3(Side,1,Side);
 var clip=WaveHarmonic.Crest._Extensions.AddComponent<ClipLodInput>(maskObject,LodInputMode.Texture);clip.GetData<ClipTextureLodInputData>().Texture=mask;
 Water.Reflections.Enabled=true;Water.Reflections.Resolution=256;Water.Reflections.DisableShadows=true;Water.Reflections.RenderOnlySingleCamera=true;Water.Reflections.Layers=~(1<<4);Water.Underwater.Enabled=false;
 Water.Surface.Material=CreateTypedWaterMaterial(WaterBodyKind.Ocean);
 var mat=Water.Surface.Material;mat.SetFloat("_Crest_FoamEnabled",1);mat.SetTexture("_Crest_FoamTexture",Resources.Load<Texture2D>("Coasts/CoastalFoam"));mat.SetFloat("_Crest_FoamFeather",.65f);mat.SetFloat("_Crest_FoamScale",5);mat.SetFloat("_Crest_FoamIntensityAlbedo",1);mat.SetFloat("_Crest_PlanarReflectionsEnabled",1);mat.SetFloat("_Crest_PlanarReflectionsApplySmoothness",1);mat.SetFloat("_Crest_Smoothness",.8f);mat.SetFloat("_Crest_SmoothnessFar",.8f);mat.SetFloat("_Crest_PlanarReflectionsIntensity",.65f);
 if(Baseline.fftTemplate){var go=Instantiate(Baseline.fftTemplate,root.transform);go.transform.position=Vector3.up*View.SeaLevel;var fft=go.GetComponent<ShapeFFT>();fft.Weight=.55f;fft.OverrideGlobalWindSpeed=true;fft.WindSpeed=18;fft.OverrideGlobalWindDirection=true;fft.WaveDirectionHeadingAngle=0;fft.OverrideGlobalWindTurbulence=true;fft.WindTurbulence=.015f;fft.WindAlignment=.95f;}
 var swellObject=new GameObject("Incoming coastal swell");swellObject.transform.SetParent(root.transform,false);var swell=WaveHarmonic.Crest._Extensions.AddComponent<ShapeGerstner>(swellObject,LodInputMode.Global);swell.Swell=true;swell.ReverseWaveWeight=0;swell.Weight=.3f;swell.OverrideGlobalWindSpeed=true;swell.WindSpeed=30;swell.OverrideGlobalWindDirection=true;swell.WaveDirectionHeadingAngle=0;
 var tag=root.AddComponent<RegionWaterBodyTag>();tag.bodyId="coastal-ocean";tag.kind=WaterBodyKind.Ocean;
 var probe=new GameObject("Coastal seabed depth").AddComponent<DepthProbe>();probe.transform.SetParent(holder.transform,false);probe.transform.position=new Vector3(Origin.x+Side/2,View.SeaLevel,Origin.y+Side/2);probe.transform.localScale=new Vector3(Side,1,Side);probe.Type=DepthProbeMode.RealTime;probe.RefreshMode=DepthProbeRefreshMode.ViaScripting;probe.Layers=1<<30;probe.Resolution=1024;probe.SavedTexture=null;probe.CaptureRange=new Vector2(-40,terrainData.size.y+10);probe.GenerateSignedDistanceField=true;
 root.AddComponent<CoastalWaterClock>().Initialize(Water);
 BuildIncomingSurf(holder.transform);
 root.AddComponent<CoastalWaterPresentation>().Initialize(Water.Viewer,mat);
 root.SetActive(true);holder.SetActive(true);StartCoroutine(RefreshDepth(probe));
 }
 void BuildIncomingSurf(Transform holder){
 const int segments=256;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[segments*6];
 for(int i=0;i<=segments;i++){
 float z=Origin.y+Side*i/segments;float shore=CoastlineTestRegion.ShoreX(z);
 for(int edge=0;edge<2;edge++){float d=edge==0?-34:8;int v=i*2+edge;vertices[v]=new Vector3(Mathf.Clamp(shore+d,Origin.x,Origin.x+Side),View.SeaLevel,z);uv[v]=new Vector2(d,z);}
 if(i<segments){int t=i*6,v=i*2;triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;triangles[t+3]=v+1;triangles[t+4]=v+2;triangles[t+5]=v+3;}
 }
 var mesh=Own(new Mesh{name="Coast-following incoming surf"});mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();
 var go=new GameObject("Animated shoreline surf",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(holder,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
 var material=Own(new Material(Resources.Load<Shader>("Coasts/SurfFoam")));material.SetFloat("_Crest_Strength",2.8f);var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.forceRenderingOff=true;
 go.AddComponent<CoastalSurfMotion>().Initialize(material);
 var input=WaveHarmonic.Crest._Extensions.AddComponent<FoamLodInput>(go,LodInputMode.Renderer);input.GetData<FoamRendererLodInputData>().Renderer=renderer;
 }
}}
