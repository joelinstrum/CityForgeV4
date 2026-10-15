using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.Environment {
// Keeps the terrain presentation's sky fill identical in the editor and saved-scene playback.
[ExecuteAlways]
public sealed class TerrainPresentationLighting : MonoBehaviour {
public Color ambientFill=new Color(.28f,.32f,.29f);
public Color skyFill=new Color(.08f,.10f,.12f);
void OnEnable(){Apply();}
void OnValidate(){Apply();}
public void Apply(){RenderSettings.ambientMode=AmbientMode.Custom;var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(ambientFill);probe.AddDirectionalLight(Vector3.up,skyFill,1);RenderSettings.ambientProbe=probe;}
}
}
