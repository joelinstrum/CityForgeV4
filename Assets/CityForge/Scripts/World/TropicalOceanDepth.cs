using UnityEngine;
using WaveHarmonic.Crest;
namespace CityForge.World {
/// <summary>Shared continental shelf profile, in whole-region units, never district-local.</summary>
public static class TropicalOceanDepth {
 public const float Sea=30, MaximumDepth=24;
 public static bool DeepRecipe(RegionCreationSettings recipe)=>recipe!=null&&recipe.version>=12&&recipe.climate==RegionClimate.Tropical&&recipe.terrainShape!=RegionTerrainShape.Land&&recipe.terrainShape!=RegionTerrainShape.MountainBorder;
 public static float Blend(float offshore)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(offshore/.12f));
 public static float Depth(float offshore)=>Mathf.Lerp(0,MaximumDepth,Blend(offshore));
 public static Color Scatter(float offshore)=>Color.Lerp(new Color(.035f,.24f,.30f,1),new Color(.003f,.01f,.04f,1),Blend(offshore));
 public static Color MapColor(float offshore)=>Color.Lerp(new Color(.16f,.69f,.77f),new Color(.025f,.18f,.35f),Blend(offshore));
}
public sealed partial class MeadowDistrictLandscape {
 ScatteringTextureLodInputData BuildTropicalDepthInput(Transform holder){
  if(View.Climate!=RegionClimate.Tropical)return null;
  var layout=View.CreationSettings?.version>=6?new RegionTerrainLayout(View.CreationSettings):null;
  // Extend past the clipped footprint so LOD filtering cannot expose the uniform fallback color at map edges.
  float inputSide=Side*1.1f;var inputOrigin=Origin-Vector2.one*(inputSide-Side)*.5f;
  const int n=256;var colors=new Color[n*n];
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){
   var p=inputOrigin+new Vector2((x+.5f)/n,(z+.5f)/n)*inputSide;
   float offshore=layout!=null&&layout.HasOcean?-layout.LandDistance(p/View.MapSide+Vector2.one*.5f):-WaterDistance(p)/View.MapSide;
   colors[z*n+x]=TropicalOceanDepth.Scatter(Mathf.Max(0,offshore));
  }
  var texture=Own(new Texture2D(n,n,TextureFormat.RGBAHalf,false,true){name="Shared tropical shelf colors",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear});texture.SetPixels(colors);texture.Apply(false,true);
  Water.ScatteringLod.Enabled=true;Water.ScatteringLod.Resolution=256;
  var go=new GameObject("Tropical shelf scattering");go.transform.SetParent(holder,false);go.transform.position=new Vector3(Origin.x+Side/2,View.SeaLevel,Origin.y+Side/2);go.transform.localScale=new Vector3(inputSide,1,inputSide);
  var input=WaveHarmonic.Crest._Extensions.AddComponent<ScatteringLodInput>(go,LodInputMode.Texture);input.FeatherWidth=0;
  var data=input.GetData<ScatteringTextureLodInputData>();data.Texture=texture;return data;
 }
}
}
