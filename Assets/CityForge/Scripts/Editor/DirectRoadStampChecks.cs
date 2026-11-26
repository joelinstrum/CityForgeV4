#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEditor;using CityForge.World;using CityForge.UI;
public static class DirectRoadStampChecks {
 static readonly string Out=Path.GetFullPath("Library/RoadShapeChecks")+Path.DirectorySeparatorChar;
 static readonly BindingFlags I=BindingFlags.NonPublic|BindingFlags.Instance;static List<string> checks=new();
 static void Check(bool b,string s){if(!b)throw new Exception(s);checks.Add("PASS "+s);}
 class Surface:PointWireSurface {public Collider Ground;public override bool Available=>true;public override bool CanEdit=>true;public override Collider Collider=>Ground;public override bool Contains(Vector2 p)=>true;public override float Height(Vector2 p)=>0;}
 public static void Run(){Directory.CreateDirectory(Out);try{
 var host=new GameObject("Direct road gesture check");var view=host.AddComponent<PrototypeRegionView>();view.enabled=false;var wire=host.AddComponent<PointWireTool>();var cam=new GameObject("Camera",typeof(Camera)).GetComponent<Camera>();var hud=new GameObject("HUD",typeof(Canvas)).GetComponent<Canvas>();var collider=host.AddComponent<BoxCollider>();wire.Initialize(cam,hud.transform,new Surface{Ground=collider},Color.cyan);wire.Label="Dirt Road";wire.OpenPath=true;wire.Arm();
 var type=typeof(PrototypeRegionView);type.GetField("roadWire",I).SetValue(view,wire);var button=new GameObject("Finish",typeof(RectTransform),typeof(Button));var text=new GameObject("Caption",typeof(RectTransform),typeof(Text));text.transform.SetParent(button.transform);type.GetField("finishRoadButton",I).SetValue(view,button.GetComponent<Button>());
 var road=new DistrictRoadRecord{surfaceId="dirt",points=new[]{Vector2.zero,new Vector2(40,0)},width=3};var mesh=host.AddComponent<MeshFilter>();((IDictionary)type.GetField("districtRoadMeshes",I).GetValue(view)).Add(road,mesh);
 var hint=type.GetMethod("RefreshRoadPlacementHint",I);
 hint.Invoke(view,new object[]{true});Check(wire.DrawingHint.Contains("r/d/t/x/s/i"),"Open-ground road hint advertises every shape");
 wire.PlaceMarker(Vector2.zero);hint.Invoke(view,new object[]{true});Check(wire.DrawingHint.StartsWith("Continue")&&wire.DrawingHint.Contains("r/d/t/x/s/i"),"Existing-road continuation advertises every shape without Transport");
 wire.PlaceOnRelease=true;hint.Invoke(view,new object[]{true});Check(wire.DrawingHint.Contains("r/d/t/x/s/i"),"Drag/release street hint advertises every shape");
 var shape=type.GetNestedType("RoadStampShape",BindingFlags.NonPublic);
 foreach(var name in new[]{"Roundabout","D","T","X","S","Intersection"}){
 wire.PlaceMarker(new Vector2(10,0));type.GetField("roadGestureStart",I).SetValue(view,(Vector2?)new Vector2(20,20));
 type.GetMethod("SetRoadStamp",I).Invoke(view,new object[]{Enum.Parse(shape,name)});
 Check(type.GetField("roadGestureStart",I).GetValue(view)==null,name+" detaches in-progress direct drag");
 Check(wire.Points.Count==0&&wire.StampPaths!=null&&wire.Armed,name+" replaces unfinished line with stamp preview");
 type.GetMethod("CompleteRoadGesture",I).Invoke(view,new object[]{new Event{type=EventType.MouseUp,button=0,mousePosition=new Vector2(120,120)}});
 Check(((ICollection)type.GetField("districtRoads",I).GetValue(view)).Count==0,name+" old drag release cannot commit a stray road");
 type.GetMethod("SetRoadStamp",I).Invoke(view,new object[]{Enum.Parse(shape,"None")});
 Check(wire.Armed&&wire.StampPaths==null,name+" exits to ordinary drawing without Transport");
 }
 wire.Cancel();File.WriteAllLines(Out+"Direct-Drag-Checks.txt",checks);EditorApplication.Exit(0);
 }catch(Exception e){File.WriteAllText(Out+"Direct-Drag-Error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
