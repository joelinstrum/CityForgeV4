#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;

namespace CityForge.Editor
{
    public static class EstablishedRouteChecks
    {
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value);
        private static object Get(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj);
        private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj,args);
        public static void Run()
        {
            var root = new GameObject("Disposable established route checks");
            try
            {
                var view = root.AddComponent<PrototypeRegionView>();
                var points = new[] { Vector2.zero, new Vector2(100,0), new Vector2(100,100) };
                var before = new NavigationState(view);
                var route = new EstablishedRoute(points, before);
                int probes = 0;
                Func<Vector2,Vector2,bool> clear = (a,b) => { probes++; Require(Vector2.Distance(a,b)<=1.001f,"Route check exceeded local one-metre section"); return b.x<80; };
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                for (int i=0;i<10000;i++) Require(route.CanTravel(0,Vector2.zero,Vector2.right*.1f,before,clear),"Certified route rejected");
                Require(probes==0,"Unchanged established route evaluated geometry");
                Require(GC.GetAllocatedBytesForCurrentThread()-allocated<128,"Established movement allocated per tick");
                Set(view,"<LotPresentationBuilds>k__BackingField",1);
                var changed = new NavigationState(view);
                Require(route.CanTravel(0,Vector2.zero,Vector2.right*.1f,changed,clear),"Distant new blocker invalidated whole route");
                Require(probes==1,"Edit scanned more than the entered section");
                for(int i=0;i<1000;i++)Require(route.CanTravel(0,Vector2.right*.1f,Vector2.right*.2f,changed,clear),"Cached section rejected");
                Require(probes==1,"Repeated movement rechecked the same clear section");
                Require(!route.CanTravel(0,Vector2.right*79,Vector2.right*79.1f,changed,clear),"New blocker was crossed");
                Require(route.CanTravel(0,Vector2.right*.2f,Vector2.right*.1f,changed,clear),"Reverse trip failed to reuse clearance");
                Set(view,"<LotPresentationBuilds>k__BackingField",2);
                Require(!route.CanTravel(0,Vector2.zero,Vector2.right*.1f,new NavigationState(view),(a,b)=>false),"Second edit reused stale section clearance");
                var loop = new EstablishedRoute(points,changed,false);
                Require(loop.CanTravel(2,new Vector2(50,50),new Vector2(49.9f,49.9f),changed,(a,b)=>true),"Closed circuit wrapping failed");
                CheckRoadRemoval(view,root);
                CheckPedestrianPersistence(view,root);
                Debug.Log("ESTABLISHED ROUTE CHECKS PASS: 10000 unchanged moves without geometry checks; distant edits preserve route; local checks cached, blocker stops movement; reverse/closed paths, second-edit invalidation, removed-road safety, pedestrian identity/circuit persistence.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            FreightChecks.Run();
        }
        private static void CheckRoadRemoval(PrototypeRegionView view,GameObject root)
        {
            var roads=(Dictionary<DistrictRoadRecord,MeshFilter>)Get(view,"districtRoadMeshes");
            var road=new DistrictRoadRecord { width=4,points=new[]{Vector2.zero,new Vector2(100,0)} };
            roads.Add(road,root.AddComponent<MeshFilter>());
            var original=view.CaptureRouteRoads();
            Require(view.RouteRoadAvailable(Vector2.right*50,original),"Existing road rejected");
            roads.Clear();Set(view,"<FreightRoadRevision>k__BackingField",1);
            Require(!view.RouteRoadAvailable(Vector2.right*50,original),"Deleted road remained traversable");
            Require(view.RouteRoadAvailable(new Vector2(50,10),original),"Off-road access spur rejected after unrelated road edit");
            var sidewalk = new[] { new PrototypeRegionView.RouteRoad(new[]{new Vector2(0,10),new Vector2(100,10)},1,false) };
            Set(view,"pedestrianSidewalkSnapshot",sidewalk);
            Require(view.RouteWalkingSurfaceAvailable(new Vector2(50,10),Array.Empty<PrototypeRegionView.RouteRoad>(),sidewalk),"Existing sidewalk rejected");
            Set(view,"pedestrianSidewalkSnapshot",Array.Empty<PrototypeRegionView.RouteRoad>());
            Require(!view.RouteWalkingSurfaceAvailable(new Vector2(50,10),Array.Empty<PrototypeRegionView.RouteRoad>(),sidewalk),"Removed sidewalk remained traversable");
        }
        private static void CheckPedestrianPersistence(PrototypeRegionView view,GameObject root)
        {
            var lot=new DistrictLotRecord { definition=new LotDefinition() };
            var roots=(Dictionary<DistrictLotRecord,Transform>)Get(view,"districtLotRoots");roots.Add(lot,root.transform);
            var type=typeof(PrototypeRegionView).GetNestedType("Pedestrian",BindingFlags.NonPublic);
            var walker=Activator.CreateInstance(type,true);
            var body=new GameObject("Existing pedestrian");body.transform.SetParent(root.transform);
            var circuit=new[]{Vector2.zero,Vector2.right*10,Vector2.one*10};
            Set(walker,"Body",body.transform);Set(walker,"Ambient",true);Set(walker,"OriginLot",lot);Set(walker,"Route",circuit);Set(walker,"Next",2);
            var actors=(IList)Get(view,"pedestrians");actors.Add(walker);
            Call(view,"RebuildPedestrianRoutes");Call(view,"RebuildAmbientTraffic",LotAssetCatalog.Load());
            Require(actors.Count==1&&ReferenceEquals(actors[0],walker)&&ReferenceEquals(Get(walker,"Route"),circuit)&&(int)Get(walker,"Next")==2,"Layout refresh recreated pedestrian or reset its circuit");
        }
    }
}
#endif
