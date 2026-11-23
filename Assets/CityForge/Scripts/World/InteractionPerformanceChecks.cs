#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using CityForge.Lots;
using UnityEngine;
namespace CityForge.Editor
{
    public static class InteractionPerformanceChecks
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,F).SetValue(obj,value);
        static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,F).Invoke(obj,args);
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        public static void Run()
        {
            PlayerGrowthChecks.Run();
            var root=new GameObject("Disposable local demolition grading");
            var data=new TerrainData{heightmapResolution=65,size=new Vector3(64,64,64)};
            var heights=new float[65,65];for(int z=0;z<65;z++)for(int x=0;x<65;x++)heights[z,x]=20f/64;
            data.SetHeights(0,0,heights);
            try
            {
                var view=root.AddComponent<World.PrototypeRegionView>();
                var terrain=root.AddComponent<Terrain>();terrain.terrainData=data;
                var land=root.AddComponent<World.MeadowDistrictLandscape>();
                Set(view,"approvedDistrict",land);Set(view,"<SelectedDistrict>k__BackingField",-1);
                Set(land,"<DisplayTerrain>k__BackingField",terrain);
                var grading=root.AddComponent<World.DistrictLotGrading>();Call(grading,"Awake");
                var removed=new World.DistrictLotRecord{center=new Vector2(16,16),cutToLowest=true,cutGroundHeight=18,
                    definition=new LotDefinition{width=4,length=4}};
                var kept=new World.DistrictLotRecord{center=new Vector2(32,16),cutToLowest=true,cutGroundHeight=17,
                    definition=new LotDefinition{width=4,length=4}};
                view.PlacedLots.Add(kept);
                grading.Cut(new Rect(8,8,16,16),p=>new Rect(8,8,16,16).Contains(p),18,p=>false,exact:true);
                grading.Cut(new Rect(24,8,16,16),p=>new Rect(24,8,16,16).Contains(p),17,p=>false,exact:true);grading.Finish();
                view.ZoneGroundPlanes.Add(new World.ZoneGroundPlane{center=new Vector2(18,20),size=Vector2.one*4,height=18});
                Call(view,"RestoreRemovedLotGround",new HashSet<World.DistrictLotRecord>{removed});
                Need(Mathf.Abs(data.GetHeight(16,16)-20)<.002f,"Deleted standalone lot did not restore its original ground");
                Need(Mathf.Abs(data.GetHeight(32,16)-17)<.002f,"Local deletion modified a surviving lot's grade");
                Need(Mathf.Abs(data.GetHeight(18,20)-18)<.002f,"Deletion undid a shared authored zone grade");
                Debug.Log("INTERACTION CHECKS PASS: deleted standalone grading restores locally; neighboring lot and authored zone grades survive.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(data);}
        }
    }
}
#endif
