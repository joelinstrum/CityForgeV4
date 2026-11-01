using UnityEngine;
namespace CityForge.World {
/// <summary>Opt-in deeper pools between unchanged rock shelves, banks and shallow margins.</summary>
public sealed class StreamBedDepthStudy:MonoBehaviour {
 public int LoweredVertices {get;private set;}
 public float MaximumLowering {get;private set;}
 public float MaximumDryChange {get;private set;}
 public void Initialize(MeadowDistrictLandscape land){
  var terrain=land.DisplayTerrain.terrainData;int n=terrain.heightmapResolution;var heights=terrain.GetHeights(0,0,n,n);
  var clusters=land.GetComponent<StreamRockStudy>();var stones=land.GetComponent<IndividualStreamRockScatter>();
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){
   float h=heights[z,x]*terrain.size.y;if(h>=StreamRockTestRegion.WaterLevel-.10f)continue;
   var p=land.Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*land.Side;
   float across=Mathf.Abs(p.x-StreamRockTestRegion.Center(p.y))/StreamRockTestRegion.HalfWidth(p.y);
   float taper=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.88f,across));if(taper<=0)continue;
   float shelf=clusters.BedShelf(p);if(shelf<1)shelf=Mathf.Max(shelf,stones.BedShelf(p));
   float lower=.30f*taper*(1-shelf)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,.25f,StreamRockTestRegion.WaterLevel-h));
   if(lower<=.0001f)continue;heights[z,x]-=lower/terrain.size.y;LoweredVertices++;MaximumLowering=Mathf.Max(MaximumLowering,lower);
  }
  terrain.SetHeights(0,0,heights);
  // Read Unity's applied height quantization back to verify the entire dry bank remained exact.
  foreach(var probe in land.GetComponentsInChildren<WaveHarmonic.Crest.DepthProbe>())probe.Populate();
  var applied=terrain.GetHeights(0,0,n,n);
  for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(heights[z,x]*terrain.size.y>=StreamRockTestRegion.WaterLevel)MaximumDryChange=Mathf.Max(MaximumDryChange,Mathf.Abs(applied[z,x]-heights[z,x])*terrain.size.y);
 }
}
}
