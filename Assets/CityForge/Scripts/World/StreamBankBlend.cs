using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World {
/// <summary>Study-only dry shoulder rounding, anchored away from the unchanged wet channel.</summary>
public sealed class StreamBankBlend {
 readonly float extraLift;readonly bool refined,varied,narrow,variedSlopes,lowerBanks,raisedBanks;readonly PrototypeRegionData data;readonly Dictionary<float,Vector2> edges=new();
 public StreamBankBlend(PrototypeRegionData source,bool refine=false,bool variety=false,bool narrowBanks=false,bool slopeVariation=false,bool lowBanks=false,bool raised=false,float additionalLift=1){data=source;refined=refine;varied=variety;narrow=narrowBanks;variedSlopes=slopeVariation;lowerBanks=lowBanks;raisedBanks=raised;extraLift=additionalLift;}
 float Sample(Vector2 p)=>data.SmoothHeight(p.x/1024+.5f,p.y/1024+.5f)*PrototypeRegionData.Height;
 Vector2 Edges(float z){
  if(edges.TryGetValue(z,out var result))return result;
  float center=StreamRockTestRegion.Center(z);
  float Edge(float sign){float lo=0,hi=15;for(int i=0;i<18;i++){float mid=(lo+hi)*.5f;if(Sample(new Vector2(center+sign*mid,z))<12)lo=mid;else hi=mid;}return center+sign*(lo+hi)*.5f;}
  result=new Vector2(Edge(-1),Edge(1));edges[z]=result;return result;
 }
 // Independent, broad reaches on each side avoid a symmetrical canal profile.
 public static float LowMargin(Vector2 p){float side=p.x<StreamRockTestRegion.Center(p.y)?0:97;return Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.66f,Mathf.PerlinNoise(p.y/19+431,side+83)));}
 // Smooth, asymmetric reaches retain occasional tight cut banks between gentler pullbacks.
 public static float TightBank(Vector2 p){float side=p.x<StreamRockTestRegion.Center(p.y)?0:61;return Mathf.SmoothStep(0,1,Mathf.InverseLerp(.37f,.62f,Mathf.PerlinNoise(p.y/17+721,side+31)));}
 public float Height(Vector2 p,float normalized){
  float original=UpperHeight(p,normalized),h=original*PrototypeRegionData.Height;
  if(!lowerBanks||h<=12)return original;
  var edge=Edges(p.y);float distance=p.x<edge.x?edge.x-p.x:p.x>edge.y?p.x-edge.y:0;
  if(distance>=10)return original;
  // Scale bank relief about the fixed water plane. Feather the surrounding meadow to avoid a new outer wall.
  float influence=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,10,distance));
  return (12+(h-12)*(1-(raisedBanks?1-.22f*extraLift:.8f)*influence))/PrototypeRegionData.Height;
 }
 float UpperHeight(Vector2 p,float normalized){
  if(!narrow||normalized*PrototypeRegionData.Height<=12)return WideHeight(p,normalized);
  var edge=Edges(p.y);float distance=p.x<edge.x?edge.x-p.x:p.x>edge.y?p.x-edge.y:0;
  if(distance<=.75f||distance>=8)return WideHeight(p,normalized);
  // Compress only the dry shoulder about the existing water edge; the wet bed and rock seats stay fixed.
  float compression=variedSlopes?Mathf.Lerp(1.25f,3,TightBank(p)):3;
  float sign=p.x<edge.x?-1:1;var q=p+new Vector2(sign*(distance-.75f)*(compression-1),0);
  float compressed=WideHeight(q,Sample(q)/PrototypeRegionData.Height);
  float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,12.10f,normalized*PrototypeRegionData.Height))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,8,distance)));
  return Mathf.Lerp(WideHeight(p,normalized),compressed,fade);
 }
 float WideHeight(Vector2 p,float normalized){
  float original=RefinedHeight(p,normalized),h=original*PrototypeRegionData.Height;
  if(!varied||h<=12.05f||h>=15||Mathf.Abs(p.x-StreamRockTestRegion.Center(p.y))>14)return original;
  var edge=Edges(p.y);float distance=p.x<edge.x?edge.x-p.x:p.x>edge.y?p.x-edge.y:0;
  float low=LowMargin(p),target=12.08f+.12f*distance+.03f*distance*distance;
  float amount=low*Mathf.SmoothStep(0,1,Mathf.InverseLerp(12.05f,12.35f,h))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,8,distance)));
  return Mathf.Lerp(h,Mathf.Min(h,target),amount)/PrototypeRegionData.Height;
 }
 float RefinedHeight(Vector2 p,float normalized){
  float original=Shoulder(p,normalized),h=original*PrototypeRegionData.Height;
  if(!refined||h<=12.25f||h>=15||Mathf.Abs(p.x-StreamRockTestRegion.Center(p.y))>10)return original;
  float along=0;for(int j=-1;j<=1;j++)for(int i=-2;i<=2;i++){var q=p+new Vector2(j*1.25f,i*1.25f);along+=Shoulder(q,Sample(q)/PrototypeRegionData.Height)*(i==0?4:i==-1||i==1?2:1)*(j==0?2:1);}
  along/=40;
  float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(12.25f,12.9f,h));
  return Mathf.Lerp(original,Mathf.Max(12.25f/PrototypeRegionData.Height,along),fade*.9f);
 }
 float Shoulder(Vector2 p,float normalized){
  float height=normalized*PrototypeRegionData.Height;if(height<=12.25f||height>=15||Mathf.Abs(p.x-StreamRockTestRegion.Center(p.y))>12)return normalized;
  var edge=Edges(p.y);float distance=p.x<edge.x?edge.x-p.x:p.x>edge.y?p.x-edge.y:0;
  // First metre beyond the wet boundary remains exact, including bordering rock seats.
  if(distance<=1)return normalized;
  float average=0;for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)average+=Sample(p+new Vector2(x,z)*1.5f)*(x==0?2:1)*(z==0?2:1);
  average/=16;
  float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,2,distance))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,8,distance)));
  return Mathf.Lerp(height,Mathf.Min(height,average),fade*.9f)/PrototypeRegionData.Height;
 }
 // Alpha carries terrain smoothness; matte banks need low alpha even though coverage is opaque.
 // Low-contrast, seamless earth grain: no photographed blocks or directional tile landmarks.
 public static Texture2D EarthTexture(bool matte=false){
  const int n=512;var texture=new Texture2D(n,n,TextureFormat.RGBA32,true){name="Fine neutral stream earth",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
  var pixels=new Color32[n*n];
  float Periodic(float u,float v,float scale){float a=Mathf.PerlinNoise(u*scale+17,v*scale+31),b=Mathf.PerlinNoise((u-1)*scale+17,v*scale+31),c=Mathf.PerlinNoise(u*scale+17,(v-1)*scale+31),d=Mathf.PerlinNoise((u-1)*scale+17,(v-1)*scale+31);return Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v);}
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   float u=x/(float)n,v=y/(float)n;uint h=(uint)(x+y*n+831071);h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;h^=h>>16;
   float grain=(h&65535)/65535f;
   float value=.78f+(Periodic(u,v,19)-.5f)*.12f+(Periodic(u,v,81)-.5f)*.10f+(grain-.5f)*.10f;
   byte channel=(byte)Mathf.RoundToInt(value*255);pixels[y*n+x]=new Color32(channel,channel,channel,matte?(byte)8:(byte)255);
  }
  texture.SetPixels32(pixels);texture.Apply(true,true);return texture;
 }
 public static Vector2 VariedCover(MeadowDistrictLandscape land,Vector2 p,float distance)=>VariedCover(land,p,distance,p);
 public static Vector2 VariedCover(MeadowDistrictLandscape land,Vector2 p,float distance,Vector2 coordinates){
  float low=LowMargin(coordinates),side=coordinates.x<StreamRockTestRegion.Center(coordinates.y)?0:79;
  float exposure=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.67f,Mathf.PerlinNoise(coordinates.y/11+619,side+47)));
  float flecks=land.Noise(p+new Vector2(13,-37),.85f,163);
  float reach=Mathf.Lerp(.45f,2.5f,exposure)*(1-low*.35f);
  float fringe=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.2f,reach,distance+(flecks-.5f)*1.15f));
  float wet=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.3f,.1f,distance));
  float dirt=fringe*wet*Mathf.Lerp(.035f,.78f,exposure)*(1-low*.48f)*Mathf.Lerp(.45f,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.70f,flecks)));
  // The native pale soil underlay must also recede, otherwise it leaves a continuous rim.
  float soil=fringe*Mathf.Lerp(.025f,.24f,exposure)*Mathf.Lerp(.5f,1,flecks);
  return new Vector2(soil,dirt);
 }
 public static float Soil(MeadowDistrictLandscape land,Vector2 point,float distance){
  float patch=land.Noise(point,3.5f,731),broad=land.Noise(point,10,817);
  float reach=Mathf.Lerp(1.4f,3.1f,broad),feather=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.7f,reach,distance+(patch-.5f)*.65f));
  return Mathf.Lerp(.58f,.72f,patch)*feather;
 }
 public static float Dirt(MeadowDistrictLandscape land,Vector2 point,float distance){
  float patch=land.Noise(point,2.2f,947),broad=land.Noise(point,7,1031);
  float reach=Mathf.Lerp(1.5f,2.8f,broad);
  float fringe=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,reach,distance+(patch-.5f)*.55f));
  float wetEdge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.35f,.1f,distance));
  return fringe*wetEdge*Mathf.Lerp(.70f,.98f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.72f,patch)));
 }

}
}
