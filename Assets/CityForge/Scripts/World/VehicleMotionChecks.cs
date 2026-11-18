#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForge.Lots;
using CityForge.World;
using UnityEditor;
using UnityEngine;

namespace CityForge.Editor
{
    public static class VehicleMotionChecks
    {
        private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target,value);
        public static void Run()
        {
            Require(EditorApplication.ExecuteMenuItem("CityForge/Lots/Prepare local catalog"), "Catalog preparation menu missing");
            FreightChecks.Run();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Resources/Lots/V3Vehicles/LumberWagon/Team.prefab");
            var root = UnityEngine.Object.Instantiate(prefab);
            var harness = new GameObject("Vehicle motion checks");
            try
            {
                Require(root.transform.childCount == 2, "Expected wagon and single horse");
                foreach(var animator in root.GetComponentsInChildren<Animator>()) if(animator.runtimeAnimatorController) { animator.Rebind();animator.Update(0); }
                var wagon = BoundsOf(root.transform.GetChild(0));
                var horse = BoundsOf(root.transform.GetChild(1));
                Require(Mathf.Abs(horse.size.y - 2.3f) < .03f, "Lumber horse height incorrect: " + horse.size.y);
                Require(Mathf.Abs(horse.center.x - wagon.center.x) < .02f, "Horse and wagon are not centered");
                Require(Mathf.Abs(horse.min.y) < .02f && Mathf.Abs(wagon.min.y) < .02f, "Team not seated on ground");
                var runtimeAsset = LotAssetCatalog.Load().Find("v3-vehicle-lumber-wagon");
                Require(!runtimeAsset.fitModel, "Normalized horse team still refits from skinned-renderer bounds");
                var placed = LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = runtimeAsset.id }, runtimeAsset, harness.transform);
                PrototypeRegionView.SetVehicleMotion(placed.transform, runtimeAsset.id, false);
                foreach(var animator in placed.GetComponentsInChildren<Animator>()) if(animator.runtimeAnimatorController) animator.Update(0);
                var placedHorse = BoundsOf(placed.transform.GetChild(0).GetChild(1));
                Require(Mathf.Abs(placedHorse.size.y - 2.3f) < .03f && Mathf.Abs(placedHorse.min.y) < .03f, "Runtime loading changed horse size or hoof seating");
                Debug.Log($"TEAM GEOMETRY PASS: horse height {horse.size.y:F3}m, horse/wagon lateral centers {horse.center.x:F3}/{wagon.center.x:F3}m, ground {horse.min.y:F3}/{wagon.min.y:F3}m.");
                var sharp = new[] { new Vector2(0,0), new Vector2(10,0), new Vector2(10,10) };
                float far = VehicleCornering.Speed(sharp, new Vector2(0,0), 1, 1, false, 2.25f, 2.25f, 1);
                float near = VehicleCornering.Speed(sharp, new Vector2(9.5f,0), 1, 1, false, 2.25f, 2.25f, 1);
                Require(far > 2f && near < 1.1f, "Wagon did not slow for a right-angle turn");
                float accelerated = VehicleCornering.Speed(sharp, Vector2.zero, 1, 1, false, 2.25f, .9f, .1f);
                Require(accelerated > .9f && accelerated <= .966f, "Acceleration is abrupt");
                var headingA = VehicleCornering.Heading(sharp,new Vector2(9.8f,0),1,1,false);
                var headingB = VehicleCornering.Heading(sharp,new Vector2(10,.01f),2,1,false);
                Require(Vector2.Angle(headingA,headingB) < 25f, "Look-ahead steering snapped at corner");
                var view = harness.AddComponent<PrototypeRegionView>();
                var wrapper = new GameObject("Team wrapper"); wrapper.transform.SetParent(harness.transform);
                root.transform.SetParent(wrapper.transform,false);
                var move = view.FreightVehicleMotion(wrapper.transform, "v3-vehicle-lumber-wagon");
                move(Vector2.up,.1f,.1f);
                float yaw = wrapper.transform.eulerAngles.y;
                move(Vector2.right,.1f,.1f);
                Require(Mathf.Abs(Mathf.DeltaAngle(yaw,wrapper.transform.eulerAngles.y)) <= 9.51f, "Horse heading snapped instead of turning smoothly");
                Require(VehicleScriptCatalog.All.Select(script=>script.id).Distinct().Count() == VehicleScriptCatalog.All.Length &&
                    VehicleScriptCatalog.All.Any(script=>script.category=="Freight"), "Script library definitions invalid");
                CheckEagleSidewalk(view,harness.transform);
                Debug.Log("VEHICLE MOTION CHECKS PASS: corner braking, smooth acceleration, look-ahead steering, bounded horse heading rate, scalable script definitions, sidewalk pickup exception with interior collision preserved.");
            }
            finally { UnityEngine.Object.DestroyImmediate(harness); if(root) UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CheckEagleSidewalk(PrototypeRegionView view, Transform parent)
        {
            var lot = new DistrictLotRecord { definition = new LotDefinition() };
            var lotRoot = new GameObject("Sidewalk lot").transform; lotRoot.SetParent(parent);
            var lots = (Dictionary<DistrictLotRecord,Transform>)view.GetType().GetField("districtLotRoots",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
            lots.Add(lot,lotRoot);
            var left = new GameObject("Test grip left").transform; left.SetParent(parent);
            var right = new GameObject("Test grip right").transform; right.SetParent(parent);
            Set(view,"eagleLeftGrip",left); Set(view,"eagleRightGrip",right);
            var envelopes = (List<(Bounds bounds, Matrix4x4 worldToLocal, float roof)>)view.GetType().GetField("eagleBuildingBounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
            envelopes.Add((new Bounds(new Vector3(0,5,0),new Vector3(24,10,32)),Matrix4x4.identity,12));
            var canReach = view.GetType().GetMethod("CanReachEaglePedestrian",BindingFlags.NonPublic|BindingFlags.Instance);
            left.position = right.position = new Vector3(11,1.62f,0);
            Require((bool)canReach.Invoke(view,new object[]{new Vector3(11.3f,1.62f,0)}),"Outer sidewalk blocked by building renderer envelope");
            left.position = right.position = new Vector3(0,1.62f,0);
            Require(!(bool)canReach.Invoke(view,new object[]{new Vector3(.3f,1.62f,0)}),"Interior building collision was disabled");
            Require(EagleSidewalkGeometry.OuterEdge(lot.definition.LandBounds,new Vector2(11,0)) &&
                !EagleSidewalkGeometry.OuterEdge(lot.definition.LandBounds,Vector2.zero),"Sidewalk band leaked into lot interior");
        }
        private static Bounds BoundsOf(Transform root)
        {
            bool first = true; var result = new Bounds();
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var b=renderer.localBounds;
                if(renderer is SkinnedMeshRenderer skin && skin.sharedMesh)
                {
                    var mesh=skin.sharedMesh;var weights=mesh.boneWeights;var vertices=mesh.vertices;
                    var matrices=skin.bones.Select((bone,index)=>bone.localToWorldMatrix*mesh.bindposes[index]).ToArray();
                    for(int i=0;i<vertices.Length;i++)
                    {
                        var v=vertices[i];var w=weights[i];var point=Vector3.zero;
                        if(w.weight0>0)point+=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0;
                        if(w.weight1>0)point+=matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1;
                        if(w.weight2>0)point+=matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2;
                        if(w.weight3>0)point+=matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                        if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);
                    }
                    continue;
                }
                for(int i=0;i<8;i++)
                {
                    var p=renderer.transform.TransformPoint(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z));
                    if(first){result=new Bounds(p,Vector3.zero);first=false;} else result.Encapsulate(p);
                }
            }
            return result;
        }
        public static void Render()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Resources/Lots/V3Vehicles/LumberWagon/Team.prefab");
            var team=UnityEngine.Object.Instantiate(prefab);
            foreach(var animator in team.GetComponentsInChildren<Animator>()) if(animator.runtimeAnimatorController) { animator.Rebind();animator.Update(0); }
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(team,scene);
            var lightObject=new GameObject("Preview sun"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);
            var sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2;lightObject.transform.rotation=Quaternion.Euler(40,-30,0);
            var cameraObject=new GameObject("Preview camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=4.5f;
            camera.backgroundColor=new Color(.14f,.17f,.19f);camera.clearFlags=CameraClearFlags.SolidColor;
            Directory.CreateDirectory("Library/VehicleMotionChecks");
            var target=new RenderTexture(1200,800,24);camera.targetTexture=target;
            var previous=RenderTexture.active;
            try
            {
                foreach(var angle in new[]{"side","front"})
                {
                    cameraObject.transform.position=angle=="side"?new Vector3(10,4,-2):new Vector3(0,3,10);
                    cameraObject.transform.LookAt(new Vector3(0,1,-1.5f));
                    camera.Render();RenderTexture.active=target;
                    var image=new Texture2D(1200,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,800),0,0);image.Apply();
                    File.WriteAllBytes("Library/VehicleMotionChecks/lumber-"+angle+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                }
            }
            finally { RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
#endif
