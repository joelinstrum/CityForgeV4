using UnityEngine;
namespace CityForge.World {
/// <summary>One optional private dirt palette for courtyard, soil and bank layers.</summary>
public static class SharedDirtSurface {
 public static bool Apply(TerrainLayer layer,bool damp=false){
  var texture=Resources.Load<Texture2D>("NativeCamps/Courtyard-Dirt");if(!texture)return false;
  layer.diffuseTexture=texture;layer.normalMapTexture=null;layer.maskMapTexture=null;
  layer.tileSize=new Vector2(2.5f,2.5f);layer.diffuseRemapMin=Vector4.zero;
  layer.diffuseRemapMax=damp?new Vector4(.65f,.65f,.65f,1):Vector4.one;
  layer.smoothness=0;layer.metallic=0;layer.specular=new Color(.02f,.02f,.02f);return true;
 }
}
}
