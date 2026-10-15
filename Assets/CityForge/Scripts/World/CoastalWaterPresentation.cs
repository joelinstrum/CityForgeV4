using UnityEngine;
namespace CityForge.World {
// The accepted perspective ocean profile is restored exactly when I selects the lower view.
public sealed class CoastalWaterPresentation:MonoBehaviour {
 Camera viewer;Material material;bool applied,lastOrtho;
 readonly string[] properties={"_Crest_ApplyDisplacementNormals","_Crest_NormalMapStrength","_Crest_Smoothness","_Crest_SmoothnessFar","_Crest_PlanarReflectionsIntensity"};
 float[] perspective;float ambient,direct;Color scattering;
 public void Initialize(Camera camera,Material surface){viewer=camera;material=surface;ambient=material.GetFloat("_Crest_AmbientTerm");direct=material.GetFloat("_Crest_DirectTerm");scattering=material.GetColor("_Crest_Scattering");perspective=new float[properties.Length];for(int i=0;i<properties.Length;i++)if(material.HasProperty(properties[i]))perspective[i]=material.GetFloat(properties[i]);Apply();}
 void LateUpdate(){if(viewer&&(!applied||lastOrtho!=viewer.orthographic))Apply();}
 void Apply(){if(!viewer||!material)return;lastOrtho=viewer.orthographic;var iso=new[]{1f,.012f,.62f,.62f,.28f};for(int i=0;i<properties.Length;i++)if(material.HasProperty(properties[i]))material.SetFloat(properties[i],lastOrtho?iso[i]:perspective[i]);material.SetFloat("_Crest_AmbientTerm",lastOrtho?ambient*2.6f:ambient);material.SetFloat("_Crest_DirectTerm",lastOrtho?direct*1.4f:direct);material.SetColor("_Crest_Scattering",lastOrtho?new Color(scattering.r*1.3f,scattering.g*1.8f,scattering.b*2.1f,scattering.a):scattering);applied=true;}
}
}
