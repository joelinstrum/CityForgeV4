using System;using System.Collections.Generic;using UnityEngine;
namespace CityForge.World {
/// <summary>Grade-limited Dijkstra route through the lowlands; checks intermediate samples, not just grid corners.</summary>
public static class MountainRoad {
public static Vector2[] Build(MeadowDistrictLandscape land){
const int n=65;float step=land.Side/(n-1);var cost=new float[n*n];var previous=new int[n*n];var done=new bool[n*n];Array.Fill(cost,float.PositiveInfinity);Array.Fill(previous,-1);
Vector2 Point(int i)=>land.Origin+new Vector2(i%n,i/n)*step;
int start= Mathf.RoundToInt(land.Side*.36f/step)+n*2;cost[start]=0;int target=-1;
for(int iteration=0;iteration<n*n;iteration++){
int current=-1;float best=float.PositiveInfinity;for(int i=0;i<cost.Length;i++)if(!done[i]&&cost[i]<best){best=cost[i];current=i;}if(current<0)break;done[current]=true;
// End at the valley trailhead below the mountain belt; the road does not need to climb a summit.
if(current/n>=26&&current%n>=17&&current%n<=30){target=current;break;}
var a=Point(current);for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){
if(dx==0&&dz==0)continue;int x=current%n+dx,z=current/n+dz;if(x<2||x>62||z<2||z>62)continue;int next=z*n+x;if(done[next])continue;var b=Point(next);float length=Vector2.Distance(a,b),maxGrade=0,maxSlope=0;float h=land.Height(a);
for(int j=1;j<=8;j++){var p=Vector2.Lerp(a,b,j/8f);float nh=land.Height(p);maxGrade=Mathf.Max(maxGrade,Mathf.Abs(nh-h)/(length/8));maxSlope=Mathf.Max(maxSlope,land.MountainSlope(p));h=nh;}
if(maxGrade>.16f||maxSlope>26)continue;
float height=(land.Height(a)+land.Height(b))*.5f;float value=best+length*(1+maxGrade*5+maxSlope*.018f+height*.004f);
if(value<cost[next]){cost[next]=value;previous[next]=current;}
}
}
if(target<0)throw new Exception("No safe mountain road route found");var path=new List<Vector2>();for(int at=target;at>=0;at=previous[at])path.Add(Point(at));path.Reverse();
var dense=new List<Vector2>{path[0]};for(int i=1;i<path.Count;i++){int count=Mathf.CeilToInt(Vector2.Distance(path[i-1],path[i])/2);for(int j=1;j<=count;j++)dense.Add(Vector2.Lerp(path[i-1],path[i],j/(float)count));}
// Round grid bends only where the grade remains safe.
for(int pass=0;pass<3;pass++){var smoothed=dense.ToArray();for(int i=8;i<dense.Count-8;i++){
var candidate=(dense[i-8]+dense[i]*2+dense[i+8])*.25f;float gradeA=Mathf.Abs(land.Height(candidate)-land.Height(dense[i-1]))/Mathf.Max(.01f,Vector2.Distance(candidate,dense[i-1]));float gradeB=Mathf.Abs(land.Height(candidate)-land.Height(dense[i+1]))/Mathf.Max(.01f,Vector2.Distance(candidate,dense[i+1]));
if(gradeA<.16f&&gradeB<.16f&&land.MountainSlope(candidate)<26)smoothed[i]=candidate;}dense=new List<Vector2>(smoothed);}
if(dense.Count>1024)throw new Exception("Mountain road exceeds saved route limit");return dense.ToArray();
}
public static float MaximumGrade(MeadowDistrictLandscape land){float max=0;for(int i=1;i<land.Road.Count;i++)max=Mathf.Max(max,Mathf.Abs(land.Height(land.Road[i])-land.Height(land.Road[i-1]))/Vector2.Distance(land.Road[i],land.Road[i-1]));return max;}
}
}
