using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace CityForge.World {
// The accepted perspective ocean profile is restored exactly when I selects the lower view.
public sealed class CoastalWaterPresentation:MonoBehaviour {
 // Apply to a district-owned material before Initialize captures its perspective colors.
 public static void ApplyClimate(Material surface,RegionClimate climate){
 if(climate!=RegionClimate.Tropical)return;
 // Retain red absorption for turquoise water, with clearer blue/green transmission.
 surface.SetColor("_Crest_Absorption",new Color(.32f,.065f,.035f,0));
 surface.SetColor("_Crest_Scattering",new Color(.035f,.24f,.30f,1));
 }
 WaveHarmonic.Crest.ScatteringTextureLodInputData depthColors,harborColors;
 bool calmHarbor;
 Camera viewer;Material material;Light sunlight;bool applied,lastOrtho;float lastGlare=-1;
 readonly string[] properties={"_Crest_ApplyDisplacementNormals","_Crest_NormalMapStrength","_Crest_Smoothness","_Crest_SmoothnessFar","_Crest_PlanarReflectionsIntensity"};
 float[] perspective;float ambient,direct,normalScale;Color scattering;
 UniversalAdditionalCameraData cameraData;
 float previousJitterScale,coastalJitterScale;
 bool antialiasingChanged;
 void ApplyCoastalAntialiasing(){
 if(!viewer||antialiasingChanged)return;
 cameraData=viewer.GetUniversalAdditionalCameraData();
 if(cameraData.antialiasing!=AntialiasingMode.TemporalAntiAliasing)return;
 // Limit shoreline subpixel movement while retaining TAA's accepted color path.
 previousJitterScale=cameraData.taaSettings.jitterScale;
 coastalJitterScale=Mathf.Min(previousJitterScale,.125f);
 cameraData.taaSettings.jitterScale=coastalJitterScale;
 antialiasingChanged=true;
 }
 void OnEnable(){ApplyCoastalAntialiasing();}
 void OnDisable(){
 if(antialiasingChanged&&cameraData&&Mathf.Approximately(cameraData.taaSettings.jitterScale,coastalJitterScale))cameraData.taaSettings.jitterScale=previousJitterScale;
 antialiasingChanged=false;
 }
 public void Initialize(Camera camera,Material surface,WaveHarmonic.Crest.ScatteringTextureLodInputData depth=null,
 WaveHarmonic.Crest.ScatteringTextureLodInputData harbor=null,bool calm=false){depthColors=depth;harborColors=harbor;calmHarbor=calm;viewer=camera;material=surface;var land=GetComponentInParent<MeadowDistrictLandscape>();if(land)foreach(var light in land.GetComponentsInChildren<Light>())if(light.type==LightType.Directional&&(!sunlight||light.intensity>sunlight.intensity))sunlight=light;ambient=material.GetFloat("_Crest_AmbientTerm");direct=material.GetFloat("_Crest_DirectTerm");scattering=material.GetColor("_Crest_Scattering");normalScale=material.GetFloat("_Crest_NormalMapScale");perspective=new float[properties.Length];for(int i=0;i<properties.Length;i++)if(material.HasProperty(properties[i]))perspective[i]=material.GetFloat(properties[i]);Apply();}
 float Glare(){if(!viewer||!sunlight||!sunlight.enabled)return 0;float alignment=Vector3.Dot(Vector3.Reflect(sunlight.transform.forward,Vector3.up),-viewer.transform.forward);return Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,.98f,alignment));}
 void LateUpdate()
 {
     if (!viewer || !material) return;
     if (!applied || lastOrtho != viewer.orthographic || Mathf.Abs(lastGlare - Glare()) > .003f)
         Apply();
     else if (lastOrtho)
         ApplyRipples();
 }

 float lastRippleBlend = -1f;
 void ApplyRipples()
 {
     // Crest already blends its wave detail by distance. Widen the surface normal
     // pattern in broad isometric views so the ripples survive pixel filtering.
     float blend = lastOrtho ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(24f, 110f, viewer.orthographicSize)) : 0f;
     if (Mathf.Approximately(lastRippleBlend, blend)) return;
     lastRippleBlend = blend;
     material.SetFloat("_Crest_NormalMapScale", normalScale * Mathf.Lerp(1f, calmHarbor ? 1.45f : 2.1f, blend));
     material.SetFloat("_Crest_NormalMapStrength", lastOrtho ? (calmHarbor ? Mathf.Lerp(.045f, .085f, blend) : Mathf.Lerp(.025f, .085f, blend)) : perspective[1]);
 }
 void Apply(){if(!viewer||!material)return;ApplyCoastalAntialiasing();lastOrtho=viewer.orthographic;lastGlare=Glare();float gloss=Mathf.Lerp(.62f,.20f,lastGlare);var iso=new[]{1f,.025f,gloss,gloss,.28f};for(int i=0;i<properties.Length;i++)if(material.HasProperty(properties[i]))material.SetFloat(properties[i],lastOrtho?iso[i]:perspective[i]);material.SetFloat("_Crest_AmbientTerm",lastOrtho?ambient*2.6f:ambient);material.SetFloat("_Crest_DirectTerm",lastOrtho?direct*1.4f:direct);material.SetColor("_Crest_Scattering",lastOrtho?new Color(scattering.r*1.3f,scattering.g*1.8f,scattering.b*2.1f,scattering.a):scattering);if(depthColors!=null)depthColors.Multiplier=lastOrtho?new Vector4(1.3f,1.8f,2.1f,1):Vector4.one;if(harborColors!=null)harborColors.Multiplier=lastOrtho?new Vector4(1.3f,1.48f,1.42f,1):Vector4.one;lastRippleBlend=-1f;ApplyRipples();applied=true;}
}
}
