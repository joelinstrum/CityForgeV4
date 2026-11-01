using UnityEngine;
namespace CityForge.World {
/// <summary>Opt-in stream study only; ordinary recipes and existing saved regions are untouched.</summary>
public static class StreamRockTestRegion {
 public const string OriginalId="stream-rock-study-2026-10-31",Pass2Id="stream-rock-study-pass2-2026-10-31",Pass3Id="stream-rock-study-pass3-2026-10-31",Pass4Id="stream-rock-study-pass4-2026-10-31",Pass5Id="stream-rock-study-pass5-2026-10-31",Pass6Id="stream-rock-study-pass6-2026-10-31",Pass7Id="stream-rock-study-pass7-2026-10-31",Pass8Id="stream-rock-study-pass8-2026-10-31",Pass9Id="stream-rock-study-pass9-2026-10-31",Pass10Id="stream-rock-study-pass10-2026-10-31",Pass11Id="stream-rock-study-pass11-2026-10-31",Pass12Id="stream-rock-study-pass12-2026-10-31",Pass13Id="stream-rock-study-pass13-2026-10-31",Pass14Id="stream-rock-study-pass14-2026-10-31",Pass15Id="stream-rock-study-pass15-2026-10-31",Pass16Id="stream-rock-study-pass16-2026-10-31",Id="stream-rock-study-pass17-2026-10-31";
 public const float Side=1024, WaterLevel=12;
 // Compensate for the 2m field/monotone interpolation; measured water and slope targets are 1.25/.75.
 const float SampledWidthScale=1.34f,SampledBankScale=.55f;
 public static bool Applies(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id||view.SavedRegionId==Pass9Id||view.SavedRegionId==Pass8Id||view.SavedRegionId==Pass7Id||view.SavedRegionId==Pass6Id||view.SavedRegionId==Pass5Id||view.SavedRegionId==Pass4Id||view.SavedRegionId==Pass3Id||view.SavedRegionId==Pass2Id||view.SavedRegionId==OriginalId;
 public static bool Revised(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id||view.SavedRegionId==Pass9Id||view.SavedRegionId==Pass8Id||view.SavedRegionId==Pass7Id||view.SavedRegionId==Pass6Id||view.SavedRegionId==Pass5Id||view.SavedRegionId==Pass4Id||view.SavedRegionId==Pass3Id||view.SavedRegionId==Pass2Id;
 public static bool Refined(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id||view.SavedRegionId==Pass9Id||view.SavedRegionId==Pass8Id||view.SavedRegionId==Pass7Id||view.SavedRegionId==Pass6Id||view.SavedRegionId==Pass5Id||view.SavedRegionId==Pass4Id||view.SavedRegionId==Pass3Id;
 public static bool IndividualMix(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id||view.SavedRegionId==Pass9Id||view.SavedRegionId==Pass8Id||view.SavedRegionId==Pass7Id||view.SavedRegionId==Pass6Id||view.SavedRegionId==Pass5Id||view.SavedRegionId==Pass4Id;
 public static bool SoftBanks(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id||view.SavedRegionId==Pass9Id||view.SavedRegionId==Pass8Id||view.SavedRegionId==Pass7Id||view.SavedRegionId==Pass6Id||view.SavedRegionId==Pass5Id;
 public static bool ReadableFish(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id;
 public static bool StockedFish(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id;
 public static bool FinalBankLift(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id;
 public static bool BankTouchup(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id;
 public static bool LowerBanks(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id;
 public static bool DeeperBed(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id;
 public static bool VariedSlopes(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id;
 public static bool NarrowBanks(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id||view.SavedRegionId==Pass9Id;
 public static bool VariedBanks(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id||view.SavedRegionId==Pass9Id||view.SavedRegionId==Pass8Id;
 public static bool NaturalSoil(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id||view.SavedRegionId==Pass9Id||view.SavedRegionId==Pass8Id||view.SavedRegionId==Pass7Id;
 public static bool DirtBanks(PrototypeRegionView view)=>view.SavedRegionId==Id||view.SavedRegionId==Pass16Id||view.SavedRegionId==Pass15Id||view.SavedRegionId==Pass14Id||view.SavedRegionId==Pass13Id||view.SavedRegionId==Pass12Id||view.SavedRegionId==Pass11Id||view.SavedRegionId==Pass10Id||view.SavedRegionId==Pass9Id||view.SavedRegionId==Pass8Id||view.SavedRegionId==Pass7Id||view.SavedRegionId==Pass6Id;
 public static float Center(float z)=>128+38*Mathf.Sin((z-120)/100)+12*Mathf.Sin(z/43);
 static float OriginalHalfWidth(float z)=>2.9f+.45f*Mathf.Sin(z/37);
 public static float OriginalWetHalfWidth(float z){
  float depth=.42f+.12f*Mathf.Sin(z/53),lo=0,hi=1;
  for(int i=0;i<20;i++){float t=(lo+hi)*.5f;if(Mathf.SmoothStep(0,1,t)<depth/(depth+1.5f))lo=t;else hi=t;}
  float half=OriginalHalfWidth(z);return Mathf.Lerp(half*.45f,half+3,(lo+hi)*.5f);
 }
 public static float HalfWidth(float z,bool revised=true)=>revised?OriginalWetHalfWidth(z)*SampledWidthScale:OriginalHalfWidth(z);
 public static void ConfigureWater(Material material,bool refined=false){
  // Neutral, lower absorption keeps the shallow stone/soil bed visible. Owned material only.
  material.SetVector("_Crest_Absorption",refined?new Vector4(.72f,.50f,.38f,0):new Vector4(.35f,.28f,.24f,0));
  material.SetColor("_Crest_Scattering",refined?new Color(.045f,.105f,.11f,1):new Color(.12f,.16f,.135f,1));
  if(refined)material.SetFloat("_Crest_PlanarReflectionsIntensity",.35f);
 }
 public static SavedRegionSnapshot Create(bool revised=true){
  var recipe=new RegionCreationSettings{seed=831071,districtGrid=2,streams=1,vegetation=RegionVegetation.OpenMeadow,moisture=RegionMoisture.Wet};
  var heights=new float[513,513];var distances=new float[513,513];
  for(int z=0;z<513;z++){
   float wz=z*2-512,half=OriginalHalfWidth(wz),oldShore=OriginalWetHalfWidth(wz);
   for(int x=0;x<513;x++){
    float wx=x*2-512,d=Mathf.Abs(wx-Center(wz)),profileDistance=d;
    // Expand the actual wet cross-section 25%, compress the exposed slope 25%.
    if(revised)profileDistance=d<=oldShore*SampledWidthScale?d/SampledWidthScale:oldShore+(d-oldShore*SampledWidthScale)/SampledBankScale;
    float depth=.42f+.12f*Mathf.Sin(wz/53),bank=Mathf.SmoothStep(0,1,Mathf.InverseLerp(half*.45f,half+3,profileDistance));
    float ground=WaterLevel+Mathf.Lerp(-depth,1.5f,bank)+Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,65,d))*1.8f*Mathf.PerlinNoise(wx/90+50,wz/90+50);
    heights[z,x]=ground/PrototypeRegionData.Height;distances[z,x]=(d-(revised?oldShore*SampledWidthScale:half))/Side*PrototypeRegionData.Side;
   }
  }
  var points=new Vector2[513];for(int i=0;i<points.Length;i++){float z=i*2-512;points[i]=new Vector2(Center(z)/Side+.5f,z/Side+.5f);}
  var body=new RegionWaterBody{id="shallow-stream-study",kind=WaterBodyKind.River,centerline=points,halfWidthNormalized=3.35f*(revised?1.25f:1)/Side,hasSurfaceLevel=true,surfaceLevelNormalized=WaterLevel/PrototypeRegionData.Height,flowSpeed=.55f,appearanceProfile="calm-river"};
  var data=new PrototypeRegionData(recipe.seed,heights,distances){WaterBodies=new[]{body}};data.Biomes=new RegionBiomeMap(recipe,data);
  return new SavedRegionSnapshot{data=data,info=new SavedRegionInfo{id=revised?Id:OriginalId,name=revised?"Stream Rock Study — Pass 17":"Stream Rock Study",seed=recipe.seed,size=(int)RegionSize.Small,district=3,creation=recipe,meadow=true,waterBodies=data.WaterBodies,districts=data.Biomes.DescribeDistricts(),terrainHeightRange=PrototypeRegionData.Height,savedUtc=System.DateTime.UtcNow.ToString("O"),camera=new RegionCameraState{focus=new Vector3(Center(170),WaterLevel,170),pitch=CameraSystem.CityForgeCameraController.IsometricPitch,yaw=45,distance=500,size=24,zoom=1,hasViewTilt=true,viewTilt=1}}};
 }
}
}
