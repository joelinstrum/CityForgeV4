#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;using UnityEditor;using CityForge.World;using CityForge.UI;using CityForge.Lots;
public static class DiagonalRoadChecks {
 static readonly string Out=Path.GetFullPath("Library/RoadShapeChecks")+Path.DirectorySeparatorChar;
 static readonly BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic;
 static List<string> checks=new();static PrototypeRegionView view;
 static void Check(bool pass,string text){if(!pass)throw new Exception(text);checks.Add("PASS "+text);}
 static Vector2[] Align(Vector2[] p,bool loop=false,int width=4,int moved=-1,Vector2[] original=null)=>(Vector2[])typeof(PrototypeRegionView).GetMethod("AlignRoadGuide",I).Invoke(view,new object[]{p,loop,width,moved,original});
 static bool Same(Vector2 a,Vector2 b)=>(a-b).sqrMagnitude<.000001f;
 static void Directions(Vector2[] p,bool closed,string label){for(int i=0;i<p.Length-(closed?0:1);i++){var d=p[(i+1)%p.Length]-p[i];Check(Mathf.Abs(d.x)<.001f||Mathf.Abs(d.y)<.001f||Mathf.Abs(Mathf.Abs(d.x)-Mathf.Abs(d.y))<.001f,label+" segment "+i);}}
 public static void Run(){Directory.CreateDirectory(Out);try{
 var go=new GameObject("Isolated diagonal checks");view=go.AddComponent<PrototypeRegionView>();view.enabled=false;
 var old=new LegacyRoadAlignment();var random=new System.Random(2718);
 foreach(int width in new[]{3,4,12,20})for(int j=0;j<50;j++){
 float x=(float)random.NextDouble()*100-50,y=(float)random.NextDouble()*100-50;
 var p=new[]{new Vector2(x,y),new Vector2(x+20,y),new Vector2(x+20,y+24),new Vector2(x-8,y+24)};
 foreach(bool closed in new[]{false,true})foreach(int moved in new[]{-1,1}){
 var a=Align(p,closed,width,moved,moved<0?null:p);var b=old.AlignRoadGuide(p,closed,width,moved,moved<0?null:p);
 Check(a.Zip(b,Same).All(v=>v),"Existing cardinal alignment unchanged width "+width+" loop "+closed+" drag "+moved);
 }}
 foreach(int width in new[]{3,4,12,20})for(int angle=0;angle<360;angle+=45){float r=angle*Mathf.Deg2Rad;var d=new Vector2(Mathf.Cos(r),Mathf.Sin(r));var p=new[]{new Vector2(2,2),new Vector2(2,2)+d*48};var a=Align(p,false,width);Directions(a,false,"45-degree heading "+angle);Check(Vector2.Dot((a[1]-a[0]).normalized,d)>.999f,"Heading preserved "+angle);}
 var mixed=new[]{new Vector2(2,2),new Vector2(34,2),new Vector2(66,34),new Vector2(66,66),new Vector2(34,98)};
 var m=Align(mixed);Directions(m,false,"Mixed cardinal/diagonal guide");var dragged=(Vector2[])m.Clone();dragged[2]+=new Vector2(8,4);var edit=Align(dragged,false,4,2,m);Directions(edit,false,"Dragged mixed corner");
 var diamond=new[]{new Vector2(0,40),new Vector2(40,0),new Vector2(0,-40),new Vector2(-40,0)};Directions(Align(diamond,true),true,"Closed diagonal loop");
 var wire=go.AddComponent<PointWireTool>();typeof(PrototypeRegionView).GetField("roadWire",I).SetValue(view,wire);var path=(PointWirePath)typeof(PointWireTool).GetField("path",I).GetValue(wire);path.TryAdd(new Vector2(2,2));
 var snap=typeof(PrototypeRegionView).GetMethod("SnapRoadGridPoint",I);
 for(int angle=0;angle<360;angle+=45){float r=(angle+4)*Mathf.Deg2Rad;var raw=new Vector2(2,2)+new Vector2(Mathf.Cos(r),Mathf.Sin(r))*38;var p=(Vector2)snap.Invoke(view,new object[]{raw});Directions(new[]{new Vector2(2,2),p},false,"Mouse preview "+angle);Check(Mathf.Abs(Vector2.Distance(new Vector2(2,2),p)/4-Mathf.Round(Vector2.Distance(new Vector2(2,2),p)/4))<.001f,"Rotated four-metre spacing "+angle);}
 var record=new DistrictRoadRecord{points=m,width=4,gridAligned=true,surfaceId="dirt"};var json=JsonUtility.ToJson(record);var saved=JsonUtility.FromJson<DistrictRoadRecord>(json);Check(saved.gridAligned&&saved.points.Zip(m,Same).All(v=>v),"Diagonal coordinates and grid flag survive serialization");
 // A guide extension must keep its old endpoint and all snapped headings.
 var extended=(Vector2[])typeof(PrototypeRegionView).GetMethod("BuildRoadExtensionGuide",I).Invoke(view,new object[]{record,false,new[]{m[^1],m[^1]+new Vector2(28,28)}});Check(Same(extended[m.Length-1],m[^1]),"Extension retains exact existing endpoint");Directions(extended,false,"Connected extension");
 File.WriteAllLines(Out+"Diagonal-Checks.txt",checks);File.WriteAllText(Out+"Guides.json",JsonUtility.ToJson(new Proof{mixed=m,dragged=edit,diamond=Align(diamond,true)},true));UnityEngine.Object.DestroyImmediate(go);EditorApplication.Exit(0);
 }catch(Exception e){File.WriteAllText(Out+"Diagonal-Error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
 [Serializable]class Proof{public Vector2[] mixed,dragged,diamond;}
}
#endif
